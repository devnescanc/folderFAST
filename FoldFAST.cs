using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: System.Reflection.AssemblyTitle("Fold FAST v6.0 - Open Source Download & Media Manager")]
[assembly: System.Reflection.AssemblyProduct("Fold FAST")]
[assembly: System.Reflection.AssemblyVersion("6.0.0.0")]

namespace FoldFast
{
    public class DownloadTask
    {
        public string Id = Guid.NewGuid().ToString("N").Substring(0, 8);
        public string Url = "";
        public string PageUrl = "";
        public string FallbackStreamUrl = "";
        public string Referer = "";
        public string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36";
        public string MediaType = "video";
        public string FormatExt = "mp4";
        public string Quality = "1080";
        public bool UseYtDlp = false;
        public string FileName = "download.mp4";
        public string Category = "General";
        public string SaveDir = "";
        public int Connections = 8;
        public long TotalSize = 0;
        public long DownloadedSize = 0;
        public double ManualProgress = 0.0;
        public bool Resumable = false;
        public string Status = "Descargando";
        public double SpeedBps = 0;
        public string CreatedAt = DateTime.Now.ToString("HH:mm");
        public volatile bool CancelRequested = false;
        public Process ExternalProcess = null;

        public string DestPath
        {
            get
            {
                return Path.Combine(SaveDir, MainForm.SafeSanitizeFileName(FileName, ".bin"));
            }
        }

        public string TempDir
        {
            get
            {
                return Path.Combine(SaveDir, ".foldfast_" + Id);
            }
        }

        public double ProgressPercent
        {
            get
            {
                if (Status == "Completado" || Status == "Completed") return 100.0;
                if (ManualProgress > 0.0) return ManualProgress > 100.0 ? 100.0 : ManualProgress;
                if (TotalSize <= 0) return 0.0;
                double pct = (DownloadedSize * 100.0) / TotalSize;
                return pct > 100.0 ? 100.0 : pct;
            }
        }
    }

    public class GradientCardButton : Panel
    {
        public Color ColorStart = Color.FromArgb(255, 56, 92);
        public Color ColorEnd = Color.FromArgb(255, 77, 66);
        public Color TextColor = Color.White;
        public Color SubTextColor = Color.FromArgb(235, 235, 245);
        public Color BadgeBgColor = Color.FromArgb(65, 255, 255, 255);
        public Color IconBoxColor = Color.FromArgb(45, 0, 0, 0);
        public Color IconGlyphColor = Color.White;
        public string TitleText = "";
        public string SubtitleText = "";
        public string PillText = "";
        public string IconSymbol = "⚡";
        private bool isHovered = false;

        public GradientCardButton()
        {
            this.DoubleBuffered = true;
            this.Cursor = Cursors.Hand;
            this.BackColor = Color.Transparent;
            this.MouseEnter += (s, e) => { isHovered = true; this.Invalidate(); };
            this.MouseLeave += (s, e) => { isHovered = false; this.Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new Rectangle(1, 1, this.Width - 3, this.Height - 3);
            using (GraphicsPath path = MainForm.GetRoundRect(rect, 18))
            using (LinearGradientBrush brush = new LinearGradientBrush(rect, ColorStart, ColorEnd, LinearGradientMode.Horizontal))
            {
                g.FillPath(brush, path);
                if (isHovered)
                {
                    using (Pen p = new Pen(Color.FromArgb(190, 255, 255, 255), 2f))
                    {
                        g.DrawPath(p, path);
                    }
                }
            }

            Rectangle iconRect = new Rectangle(14, 16, 40, 40);
            using (GraphicsPath iconPath = MainForm.GetRoundRect(iconRect, 11))
            using (SolidBrush ib = new SolidBrush(IconBoxColor))
            {
                g.FillPath(ib, iconPath);
            }
            using (Font iconFont = new Font("Segoe UI Emoji", 13F, FontStyle.Bold))
            using (SolidBrush igb = new SolidBrush(IconGlyphColor))
            {
                SizeF isz = g.MeasureString(IconSymbol, iconFont);
                g.DrawString(IconSymbol, iconFont, igb, iconRect.X + (iconRect.Width - isz.Width) / 2, iconRect.Y + (iconRect.Height - isz.Height) / 2);
            }

            using (Font fTitle = new Font("Segoe UI", 10.5F, FontStyle.Bold))
            using (SolidBrush tb = new SolidBrush(TextColor))
            {
                g.DrawString(TitleText, fTitle, tb, new PointF(64, 13));
            }

            using (Font fSub = new Font("Segoe UI", 8.2F, FontStyle.Regular))
            using (SolidBrush sb = new SolidBrush(SubTextColor))
            {
                RectangleF subRect = new RectangleF(64, 34, this.Width - 74, 32);
                g.DrawString(SubtitleText, fSub, sb, subRect);
            }

            if (!string.IsNullOrEmpty(PillText))
            {
                using (Font fPill = new Font("Segoe UI", 8F, FontStyle.Bold))
                {
                    SizeF psz = g.MeasureString(PillText, fPill);
                    Rectangle pillRect = new Rectangle(64, this.Height - 30, (int)psz.Width + 18, 21);
                    using (GraphicsPath pp = MainForm.GetRoundRect(pillRect, 10))
                    using (SolidBrush pb = new SolidBrush(BadgeBgColor))
                    using (SolidBrush ptb = new SolidBrush(TextColor))
                    {
                        g.FillPath(pb, pp);
                        g.DrawString(PillText, fPill, ptb, pillRect.X + 9, pillRect.Y + 3);
                    }
                }
            }
        }
    }

    public class MainForm : Form
    {
        private MenuStrip mainMenu;
        private ToolStripMenuItem menuFile;
        private ToolStripMenuItem menuItemNewUrl;
        private ToolStripMenuItem menuItemNewMedia;
        private ToolStripMenuItem menuItemOpenFolder;
        private ToolStripMenuItem menuItemStartup;
        private ToolStripMenuItem menuItemExit;

        private ToolStripMenuItem menuTheme;
        private ToolStripMenuItem menuItemDark;
        private ToolStripMenuItem menuItemLight;

        private ToolStripMenuItem menuLang;
        private ToolStripMenuItem menuItemEs;
        private ToolStripMenuItem menuItemEn;

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private bool allowRealExit = false;

        private Panel header;
        private Panel cardsRow;
        private Panel bodyPanel;
        private Panel leftSidebar;
        private Panel rightContainer;
        private Panel statusPanel;
        private Label lblBrand;
        private Label lblSubBrand;
        private Label lblLim;
        private Button btnLangToggle;
        private Button btnThemeToggle;
        private Button btnOpenFolderTop;
        private GradientCardButton cardMedia;
        private GradientCardButton cardDirect;
        private GradientCardButton cardBatch;
        private GradientCardButton cardManage;
        private ListView listView;
        private FlowLayoutPanel catPanel;
        private Label lblStatus;
        private Label lblTotalSpeed;
        private NumericUpDown numSpeedLimit;
        private CheckBox chkClipboard;
        private System.Windows.Forms.Timer uiTimer;
        private Image brandLogoImg;

        private List<DownloadTask> tasks = new List<DownloadTask>();
        private string selectedCategory = "Todas";
        private List<Button> catButtons = new List<Button>();
        private string baseDir;
        private string binDir;
        private string settingsFile;
        private string ytDlpPath;
        private string ffmpegPath;
        private string lastClipboard = "";
        private volatile int speedLimitKbps = 0;
        public volatile bool IsDarkMode = true;
        public volatile string CurrentLang = "es";
        public volatile bool RunOnStartup = true;
        private HttpListener bridgeListener;
        private static readonly object binaryLock = new object();

        private static readonly Color CoralStart = Color.FromArgb(255, 56, 92);
        private static readonly Color CoralEnd = Color.FromArgb(255, 79, 64);
        private static readonly Color LimeStart = Color.FromArgb(180, 255, 57);
        private static readonly Color LimeEnd = Color.FromArgb(56, 229, 77);
        private static readonly Color BlueStart = Color.FromArgb(37, 99, 235);
        private static readonly Color BlueEnd = Color.FromArgb(29, 78, 216);

        private Color ThemeBgMain { get { return IsDarkMode ? Color.FromArgb(18, 18, 22) : Color.FromArgb(241, 245, 249); } }
        private Color ThemeBgSurface { get { return IsDarkMode ? Color.FromArgb(26, 26, 31) : Color.FromArgb(255, 255, 255); } }
        private Color ThemeBgAltRow { get { return IsDarkMode ? Color.FromArgb(31, 31, 37) : Color.FromArgb(248, 250, 252); } }
        private Color ThemeBgCard { get { return IsDarkMode ? Color.FromArgb(38, 38, 44) : Color.FromArgb(226, 232, 240); } }
        private Color ThemeTextMain { get { return IsDarkMode ? Color.White : Color.FromArgb(15, 23, 42); } }
        private Color ThemeTextMuted { get { return IsDarkMode ? Color.FromArgb(161, 161, 170) : Color.FromArgb(100, 116, 139); } }

