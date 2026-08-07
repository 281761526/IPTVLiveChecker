# IPTV Live Checker - 项目长期记忆

## 项目概况
- WinForms (.NET Framework 4.7.2) IPTV 直播源检测工具
- 主代码文件：`IPTVLiveCheckerMain.cs`（超大文件，约 2 万+行）
- 构建命令：`dotnet build IPTVLiveChecker.csproj -c Release`
- 产物路径：`bin/Release/net472/IPTVLiveChecker.exe`
- 重要：工作区有后台进程会把**未提交**的代码改动反复回退到已提交版本（HEAD）。本回合曾因此整段丢失底部栏代码。对策：编辑 `IPTVLiveCheckerMain.cs` 后**立即 `git commit`**，让 HEAD 包含改动，回退即等于保留；不要留未提交改动。
- 编辑方式：单点小改可直接用 Edit 工具（每次编辑前需重新 Read 刷新快照）；大段或多处改动建议用 Python 脚本一次性 read→modify→write 原子完成，再紧接着 commit，把回退窗口缩到最小。

## 底部筛选栏（最终实现，2026-08-07~08 重写）
- 字段：`_filterBar`(DockStyle.Bottom 面板)、`_filterStatusAvail/_filterStatusUnavail/_filterLatencyLow/_filterLatencyHigh`(bool，默认 **false**)、`_filterRes`(bool[5]，默认全 false)、`_lblFilterCount`(右对齐计数标签)
- 构建方法：`CreateFilterBar()`，内部局部函数 `MakeFilterChip(text,initial,onToggle)`
- **Label 宽度截断修复（重要教训）**：**不要用 `lbl.Width` 来计算容器尺寸** —— Label.AutoSize=true 在 .NET Framework 4.7.2 下 Width 是延迟到首次 Layout 才更新，构造时取到的值是默认宽度（导致标签文字被父 Panel 裁剪）。必须用 `TextRenderer.MeasureText(g, text, font)` 精确测量后**显式设置** `lbl.Size`（AutoSize=false）与 `host.Size`
- **没有**底部独立的"合并导出"按钮；筛选选择绑定顶部工具栏现有 `BtnExport_Click`
- 筛选逻辑：`PassesBottomFilter(ch)` 三维判断（某维全不勾选=该维不过滤）；`GetFilteredChannels()` 收集结果
- 生效点：`RefreshGrid()` 网格应用筛选（在原 group/search 基础上叠加）；`BtnExport_Click` 用 try/finally 临时把 `allChannels` 切换为筛选结果再调用各 Export 方法（export 方法都是 `new List<ChannelInfo>(allChannels)` 拷贝读取）
- 默认"全不勾选 = 不过滤，显示/导出全部"
- 布局挂载（2026-08-08 修正）：新建 `bottomContainer`(Panel, DockStyle.Bottom, Height=SY(32)+SY(34)) 加入 mainArea；`statusBarContainer`(DockStyle.Top, SY32) 与 `_filterBar`(DockStyle.Bottom, SY34) 都作为其子控件——**筛选栏位于最底部**，状态栏在其上方。高度 SY38→SY34（芯片 SY30→SY26）以与状态栏视觉一致。
- 注意：WinForms 多控件同边 docking 顺序易出歧义，用嵌套固定高度容器可彻底消除。

## 状态栏进度条
- `statusBarRef.Paint` 绘制全宽绿色渐变填充
- 五个状态标签设为 Visible=false，文字由 Paint 统一绘制以避免透明背景挖洞
- `UpdateLabelColorsBasedOnProgress` 控制文字在填充区域的对比色

## 技术约束
- 编译验证：0 警告 0 错误为通过标准（Release 构建）
- `ChannelInfo` 字段：`Name/Url/Location/Resolution/Speed/Group/Status`（均为 string）；`Speed` 形如 `"250ms"` 或 `"超时"`，`Resolution` 形如 `"1920x1080"` 或 `"直播"`
