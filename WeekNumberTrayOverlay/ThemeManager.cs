using System;
using System.Drawing;
using System.Windows.Forms;

namespace WeekNumberTrayOverlay
{
    public enum ThemeStyle
    {
        Indigo,
        Standard,
        Dark,
        Retro95
    }

    public class ThemeManager
    {
        public static ThemeStyle CurrentTheme { get; private set; } = ThemeStyle.Standard;

        // Theme colors
        private static readonly Color IndigoBackgroundColor = Color.FromArgb(79, 70, 229); // indigo-600
        private static readonly Color IndigoHoverColor = Color.FromArgb(99, 102, 241); // indigo-500
        private static readonly Color IndigoTextColor = Color.White;

        private static readonly Color StandardBackgroundColor = Color.FromArgb(245, 245, 245); // off-white
        private static readonly Color StandardHoverColor = Color.FromArgb(230, 230, 230); // slightly darker off-white
        private static readonly Color StandardTextColor = Color.Black;

        private static readonly Color DarkBackgroundColor = Color.FromArgb(10, 10, 10); // not quite black
        private static readonly Color DarkHoverColor = Color.FromArgb(30, 30, 30); // slightly lighter
        private static readonly Color DarkTextColor = Color.FromArgb(245, 245, 245); // off-white

        private static readonly Color Retro95BackgroundColor = Color.FromArgb(245, 245, 245); // off-white
        private static readonly Color Retro95HoverColor = Color.FromArgb(75, 0, 130); // #4B0082 indigo
        private static readonly Color Retro95TextColor = Color.Black;
        private static readonly Color Retro95BorderColor = Color.Black;
        private static readonly Color Retro95HoverTextColor = Color.FromArgb(245, 245, 245); // off-white

        public static void ApplyTheme(Form form)
        {
            if (form is OverlayForm overlayForm)
            {
                overlayForm.ApplyCurrentTheme();
            }
        }

        public static void SetTheme(ThemeStyle theme)
        {
            CurrentTheme = theme;
        }

        public static Color GetBackgroundColor()
        {
            return CurrentTheme switch
            {
                ThemeStyle.Standard => StandardBackgroundColor,
                ThemeStyle.Dark => DarkBackgroundColor,
                ThemeStyle.Retro95 => Retro95BackgroundColor,
                _ => IndigoBackgroundColor
            };
        }

        public static Color GetHoverColor()
        {
            return CurrentTheme switch
            {
                ThemeStyle.Standard => StandardHoverColor,
                ThemeStyle.Dark => DarkHoverColor,
                ThemeStyle.Retro95 => Retro95HoverColor,
                _ => IndigoHoverColor
            };
        }

        public static Color GetTextColor()
        {
            return CurrentTheme switch
            {
                ThemeStyle.Standard => StandardTextColor,
                ThemeStyle.Dark => DarkTextColor,
                ThemeStyle.Retro95 => Retro95TextColor,
                _ => IndigoTextColor
            };
        }

        public static Color GetHoverTextColor()
        {
            return CurrentTheme switch
            {
                ThemeStyle.Retro95 => Retro95HoverTextColor,
                _ => GetTextColor() // For other themes, text color doesn't change on hover
            };
        }

        public static bool HasCustomBorder()
        {
            return CurrentTheme == ThemeStyle.Retro95;
        }

        public static Color GetBorderColor()
        {
            return CurrentTheme switch
            {
                ThemeStyle.Retro95 => Retro95BorderColor,
                _ => Color.FromArgb(30, 255, 255, 255) // Default subtle border
            };
        }

        public static int GetBorderWidth()
        {
            return CurrentTheme == ThemeStyle.Retro95 ? 2 : 1;
        }

        public static bool UsesAnimation()
        {
            return CurrentTheme == ThemeStyle.Retro95;
        }
    }
} 