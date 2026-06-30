using System;
using System.Drawing;
using System.IO;

namespace Hotshield.Resources
{
    public static class AppIcons
    {
        public static Icon ShieldGreen => LoadIcon("shield_green.ico");
        public static Icon ShieldAmber => LoadIcon("shield_amber.ico");
        public static Icon ShieldRed => LoadIcon("shield_red.ico");

        private static Icon LoadIcon(string name)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", name);
            return File.Exists(path) ? new Icon(path) : SystemIcons.Shield;
        }
    }
}
