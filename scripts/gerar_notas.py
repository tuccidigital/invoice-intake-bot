"""Gera os dados de teste do P03 a partir da lumina-seed.

Saída em dados/:
  pedidos-aprovados.csv  18 compras aprovadas (formato do P01, carregável pelo infra/carregar-pedidos.ps1)
  notas/*.xml            20 NF-e fictícias (layout 4.00, só os grupos lidos pelo fluxo)
  gabarito.csv           resultado esperado de cada nota: 14 Conferida, 3 Divergente, 2 Sem pedido, 1 Duplicada

Uso: python3 scripts/gerar_notas.py   (determinístico: mesma semente, mesmos arquivos)
"""
from __future__ import annotations

import csv
import random
import sys
from datetime import datetime, timedelta
from decimal import Decimal, ROUND_HALF_UP
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[1]
SEED = RAIZ / "dados" / "seed"  # cópia da lumina-seed (00-padroes) para o repositório ser autossuficiente
sys.path.insert(0, str(Path(__file__).resolve().parent))
from nfe import ItemNfe, nfe_xml  # noqa: E402

DADOS = RAIZ / "dados"
CENT = Decimal("0.01")
rnd = random.Random(20261016)


def ler(nome: str) -> list[dict]:
    with open(SEED / nome, encoding="utf-8") as f:
        return list(csv.DictReader(f, delimiter=";"))


fornecedores = {f["Id"]: f for f in ler("fornecedores.csv")}
produtos = ler("produtos.csv")
def brl(v: Decimal) -> str:
    return "R$ " + f"{v:,.2f}".replace(",", "X").replace(".", ",").replace("X", ".")


so_digitos = lambda s: "".join(c for c in s if c.isdigit())  # noqa: E731

# ---------------------------------------------------------------- pedidos aprovados
pedidos = []
base = datetime(2026, 10, 1, 9, 0)
escolhidos = rnd.sample(produtos, 18)
for n, prod in enumerate(escolhidos, start=1):
    forn = fornecedores[prod["FornecedorPadraoId"]]
    itens = [prod] + ([rnd.choice([p for p in produtos if p["FornecedorPadraoId"] == prod["FornecedorPadraoId"] and p is not prod] or [prod])] if rnd.random() < 0.35 else [])
    linhas = []
    for p in {p["Codigo"]: p for p in itens}.values():
        qtd = Decimal(rnd.choice([2, 4, 5, 10, 12, 20, 25, 40, 60, 100]))
        preco = (Decimal(p["PrecoReferencia"]) * Decimal(str(round(rnd.uniform(0.94, 1.06), 4)))).quantize(CENT, ROUND_HALF_UP)
        linhas.append({"codigo": p["Codigo"], "descricao": p["Descricao"], "qtd": qtd, "preco": preco})
    total = sum((l["qtd"] * l["preco"]).quantize(CENT) for l in linhas)
    pedidos.append({
        "numero": f"COM-2026-{900 + n:06d}", "fornecedor": forn, "data": base + timedelta(days=rnd.randint(0, 12), hours=rnd.randint(0, 7)),
        "itens": linhas, "total": total,
    })

with open(DADOS / "pedidos-aprovados.csv", "w", encoding="utf-8", newline="") as f:
    w = csv.writer(f, delimiter=";")
    w.writerow(["Numero", "FornecedorId", "Fornecedor", "CNPJ", "DataAprovacao", "Codigo", "Descricao", "Quantidade", "PrecoUnitario", "ValorTotalPedido"])
    for p in pedidos:
        for l in p["itens"]:
            w.writerow([p["numero"], p["fornecedor"]["Id"], p["fornecedor"]["Nome"], p["fornecedor"]["CNPJ"], p["data"].strftime("%Y-%m-%d"),
                        l["codigo"], l["descricao"], l["qtd"], l["preco"], p["total"]])

# ---------------------------------------------------------------- notas
(DADOS / "notas").mkdir(exist_ok=True)
for antigo in (DADOS / "notas").glob("*.xml"):
    antigo.unlink()

gabarito = []
numero_nf = {}


