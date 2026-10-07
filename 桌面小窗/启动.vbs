' 桌面小窗 —— 启动器
'
' 优先走「桌面小窗.exe」（WebView2 宿主，有真正的 Win11 亚克力：
' 窗口无边框、圆角、内容半透明，拖动时能隐约透出桌面）。
' 若 exe 缺失或启动失败，自动退回 Edge --app 模式（无地址栏、无标签页），
' 并调 PowerShell 去标题栏 + 打圆角，作为不依赖工具链的备用方案。

Option Explicit
Dim shell, fso, baseDir, exe, edge, candidates, c, i, envPath, ps, page, cmd

Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")

baseDir = fso.GetParentFolderName(WScript.ScriptFullName)
page = "file:///" & Replace(baseDir, "\", "/") & "/index.html"
exe = baseDir & "\桌面小窗.exe"

' ── 方案一：WebView2 宿主（亚克力）──
If fso.FileExists(exe) Then
  On Error Resume Next
  shell.Run """" & exe & """", 1, False
  If Err.Number = 0 Then
    WScript.Quit 0
  End If
  On Error GoTo 0
End If

' ── 方案二：退回 Edge --app 模式 ──
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
  ' 连 Edge 都没有：直接用系统默认浏览器打开，程序仍可用
  shell.Run """" & Replace(page, "file:///", "") & """", 1, False
Else
  ' --app 模式：无地址栏、无标签页、独立窗口
  shell.Run """" & edge & """ --app=""" & page & """ --window-size=1200,740", 1, False
End If

' 等页面窗口出现后，去标题栏并打圆角（隐藏运行，不闪黑窗）
WScript.Sleep 1400
ps = shell.ExpandEnvironmentStrings("%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe")
decorator = baseDir & "\窗口修饰.ps1"
If fso.FileExists(ps) And fso.FileExists(decorator) Then
  cmd = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File """ & decorator & """"
  shell.Run """" & ps & """ " & cmd, 0, False
End If