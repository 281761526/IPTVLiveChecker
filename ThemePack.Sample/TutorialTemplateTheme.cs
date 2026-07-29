using System;
using System.Collections.Generic;
using System.Drawing;
using IPTVLiveChecker;

namespace ThemePack.Sample;

/// <summary>
/// ╔══════════════════════════════════════════════════════════════════╗
/// ║                    DLL 主题开发完整教程模板                         ║
/// ║              覆盖：颜色 / 字体 / 动效 / 布局 / 高级逻辑              ║
/// ╚══════════════════════════════════════════════════════════════════╝
///
/// 【快速开始】
///   1. 创建类库项目（net472 + x64），引用主程序 IPTVLiveChecker.exe
///   2. 继承 AppTheme，在构造函数中赋值字段
///   3. 编译后将 DLL 放入 themes/ 目录，启动主程序即可看到新主题
///
/// 【字段总览】共 5 大类、35+ 个可自定义字段：
///   ┌─ 颜色（27 个） ─── 见下方第 1 节
///   ├─ 效果元数据（5 个）── 见下方第 2 节
///   ├─ 字体（2 个）───── 见下方第 3 节
///   ├─ 布局（2 个）───── 见下方第 4 节
///   └─ 高级逻辑 ──────── 见下方第 5 节（Initialize 虚方法）
/// </summary>
public class TutorialTemplateTheme : AppTheme
{
    public TutorialTemplateTheme()
    {
        // ╔══════════════════════════════════════════════════════════════╗
        // ║  第 1 节：颜色系统（27 个字段，全部可选）                       ║
        // ╚══════════════════════════════════════════════════════════════╝
        //
        // 颜色分 4 组：核心色板 / 功能色 / 按钮色 / 状态色
        // 使用 Color.FromArgb(R, G, B) 或 Color.FromArgb(A, R, G, B)（带透明度）

        // ---- 1.1 核心色板（决定界面整体观感）----
        Name = "教程模板";                    // 主题名称（菜单显示 + 持久化标识，必须唯一）

        Primary = Color.FromArgb(100, 149, 237);    // 主色调（标题、强调元素）  CornflowerBlue
        PrimaryDark = Color.FromArgb(80, 120, 200); // 主色深色变体（悬停态）
        Accent = Color.FromArgb(255, 130, 80);      // 强调色（点缀、活跃指示）

        Bg = Color.FromArgb(248, 249, 252);          // 主背景色
        BgAlt = Color.FromArgb(240, 242, 248);       // 交替背景色（条纹、次要区域）
        Surface = Color.FromArgb(235, 238, 245);     // 卡片/面板表面色
        Border = Color.FromArgb(210, 215, 225);      // 边框色

        TextPrimary = Color.FromArgb(35, 40, 50);    // 主文字色
        TextSecondary = Color.FromArgb(110, 118, 130);// 次要文字色

        HeaderBg = Color.FromArgb(232, 236, 244);    // 表头/标题栏背景
        SelectRow = Color.FromArgb(220, 230, 248);   // 选中行背景
        SelectRowText = Color.FromArgb(35, 40, 50);  // 选中行文字
        StatusBarBg = Color.FromArgb(238, 240, 246); // 状态栏背景

        // ---- 1.2 功能色（状态指示、提示信息）----
        TipBg = Color.FromArgb(235, 240, 250);       // 提示框背景
        LinkTextColor = Color.FromArgb(50, 100, 180);// 链接文字色

        SuccessColor = Color.FromArgb(60, 160, 75);  // 成功（绿色）
        ErrorColor = Color.FromArgb(220, 70, 70);    // 错误（红色）
        WarnColor = Color.FromArgb(230, 160, 40);    // 警告（橙色）
        InfoColor = Color.FromArgb(60, 130, 200);    // 信息（蓝色）

        // ---- 1.3 按钮色（开始检测 / 复制 等操作按钮）----
        PlayBtnBg = Color.FromArgb(60, 160, 75);     // "开始检测"按钮背景
        PlayBtnText = Color.White;                   // 按钮文字色
        CopyBtnBg = Color.FromArgb(60, 130, 200);    // "复制"按钮背景
        CopyBtnText = Color.White;

        // ---- 1.4 状态标签色（数据网格中的状态徽章）----
        StatusTagBg = Color.FromArgb(235, 240, 248); // 状态标签背景
        StatusTagBorder = Color.FromArgb(60, 130, 200);// 状态标签边框

        // ╔══════════════════════════════════════════════════════════════╗
        // ║  第 2 节：动效元数据（5 个字段，控制毛玻璃 + 动画背景）          ║
        // ╚══════════════════════════════════════════════════════════════╝
        //
        // 毛玻璃和动画背景是独立的，可以单独启用或组合使用。

        // ---- 2.1 毛玻璃效果 ----
        GlassEnabled = true;          // 启用毛玻璃（半透明面板）
        GlassOpacity = 200;           // 不透明度 0-255（210=默认，越低越透明）
        GlassBlur = true;             // 启用 DWM 模糊（需要 Win10 1803+）

        // ---- 2.2 动画背景 ----
        // 5 种内置动画类型：
        //   "aurora"  - 极光流动（默认，多色渐变缓慢漂移）
        //   "neon"    - 霓虹脉冲（亮色光斑跳动）
        //   "pulse"   - 心跳脉冲（单色明暗呼吸）
        //   "fluid"   - 流体扩散（液态色彩蔓延）
        //   "breath"  - 呼吸渐变（柔和明暗交替）
        AnimationType = "aurora";
        AnimationSpeed = 1.2;         // 速度因子（0.5=慢, 1.0=默认, 2.0=快）

        // 渐变色斑颜色列表（动画背景会在这几个颜色之间过渡）
        // 透明度 Alpha 控制色斑浓度（建议 40-90，太高会遮挡控件）
        GradientStops = new List<Color>
        {
            Color.FromArgb(60, 100, 149, 237),  // 蓝
            Color.FromArgb(60, 130, 200, 180),  // 青
            Color.FromArgb(60, 200, 130, 220),  // 紫
            Color.FromArgb(60, 255, 180, 100)   // 橙
        };

        // ╔══════════════════════════════════════════════════════════════╗
        // ║  第 3 节：字体自定义（2 个字段）                                ║
        // ╚══════════════════════════════════════════════════════════════╝
        //
        // 留空/null 则继承用户在设置中选择的字体。
        // 设置后仅影响当前主题，切换其他主题时自动恢复。

        this.FontFamily = "Segoe UI";      // 字体族（"Microsoft YaHei", "Segoe UI", "Consolas" 等）
        this.FontScale = 1.05;             // 字号缩放（1.0=原始, 1.1=放大10%, 0.9=缩小10%）

        // ╔══════════════════════════════════════════════════════════════╗
        // ║  第 4 节：布局参数（2 个字段，微调圆角和间距）                   ║
        // ╚══════════════════════════════════════════════════════════════╝
        //
        // 这些是全局缩放因子，影响所有控件。

        this.CornerRadius = 8;             // 圆角半径（0=直角, 6=默认, 12=大圆角）
        this.SpacingScale = 1.1;           // 间距缩放（1.0=默认, 1.2=更宽松, 0.8=更紧凑）
    }

