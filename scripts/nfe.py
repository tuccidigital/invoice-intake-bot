"""Gera XML de NF-e fictícia no layout 4.00, só com os grupos que os projetos leem.

Usado pelo P03 (20 notas de teste) e pelo P08 (amostras para upload no portal).
Não tem assinatura digital nem protocolo da SEFAZ. CNPJs dos emitentes são os da
Lumina seed (começam com 00 e têm dígito verificador propositalmente inválido).
"""
from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime
from decimal import Decimal, ROUND_HALF_UP
from xml.sax.saxutils import escape

CNPJ_LUMINA = "00000999000199"  # destinatária fictícia
UF_SP = "35"


@dataclass
class ItemNfe:
    codigo: str
    descricao: str
    quantidade: Decimal
    valor_unitario: Decimal

    @property
    def valor(self) -> Decimal:
        return (self.quantidade * self.valor_unitario).quantize(Decimal("0.01"), ROUND_HALF_UP)


def dv_mod11(base: str) -> int:
    soma, peso = 0, 2
    for d in reversed(base):
        soma += int(d) * peso
        peso = 2 if peso == 9 else peso + 1
    resto = soma % 11
    return 0 if resto < 2 else 11 - resto


def chave_acesso(cnpj: str, numero: int, emissao: datetime, serie: int = 1) -> str:
    """44 dígitos: UF, AAMM, CNPJ, modelo 55, série, número, tipo de emissão, código numérico, DV."""
    codigo = f"{(numero * 7919) % 100_000_000:08d}"
    base = f"{UF_SP}{emissao:%y%m}{cnpj}55{serie:03d}{numero:09d}1{codigo}"
    return base + str(dv_mod11(base))


def nfe_xml(*, cnpj_emitente: str, nome_emitente: str, numero: int, emissao: datetime,
            itens: list[ItemNfe], pedido: str | None = None, serie: int = 1) -> tuple[str, str]:
    """Devolve (chave, xml). O número do pedido vai em infAdic e em xPed de cada item."""
    chave = chave_acesso(cnpj_emitente, numero, emissao, serie)
    total = sum((i.valor for i in itens), Decimal("0.00"))
    dets = []
    for n, i in enumerate(itens, start=1):
        xped = f"<xPed>{escape(pedido)}</xPed>" if pedido else ""
        dets.append(
            f'<det nItem="{n}"><prod><cProd>{escape(i.codigo)}</cProd><xProd>{escape(i.descricao)}</xProd>'
            f"<NCM>39021020</NCM><CFOP>5101</CFOP><uCom>UN</uCom><qCom>{i.quantidade:.4f}</qCom>"
            f"<vUnCom>{i.valor_unitario:.2f}</vUnCom><vProd>{i.valor:.2f}</vProd>{xped}</prod></det>"
        )
    inf_adic = f"<infAdic><infCpl>Pedido de compra {escape(pedido)}. Documento fictício para demonstração.</infCpl></infAdic>" if pedido else \
        "<infAdic><infCpl>Documento fictício para demonstração.</infCpl></infAdic>"
    xml = (
        '<?xml version="1.0" encoding="UTF-8"?>\n'
        '<nfeProc xmlns="http://www.portalfiscal.inf.br/nfe" versao="4.00">'
        f'<NFe><infNFe Id="NFe{chave}" versao="4.00">'
        f"<ide><cUF>{UF_SP}</cUF><natOp>Venda de mercadoria</natOp><mod>55</mod><serie>{serie}</serie>"
        f"<nNF>{numero}</nNF><dhEmi>{emissao:%Y-%m-%dT%H:%M:%S}-03:00</dhEmi><tpNF>1</tpNF><tpAmb>2</tpAmb></ide>"
        f"<emit><CNPJ>{cnpj_emitente}</CNPJ><xNome>{escape(nome_emitente)}</xNome><enderEmit><UF>SP</UF></enderEmit></emit>"
        f"<dest><CNPJ>{CNPJ_LUMINA}</CNPJ><xNome>Lumina Componentes Industriais Ltda. (fictícia)</xNome></dest>"
        + "".join(dets)
        + f"<total><ICMSTot><vProd>{total:.2f}</vProd><vNF>{total:.2f}</vNF></ICMSTot></total>"
        + inf_adic
        + "</infNFe></NFe></nfeProc>\n"
    )
    return chave, xml
