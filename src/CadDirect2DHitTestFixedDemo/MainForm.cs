namespace CadDirect2DHitTestFixedDemo;

public sealed class MainForm : Form
{
    private readonly Direct2DCanvas _canvas;

    public MainForm()
    {
        Text = "Direct2D CAD Demo - double HitTest fixed";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 1200;
        Height = 800;

        _canvas = new Direct2DCanvas
        {
            Dock = DockStyle.Fill
        };

        _canvas.StatusChanged += status =>
        {
            Text = "Direct2D CAD Demo - " + status;
        };

        Controls.Add(_canvas);
    }
}
