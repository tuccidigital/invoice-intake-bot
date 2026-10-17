# Fluxos · P03 Robô de Entrada de NF-e

*Dados fictícios. Lumina Componentes é uma empresa fictícia.*

Dois fluxos, dentro da solução `Tucci` (prefixo `tcc`). O Allan monta no designer seguindo este passo a passo; para colar ação por ação, use a skill `flow-clipboard`. As regras de conferência têm uma implementação de referência em `scripts/conferir.py`: se o fluxo discordar dela com os 20 XMLs de teste, o fluxo está errado.

## Variáveis de ambiente

| Nome | Exemplo | Uso |
| --- | --- | --- |
| `tcc_SiteProcessos` | `https://contoso.sharepoint.com/sites/lumina-processos` | Site com as listas |
| `tcc_PastaMonitorada` | `Fiscal/NF-e` | Pasta do Outlook que recebe as notas |
| `tcc_ToleranciaValor` | `0.02` | Diferença aceita entre nota e pedido (2%) |
| `tcc_EquipeCompras` / `tcc_CanalCompras` | ids do Teams | Aviso de divergência |

## Fluxo 1 · `TCC | NF-e | Nova nota por e-mail`

**Gatilho:** Office 365 Outlook · *Quando um novo email chegar (V3)*. Pasta: `tcc_PastaMonitorada`. Incluir anexos: Sim. Somente com anexos: Sim. Na demo, uma regra do Outlook move os e-mails com `[NF-e TESTE]` no assunto para essa pasta.

**Configuração do gatilho:** controle de simultaneidade ligado, grau 1. As notas entram uma por vez, e a checagem de duplicidade não sofre corrida.

### Escopo `Try`

1. **Inicializar variável** `vResultado` (Cadeia) = `""`, `vMotivo` (Cadeia) = `""`, `vPedidoId` (Inteiro) = `0`.

