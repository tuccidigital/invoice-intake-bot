using System.Text.Json;

namespace ErpAtlas.Core;

/// <summary>Grava os lançamentos num arquivo JSON local (o "banco" do simulador).</summary>
public sealed class Repositorio
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private readonly string _arquivo;
    private readonly string _arquivoOpcoes;
    private int _gravacoes;

    public Repositorio(string? pasta = null)
    {
        pasta ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ErpAtlas");
        Directory.CreateDirectory(pasta);
        _arquivo = Path.Combine(pasta, "lancamentos.json");
        _arquivoOpcoes = Path.Combine(pasta, "opcoes.json");
        Opcoes = File.Exists(_arquivoOpcoes) ? JsonSerializer.Deserialize<Opcoes>(File.ReadAllText(_arquivoOpcoes)) ?? new() : new();
    }

    public Opcoes Opcoes { get; }

    public void SalvarOpcoes() => File.WriteAllText(_arquivoOpcoes, JsonSerializer.Serialize(Opcoes, Json));

    public List<Lancamento> Todos() =>
        File.Exists(_arquivo) ? JsonSerializer.Deserialize<List<Lancamento>>(File.ReadAllText(_arquivo)) ?? [] : [];

    /// <summary>Valida, numera e grava. Devolve o número do lançamento ou lança com os erros.</summary>
    public string Gravar(Lancamento l, string usuario)
    {
        var todos = Todos();
        var erros = Validacao.Validar(l, todos);
        if (erros.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, erros));

        _gravacoes++;
        if (Opcoes.FalharACada > 0 && _gravacoes % Opcoes.FalharACada == 0)
            throw new FalhaSimuladaException("ERRO 4021 - TABELA DE LANCAMENTOS BLOQUEADA POR OUTRO USUARIO. TENTE NOVAMENTE.");

        l.Numero = $"LF-{DateTime.Now:yyyy}-{todos.Count + 1:000000}";
        l.LancadoEm = DateTime.Now;
        l.Usuario = usuario;
        l.Fornecedor = CadastroFornecedores.PorCnpj(l.CnpjFornecedor)!.Nome;
        todos.Add(l);
        File.WriteAllText(_arquivo, JsonSerializer.Serialize(todos, Json));
        return l.Numero;
    }

    public void Limpar()
    {
        if (File.Exists(_arquivo)) File.Delete(_arquivo);
    }
}
