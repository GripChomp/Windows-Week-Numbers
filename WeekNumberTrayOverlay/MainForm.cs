using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WeekNumberTrayOverlay
{
    public partial class MainForm : Form
    {
        private NotifyIcon notifyIcon = null!;
        private ContextMenuStrip contextMenu = null!;
        private OverlayForm overlayForm = null!;
        private System.Windows.Forms.Timer updateTimer = null!;
        private int currentWeekNumber;
        private DateTime lastCheckedDate;
        private bool showAsOverlay = true;
        private bool useDarkTrayIcon = false; // Default to white text (black background)

        public MainForm()
        {
            InitializeComponent();
            InitializeUI();
            LoadSettings();
            
            // Force apply a theme to ensure correct initial appearance
            ThemeManager.SetTheme(ThemeManager.CurrentTheme);
            overlayForm.ApplyCurrentTheme();
            
            SetupTimer();
            UpdateWeekNumber();
            
            // Size and position the form before showing
            overlayForm.Size = new Size(110, 40);
            overlayForm.PerformLayout();
            
            // Ensure display mode is applied correctly
            ApplyDisplayMode();
            UpdateMenuItemStates();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(0, 0);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "MainForm";
            this.Opacity = 0D;
            this.ShowInTaskbar = false;
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.StartPosition = FormStartPosition.Manual; // Prevent form from showing
            this.Location = new Point(-2000, -2000); // Position off-screen
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
        }

        private void MainForm_Load(object? sender, EventArgs e)
        {
            // Hide the main form completely and prevent it from showing in the taskbar
            this.Hide();
            this.ShowInTaskbar = false;
        }

        private void InitializeUI()
        {
            // Create overlay form with reference to this main form
            overlayForm = new OverlayForm(this);
            
            // Initialize with current week number immediately
            currentWeekNumber = GetCurrentIsoWeekNumber();
            overlayForm.UpdateWeekNumber(currentWeekNumber);

            // Create context menu
            contextMenu = new ContextMenuStrip();
            contextMenu.AutoClose = false; // Prevent auto-closing behavior
            
            // Add click handler to close menu when clicking outside
            contextMenu.LostFocus += (s, e) => contextMenu.Close();
            
            var showAsOverlayItem = new ToolStripMenuItem("Show as overlay");
            showAsOverlayItem.Click += (s, e) => {
                showAsOverlay = true;
                SaveSettings();
                ApplyDisplayMode();
                UpdateMenuItemStates();
                contextMenu.Close(); // Close the menu after selection
            };
            
            var showInTrayItem = new ToolStripMenuItem("Show in tray");
            showInTrayItem.Click += (s, e) => {
                showAsOverlay = false;
                SaveSettings();
                ApplyDisplayMode();
                UpdateMenuItemStates();
                contextMenu.Close(); // Close the menu after selection
            };

            var autoStartItem = new ToolStripMenuItem("Start with Windows");
            autoStartItem.Click += (s, e) => {
                autoStartItem.Checked = !autoStartItem.Checked;
                SetAutoStart(autoStartItem.Checked);
                SaveSettings();
                // Don't close menu for checkbox items to allow multiple selections
            };
            autoStartItem.Checked = IsAutoStartEnabled();

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => {
                contextMenu.Close();
                Application.Exit();
            };

            contextMenu.Items.AddRange(new ToolStripItem[] {
                showAsOverlayItem,
                showInTrayItem,
                new ToolStripSeparator(),
                autoStartItem,
                new ToolStripSeparator(),
                exitItem
            });
            
            // Set initial checked state based on current mode
            UpdateMenuItemStates();

            // Create notify icon
            notifyIcon = new NotifyIcon
            {
                Visible = false
                // Don't set ContextMenuStrip here to prevent auto-closing behavior
            };

            notifyIcon.MouseClick += (s, e) => {
                if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
                {
                    // Update menu state before showing
                    UpdateMenuItemStates();
                    
                    // Show the context menu and keep it open until user clicks elsewhere
                    // Use Control.MousePosition instead of Cursor.Position to avoid taskbar flash
                    contextMenu.Show(Control.MousePosition);
                }
            };
            
            // Prevent the default context menu behavior
            notifyIcon.ContextMenuStrip = null;

            // Set initial icon
            UpdateNotifyIcon();
        }

        private void LoadSettings()
        {
            // Default to Standard theme for new users
            ThemeManager.SetTheme(ThemeStyle.Standard);
            
            bool settingsLoaded = false;
            
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\WeekNumberTrayOverlay", false))
            {
                if (key != null)
                {
                    settingsLoaded = true;
                    
                    // Load display mode
                    var displayMode = key.GetValue("DisplayMode");
                    if (displayMode != null)
                    {
                        showAsOverlay = displayMode.ToString() == "Overlay";
                    }

                    // Load theme (only if user has previously set one)
                    var theme = key.GetValue("Theme");
                    if (theme != null && Enum.TryParse<ThemeStyle>(theme.ToString(), out var themeStyle))
                    {
                        ThemeManager.SetTheme(themeStyle);
                    }
                    
                    // Load tray icon color preference
                    var trayIconColor = key.GetValue("TrayIconColor");
                    if (trayIconColor != null)
                    {
                        useDarkTrayIcon = trayIconColor.ToString() == "Dark";
                    }
                }
            }
            
            // If this is the first run with no settings, default to overlay mode
            if (!settingsLoaded)
            {
                showAsOverlay = true;
            }
            
            // Make sure menu items reflect the loaded settings
            if (contextMenu != null)
            {
                UpdateMenuItemStates();
            }
        }

        private void SaveSettings()
        {
            using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(@"Software\WeekNumberTrayOverlay"))
            {
                if (key != null)
                {
                    // Save display mode
                    key.SetValue("DisplayMode", showAsOverlay ? "Overlay" : "Tray");
                    
                    // Save theme
                    key.SetValue("Theme", ThemeManager.CurrentTheme.ToString());
                    
                    // Save tray icon color preference
                    key.SetValue("TrayIconColor", useDarkTrayIcon ? "Dark" : "Light");
                }
            }
        }

        private void SetupTimer()
        {
            updateTimer = new System.Windows.Forms.Timer
            {
                Interval = 60000 // Check every minute
            };
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();
            
            lastCheckedDate = DateTime.Now.Date;
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            
            // Check if day has changed or it's midnight
            if (now.Date != lastCheckedDate.Date || 
                (now.Hour == 0 && now.Minute == 0))
            {
                UpdateWeekNumber();
                lastCheckedDate = now.Date;
            }
        }

        private void UpdateWeekNumber()
        {
            currentWeekNumber = GetCurrentIsoWeekNumber();
            UpdateNotifyIcon();
            overlayForm.UpdateWeekNumber(currentWeekNumber);
        }

        private void UpdateNotifyIcon()
        {
            // Create a bitmap with the week number
            using (Bitmap bitmap = new Bitmap(16, 16))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // Set background color based on text color choice
                g.Clear(useDarkTrayIcon ? Color.White : Color.Black);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                
                // Draw the week number
                using (Font font = new Font("Arial", 8, FontStyle.Bold))
                using (Brush brush = new SolidBrush(useDarkTrayIcon ? Color.Black : Color.White))
                {
                    string text = currentWeekNumber.ToString();
                    SizeF textSize = g.MeasureString(text, font);
                    g.DrawString(text, font, brush, 
                        (16 - textSize.Width) / 2, 
                        (16 - textSize.Height) / 2);
                }

                // Set the icon
                IntPtr hIcon = bitmap.GetHicon();
                notifyIcon.Icon = Icon.FromHandle(hIcon);
                notifyIcon.Text = $"Week {currentWeekNumber}";
            }
        }

        private void ApplyDisplayMode()
        {
            if (showAsOverlay)
            {
                notifyIcon.Visible = false;
                
                // Ensure form is properly sized and positioned before showing
                overlayForm.ApplyCurrentTheme();
                overlayForm.Show();
            }
            else
            {
                overlayForm.Hide();
                notifyIcon.Visible = true;
            }
            
            // Update menu item states to reflect current mode
            UpdateMenuItemStates();
        }
        
        public void SetDisplayMode(bool useOverlay)
        {
            // Only update if the mode is actually changing
            if (showAsOverlay != useOverlay)
            {
                showAsOverlay = useOverlay;
                SaveSettings();
                ApplyDisplayMode();
                UpdateMenuItemStates();
            }
        }
        
        public void SetTrayIconColor(bool useDark)
        {
            useDarkTrayIcon = useDark;
            SaveSettings();
            UpdateNotifyIcon();
        }
        
        public bool IsDarkTrayIconEnabled()
        {
            return useDarkTrayIcon;
        }

        public bool IsAutoStartEnabled()
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
            {
                return key?.GetValue("WeekNumberTrayOverlay") != null;
            }
        }

        public void SetAutoStart(bool enable)
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (key != null)
                {
                    if (enable)
                    {
                        key.SetValue("WeekNumberTrayOverlay", Application.ExecutablePath);
                    }
                    else
                    {
                        key.DeleteValue("WeekNumberTrayOverlay", false);
                    }
                }
            }
        }

        public static int GetCurrentIsoWeekNumber()
        {
            var today = DateTime.Now;
            var calendar = CultureInfo.CurrentCulture.Calendar;
            var weekRule = CultureInfo.CurrentCulture.DateTimeFormat.CalendarWeekRule;
            var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            return calendar.GetWeekOfYear(today, weekRule, firstDayOfWeek);
        }

        private void UpdateMenuItemStates()
        {
            // Update checked state of display mode menu items
            foreach (ToolStripItem item in contextMenu.Items)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    if (menuItem.Text == "Show as overlay")
                    {
                        menuItem.Checked = showAsOverlay;
                    }
                    else if (menuItem.Text == "Show in tray")
                    {
                        menuItem.Checked = !showAsOverlay;
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                notifyIcon?.Dispose();
                contextMenu?.Dispose();
                updateTimer?.Dispose();
                overlayForm?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
} 