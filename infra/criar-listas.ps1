<#
.SYNOPSIS
  Cria no SharePoint as listas do P03 (NotasRecebidas, biblioteca XML NF-e, LogErros) e, se o P01
  ainda não estiver instalado, uma versão mínima de Fornecedores e Solicitacoes com os 18 pedidos de teste.

.EXAMPLE
  ./criar-listas.ps1 -Site https://SEUTENANT.sharepoint.com/sites/lumina-processos -ClientId <id do app do PnP>

.NOTES
  Requer PowerShell 7 e o módulo PnP.PowerShell (Install-Module PnP.PowerShell -Scope CurrentUser).
  Desde 2024 o PnP exige um registro de aplicativo próprio: Register-PnPEntraIDAppForInteractiveLogin.
  Rodar só com aprovação do Allan (altera o ambiente Microsoft). Idempotente: o que já existe é mantido.
  Dados fictícios (Lumina Componentes).
#>
param(
    [Parameter(Mandatory)] [string] $Site,
    [Parameter(Mandatory)] [string] $ClientId,
    [switch] $SemPedidosDeTeste
)
$ErrorActionPreference = 'Stop'
$dados = Join-Path $PSScriptRoot '..\dados'
Connect-PnPOnline -Url $Site -Interactive -ClientId $ClientId

function Garantir-Lista([string]$titulo, [string]$modelo = 'GenericList') {
    $l = Get-PnPList -Identity $titulo -ErrorAction SilentlyContinue
    if (-not $l) { $l = New-PnPList -Title $titulo -Template $modelo -OnQuickLaunch; Write-Host "Criada: $titulo" }
    else { Write-Host "Já existe: $titulo" }
    return $l
}
function Garantir-Campo([string]$lista, [string]$interno, [string]$exibicao, [string]$tipo, [hashtable]$extra = @{}) {
    if (Get-PnPField -List $lista -Identity $interno -ErrorAction SilentlyContinue) { return }
    $p = @{ List = $lista; InternalName = $interno; DisplayName = $exibicao; Type = $tipo; AddToDefaultView = $true }
    if ($extra.Choices) { $p.Choices = $extra.Choices }
    Add-PnPField @p | Out-Null
    if ($extra.Indexado) { Set-PnPField -List $lista -Identity $interno -Values @{ Indexed = $true; EnforceUniqueValues = [bool]$extra.Unico } | Out-Null }
}
function Garantir-Pesquisa([string]$lista, [string]$interno, [string]$exibicao, [string]$listaAlvo) {
    if (Get-PnPField -List $lista -Identity $interno -ErrorAction SilentlyContinue) { return }
    $alvo = Get-PnPList -Identity $listaAlvo
    $xml = "<Field Type='Lookup' DisplayName='$exibicao' Name='$interno' StaticName='$interno' List='{$($alvo.Id)}' ShowField='Title' />"
    Add-PnPFieldFromXml -List $lista -FieldXml $xml | Out-Null
}

