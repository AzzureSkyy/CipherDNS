namespace CipherDNS;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        using var single = new Mutex(true, "CipherDNS.SingleInstance", out var first);
        if (!first)
        {
            MessageBox.Show("CipherDNS is already running (check the tray).", "CipherDNS");
            return;
        }
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(args.Contains("--tray")));
    }
}
