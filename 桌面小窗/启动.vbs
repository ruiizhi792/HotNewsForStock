' 桌面小窗 —— 无边框 / 渐变透明启动器
'
' 做两件事：
'   1) 用 Edge --app 模式打开本地页面（无地址栏、无标签页）
'   2) 调 PowerShell 去掉系统标题栏，并把窗口区域切成圆角
'      —— 四角之外是真正的桌面，小窗看起来「浮」在桌面上、边缘渐隐。
'
' 说明：Chromium 的 app 窗口客户区本身始终不透明，
'       所以能透出桌面的是「窗口形状（圆角）」而不是页面像素；
'       页面内的渐变描边负责由内向外渐隐的观感。
Option Explicit
Dim shell, fso, page, edge, candidates, c, i, envPath, ps, baseDir, cmd
Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")

baseDir = fso.GetParentFolderName(WScript.ScriptFullName)
page = "file:///" & Replace(baseDir, "\", "/") & "/index.html"

' 候选路径：优先 64 位，再 32 位，最后当前用户安装目录
envPath = shell.ExpandEnvironmentStrings("%LOCALAPPDATA%\Microsoft\Edge\Application\msedge.exe")
candidates = Array( _
  "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe", _
  "C:\Program Files\Microsoft\Edge\Application\msedge.exe", _
  envPath )

edge = ""
For i = 0 To UBound(candidates)
  c = candidates(i)
  If edge = "" And fso.FileExists(c) Then edge = c
Next

If edge = "" Then
  ' 找不到 Edge 时退回默认浏览器（体验降级但仍可用）
  shell.Run """" & Replace(page, "file:///", "") & """", 1, False
Else
  ' --app 模式：无地址栏、无标签页、独立窗口 —— 小程序观感的关键
  shell.Run """" & edge & """ --app=""" & page & """ --window-size=1200,740", 1, False
End If

' 等页面窗口出现后，再去标题栏并打圆角（隐藏运行，不闪黑窗）
WScript.Sleep 1400
ps = shell.ExpandEnvironmentStrings("%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe")
If fso.FileExists(ps) Then
  cmd = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File """ & baseDir & "\窗口修饰.ps1"""
  shell.Run """" & ps & """ " & cmd, 0, False
End If