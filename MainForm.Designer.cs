namespace Resize_Game;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        // this.components = new System.ComponentModel.Container();
        // this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        // this.ClientSize = new System.Drawing.Size(800, 450);
        // this.Text = "摸鱼小助手";

        this.Opacity = 0;
        this.ShowInTaskbar = false;

        notifyIcon.Icon = new Icon("resources/icon.ico");
        notifyIcon.Text = "摸鱼小助手";
        notifyIcon.Visible = true;

        ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
        ToolStripMenuItem menuItem_reset = new ToolStripMenuItem();
        ToolStripMenuItem menuItem_exit = new ToolStripMenuItem();

        // 创建菜单选项
        menuItem_auto.Text = "自动";
        menuItem_auto.Checked = true; // 设置选项前打钩
        menuItem_auto.Click += MenuItem_Auto_Click; // 点击事件
        menuItem_stay.Text = "保持";
        menuItem_stay.Click += MenuItem_Stay_Click; // 点击事件
        menuItem_reset.Text = "初始化";
        menuItem_reset.Click += MenuItem_Reset_Click; // 点击事件
        menuItem_exit.Text = "退出";
        menuItem_exit.Click += MenuItem_Exit_Click; // 点击事件

        // 添加菜单选项到右键菜单
        contextMenuStrip.Items.Add(menuItem_auto);
        contextMenuStrip.Items.Add(menuItem_stay);
        contextMenuStrip.Items.Add("-");
        contextMenuStrip.Items.Add(menuItem_reset);
        contextMenuStrip.Items.Add("-");
        contextMenuStrip.Items.Add(menuItem_exit);
        notifyIcon.ContextMenuStrip = contextMenuStrip;
    }

    #endregion
}
