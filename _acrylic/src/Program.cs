// 桌面小窗 —— Win11 亚克力宿主
//关键点：
//   1) WebView2 的 DefaultBackgroundColor设为 Transparent —— 这是页面级真透明的必要条件，
//      Chromium 自带的 app 窗口做不到这一点（客户区始终不透明），WebView2 可以。
//   2) 窗口背景设为 Transparent，并启用 Win11 的 DWM 亚克力（backdrop）。
//      这样窗口内容与桌面之间形成真实的模糊 + 透色，拖动时能隐约看到桌面。
//   3) 去掉系统边框，用 -webkit-app-region:drag 的顶部条拖动窗口。

using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace DeskWin
{
    internal static class Native
    {
        // ── Win11 亚克力相关 ──
        // DWMWA_SYSTEMBACKDROP_TYPE = 38
        //取值：2 = 主窗口亚克力（Mica/Acrylic），3 = 瞬态窗口亚克力，0 = 不使用
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        // DWMWA_BORDER_COLOR = 34，用于把系统边框设为透明
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref uint value, int size);

        // 让客户区延伸到窗口最外层，配合透明背景
        [DllImport("dwmapi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS m);

        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS { public int left, right, top, bottom; }

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr h, int i);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr h, int i, int v);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateRoundRectRgn(int l, int t, int r, int b, int w, int h);

        [DllImport("user32.dll")]
        public static extern int SetWindowRgn(IntPtr h, IntPtr rgn, bool redraw);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);

        public const int GWL_STYLE = -16;
        public const int WS_CAPTION = 0x00C00000;
        public const int WS_THICKFRAME = 0x00040000;
        public const int WS_MINIMIZEBOX = 0x00020000;
        public const int WS_SYSMENU = 0x00080000;
        public const int WS_EX_APPWINDOW = 0x00040000;

        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        public const int DWMWA_BORDER_COLOR = 34;
        public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        public const uint DWMWA_COLOR_NONE = 0xFFFFFFFE;

        /// <summary>应用无边框 + 亚克力 +圆角</summary>
        public static void ApplyFramelessAcrylic(Form f, int radius)
        {
            IntPtr h = f.Handle;

            // 去掉标题栏，保留边框能力（可缩放）与最小化/关闭
            int style = GetWindowLong(h, GWL_STYLE);
            SetWindowLong(h, GWL_STYLE, (style & ~WS_CAPTION) | WS_THICKFRAME | WS_MINIMIZEBOX | WS_SYSMENU);

            // 圆角窗口区域：圆角之外裁掉，桌面透出来
            var r = f.Bounds;
            IntPtr reg = CreateRoundRectRgn(0, 0, r.Width + 1, r.Height + 1, radius, radius);
            SetWindowRgn(h, reg, true);

            // 客户区延伸到最外层
            var m = new MARGINS { left = -1, right = -1, top = -1, bottom = -1 };
            DwmExtendFrameIntoClientArea(h, ref m);

            // 暗色模式（让内容用浅色文字）
            int dark = 1;
            DwmSetWindowAttribute(h, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));

            // 窗口圆角偏好：2 = 始终圆角
            int corner = 2;
            DwmSetWindowAttribute(h, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));

            // 边框颜色设为不可见
            uint noBorder = DWMWA_COLOR_NONE;
            DwmSetWindowAttribute(h, DWMWA_BORDER_COLOR, ref noBorder, sizeof(uint));

            // 亚克力背景：3 = 瞬态窗口亚克力（适合小工具窗，层次更明显）
            int backdrop = 3;
            int hr = DwmSetWindowAttribute(h, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
            System.Diagnostics.Debug.WriteLine("DwmSetWindowAttribute(SYSTEMBACKDROP) = " + hr);
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly WebView2 _web;
        private readonly string _pagePath;
        private int _radius = 22;

        public MainForm(string pagePath)
        {
            _pagePath = pagePath;

            FormBorderStyle = FormBorderStyle.None;
            // 注意：不要在构造期设 BackColor = Transparent。
              // WinForms 要求控件句柄创建后才允许透明底色，提前设会抛
              // "Control does not support transparent background colors."。
              // 改在 OnHandleCreated 里设。
            // 也不要TransparencyKey —— 它把「等于该颜色的像素」变镂空，
              // 深色面板一旦用到相近色就会被打穿。
            Size = new Size(1200, 740);
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;

            // 按显示器 DPI 换算圆角，避免高分屏下圆角过大
            _radius = (int)(22 * DeviceDpi / 96.0);

            // 透明背景只需把控件的 DefaultBackgroundColor 设为 Transparent。
            // （本 SDK 里没有 CoreWebView2ControllerOptions.IsTransparent，那套是更早的实验 API）
            _web = new WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = Color.Transparent,
            };
            Controls.Add(_web);

            Shown += OnShown;
            Resize += (s, e) =>
            {
                var r = Bounds;
                IntPtr reg = Native.CreateRoundRectRgn(0, 0, r.Width + 1, r.Height + 1, _radius, _radius);
                Native.SetWindowRgn(Handle, reg, true);
            };
        }

