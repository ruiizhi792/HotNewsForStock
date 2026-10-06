' 桌面小窗 启动器
' 用 Edge 的 --app 模式打开本地页面：无地址栏、无标签页，观感就是一个独立小程序。
' Win10/11 出厂自带 Edge，无需任何安装。找不到 Edge 时退回默认浏览器。
Option Explicit
Dim shell, fso, page, edge, candidates, c, i, envPath
Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
page = "file:///" & Replace(fso.GetParentFolderName(WScript.ScriptFullName), "\", "/") & "/index.html"

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
  ' --window-size 指定初始窗口尺寸；文件本身用视口自适应，窗口可自由缩放
  shell.Run """" & edge & """ --app=""" & page & """ --window-size=1180,720", 1, False
End If