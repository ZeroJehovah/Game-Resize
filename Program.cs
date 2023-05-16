namespace Game_Resize
{
    static class Program
    {
        // private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Mutex mutex = new Mutex(true, "Game-Resize");
            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                // 已经有一个实例在运行，退出当前实例
                MessageBox.Show("程序已经在运行了", "提示");
                return;
            }

            Console.WriteLine("Application started");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