2. **Aplicar a cada** `triggerOutputs()?['body/attachments']`
   - **Condição** `É XML`: `endsWith(toLower(items('Aplicar_a_cada')?['name']), '.xml')` é igual a `true`. Se não, siga para o próximo anexo.

   Dentro do **Sim**:

   3. **Compor** `XML`:
      ```
      xml(base64ToString(items('Aplicar_a_cada')?['contentBytes']))
      ```
   4. **Compor** `Campos` (um objeto com tudo que a nota diz; `string()` no XPath devolve texto direto, sem array):
      ```json
      {
        "chave":   "@{substring(xpath(outputs('XML'), 'string(//*[local-name()=\"infNFe\"]/@Id)'), 3)}",
        "numero":  "@{xpath(outputs('XML'), 'string(//*[local-name()=\"ide\"]/*[local-name()=\"nNF\"])')}",
        "serie":   "@{xpath(outputs('XML'), 'string(//*[local-name()=\"ide\"]/*[local-name()=\"serie\"])')}",
        "emissao": "@{xpath(outputs('XML'), 'string(//*[local-name()=\"ide\"]/*[local-name()=\"dhEmi\"])')}",
        "cnpj":    "@{xpath(outputs('XML'), 'string(//*[local-name()=\"emit\"]/*[local-name()=\"CNPJ\"])')}",
        "nome":    "@{xpath(outputs('XML'), 'string(//*[local-name()=\"emit\"]/*[local-name()=\"xNome\"])')}",
        "valor":   "@{xpath(outputs('XML'), 'string(//*[local-name()=\"ICMSTot\"]/*[local-name()=\"vNF\"])')}",
        "pedido":  "@{xpath(outputs('XML'), 'string((//*[local-name()=\"xPed\"])[1])')}"
      }
      ```
   5. **Selecionar** `Itens`. De: `xpath(outputs('XML'), '//*[local-name()="det"]')`. Mapear (modo texto):
      ```
      {
        "codigo": "@{xpath(xml(item()), 'string(//*[local-name()=\"cProd\"])')}",
        "descricao": "@{xpath(xml(item()), 'string(//*[local-name()=\"xProd\"])')}",
        "quantidade": "@{xpath(xml(item()), 'string(//*[local-name()=\"qCom\"])')}",
        "valorUnitario": "@{xpath(xml(item()), 'string(//*[local-name()=\"vUnCom\"])')}"
      }
      ```
   6. **Condição** `Chave válida`: `length(outputs('Campos')?['chave'])` é igual a `44`. Se não: registrar em `LogErros` ("Anexo XML não é NF-e") e seguir.

   7. **SharePoint · Obter itens** `Duplicada?` na lista `NotasRecebidas`. Consulta de filtro: `Title eq '@{outputs('Campos')?['chave']}'`. Contagem superior: 1.
      **Condição** `length(outputs('Duplicada?')?['body/value'])` é maior que `0` → **Sim:** registrar em `LogErros` (`Fluxo` = nome do fluxo, `Mensagem` = `Nota duplicada ignorada`, `Item` = chave) e ir para o próximo anexo (nenhuma outra ação nesse ramo).

   No **Não** (nota nova):

   8. **SharePoint · Obter itens** `Pedido` na lista `Solicitacoes`. Filtro (só se `pedido` não estiver vazio; senão, pule para o passo 10 com `Sem pedido`):
      ```
      Title eq '@{outputs('Campos')?['pedido']}' and Tipo eq 'Compra'
      ```
   9. **SharePoint · Obter itens** `Fornecedor` na lista `Fornecedores`. Filtro com o CNPJ formatado como está na lista:
      ```
      CNPJ eq '@{concat(substring(outputs('Campos')?['cnpj'],0,2),'.',substring(outputs('Campos')?['cnpj'],2,3),'.',substring(outputs('Campos')?['cnpj'],5,3),'/',substring(outputs('Campos')?['cnpj'],8,4),'-',substring(outputs('Campos')?['cnpj'],12,2))}'
      ```
   10. **Conferência** (condições aninhadas, gravando `vResultado` e `vMotivo`):

       | Ordem | Teste | Resultado | Motivo |
       | --- | --- | --- | --- |
       | a | `empty(outputs('Campos')?['pedido'])` | Sem pedido | `Nota sem número de pedido (xPed)` |
       | b | `length(outputs('Pedido')?['body/value'])` igual a 0 | Sem pedido | `Pedido @{outputs('Campos')?['pedido']} não encontrado` |
       | c | `first(outputs('Pedido')?['body/value'])?['Status/Value']` diferente de `Aprovado` | Divergente | `Pedido @{...} está @{status}, não Aprovado` |
       | d | `first(outputs('Pedido')?['body/value'])?['Fornecedor/Id']` diferente de `first(outputs('Fornecedor')?['body/value'])?['ID']` (ou fornecedor não achado) | Divergente | `Fornecedor da nota diferente do fornecedor do pedido` |
       | e | diferença acima da tolerância (abaixo) | Divergente | texto abaixo |
       | f | nenhum dos anteriores | Conferida | vazio |

       Diferença (Compor `Diferenca`):
       ```
       div(sub(float(outputs('Campos')?['valor']), float(first(outputs('Pedido')?['body/value'])?['ValorTotal'])), float(first(outputs('Pedido')?['body/value'])?['ValorTotal']))
       ```
       Teste do passo e: `greater(abs(outputs('Diferenca')), float(parameters('tcc_ToleranciaValor')))`. Não existe `abs()` no Power Automate: use `max(outputs('Diferenca'), mul(outputs('Diferenca'), -1))`.

       Motivo da divergência de valor (é o que o critério de aceite pede: valor da nota, do pedido e diferença percentual):
       ```
       concat('Valor da nota ', formatNumber(float(outputs('Campos')?['valor']), 'C2', 'pt-BR'),
              ', pedido ', formatNumber(float(first(outputs('Pedido')?['body/value'])?['ValorTotal']), 'C2', 'pt-BR'),
              ', diferença ', formatNumber(mul(outputs('Diferenca'), 100), '+0.0;-0.0', 'pt-BR'), '%')
       ```

   11. **SharePoint · Criar arquivo** na biblioteca `XML NF-e`. Caminho da pasta: `/XML NF-e/@{formatDateTime(utcNow(), 'yyyy')}/@{formatDateTime(utcNow(), 'MM')}`. Nome: `NFe@{outputs('Campos')?['chave']}.xml`. Conteúdo: `base64ToBinary(items('Aplicar_a_cada')?['contentBytes'])`. A ação cria as pastas de ano e mês se não existirem.

   12. **SharePoint · Criar item** em `NotasRecebidas`:

       | Coluna | Valor |
       | --- | --- |
       | Title (Chave de acesso) | `outputs('Campos')?['chave']` |
       | Numero | `int(outputs('Campos')?['numero'])` |
       | Serie | `int(outputs('Campos')?['serie'])` |
       | DataEmissao | `outputs('Campos')?['emissao']` |
       | FornecedorCNPJ | `outputs('Campos')?['cnpj']` |
       | FornecedorNome | `outputs('Campos')?['nome']` |
       | ValorTotal | `float(outputs('Campos')?['valor'])` |
       | PedidoVinculado Id | `if(equals(length(outputs('Pedido')?['body/value']), 0), null, first(outputs('Pedido')?['body/value'])?['ID'])` |
       | Resultado | `variables('vResultado')` |
       | Motivo | `variables('vMotivo')` |
       | StatusLancamento | `if(equals(variables('vResultado'), 'Conferida'), 'Pendente', 'Não se aplica')` |
       | ArquivoXML | link do arquivo criado no passo 11 (`body('Criar_arquivo')?['{Link}']`) |
       | ItensJSON | `string(body('Itens'))` |

   13. **Condição** `variables('vResultado')` diferente de `Conferida` → **Teams · Postar mensagem em um chat ou canal** (como Flow bot) no canal de Compras:
       ```
       NF-e @{outputs('Campos')?['numero']} de @{outputs('Campos')?['nome']}: @{variables('vResultado')}.
       @{variables('vMotivo')}
       Abrir a nota: @{body('Criar_item')?['{Link}']}
       ```

