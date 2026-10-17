using ErpAtlas.Core;

namespace ErpAtlas.Tests;

public class RegrasTests
{
    private static Lancamento Valido(string chave = "35261000000101000199550010000012341000012345") => new()
    {
        ChaveAcesso = chave, NumeroNota = 1234, Serie = 1, Emissao = DateTime.Today, CnpjFornecedor = "00.000.101/0001-99",
        Pedido = "COM-2026-000901", ValorTotal = 575.00m,
        Itens = [new ItemLancamento { Codigo = "MP-001", Quantidade = 2, ValorUnitario = 287.50m }],
    };

    private static Repositorio NovoRepositorio() => new(Path.Combine(Path.GetTempPath(), "atlas-teste-" + Guid.NewGuid().ToString("N")));

    [Fact]
    public void Cadastro_embutido_tem_os_15_fornecedores_da_seed() =>
        Assert.Equal(15, CadastroFornecedores.Todos.Count);

    [Fact]
    public void Acha_fornecedor_pelo_CNPJ_com_ou_sem_mascara()
    {
        Assert.Equal("Polimax Resinas", CadastroFornecedores.PorCnpj("00.000.101/0001-99")?.Nome);
        Assert.Equal("Polimax Resinas", CadastroFornecedores.PorCnpj("00000101000199")?.Nome);
        Assert.Null(CadastroFornecedores.PorCnpj("11.111.111/0001-11"));
    }

    [Fact]
    public void Lancamento_valido_passa() => Assert.Empty(Validacao.Validar(Valido(), []));

    [Fact]
    public void Soma_dos_itens_diferente_do_total_e_recusada()
    {
        var l = Valido();
        l.ValorTotal = 600m;
        Assert.Contains(Validacao.Validar(l, []), e => e.StartsWith("E107"));
    }

    [Fact]
    public void Chave_curta_e_fornecedor_desconhecido_sao_recusados()
    {
        var l = Valido("123");
        l.CnpjFornecedor = "11111111000111";
        var erros = Validacao.Validar(l, []);
        Assert.Contains(erros, e => e.StartsWith("E101"));
        Assert.Contains(erros, e => e.StartsWith("E104"));
    }

    [Fact]
    public void Grava_numera_e_recusa_a_mesma_nota_de_novo()
    {
        var r = NovoRepositorio();
        Assert.Equal($"LF-{DateTime.Now:yyyy}-000001", r.Gravar(Valido(), "FISCAL"));
        var erro = Assert.Throws<InvalidOperationException>(() => r.Gravar(Valido(), "FISCAL"));
        Assert.Contains("E108", erro.Message);
    }

    [Fact]
    public void Falha_simulada_acontece_na_gravacao_configurada()
    {
        var r = NovoRepositorio();
        r.Opcoes.FalharACada = 2;
        r.Gravar(Valido("35261000000101000199550010000012341000012345"), "FISCAL");
        Assert.Throws<FalhaSimuladaException>(() => r.Gravar(Valido("35261000000101000199550010000012351000012346"), "FISCAL"));
        Assert.Single(r.Todos());
    }
}