# ---------- P01 mínimo (só se o P01 não estiver instalado) ----------
if (-not $SemPedidosDeTeste) {
    Garantir-Lista 'Fornecedores' | Out-Null
    Garantir-Campo 'Fornecedores' 'CNPJ' 'CNPJ' 'Text' @{ Indexado = $true }
    Garantir-Lista 'Solicitacoes' | Out-Null
    Garantir-Campo 'Solicitacoes' 'Tipo' 'Tipo' 'Choice' @{ Choices = 'Compra', 'Reembolso', 'Férias' }
    Garantir-Campo 'Solicitacoes' 'Status' 'Status' 'Choice' @{ Choices = 'Rascunho', 'Em andamento', 'Aprovado', 'Reprovado', 'Cancelado'; Indexado = $true }
    Garantir-Pesquisa 'Solicitacoes' 'Fornecedor' 'Fornecedor' 'Fornecedores'
    Garantir-Campo 'Solicitacoes' 'ValorTotal' 'Valor total' 'Currency'
    Garantir-Campo 'Solicitacoes' 'DataConclusao' 'Data de conclusão' 'DateTime'

    $idPorCnpj = @{}
    foreach ($f in (Import-Csv (Join-Path $dados 'seed\fornecedores.csv') -Delimiter ';')) {
        $item = Get-PnPListItem -List 'Fornecedores' -Query "<View><Query><Where><Eq><FieldRef Name='CNPJ'/><Value Type='Text'>$($f.CNPJ)</Value></Eq></Where></Query></View>"
        if (-not $item) { $item = Add-PnPListItem -List 'Fornecedores' -Values @{ Title = $f.Nome; CNPJ = $f.CNPJ } }
        $idPorCnpj[$f.CNPJ] = $item.Id
    }
    $pedidos = Import-Csv (Join-Path $dados 'pedidos-aprovados.csv') -Delimiter ';' | Group-Object Numero
    foreach ($p in $pedidos) {
        $l = $p.Group[0]
        $existe = Get-PnPListItem -List 'Solicitacoes' -Query "<View><Query><Where><Eq><FieldRef Name='Title'/><Value Type='Text'>$($p.Name)</Value></Eq></Where></Query></View>"
        if ($existe) { continue }
        Add-PnPListItem -List 'Solicitacoes' -Values @{
            Title = $p.Name; Tipo = 'Compra'; Status = 'Aprovado'; Fornecedor = $idPorCnpj[$l.CNPJ]
            ValorTotal = [double]::Parse($l.ValorTotalPedido, [cultureinfo]::InvariantCulture); DataConclusao = $l.DataAprovacao
        } | Out-Null
        Write-Host "Pedido $($p.Name) carregado"
    }
}

# ---------- P03 ----------
Garantir-Lista 'NotasRecebidas' | Out-Null
Set-PnPField -List 'NotasRecebidas' -Identity 'Title' -Values @{ Title = 'Chave de acesso' } | Out-Null
Set-PnPField -List 'NotasRecebidas' -Identity 'Title' -Values @{ Indexed = $true; EnforceUniqueValues = $true } | Out-Null
Garantir-Campo 'NotasRecebidas' 'Numero' 'Número' 'Number'
Garantir-Campo 'NotasRecebidas' 'Serie' 'Série' 'Number'
Garantir-Campo 'NotasRecebidas' 'DataEmissao' 'Data de emissão' 'DateTime'
Garantir-Campo 'NotasRecebidas' 'FornecedorCNPJ' 'CNPJ do fornecedor' 'Text'
Garantir-Campo 'NotasRecebidas' 'FornecedorNome' 'Fornecedor' 'Text'
Garantir-Campo 'NotasRecebidas' 'ValorTotal' 'Valor total' 'Currency'
if (-not $SemPedidosDeTeste -or (Get-PnPList -Identity 'Solicitacoes' -ErrorAction SilentlyContinue)) {
    Garantir-Pesquisa 'NotasRecebidas' 'PedidoVinculado' 'Pedido vinculado' 'Solicitacoes'
}
Garantir-Campo 'NotasRecebidas' 'Resultado' 'Resultado' 'Choice' @{ Choices = 'Conferida', 'Divergente', 'Sem pedido'; Indexado = $true }
Garantir-Campo 'NotasRecebidas' 'Motivo' 'Motivo' 'Note'
Garantir-Campo 'NotasRecebidas' 'StatusLancamento' 'Status do lançamento' 'Choice' @{ Choices = 'Pendente', 'Lançada', 'Erro', 'Não se aplica'; Indexado = $true }
Garantir-Campo 'NotasRecebidas' 'NumeroLancamentoERP' 'Lançamento no ERP' 'Text'
Garantir-Campo 'NotasRecebidas' 'ArquivoXML' 'Arquivo XML' 'URL'
Garantir-Campo 'NotasRecebidas' 'ItensJSON' 'Itens (JSON)' 'Note'
Garantir-Campo 'NotasRecebidas' 'CapturaErro' 'Captura do erro' 'URL'

Garantir-Lista 'XML NF-e' 'DocumentLibrary' | Out-Null
Garantir-Lista 'LogErros' | Out-Null
Garantir-Campo 'LogErros' 'Fluxo' 'Fluxo' 'Text'
Garantir-Campo 'LogErros' 'Mensagem' 'Mensagem' 'Note'
Garantir-Campo 'LogErros' 'Item' 'Item' 'Text'
Garantir-Campo 'LogErros' 'Data' 'Data' 'DateTime'

Write-Host "Pronto. Confira as listas em $Site"
