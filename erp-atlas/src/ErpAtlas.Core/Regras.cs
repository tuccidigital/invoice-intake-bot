using System.Globalization;
using System.Reflection;

namespace ErpAtlas.Core;

public static class Cnpj
{
    public static string Digitos(string s) => new(s.Where(char.IsDigit).ToArray());

    public static string Formatar(string s)
    {
        var d = Digitos(s);
        return d.Length == 14 ? $"{d[..2]}.{d[2..5]}.{d[5..8]}/{d[8..12]}-{d[12..]}" : s;
    }
}

/// <summary>Cadastro de fornecedores do "ERP" (vem da lumina-seed, embutido no executável).</summary>
public static class CadastroFornecedores
{
    private static readonly Lazy<IReadOnlyList<Fornecedor>> Lista = new(Carregar);
    public static IReadOnlyList<Fornecedor> Todos => Lista.Value;

    public static Fornecedor? PorCnpj(string cnpj)
    {
        var d = Cnpj.Digitos(cnpj);
        return Todos.FirstOrDefault(f => f.Cnpj == d);
    }

    private static IReadOnlyList<Fornecedor> Carregar()
    {
        using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("fornecedores.csv")
            ?? throw new InvalidOperationException("Cadastro de fornecedores não embutido.");
        using var r = new StreamReader(s);
        return r.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1)
            .Select(l => l.TrimEnd('\r').Split(';'))
            .Select(c => new Fornecedor(int.Parse(c[0], CultureInfo.InvariantCulture), c[1], Cnpj.Digitos(c[2])))
            .ToList();
    }
}

/// <summary>Validações da tela "Entrada de Nota Fiscal". Mensagens no estilo do sistema antigo.</summary>
public static class Validacao
{
    public static IReadOnlyList<string> Validar(Lancamento l, IEnumerable<Lancamento> existentes)
    {
        var erros = new List<string>();
        if (l.ChaveAcesso.Length != 44 || !l.ChaveAcesso.All(char.IsDigit)) erros.Add("E101 - CHAVE DE ACESSO DEVE TER 44 DIGITOS");
        if (l.NumeroNota <= 0) erros.Add("E102 - NUMERO DA NOTA INVALIDO");
        if (l.Emissao == default || l.Emissao.Date > DateTime.Today.AddDays(1)) erros.Add("E103 - DATA DE EMISSAO INVALIDA");
        if (CadastroFornecedores.PorCnpj(l.CnpjFornecedor) is null) erros.Add("E104 - FORNECEDOR NAO CADASTRADO");
        if (l.Itens.Count == 0) erros.Add("E105 - INFORME AO MENOS UM ITEM");
        if (l.Itens.Any(i => i.Quantidade <= 0 || i.ValorUnitario <= 0)) erros.Add("E106 - ITEM COM QUANTIDADE OU VALOR ZERADO");
        var soma = l.Itens.Sum(i => i.Total);
        if (l.Itens.Count > 0 && Math.Abs(soma - l.ValorTotal) > 0.05m)
            erros.Add($"E107 - SOMA DOS ITENS ({soma.ToString("N2", Br)}) DIFERENTE DO TOTAL ({l.ValorTotal.ToString("N2", Br)})");
        if (existentes.Any(e => e.ChaveAcesso == l.ChaveAcesso)) erros.Add("E108 - NOTA JA LANCADA");
        return erros;
    }

    public static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");
}

public sealed class FalhaSimuladaException(string mensagem) : Exception(mensagem);