def emitir(pedido_ref: str | None, forn: dict, linhas: list[dict], fator: Decimal, esperado: str, motivo: str, dias: int = 2, numero: int | None = None):
    cnpj = so_digitos(forn["CNPJ"])
    if numero is None:
        numero_nf[cnpj] = numero_nf.get(cnpj, 1000 + int(forn["Id"]) * 137) + 1
        numero = numero_nf[cnpj]
    itens = [ItemNfe(l["codigo"], l["descricao"], l["qtd"], (l["preco"] * fator).quantize(CENT, ROUND_HALF_UP)) for l in linhas]
    emissao = datetime(2026, 10, 14, 8, 0) + timedelta(days=dias, minutes=rnd.randint(0, 600))
    chave, xml = nfe_xml(cnpj_emitente=cnpj, nome_emitente=forn["Nome"], numero=numero, emissao=emissao, itens=itens, pedido=pedido_ref)
    seq = len(gabarito) + 1
    arquivo = f"{seq:02d}-NFe{chave}.xml"
    (DADOS / "notas" / arquivo).write_text(xml, encoding="utf-8")
    total = sum((i.valor for i in itens), Decimal("0.00"))
    gabarito.append({"Seq": seq, "Arquivo": arquivo, "Chave": chave, "Numero": numero, "Fornecedor": forn["Nome"], "CNPJ": forn["CNPJ"],
                     "Pedido": pedido_ref or "", "ValorNota": total, "ResultadoEsperado": esperado, "Motivo": motivo})
    return chave, xml


# 14 que batem: 10 exatas e 4 dentro da tolerância de 2% (frete ou arredondamento).
fatores_ok = [Decimal("1")] * 10 + [Decimal("1.004"), Decimal("0.991"), Decimal("1.015"), Decimal("0.983")]
rnd.shuffle(fatores_ok)
for p, fator in zip(pedidos[:14], fatores_ok):
    emitir(p["numero"], p["fornecedor"], p["itens"], fator, "Conferida", "")

# 3 com valor divergente (acima de 2%).
for p, fator in zip(pedidos[14:17], [Decimal("1.065"), Decimal("0.955"), Decimal("1.12")]):
    tot = sum(((l["preco"] * fator).quantize(CENT, ROUND_HALF_UP) * l["qtd"]).quantize(CENT) for l in p["itens"])
    dif = (tot - p["total"]) / p["total"] * 100
    motivo = f"Valor da nota {brl(tot)}, pedido {brl(p['total'])}, diferença {dif:+.1f}%"
    motivo = motivo.replace(f"{dif:+.1f}%", f"{dif:+.1f}%".replace(".", ","))
    emitir(p["numero"], p["fornecedor"], p["itens"], fator, "Divergente", motivo)

# 2 sem pedido: número que não existe e nota sem número de pedido.
f_lub = fornecedores["15"]
emitir("COM-2026-000999", f_lub, [{"codigo": "PC-005", "descricao": "Óleo hidráulico ISO 68 (balde 20 L)", "qtd": Decimal(6), "preco": Decimal("498.00")}],
       Decimal(1), "Sem pedido", "Pedido COM-2026-000999 não encontrado")
f_clean = fornecedores["14"]
emitir(None, f_clean, [{"codigo": "SV-004", "descricao": "Limpeza industrial mensal", "qtd": Decimal(1), "preco": Decimal("4200.00")}],
       Decimal(1), "Sem pedido", "Nota sem número de pedido (xPed)")

# 1 duplicada: a primeira nota reenviada pelo fornecedor (mesma chave).
primeira = gabarito[0]
xml_dup = (DADOS / "notas" / primeira["Arquivo"]).read_text(encoding="utf-8")
seq = len(gabarito) + 1
arquivo = f"{seq:02d}-NFe{primeira['Chave']}-reenvio.xml"
(DADOS / "notas" / arquivo).write_text(xml_dup, encoding="utf-8")
gabarito.append({**primeira, "Seq": seq, "Arquivo": arquivo, "ResultadoEsperado": "Duplicada", "Motivo": f"Mesma chave da nota {primeira['Seq']:02d}"})

with open(DADOS / "gabarito.csv", "w", encoding="utf-8", newline="") as f:
    w = csv.DictWriter(f, fieldnames=list(gabarito[0].keys()), delimiter=";")
    w.writeheader()
    w.writerows(gabarito)

contagem = {}
for g in gabarito:
    contagem[g["ResultadoEsperado"]] = contagem.get(g["ResultadoEsperado"], 0) + 1
print(len(pedidos), "pedidos;", len(gabarito), "notas;", contagem)
