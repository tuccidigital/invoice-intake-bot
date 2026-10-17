using System.Globalization;
using ErpAtlas.Core;

namespace ErpAtlas;

/// <summary>
/// "ENTRADA DE NOTA FISCAL": a tela que o Otávio digitava o dia todo e o robô passa a preencher.
/// Itens entram um a um pelos campos de item e o botão "Incluir item" (como em muitos ERPs antigos).
/// Depois de gravar, o número do lançamento aparece em lblLancamento (o robô lê dali).
/// </summary>
internal sealed class TelaEntradaNota : Form
{
    private static readonly CultureInfo Br = Validacao.Br;
    private readonly Repositorio _repositorio;
    private readonly string _usuario;

    private readonly TextBox _chave = new() { MaxLength = 44 };
    private readonly TextBox _numero = new() { MaxLength = 9 };
    private readonly TextBox _serie = new() { MaxLength = 3, Text = "1" };
    private readonly MaskedTextBox _emissao = new() { Mask = "00/00/0000", ValidatingType = typeof(DateTime) };
    private readonly MaskedTextBox _cnpj = new() { Mask = "00.000.000/0000-00" };
    private readonly Label _razao = new() { BorderStyle = BorderStyle.Fixed3D, BackColor = SystemColors.Info };
    private readonly TextBox _pedido = new() { CharacterCasing = CharacterCasing.Upper, MaxLength = 20 };
    private readonly TextBox _valorTotal = new() { TextAlign = HorizontalAlignment.Right };
    private readonly TextBox _itemCodigo = new() { CharacterCasing = CharacterCasing.Upper };
    private readonly TextBox _itemQtd = new() { TextAlign = HorizontalAlignment.Right };
    private readonly TextBox _itemValor = new() { TextAlign = HorizontalAlignment.Right };
    private readonly DataGridView _grade = new();
    private readonly Label _somaItens = new() { TextAlign = ContentAlignment.MiddleRight };
    private readonly Label _mensagem = new() { ForeColor = Color.DarkRed };
    private readonly Label _lancamento = new() { Font = Estilo.Negrito, ForeColor = Color.DarkGreen };
    private readonly List<ItemLancamento> _itens = [];

    public TelaEntradaNota(Repositorio repositorio, string usuario)
    {
        _repositorio = repositorio;
        _usuario = usuario;
        Estilo.Aplicar(this, "Entrada de Nota Fiscal");
        Name = "TelaEntradaNota";
        ClientSize = new Size(760, 600);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        Controls.Add(Estilo.FaixaDeTitulo("FIS0310 - Entrada de Nota Fiscal de Compra", 760));
        int x1 = 20, x2 = 170, y = 46;
        void Linha(string rotulo, Control c, string nome, int largura)
        {
            Controls.Add(Estilo.Rotulo(rotulo, x1, y));
            Controls.Add(Estilo.Campo(c, nome, x2, y, largura));
            y += 30;
        }
        Linha("Chave de acesso", _chave, "txtChaveAcesso", 400);
        Linha("Número da nota", _numero, "txtNumeroNota", 120);
        Linha("Série", _serie, "txtSerie", 60);
        Linha("Data de emissão", _emissao, "txtDataEmissao", 120);
        Linha("CNPJ do fornecedor", _cnpj, "txtCnpjFornecedor", 160);
        Controls.Add(Estilo.Campo(_razao, "lblRazaoSocial", 340, y - 30, 390));
        _razao.Height = 22;
        Linha("Pedido de compra", _pedido, "txtPedido", 160);
        Linha("Valor total da nota", _valorTotal, "txtValorTotal", 160);

        // Itens
        y += 6;
        Controls.Add(new Label { Text = "ITENS DA NOTA", Font = Estilo.Negrito, Location = new Point(x1, y), AutoSize = true });
        y += 24;
        Controls.Add(Estilo.Rotulo("Código", x1, y, 60));
        Controls.Add(Estilo.Campo(_itemCodigo, "txtItemCodigo", 80, y, 100));
        Controls.Add(Estilo.Rotulo("Qtde", 195, y, 40));
        Controls.Add(Estilo.Campo(_itemQtd, "txtItemQuantidade", 240, y, 90));
        Controls.Add(Estilo.Rotulo("Vlr. unit.", 345, y, 70));
        Controls.Add(Estilo.Campo(_itemValor, "txtItemValorUnitario", 420, y, 110));
        var incluir = Estilo.Botao("&Incluir item", "btnIncluirItem", 545, y - 2, 100);
        incluir.Click += (_, _) => IncluirItem();
        Controls.Add(incluir);
        var remover = Estilo.Botao("&Remover", "btnRemoverItem", 650, y - 2, 90);
        remover.Click += (_, _) => RemoverItem();
        Controls.Add(remover);
        y += 34;

        _grade.Name = "grdItens";
        _grade.AccessibleName = "grdItens";
        _grade.Location = new Point(x1, y);
        _grade.Size = new Size(720, 150);
        _grade.ReadOnly = true;
        _grade.AllowUserToAddRows = false;
        _grade.RowHeadersVisible = false;
        _grade.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grade.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grade.BackgroundColor = SystemColors.Window;
        _grade.Columns.Add("Codigo", "CÓDIGO");
        _grade.Columns.Add("Quantidade", "QTDE");
        _grade.Columns.Add("ValorUnitario", "VLR. UNIT.");
        _grade.Columns.Add("Total", "TOTAL");
        foreach (DataGridViewColumn c in _grade.Columns) if (c.Index > 0) c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        Controls.Add(_grade);
        y += 154;
        Controls.Add(Estilo.Campo(_somaItens, "lblSomaItens", 440, y, 300));
        y += 28;

        Controls.Add(Estilo.Campo(_mensagem, "lblMensagem", x1, y, 720));
        _mensagem.Height = 36;
        y += 38;
        Controls.Add(Estilo.Campo(_lancamento, "lblLancamento", x1, y, 400));

        var gravar = Estilo.Botao("F10 - &Gravar", "btnGravar", 410, y - 4, 110);
        var limpar = Estilo.Botao("&Limpar", "btnLimpar", 525, y - 4, 100);
        var fechar = Estilo.Botao("&Fechar", "btnFechar", 630, y - 4, 110);
        gravar.Click += async (_, _) => await GravarAsync(gravar);
        limpar.Click += (_, _) => Limpar();
        fechar.Click += (_, _) => Close();
        Controls.AddRange([gravar, limpar, fechar]);
        KeyDown += async (_, e) => { if (e.KeyCode == Keys.F10) { e.Handled = true; await GravarAsync(gravar); } };
        CancelButton = fechar;

        _cnpj.Leave += (_, _) => BuscarFornecedor();
        AtualizarSoma();
    }

