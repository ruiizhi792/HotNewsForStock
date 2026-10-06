$ErrorActionPreference = 'SilentlyContinue'

Add-Type @"
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public class WinDeco {
  public delegate bool EnumProc(IntPtr h, IntPtr l);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L,T,Rt,B; }
  [StructLayout(LayoutKind.Sequential)] public struct MARGINS { public int l,r,t,b; }

  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowTextW(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr h, int i);
  [DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr h, int i, int v);
  [DllImport("dwmapi.dll")] public static extern int DwmExtendFrameIntoClientArea(IntPtr h, ref MARGINS m);
  [DllImport("gdi32.dll")] public static extern IntPtr CreateRoundRectRgn(int l,int t,int r,int b,int w,int h);
  [DllImport("user32.dll")] public static extern int SetWindowRgn(IntPtr h, IntPtr rgn, bool redraw);

  public static List<IntPtr> Visible() {
    var l = new List<IntPtr>();
    EnumWindows((h,x) => { if (IsWindowVisible(h)) l.Add(h); return true; }, IntPtr.Zero);
    return l;
  }
  public static string Title(IntPtr h) {
    var sb = new StringBuilder(512);
    GetWindowTextW(h, sb, 512);
    return sb.ToString();
  }
  public static IntPtr FindByTitle(string want) {
    foreach (var h in Visible()) { if (Title(h) == want) return h; }
    return IntPtr.Zero;
  }
  public static void Apply(IntPtr h, int radius) {
    int GWL_STYLE = -16;
    int WS_CAPTION = 0x00C00000;
    int WS_THICKFRAME = 0x00040000;
    int style = GetWindowLong(h, GWL_STYLE);
    SetWindowLong(h, GWL_STYLE, (style & ~WS_CAPTION) | WS_THICKFRAME);

    MARGINS m = new MARGINS();
    m.l = -1; m.r = -1; m.t = -1; m.b = -1;
    DwmExtendFrameIntoClientArea(h, ref m);

    RECT r;
    GetWindowRect(h, out r);
    int w = r.Rt - r.L;
    int ht = r.B - r.T;
    IntPtr reg = CreateRoundRectRgn(0, 0, w + 1, ht + 1, radius, radius);
    SetWindowRgn(h, reg, true);
  }
}
"@

$TARGET_TITLE = [char]0x684C + [string][char]0x9762 + [char]0x5C0F + [char]0x7A97  # 桌面小窗
$RADIUS = 44
$state = @{ W = 0; H = 0 }

$hwnd = [IntPtr]::Zero
for ($i = 0; $i -lt 60; $i++) {
    $hwnd = [WinDeco]::FindByTitle($TARGET_TITLE)
    if ($hwnd -ne [IntPtr]::Zero) { break }
    Start-Sleep -Milliseconds 200
}
if ($hwnd -eq [IntPtr]::Zero) { exit 0 }

[WinDeco]::Apply($hwnd, $RADIUS)

$r = New-Object WinDeco+RECT
[void][WinDeco]::GetWindowRect($hwnd, [ref]$r)
$state.W = $r.Rt - $r.L
$state.H = $r.B - $r.T

# 尺寸变化时重设圆角，否则区域会被拉伸变形
$timer = New-Object System.Timers.Timer
$timer.Interval = 400
$timer.Add_Sub({
    $p = [WinDeco]::FindByTitle($TARGET_TITLE)
    if ($p -eq [IntPtr]::Zero) { $timer.Stop(); return }
    $rr = New-Object WinDeco+RECT
    [void][WinDeco]::GetWindowRect($p, [ref]$rr)
    $ww = $rr.Rt - $rr.L
    $hh = $rr.B - $rr.T
    if ($ww -ne $state.W -or $hh -ne $state.H) {
        $state.W = $ww
        $state.H = $hh
        $reg = [WinDeco]::CreateRoundRectRgn(0, 0, $ww + 1, $hh + 1, 44, 44)
        [void][WinDeco]::SetWindowRgn($p, $reg, $true)
    }
})
$timer.Start()