namespace CipherDNS;

public sealed class MainForm : Form
{
    readonly Engine engine = new();
    readonly ListView list = new() { View = View.Details, FullRowSelect = true, MultiSelect = false, Dock = DockStyle.Fill, HideSelection = false };
    readonly TextBox log = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Font = new Font("Consolas", 8.5f) };
    readonly Label status = new() { Dock = DockStyle.Top, Height = 28, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
    readonly Button connect = new() { Text = "Connect", Width = 100 };
    readonly Button disconnect = new() { Text = "Disconnect", Width = 100 };
    readonly Button test = new() { Text = "Test", Width = 80 };
    readonly CheckBox blockIpv6 = new() { Text = "Block IPv6 lookups (AAAA)", AutoSize = true, Checked = true };
    readonly NotifyIcon tray = new() { Text = "CipherDNS", Visible = true };
    bool quitting;

    public MainForm()
    {
        Text = "CipherDNS - personal DNSCrypt";
        Size = new Size(760, 560);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = SystemIcons.Shield;
        tray.Icon = SystemIcons.Shield;

        list.Columns.Add("Service", 220);
        list.Columns.Add("Type", 90);
        list.Columns.Add("Description", 400);
        foreach (var p in Providers.All)
            list.Items.Add(new ListViewItem([p.Name, p.Category, p.Description]) { Tag = p });
        list.Items[0].Selected = true;
        list.DoubleClick += (_, _) => Connect();

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 38, Padding = new Padding(4) };
        buttons.Controls.AddRange([connect, disconnect, test, blockIpv6]);
        connect.Click += (_, _) => Connect();
        disconnect.Click += (_, _) => Disconnect();
        test.Click += async (_, _) => Append(await Engine.TestAsync());

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 300 };
        split.Panel1.Controls.Add(list);
        split.Panel2.Controls.Add(log);
        Controls.Add(split);
        Controls.Add(buttons);
        Controls.Add(status);

        var menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => ShowWindow());
        menu.Items.Add("Disconnect", null, (_, _) => Disconnect());
        menu.Items.Add("Quit (restore DNS)", null, (_, _) => { quitting = true; Close(); });
        tray.ContextMenuStrip = menu;
        tray.DoubleClick += (_, _) => ShowWindow();

        engine.Log += line => BeginInvoke(() => Append(line));
        // A crash last time could leave Windows pointed at 127.0.0.1 with nothing listening.
        engine.RestoreWindowsDns();
        UpdateStatus();
    }

    void Connect()
    {
        if (list.SelectedItems.Count == 0) return;
        var provider = (Provider)list.SelectedItems[0].Tag!;
        try
        {
            Append($"Starting DNSCrypt with {provider.Name}...");
            engine.Start(provider, blockIpv6.Checked);
            tray.ShowBalloonTip(2000, "CipherDNS", $"Encrypted DNS via {provider.Name}", ToolTipIcon.Info);
        }
        catch (Exception e)
        {
            Append("Failed to start: " + e.Message);
            engine.Stop();
        }
        UpdateStatus();
    }

    void Disconnect()
    {
        engine.Stop();
        Append("Disconnected; Windows DNS restored.");
        UpdateStatus();
    }

    void UpdateStatus()
    {
        var on = engine.Running;
        status.Text = on ? $"  Protected: {engine.Current!.Name}" : "  Not protected - using normal Windows DNS";
        status.ForeColor = on ? Color.ForestGreen : Color.Firebrick;
        tray.Text = on ? $"CipherDNS: {Trim(engine.Current!.Name, 50)}" : "CipherDNS: off";
        disconnect.Enabled = test.Enabled = on;
    }

    static string Trim(string s, int n) => s.Length <= n ? s : s[..n];

    void Append(string line)
    {
        log.AppendText($"{DateTime.Now:HH:mm:ss}  {line}{Environment.NewLine}");
        if (!engine.Running) UpdateStatus();
    }

    void ShowWindow() { Show(); WindowState = FormWindowState.Normal; Activate(); }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!quitting && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            tray.ShowBalloonTip(1500, "CipherDNS", "Still running in the tray.", ToolTipIcon.None);
            return;
        }
        engine.Dispose();
        tray.Visible = false;
        base.OnFormClosing(e);
    }
}