### Escopo `Catch` (executar após: `Try` falhou ou atingiu o tempo limite)

- **Filtrar matriz** `Falhas`: `result('Try')` onde `item()?['status']` é igual a `Failed`.
- **Criar item** em `LogErros`: `Fluxo` = `workflow()?['tags']?['flowDisplayName']`, `Mensagem` = `string(first(body('Falhas'))?['error'])`, `Item` = `triggerOutputs()?['body/subject']`, `Data` = `utcNow()`.
- **Postar no Teams** para o Allan (na demo) ou para o dono do processo: "Falha ao ler NF-e do e-mail <assunto>. Veja LogErros."

### Escopo `Finally` (executar após: `Catch` concluído, ignorado ou falhou)

- Nenhuma ação obrigatória. Na demo, **Compor** com a contagem do dia para conferir o critério de aceite.

### Teste de aceite

Rodar `infra/enviar-emails-teste.ps1 -Para <sua caixa>` (20 e-mails, 5 segundos entre eles). Esperado na lista `NotasRecebidas`: 14 Conferida, 3 Divergente, 2 Sem pedido; e 1 linha "Nota duplicada ignorada" em `LogErros`. Conferir item a item com `dados/gabarito.csv`.

## Fluxo 2 · `TCC | NF-e | Lançar no ERP` (desktop)

Power Automate para desktop, rodando na máquina onde o ERP Atlas está instalado.

### Duas formas de disparar

