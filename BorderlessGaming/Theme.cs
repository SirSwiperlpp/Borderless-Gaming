using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BorderlessGaming
{
    /// <summary>
    ///     Lightweight dark/light theming for the WinForms UI.
    ///     Dark mode is the default; it can be toggled at runtime from the Options menu.
    /// </summary>
    public static class Theme
    {
        // Dark palette (roughly matching the Visual Studio "Dark" theme).
        public static readonly Color DarkBackground = Color.FromArgb(32, 32, 32);
        public static readonly Color DarkSurface = Color.FromArgb(45, 45, 48);
        public static readonly Color DarkControl = Color.FromArgb(51, 51, 55);
        public static readonly Color DarkForeground = Color.FromArgb(240, 240, 240);
        public static readonly Color DarkBorder = Color.FromArgb(63, 63, 70);
        public static readonly Color DarkAccent = Color.FromArgb(0, 122, 204);

        /// <summary>
        ///     Whether the most recently applied theme was the dark one.
        /// </summary>
        public static bool DarkModeEnabled { get; private set; }

        /// <summary>
        ///     Applies the dark or light theme to a form and all of its controls.
        ///     Safe to call from a constructor: the title-bar color is applied once the handle exists.
        /// </summary>
        public static void Apply(Form form, bool dark)
        {
            if (form == null)
            {
                return;
            }

            DarkModeEnabled = dark;

            if (dark)
            {
                form.BackColor = DarkBackground;
                form.ForeColor = DarkForeground;
            }
            else
            {
                form.BackColor = SystemColors.Control;
                form.ForeColor = SystemColors.ControlText;
            }

            ApplyToControls(form.Controls, dark);

            // Dark title bar (Windows 10 2004+ / Windows 11). Needs a valid window handle.
            if (form.IsHandleCreated)
            {
                UseImmersiveDarkMode(form.Handle, dark);
            }
            else
            {
                void OnHandleCreated(object s, EventArgs e)
                {
                    UseImmersiveDarkMode(form.Handle, dark);
                    form.HandleCreated -= OnHandleCreated;
                }

                form.HandleCreated += OnHandleCreated;
            }

            form.Invalidate(true);
        }

        private static void ApplyToControls(Control.ControlCollection controls, bool dark)
        {
            foreach (Control control in controls)
            {
                ApplyToControl(control, dark);

                if (control.ContextMenuStrip != null)
                {
                    ApplyToToolStrip(control.ContextMenuStrip, dark);
                }

                if (control.Controls.Count > 0)
                {
                    ApplyToControls(control.Controls, dark);
                }
            }
        }

        private static void ApplyToControl(Control control, bool dark)
        {
            switch (control)
            {
                case ToolStrip toolStrip:
                    ApplyToToolStrip(toolStrip, dark);
                    break;

                case ListBox listBox:
                    listBox.BorderStyle = dark ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
                    listBox.BackColor = dark ? DarkSurface : SystemColors.Window;
                    listBox.ForeColor = dark ? DarkForeground : SystemColors.WindowText;
                    break;

                case TextBox textBox:
                    textBox.BorderStyle = dark ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
                    textBox.BackColor = dark ? DarkSurface : SystemColors.Window;
                    textBox.ForeColor = dark ? DarkForeground : SystemColors.WindowText;
                    break;

                case Button button:
                    button.UseVisualStyleBackColor = !dark;
                    button.FlatStyle = dark ? FlatStyle.Flat : FlatStyle.Standard;
                    button.BackColor = dark ? DarkControl : SystemColors.Control;
                    button.ForeColor = dark ? DarkForeground : SystemColors.ControlText;
                    button.FlatAppearance.BorderColor = dark ? DarkBorder : SystemColors.ControlDark;
                    break;

                case LinkLabel linkLabel:
                    linkLabel.BackColor = Color.Transparent;
                    linkLabel.ForeColor = dark ? DarkForeground : SystemColors.ControlText;
                    linkLabel.LinkColor = dark ? DarkAccent : Color.FromArgb(0, 0, 238);
                    break;

                case Label label:
                    label.BackColor = Color.Transparent;
                    label.ForeColor = dark ? DarkForeground : SystemColors.ControlText;
                    break;

                default:
                    control.BackColor = dark ? DarkBackground : SystemColors.Control;
                    control.ForeColor = dark ? DarkForeground : SystemColors.ControlText;
                    break;
            }
        }

        /// <summary>
        ///     Themes a menu/status/context strip (and, through its renderer, any drop-down items).
        /// </summary>
        public static void ApplyToToolStrip(ToolStrip strip, bool dark)
        {
            if (strip == null)
            {
                return;
            }

            strip.Renderer = dark
                ? new DarkToolStripRenderer()
                : new ToolStripProfessionalRenderer();
            strip.BackColor = dark ? DarkSurface : SystemColors.Control;
            strip.ForeColor = dark ? DarkForeground : SystemColors.ControlText;
        }

        #region Dark title bar (DWM)

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19; // Windows 10 1809..1909
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;     // Windows 10 2004+

        private static void UseImmersiveDarkMode(IntPtr handle, bool enabled)
        {
            try
            {
                var value = enabled ? 1 : 0;
                if (DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref value, sizeof(int));
                }
            }
            catch
            {
                // DWM attribute not supported on this OS build; ignore.
            }
        }

        #endregion

        /// <summary>
        ///     Professional renderer with a dark color table that also forces light text and arrows.
        /// </summary>
        private sealed class DarkToolStripRenderer : ToolStripProfessionalRenderer
        {
            public DarkToolStripRenderer() : base(new DarkColorTable())
            {
                RoundedEdges = false;
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = e.Item.Enabled ? DarkForeground : Color.FromArgb(120, 120, 120);
                base.OnRenderItemText(e);
            }

            protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
            {
                e.ArrowColor = DarkForeground;
                base.OnRenderArrow(e);
            }
        }

        private sealed class DarkColorTable : ProfessionalColorTable
        {
            private static readonly Color Bar = DarkSurface;
            private static readonly Color Menu = Color.FromArgb(37, 37, 38);
            private static readonly Color Hover = Color.FromArgb(62, 62, 64);
            private static readonly Color Border = DarkBorder;

            public DarkColorTable()
            {
                UseSystemColors = false;
            }

            public override Color MenuStripGradientBegin => Bar;
            public override Color MenuStripGradientEnd => Bar;

            public override Color StatusStripGradientBegin => Bar;
            public override Color StatusStripGradientEnd => Bar;

            public override Color ToolStripDropDownBackground => Menu;
            public override Color ImageMarginGradientBegin => Menu;
            public override Color ImageMarginGradientMiddle => Menu;
            public override Color ImageMarginGradientEnd => Menu;

            public override Color MenuItemSelected => Hover;
            public override Color MenuItemSelectedGradientBegin => Hover;
            public override Color MenuItemSelectedGradientEnd => Hover;
            public override Color MenuItemPressedGradientBegin => Menu;
            public override Color MenuItemPressedGradientEnd => Menu;
            public override Color MenuItemBorder => DarkAccent;
            public override Color MenuBorder => Border;

            public override Color ButtonSelectedGradientBegin => Hover;
            public override Color ButtonSelectedGradientEnd => Hover;
            public override Color ButtonSelectedBorder => DarkAccent;
            public override Color ButtonPressedGradientBegin => Menu;
            public override Color ButtonPressedGradientEnd => Menu;

            public override Color CheckBackground => DarkAccent;
            public override Color CheckSelectedBackground => DarkAccent;
            public override Color CheckPressedBackground => DarkAccent;

            public override Color SeparatorDark => Border;
            public override Color SeparatorLight => Border;

            public override Color ToolStripBorder => Bar;
        }
    }
}
