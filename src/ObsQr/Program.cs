using System.Diagnostics;

namespace ObsQr;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, "ObsQr.SingleInstance", out var first);
        if (!first) return;
        ApplicationConfiguration.Initialize();
        var s = Settings.Load();
        var obs = new ObsClient(s);
        var ctl = new Ctl(s, obs);
        var cts = new CancellationTokenSource();
        try { Web.Build(s, ctl).StartAsync().GetAwaiter().GetResult(); }
        catch (Exception e)
        {
            MessageBox.Show($"Could not listen on port {s.Port}: {e.Message}", "OBS QR Stream Control");
            return;
        }
        _ = obs.Run(cts.Token);
        ctl.Start(cts.Token);

        var open = (string path) => Process.Start(new ProcessStartInfo($"http://localhost:{s.Port}{path}") { UseShellExecute = true });
        var menu = new ContextMenuStrip();
        menu.Items.Add("Print QR code…", null, (_, _) => open("/print"));
        menu.Items.Add("Set OBS WebSocket password…", null, (_, _) =>
        {
            var p = Prompt("OBS WebSocket password", "Password from OBS > Tools > WebSocket Server Settings:");
            if (p != null) { s.ObsPass = p; s.Save(); }
        });
        menu.Items.Add("Exit", null, (_, _) => Application.Exit());
        using var tray = new NotifyIcon { Icon = SystemIcons.Application, Visible = true, ContextMenuStrip = menu, Text = "OBS QR Stream Control" };
        tray.DoubleClick += (_, _) => open("/print");
        if (s.IsNew) tray.ShowBalloonTip(8000, "OBS QR Stream Control", "Right-click this icon > Print QR code.", ToolTipIcon.Info);
        Application.Run();
        cts.Cancel();
    }

    static string Prompt(string title, string label)
    {
        using var f = new Form { Text = title, FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterScreen, ClientSize = new Size(380, 96), MaximizeBox = false, MinimizeBox = false };
        var t = new TextBox { Left = 10, Top = 34, Width = 360, UseSystemPasswordChar = true };
        var ok = new Button { Text = "OK", Left = 295, Top = 62, DialogResult = DialogResult.OK };
        f.Controls.AddRange(new Control[] { new Label { Text = label, Left = 10, Top = 10, Width = 360 }, t, ok });
        f.AcceptButton = ok;
        return f.ShowDialog() == DialogResult.OK ? t.Text : null;
    }
}
