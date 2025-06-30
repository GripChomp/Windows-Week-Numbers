using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WeekNumberTrayOverlay
{
    public class OverlayForm : Form
    {
        private Label weekNumberLabel = null!;
        private System.Windows.Forms.Timer fullscreenCheckTimer = null!;
        private ContextMenuStrip themeContextMenu = null!;
        private Retro95Effects? retro95Effects = null;
        private MainForm mainForm;

        // For rounded corners
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect,
            int nWidthEllipse,
            int nHeightEllipse
        );

        // For fullscreen detection
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public OverlayForm(MainForm owner)
        {
            // Store reference to the main form
            this.mainForm = owner;
            
            // Set size explicitly before initialization
            this.Size = new Size(110, 40);
            this.MinimumSize = new Size(110, 40);
            
            InitializeComponent();
            SetupThemeMenu();
            
            // Apply theme first to ensure correct appearance on initial load
            ApplyCurrentTheme();
            
            // Position after theme is applied to ensure correct size
            PositionOverlay();
            SetupFullscreenDetection();
            
            // Force layout and size again
            this.Size = new Size(110, 40);
            this.PerformLayout();
            
            // Save position when the form is moved
            this.LocationChanged += (s, e) => SavePosition();
        }

        private void InitializeComponent()
        {
            this.weekNumberLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // weekNumberLabel
            // 
            this.weekNumberLabel.AutoSize = false;
            this.weekNumberLabel.Dock = DockStyle.Fill;
            this.weekNumberLabel.BackColor = System.Drawing.Color.Transparent;
            this.weekNumberLabel.Margin = new Padding(0);
            this.weekNumberLabel.Padding = new Padding(0);
            // Try to use Inter font if available, otherwise fall back to Segoe UI
            try {
                this.weekNumberLabel.Font = new System.Drawing.Font("Inter", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            } catch {
                this.weekNumberLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            }
            this.weekNumberLabel.ForeColor = System.Drawing.Color.White;
            this.weekNumberLabel.Name = "weekNumberLabel";
            this.weekNumberLabel.Size = new System.Drawing.Size(100, 38);
            this.weekNumberLabel.TabIndex = 0;
            this.weekNumberLabel.Text = "Week 0";
            this.weekNumberLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // OverlayForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(79, 70, 229); // Indigo-600
            this.ClientSize = new System.Drawing.Size(110, 40);
            this.Padding = new Padding(6);
            this.Controls.Add(this.weekNumberLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "OverlayForm";
            this.Opacity = 0.95D;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Week Number";
            this.TopMost = true;
            
            // Add hover effect handlers
            this.MouseEnter += OverlayForm_MouseEnter;
            this.MouseLeave += OverlayForm_MouseLeave;
            this.weekNumberLabel.MouseEnter += (s, e) => this.OnMouseEnter(e);
            this.weekNumberLabel.MouseLeave += (s, e) => this.OnMouseLeave(e);

            // Make form draggable
            this.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    NativeMethods.ReleaseCapture();
                    NativeMethods.SendMessage(Handle, NativeMethods.WM_NCLBUTTONDOWN, NativeMethods.HT_CAPTION, 0);
                }
            };
            this.weekNumberLabel.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    NativeMethods.ReleaseCapture();
                    NativeMethods.SendMessage(Handle, NativeMethods.WM_NCLBUTTONDOWN, NativeMethods.HT_CAPTION, 0);
                }
            };

            // Override paint to handle custom themes
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.Paint += OverlayForm_Paint;

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void SetupThemeMenu()
        {
            themeContextMenu = new ContextMenuStrip();
            
            // Theme options
            var indigoItem = new ToolStripMenuItem("Indigo Theme");
            indigoItem.Click += (s, e) => ChangeTheme(ThemeStyle.Indigo);
            indigoItem.Checked = ThemeManager.CurrentTheme == ThemeStyle.Indigo;
            
            var standardItem = new ToolStripMenuItem("Standard Theme");
            standardItem.Click += (s, e) => ChangeTheme(ThemeStyle.Standard);
            standardItem.Checked = ThemeManager.CurrentTheme == ThemeStyle.Standard;
            
            var darkItem = new ToolStripMenuItem("Dark Theme");
            darkItem.Click += (s, e) => ChangeTheme(ThemeStyle.Dark);
            darkItem.Checked = ThemeManager.CurrentTheme == ThemeStyle.Dark;
            
            var retro95Item = new ToolStripMenuItem("Retro 95 Theme");
            retro95Item.Click += (s, e) => ChangeTheme(ThemeStyle.Retro95);
            retro95Item.Checked = ThemeManager.CurrentTheme == ThemeStyle.Retro95;
            
            // Settings submenu
            var settingsMenu = new ToolStripMenuItem("Settings");
            
            // Display mode options
            var showAsOverlayItem = new ToolStripMenuItem("Show as overlay");
            var showInTrayItem = new ToolStripMenuItem("Show in tray");
            
            showAsOverlayItem.Click += (s, e) => {
                mainForm.SetDisplayMode(true);
                showAsOverlayItem.Checked = true;
                showInTrayItem.Checked = false;
            };
            showInTrayItem.Click += (s, e) => {
                mainForm.SetDisplayMode(false);
                showAsOverlayItem.Checked = false;
                showInTrayItem.Checked = true;
            };
            
            // Set initial checked state based on current mode
            bool isOverlayMode = true; // Default to overlay mode
            try {
                // Try to get the current mode from registry
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\WeekNumberTrayOverlay", false))
                {
                    if (key != null)
                    {
                        var displayMode = key.GetValue("DisplayMode");
                        if (displayMode != null)
                        {
                            isOverlayMode = displayMode.ToString() == "Overlay";
                        }
                    }
                }
            } catch { /* Ignore any registry errors */ }
            
            showAsOverlayItem.Checked = isOverlayMode;
            showInTrayItem.Checked = !isOverlayMode;
            
            // Autostart option
            var autoStartItem = new ToolStripMenuItem("Start with Windows");
            autoStartItem.Click += (s, e) => {
                autoStartItem.Checked = !autoStartItem.Checked;
                mainForm.SetAutoStart(autoStartItem.Checked);
            };
            autoStartItem.Checked = mainForm.IsAutoStartEnabled();
            
            // Exit option
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => Application.Exit();
            
            // Tray icon color options
            var trayIconColorMenu = new ToolStripMenuItem("Tray Icon Color");
            
            var whiteTrayTextItem = new ToolStripMenuItem("White text (dark background)");
            var blackTrayTextItem = new ToolStripMenuItem("Black text (light background)");
            
            whiteTrayTextItem.Click += (s, e) => {
                mainForm.SetTrayIconColor(false);
                whiteTrayTextItem.Checked = true;
                blackTrayTextItem.Checked = false;
            };
            blackTrayTextItem.Click += (s, e) => {
                mainForm.SetTrayIconColor(true);
                whiteTrayTextItem.Checked = false;
                blackTrayTextItem.Checked = true;
            };
            
            // Set initial checked state based on current setting
            bool isDarkTrayIcon = mainForm.IsDarkTrayIconEnabled();
            whiteTrayTextItem.Checked = !isDarkTrayIcon;
            blackTrayTextItem.Checked = isDarkTrayIcon;
            
            trayIconColorMenu.DropDownItems.AddRange(new ToolStripItem[] {
                whiteTrayTextItem,
                blackTrayTextItem
            });
            
            // Add items to settings menu
            settingsMenu.DropDownItems.AddRange(new ToolStripItem[] {
                showAsOverlayItem,
                showInTrayItem,
                new ToolStripSeparator(),
                trayIconColorMenu,
                new ToolStripSeparator(),
                autoStartItem
            });
            
            themeContextMenu.Items.AddRange(new ToolStripItem[] {
                indigoItem,
                standardItem,
                darkItem,
                retro95Item,
                new ToolStripSeparator(),
                settingsMenu,
                exitItem
            });
            
            // Show context menu on right-click
            this.MouseClick += (s, e) => {
                if (e.Button == MouseButtons.Right)
                {
                    themeContextMenu.Show(this, e.Location);
                }
            };
            
            this.weekNumberLabel.MouseClick += (s, e) => {
                if (e.Button == MouseButtons.Right)
                {
                    themeContextMenu.Show(this, e.Location);
                }
            };
        }

        private void ChangeTheme(ThemeStyle theme)
        {
            ThemeManager.SetTheme(theme);
            
            // Update checkmarks
            foreach (ToolStripItem item in themeContextMenu.Items)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    menuItem.Checked = menuItem.Text.Contains(theme.ToString());
                }
            }
            
            // Apply the theme
            ApplyCurrentTheme();
        }

        public void ApplyCurrentTheme()
        {
            // Force correct size
            this.Size = new Size(110, 40);
            
            // Apply theme colors
            this.BackColor = ThemeManager.GetBackgroundColor();
            this.weekNumberLabel.ForeColor = ThemeManager.GetTextColor();
            
            // Ensure consistent padding and layout
            this.SuspendLayout();
            this.Padding = new Padding(6);
            this.weekNumberLabel.Margin = new Padding(0);
            this.weekNumberLabel.Padding = new Padding(0);
            
            // Handle Retro 95 effects
            if (ThemeManager.CurrentTheme == ThemeStyle.Retro95)
            {
                if (retro95Effects == null)
                {
                    retro95Effects = new Retro95Effects(this);
                }
            }
            
            // Apply rounded corners for non-Retro 95 themes
            if (ThemeManager.CurrentTheme != ThemeStyle.Retro95)
            {
                int cornerRadius = 8; // Indigo rounded-lg style
                this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, cornerRadius, cornerRadius));
            }
            else
            {
                // Square corners for Retro 95 theme
                this.Region = null;
            }
            
            // Force layout update
            this.ResumeLayout(true);
            this.PerformLayout();
            this.Invalidate();
        }

        private void OverlayForm_Paint(object? sender, PaintEventArgs e)
        {
            if (ThemeManager.CurrentTheme == ThemeStyle.Retro95)
            {
                // Draw Retro 95-style background and border
                if (retro95Effects != null)
                {
                    retro95Effects.DrawRetro95Background(e.Graphics, this.ClientRectangle);
                    retro95Effects.DrawRetro95Border(e.Graphics, this.ClientRectangle, ThemeManager.GetBorderWidth());
                    
                    // Hide the label and draw pixel text directly
                    this.weekNumberLabel.Visible = false;
                    retro95Effects.DrawPixelText(e.Graphics, this.weekNumberLabel.Text, this.weekNumberLabel.Font, this.ClientRectangle);
                }
            }
            else
            {
                // For other themes, draw a subtle border
                using (Pen borderPen = new Pen(ThemeManager.GetBorderColor(), ThemeManager.GetBorderWidth()))
                {
                    e.Graphics.DrawRoundedRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1, 8);
                }
                
                // Show the regular label
                this.weekNumberLabel.Visible = true;
            }
        }

        private void OverlayForm_MouseEnter(object? sender, EventArgs e)
        {
            if (ThemeManager.CurrentTheme == ThemeStyle.Retro95)
            {
                // Start Retro 95 animation
                if (retro95Effects != null)
                {
                    retro95Effects.StartHoverAnimation();
                }
            }
            else
            {
                // Standard hover effect
                this.BackColor = ThemeManager.GetHoverColor();
                this.Cursor = Cursors.Hand;
            }
        }

        private void OverlayForm_MouseLeave(object? sender, EventArgs e)
        {
            if (ThemeManager.CurrentTheme == ThemeStyle.Retro95)
            {
                // Stop Retro 95 animation
                if (retro95Effects != null)
                {
                    retro95Effects.StopHoverAnimation();
                }
            }
            else
            {
                // Standard hover effect
                this.BackColor = ThemeManager.GetBackgroundColor();
                this.Cursor = Cursors.Default;
            }
        }

        private void SetupFullscreenDetection()
        {
            fullscreenCheckTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000 // Check every second
            };
            fullscreenCheckTimer.Tick += FullscreenCheckTimer_Tick;
            fullscreenCheckTimer.Start();
        }

        private void FullscreenCheckTimer_Tick(object? sender, EventArgs e)
        {
            bool isFullscreenAppRunning = IsAnyApplicationFullscreen();
            this.TopMost = !isFullscreenAppRunning;
        }

        private bool IsAnyApplicationFullscreen()
        {
            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero) return false;

            // Get foreground window dimensions
            GetWindowRect(foregroundWindow, out RECT windowRect);
            
            // Get the screen dimensions
            Rectangle screenBounds = Screen.FromHandle(foregroundWindow).Bounds;
            
            // Check if the window covers the entire screen
            bool isFullscreen = 
                windowRect.Left <= 0 && 
                windowRect.Top <= 0 && 
                windowRect.Right >= screenBounds.Width && 
                windowRect.Bottom >= screenBounds.Height;
            
            return isFullscreen;
        }

        private void PositionOverlay()
        {
            // Try to load saved position
            Point? savedPosition = LoadPosition();
            
            if (savedPosition.HasValue)
            {
                // Make sure the position is within screen bounds
                Point pos = savedPosition.Value;
                Rectangle screenBounds = Screen.GetWorkingArea(pos);
                
                // Keep the form within the screen bounds
                pos.X = Math.Max(screenBounds.Left, Math.Min(pos.X, screenBounds.Right - this.Width));
                pos.Y = Math.Max(screenBounds.Top, Math.Min(pos.Y, screenBounds.Bottom - this.Height));
                
                this.Location = pos;
            }
            else
            {
                // Use default position (bottom of screen, 50% to the right from middle)
                int taskbarHeight = Screen.PrimaryScreen.Bounds.Height - Screen.PrimaryScreen.WorkingArea.Height;
                int screenMiddleX = Screen.PrimaryScreen.WorkingArea.Width / 2;
                int positionX = screenMiddleX + (screenMiddleX / 2) - (this.Width / 2);
                
                this.Location = new Point(
                    positionX,
                    Screen.PrimaryScreen.WorkingArea.Bottom - this.Height - 5
                );
            }
        }
        
        private Point? LoadPosition()
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\WeekNumberTrayOverlay", false))
            {
                if (key != null)
                {
                    var x = key.GetValue("PositionX");
                    var y = key.GetValue("PositionY");
                    
                    if (x != null && y != null && 
                        int.TryParse(x.ToString(), out int posX) && 
                        int.TryParse(y.ToString(), out int posY))
                    {
                        return new Point(posX, posY);
                    }
                }
            }
            
            return null;
        }
        
        public void SavePosition()
        {
            using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(@"Software\WeekNumberTrayOverlay"))
            {
                if (key != null)
                {
                    key.SetValue("PositionX", this.Location.X);
                    key.SetValue("PositionY", this.Location.Y);
                }
            }
        }

        public void UpdateWeekNumber(int weekNumber)
        {
            if (weekNumberLabel.InvokeRequired)
            {
                weekNumberLabel.Invoke(new Action(() => weekNumberLabel.Text = $"Week {weekNumber}"));
            }
            else
            {
                weekNumberLabel.Text = $"Week {weekNumber}";
            }
            
            // Refresh to update the Retro 95 text if needed
            this.Invalidate();
        }
    }

    // Native methods for window dragging
    internal static class NativeMethods
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
    }
} 