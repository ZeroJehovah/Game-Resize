using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Configuration;

namespace Game_Resize
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
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        const uint WM_GETICON = 0x007f;
        const uint ICON_SMALL2 = 2;
        const uint ICON_SMALL = 0;
        const uint ICON_BIG = 1;
        const int VK_MENU = 0x12; // Alt 键的虚拟键码
        const uint SWP_NOACTIVATE = 0x0010; // 不激活窗口

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1); // 窗口置顶
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2); // 窗口取消置顶

        private readonly int smallClientWidth;
        private readonly string TARGET_CLASS_NAME;
        private readonly string TARGET_FORM_TITLE;

        private static int BORDER_WIDTH = 0;
        private static int BORDER_HEIGHT = 0;

        private static RECT STAR_RAIL_ORIGINAL;
        private static RECT STAR_RAIL_SMALL;
        private static bool RECT_STAY = false;
        private static bool ALT_MOVING = false;
        private static int LOST_TARGET_FORM_TIMES = 0;
        private static readonly int LOST_TARGET_FORM_TIMES_MAX = 10;

        private NotifyIcon notifyIcon = new NotifyIcon();
        private ToolStripMenuItem menuItem_auto = new ToolStripMenuItem();
        private ToolStripMenuItem menuItem_stay = new ToolStripMenuItem();
        private IntPtr targetForm = IntPtr.Zero;
        private IntPtr lastFocusForm = IntPtr.Zero;
        private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

        /**
         * 构造函数，初始化通知栏图标、菜单、配置；初始化Timer
         */
        public MainForm()
        {
            InitializeComponent();
            this.Load += new EventHandler(Form1_Load);
            smallClientWidth = int.Parse(ConfigurationManager.AppSettings["SmallRectWidth"] ?? "400");
            TARGET_CLASS_NAME = ConfigurationManager.AppSettings["TargetClassName"] ?? string.Empty;
            TARGET_FORM_TITLE = ConfigurationManager.AppSettings["TargetFormTitle"] ?? string.Empty;
            if (smallClientWidth == 0 || TARGET_CLASS_NAME == string.Empty || TARGET_FORM_TITLE == string.Empty)
            {
                Console.WriteLine("初始化失败");
                notifyIcon.Visible = false;
                Application.Exit();
            }
            notifyIcon.Text = TARGET_FORM_TITLE + "摸鱼小助手";

            timer.Interval = 100;
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        /**
         * 定时任务，检查目标窗口是否存在以及是否获取焦点
         */
        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!initTargetForm())
            {
                return;
            }
            IntPtr currentFocus = GetForegroundWindow();
            String currentFocusTitle = GetWindowTitle(currentFocus);
            // Console.WriteLine("currentFocusTitle: " + currentFocusTitle);
            if (currentFocusTitle == string.Empty || currentFocusTitle == this.Text || currentFocusTitle == "任务切换" || currentFocusTitle == "任务视图" || currentFocusTitle == "搜索")
            {
                // 如果当前焦点是系统组件，则视为未改变焦点状态
                return;
            }
            RECT currentRect = new RECT();
            GetWindowRect(targetForm, out currentRect);
            bool isCurrentOriginal = currentRect.Right - currentRect.Left > smallClientWidth + 20;

            if (currentFocus == targetForm && (GetAsyncKeyState(VK_MENU) & 0x8000) != 0 && !isCurrentOriginal)
            {
                // 目标窗口获取焦点，并且Alt 键处于按下状态，当前为缩小尺寸，则视为未改变焦点状态
                ALT_MOVING = true;
                return;
            }
            if (currentFocus == targetForm && (GetAsyncKeyState(VK_MENU) & 0x8000) == 0 && !isCurrentOriginal && ALT_MOVING)
            {
                ALT_MOVING = false;
                // 读取新的缩小尺寸并保存
                if (!STAR_RAIL_SMALL.Equals(currentRect))
                {
                    STAR_RAIL_SMALL = currentRect;
                    Console.WriteLine("update small RECT: " + JsonSerializer.Serialize(STAR_RAIL_SMALL));
                    saveSmallRECT();
                }
                changeFocus2LastFocusForm();
                return;
            }
            lastFocusForm = currentFocus;
            if (RECT_STAY)
            {
                // 如果选择“保持”，则不执行
                return;
            }
            if (!isCurrentOriginal && lastFocusForm == targetForm)
            {
                // 目标窗口为缩小尺寸时获取焦点，则改变为原尺寸
                change2original();
            }
            else if (isCurrentOriginal && lastFocusForm != targetForm)
            {
                // 目标窗口为原尺寸时失去焦点，则改变为缩小尺寸
                change2small(currentRect);
            }
        }

        /**
         * 检查目标窗口的尺寸是否初始化到缓存，如果没有，则执行初始化
         */
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
                Console.WriteLine("init original RECT: " + JsonSerializer.Serialize(STAR_RAIL_ORIGINAL));

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
                    Console.WriteLine($"originalFormWidth: {originalFormWidth}, originalFormHeight: {originalFormHeight}, originalClientWidth: {originalClientWidth}, originalClientHeight: {originalClientHeight}");
                    BORDER_WIDTH = originalFormWidth - originalClientWidth;
                    BORDER_HEIGHT = originalFormHeight - originalClientHeight;
                    Console.WriteLine($"BORDER_WIDTH: {BORDER_WIDTH}, BORDER_HEIGHT: {BORDER_HEIGHT}");
                    int smallClientHeight = smallClientWidth * originalClientHeight / originalClientWidth;
                    int smallFormWidth = smallClientWidth + BORDER_WIDTH;
                    int smallFormHeight = smallClientHeight + BORDER_HEIGHT;
                    Console.WriteLine($"smallFormWidth: {smallFormWidth}, smallFormHeight: {smallFormHeight}, smallClientWidth: {smallClientWidth}, smallClientHeight: {smallClientHeight}");
                    STAR_RAIL_SMALL.Left = STAR_RAIL_ORIGINAL.Left;
                    STAR_RAIL_SMALL.Right = STAR_RAIL_ORIGINAL.Left + smallFormWidth;
                    STAR_RAIL_SMALL.Top = STAR_RAIL_ORIGINAL.Top;
                    STAR_RAIL_SMALL.Bottom = STAR_RAIL_ORIGINAL.Top + smallFormHeight;
                    Console.WriteLine("init small RECT: " + JsonSerializer.Serialize(STAR_RAIL_SMALL));
                    saveSmallRECT();
                }
                else
                {
                    STAR_RAIL_SMALL.Left = smallLeft;
                    STAR_RAIL_SMALL.Right = smallRight;
                    STAR_RAIL_SMALL.Top = smallTop;
                    STAR_RAIL_SMALL.Bottom = smallBottom;
                    Console.WriteLine("load small RECT from config: " + JsonSerializer.Serialize(STAR_RAIL_SMALL));
                }
                setTargetIcon();
                return true;
            }
            if (!IsWindow(targetForm))
            {
                if (++LOST_TARGET_FORM_TIMES > LOST_TARGET_FORM_TIMES_MAX)
                {
                    Console.WriteLine("lost target form");
                    targetForm = IntPtr.Zero;
                    resetIcon();
                }
                return false;
            }
            LOST_TARGET_FORM_TIMES = 0;
            return true;
        }

        /**
         * 将目标窗口调整为原尺寸
         */
        private void change2original()
        {
            if (targetForm == IntPtr.Zero || !IsWindow(targetForm))
            {
                return;
            }
            if (lastFocusForm == targetForm)
            {
                SetWindowPos(targetForm, HWND_NOTOPMOST, STAR_RAIL_ORIGINAL.Left, STAR_RAIL_ORIGINAL.Top, STAR_RAIL_ORIGINAL.Right - STAR_RAIL_ORIGINAL.Left, STAR_RAIL_ORIGINAL.Bottom - STAR_RAIL_ORIGINAL.Top, 0);
            }
            else
            {
                SetWindowPos(targetForm, lastFocusForm, STAR_RAIL_ORIGINAL.Left, STAR_RAIL_ORIGINAL.Top, STAR_RAIL_ORIGINAL.Right - STAR_RAIL_ORIGINAL.Left, STAR_RAIL_ORIGINAL.Bottom - STAR_RAIL_ORIGINAL.Top, SWP_NOACTIVATE);
                changeFocus2LastFocusForm();
            }

            Console.WriteLine("change to original RECT");
        }

        /**
         * 将目标窗口缩小并置顶
         */
        private void change2small(RECT originalRect)
        {
            if (targetForm == IntPtr.Zero || !IsWindow(targetForm))
            {
                return;
            }
            // 读取新的原尺寸并保存
            if (!STAR_RAIL_ORIGINAL.Equals(originalRect))
            {
                STAR_RAIL_ORIGINAL = originalRect;
                Console.WriteLine("update original RECT: " + JsonSerializer.Serialize(STAR_RAIL_SMALL));
            }

            SetWindowPos(targetForm, HWND_TOPMOST, STAR_RAIL_SMALL.Left, STAR_RAIL_SMALL.Top, STAR_RAIL_SMALL.Right - STAR_RAIL_SMALL.Left, STAR_RAIL_SMALL.Bottom - STAR_RAIL_SMALL.Top, 0);
            Console.WriteLine("change to small RECT");
        }

        private void changeFocus2LastFocusForm()
        {
            String lastFocusFormTitle = GetWindowTitle(lastFocusForm);
            Console.WriteLine("change focus to :" + lastFocusFormTitle);
            SetForegroundWindow(lastFocusForm);
        }

        /**
         * “自动”菜单选项事件
         */
        private void MenuItem_Auto_Click(object sender, EventArgs e)
        {
            RECT_STAY = false;
            menuItem_auto.Checked = true;
            menuItem_stay.Checked = false;
        }

        /**
         * “保持”菜单选项事件
         */
        private void MenuItem_Stay_Click(object sender, EventArgs e)
        {
            RECT_STAY = true;
            menuItem_auto.Checked = false;
            menuItem_stay.Checked = true;
        }

        /**
         * “初始化”菜单选项事件
         */
        private void MenuItem_Reset_Click(object sender, EventArgs e)
        {
            change2original();
            targetForm = IntPtr.Zero;
            STAR_RAIL_SMALL.Left = 0;
            STAR_RAIL_SMALL.Right = 0;
            STAR_RAIL_SMALL.Top = 0;
            STAR_RAIL_SMALL.Bottom = 0;
            Console.WriteLine("reset small RECT: " + JsonSerializer.Serialize(STAR_RAIL_SMALL));
            saveSmallRECT();
        }

        // /**
        //  * “测试缩小”菜单选项事件
        //  */
        // private void MenuItem_Test_Small_Click(object sender, EventArgs e)
        // {
        //     RECT currentRect = new RECT();
        //     GetWindowRect(targetForm, out currentRect);
        //     bool isCurrentOriginal = currentRect.Right - currentRect.Left > smallClientWidth + 20;
        //     if (isCurrentOriginal)
        //     {
        //         change2small(currentRect);
        //     }
        // }

        // /**
        //  * “测试恢复”菜单选项事件
        //  */
        // private void MenuItem_Test_Original_Click(object sender, EventArgs e)
        // {
        //     lastFocusForm = targetForm;
        //     change2original();
        // }

        /**
         * “退出”菜单选项事件
         */
        private void MenuItem_Exit_Click(object sender, EventArgs e)
        {
            timer.Stop();
            change2original();
            notifyIcon.Visible = false;
            Application.Exit();
        }

        /**
         * 获取窗口标题，主要用于判断获取焦点的是否为系统组件
         */
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

        /**
         * 将缩小的尺寸信息保存到配置文件
         */
        private static void saveSmallRECT()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["SmallLeft"].Value = STAR_RAIL_SMALL.Left.ToString();
            config.AppSettings.Settings["SmallRight"].Value = STAR_RAIL_SMALL.Right.ToString();
            config.AppSettings.Settings["SmallTop"].Value = STAR_RAIL_SMALL.Top.ToString();
            config.AppSettings.Settings["SmallBottom"].Value = STAR_RAIL_SMALL.Bottom.ToString();
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
            Console.WriteLine("save small RECT to config");
        }

        /**
         * 程序启动后加载，将关闭默认窗口
         */
        private void Form1_Load(object? sender, EventArgs e)
        {
            this.BeginInvoke(new Action(() =>
            {
                Console.WriteLine("hide main form to notification area");
                this.Hide();
            }));
        }

        /**
         * 在检测到目标窗口后，将通知栏图标调整为目标窗口的图标
         */
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
            Console.WriteLine("set icon as target form icon");
            notifyIcon.Icon = icon;
            notifyIcon.Text = TARGET_FORM_TITLE + "摸鱼中";
        }

        /**
         * 在未检测到目标窗口时，将通知栏图标还原
         */
        private void resetIcon()
        {
            Console.WriteLine("reset icon to default");
            notifyIcon.Icon = new Icon("resources/icon.ico");
            notifyIcon.Text = TARGET_FORM_TITLE + "摸鱼小助手";
        }

        /**
         * 保存窗口位置信息的对象
         */
        private struct RECT
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

    }
}
