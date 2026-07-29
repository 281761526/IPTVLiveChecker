# WebView2 白屏修复说明（2026-07-27）

## 现象
Web 搜索窗口能正常打开，但内容区域空白，且**全程没有报错弹窗**。

## 根因（事实 / 推测）

### 事实 1 —— 初始化失败被静默吞掉（主因）
`WebView2InitCompletedHandler`（原 `IPTVLiveCheckerMain.cs:5288`）只判断 `CoreWebView2` 是否为 null，
**完全没有读取事件参数的 `IsSuccess`**。

关键点：`WebView2.EnsureCoreWebView2Async()` 在**内核初始化失败时不抛异常**，
失败信息只通过 `CoreWebView2InitializationCompleted` 事件的 `IsSuccess` / `InitializationException` 参数传递。
因此：
1. 初始化失败 → `CoreWebView2 == null` → 处理器 `return`，**无任何提示**；
2. `dlg.Load` 里 `await` 之后看到 `webViewPendingUrl` 仍非空，于是对 **为 null 的内核** 设置 `Source` 属性
   （原 `:12793`），该导航被 WebView2 静默丢弃 → **白屏，无报错**。

最常见的触发场景：**本机未安装 / 被禁用 Microsoft Edge WebView2 Runtime**。
注意 `IsWebView2Supported()` 只检查 Edge 浏览器、loader、注册表，**并不保证 `EnsureCoreWebView2Async()` 一定成功**。

### 事实 2 —— 导航双路径竞争
原代码在事件处理器里 `core.Navigate(...)`（原 `:5308`）**和** `dlg.Load` 里 `Source=`（原 `:12793`）
两处都会触发导航，属于不必要的重复/竞争。

### 推测（环境相关，需本机验证，非代码缺陷）
窗体 `SetRoundedRegion()` 设置的非矩形 `Region` 可能在部分 WebView2 版本下干扰其独立合成层渲染。
若下列修复后仍空白，请按文末排查步骤验证。

## 修复内容
文件：`IPTVLiveChecker\IPTVLiveCheckerMain.cs`（已通过 `dotnet build` 编译，**0 警告 0 错误**）

1. **`WebView2InitCompletedHandler`**：通过反射读取 `IsSuccess` 与 `InitializationException`；
   初始化失败时显式弹窗提示原因（不再静默），并移除其中的重复导航。
2. **`dlg.Load` 异步处理**：`await EnsureCoreWebView2Async()` 之后，统一在 `CoreWebView2 != null` 时导航
   （优先 `CoreWebView2.Navigate`，无该方法时兜底用 `Source` 属性）；
   内核仍为 null 时只清理待导航标记，错误已由初始化处理器报出。

## 本机验证步骤（必须在 Windows 桌面执行，本沙箱为 Linux 无法运行 GUI）
1. 关闭正在运行的 IPTV 检测程序（解除 `bin\Debug` exe 文件锁）；
2. `dotnet build IPTVLiveChecker.csproj -c Debug`；
3. 双击 `bin\Debug\net472\IPTVLiveChecker.exe`，打开 Web 搜索窗口：
   - 若弹“WebView2 内核初始化失败”→ 安装/修复 **Microsoft Edge WebView2 Runtime**
     （官方地址：https://developer.microsoft.com/microsoft-edge/webview2/ ）；
   - 若正常加载 FOFA 等站点 → 修复生效；
   - 若仍空白但无报错 → 临时注释 `SetRoundedRegion()` 调用，重启验证是否为圆角 Region 干扰渲染。
