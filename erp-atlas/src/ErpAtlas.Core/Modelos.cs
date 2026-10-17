namespace ErpAtlas.Core;

public sealed record Fornecedor(int Codigo, string Nome, string Cnpj);

public sealed class ItemLancamento
{
    public string Codigo { get; set; } = "";
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal Total => Math.Round(Quantidade * ValorUnitario, 2);
}

public sealed class Lancamento
{
    public string Numero { get; set; } = "";
    public DateTime LancadoEm { get; set; }
    public string Usuario { get; set; } = "";
    public string ChaveAcesso { get; set; } = "";
    public int NumeroNota { get; set; }
    public int Serie { get; set; } = 1;
    public DateTime Emissao { get; set; }
    public string CnpjFornecedor { get; set; } = "";
    public string Fornecedor { get; set; } = "";
    public string Pedido { get; set; } = "";
    public decimal ValorTotal { get; set; }
    public List<ItemLancamento> Itens { get; set; } = [];
}

/// <summary>Opções do simulador, para demonstrar como o robô trata falha do ERP.</summary>
public sealed class Opcoes
{
    /// <summary>A cada N gravações, a gravação falha com "Erro 4021: tabela de lançamentos bloqueada". 0 = nunca.</summary>
    public int FalharACada { get; set; }
    /// <summary>Atraso artificial ao gravar, em milissegundos (sistemas antigos são lentos; o robô tem de esperar).</summary>
    public int AtrasoGravacaoMs { get; set; } = 800;
}
