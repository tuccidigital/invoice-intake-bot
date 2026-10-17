---
titulo: Robô de Entrada de NF-e
resumo: As notas chegam por e-mail, são conferidas com o pedido e lançadas no ERP antigo por um robô, sem ninguém digitar. Power Automate na nuvem e no desktop.
setor: Indústria (fiscal e compras)
stack: Power Automate, Power Automate para desktop, SharePoint, Teams, C# (simulador do ERP)
ficticio: true
video: /cases/robo-nfe.mp4
---

## Problema

Otávio Lima, do Fiscal da Lumina, recebia cerca de 600 notas por mês por e-mail. Abria cada XML, conferia com o pedido de compra e digitava no ERP Atlas, um sistema desktop antigo sem API. Divergência de valor só aparecia no fechamento do mês.

## O que construímos

1. **Leitura do XML** assim que o e-mail chega, sem OCR: chave, número, fornecedor, valor e itens.
2. **Conferência com o pedido aprovado:** mesmo fornecedor, pedido aprovado e valor dentro de 2%.
3. **Aviso no Teams** para Compras quando a nota diverge, com o valor da nota, o do pedido e a diferença.
4. **Robô no ERP antigo:** abre o sistema, digita a nota e devolve o número do lançamento para a lista.

## Como funciona

- **Nota repetida não entra duas vezes.** O fluxo checa a chave de acesso antes de gravar.
- **Erro do ERP não para a fila.** O robô tira uma captura da tela, marca a nota como erro e segue para a próxima.
- **Tudo rastreável.** Cada nota tem resultado, motivo, link do XML e número do lançamento numa lista do SharePoint.

## Resultado esperado

Com os 20 e-mails de teste da Lumina: 14 notas conferidas e lançadas pelo robô, 3 divergências avisadas no Teams com o motivo, 2 notas sem pedido separadas e 1 duplicada ignorada. Nenhuma digitação.

## Licenças necessárias

Os fluxos de nuvem usam só conectores do Microsoft 365. Disparar o robô de desktop a partir da nuvem exige Power Automate Premium para o usuário do robô.

## Quanto tempo levou

Fluxos, simulador e testes: cerca de 2 semanas. No cliente, o tempo depende do ERP real: mapear as telas e testar o robô leva de 1 a 2 semanas.

*Empresa fictícia, dados fictícios.*
