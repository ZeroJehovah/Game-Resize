# 简介
一个简单的，使用C#语言编写的，用于摸鱼的小工具。  
ChatGPT提供了大量帮助。
# 作用
当目标窗口失去焦点，使其缩小并置顶；当恢复焦点，使其回复原尺寸。
# 使用方法
1. 下载[最新版本](https://github.com/ZeroJehovah/Game-Resize/releases)并解压；
   > 如果是更新版本，则不要替换原来的```Game_Resize.dll.config```文件
2. ```Game_Resize.dll.config```文件中，需要配置以下选项：
   > SmallRectWidth: 缩小后的窗口宽度，可自行设置，初始化后生效  
   > TargetClassName: 需要缩小的窗口的类名，可通过[Window Detective](https://windowdetective.sourceforge.io/)等工具获取  
   > TargetFormTitle: 需要缩小的窗口的标题
3. 启动程序
# 常见问题
1. 无法运行/缺少依赖/奇奇怪怪的bug：
   1. 本程序基于.net sdk 6.0开发，可尝试[安装](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)后使用；
   2. 本程序是在win11上开发和测试的，可尝试[升级](https://www.microsoft.com/zh-cn/windows/get-windows-11)后使用；
2. 本程序仅对[崩坏：星穹铁道](https://sr.mihoyo.com/)这款游戏做了测试，如果对你想控制的软件支持不理想，可以考虑其它程序。
3. 使用前需要将程序设置为窗口化。
4. 在缩小显示时，按住```Alt```可以拖动窗口。
5. 本程序已基本满足我的需求，下面的功能现在没有，以后大概率也不会有：
   > 多系统兼容、自动检测程序窗口、多程序支持、缩小时静音、缩放时动画、开机自启动、自动更新等
6. 本程序仅几百行代码，欢迎二次开发。