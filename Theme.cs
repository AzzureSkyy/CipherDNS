using System.Runtime.InteropServices;

namespace CipherDNS;

/// <summary>Dark charcoal colour scheme.</summary>
public static class Theme
{
    public static readonly Color Back = Color.FromArgb(32, 34, 37);
    public static readonly Color Panel = Color.FromArgb(43, 45, 49);
    public static readonly Color Input = Color.FromArgb(24, 25, 28);
    public static readonly Color Border = Color.FromArgb(62, 65, 70);
    public static readonly Color Text = Color.FromArgb(225, 226, 228);
    public static readonly Color Muted = Color.FromArgb(150, 153, 158);
    public static readonly Color Accent = Color.FromArgb(0, 200, 155);
    public static readonly Color Danger = Color.FromArgb(235, 95, 90);
    public static readonly Color Selected = Color.FromArgb(0, 110, 88);

    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)] static extern int SetWindowTheme(IntPtr hwnd, string app, string? id);

    /// <summary>Dark title bar on Windows 10/11.</summary>
    public static void DarkTitleBar(Form form)
    {
        int on = 1;
        DwmSetWindowAttribute(form.Handle, 20, ref on, sizeof(int));
    }

    /// <summary>Dark scrollbars for list and text controls.</summary>
    public static void DarkScrollbars(Control control) => SetWindowTheme(control.Handle, "DarkMode_Explorer", null);

    public static Button MakeButton(string text, bool primary = false)
    {
        var b = new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(14, 4, 14, 4),
            Margin = new Padding(0, 0, 8, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Accent : Panel,
            ForeColor = primary ? Color.FromArgb(15, 20, 18) : Text,
            Font = new Font("Segoe UI", 9.5f, primary ? FontStyle.Bold : FontStyle.Regular),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
        };
        b.FlatAppearance.BorderColor = primary ? Accent : Border;
        b.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(0, 225, 175) : Color.FromArgb(55, 58, 63);
        b.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(0, 170, 130) : Color.FromArgb(66, 69, 75);
        return b;
    }
}

/// <summary>Context menu colours to match the dark theme.</summary>
public sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    public DarkMenuRenderer() : base(new DarkColors()) { }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? Theme.Text : Theme.Muted;
        base.OnRenderItemText(e);
    }

    sealed class DarkColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Theme.Panel;
        public override Color ImageMarginGradientBegin => Theme.Panel;
        public override Color ImageMarginGradientMiddle => Theme.Panel;
        public override Color ImageMarginGradientEnd => Theme.Panel;
        public override Color MenuBorder => Theme.Border;
        public override Color MenuItemBorder => Theme.Selected;
        public override Color MenuItemSelected => Theme.Selected;
        public override Color SeparatorDark => Theme.Border;
        public override Color SeparatorLight => Theme.Border;
    }
}
