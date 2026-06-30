using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Hotshield.Resources
{
    public static class ShieldIcons
    {
        private static Icon? _green, _amber, _red;

        public static Icon Green => _green ??= CreateShieldIcon(Color.FromArgb(16, 185, 129));
        public static Icon Amber => _amber ??= CreateShieldIcon(Color.FromArgb(245, 158, 11));
        public static Icon Red => _red ??= CreateShieldIcon(Color.FromArgb(239, 68, 68));

        private static Icon CreateShieldIcon(Color color)
        {
            using var bitmap = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // shield shape (simple: a pentagon-like polygon, filled with color, white outline)
            var shieldPoints = new Point[]
            {
                new Point(8, 1),  // top center
                new Point(3, 4),
                new Point(3, 10),
                new Point(8, 14), // bottom point
                new Point(13, 10),
                new Point(13, 4)
            };
            using var path = new GraphicsPath();
            path.AddPolygon(shieldPoints);
            g.FillPath(new SolidBrush(color), path);
            g.DrawPath(new Pen(Color.White, 1.2f), path);

            // small inner highlight (optional)
            var innerPoints = new Point[]
            {
                new Point(8, 3),
                new Point(5, 5),
                new Point(5, 9),
                new Point(8, 12),
                new Point(11, 9),
                new Point(11, 5)
            };
            using var innerPath = new GraphicsPath();
            innerPath.AddPolygon(innerPoints);
            g.FillPath(new SolidBrush(Color.FromArgb(80, Color.White)), innerPath);

            // convert to icon
            IntPtr hIcon = bitmap.GetHicon();
            var icon = Icon.FromHandle(hIcon);
            return (Icon)icon.Clone();
        }
    }
}
