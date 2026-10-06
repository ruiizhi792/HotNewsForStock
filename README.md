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
├── index.html                  all UI and logic, single file, zero dependencies
├── 启动.vbs      Launch.vbs        double-click to open the app
├── 窗口修饰.ps1  WindowStyling.ps1 strips the title bar, applies rounded corners
└── 使用说明.txt   ReadMe.txt          brief usage notes
```

The app is a local HTML page launched in Edge's `--app` mode. Not Electron or Tauri (those
need a Node/Rust toolchain and produce builds hundreds of megabytes in size), and not `.hta`
(IE engine — no modern JS, canvas, or fetch).

### A note on the frameless gradient border

The window is frameless and its corners are genuinely transparent — the desktop shows
through the rounded corners, and the border fades outward. Getting there required a small
Win32 helper (`WindowStyling.ps1`), which strips `WS_CAPTION` and applies a rounded window
region via `SetWindowRgn`.

One honest caveat: **Chromium app windows always paint an opaque client area.** Making the
page background itself transparent (`background: transparent` in CSS) has no effect — this
was verified by pixel-sampling a real window on screen. So what you see is the *window shape*
being cut away at the corners, not per-pixel page transparency. The CSS gradient border
supplies the fade-from-inside look. A true acrylic/mica blur would need Electron or Tauri,
which would break the "one small file, no toolchain" constraint.

## Known limitations

- Relies on a public CORS proxy. Public proxies are flaky, so failures automatically retry
  the next endpoint; if all fail, the app falls back to sample data. You may see
  browser-level CORS errors in devtools in that case — there will be no script errors and no
  blank screen.
- RSS headlines are mostly English. With an AI key configured they get translated to
  Chinese; without one they show as-is.
- Verified on Windows + Edge only. Other browsers are untested.
- The frameless styling needs PowerShell (built into Windows). If it can't run, the app
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

### 无边框渐变边框是怎么做的

窗口无边框，四角是真透明的——桌面从圆角处透出来，边缘向外渐隐。
靠的是一个小的 Win32 辅助脚本（`窗口修饰.ps1`）：去掉 `WS_CAPTION`，再用
`SetWindowRgn` 把窗口区域切成圆角。

需要说明一点：**Chromium 的 app 窗口客户区始终是不透明的**，
页面里写 `background: transparent` 不生效（这一点用真实窗口做过像素采样验证）。
所以透出桌面的是「窗口形状被裁掉」的部分，不是页面像素级透明；
由内向外的渐隐观感由 CSS 的渐变描边负责。真正的亚克力/毛玻璃效果需要 Electron 或
Tauri，那会破坏「单个小文件、不装工具链」这个前提。

### 已知限制

- 依赖公共 CORS 代理，代理时好时坏，失败会自动换端点重试；全挂则走示例数据。
  这种情况下 F12 里可能看到浏览器层面的 CORS 报错，但不会有脚本报错，也不会白屏。
- RSS 标题多为英文，配置 AI 后会被翻成中文；没配则显示原文。
- 只验证了 Windows + Edge，其他浏览器没测。
- 无边框效果依赖 PowerShell（Windows 自带）。若无法执行，程序仍能打开，只是会带普通标题栏。

### 免责声明

本项目仅为个人学习与娱乐用途的桌面小工具。所有 AI 生成的评语与倾向标签均为程序自动生成，
**仅供参考，不构成任何投资建议**。据此操作，风险自负。