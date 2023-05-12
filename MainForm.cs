using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using log4net;
using System.Configuration;

namespace Resize_Game
{
    public partial class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        const uint WM_GETICON = 0x007f;
        const uint ICON_SMALL2 = 2;
        const uint ICON_SMALL = 0;
        const uint ICON_BIG = 1;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1); // 窗口置顶
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2); // 窗口取消置顶

        private static readonly ILog log = LogManager.GetLogger(typeof(MainForm));

        private readonly int smallClientWidth;
        private readonly string TARGET_CLASS_NAME;
        private readonly string TARGET_FORM_TITLE;

        private static int BORDER_WIDTH = 0;
        private static int BORDER_HEIGHT = 0;

        private static RECT STAR_RAIL_ORIGINAL;
        private static RECT STAR_RAIL_SMALL;
        private static bool RECT_STAY = false;
        private static int LOST_TARGET_FORM_TIMES = 0;
        private static readonly int LOST_TARGET_FORM_TIMES_MAX = 10;

        private NotifyIcon notifyIcon = new NotifyIcon();
        private ToolStripMenuItem menuItem_auto = new ToolStripMenuItem();
        private ToolStripMenuItem menuItem_stay = new ToolStripMenuItem();
        private IntPtr targetForm = IntPtr.Zero;
        private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

        public MainForm()
        {
            InitializeComponent();
            this.Load += new EventHandler(Form1_Load);
            smallClientWidth = int.Parse(ConfigurationManager.AppSettings["SmallRectWidth"] ?? "400");
            TARGET_CLASS_NAME = ConfigurationManager.AppSettings["TargetClassName"] ?? string.Empty;
            TARGET_FORM_TITLE = ConfigurationManager.AppSettings["TargetFormTitle"] ?? string.Empty;
            if (smallClientWidth == 0 || TARGET_CLASS_NAME == string.Empty || TARGET_FORM_TITLE == string.Empty)
            {
                log.Info("初始化失败");
                notifyIcon.Visible = false;
                Application.Exit();
            }
            notifyIcon.Text = TARGET_FORM_TITLE + "摸鱼小助手";

            timer.Interval = 100;
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!initTargetForm())
            {
                return;
            }
            IntPtr currentFocus = GetForegroundWindow();
            String currentFocusTitle = GetWindowTitle(currentFocus);
            // log.Info("currentFocusTitle: " + currentFocusTitle);
            if (RECT_STAY)
            {
                // 如果选择“保持”，则不执行
                return;
            }
            if (currentFocus == targetForm)
            {
                // 如果目标窗口获取焦点，则改变为原尺寸
                change2original();
            }
            else if (currentFocus != targetForm && currentFocusTitle != string.Empty && currentFocusTitle != this.Text && currentFocusTitle != "任务切换" && currentFocusTitle != "任务视图")
            {
                // 如果目标窗口失去焦点，并且焦点标题不为空，并且焦点标题不是本程序，则改变为缩小尺寸
                change2small();
            }
        }

        private bool initTargetForm()
        {
            if (targetForm == IntPtr.Zero)
            {
                targetForm = FindWindow(TARGET_CLASS_NAME, TARGET_FORM_TITLE);
                if (targetForm == IntPtr.Zero)
                {
                    return false;
                }
                LOST_TARGET_FORM_TIMES = 0;
                GetWindowRect(targetForm, out STAR_RAIL_ORIGINAL);
                int originalFormWidth = STAR_RAIL_ORIGINAL.Right - STAR_RAIL_ORIGINAL.Left;
                int originalFormHeight = STAR_RAIL_ORIGINAL.Bottom - STAR_RAIL_ORIGINAL.Top;
                if (originalFormWidth < smallClientWidth + 20)
                {
                    targetForm = IntPtr.Zero;
                    return false;
                }
                log.Info("init original RECT: " + JsonSerializer.Serialize(STAR_RAIL_ORIGINAL));

                int smallLeft = int.Parse(ConfigurationManager.AppSettings["SmallLeft"] ?? "0");
                int smallRight = int.Parse(ConfigurationManager.AppSettings["SmallRight"] ?? "0");
                int smallTop = int.Parse(ConfigurationManager.AppSettings["SmallTop"] ?? "0");
                int smallBottom = int.Parse(ConfigurationManager.AppSettings["SmallBottom"] ?? "0");
                if (smallLeft == smallRight || smallTop == smallBottom)
                {
                    RECT originalClientRect = new RECT();
                    GetClientRect(targetForm, out originalClientRect);
                    int originalClientWidth = originalClientRect.Right - originalClientRect.Left;
                    int originalClientHeight = originalClientRect.Bottom - originalClientRect.Top;
                    log.Info($"originalFormWidth: {originalFormWidth}, originalFormHeight: {originalFormHeight}, originalClientWidth: {originalClientWidth}, originalClientHeight: {originalClientHeight}");
                    BORDER_WIDTH = originalFormWidth - originalClientWidth;
                    BORDER_HEIGHT = originalFormHeight - originalClientHeight;
                    log.Info($"BORDER_WIDTH: {BORDER_WIDTH}, BORDER_HEIGHT: {BORDER_HEIGHT}");
                    int smallClientHeight = smallClientWidth * originalClientHeight / originalClientWidth;
                    int smallFormWidth = smallClientWidth + BORDER_WIDTH;
                    int smallFormHeight = smallClientHeight + BORDER_HEIGHT;
                    log.Info($"smallFormWidth: {smallFormWidth}, smallFormHeight: {smallFormHeight}, smallClientWidth: {smallClientWidth}, smallClientHeight: {smallClientHeight}");
                    STAR_RAIL_SMALL.Left = STAR_RAIL_ORIGINAL.Left;
                    STAR_RAIL_SMALL.Right = STAR_RAIL_ORIGINAL.Left + smallFormWidth;
                    STAR_RAIL_SMALL.Top = STAR_RAIL_ORIGINAL.Top;
                    STAR_RAIL_SMALL.Bottom = STAR_RAIL_ORIGINAL.Top + smallFormHeight;
                    log.Info("init small RECT: " + JsonSerializer.Serialize(STAR_RAIL_SMALL));
                    saveSmallRECT();
                }
                else
                {
                    STAR_RAIL_SMALL.Left = smallLeft;
                    STAR_RAIL_SMALL.Right = smallRight;
                    STAR_RAIL_SMALL.Top = smallTop;
                    STAR_RAIL_SMALL.Bottom = smallBottom;
                    log.Info("load small RECT from config: " + JsonSerializer.Serialize(STAR_RAIL_SMALL));
                }
                setTargetIcon();
                return true;
            }
            if (!IsWindow(targetForm))
            {
                if (++LOST_TARGET_FORM_TIMES > LOST_TARGET_FORM_TIMES_MAX)
                {
                    log.Info("lost target form");
                    targetForm = IntPtr.Zero;
                    resetIcon();
                }
                return false;
            }
            LOST_TARGET_FORM_TIMES = 0;
            return true;
        }

        private void change2original()
        {
            if (targetForm == IntPtr.Zero || !IsWindow(targetForm))
            {
                return;
            }
            RECT tempRect = new RECT();
            GetWindowRect(targetForm, out tempRect);
            if (tempRect.Right - tempRect.Left > smallClientWidth + 20)
            {
                // 如果本来就是原尺寸，不执行操作
                return;
            }
            // 读取新的缩小尺寸并保存
            if (!STAR_RAIL_SMALL.Equals(tempRect))
            {
                STAR_RAIL_SMALL = tempRect;
                log.Info("update small RECT: " + JsonSerializer.Serialize(STAR_RAIL_SMALL));
                saveSmallRECT();
            }
            SetWindowPos(targetForm, HWND_NOTOPMOST, STAR_RAIL_ORIGINAL.Left, STAR_RAIL_ORIGINAL.Top, STAR_RAIL_ORIGINAL.Right - STAR_RAIL_ORIGINAL.Left, STAR_RAIL_ORIGINAL.Bottom - STAR_RAIL_ORIGINAL.Top, 0);
            log.Info("change to original RECT");
        }

        private void change2small()
        {
            if (targetForm == IntPtr.Zero || !IsWindow(targetForm))
            {
                return;
            }
            RECT tempRect = new RECT();
            GetWindowRect(targetForm, out tempRect);
            if (tempRect.Right - tempRect.Left < smallClientWidth + 20)
            {
                // 如果本来就是缩小尺寸，不执行操作
                return;
            }
            // 读取新的原尺寸并保存
            if (!STAR_RAIL_ORIGINAL.Equals(tempRect))
            {
                STAR_RAIL_ORIGINAL = tempRect;
                log.Info("update original RECT: " + JsonSerializer.Serialize(STAR_RAIL_SMALL));
            }

            SetWindowPos(targetForm, HWND_TOPMOST, STAR_RAIL_SMALL.Left, STAR_RAIL_SMALL.Top, STAR_RAIL_SMALL.Right - STAR_RAIL_SMALL.Left, STAR_RAIL_SMALL.Bottom - STAR_RAIL_SMALL.Top, 0);
            log.Info("change to small RECT");
        }

        private void MenuItem_Auto_Click(object sender, EventArgs e)
        {
            RECT_STAY = false;
            menuItem_auto.Checked = true;
            menuItem_stay.Checked = false;
        }

        private void MenuItem_Stay_Click(object sender, EventArgs e)
        {
            RECT_STAY = true;
            menuItem_auto.Checked = false;
            menuItem_stay.Checked = true;
        }

        private void MenuItem_Reset_Click(object sender, EventArgs e)
        {
            change2original();
            targetForm = IntPtr.Zero;
            STAR_RAIL_SMALL.Left = 0;
            STAR_RAIL_SMALL.Right = 0;
            STAR_RAIL_SMALL.Top = 0;
            STAR_RAIL_SMALL.Bottom = 0;
            log.Info("reset small RECT: " + JsonSerializer.Serialize(STAR_RAIL_SMALL));
            saveSmallRECT();
        }

        private void MenuItem_Exit_Click(object sender, EventArgs e)
        {
            timer.Stop();
            change2original();
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private static string GetWindowTitle(IntPtr hWnd)
        {
            const int nChars = 256;
            StringBuilder sb = new StringBuilder(nChars);
            if (GetWindowText(hWnd, sb, nChars) > 0)
            {
                return sb.ToString();
            }
            return string.Empty;
        }

        private static void saveSmallRECT()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["SmallLeft"].Value = STAR_RAIL_SMALL.Left.ToString();
            config.AppSettings.Settings["SmallRight"].Value = STAR_RAIL_SMALL.Right.ToString();
            config.AppSettings.Settings["SmallTop"].Value = STAR_RAIL_SMALL.Top.ToString();
            config.AppSettings.Settings["SmallBottom"].Value = STAR_RAIL_SMALL.Bottom.ToString();
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
            log.Info("save small RECT to config");
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            this.BeginInvoke(new Action(() =>
            {
                log.Info("hide main form to notification area");
                this.Hide();
            }));
        }

        private void setTargetIcon()
        {
            IntPtr hIcon = SendMessage(targetForm, WM_GETICON, new IntPtr(ICON_SMALL2), IntPtr.Zero);
            hIcon = hIcon == IntPtr.Zero ? SendMessage(targetForm, WM_GETICON, new IntPtr(ICON_SMALL), IntPtr.Zero) : hIcon;
            hIcon = hIcon == IntPtr.Zero ? SendMessage(targetForm, WM_GETICON, new IntPtr(ICON_BIG), IntPtr.Zero) : hIcon;
            if (hIcon == IntPtr.Zero)
            {
                return;
            }
            Icon icon = Icon.FromHandle(hIcon);
            if (icon == null)
            {
                return;
            }
            log.Info("set icon as target form icon");
            notifyIcon.Icon = icon;
        }

        private void resetIcon()
        {
            log.Info("reset icon to default");
            notifyIcon.Icon = new Icon("resources/icon.ico");
        }

        private struct RECT
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

    }
}
