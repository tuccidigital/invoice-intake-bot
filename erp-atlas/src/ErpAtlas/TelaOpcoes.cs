using ErpAtlas.Core;

namespace ErpAtlas;

internal sealed class TelaOpcoes : Form
{
    public TelaOpcoes(Repositorio repositorio)
    {
        Estilo.Aplicar(this, "Opções do simulador");
        Name = "TelaOpcoes";
        ClientSize = new Size(440, 200);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Controls.Add(Estilo.FaixaDeTitulo("Opções do simulador", 440));

        var falhar = new NumericUpDown { Minimum = 0, Maximum = 100, Value = repositorio.Opcoes.FalharACada };
        var atraso = new NumericUpDown { Minimum = 0, Maximum = 10000, Increment = 100, Value = repositorio.Opcoes.AtrasoGravacaoMs };
        Controls.Add(Estilo.Rotulo("Falhar a cada N gravações", 20, 50, 220));
        Controls.Add(Estilo.Campo(falhar, "numFalharACada", 250, 50, 80));
        Controls.Add(new Label { Text = "0 = nunca falha", Location = new Point(340, 53), AutoSize = true, ForeColor = SystemColors.GrayText });
        Controls.Add(Estilo.Rotulo("Atraso ao gravar (ms)", 20, 84, 220));
        Controls.Add(Estilo.Campo(atraso, "numAtrasoGravacao", 250, 84, 80));

        var salvar = Estilo.Botao("&Salvar", "btnSalvar", 210, 150);
        var cancelar = Estilo.Botao("&Cancelar", "btnCancelar", 320, 150);
        salvar.Click += (_, _) =>
        {
            repositorio.Opcoes.FalharACada = (int)falhar.Value;
            repositorio.Opcoes.AtrasoGravacaoMs = (int)atraso.Value;
            repositorio.SalvarOpcoes();
            Close();
        };
        cancelar.Click += (_, _) => Close();
        Controls.AddRange([salvar, cancelar]);
        AcceptButton = salvar;
        CancelButton = cancelar;
    }
}
