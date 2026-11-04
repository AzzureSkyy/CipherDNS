namespace CipherDNS;

// Self-drawn notification so it always uses the CipherDNS icon and theme,
// instead of Windows' balloon toast which shows a cached app icon.
sealed class Toast : Form
{
    static Toast? current;
    readonly System.Windows.Forms.Timer timer = new();

    Toast(string title, string message, bool active, int ms)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Theme.Border;
        Padding = new Padding(1);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96f, 96f);
        ClientSize = new Size(340, 76);

        var body = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Panel, Padding = new Padding(12) };
        var icon = new PictureBox
        {
            Image = AppIcon.Create(active, 64).ToBitmap(), SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(40, 40), Location = new Point(14, 17),
        };
        var head = new Label
        {
            Text = title, AutoSize = true, ForeColor = active ? Theme.Accent : Theme.Text,
            Font = new Font("Segoe UI Semibold", 10f), Location = new Point(66, 14),
        };
        var text = new Label
        {
            Text = message, AutoEllipsis = true, ForeColor = Theme.Muted,
            Font = new Font("Segoe UI", 9f), Location = new Point(66, 38), Size = new Size(260, 22),
        };
        body.Controls.AddRange([icon, head, text]);
        Controls.Add(body);
        foreach (Control c in new Control[] { body, icon, head, text }) c.Click += (_, _) => Close();

        timer.Interval = ms;
        timer.Tick += (_, _) => Close();
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x80 | 0x08000000; // WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE
            return cp;
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        var area = Screen.PrimaryScreen!.WorkingArea;
        Location = new Point(area.Right - Width - 12, area.Bottom - Height - 12);
        Theme.DarkTitleBar(this);
        timer.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        timer.Dispose();
        if (current == this) current = null;
        base.OnFormClosed(e);
    }

    public static void Show(string title, string message, bool active, int ms = 3500)
    {
        current?.Close();
        current = new Toast(title, message, active, ms);
        current.Show();
    }
}
