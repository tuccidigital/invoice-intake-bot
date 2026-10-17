using ErpAtlas.Core;

namespace ErpAtlas;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var repositorio = new Repositorio();
        // Linha de comando para o robô e para a demonstração: --falhar-a-cada 5 (0 desliga).
        var i = Array.IndexOf(args, "--falhar-a-cada");
        if (i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out var n)) repositorio.Opcoes.FalharACada = Math.Max(0, n);

        using var login = new TelaLogin();
        if (login.ShowDialog() != DialogResult.OK) return;
        Application.Run(new TelaPrincipal(repositorio, login.Usuario));
    }
}