        private static readonly Dictionary<string, string[]> Categories = new Dictionary<string, string[]>
        {
            { "Comprimidos", new[] { ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz" } },
            { "Documentos",  new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv", ".epub" } },
            { "Musica",      new[] { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a", ".wma", ".opus" } },
            { "Programas",   new[] { ".exe", ".msi", ".iso", ".bat", ".cmd", ".apk", ".dmg" } },
            { "Videos",      new[] { ".mp4", ".mkv", ".avi", ".mov", ".webm", ".flv", ".wmv", ".m4v", ".ts" } }
        };

        public string Tr(string es, string en)
        {
            return CurrentLang == "en" ? en : es;
        }

        public static GraphicsPath GetRoundRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();
            if (radius == 0 || bounds.Width <= diameter || bounds.Height <= diameter)
            {
                path.AddRectangle(bounds);
                return path;
            }
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static string SafeSanitizeFileName(string rawInput, string defaultExt)
        {
            if (string.IsNullOrEmpty(defaultExt)) defaultExt = ".mp4";
            if (!defaultExt.StartsWith(".")) defaultExt = "." + defaultExt;
            defaultExt = Regex.Replace(defaultExt.ToLowerInvariant(), @"[^\.a-z0-9]", "");
            if (defaultExt.Length < 2) defaultExt = ".mp4";

            if (string.IsNullOrWhiteSpace(rawInput))
            {
                return "foldfast_" + DateTime.Now.ToString("HHmmss") + defaultExt;
            }

            StringBuilder sb = new StringBuilder(rawInput.Length);
            char[] invalidChars = Path.GetInvalidFileNameChars();
            char[] invalidPath = Path.GetInvalidPathChars();

            foreach (char c in rawInput)
            {
                if (c < 32 || c == '|' || c == '"' || c == '<' || c == '>' || c == ':' || c == '*' || c == '?' || c == '/' || c == '\\')
                {
                    sb.Append('_');
                    continue;
                }
                bool bad = false;
                for (int i = 0; i < invalidChars.Length; i++)
                {
                    if (c == invalidChars[i]) { bad = true; break; }
                }
                if (!bad)
                {
                    for (int i = 0; i < invalidPath.Length; i++)
                    {
                        if (c == invalidPath[i]) { bad = true; break; }
                    }
                }
                sb.Append(bad ? '_' : c);
            }

            string clean = Regex.Replace(sb.ToString(), @"_+", "_").Trim(' ', '_', '.');
            if (string.IsNullOrEmpty(clean))
            {
                clean = "foldfast_" + DateTime.Now.ToString("HHmmss");
            }

            int lastDot = clean.LastIndexOf('.');
            if (lastDot > 0 && (clean.Length - lastDot) <= 6)
            {
                clean = clean.Substring(0, lastDot);
            }

            if (clean.Length > 85)
            {
                clean = clean.Substring(0, 85).Trim(' ', '_', '.');
            }
            if (string.IsNullOrEmpty(clean))
            {
                clean = "foldfast_" + DateTime.Now.ToString("HHmmss");
            }

            return clean + defaultExt;
        }

        public static string GuessFileName(string url, string title, string mediaType, string formatExt)
        {
            string ext = !string.IsNullOrEmpty(formatExt) ? formatExt.ToLowerInvariant() : "";
            ext = Regex.Replace(ext, @"[^a-z0-9]", "");

            if (string.IsNullOrEmpty(ext))
            {
                try
                {
                    string cleanU = (url ?? "").Split('?')[0].Split('#')[0];
                    int lastDot = cleanU.LastIndexOf('.');
                    int lastSlash = cleanU.LastIndexOf('/');
                    if (lastDot > lastSlash && (cleanU.Length - lastDot) <= 6)
                    {
                        ext = Regex.Replace(cleanU.Substring(lastDot + 1).ToLowerInvariant(), @"[^a-z0-9]", "");
                    }
                }
                catch { }
            }
            if (string.IsNullOrEmpty(ext))
            {
                ext = mediaType == "audio" ? "mp3" : (mediaType == "video" ? "mp4" : "bin");
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                return SafeSanitizeFileName(title, "." + ext);
            }
            try
            {
                string cleanUrl = (url ?? "").Split('?')[0].Split('#')[0];
                int slash = cleanUrl.LastIndexOf('/');
                string lastSegment = slash >= 0 ? cleanUrl.Substring(slash + 1) : cleanUrl;
                lastSegment = Uri.UnescapeDataString(lastSegment);
                if (string.IsNullOrWhiteSpace(lastSegment) || lastSegment == "videoplayback" || lastSegment == "watch")
                {
                    return SafeSanitizeFileName("foldfast_" + DateTime.Now.ToString("HHmmss"), "." + ext);
                }
                return SafeSanitizeFileName(lastSegment, "." + ext);
            }
            catch
            {
                return "foldfast_" + DateTime.Now.ToString("HHmmss") + "." + ext;
            }
        }

        public MainForm(bool startMinimized)
        {
            ServicePointManager.DefaultConnectionLimit = 64;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            this.Size = new Size(1120, 730);
            this.MinimumSize = new Size(960, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5F);

            baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "FoldFAST");
            binDir = Path.Combine(baseDir, "bin");
            settingsFile = Path.Combine(baseDir, "settings.json");
            ytDlpPath = Path.Combine(binDir, "yt-dlp.exe");
            ffmpegPath = Path.Combine(binDir, "ffmpeg.exe");
            Directory.CreateDirectory(baseDir);
            Directory.CreateDirectory(binDir);

            LoadSettings();

            try
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string icoFile = Path.Combine(exeDir, "app_icon.ico");
                string pngFile = Path.Combine(exeDir, "FoldFAST_Chrome_Extension", "icon48.png");
                if (File.Exists(icoFile)) this.Icon = new Icon(icoFile);
                if (File.Exists(pngFile)) brandLogoImg = Image.FromFile(pngFile);
            }
            catch { }

            EnsureDesktopShortcut();
            SetWindowsStartup(RunOnStartup);
            BuildModernUI();
            SetupSystemTray();
            ApplyLanguage();
            ApplyTheme();
            StartChromeBridgeServer();

            // Pre-inicializar motores en segundo plano al abrir para que las descargas de YouTube inicien rápido sin esperas
            Task.Run(() => WarmUpBinariesInBackground());

            uiTimer = new System.Windows.Forms.Timer();
            uiTimer.Interval = 350;
            uiTimer.Tick += UiTimer_Tick;
            uiTimer.Start();

            this.FormClosing += MainForm_FormClosing;

            if (startMinimized)
            {
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                this.Load += (s, e) => { this.Hide(); };
            }
        }

        private void WarmUpBinariesInBackground()
        {
            try
            {
                DownloadTask dummy = new DownloadTask();
                EnsureMediaBinaries(dummy, false);
            }
            catch { }
        }

        private void SetupSystemTray()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("⚡ Abrir Fold FAST / Open", null, (s, e) => RestoreFromTray());
            trayMenu.Items.Add("📂 Abrir Carpeta / Open Folder", null, (s, e) => { try { Process.Start("explorer.exe", baseDir); } catch { } });
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("❌ Salir completamente / Exit", null, (s, e) =>
            {
                allowRealExit = true;
                if (trayIcon != null) trayIcon.Visible = false;
                Application.Exit();
            });

            trayIcon = new NotifyIcon
            {
                Text = "Fold FAST - Activo en segundo plano",
                Icon = this.Icon ?? SystemIcons.Application,
                ContextMenuStrip = trayMenu,
                Visible = true
            };
            trayIcon.DoubleClick += (s, e) => RestoreFromTray();
        }

        private void RestoreFromTray()
        {
            this.Show();
            this.ShowInTaskbar = true;
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            this.Activate();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Mantener activo en segundo plano al cerrar con la X (salvo cuando Windows se apaga o el usuario elige Salir completamente)
            if (!allowRealExit && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                if (trayIcon != null)
                {
                    trayIcon.ShowBalloonTip(
                        2000,
                        "Fold FAST",
                        Tr("Fold FAST sigue activo en segundo plano para capturar tus descargas.", "Fold FAST is still active in the background to capture your downloads."),
                        ToolTipIcon.Info
                    );
                }
            }
        }

        public void SetWindowsStartup(bool enable)
        {
            RunOnStartup = enable;
            try
            {
                using (RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (rk != null)
                    {
                        if (enable)
                        {
                            rk.SetValue("FoldFAST", "\"" + Application.ExecutablePath + "\" --minimized");
                        }
                        else
                        {
                            rk.DeleteValue("FoldFAST", false);
                        }
                    }
                }
            }
            catch { }
            if (menuItemStartup != null) menuItemStartup.Checked = enable;
        }

        private void EnsureDesktopShortcut()
        {
            try
            {
                string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string lnkPath = Path.Combine(desktopDir, "Fold FAST.lnk");
                string exePath = Application.ExecutablePath;
                string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_icon.ico");
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    object shell = Activator.CreateInstance(shellType);
                    object shortcut = shellType.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { lnkPath });
                    Type scType = shortcut.GetType();
                    scType.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { exePath });
                    scType.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { AppDomain.CurrentDomain.BaseDirectory });
                    scType.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { "Fold FAST - Open Source Download Manager" });
                    if (File.Exists(icoPath))
                    {
                        scType.InvokeMember("IconLocation", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { icoPath });
                    }
                    scType.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, shortcut, null);
                }
            }
            catch { }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(settingsFile))
                {
                    string txt = File.ReadAllText(settingsFile, Encoding.UTF8);
                    if (txt.Contains("\"light\"")) IsDarkMode = false;
                    if (txt.Contains("\"en\"")) CurrentLang = "en";
                    if (txt.Contains("\"startup\":false")) RunOnStartup = false;
                }
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                string json = "{\"theme\":\"" + (IsDarkMode ? "dark" : "light") + "\",\"lang\":\"" + CurrentLang + "\",\"startup\":" + (RunOnStartup ? "true" : "false") + "}";
                File.WriteAllText(settingsFile, json, Encoding.UTF8);
            }
            catch { }
        }

        public void ToggleTheme(bool? forceDark = null)
        {
            if (forceDark.HasValue) IsDarkMode = forceDark.Value;
            else IsDarkMode = !IsDarkMode;
            SaveSettings();
            ApplyTheme();
            ApplyLanguage();
        }

        public void ToggleLanguage(string forceLang = null)
        {
            if (!string.IsNullOrEmpty(forceLang)) CurrentLang = (forceLang == "en") ? "en" : "es";
            else CurrentLang = (CurrentLang == "es") ? "en" : "es";
            SaveSettings();
            ApplyLanguage();
            ApplyTheme();
            RefreshListView();
        }

        private void ApplyLanguage()
        {
            this.Text = Tr(
                "Fold FAST v6.0 - Gestor y Acelerador de Descargas Open Source",
                "Fold FAST v6.0 - Open Source Download & Media Accelerator"
            );

            // Actualizar barra de menú superior
            if (menuFile != null)
            {
                menuFile.Text = Tr("📁 Archivo", "📁 File");
                menuItemNewUrl.Text = Tr("➕ Añadir Enlace Directo...", "➕ Add Direct Link...");
                menuItemNewMedia.Text = Tr("🎬 Descargar Video o Audio...", "🎬 Download Video or Audio...");
                menuItemOpenFolder.Text = Tr("📂 Abrir Carpeta de Descargas", "📂 Open Downloads Folder");
                menuItemStartup.Text = Tr("🚀 Iniciar junto con Windows", "🚀 Start with Windows");
                menuItemStartup.Checked = RunOnStartup;
                menuItemExit.Text = Tr("❌ Salir completamente de Fold FAST", "❌ Exit Fold FAST completely");
            }

            if (menuTheme != null)
            {
                menuTheme.Text = Tr("🎨 Tema (Modo Oscuro / Claro)", "🎨 Theme (Dark / Light Mode)");
                menuItemDark.Text = Tr("🌙 Modo Oscuro", "🌙 Dark Mode");
                menuItemLight.Text = Tr("☀️ Modo Claro", "☀️ Light Mode");
                menuItemDark.Checked = IsDarkMode;
                menuItemLight.Checked = !IsDarkMode;
            }

            if (menuLang != null)
            {
                menuLang.Text = Tr("🌐 Idioma / Language", "🌐 Language / Idioma");
                menuItemEs.Text = "🇪🇸 Español";
                menuItemEn.Text = "🇺🇸 English";
                menuItemEs.Checked = (CurrentLang == "es");
                menuItemEn.Checked = (CurrentLang == "en");
            }

            if (lblSubBrand != null)
            {
                lblSubBrand.Text = Tr(
                    "Activo junto con Windows • Captura videos, música y cualquier archivo automáticamente",
                    "Starts with Windows • Automatically captures videos, audio & any file download"
                );
            }

            if (btnLangToggle != null)
            {
                btnLangToggle.Text = CurrentLang == "es" ? "🌐 Español ▾" : "🌐 English ▾";
            }

            if (btnThemeToggle != null)
            {
                btnThemeToggle.Text = IsDarkMode ? Tr("☀️ Modo Claro", "☀️ Light Mode") : Tr("🌙 Modo Oscuro", "🌙 Dark Mode");
            }

            if (lblLim != null) lblLim.Text = Tr("Límite KB/s:", "Limit KB/s:");
            if (btnOpenFolderTop != null) btnOpenFolderTop.Text = Tr("📂 Carpeta", "📂 Folder");
            if (chkClipboard != null) chkClipboard.Text = Tr("Vigilar portapapeles", "Watch clipboard");

            if (cardMedia != null)
            {
                cardMedia.TitleText = Tr("Nuevo Video o Audio", "New Video or Audio");
                cardMedia.SubtitleText = Tr("Facebook, YouTube, TikTok, Instagram o cualquier web", "Facebook, YouTube, TikTok, Instagram or any site");
                cardMedia.PillText = Tr("🎬 Elegir Calidad", "🎬 Pick Quality");
                cardMedia.Invalidate();
            }
            if (cardDirect != null)
            {
                cardDirect.TitleText = Tr("Descarga Directa 8x", "8x Direct Download");
                cardDirect.SubtitleText = Tr("Acelera archivos .zip, .exe, .iso, .pdf en 8 hilos", "Accelerate .zip, .exe, .iso, .pdf files with 8 threads");
                cardDirect.PillText = Tr("➕ Añadir Enlace", "➕ Add Link");
                cardDirect.Invalidate();
            }
            if (cardBatch != null)
            {
                cardBatch.TitleText = Tr("Descarga por Lotes", "Batch Download");
                cardBatch.SubtitleText = Tr("Pega listas de enlaces o secuencias [01-10]", "Paste multiple links or wildcard ranges [01-10]");
                cardBatch.PillText = Tr("🚀 Abrir Lotes", "🚀 Open Batch");
                cardBatch.Invalidate();
            }
            if (cardManage != null)
            {
                cardManage.TitleText = Tr("Control de Tareas", "Task Controls");
                cardManage.SubtitleText = Tr("Pausa, reanuda o limpia las descargas", "Pause, resume or clean selected downloads");
                cardManage.PillText = Tr("⏯ Administrar", "⏯ Manage");
                cardManage.Invalidate();
            }

            Dictionary<string, string> catLabels = new Dictionary<string, string>
            {
                { "Todas",       Tr("📁  Todas las Tareas", "📁  All Downloads") },
                { "Videos",      Tr("🎬  Videos",           "🎬  Videos") },
                { "Musica",      Tr("🎵  Música y Audio",   "🎵  Music & Audio") },
                { "Comprimidos", Tr("📦  Comprimidos",      "📦  Compressed") },
                { "Programas",   Tr("💻  Programas (.exe)", "💻  Programs (.exe)") },
                { "Documentos",  Tr("📄  Documentos",       "📄  Documents") },
                { "En curso",    Tr("⚡  En Curso / Cola",  "⚡  Active / Queue") },
                { "Completado",  Tr("✅  Completadas",      "✅  Completed") }
            };

            foreach (Button cb in catButtons)
            {
                string key = (string)cb.Tag;
                if (catLabels.ContainsKey(key)) cb.Text = catLabels[key];
            }

            if (listView != null && listView.Columns.Count >= 7)
            {
                listView.Columns[0].Text = Tr("Archivo / Título (Doble clic abre carpeta)", "File / Title (Double-click opens folder)");
                listView.Columns[1].Text = Tr("Formato / Calidad", "Format / Quality");
                listView.Columns[2].Text = Tr("Tamaño", "Size");
                listView.Columns[3].Text = Tr("Estado", "Status");
                listView.Columns[4].Text = Tr("Progreso", "Progress");
                listView.Columns[5].Text = Tr("Velocidad", "Speed");
                listView.Columns[6].Text = Tr("Hora", "Time");
                listView.Invalidate();
            }

            if (lblStatus != null)
            {
                lblStatus.Text = Tr(
                    "🟢 Fold FAST v6.0 activo en segundo plano • Doble clic en cualquier archivo descargado abre su carpeta.",
                    "🟢 Fold FAST v6.0 active in background • Double-click any downloaded file to open its folder."
                );
            }
        }

        private void ApplyTheme()
        {
            this.BackColor = ThemeBgMain;
            this.ForeColor = ThemeTextMain;

            if (mainMenu != null)
            {
                mainMenu.BackColor = IsDarkMode ? Color.FromArgb(32, 32, 38) : Color.FromArgb(226, 232, 240);
                mainMenu.ForeColor = ThemeTextMain;
                foreach (ToolStripMenuItem topItem in mainMenu.Items)
                {
                    topItem.ForeColor = ThemeTextMain;
                }
            }

            if (header != null) header.BackColor = ThemeBgSurface;
            if (lblBrand != null) lblBrand.ForeColor = ThemeTextMain;
            if (lblSubBrand != null) lblSubBrand.ForeColor = ThemeTextMuted;
            if (lblLim != null) lblLim.ForeColor = ThemeTextMuted;

            if (numSpeedLimit != null)
            {
                numSpeedLimit.BackColor = ThemeBgCard;
                numSpeedLimit.ForeColor = ThemeTextMain;
            }

            if (btnLangToggle != null)
            {
                btnLangToggle.BackColor = BlueStart;
                btnLangToggle.ForeColor = Color.White;
            }

            if (btnThemeToggle != null)
            {
                btnThemeToggle.BackColor = ThemeBgCard;
                btnThemeToggle.ForeColor = ThemeTextMain;
            }

            if (btnOpenFolderTop != null)
            {
                btnOpenFolderTop.BackColor = ThemeBgCard;
                btnOpenFolderTop.ForeColor = ThemeTextMain;
            }

            if (cardsRow != null) cardsRow.BackColor = ThemeBgMain;
            if (cardManage != null)
            {
                cardManage.ColorStart = IsDarkMode ? Color.FromArgb(40, 40, 46) : Color.FromArgb(226, 232, 240);
                cardManage.ColorEnd = IsDarkMode ? Color.FromArgb(32, 32, 37) : Color.FromArgb(203, 213, 225);
                cardManage.TextColor = ThemeTextMain;
                cardManage.SubTextColor = ThemeTextMuted;
                cardManage.BadgeBgColor = IsDarkMode ? Color.FromArgb(63, 63, 70) : Color.FromArgb(148, 163, 184);
                cardManage.IconBoxColor = IsDarkMode ? Color.FromArgb(63, 63, 70) : Color.FromArgb(51, 65, 85);
                cardManage.Invalidate();
            }

            if (bodyPanel != null) bodyPanel.BackColor = ThemeBgMain;
            if (leftSidebar != null) leftSidebar.BackColor = ThemeBgSurface;
            if (catPanel != null) catPanel.BackColor = ThemeBgSurface;
            if (chkClipboard != null) chkClipboard.ForeColor = ThemeTextMain;

            foreach (Button cb in catButtons)
            {
                bool active = ((string)cb.Tag == selectedCategory);
                cb.BackColor = active ? BlueStart : ThemeBgCard;
                cb.ForeColor = active ? Color.White : ThemeTextMain;
            }

            if (rightContainer != null) rightContainer.BackColor = ThemeBgMain;
            if (listView != null)
            {
                listView.BackColor = ThemeBgSurface;
                listView.ForeColor = ThemeTextMain;
                listView.Invalidate();
            }

            if (statusPanel != null) statusPanel.BackColor = ThemeBgSurface;
            if (lblStatus != null) lblStatus.ForeColor = ThemeTextMuted;
            if (lblTotalSpeed != null) lblTotalSpeed.ForeColor = IsDarkMode ? LimeStart : BlueStart;
        }

        private void BuildModernUI()
        {
            // 0. BARRA DE MENÚ SUPERIOR SIEMPRE VISIBLE (Tema Oscuro/Claro + Idioma Español/Inglés + Inicio con Windows)
            mainMenu = new MenuStrip
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(8, 4, 8, 4)
            };

            menuFile = new ToolStripMenuItem("📁 Archivo");
            menuItemNewUrl = new ToolStripMenuItem("➕ Añadir Enlace Directo...", null, (s, e) => PromptAddUrl(""));
            menuItemNewMedia = new ToolStripMenuItem("🎬 Descargar Video o Audio...", null, (s, e) => PromptMediaDownload(""));
            menuItemOpenFolder = new ToolStripMenuItem("📂 Abrir Carpeta de Descargas", null, (s, e) => { try { Process.Start("explorer.exe", baseDir); } catch { } });
            menuItemStartup = new ToolStripMenuItem("🚀 Iniciar junto con Windows", null, (s, e) =>
            {
                SetWindowsStartup(!RunOnStartup);
                SaveSettings();
            });
            menuItemExit = new ToolStripMenuItem("❌ Salir completamente", null, (s, e) =>
            {
                allowRealExit = true;
                if (trayIcon != null) trayIcon.Visible = false;
                Application.Exit();
            });
            menuFile.DropDownItems.AddRange(new ToolStripItem[] {
                menuItemNewUrl, menuItemNewMedia, menuItemOpenFolder,
                new ToolStripSeparator(), menuItemStartup,
                new ToolStripSeparator(), menuItemExit
            });

            menuTheme = new ToolStripMenuItem("🎨 Tema (Modo Oscuro / Claro)");
            menuItemDark = new ToolStripMenuItem("🌙 Modo Oscuro (Dark)", null, (s, e) => ToggleTheme(true));
            menuItemLight = new ToolStripMenuItem("☀️ Modo Claro (Light)", null, (s, e) => ToggleTheme(false));
            menuTheme.DropDownItems.AddRange(new ToolStripItem[] { menuItemDark, menuItemLight });

            menuLang = new ToolStripMenuItem("🌐 Idioma / Language");
            menuItemEs = new ToolStripMenuItem("🇪🇸 Español", null, (s, e) => ToggleLanguage("es"));
            menuItemEn = new ToolStripMenuItem("🇺🇸 English", null, (s, e) => ToggleLanguage("en"));
            menuLang.DropDownItems.AddRange(new ToolStripItem[] { menuItemEs, menuItemEn });

            mainMenu.Items.AddRange(new ToolStripItem[] { menuFile, menuTheme, menuLang });
            this.MainMenuStrip = mainMenu;

            // 1. Cabecera con FlowLayoutPanel derecho adaptable a cualquier pantalla/zoom
            header = new Panel { Dock = DockStyle.Top, Height = 62, Padding = new Padding(16, 10, 16, 10) };

            PictureBox picLogo = new PictureBox
            {
                Size = new Size(40, 40),
                Location = new Point(18, 11),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            if (brandLogoImg != null) picLogo.Image = brandLogoImg;

            lblBrand = new Label
            {
                Text = "Fold FAST",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(66, 9)
            };
            lblSubBrand = new Label
            {
                Font = new Font("Segoe UI", 8.5F),
                AutoSize = true,
                Location = new Point(68, 36)
            };

            FlowLayoutPanel rightControls = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 0),
                BackColor = Color.Transparent
            };

            btnOpenFolderTop = new Button
            {
                Text = "📂 Carpeta",
                Size = new Size(92, 32),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.8F, FontStyle.Bold),
                Margin = new Padding(6, 0, 0, 0)
            };
            btnOpenFolderTop.FlatAppearance.BorderSize = 0;
            btnOpenFolderTop.Click += (s, e) => { try { Process.Start("explorer.exe", baseDir); } catch { } };

            btnThemeToggle = new Button
            {
                Text = "☀️ Modo Claro",
                Size = new Size(116, 32),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.8F, FontStyle.Bold),
                Margin = new Padding(6, 0, 0, 0)
            };
            btnThemeToggle.FlatAppearance.BorderSize = 0;
            btnThemeToggle.Click += (s, e) => ToggleTheme();

            btnLangToggle = new Button
            {
                Text = "🌐 Español ▾",
                Size = new Size(105, 32),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.8F, FontStyle.Bold),
                Margin = new Padding(6, 0, 0, 0)
            };
            btnLangToggle.FlatAppearance.BorderSize = 0;
            btnLangToggle.Click += (s, e) => ToggleLanguage();

            numSpeedLimit = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 1000000,
                Value = 0,
                Width = 72,
                Margin = new Padding(4, 5, 0, 0),
                BorderStyle = BorderStyle.FixedSingle
            };
            numSpeedLimit.ValueChanged += (s, e) => { speedLimitKbps = (int)numSpeedLimit.Value; };

            lblLim = new Label
            {
                Text = "Límite KB/s:",
                AutoSize = true,
                Margin = new Padding(6, 8, 0, 0)
            };

            rightControls.Controls.AddRange(new Control[] { btnOpenFolderTop, btnThemeToggle, btnLangToggle, numSpeedLimit, lblLim });
            header.Controls.Add(rightControls);
            header.Controls.Add(picLogo);
            header.Controls.Add(lblBrand);
            header.Controls.Add(lblSubBrand);

            cardsRow = new Panel { Dock = DockStyle.Top, Height = 122, Padding = new Padding(16, 12, 16, 8) };

            cardMedia = new GradientCardButton
            {
                ColorStart = CoralStart,
                ColorEnd = CoralEnd,
                TextColor = Color.White,
                SubTextColor = Color.FromArgb(255, 228, 232),
                BadgeBgColor = Color.FromArgb(75, 255, 255, 255),
                IconBoxColor = Color.FromArgb(55, 0, 0, 0),
                IconGlyphColor = Color.White,
                IconSymbol = "⚡",
                Size = new Size(255, 100),
                Location = new Point(16, 12)
            };
            cardMedia.Click += (s, e) => PromptMediaDownload("");

            cardDirect = new GradientCardButton
            {
                ColorStart = LimeStart,
                ColorEnd = LimeEnd,
                TextColor = Color.FromArgb(12, 20, 8),
                SubTextColor = Color.FromArgb(28, 45, 20),
                BadgeBgColor = Color.FromArgb(45, 0, 0, 0),
                IconBoxColor = Color.FromArgb(24, 34, 18),
                IconGlyphColor = LimeStart,
                IconSymbol = "✨",
                Size = new Size(255, 100),
                Location = new Point(285, 12)
            };
            cardDirect.Click += (s, e) => PromptAddUrl("");

            cardBatch = new GradientCardButton
            {
                ColorStart = BlueStart,
                ColorEnd = BlueEnd,
                TextColor = Color.White,
                SubTextColor = Color.FromArgb(219, 234, 254),
                BadgeBgColor = Color.FromArgb(65, 255, 255, 255),
                IconBoxColor = Color.White,
                IconGlyphColor = BlueStart,
                IconSymbol = "📦",
                Size = new Size(255, 100),
                Location = new Point(554, 12)
            };
            cardBatch.Click += (s, e) => PromptBatchAdd();

            cardManage = new GradientCardButton
            {
                ColorStart = Color.FromArgb(40, 40, 46),
                ColorEnd = Color.FromArgb(32, 32, 37),
                TextColor = Color.White,
                SubTextColor = Color.FromArgb(161, 161, 170),
                BadgeBgColor = Color.FromArgb(63, 63, 70),
                IconBoxColor = Color.FromArgb(63, 63, 70),
                IconGlyphColor = Color.White,
                IconSymbol = "⚙",
                Size = new Size(255, 100),
                Location = new Point(823, 12)
            };
            cardManage.Click += (s, e) => ShowTaskActionMenu(cardManage);

            cardsRow.Controls.AddRange(new Control[] { cardMedia, cardDirect, cardBatch, cardManage });

            statusPanel = new Panel { Dock = DockStyle.Bottom, Height = 34, Padding = new Padding(16, 7, 16, 7) };
            lblStatus = new Label
            {
                Dock = DockStyle.Left,
                AutoSize = true
            };
            lblTotalSpeed = new Label
            {
                Text = "⚡ 0 KB/s",
                Dock = DockStyle.Right,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            statusPanel.Controls.Add(lblStatus);
            statusPanel.Controls.Add(lblTotalSpeed);

            bodyPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 4, 16, 10) };

            leftSidebar = new Panel { Dock = DockStyle.Left, Width = 195, Padding = new Padding(10) };
            catPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            string[] catKeys = { "Todas", "Videos", "Musica", "Comprimidos", "Programas", "Documentos", "En curso", "Completado" };
            foreach (string key in catKeys)
            {
                Button b = new Button
                {
                    Tag = key,
                    Width = 172,
                    Height = 34,
                    TextAlign = ContentAlignment.MiddleLeft,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0, 0, 0, 6)
                };
                b.FlatAppearance.BorderSize = 0;
                b.Click += (s, e) =>
                {
                    selectedCategory = (string)((Button)s).Tag;
                    ApplyTheme();
                    RefreshListView();
                };
                catButtons.Add(b);
                catPanel.Controls.Add(b);
            }

            chkClipboard = new CheckBox
            {
                Dock = DockStyle.Bottom,
                Checked = true,
                Height = 28
            };

            leftSidebar.Controls.Add(catPanel);
            leftSidebar.Controls.Add(chkClipboard);

            rightContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 0, 0) };

            listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                MultiSelect = true,
                BorderStyle = BorderStyle.None,
                OwnerDraw = true
            };
            listView.Columns.Add("Archivo / Título", 275);
            listView.Columns.Add("Formato / Calidad", 125, HorizontalAlignment.Center);
            listView.Columns.Add("Tamaño", 95, HorizontalAlignment.Right);
            listView.Columns.Add("Estado", 135, HorizontalAlignment.Center);
            listView.Columns.Add("Progreso", 130, HorizontalAlignment.Center);
            listView.Columns.Add("Velocidad", 95, HorizontalAlignment.Right);
            listView.Columns.Add("Hora", 65, HorizontalAlignment.Center);

            ImageList rowHeightList = new ImageList { ImageSize = new Size(1, 36) };
            listView.SmallImageList = rowHeightList;

            listView.DrawColumnHeader += ListView_DrawColumnHeader;
            listView.DrawItem += (s, e) => { };
            listView.DrawSubItem += ListView_DrawSubItem;
            listView.DoubleClick += (s, e) => OpenOrVerifySelectedFile();
            listView.MouseDoubleClick += (s, e) => OpenOrVerifySelectedFile();

            rightContainer.Controls.Add(listView);
            bodyPanel.Controls.Add(rightContainer);
            bodyPanel.Controls.Add(leftSidebar);

            this.Controls.Add(bodyPanel);
            this.Controls.Add(cardsRow);
            this.Controls.Add(header);
            this.Controls.Add(mainMenu);
            this.Controls.Add(statusPanel);
        }

        private string TranslateStatus(string st)
        {
            if (CurrentLang != "en") return st;
            if (st == "En cola") return "Queued";
            if (st == "Conectando" || st.StartsWith("Conectando")) return "Connecting...";
            if (st == "Descargando") return "Downloading";
            if (st == "Pausado") return "Paused";
            if (st == "Combinando" || st.StartsWith("Uniendo")) return "Merging HD...";
            if (st == "Instalando motor...") return "Installing engine...";
            if (st == "Instalando fusionador HD...") return "Installing HD merger...";
            if (st == "Completado") return "Completed";
            return st;
        }

        private void ListView_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            Color hdrBg = IsDarkMode ? Color.FromArgb(32, 32, 38) : Color.FromArgb(226, 232, 240);
            using (SolidBrush bg = new SolidBrush(hdrBg))
            using (SolidBrush fg = new SolidBrush(ThemeTextMuted))
            using (Font f = new Font("Segoe UI", 8.8F, FontStyle.Bold))
            {
                e.Graphics.FillRectangle(bg, e.Bounds);
                StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center };
                if (e.Header.TextAlign == HorizontalAlignment.Center) sf.Alignment = StringAlignment.Center;
                else if (e.Header.TextAlign == HorizontalAlignment.Right) sf.Alignment = StringAlignment.Far;
                Rectangle r = new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height);
                e.Graphics.DrawString(e.Header.Text, f, fg, r, sf);
            }
        }

        private void ListView_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            bool selected = e.Item.Selected;

            Color selColor = IsDarkMode ? Color.FromArgb(45, 55, 78) : Color.FromArgb(219, 234, 254);
            Color rowBg = selected ? selColor : (e.ItemIndex % 2 == 0 ? ThemeBgSurface : ThemeBgAltRow);
            using (SolidBrush bg = new SolidBrush(rowBg))
            {
                g.FillRectangle(bg, e.Bounds);
            }

            DownloadTask t = e.Item.Tag as DownloadTask;
            if (e.ColumnIndex == 4 && t != null)
            {
                Rectangle barBgRect = new Rectangle(e.Bounds.X + 8, e.Bounds.Y + 9, e.Bounds.Width - 16, 18);
                Color trackColor = IsDarkMode ? Color.FromArgb(52, 52, 60) : Color.FromArgb(203, 213, 225);
                using (GraphicsPath bp = GetRoundRect(barBgRect, 8))
                using (SolidBrush bbg = new SolidBrush(trackColor))
                {
                    g.FillPath(bbg, bp);
                }

                double pct = t.ProgressPercent;
                int fillW = (int)((barBgRect.Width * pct) / 100.0);
                if (fillW > 4)
                {
                    Rectangle fillRect = new Rectangle(barBgRect.X, barBgRect.Y, Math.Max(8, fillW), barBgRect.Height);
                    Color c1 = t.Status == "Completado" ? LimeStart : (t.MediaType == "video" ? CoralStart : BlueStart);
                    Color c2 = t.Status == "Completado" ? LimeEnd : (t.MediaType == "video" ? CoralEnd : BlueEnd);
                    using (GraphicsPath fp = GetRoundRect(fillRect, 8))
                    using (LinearGradientBrush lb = new LinearGradientBrush(fillRect, c1, c2, LinearGradientMode.Horizontal))
                    {
                        g.FillPath(lb, fp);
                    }
                }

                string pctTxt = pct.ToString("0.0") + "%";
                using (Font pf = new Font("Segoe UI", 8F, FontStyle.Bold))
                using (SolidBrush ptb = new SolidBrush(pct > 55 && t.Status == "Completado" ? Color.Black : (IsDarkMode ? Color.White : Color.FromArgb(15, 23, 42))))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(pctTxt, pf, ptb, barBgRect, sf);
                }
                return;
            }

            Color txtColor = ThemeTextMain;
            if (e.ColumnIndex == 1) txtColor = IsDarkMode ? Color.FromArgb(56, 189, 248) : BlueStart;
            else if (e.ColumnIndex == 3 && t != null)
            {
                if (t.Status == "Completado") txtColor = IsDarkMode ? LimeStart : Color.FromArgb(22, 163, 74);
                else if (t.Status.StartsWith("Error")) txtColor = CoralStart;
                else if (t.Status == "Descargando" || t.Status.StartsWith("Uniendo")) txtColor = IsDarkMode ? Color.FromArgb(96, 165, 250) : BlueStart;
            }

            using (SolidBrush fg = new SolidBrush(txtColor))
            using (Font f = new Font("Segoe UI", e.ColumnIndex == 0 ? 9.2F : 8.8F, e.ColumnIndex == 0 ? FontStyle.Bold : FontStyle.Regular))
            {
                StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                if (listView.Columns[e.ColumnIndex].TextAlign == HorizontalAlignment.Center) sf.Alignment = StringAlignment.Center;
                else if (listView.Columns[e.ColumnIndex].TextAlign == HorizontalAlignment.Right) sf.Alignment = StringAlignment.Far;
                Rectangle r = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 16, e.Bounds.Height);
                g.DrawString(e.SubItem.Text, f, fg, r, sf);
            }
        }

        private void ShowTaskActionMenu(Control anchor)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add(Tr("📂 Abrir Carpeta de Seleccionada", "📂 Open Folder of Selected"), null, (s, e) => OpenOrVerifySelectedFile());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Tr("▶ Reanudar Seleccionadas", "▶ Resume Selected"), null, (s, e) => ResumeSelected());
            menu.Items.Add(Tr("⏸ Pausar Seleccionadas", "⏸ Pause Selected"), null, (s, e) => PauseSelected());
            menu.Items.Add(Tr("⏹ Pausar Todas", "⏹ Pause All"), null, (s, e) => PauseAll());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Tr("🗑 Eliminar Seleccionadas", "🗑 Delete Selected"), null, (s, e) => DeleteSelected());
            menu.Items.Add(Tr("✨ Limpiar Completadas", "✨ Clear Completed"), null, (s, e) =>
            {
                lock (tasks) { tasks.RemoveAll(t => t.Status == "Completado"); }
                RefreshListView();
            });
            menu.Show(anchor, new Point(0, anchor.Height));
        }

        public static string DetectCategory(string fileName, string mediaType)
        {
            if (mediaType == "audio") return "Musica";
            if (mediaType == "video") return "Videos";
            try
            {
                int dot = (fileName ?? "").LastIndexOf('.');
                string ext = dot >= 0 ? fileName.Substring(dot).ToLowerInvariant() : "";
                foreach (var kv in Categories)
                {
                    foreach (string e in kv.Value)
                    {
                        if (ext == e) return kv.Key;
                    }
                }
            }
            catch { }
            return "General";
        }

        public static bool IsStreamingSiteOrBlob(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            string u = url.ToLowerInvariant();
            if (u.StartsWith("blob:")) return true;
            string[] sites = {
                "youtube.com/", "youtu.be/", "googlevideo.com/", "tiktok.com/", "tiktokcdn",
                "facebook.com/", "fb.watch/", "fb.com/", "fbcdn.net/", "instagram.com/", "cdninstagram.com/",
                "twitter.com/", "x.com/", "twimg.com/", "vimeo.com/", "vimeocdn.com/",
                "soundcloud.com/", "twitch.tv/", "dailymotion.com/", "reddit.com/", "pinterest.com/",
                "bilibili.com/", "threads.net/", ".m3u8", ".mpd"
            };
            foreach (string s in sites)
            {
                if (u.Contains(s)) return true;
            }
            return false;
        }

        private void PromptMediaDownload(string prefillUrl)
        {
            Form dlg = new Form
            {
                Text = Tr("Descargar Video o Audio (1 Solo Archivo)", "Download Video or Audio (Single File)"),
                Size = new Size(480, 230),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = ThemeBgSurface,
                ForeColor = ThemeTextMain
            };

            Label l1 = new Label { Text = Tr("Enlace del Video o Red Social (Facebook, YouTube, TikTok, etc.):", "Video or Social Media Link (Facebook, YouTube, TikTok, etc.):"), Location = new Point(18, 16), AutoSize = true };
            TextBox txtUrl = new TextBox { Text = prefillUrl, Location = new Point(18, 40), Width = 425, BackColor = ThemeBgCard, ForeColor = ThemeTextMain, BorderStyle = BorderStyle.FixedSingle };

            Label lPreset = new Label { Text = Tr("Elige Formato y Calidad:", "Choose Format & Quality:"), Location = new Point(18, 78), AutoSize = true };
            ComboBox cmbPreset = new ComboBox
            {
                Location = new Point(18, 102),
                Width = 425,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ThemeBgCard,
                ForeColor = ThemeTextMain
            };
            cmbPreset.Items.AddRange(new object[] {
                "🎬 MP4 • 1080p Full HD",
                "🎬 MP4 • 720p HD",
                "🎬 MP4 • 480p SD",
                "🎬 MP4 • 360p",
                "🎬 MKV • Auto",
                "🎵 MP3 • 320 kbps",
                "🎵 MP3 • 128 kbps",
                "🎵 M4A • Audio",
                "🎵 WAV • Audio"
            });
            cmbPreset.SelectedIndex = 0;

            Button btnGo = new Button
            {
                Text = Tr("⚡ Descargar Ahora", "⚡ Download Now"),
                Location = new Point(18, 145),
                Size = new Size(200, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = CoralStart,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGo.FlatAppearance.BorderSize = 0;
            btnGo.Click += (s, e) =>
            {
                string u = txtUrl.Text.Trim();
                if (u.StartsWith("http://") || u.StartsWith("https://"))
                {
                    string[,] presets = {
                        { "video", "mp4", "1080" },
                        { "video", "mp4", "720"  },
                        { "video", "mp4", "480"  },
                        { "video", "mp4", "360"  },
                        { "video", "mkv", "best" },
                        { "audio", "mp3", "320k" },
                        { "audio", "mp3", "128k" },
                        { "audio", "m4a", "320k" },
                        { "audio", "wav", "best" }
                    };
                    int idx = Math.Max(0, cmbPreset.SelectedIndex);
                    AddAndStartDownload(u, u, "", "", presets[idx, 0], presets[idx, 1], presets[idx, 2], "", "", 8);
                    dlg.Close();
                }
            };

            dlg.Controls.AddRange(new Control[] { l1, txtUrl, lPreset, cmbPreset, btnGo });
            dlg.ShowDialog(this);
        }

        private void PromptAddUrl(string prefill)
        {
            Form dlg = new Form
            {
                Text = Tr("Añadir Descarga Directa Multihilo", "Add Multi-Threaded Direct Download"),
                Size = new Size(520, 230),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = ThemeBgSurface,
                ForeColor = ThemeTextMain
            };

            Label l1 = new Label { Text = Tr("Dirección URL del archivo:", "File URL:"), Location = new Point(16, 16), AutoSize = true };
            TextBox txtUrl = new TextBox { Text = prefill, Location = new Point(16, 38), Width = 470, BackColor = ThemeBgCard, ForeColor = ThemeTextMain, BorderStyle = BorderStyle.FixedSingle };
            Label l2 = new Label { Text = Tr("Nombre personalizado (opcional):", "Custom filename (optional):"), Location = new Point(16, 76), AutoSize = true };
            TextBox txtName = new TextBox { Location = new Point(210, 72), Width = 276, BackColor = ThemeBgCard, ForeColor = ThemeTextMain, BorderStyle = BorderStyle.FixedSingle };
            Label l3 = new Label { Text = Tr("Conexiones simultáneas (Hilos):", "Simultaneous Connections (Threads):"), Location = new Point(16, 112), AutoSize = true };
            NumericUpDown numConn = new NumericUpDown { Minimum = 1, Maximum = 32, Value = 8, Location = new Point(230, 108), Width = 70, BackColor = ThemeBgCard, ForeColor = ThemeTextMain };

            Button btnOk = new Button
            {
                Text = Tr("⚡ Descargar en 8 Hilos", "⚡ Download (8 Threads)"),
                Location = new Point(16, 148),
                Size = new Size(185, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = BlueStart,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnOk.FlatAppearance.BorderSize = 0;

            btnOk.Click += (s, e) =>
            {
                string u = txtUrl.Text.Trim();
                if (u.StartsWith("http://") || u.StartsWith("https://"))
                {
                    AddAndStartDownload(u, "", "", txtName.Text.Trim(), "file", "", "Directo", "", "", (int)numConn.Value);
                    dlg.Close();
                }
            };

            dlg.Controls.AddRange(new Control[] { l1, txtUrl, l2, txtName, l3, numConn, btnOk });
            dlg.ShowDialog(this);
        }

        private void PromptBatchAdd()
        {
            Form dlg = new Form
            {
                Text = Tr("Descarga por Lotes [01-10]", "Batch Downloader [01-10]"),
                Size = new Size(540, 320),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = ThemeBgSurface,
                ForeColor = ThemeTextMain
            };
            Label l1 = new Label { Text = Tr("Pega varios enlaces (uno por línea) o usa comodines como https://sitio.com/parte_[01-05].zip:", "Paste multiple links (one per line) or use wildcards like https://site.com/part_[01-05].zip:"), Location = new Point(14, 12), AutoSize = true };
            TextBox txtBatch = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Location = new Point(14, 38), Size = new Size(495, 190), BackColor = ThemeBgCard, ForeColor = ThemeTextMain };
            Button btnGo = new Button { Text = Tr("🚀 Añadir e Iniciar Todos", "🚀 Add & Start All"), Location = new Point(14, 238), Size = new Size(190, 34), FlatStyle = FlatStyle.Flat, BackColor = BlueStart, ForeColor = Color.White };
            btnGo.FlatAppearance.BorderSize = 0;
            btnGo.Click += (s, e) =>
            {
                string[] lines = txtBatch.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string raw in lines)
                {
                    string line = raw.Trim();
                    Match m = Regex.Match(line, @"\[(\d+)-(\d+)\]");
                    if (m.Success)
                    {
                        int start = int.Parse(m.Groups[1].Value);
                        int end = int.Parse(m.Groups[2].Value);
                        int pad = m.Groups[1].Value.StartsWith("0") ? m.Groups[1].Value.Length : 0;
                        for (int i = start; i <= end && (i - start) <= 200; i++)
                        {
                            string numStr = pad > 0 ? i.ToString().PadLeft(pad, '0') : i.ToString();
                            string expanded = line.Substring(0, m.Index) + numStr + line.Substring(m.Index + m.Length);
                            AddAndStartDownload(expanded, "", "", "", "file", "", "Lote", "", "", 8);
                        }
                    }
                    else if (line.StartsWith("http"))
                    {
                        AddAndStartDownload(line, "", "", "", "file", "", "Lote", "", "", 8);
                    }
                }
                dlg.Close();
            };
            dlg.Controls.AddRange(new Control[] { l1, txtBatch, btnGo });
            dlg.ShowDialog(this);
        }

        public void AddAndStartDownload(string url, string pageUrl, string fallbackStreamUrl, string customTitle, string mediaType, string formatExt, string quality, string referer, string userAgent, int connections)
        {
            try
            {
                bool useYtDlp = false;
                string effectiveUrl = (url ?? "").Trim();
                pageUrl = (pageUrl ?? "").Trim();
                fallbackStreamUrl = (fallbackStreamUrl ?? "").Trim();

                if (mediaType != "file")
                {
                    if (!string.IsNullOrEmpty(pageUrl) && IsStreamingSiteOrBlob(pageUrl))
                    {
                        effectiveUrl = pageUrl;
                        useYtDlp = true;
                    }
                    else if (string.IsNullOrEmpty(effectiveUrl) || IsStreamingSiteOrBlob(effectiveUrl))
                    {
                        effectiveUrl = !string.IsNullOrEmpty(pageUrl) ? pageUrl : effectiveUrl;
                        useYtDlp = true;
                    }
                    else if (mediaType == "video" || mediaType == "audio")
                    {
                        if (!string.IsNullOrEmpty(pageUrl) && pageUrl.StartsWith("http"))
                        {
                            useYtDlp = true;
                        }
                    }
                }

                string fname = GuessFileName(effectiveUrl, customTitle, mediaType, formatExt);
                if (string.IsNullOrEmpty(formatExt))
                {
                    int dot = fname.LastIndexOf('.');
                    formatExt = dot >= 0 ? fname.Substring(dot + 1).ToLowerInvariant() : "bin";
                }
                formatExt = Regex.Replace(formatExt.ToLowerInvariant(), @"[^a-z0-9]", "");
                if (string.IsNullOrEmpty(formatExt)) formatExt = "bin";

                if (string.IsNullOrEmpty(quality))
                {
                    quality = mediaType == "audio" ? "320k" : (mediaType == "video" ? "1080" : "Directo");
                }

                string cat = DetectCategory(fname, mediaType);
                string catDir = Path.Combine(baseDir, cat);
                Directory.CreateDirectory(catDir);

                DownloadTask task = new DownloadTask
                {
                    Url = effectiveUrl,
                    PageUrl = pageUrl,
                    FallbackStreamUrl = fallbackStreamUrl,
                    Referer = !string.IsNullOrEmpty(referer) ? referer : pageUrl,
                    MediaType = mediaType,
                    FormatExt = formatExt,
                    Quality = quality,
                    UseYtDlp = useYtDlp,
                    FileName = fname,
                    Category = cat,
                    SaveDir = catDir,
                    Connections = connections,
                    Status = "Descargando",
                    ManualProgress = 2.0
                };
                if (!string.IsNullOrEmpty(userAgent)) task.UserAgent = userAgent;

                lock (tasks) { tasks.Insert(0, task); }
                RefreshListView();
                lblStatus.Text = "🚀 " + Tr("Descarga iniciada al instante: ", "Download started immediately: ") + task.FileName;

                if (useYtDlp)
                {
                    StartYtDlpTaskAsync(task);
                }
                else
                {
                    StartTaskAsync(task);
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Info: " + ex.Message;
            }
        }

        private bool EnsureMediaBinaries(DownloadTask task, bool waitFfmpeg = false)
        {
            lock (binaryLock)
            {
                if (!File.Exists(ytDlpPath) || new FileInfo(ytDlpPath).Length < 1000000)
                {
                    try
                    {
                        task.Status = "Instalando motor...";
                        string dlUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
                        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(dlUrl);
                        req.UserAgent = task.UserAgent;
                        req.Timeout = 60000;
                        using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                        using (Stream s = resp.GetResponseStream())
                        using (FileStream fs = new FileStream(ytDlpPath, FileMode.Create, FileAccess.Write))
                        {
                            s.CopyTo(fs);
                        }
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            // Descargar ffmpeg en paralelo sin bloquear el inicio inmediato si aún no está instalado
            if (!File.Exists(ffmpegPath) || new FileInfo(ffmpegPath).Length < 1000000)
            {
                Action dlFfmpeg = () =>
                {
                    lock (binaryLock)
                    {
                        if (File.Exists(ffmpegPath) && new FileInfo(ffmpegPath).Length > 1000000) return;
                        try
                        {
                            string ffZipUrl = "https://github.com/yt-dlp/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";
                            string tempZip = Path.Combine(binDir, "ffmpeg_temp.zip");
                            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(ffZipUrl);
                            req.UserAgent = task.UserAgent;
                            req.Timeout = 120000;
                            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                            using (Stream s = resp.GetResponseStream())
                            using (FileStream fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write))
                            {
                                s.CopyTo(fs);
                            }

                            using (ZipArchive archive = ZipFile.OpenRead(tempZip))
                            {
                                foreach (ZipArchiveEntry entry in archive.Entries)
                                {
                                    if (entry.Name.Equals("ffmpeg.exe", StringComparison.OrdinalIgnoreCase) ||
                                        entry.Name.Equals("ffprobe.exe", StringComparison.OrdinalIgnoreCase))
                                    {
                                        string destFile = Path.Combine(binDir, entry.Name.ToLowerInvariant());
                                        using (Stream es = entry.Open())
                                        using (FileStream ofs = new FileStream(destFile, FileMode.Create, FileAccess.Write))
                                        {
                                            es.CopyTo(ofs);
                                        }
                                    }
                                }
                            }
                            try { File.Delete(tempZip); } catch { }
                        }
                        catch { }
                    }
                };
                if (waitFfmpeg) dlFfmpeg();
                else Task.Run(dlFfmpeg);
            }

            return File.Exists(ytDlpPath);
        }

        private bool RunYtDlpProcess(DownloadTask task, string formatArgs, string outTemplate, string targetUrl)
        {
            if (string.IsNullOrEmpty(targetUrl) || targetUrl.StartsWith("blob:")) return false;

            bool hasFfmpeg = File.Exists(ffmpegPath) && new FileInfo(ffmpegPath).Length > 1000000;
            // Flags de aceleración rápida (--concurrent-fragments 16 --http-chunk-size 10M --no-cache-dir) para arranque rápido en YouTube
            string args = "--newline --no-playlist --no-warnings --no-check-certificates --no-cache-dir --concurrent-fragments 16 --http-chunk-size 10M ";
            if (!string.IsNullOrEmpty(task.UserAgent))
            {
                args += "--user-agent \"" + task.UserAgent.Replace("\"", "") + "\" ";
            }
            if (!string.IsNullOrEmpty(task.Referer) && task.Referer.StartsWith("http"))
            {
                args += "--referer \"" + task.Referer.Replace("\"", "") + "\" ";
            }
            if (hasFfmpeg)
            {
                args += "--ffmpeg-location \"" + binDir + "\" ";
            }
            if (speedLimitKbps > 0)
            {
                args += "-r " + speedLimitKbps + "K ";
            }
            args += formatArgs + " -o \"" + outTemplate + "\" \"" + targetUrl + "\"";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            Process proc = new Process { StartInfo = psi };
            task.ExternalProcess = proc;

            DataReceivedEventHandler onLine = (s, ev) =>
            {
                if (string.IsNullOrEmpty(ev.Data)) return;
                if (ev.Data.Contains("[Merger]") || ev.Data.Contains("Merging formats"))
                {
                    task.Status = "Uniendo video+audio...";
                }
                Match mPct = Regex.Match(ev.Data, @"(\d+(?:\.\d+)?)%");
                if (mPct.Success)
                {
                    double p;
                    if (double.TryParse(mPct.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out p))
                    {
                        if (p > task.ManualProgress) task.ManualProgress = p;
                    }
                }
                Match mSpeed = Regex.Match(ev.Data, @"at\s+(\d+(?:\.\d+)?)([KMG]i?B)/s");
                if (mSpeed.Success)
                {
                    double val;
                    if (double.TryParse(mSpeed.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out val))
                    {
                        string unit = mSpeed.Groups[2].Value.ToUpperInvariant();
                        if (unit.StartsWith("K")) task.SpeedBps = val * 1024.0;
                        else if (unit.StartsWith("M")) task.SpeedBps = val * 1024.0 * 1024.0;
                        else if (unit.StartsWith("G")) task.SpeedBps = val * 1024.0 * 1024.0 * 1024.0;
                    }
                }
            };

            proc.OutputDataReceived += onLine;
            proc.ErrorDataReceived += onLine;

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();
            task.ExternalProcess = null;

            return proc.ExitCode == 0;
        }

        private void CleanupSeparateStreamFiles(string dir, string baseName, string keepFullPath)
        {
            try
            {
                string[] all = Directory.GetFiles(dir);
                foreach (string f in all)
                {
                    string fn = Path.GetFileName(f);
                    if (!fn.StartsWith(baseName, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(f, keepFullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        if (Regex.IsMatch(fn, @"\.f\d+\.") || f.EndsWith(".part") || f.EndsWith(".ytdl") || f.EndsWith(".temp"))
                        {
                            try { File.Delete(f); } catch { }
                        }
                    }
                }
            }
            catch { }
        }

        private void StartYtDlpTaskAsync(DownloadTask task)
        {
            task.CancelRequested = false;
            task.Status = "Descargando";
            task.ManualProgress = 3.0;

            Task.Run(() =>
            {
                try
                {
                    if (!EnsureMediaBinaries(task, false))
                    {
                        task.Status = "Error motor video";
                        return;
                    }

                    bool hasFfmpeg = File.Exists(ffmpegPath) && new FileInfo(ffmpegPath).Length > 1000000;
                    task.Status = "Descargando";
                    task.ManualProgress = 5.0;

                    string safeFull = SafeSanitizeFileName(task.FileName, "." + task.FormatExt);
                    int dotIdx = safeFull.LastIndexOf('.');
                    string baseName = dotIdx > 0 ? safeFull.Substring(0, dotIdx) : safeFull;
                    baseName = baseName.Replace("%", "_");

                    CleanupSeparateStreamFiles(task.SaveDir, baseName, "");

                    string outTemplate = Path.Combine(task.SaveDir, baseName + ".%(ext)s");
                    string primaryFormatArgs;

                    if (task.MediaType == "audio")
                    {
                        if (hasFfmpeg)
                        {
                            string aFmt = (task.FormatExt == "wav" || task.FormatExt == "m4a") ? task.FormatExt : "mp3";
                            string aQual = task.Quality == "128k" ? "5" : "0";
                            primaryFormatArgs = "-f \"ba/b\" -x --audio-format " + aFmt + " --audio-quality " + aQual;
                        }
                        else
                        {
                            primaryFormatArgs = "-f \"ba[ext=m4a]/ba/b\"";
                        }
                    }
                    else
                    {
                        int maxH = 0;
                        int.TryParse(Regex.Replace(task.Quality ?? "", @"[^\d]", ""), out maxH);
                        string targetContainer = (task.FormatExt == "mkv" || task.FormatExt == "webm") ? task.FormatExt : "mp4";

                        if (hasFfmpeg)
                        {
                            if (maxH > 0)
                            {
                                primaryFormatArgs = "-f \"bv*[height<=" + maxH + "]+ba/b[height<=" + maxH + "]/b/best\" --merge-output-format " + targetContainer;
                            }
                            else
                            {
                                primaryFormatArgs = "-f \"bv*+ba/b/best\" --merge-output-format " + targetContainer;
                            }
                        }
                        else
                        {
                            if (maxH > 0)
                            {
                                primaryFormatArgs = "-f \"b[height<=" + maxH + "]/b/best\"";
                            }
                            else
                            {
                                primaryFormatArgs = "-f \"b/best\"";
                            }
                        }
                    }

                    bool ok = RunYtDlpProcess(task, primaryFormatArgs, outTemplate, task.Url);

                    if (!ok && !task.CancelRequested && !string.IsNullOrEmpty(task.FallbackStreamUrl) && task.FallbackStreamUrl.StartsWith("http"))
                    {
                        ok = RunYtDlpProcess(task, primaryFormatArgs, outTemplate, task.FallbackStreamUrl);
                    }

                    if (!ok && !task.CancelRequested)
                    {
                        string backupTarget = (!string.IsNullOrEmpty(task.FallbackStreamUrl) && task.FallbackStreamUrl.StartsWith("http"))
                            ? task.FallbackStreamUrl
                            : task.Url;
                        ok = RunYtDlpProcess(task, "-f \"b/best\"", outTemplate, backupTarget);
                    }

                    if (!ok && !task.CancelRequested && !string.IsNullOrEmpty(task.FallbackStreamUrl) && task.FallbackStreamUrl.StartsWith("http"))
                    {
                        task.Url = task.FallbackStreamUrl;
                        task.UseYtDlp = false;
                        StartTaskAsync(task);
                        return;
                    }

                    task.SpeedBps = 0;

                    if (task.CancelRequested)
                    {
                        task.Status = "Pausado";
                        return;
                    }

                    if (ok)
                    {
                        string[] allFiles = Directory.GetFiles(task.SaveDir);
                        FileInfo bestFile = null;
                        foreach (string m in allFiles)
                        {
                            string fn = Path.GetFileName(m);
                            if (!fn.StartsWith(baseName + ".", StringComparison.OrdinalIgnoreCase)) continue;
                            if (Regex.IsMatch(fn, @"\.f\d+\.") || m.EndsWith(".part") || m.EndsWith(".ytdl"))
                                continue;
                            FileInfo fi = new FileInfo(m);
                            if (bestFile == null || fi.Length > bestFile.Length)
                                bestFile = fi;
                        }

                        if (bestFile != null)
                        {
                            CleanupSeparateStreamFiles(task.SaveDir, baseName, bestFile.FullName);
                            task.FileName = bestFile.Name;
                            task.TotalSize = bestFile.Length;
                            task.DownloadedSize = bestFile.Length;
                        }
                        task.ManualProgress = 100.0;
                        task.Status = "Completado";
                    }
                    else
                    {
                        task.Status = "Error";
                    }
                }
                catch
                {
                    task.SpeedBps = 0;
                    task.Status = task.CancelRequested ? "Pausado" : "Error";
                }
            });
        }

        private void StartTaskAsync(DownloadTask task)
        {
            task.CancelRequested = false;
            task.Status = "Descargando";

            Task.Run(() =>
            {
                try
                {
                    Directory.CreateDirectory(task.SaveDir);
                    Directory.CreateDirectory(task.TempDir);

                    long contentLength = -1;
                    bool acceptRanges = false;
                    try
                    {
                        HttpWebRequest probe = (HttpWebRequest)WebRequest.Create(task.Url);
                        probe.UserAgent = task.UserAgent;
                        if (!string.IsNullOrEmpty(task.Referer)) probe.Referer = task.Referer;
                        probe.Method = "GET";
                        probe.AddRange(0, 0);
                        probe.Timeout = 12000;
                        using (HttpWebResponse resp = (HttpWebResponse)probe.GetResponse())
                        {
                            if (resp.StatusCode == HttpStatusCode.PartialContent)
                            {
                                acceptRanges = true;
                                string cr = resp.Headers["Content-Range"];
                                if (!string.IsNullOrEmpty(cr) && cr.Contains("/"))
                                {
                                    long.TryParse(cr.Split('/')[1].Trim(), out contentLength);
                                }
                            }
                            if (contentLength <= 0 && resp.ContentLength > 0)
                            {
                                contentLength = resp.ContentLength;
                            }
                        }
                    }
                    catch { }

                    if (task.CancelRequested) { task.Status = "Pausado"; return; }

                    task.TotalSize = contentLength > 0 ? contentLength : 0;
                    task.Resumable = acceptRanges && contentLength > 0;
                    int numParts = (task.Resumable && task.TotalSize > 65536) ? task.Connections : 1;

                    task.Status = "Descargando";
                    long partSize = numParts > 1 ? (task.TotalSize / numParts) : task.TotalSize;

                    List<Task> workers = new List<Task>();
                    bool anyError = false;

                    for (int i = 0; i < numParts; i++)
                    {
                        int idx = i;
                        long startByte = idx * partSize;
                        long endByte = (idx == numParts - 1) ? (task.TotalSize - 1) : (startByte + partSize - 1);

                        workers.Add(Task.Run(() =>
                        {
                            string partPath = Path.Combine(task.TempDir, "part_" + idx + ".tmp");
                            long existing = File.Exists(partPath) ? new FileInfo(partPath).Length : 0;
                            if (!task.Resumable) existing = 0;

                            long reqStart = startByte + existing;
                            if (task.Resumable && endByte >= 0 && reqStart > endByte) return;

                            try
                            {
                                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(task.Url);
                                req.UserAgent = task.UserAgent;
                                if (!string.IsNullOrEmpty(task.Referer)) req.Referer = task.Referer;
                                req.Timeout = 20000;
                                if (task.Resumable && endByte >= 0)
                                {
                                    req.AddRange(reqStart, endByte);
                                }

                                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                                using (Stream netStream = resp.GetResponseStream())
                                using (FileStream fs = new FileStream(partPath, existing > 0 ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    byte[] buffer = new byte[32768];
                                    int read;
                                    while (!task.CancelRequested && (read = netStream.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        fs.Write(buffer, 0, read);
                                        if (speedLimitKbps > 0)
                                        {
                                            int delayMs = (int)((read * 1000.0) / (speedLimitKbps * 1024.0 / numParts));
                                            if (delayMs > 0) Thread.Sleep(Math.Min(delayMs, 500));
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                if (!task.CancelRequested) anyError = true;
                            }
                        }));
                    }

                    long prevBytes = task.DownloadedSize;
                    Stopwatch sw = Stopwatch.StartNew();

                    while (!Task.WhenAll(workers).Wait(250))
                    {
                        if (task.CancelRequested) break;
                        long sum = 0;
                        for (int i = 0; i < numParts; i++)
                        {
                            string p = Path.Combine(task.TempDir, "part_" + i + ".tmp");
                            if (File.Exists(p)) sum += new FileInfo(p).Length;
                        }
                        task.DownloadedSize = sum;
                        double dt = sw.Elapsed.TotalSeconds;
                        if (dt > 0)
                        {
                            task.SpeedBps = Math.Max(0, (sum - prevBytes) / dt);
                            prevBytes = sum;
                            sw.Restart();
                        }
                    }

                    task.SpeedBps = 0;
                    if (task.CancelRequested) { task.Status = "Pausado"; return; }
                    if (anyError)
                    {
                        try { Directory.Delete(task.TempDir, true); } catch { }
                        if (!task.UseYtDlp)
                        {
                            task.UseYtDlp = true;
                            if (!string.IsNullOrEmpty(task.PageUrl) && task.PageUrl.StartsWith("http"))
                            {
                                task.Url = task.PageUrl;
                            }
                            StartYtDlpTaskAsync(task);
                            return;
                        }
                        task.Status = "Error";
                        return;
                    }

                    task.Status = "Combinando";
                    using (FileStream outFs = new FileStream(task.DestPath, FileMode.Create, FileAccess.Write))
                    {
                        for (int i = 0; i < numParts; i++)
                        {
                            string p = Path.Combine(task.TempDir, "part_" + i + ".tmp");
                            if (File.Exists(p))
                            {
                                using (FileStream inFs = new FileStream(p, FileMode.Open, FileAccess.Read))
                                {
                                    inFs.CopyTo(outFs);
                                }
                            }
                        }
                    }
                    try { Directory.Delete(task.TempDir, true); } catch { }
                    if (task.TotalSize <= 0 && File.Exists(task.DestPath))
                    {
                        task.TotalSize = new FileInfo(task.DestPath).Length;
                    }
                    task.DownloadedSize = task.TotalSize;
                    task.ManualProgress = 100.0;
                    task.Status = "Completado";
                }
                catch
                {
                    task.SpeedBps = 0;
                    task.Status = task.CancelRequested ? "Pausado" : "Error";
                }
            });
        }

        private void ResumeSelected()
        {
            foreach (ListViewItem lvi in listView.SelectedItems)
            {
                DownloadTask t = lvi.Tag as DownloadTask;
                if (t != null)
                {
                    if (t.UseYtDlp) StartYtDlpTaskAsync(t);
                    else StartTaskAsync(t);
                }
            }
        }

        private void PauseSelected()
        {
            foreach (ListViewItem lvi in listView.SelectedItems)
            {
                DownloadTask t = lvi.Tag as DownloadTask;
                if (t != null && (t.Status == "Descargando" || t.Status == "Conectando" || t.Status.StartsWith("Conectando")))
                {
                    t.CancelRequested = true;
                    if (t.ExternalProcess != null && !t.ExternalProcess.HasExited)
                    {
                        try { t.ExternalProcess.Kill(); } catch { }
                    }
                    t.Status = "Pausado";
                }
            }
        }

        private void PauseAll()
        {
            lock (tasks)
            {
                foreach (var t in tasks)
                {
                    if (t.Status == "Descargando" || t.Status == "Conectando")
                    {
                        t.CancelRequested = true;
                        if (t.ExternalProcess != null && !t.ExternalProcess.HasExited)
                        {
                            try { t.ExternalProcess.Kill(); } catch { }
                        }
                        t.Status = "Pausado";
                    }
                }
            }
        }

        private void DeleteSelected()
        {
            foreach (ListViewItem lvi in listView.SelectedItems)
            {
                DownloadTask t = lvi.Tag as DownloadTask;
                if (t != null)
                {
                    t.CancelRequested = true;
                    if (t.ExternalProcess != null && !t.ExternalProcess.HasExited)
                    {
                        try { t.ExternalProcess.Kill(); } catch { }
                    }
                    lock (tasks) { tasks.Remove(t); }
                }
            }
            RefreshListView();
        }

        // Doble clic en cualquier fila SIEMPRE abre la carpeta de descarga (resaltando el archivo si existe)
        private void OpenOrVerifySelectedFile()
        {
            try
            {
                if (listView.SelectedItems.Count == 0)
                {
                    Process.Start("explorer.exe", baseDir);
                    return;
                }
                DownloadTask t = listView.SelectedItems[0].Tag as DownloadTask;
                if (t == null)
                {
                    Process.Start("explorer.exe", baseDir);
                    return;
                }
                if (File.Exists(t.DestPath))
                {
                    Process.Start("explorer.exe", "/select,\"" + t.DestPath + "\"");
                }
                else if (!string.IsNullOrEmpty(t.SaveDir) && Directory.Exists(t.SaveDir))
                {
                    Process.Start("explorer.exe", "\"" + t.SaveDir + "\"");
                }
                else
                {
                    Process.Start("explorer.exe", "\"" + baseDir + "\"");
                }
            }
            catch { }
        }

        private static string ExtractJsonString(string json, string key)
        {
            Match m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"");
            if (!m.Success) return "";
            try { return Regex.Unescape(m.Groups[1].Value); }
            catch { return m.Groups[1].Value; }
        }

        private void StartChromeBridgeServer()
        {
            Task.Run(() =>
            {
                try
                {
                    bridgeListener = new HttpListener();
                    bridgeListener.Prefixes.Add("http://127.0.0.1:16888/");
                    bridgeListener.Start();
                    while (bridgeListener.IsListening)
                    {
                        HttpListenerContext ctx = bridgeListener.GetContext();
                        HttpListenerRequest req = ctx.Request;
                        HttpListenerResponse resp = ctx.Response;
                        resp.Headers.Add("Access-Control-Allow-Origin", "*");
                        resp.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                        resp.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                        if (req.HttpMethod == "OPTIONS")
                        {
                            resp.StatusCode = 200;
                            resp.Close();
                            continue;
                        }

                        if (req.Url.AbsolutePath.StartsWith("/ping") || req.Url.AbsolutePath.StartsWith("/status"))
                        {
                            string themeStr = IsDarkMode ? "dark" : "light";
                            byte[] buf = Encoding.UTF8.GetBytes("{\"ok\":true,\"app\":\"FoldFAST.exe\",\"version\":\"6.0\",\"theme\":\"" + themeStr + "\",\"lang\":\"" + CurrentLang + "\"}");
                            resp.ContentType = "application/json";
                            resp.OutputStream.Write(buf, 0, buf.Length);
                            resp.Close();
                            continue;
                        }

                        if (req.HttpMethod == "POST" && req.Url.AbsolutePath.StartsWith("/theme"))
                        {
                            string body = new StreamReader(req.InputStream, Encoding.UTF8).ReadToEnd();
                            string requestedTheme = ExtractJsonString(body, "theme");
                            this.BeginInvoke((MethodInvoker)delegate
                            {
                                if (requestedTheme == "light") ToggleTheme(false);
                                else if (requestedTheme == "dark") ToggleTheme(true);
                                else ToggleTheme();
                            });
                            string newThemeStr = IsDarkMode ? "dark" : "light";
                            byte[] tBuf = Encoding.UTF8.GetBytes("{\"ok\":true,\"theme\":\"" + newThemeStr + "\"}");
                            resp.ContentType = "application/json";
                            resp.OutputStream.Write(tBuf, 0, tBuf.Length);
                            resp.Close();
                            continue;
                        }

                        if (req.HttpMethod == "POST" && req.Url.AbsolutePath.StartsWith("/lang"))
                        {
                            string body = new StreamReader(req.InputStream, Encoding.UTF8).ReadToEnd();
                            string requestedLang = ExtractJsonString(body, "lang");
                            this.BeginInvoke((MethodInvoker)delegate
                            {
                                ToggleLanguage(requestedLang);
                            });
                            byte[] lBuf = Encoding.UTF8.GetBytes("{\"ok\":true,\"lang\":\"" + CurrentLang + "\"}");
                            resp.ContentType = "application/json";
                            resp.OutputStream.Write(lBuf, 0, lBuf.Length);
                            resp.Close();
                            continue;
                        }

                        if (req.HttpMethod == "POST" && req.Url.AbsolutePath.StartsWith("/open_folder"))
                        {
                            this.BeginInvoke((MethodInvoker)delegate
                            {
                                try { Process.Start("explorer.exe", baseDir); } catch { }
                            });
                            byte[] fBuf = Encoding.UTF8.GetBytes("{\"ok\":true}");
                            resp.ContentType = "application/json";
                            resp.OutputStream.Write(fBuf, 0, fBuf.Length);
                            resp.Close();
                            continue;
                        }

                        if (req.HttpMethod == "POST" && req.Url.AbsolutePath.StartsWith("/add"))
                        {
                            string body = new StreamReader(req.InputStream, Encoding.UTF8).ReadToEnd();
                            string url = ExtractJsonString(body, "url");
                            string pageUrl = ExtractJsonString(body, "pageUrl");
                            string fallbackStreamUrl = ExtractJsonString(body, "fallbackStreamUrl");
                            string title = ExtractJsonString(body, "title");
                            string filename = ExtractJsonString(body, "filename");
                            string mediaType = ExtractJsonString(body, "mediaType");
                            string formatExt = ExtractJsonString(body, "formatExt");
                            string quality = ExtractJsonString(body, "quality");
                            string referer = ExtractJsonString(body, "referer");
                            string ua = ExtractJsonString(body, "userAgent");

                            if (string.IsNullOrEmpty(mediaType)) mediaType = "video";
                            string customName = !string.IsNullOrEmpty(filename) ? filename : title;

                            if (!string.IsNullOrEmpty(url) || !string.IsNullOrEmpty(pageUrl) || !string.IsNullOrEmpty(fallbackStreamUrl))
                            {
                                this.BeginInvoke((MethodInvoker)delegate
                                {
                                    RestoreFromTray();
                                    AddAndStartDownload(url, pageUrl, fallbackStreamUrl, customName, mediaType, formatExt, quality, referer, ua, 8);
                                });
                            }
                            byte[] okBuf = Encoding.UTF8.GetBytes("{\"ok\":true}");
                            resp.ContentType = "application/json";
                            resp.OutputStream.Write(okBuf, 0, okBuf.Length);
                            resp.Close();
                            continue;
                        }
                        resp.StatusCode = 404;
                        resp.Close();
                    }
                }
                catch { }
            });
        }

        private List<DownloadTask> GetFilteredTasks()
        {
            List<DownloadTask> res = new List<DownloadTask>();
            lock (tasks)
            {
                foreach (var t in tasks)
                {
                    if (selectedCategory == "Todas") res.Add(t);
                    else if (selectedCategory == "En curso" && t.Status != "Completado") res.Add(t);
                    else if (selectedCategory == "Completado" && t.Status == "Completado") res.Add(t);
                    else if (t.Category == selectedCategory) res.Add(t);
                }
            }
            return res;
        }

        private static string FormatBadgeText(DownloadTask t)
        {
            string fmt = string.IsNullOrEmpty(t.FormatExt) ? "BIN" : t.FormatExt.ToUpperInvariant();
            string q = string.IsNullOrEmpty(t.Quality) ? "" : (" • " + (t.Quality.EndsWith("p") || t.Quality.EndsWith("k") || t.Quality == "Directo" || t.Quality == "Lote" || t.Quality == "best" ? t.Quality : t.Quality + "p"));
            if (q == " • best") q = " • Auto";
            return fmt + q;
        }

        private void RefreshListView()
        {
            var filtered = GetFilteredTasks();
            listView.BeginUpdate();
            listView.Items.Clear();
            foreach (var t in filtered)
            {
                ListViewItem item = new ListViewItem(t.FileName);
                item.SubItems.Add(FormatBadgeText(t));
                item.SubItems.Add(FormatBytes(t.TotalSize));
                item.SubItems.Add(TranslateStatus(t.Status));
                item.SubItems.Add(t.ProgressPercent.ToString("0.0") + "%");
                item.SubItems.Add(FormatBytes((long)t.SpeedBps) + "/s");
                item.SubItems.Add(t.CreatedAt);
                item.Tag = t;
                listView.Items.Add(item);
            }
            listView.EndUpdate();
        }

        private void UiTimer_Tick(object sender, EventArgs e)
        {
            var filtered = GetFilteredTasks();
            if (listView.Items.Count != filtered.Count)
            {
                RefreshListView();
            }
            else
            {
                double totalSpeed = 0;
                bool needsRedraw = false;
                for (int i = 0; i < listView.Items.Count; i++)
                {
                    DownloadTask t = listView.Items[i].Tag as DownloadTask;
                    if (t != null)
                    {
                        totalSpeed += t.SpeedBps;
                        string newSize = FormatBytes(t.TotalSize);
                        string newStatus = TranslateStatus(t.Status);
                        string newPct = t.ProgressPercent.ToString("0.0") + "%";
                        string newSpd = FormatBytes((long)t.SpeedBps) + "/s";

                        if (listView.Items[i].SubItems[0].Text != t.FileName ||
                            listView.Items[i].SubItems[2].Text != newSize ||
                            listView.Items[i].SubItems[3].Text != newStatus ||
                            listView.Items[i].SubItems[4].Text != newPct ||
                            listView.Items[i].SubItems[5].Text != newSpd)
                        {
                            listView.Items[i].SubItems[0].Text = t.FileName;
                            listView.Items[i].SubItems[1].Text = FormatBadgeText(t);
                            listView.Items[i].SubItems[2].Text = newSize;
                            listView.Items[i].SubItems[3].Text = newStatus;
                            listView.Items[i].SubItems[4].Text = newPct;
                            listView.Items[i].SubItems[5].Text = newSpd;
                            needsRedraw = true;
                        }
                    }
                }
                if (needsRedraw) listView.Invalidate();
                lblTotalSpeed.Text = "⚡ " + (totalSpeed > 0 ? FormatBytes((long)totalSpeed) + "/s" : "0 KB/s");
            }

            if (chkClipboard.Checked && Clipboard.ContainsText())
            {
                try
                {
                    string clip = Clipboard.GetText().Trim();
                    if (clip != lastClipboard && (clip.StartsWith("http://") || clip.StartsWith("https://")))
                    {
                        lastClipboard = clip;
                        string cat = DetectCategory(GuessFileName(clip, "", "file", ""), "file");
                        if (cat != "General")
                        {
                            RestoreFromTray();
                            AddAndStartDownload(clip, "", "", "", "file", "", "Directo", "", "", 8);
                        }
                    }
                }
                catch { }
            }
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "--";
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double val = bytes;
            int idx = 0;
            while (val >= 1024 && idx < units.Length - 1) { val /= 1024; idx++; }
            return idx == 0 ? ((long)val + " B") : (val.ToString("0.00") + " " + units[idx]);
        }

        [STAThread]
        public static void Main(string[] args)
        {
            bool minimized = false;
            if (args != null)
            {
                foreach (string a in args)
                {
                    if (a == "--minimized" || a == "-m") minimized = true;
                }
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => { };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => { };
            Application.Run(new MainForm(minimized));
        }
    }
}
