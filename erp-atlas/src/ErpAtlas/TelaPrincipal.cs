using ErpAtlas.Core;

namespace ErpAtlas;

internal sealed class TelaPrincipal : Form
{
    private readonly Repositorio _repositorio;
    private readonly string _usuario;

    public TelaPrincipal(Repositorio repositorio, string usuario)
    {
        _repositorio = repositorio;
        _usuario = usuario;
        Estilo.Aplicar(this, "Menu principal");
        Name = "TelaPrincipal";
        ClientSize = new Size(820, 520);

        var menu = new MenuStrip { Name = "mnuPrincipal" };
        var movimento = new ToolStripMenuItem("&Movimento") { Name = "mnuMovimento" };
        var entrada = new ToolStripMenuItem("&Entrada de Nota Fiscal", null, (_, _) => AbrirEntrada()) { Name = "mnuEntradaNota", ShortcutKeys = Keys.F5 };
        movimento.DropDownItems.Add(entrada);
        var consultas = new ToolStripMenuItem("&Consultas") { Name = "mnuConsultas" };
        consultas.DropDownItems.Add(new ToolStripMenuItem("&Lançamentos de notas", null, (_, _) => AbrirConsulta()) { Name = "mnuConsultaLancamentos", ShortcutKeys = Keys.F6 });
        var sistema = new ToolStripMenuItem("&Sistema") { Name = "mnuSistema" };
        sistema.DropDownItems.Add(new ToolStripMenuItem("&Opções do simulador", null, (_, _) => AbrirOpcoes()) { Name = "mnuOpcoes" });
        sistema.DropDownItems.Add(new ToolStripMenuItem("&Sair", null, (_, _) => Close()) { Name = "mnuSair" });
        menu.Items.AddRange([movimento, consultas, sistema]);
        MainMenuStrip = menu;

        var painel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(58, 110, 165) };
        painel.Controls.Add(new Label { Text = "ATLAS ERP", Font = new Font("Tahoma", 28F, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(40, 40) });
        painel.Controls.Add(new Label { Text = "MÓDULO FISCAL · LUMINA COMPONENTES (FICTÍCIA)", Font = Estilo.Negrito, ForeColor = Color.White, AutoSize = true, Location = new Point(44, 90) });
        var bEntrada = Estilo.Botao("F5 - Entrada de Nota Fiscal", "btnEntradaNota", 44, 140, 240);
        var bConsulta = Estilo.Botao("F6 - Consulta de lançamentos", "btnConsulta", 44, 176, 240);
        bEntrada.Click += (_, _) => AbrirEntrada();
        bConsulta.Click += (_, _) => AbrirConsulta();
        painel.Controls.Add(bEntrada);
        painel.Controls.Add(bConsulta);

        var status = new StatusStrip { Name = "barraStatus" };
        status.Items.Add(new ToolStripStatusLabel($"USUÁRIO: {usuario}"));
        status.Items.Add(new ToolStripStatusLabel($"DATA: {DateTime.Today:dd/MM/yyyy}"));
        status.Items.Add(new ToolStripStatusLabel("EMPRESA: 01") { Spring = true, TextAlign = ContentAlignment.MiddleLeft });
        status.Items.Add(new ToolStripStatusLabel("DADOS FICTÍCIOS"));

        Controls.Add(painel);
        Controls.Add(menu);
        Controls.Add(status);
    }

    private void AbrirEntrada()
    {
        using var f = new TelaEntradaNota(_repositorio, _usuario);
        f.ShowDialog(this);
    }

    private void AbrirConsulta()
    {
        using var f = new TelaConsulta(_repositorio);
        f.ShowDialog(this);
    }

    private void AbrirOpcoes()
    {
        using var f = new TelaOpcoes(_repositorio);
        f.ShowDialog(this);
    }
}
