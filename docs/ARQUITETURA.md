# Arquitetura · P03 Robô de Entrada de NF-e

*Dados fictícios. Lumina Componentes é uma empresa fictícia.*

## Componentes

| Peça | Onde roda | Papel |
| --- | --- | --- |
| `TCC | NF-e | Nova nota por e-mail` | Power Automate (nuvem) | Lê o XML, confere com o pedido, grava a nota, avisa divergência |
| `TCC | NF-e | Fila do ERP` | Power Automate (nuvem) | Monta a fila de notas conferidas e atualiza o resultado |
| `TCC | NF-e | Lançar no ERP` | Power Automate para desktop | Digita as notas no ERP Atlas |
| ERP Atlas | PC Windows | Simulador do ERP legado (C# WinForms) |
| Listas SharePoint | Site `Lumina · Processos` | `NotasRecebidas`, biblioteca `XML NF-e`, `LogErros`; lê `Solicitacoes` e `Fornecedores` do P01 |

## Dados

- `NotasRecebidas`: chave de acesso (Title, indexada e única), número, série, emissão, CNPJ e nome do emitente, valor, pedido vinculado (pesquisa em `Solicitacoes`), resultado, motivo, status do lançamento, número no ERP, link do XML, itens em JSON, captura de erro.
- `XML NF-e`: um arquivo por nota, em pastas por ano e mês.
- `LogErros`: duplicadas, anexos que não são NF-e e falhas dos fluxos.

## Segurança

- O fluxo só lê a pasta monitorada da caixa `fiscal@`, nunca a caixa inteira.
- XML lido pelo `xml()` do Power Automate, que não resolve entidades externas.
- A senha do ERP fica no cofre de credenciais do Power Automate, nunca no fluxo nem no repositório.
- O robô de desktop roda com um usuário próprio do ERP (perfil só de lançamento), para a auditoria do ERP mostrar o que foi feito por ele.
- Divergências não são lançadas: só nota Conferida entra na fila.

## Licenças

| Item | Licença |
| --- | --- |
| Fluxos de nuvem | Microsoft 365 (conectores padrão) |
| Disparo do desktop pela nuvem | Power Automate Premium por usuário do robô |
| Robô não assistido | Premium + complemento de processo |

Preços mudam: conferir a página oficial de preços do Power Automate antes de colocar número em proposta.

## Decisões

- **XML, não OCR.** O XML é o documento fiscal; ler o DANFE em PDF com IA seria mais caro e menos confiável.
- **Robô de desktop só onde não há API.** Se o ERP do cliente tiver API ou importação de arquivo, troque o passo 3 por ela: é mais rápido e não quebra quando a tela muda.
- **Fila em arquivo na demonstração.** Mantém a demo sem licença Premium; em produção a fila vai pelo conector de desktop.
