namespace Talkeo.Windows.Tray;

internal sealed class TrayIcon : IDisposable
{
    private readonly System.Windows.Forms.NotifyIcon _icon;

    public TrayIcon(Action onQuit)
    {
        var menu   = new System.Windows.Forms.ContextMenuStrip();
        var status = new System.Windows.Forms.ToolStripMenuItem("Talkeo · corriendo") { Enabled = false };
        var quit   = new System.Windows.Forms.ToolStripMenuItem("Cerrar Talkeo");
        quit.Click += (_, _) => onQuit();
        menu.Items.Add(status);
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add(quit);

        _icon = new System.Windows.Forms.NotifyIcon
        {
            Text             = "Talkeo",
            Icon             = System.Drawing.SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible          = true,
        };
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
