namespace ErpAtlas;

/// <summary>Visual de sistema dos anos 2000: cinza, Tahoma, maiúsculas. É de propósito.</summary>
internal static class Estilo
{
    public static readonly Font Fonte = new("Tahoma", 9F);
    public static readonly Font Negrito = new("Tahoma", 9F, FontStyle.Bold);
    public static readonly Font Titulo = new("Tahoma", 11F, FontStyle.Bold);
    public static readonly Color FaixaTitulo = Color.FromArgb(10, 36, 106);
    public const string Versao = "ATLAS ERP 4.2.7";

    public static void Aplicar(Form f, string titulo)
    {
        f.Text = $"{Versao} - [{titulo}]";
        f.Font = Fonte;
        f.BackColor = SystemColors.Control;
        f.StartPosition = FormStartPosition.CenterScreen;
        f.KeyPreview = true;
    }

    public static Label Rotulo(string texto, int x, int y, int largura = 140) =>
        new() { Text = texto.ToUpperInvariant(), Location = new Point(x, y + 3), Size = new Size(largura, 18), Font = Fonte };

    /// <summary>Campo com Name e AccessibleName iguais: é o que o robô usa como seletor.</summary>
    public static T Campo<T>(T c, string nome, int x, int y, int largura) where T : Control
    {
        c.Name = nome;
        c.AccessibleName = nome;
        c.Location = new Point(x, y);
        c.Width = largura;
        c.Font = Fonte;
        return c;
    }

    public static Button Botao(string texto, string nome, int x, int y, int largura = 110)
    {
        var b = new Button { Text = texto, Name = nome, AccessibleName = nome, Location = new Point(x, y), Size = new Size(largura, 28), Font = Fonte, UseVisualStyleBackColor = true };
        return b;
    }

    public static Panel FaixaDeTitulo(string texto, int largura)
    {
        var p = new Panel { Location = new Point(0, 0), Size = new Size(largura, 30), BackColor = FaixaTitulo, Dock = DockStyle.Top };
        p.Controls.Add(new Label { Text = texto.ToUpperInvariant(), ForeColor = Color.White, Font = Titulo, AutoSize = true, Location = new Point(10, 6) });
        return p;
    }
}
