"""Implementação de referência da conferência do fluxo de nuvem (mesmas regras do FLOWS.md).

Lê dados/notas/*.xml na ordem de chegada, confere com dados/pedidos-aprovados.csv e compara
com dados/gabarito.csv. Serve de especificação executável: se o fluxo discordar daqui, o
fluxo está errado. Uso: python3 scripts/conferir.py
"""
import csv
import sys
import xml.etree.ElementTree as ET
from decimal import Decimal
from pathlib import Path

DADOS = Path(__file__).resolve().parents[1] / "dados"
TOLERANCIA = Decimal("0.02")


def texto(raiz, nome):
    for e in raiz.iter():
        if e.tag.split("}")[-1] == nome:
            return (e.text or "").strip()
    return ""


def ler_nota(caminho: Path) -> dict:
    raiz = ET.parse(caminho).getroot()  # arquivos gerados por nós; o fluxo usa xpath com local-name()
    inf = next(e for e in raiz.iter() if e.tag.split("}")[-1] == "infNFe")
    return {"chave": inf.get("Id", "")[3:], "cnpj": texto(raiz, "CNPJ"), "valor": Decimal(texto(inf.find(".//{*}ICMSTot"), "vNF")),
            "pedido": texto(raiz, "xPed")}


pedidos = {}
with open(DADOS / "pedidos-aprovados.csv", encoding="utf-8") as f:
    for l in csv.DictReader(f, delimiter=";"):
        pedidos[l["Numero"]] = {"cnpj": "".join(c for c in l["CNPJ"] if c.isdigit()), "total": Decimal(l["ValorTotalPedido"])}


def conferir(n: dict, vistas: set) -> tuple[str, str]:
    if n["chave"] in vistas:
        return "Duplicada", ""
    vistas.add(n["chave"])
    if not n["pedido"]:
        return "Sem pedido", "Nota sem número de pedido (xPed)"
    p = pedidos.get(n["pedido"])
    if p is None:
        return "Sem pedido", f"Pedido {n['pedido']} não encontrado"
    if p["cnpj"] != n["cnpj"]:
        return "Divergente", "Fornecedor da nota diferente do fornecedor do pedido"
    dif = (n["valor"] - p["total"]) / p["total"]
    if abs(dif) > TOLERANCIA:
        return "Divergente", f"diferença {dif * 100:+.1f}%"
    return "Conferida", ""


gabarito = {g["Arquivo"]: g for g in csv.DictReader(open(DADOS / "gabarito.csv", encoding="utf-8"), delimiter=";")}
vistas, erros, contagem = set(), 0, {}
for arq in sorted((DADOS / "notas").glob("*.xml")):
    resultado, motivo = conferir(ler_nota(arq), vistas)
    contagem[resultado] = contagem.get(resultado, 0) + 1
    esperado = gabarito[arq.name]["ResultadoEsperado"]
    if resultado != esperado:
        erros += 1
        print(f"ERRO {arq.name}: {resultado} ({motivo}), esperado {esperado}")
print(contagem)
sys.exit(1 if erros else 0)