private async void OnShown(object sender, EventArgs e)
        {
            try
            {
                Program.Log("OnShown begin");
// 某些受限环境（沙箱 / 容器 / 部分虚拟机）里 Chromium 的渲染子进程会被拦掉，
    // 表现为 BrowserProcessExited / UtilityProcessExited 反复出现、页面全白。
       // 这里显式关掉 Chromium 自身的沙箱与 GPU 沙箱来兼容这类环境；
       // 在正常桌面上这些参数不影响亚克力效果。
      var envOptions = new CoreWebView2EnvironmentOptions(
          "--no-sandbox --disable-gpu-sandbox --disable-dev-shm-usage",
            null,   // language
     null,   // targetCompatibleBrowserVersion
            false,  // allowSingleSignOnUsingOSPrimaryAccount
       null);// customSchemeRegistrations

     var env = await CoreWebView2Environment.CreateAsync(null,
            Path.Combine(Path.GetTempPath(), "DeskWinWV2"), envOptions);
        Program.Log("env created (no-sandbox)");

     await _web.EnsureCoreWebView2Async(env);
   Program.Log("webview2 ensured");

     var c = _web.CoreWebView2;
                // 控件级透明 + 页面级透明，二者叠加才能让 DWM 亚克力透出来
                _web.DefaultBackgroundColor = System.Drawing.Color.Transparent;
                try { _web.BackColor = System.Drawing.Color.Transparent; } catch { }

                c.Settings.AreDefaultContextMenusEnabled = false;
                c.Settings.AreDevToolsEnabled = false;
                c.Settings.IsStatusBarEnabled = false;
                c.Settings.IsZoomControlEnabled = false;

                // 允许 file:// 下加载本地资源（RSS 等走https，不受影响）
                c.Settings.AreHostObjectsAllowed = true;

// 用 https 虚拟主机映射加载本地目录。
        // AccessKind 必须用 Allow —— Deny/DenyCors 会拦掉映射内的资源请求，
        // 表现为导航事件完全不触发。
   string dir = Path.GetDirectoryName(Path.GetFullPath(_pagePath));
      c.SetVirtualHostNameToFolderMapping(
            "app.local", dir, CoreWebView2HostResourceAccessKind.Allow);
        Program.Log("host mapping registered: " + dir);

        c.NavigationStarting += (s, e) =>
      {
          Program.Log("NavigationStarting uri=" + e.Uri);
        };
        c.NavigationCompleted += (s, e) =>
        {
Program.Log("NavigationCompleted ok=" + e.IsSuccess + " errStatus=" + e.WebErrorStatus);
     };
        c.ProcessFailed += (s, e) => Program.Log("ProcessFailed: " + e.ProcessFailedKind);

        // 页面就绪后再上亚克力，避免闪白
        c.NavigationCompleted += (s, args) =>
        {
 Program.Log("NavigationCompleted isSuccess=" + args.IsSuccess +
            " errStatus=" + args.WebErrorStatus);
       if (!args.IsSuccess) return;
        BeginInvoke(new Action(() =>
            {
            Native.ApplyFramelessAcrylic(this, _radius);
             Opacity = 1.0;
       }));
        };

        c.Navigate("https://app.local/index.html");

   // 兜底：即使导航事件没来，3 秒后也必须显示窗口，
        // 否则 Opacity=0 会让整个窗口隐形，看起来像"没启动"
        var timer = new System.Windows.Forms.Timer { Interval = 3000 };
        timer.Tick += (s, e) =>
        {
    timer.Stop();
       if (Opacity < 1.0)
        {
       Program.Log("fallback: forcing visible");
              BeginInvoke(new Action(() =>
   {
         Native.ApplyFramelessAcrylic(this, _radius);
   Opacity = 1.0;
     }));
         }
   };
        timer.Start();
            }
catch (Exception ex)
            {
 Program.Log("OnShown FAILED: " + ex);
       MessageBox.Show("启动失败：" + ex.Message, "桌面小窗",
        MessageBoxButtons.OK, MessageBoxIcon.Error);
   }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= Native.WS_EX_APPWINDOW;
                return cp;
            }
        }

        /// <summary>句柄建好之后才能开透明底色</summary>
        protected override void OnHandleCreated(EventArgs e)
        {
base.OnHandleCreated(e);
  try
          {
      BackColor = Color.Transparent;
     _web.BackColor = Color.Transparent;
       }
            catch { /* 透明不支持时退回普通不透明窗口，不影响功能 */ }

            // 客户区延伸到最外层，让 DWM 的亚克力覆盖整块窗口
            try
  {
        var mg = new Native.MARGINS { left = -1, right = -1, top = -1, bottom = -1 };
    Native.DwmExtendFrameIntoClientArea(Handle, ref mg);
            }
catch { }
        }
    }

    internal static class Program
    {
        // 启动期日志：WinExe 没有控制台，出错时只能落盘，方便排查
        private static readonly string LogPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup.log");

        internal static void Log(string msg)
        {
            try
            {
                File.AppendAllText(LogPath,
                    DateTime.Now.ToString("HH:mm:ss.fff") + "  " + msg + Environment.NewLine,
                    Encoding.UTF8);
            }
            catch { }
        }

        [STAThread]
        private static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Log("UNHANDLED: " + (e.ExceptionObject as Exception)?.ToString());
            };
            try
            {
                Log("--- launch ---");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

                string page = FindPage();
                Log("page = " + page + " exists=" + File.Exists(page));

                var f = new MainForm(page);
                Log("form constructed");
                f.Opacity = 0;
                Application.Run(f);
                Log("exited normally");
            }
            catch (Exception ex)
            {
                Log("FATAL: " + ex);
                MessageBox.Show("启动失败：" + ex.Message, "桌面小窗",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string FindPage()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            string p = Path.Combine(dir, "index.html");
            if (File.Exists(p)) return p;

            // 回退：与 exe 同级的上一级（开发时用）
            p = Path.Combine(dir, "..", "..", "桌面小窗", "index.html");
            if (File.Exists(p)) return Path.GetFullPath(p);

            MessageBox.Show("找不到 index.html，请确认它与 桌面小窗.exe 在同一目录。",
                "桌面小窗", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return "";
        }
    }
}