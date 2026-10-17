namespace ErpAtlas;

/// <summary>Login fixo do simulador: FISCAL / atlas2026 (fictício, documentado no README).</summary>
internal sealed class TelaLogin : Form
{
    private readonly TextBox _usuario = new() { CharacterCasing = CharacterCasing.Upper };
    private readonly TextBox _senha = new() { UseSystemPasswordChar = true };
    private readonly ComboBox _empresa = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    public string Usuario { get; private set; } = "";

    public TelaLogin()
    {
        Estilo.Aplicar(this, "Acesso ao sistema");
        ClientSize = new Size(380, 230);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "TelaLogin";

        Controls.Add(Estilo.FaixaDeTitulo("Acesso ao sistema", 380));
        Controls.Add(Estilo.Rotulo("Usuário", 20, 50, 90));
        Controls.Add(Estilo.Campo(_usuario, "txtUsuario", 120, 50, 230));
        Controls.Add(Estilo.Rotulo("Senha", 20, 82, 90));
        Controls.Add(Estilo.Campo(_senha, "txtSenha", 120, 82, 230));
        Controls.Add(Estilo.Rotulo("Empresa", 20, 114, 90));
        _empresa.Items.Add("01 - LUMINA COMPONENTES (FICTICIA)");
        _empresa.SelectedIndex = 0;
        Controls.Add(Estilo.Campo(_empresa, "cboEmpresa", 120, 114, 230));

        var entrar = Estilo.Botao("&Entrar", "btnEntrar", 120, 160);
        var sair = Estilo.Botao("&Sair", "btnSair", 240, 160);
        entrar.Click += (_, _) => Entrar();
        sair.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(entrar);
        Controls.Add(sair);
        AcceptButton = entrar;
        CancelButton = sair;
        Controls.Add(new Label { Text = "Dados fictícios · simulador de ERP legado (Tucci Digital)", Location = new Point(20, 200), AutoSize = true, ForeColor = SystemColors.GrayText });
    }

    private void Entrar()
    {
        if (_usuario.Text == "FISCAL" && _senha.Text == "atlas2026")
        {
            Usuario = _usuario.Text;
            DialogResult = DialogResult.OK;
            Close();
            return;
        }
        MessageBox.Show(this, "USUARIO OU SENHA INVALIDOS", Estilo.Versao, MessageBoxButtons.OK, MessageBoxIcon.Error);
        _senha.Clear();
        _senha.Focus();
    }
}