| Modo | Como | Licença |
| --- | --- | --- |
| **Produção** | Fluxo de nuvem `TCC | NF-e | Fila do ERP` (agendado, a cada 30 min, horário comercial) lê `NotasRecebidas` com `Resultado eq 'Conferida' and StatusLancamento eq 'Pendente'`, chama **Executar um fluxo criado com o Power Automate para desktop** (modo assistido ou não assistido) passando a fila em JSON, recebe o resultado e atualiza cada item. | Power Automate Premium (US$ 15 por usuário por mês, conferir preço atual) para o usuário do robô; não assistido exige o complemento de processo. |
| **Demonstração** | O mesmo fluxo de nuvem grava `fila-erp.json` numa pasta do OneDrive sincronizada no PC. O fluxo de desktop é iniciado à mão no console, lê a fila, lança e grava `resultado-erp.json`. Um terceiro fluxo de nuvem (OneDrive · *Quando um arquivo é criado*) lê o resultado e atualiza a lista. | Só conectores padrão. |

A fila (`fila-erp.json`) é uma lista de objetos: `id` (item da lista), `chave`, `numero`, `serie`, `emissao` (dd/MM/aaaa), `cnpj`, `pedido`, `valor` (texto com vírgula), `itens` (`codigo`, `quantidade`, `valorUnitario`, com vírgula).

### Passos do fluxo de desktop

1. **Ler arquivo de texto** `%PastaFila%\fila-erp.json` → **Converter JSON em objeto personalizado** `Fila`.
2. **Executar aplicativo** `C:\Tucci\ErpAtlas\ErpAtlas.exe`, aguardar a janela `ATLAS ERP 4.2.7 - [Acesso ao sistema]`.
3. **Login:** preencher `txtUsuario` com `FISCAL` e `txtSenha` com a credencial guardada no cofre do Power Automate (nunca no fluxo); pressionar o botão `btnEntrar`. Aguardar a janela `TelaPrincipal`.
4. Pressionar **F5** (abre `TelaEntradaNota`).
5. **Para cada** `Nota` em `Fila`:
   1. Preencher `txtChaveAcesso`, `txtNumeroNota`, `txtSerie`, `txtDataEmissao`, `txtCnpjFornecedor` (só dígitos; a máscara formata), pressionar **Tab** (o ERP busca a razão social), `txtPedido`, `txtValorTotal`.
   2. **Para cada** item: `txtItemCodigo`, `txtItemQuantidade`, `txtItemValorUnitario`, pressionar `btnIncluirItem`.
   3. Pressionar **F10**. Aguardar até 15 s por uma janela de mensagem do `ATLAS ERP 4.2.7`.
   4. **Se** o texto de `lblLancamento` começa com `LANCAMENTO`: extrair o número (`LF-aaaa-nnnnnn`), fechar a mensagem (OK), registrar `{id, status: "Lançada", lancamento}`.
   5. **Senão:** **Tirar captura de tela** da janela, salvar em `%PastaFila%\erros\<chave>.png`, ler `lblMensagem`, fechar a mensagem, pressionar `btnLimpar`, registrar `{id, status: "Erro", mensagem, captura}`.
6. Fechar o ERP (`btnFechar`, depois menu Sistema > Sair).
7. **Gravar** a lista de resultados em `%PastaFila%\resultado-erp.json`.
8. **Bloco ao ocorrer erro** em volta do passo 5: qualquer exceção de UI marca a nota como Erro com a captura, e o laço segue para a próxima.

### Seletores

O simulador dá a todo campo `Name` e `AccessibleName` iguais (ex.: `txtChaveAcesso`). No gravador do PAD, prefira o seletor por `Name` e remova o índice da janela; a janela é identificada pelo título que começa com `ATLAS ERP`.

### Teste de aceite

Com as 14 notas Conferidas na fila: 14 lançamentos `LF-...` gravados (conferir em Consultas > Lançamentos) e `NumeroLancamentoERP` preenchido na lista. Depois, em Sistema > Opções, "Falhar a cada 5": a 5ª e a 10ª gravação falham, ficam com `StatusLancamento = Erro` e a captura anexada; rodar de novo só com as que deram erro lança as duas.