    // ╔══════════════════════════════════════════════════════════════════╗
    // ║  第 5 节：高级逻辑（Initialize 虚方法）                           ║
    // ║  ────────────────────────────────────────────────────────────────║
    // ║  在主程序加载 DLL 时，实例化后会自动调用此方法。                    ║
    // ║  适合做：动态派生颜色 / 条件判断 / 读取外部资源                    ║
    // ╚══════════════════════════════════════════════════════════════════╝
    public override void Initialize()
    {
        // ---- 5.1 动态派生颜色 ----
        // 根据 Primary 自动计算 PrimaryDark（加深 25%）
        PrimaryDark = DarkenColor(Primary, 0.75);

        // ---- 5.2 根据系统主题自适应 ----
        // 如果系统是暗色模式，切换为暗色背景
        if (IsSystemDarkTheme())
        {
            Bg = Color.FromArgb(30, 34, 42);
            BgAlt = Color.FromArgb(36, 40, 50);
            Surface = Color.FromArgb(42, 46, 56);
            TextPrimary = Color.FromArgb(230, 235, 245);
            TextSecondary = Color.FromArgb(160, 170, 185);
            HeaderBg = Color.FromArgb(38, 42, 52);
        }

        // ---- 5.3 根据时间调整氛围 ----
        int hour = DateTime.Now.Hour;
        if (hour >= 22 || hour < 6)
        {
            // 深夜模式：降低动画速度，减少刺激
            AnimationSpeed = 0.5;
            GradientStops = new List<Color>
            {
                Color.FromArgb(30, 60, 80, 120),
                Color.FromArgb(30, 80, 60, 100)
            };
        }
    }

    /// <summary>把颜色按比例变暗（factor &lt; 1 变暗，&gt; 1 变亮）。</summary>
    private static Color DarkenColor(Color c, double factor)
    {
        return Color.FromArgb(
            Math.Min(255, (int)(c.R * factor)),
            Math.Min(255, (int)(c.G * factor)),
            Math.Min(255, (int)(c.B * factor)));
    }
}
