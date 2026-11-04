using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace CipherDNS;

/// <summary>Draws the CipherDNS icon: a digital globe wrapped in a net, with a small lock.</summary>
public static class AppIcon
{
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr handle);

    public static Icon Create(bool active, int size = 64)
    {
        using var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            float s = size / 64f;
            var accent = active ? Color.FromArgb(0, 220, 170) : Color.FromArgb(150, 150, 150);
            var globe = new RectangleF(4 * s, 4 * s, 50 * s, 50 * s);

            using (var fill = new LinearGradientBrush(globe, Color.FromArgb(40, 44, 48), Color.FromArgb(22, 24, 27), 45f))
                g.FillEllipse(fill, globe);
            using var net = new Pen(Color.FromArgb(200, accent), 2.2f * s);
            using var thin = new Pen(Color.FromArgb(150, accent), 1.4f * s);

            // Latitude lines.
            foreach (var f in new[] { 0.25f, 0.5f, 0.75f })
            {
                float y = globe.Top + globe.Height * f;
                float half = (float)Math.Sqrt(0.25 - Math.Pow(f - 0.5, 2)) * globe.Width;
                g.DrawLine(thin, globe.Left + globe.Width / 2 - half, y, globe.Left + globe.Width / 2 + half, y);
            }
            // Longitude arcs.
            foreach (var w in new[] { 0.25f, 0.6f })
            {
                float width = globe.Width * w;
                g.DrawEllipse(thin, globe.Left + (globe.Width - width) / 2, globe.Top, width, globe.Height);
            }
            g.DrawLine(thin, globe.Left + globe.Width / 2, globe.Top, globe.Left + globe.Width / 2, globe.Bottom);
            g.DrawEllipse(net, globe);

            // Glowing data nodes where the net crosses.
            using var node = new SolidBrush(accent);
            foreach (var (x, y) in new[] { (0.5f, 0.25f), (0.2f, 0.5f), (0.8f, 0.5f), (0.5f, 0.75f), (0.5f, 0.5f) })
                g.FillEllipse(node, globe.Left + globe.Width * x - 2.2f * s, globe.Top + globe.Height * y - 2.2f * s, 4.4f * s, 4.4f * s);

            // Padlock in the bottom-right corner.
            var body = new RectangleF(38 * s, 40 * s, 22 * s, 18 * s);
            using (var shackle = new Pen(Color.FromArgb(235, 235, 235), 3f * s))
                g.DrawArc(shackle, 42 * s, 31 * s, 14 * s, 16 * s, 180, 180);
            using (var lockFill = new SolidBrush(active ? Color.FromArgb(0, 190, 145) : Color.FromArgb(120, 120, 120)))
            using (var path = Rounded(body, 3 * s))
            {
                g.FillPath(lockFill, path);
                using var edge = new Pen(Color.FromArgb(20, 22, 25), 1.5f * s);
                g.DrawPath(edge, path);
            }
            using var hole = new SolidBrush(Color.FromArgb(20, 22, 25));
            g.FillEllipse(hole, 46.5f * s, 45 * s, 5 * s, 5 * s);
            g.FillRectangle(hole, 48 * s, 48 * s, 2 * s, 6 * s);
        }
        var handle = bmp.GetHicon();
        var icon = (Icon)Icon.FromHandle(handle).Clone();
        DestroyIcon(handle);
        return icon;
    }

    static GraphicsPath Rounded(RectangleF r, float radius)
    {
        var path = new GraphicsPath();
        float d = radius * 2;
        path.AddArc(r.Left, r.Top, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    /// <summary>Writes a multi-size .ico for the executable.</summary>
    public static void SaveIco(string path)
    {
        int[] sizes = [16, 24, 32, 48, 64, 128, 256];
        var images = sizes.Select(sz =>
        {
            using var icon = Create(true, sz);
            using var bmp = icon.ToBitmap();
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }).ToArray();
        using var fs = File.Create(path);
        using var w = new BinaryWriter(fs);
        w.Write((short)0); w.Write((short)1); w.Write((short)sizes.Length);
        int offset = 6 + 16 * sizes.Length;
        for (int i = 0; i < sizes.Length; i++)
        {
            w.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i])); w.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));
            w.Write((byte)0); w.Write((byte)0); w.Write((short)1); w.Write((short)32);
            w.Write(images[i].Length); w.Write(offset);
            offset += images[i].Length;
        }
        foreach (var img in images) w.Write(img);
    }
}
