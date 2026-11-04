namespace CipherDNS;

public sealed class MainForm : Form
{
    readonly Engine engine = new();
    readonly ListView list = new()
    {
        View = View.Details, FullRowSelect = true, MultiSelect = false, Dock = DockStyle.Fill,
        HideSelection = false, OwnerDraw = true, BorderStyle = BorderStyle.None,
        BackColor = Theme.Input, ForeColor = Theme.Text, Font = new Font("Segoe UI", 9.5f),
    };
    readonly TextBox log = new()
    {
        Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None, BackColor = Theme.Input, ForeColor = Theme.Muted,
        Font = new Font("Consolas", 9f),
    };
    readonly Label status = new() { AutoSize = true, Font = new Font("Segoe UI Semibold", 12f), Margin = new Padding(0) };
    readonly Label subtitle = new() { AutoSize = true, ForeColor = Theme.Muted, Font = new Font("Segoe UI", 9f), Margin = new Padding(0, 2, 0, 0) };
    readonly PictureBox badge = new() { SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(44, 44), Margin = new Padding(0, 0, 12, 0) };
    readonly Button connect = Theme.MakeButton("Connect", primary: true);
    readonly Button disconnect = Theme.MakeButton("Disconnect");
    readonly Button test = Theme.MakeButton("Test");
    readonly CheckBox blockIpv6 = new()
    {
        Text = "Block IPv6 lookups (AAAA)", AutoSize = true, Checked = true,
        ForeColor = Theme.Text, Font = new Font("Segoe UI", 9.5f), Margin = new Padding(8, 6, 0, 0),
    };
    readonly NotifyIcon tray = new() { Text = "CipherDNS", Visible = true };
    readonly Icon iconOn = AppIcon.Create(true), iconOff = AppIcon.Create(false);
    bool quitting;

    public MainForm()
    {
        Text = "CipherDNS";
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96f, 96f);
        BackColor = Theme.Back;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 9.5f);
        ClientSize = new Size(880, 620);
        MinimumSize = new Size(640, 480);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = iconOn;

        var titles = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        titles.Controls.AddRange([status, subtitle]);
        var header = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 0, 12) };
        header.Controls.AddRange([badge, titles]);

        list.Columns.Add("Service", 230);
        list.Columns.Add("Type", 100);
        list.Columns.Add("Description", 420);
        foreach (var p in Providers.All)
            list.Items.Add(new ListViewItem([p.Name, p.Category, p.Description]) { Tag = p });
        list.Items[0].Selected = true;
        list.DoubleClick += (_, _) => Connect();
        list.DrawColumnHeader += DrawHeader;
        list.DrawSubItem += DrawRow;
        list.Resize += (_, _) => FitLastColumn();

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false, Margin = new Padding(0, 12, 0, 12) };
        buttons.Controls.AddRange([connect, disconnect, test, blockIpv6]);
        connect.Click += (_, _) => Connect();
        disconnect.Click += (_, _) => Disconnect();
        test.Click += async (_, _) => Append(await Engine.TestAsync());

        var logLabel = new Label { Text = "ACTIVITY", AutoSize = true, ForeColor = Theme.Muted, Font = new Font("Segoe UI Semibold", 8.5f), Margin = new Padding(0, 0, 0, 4) };

        // Rows size to their content; only the list and log stretch, so nothing gets clipped.
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(18, 14, 18, 16), BackColor = Theme.Back,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
        layout.Controls.Add(header, 0, 0);
        layout.Controls.Add(Framed(list), 0, 1);
        layout.Controls.Add(buttons, 0, 2);
        layout.Controls.Add(logLabel, 0, 3);
        layout.Controls.Add(Framed(log), 0, 4);
        Controls.Add(layout);

        var menu = new ContextMenuStrip { Renderer = new DarkMenuRenderer(), ShowImageMargin = false };
        menu.Items.Add("Open", null, (_, _) => ShowWindow());
        menu.Items.Add("Disconnect", null, (_, _) => Disconnect());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quit (restore DNS)", null, (_, _) => { quitting = true; Close(); });
        tray.ContextMenuStrip = menu;
        tray.DoubleClick += (_, _) => ShowWindow();

        engine.Log += line => BeginInvoke(() => Append(line));
        // A crash last time could leave Windows pointed at 127.0.0.1 with nothing listening.
        engine.RestoreWindowsDns();
        UpdateStatus();
    }

    protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.DarkTitleBar(this);
            Theme.DarkScrollbars(list);
            Theme.DarkScrollbars(log);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            FitLastColumn();
        }

        static Panel Framed(Control inner)
        {
            var frame = new Panel { Dock = DockStyle.Fill, Padding = new Padding(1), BackColor = Theme.Border, Margin = new Padding(0) };
            var pad = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6, 4, 4, 4), BackColor = Theme.Input };
            pad.Controls.Add(inner);
            frame.Controls.Add(pad);
            return frame;
        }

        void FitLastColumn()
        {
            if (list.Columns.Count < 3) return;
            int used = list.Columns[0].Width + list.Columns[1].Width;
            list.Columns[2].Width = Math.Max(120, list.ClientSize.Width - used - 4);
        }

        static readonly Font headerFont = new("Segoe UI Semibold", 9f);

        void DrawHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
        {
            using var back = new SolidBrush(Theme.Panel);
            e.Graphics.FillRectangle(back, e.Bounds);
            using var line = new Pen(Theme.Border);
            e.Graphics.DrawLine(line, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            var text = e.Bounds; text.Inflate(-6, 0);
            TextRenderer.DrawText(e.Graphics, e.Header!.Text, headerFont, text, Theme.Muted,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        void DrawRow(object? sender, DrawListViewSubItemEventArgs e)
        {
            bool selected = e.Item!.Selected;
            bool active = engine.Running && e.Item.Tag is Provider p && p == engine.Current;
            using var back = new SolidBrush(selected ? Theme.Selected : Theme.Input);
            e.Graphics.FillRectangle(back, e.Bounds);
            var color = e.ColumnIndex == 0 ? (active ? Theme.Accent : Theme.Text) : (selected ? Theme.Text : Theme.Muted);
            var text = e.Bounds; text.Inflate(-6, 0);
            var label = e.ColumnIndex == 0 && active ? "● " + e.SubItem!.Text : e.SubItem!.Text;
            TextRenderer.DrawText(e.Graphics, label, list.Font, text, color,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
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
            status.Text = on ? "Protected" : "Not protected";
            status.ForeColor = on ? Theme.Accent : Theme.Danger;
            subtitle.Text = on ? $"Encrypted DNS via {engine.Current!.Name}" : "Using normal Windows DNS. Pick a service and press Connect.";
            badge.Image?.Dispose();
            badge.Image = (on ? iconOn : iconOff).ToBitmap();
            tray.Icon = on ? iconOn : iconOff;
            var tip = on ? $"CipherDNS: {engine.Current!.Name}" : "CipherDNS: off";
            tray.Text = tip.Length > 63 ? tip[..63] : tip;
            disconnect.Enabled = test.Enabled = on;
            list.Invalidate();
        }

        void Append(string line)
        {
            log.AppendText($"{DateTime.Now:HH:mm:ss}  {line}{Environment.NewLine}");
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
