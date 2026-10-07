# Desktop Mini Window · Today's Tendency + Doodle K-Line

**English** · [中文](#中文说明)

A double-click mini app for Windows. Nothing to install, no address bar, no browser tabs —
just a standalone little window. The whole thing is a single HTML file (~44 KB), and the
drawing board keeps working offline.

![Main window](docs/screenshot-main.png)

## Quick start

1. Download or clone this repo
2. Double-click **`桌面小窗/启动.vbs`** (Launch.vbs)

That's it. Windows 10/11 ships with Edge, so there's no runtime to install.

## What's inside the window

The window is split into two halves.

![Doodle K-line](docs/screenshot-kline.png)

### Left pane — Today's Tendency

A minimal Google-homepage-style search. Either type a tendency (e.g. `AI chips`,
`Fed rate`, `China ADRs`) and hit Enter, or click the default button below to get
today's financial headlines. Both paths feed the same pipeline.

Every item shows:

- The headline, plus source and time
- A one-or-two-sentence **AI comment** on what the news is and why it's worth noting
- A stance tag: `Bullish` (red) / `Bearish` (green) / `Neutral` (grey) — red-up/green-down,
  following the Chinese market convention
- The fixed line *"AI generated · for reference only · not investment advice"*

### Right pane — Doodle K-Line

Three controls, and only three: **Red pen / Green pen / Clear**.

- Drag to draw — candles grow in **real time**, before you release the mouse
- Red pen draws red candles, green pen draws green ones. Redrawing over the same area
  overwrites it, so you can draw "rally then pullback" by going red first, then green
- Areas you don't draw on stay blank
- Highest and lowest prices are labelled; the overall change is shown at the top right;
  a price scale runs down the right edge
- `Ctrl+Z` undoes the last stroke

This pane is **fully offline** — no market data API, all prices are synthesised locally.

## Where the data comes from

Four public RSS feeds: Yahoo Finance, CNBC, MarketWatch, Investing.com.

Each source is fetched by trying a direct request first, then a CORS proxy, then giving up on
that source without blocking the others. Every request has a 10-second timeout. If all four
fail, the app falls back to built-in sample data and clearly labels it
*"Sample data · network restricted"* — it never shows a blank screen.

![Offline fallback](docs/screenshot-offline.png)

After merging, items are deduplicated by title similarity and filtered to the last 24 hours
(relaxed to 48 hours if that yields fewer than 10). The top 10 are then ranked by a purely
local heat score:

```
heat = cross-source appearances × 3 + source-position weight + recency
```

Heat is computed locally and costs nothing. Only in **custom tendency mode** does the app
make one extra model request, asking the model to score every candidate 0–100 for relevance,
then taking the top 10.

## Do I need an API key?

Not to use it. The news list renders fine without one — the comment area just shows
*"No AI configured (click the gear icon at top right)"*.

![Settings dialog](docs/screenshot-settings.png)

To get comments, click the gear icon and fill in three fields: API endpoint, API key, model
name. The key is stored **only** in your own browser's localStorage, is sent only in the
`Authorization` header, and is never written into the code, the logs, or any endpoint other
than the one you configured.

Comments are generated with **a single batched request for all 10 items** — never one API
call per headline.

## Project layout

```
桌面小窗/  (Desktop Mini Window/)
├── 桌面小窗.exe        DesktopMiniWindow.exe   WebView2 host — real Win11 acrylic
├── index.html                            all UI and logic, single file, zero dependencies
├── 启动.vbs             Launch.vbs          double-click to open (prefers the exe)
├── 窗口修饰.ps1         WindowStyling.ps1    Win32 styler for the fallback path
└── 使用说明.txt          ReadMe.txt            brief usage notes
```

There are two launch paths, and the launcher picks automatically:

1. **`桌面小窗.exe` (preferred)** — a small C#/WebView2 host. This is what gives you **real
   Windows 11 acrylic**: a translucent, frameless, rounded window where the desktop is
   faintly visible through it while you drag.
2. **Edge `--app` mode (fallback)** — if you delete the exe, `启动.vbs` falls back to opening
   the page in an Edge app window (no address bar, no tabs) and runs a small Win32 script
   to remove the title bar and round the corners. No translucency in this mode.

Not Electron or Tauri (those need a Node/Rust toolchain and produce builds hundreds of
megabytes), and not `.hta` (IE engine — no modern JS, canvas, or fetch).

### How the acrylic is done

`桌面小窗.exe` is a ~150-line C# WinForms host around **WebView2**:

- `DefaultBackgroundColor = Transparent` on the control, plus `DwmExtendFrameIntoClientArea(-1)`
  so the page composites against the window background rather than an opaque layer.
- The page is loaded through `SetVirtualHostNameToFolderMapping` as `https://app.local/`
  rather than `file://`. This puts it in a normal secure context, which is what lets the RSS
  proxy calls and `localStorage` behave per spec.
- `DwmSetWindowAttribute` with `DWMWA_SYSTEMBACKDROP_TYPE = 3` turns on the transient-window
  acrylic; `WS_CAPTION` is stripped and a rounded window region is applied.
- Chromium's own sandbox is disabled (`--no-sandbox`) so the renderer also starts inside
  containers and locked-down environments.

The WebView2 **runtime** is already present on any Windows 10/11 machine with Edge installed,
so nothing extra is installed at runtime — the size is just the bundled .NET runtime.

One honest caveat about the fallback path: **a plain Chromium/Edge app window always paints an
opaque client area.** Making the page background transparent in CSS has no effect there — this
was verified by pixel-sampling a real window on screen. So in fallback mode what you get is the
*window shape* cut away at the corners plus a CSS gradient border, not per-pixel transparency.
Acrylic genuinely requires the WebView2 host.

## Known limitations

- Relies on a public CORS proxy. Public proxies are flaky, so failures automatically retry
  the next endpoint; if all fail, the app falls back to sample data. You may see
  browser-level CORS errors in devtools in that case — there will be no script errors and no
  blank screen.
- RSS headlines are mostly English. With an AI key configured they get translated to
  Chinese; without one they show as-is.
- Acrylic requires Windows 11 (build 22000+ / 22H2). On Windows 10 the host still runs and the
  window is frameless and rounded, but the backdrop falls back to a plain translucent tint.
- Verified on Windows 11 25H2 only. Other browsers are untested.
- The fallback path's styling needs PowerShell (built into Windows). If it can't run, the app
  still opens — just with a normal title bar.

## Disclaimer

This project is a personal learning/entertainment desktop toy. All AI-generated comments and
stance tags are produced automatically and are **for reference only — not investment advice**.
Act at your own risk.

---

<a name="中文说明"></a>

## 中文说明

一个 Windows 上双击即用的小程序。不用装任何东西，不出现浏览器地址栏和标签页，
就是一个独立的小窗口。体量很小（单文件 HTML，约 44 KB），断网也能玩。

### 怎么用

1. 下载或克隆本仓库
2. 双击 **`桌面小窗/启动.vbs`**

就这两步。Win10/11 出厂自带 Edge，不需要额外安装任何运行时。

### 窗口里有什么

左右分栏，各占一半。

**左栏 · 今日倾向**

谷歌主页式的极简搜索。要么直接写一个倾向（比如「AI 芯片」「美联储利率」「中概股」），
要么点默认按钮取当日财经热榜。两条路进的是同一套管线。

每条新闻带中文标题、来源与时间、AI 评语、倾向标签（`看多`红 / `看空`绿 / `中性`灰，
按国内习惯红涨绿跌），以及固定小字「AI 生成 · 仅供参考 · 不构成投资建议」。

**右栏 · 涂鸦 K 线**

三个控件：红笔 / 绿笔 / 清空。

- 按住拖动，拖动过程中蜡烛实时长出来
- 红笔长红蜡烛，绿笔长绿蜡烛；同一区域重画会覆盖，可先红后绿画出「上涨后回调」
- 没画的区域留空
- 标注最高价与最低价，右上角显示整段涨跌幅，右缘是价格刻度
- `Ctrl+Z` 撤销上一笔

这部分**完全离线**，不接任何行情接口，价格都是本地合成的。

### 数据从哪来

内置 Yahoo Finance、CNBC、MarketWatch、Investing.com 四个公开 RSS 源。
每个源按「直连 → CORS 代理 → 放弃该源」抓取，10 秒超时；四个源全挂时退回内置示例数据，
界面标注「示例数据 · 网络受限」，不会白屏。

合并后按标题相似度去重，只保留最近 24 小时（不足 10 条放宽到 48 小时），
再按本地热度排序取前 10：`热度 = 跨源出现次数 × 3 + 源内位置权重 + 时间新近度`。
热度纯本地计算，不花模型的钱；只有自定义倾向模式才会额外发一次模型请求打相关度。

结果按「日期 + 模式」缓存进 localStorage，当天重复打开直接出结果。

### AI 评语要不要配 Key

不配也能用，评语位置显示「未配置 AI（点右上角齿轮设置）」。
配了之后评语是**一次批量请求处理 10 条**，不会一条新闻调一次 API。
Key 只存在本机 localStorage，只在请求时放进 `Authorization` 头。

### 无边框亚克力是怎么做的

窗口无边框、半透明、圆角，拖动时能隐约看到桌面。这是靠 `桌面小窗.exe`
（一个约 150 行的 C# + WebView2 宿主）实现的：

- 控件 `DefaultBackgroundColor = Transparent`，配合 `DwmExtendFrameIntoClientArea(-1)`，
  让页面与窗口背景合成而不是盖一层不透明底
- 页面通过 `SetVirtualHostNameToFolderMapping` 映射为 `https://app.local/` 加载，
  而不是 `file://`。这样页面处在正常安全上下文，RSS 代理请求与 localStorage
  才会按标准行为工作
- `DwmSetWindowAttribute` 设 `DWMWA_SYSTEMBACKDROP_TYPE = 3` 开启瞬态窗口亚克力，
  同时去掉 `WS_CAPTION` 并给窗口区域打圆角
- 关掉 Chromium 自身的沙箱（`--no-sandbox`），让它在受限环境里也能起渲染进程

WebView2 **运行时**在装了Edge 的 Win10/11 上本来就有了，运行时不需额外安装，
体积大只是因为把 .NET 运行时打进了单文件。

如果删掉 exe，启动器会自动退回 Edge `--app` 模式：无地址栏、无标签页、圆角，
但**没有半透明亚克力**——因为纯 Chromium app 窗口的客户区始终是不透明的
（这一点用真实窗口做过像素采样验证），CSS 里的 `background: transparent` 对它无效。

### 已知限制

- 依赖公共 CORS 代理，代理时好时坏，失败会自动换端点重试；全挂则走示例数据。
  这种情况下 F12 里可能看到浏览器层面的 CORS 报错，但不会有脚本报错，也不会白屏。
- RSS 标题多为英文，配置 AI 后会被翻成中文；没配则显示原文。
- 亚克力需要 Windows 11（build 22000+ / 22H2）。Win10 上窗口仍是无边框圆角，
  但背景会退化成普通半透明色。
- 只验证了 Windows 11 25H2，其他浏览器没测。
- 退回Edge 模式时的修饰依赖 PowerShell（Windows 自带）；若无法执行，程序仍能打开，
  只是会带普通标题栏。

### 免责声明

本项目仅为个人学习与娱乐用途的桌面小工具。所有 AI 生成的评语与倾向标签均为程序自动生成，
**仅供参考，不构成任何投资建议**。据此操作，风险自负。