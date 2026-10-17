<#
.SYNOPSIS
  Manda os 20 XMLs de teste (dados/notas) como e-mails para a caixa monitorada, um por e-mail,
  pelo Outlook clássico instalado (COM). Simula fornecedores enviando notas para fiscal@.

.EXAMPLE
  ./enviar-emails-teste.ps1 -Para "sua.caixa@seudominio.com" -Intervalo 5

.NOTES
  Windows PowerShell 5.1 ou 7 com o Outlook clássico aberto. Envia e-mails de verdade: rodar só com aprovação.
  O assunto leva [NF-e TESTE] para a regra do Outlook mover para a pasta monitorada pelo fluxo.
#>
param(
    [Parameter(Mandatory)] [string] $Para,
    [int] $Intervalo = 5,
    [int] $Quantidade = 20
)
$ErrorActionPreference = 'Stop'
$pasta = Join-Path $PSScriptRoot '..\dados\notas'
$gabarito = Import-Csv (Join-Path $PSScriptRoot '..\dados\gabarito.csv') -Delimiter ';'
$outlook = New-Object -ComObject Outlook.Application

foreach ($g in ($gabarito | Select-Object -First $Quantidade)) {
    $mail = $outlook.CreateItem(0)
    $mail.To = $Para
    $mail.Subject = "[NF-e TESTE] NF-e $($g.Numero) - $($g.Fornecedor)"
    $mail.Body = "Segue a nota fiscal $($g.Numero) referente ao pedido $($g.Pedido).`r`n`r`n$($g.Fornecedor) (empresa fictícia)`r`nDocumento fictício para demonstração."
    $mail.Attachments.Add((Resolve-Path (Join-Path $pasta $g.Arquivo)).Path) | Out-Null
    $mail.Send()
    Write-Host "$($g.Seq.PadLeft(2,'0')) enviado: NF-e $($g.Numero) ($($g.ResultadoEsperado) esperado)"
    Start-Sleep -Seconds $Intervalo
}
