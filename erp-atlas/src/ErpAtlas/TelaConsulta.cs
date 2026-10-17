using ErpAtlas.Core;

namespace ErpAtlas;

internal sealed class TelaConsulta : Form
{
    public TelaConsulta(Repositorio repositorio)
    {
        Estilo.Aplicar(this, "Consulta de lançamentos");
        Name = "TelaConsulta";
        ClientSize = new Size(900, 480);
        Controls.Add(Estilo.FaixaDeTitulo("FIS0420 - Consulta de lançamentos de notas", 900));

        var grade = new DataGridView
        {
            Name = "grdLancamentos", AccessibleName = "grdLancamentos", Location = new Point(12, 44), Size = new Size(876, 380),
            ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = SystemColors.Window, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        };
        void Carregar()
        {
            grade.DataSource = repositorio.Todos()
                .OrderByDescending(l => l.LancadoEm)
                .Select(l => new
                {
                    Lancamento = l.Numero, Data = l.LancadoEm.ToString("dd/MM/yyyy HH:mm"), Nota = l.NumeroNota, l.Fornecedor,
                    CNPJ = Cnpj.Formatar(l.CnpjFornecedor), l.Pedido, Valor = l.ValorTotal.ToString("N2", Validacao.Br), Usuario = l.Usuario,
                }).ToList();
        }
        Carregar();
        Controls.Add(grade);

        var atualizar = Estilo.Botao("&Atualizar", "btnAtualizar", 12, 436);
        atualizar.Click += (_, _) => Carregar();
        var limpar = Estilo.Botao("Limpar &base (demo)", "btnLimparBase", 130, 436, 150);
        limpar.Click += (_, _) =>
        {
            if (MessageBox.Show(this, "APAGAR TODOS OS LANCAMENTOS DO SIMULADOR?", Estilo.Versao, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            repositorio.Limpar();
            Carregar();
        };
        var fechar = Estilo.Botao("&Fechar", "btnFechar", 778, 436);
        fechar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        fechar.Click += (_, _) => Close();
        atualizar.Anchor = limpar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        Controls.AddRange([atualizar, limpar, fechar]);
        CancelButton = fechar;
    }
}
