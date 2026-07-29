using System.Collections.Generic;
using System.Drawing;
using IPTVLiveChecker;

namespace ThemePack.Sample;

/// <summary>
/// 霞光暖橙主题示例（亮色 + 呼吸动画背景）。
/// 演示 <see cref="Initialize"/> 虚方法的用法：把可派生的颜色放在初始化阶段计算，
/// 而不是在构造函数里硬编码——这样可以根据系统状态、时间等条件动态决定色值。
/// </summary>
public class SunsetGlowTheme : AppTheme
{
    public SunsetGlowTheme()
    {
        Name = "霞光暖橙";

        // 基础色板（构造函数中只写确定值）
        Primary = Color.FromArgb(210, 110, 50);
        Accent = Color.FromArgb(120, 80, 180);
        Bg = Color.FromArgb(252, 242, 228);
        BgAlt = Color.FromArgb(247, 233, 213);
        Surface = Color.FromArgb(242, 225, 200);
        Border = Color.FromArgb(220, 198, 165);
        TextPrimary = Color.FromArgb(48, 32, 20);
        TextSecondary = Color.FromArgb(120, 95, 70);
        HeaderBg = Color.FromArgb(245, 230, 205);
        SelectRow = Color.FromArgb(235, 218, 188);
        SelectRowText = Color.FromArgb(48, 32, 20);
        StatusBarBg = Color.FromArgb(240, 224, 196);

        TipBg = Color.FromArgb(242, 225, 200);
        PlayBtnBg = Color.FromArgb(210, 110, 50);
        PlayBtnText = Color.White;
        CopyBtnBg = Color.FromArgb(80, 130, 195);
        CopyBtnText = Color.White;
        StatusTagBg = Color.FromArgb(238, 222, 195);
        StatusTagBorder = Color.FromArgb(210, 110, 50);
        LinkTextColor = Color.FromArgb(60, 100, 160);
        SuccessColor = Color.FromArgb(80, 160, 90);
        ErrorColor = Color.FromArgb(200, 60, 60);
        WarnColor = Color.FromArgb(210, 130, 40);
        InfoColor = Color.FromArgb(80, 130, 195);

        // 效果元数据：呼吸动画（亮色主题下更柔和）
        GlassEnabled = false;
        AnimationType = "breath";
        AnimationSpeed = 0.8;
        GradientStops = new List<Color>
        {
            Color.FromArgb(40, 255, 200, 130),
            Color.FromArgb(40, 255, 160, 100)
        };
    }

    /// <summary>
    /// 在加载时被调用，用于派生依赖型字段。
    /// 这里根据 Primary 派生 PrimaryDark（加深 20%），演示运行时计算。
    /// </summary>
    public override void Initialize()
    {
        // 派生深色变体：把 Primary 每个分量降低约 20%
        PrimaryDark = Color.FromArgb(
            (int)(Primary.R * 0.8),
            (int)(Primary.G * 0.8),
            (int)(Primary.B * 0.8));

        // 演示条件逻辑：晚间（20 点至次日 6 点）切换为更深沉的背景
        int hour = System.DateTime.Now.Hour;
        if (hour >= 20 || hour < 6)
        {
            Bg = Color.FromArgb(70, 50, 40);
            BgAlt = Color.FromArgb(78, 56, 44);
            Surface = Color.FromArgb(85, 62, 48);
            TextPrimary = Color.FromArgb(245, 230, 215);
            TextSecondary = Color.FromArgb(190, 170, 150);
        }
    }
}