    private void BuscarFornecedor()
    {
        var f = CadastroFornecedores.PorCnpj(_cnpj.Text);
        _razao.Text = f is null ? (Cnpj.Digitos(_cnpj.Text).Length == 14 ? "FORNECEDOR NAO CADASTRADO" : "") : $"{f.Codigo:0000} - {f.Nome.ToUpperInvariant()}";
    }

    private static bool LerDecimal(string s, out decimal v) =>
        decimal.TryParse(s.Trim(), NumberStyles.Number, Br, out v);

    private void IncluirItem()
    {
        if (string.IsNullOrWhiteSpace(_itemCodigo.Text) || !LerDecimal(_itemQtd.Text, out var q) || !LerDecimal(_itemValor.Text, out var v))
        {
            _mensagem.Text = "E201 - PREENCHA CODIGO, QUANTIDADE E VALOR UNITARIO DO ITEM (USE VIRGULA PARA DECIMAIS)";
            return;
        }
        var item = new ItemLancamento { Codigo = _itemCodigo.Text.Trim(), Quantidade = q, ValorUnitario = v };
        _itens.Add(item);
        _grade.Rows.Add(item.Codigo, item.Quantidade.ToString("N4", Br), item.ValorUnitario.ToString("N2", Br), item.Total.ToString("N2", Br));
        _itemCodigo.Clear();
        _itemQtd.Clear();
        _itemValor.Clear();
        _mensagem.Text = "";
        _itemCodigo.Focus();
        AtualizarSoma();
    }

    private void RemoverItem()
    {
        if (_grade.CurrentRow is not { } linha) return;
        _itens.RemoveAt(linha.Index);
        _grade.Rows.RemoveAt(linha.Index);
        AtualizarSoma();
    }

    private void AtualizarSoma() => _somaItens.Text = $"SOMA DOS ITENS: {_itens.Sum(i => i.Total).ToString("N2", Br)}";

    private async Task GravarAsync(Button botao)
    {
        _mensagem.Text = "";
        _lancamento.Text = "";
        if (!DateTime.TryParseExact(_emissao.Text, "dd/MM/yyyy", Br, DateTimeStyles.None, out var emissao)) emissao = default;
        LerDecimal(_valorTotal.Text, out var total);
        int.TryParse(_numero.Text, out var numero);
        int.TryParse(_serie.Text, out var serie);
        var l = new Lancamento
        {
            ChaveAcesso = _chave.Text.Trim(), NumeroNota = numero, Serie = serie, Emissao = emissao,
            CnpjFornecedor = Cnpj.Digitos(_cnpj.Text), Pedido = _pedido.Text.Trim(), ValorTotal = total, Itens = [.. _itens],
        };

        botao.Enabled = false;
        UseWaitCursor = true;
        _lancamento.Text = "GRAVANDO...";
        try
        {
            await Task.Delay(_repositorio.Opcoes.AtrasoGravacaoMs);
            var n = _repositorio.Gravar(l, _usuario);
            _lancamento.Text = $"LANCAMENTO {n} GRAVADO";
            MessageBox.Show(this, $"LANCAMENTO {n} GRAVADO COM SUCESSO.", Estilo.Versao, MessageBoxButtons.OK, MessageBoxIcon.Information);
            Limpar(manterResultado: true);
        }
        catch (Exception e) when (e is InvalidOperationException or FalhaSimuladaException)
        {
            _lancamento.Text = "";
            _mensagem.Text = e.Message.Replace(Environment.NewLine, " | ");
            MessageBox.Show(this, e.Message, Estilo.Versao, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            botao.Enabled = true;
            UseWaitCursor = false;
        }
    }

    private void Limpar(bool manterResultado = false)
    {
        foreach (var t in new TextBoxBase[] { _chave, _numero, _emissao, _cnpj, _pedido, _valorTotal, _itemCodigo, _itemQtd, _itemValor }) t.Clear();
        _serie.Text = "1";
        _razao.Text = "";
        _itens.Clear();
        _grade.Rows.Clear();
        AtualizarSoma();
        if (!manterResultado) { _mensagem.Text = ""; _lancamento.Text = ""; }
        _chave.Focus();
    }
}
