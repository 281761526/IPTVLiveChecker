using System.Collections.Generic;
using System.Drawing;
using IPTVLiveChecker;

namespace ThemePack.Sample;

/// <summary>
/// 极光玻璃主题示例（暗色 + 毛玻璃 + 极光动画背景）。
/// 演示最简单的用法：在构造函数中直接给所有字段赋值。
/// </summary>
public class AuroraGlassTheme : AppTheme
{
    public AuroraGlassTheme()
    {
        Name = "极光玻璃";

        // ===== 色板（暗色基底 + 冷色点缀） =====
        Primary = Color.FromArgb(120, 220, 255);
        PrimaryDark = Color.FromArgb(86, 178, 220);
        Accent = Color.FromArgb(180, 130, 255);
        Bg = Color.FromArgb(30, 35, 55);
        BgAlt = Color.FromArgb(36, 42, 66);
        Surface = Color.FromArgb(40, 46, 72);
        Border = Color.FromArgb(70, 80, 110);
        TextPrimary = Color.FromArgb(235, 240, 255);
        TextSecondary = Color.FromArgb(165, 175, 200);
        HeaderBg = Color.FromArgb(38, 44, 68);
        SelectRow = Color.FromArgb(55, 65, 95);
        SelectRowText = Color.FromArgb(235, 240, 255);
        StatusBarBg = Color.FromArgb(34, 40, 62);

        // ===== 功能色 =====
        TipBg = Color.FromArgb(45, 52, 78);
        PlayBtnBg = Color.FromArgb(80, 200, 160);
        PlayBtnText = Color.White;
        CopyBtnBg = Color.FromArgb(120, 160, 255);
        CopyBtnText = Color.White;
        StatusTagBg = Color.FromArgb(45, 55, 82);
        StatusTagBorder = Color.FromArgb(80, 200, 160);
        LinkTextColor = Color.FromArgb(140, 200, 255);
        SuccessColor = Color.FromArgb(80, 200, 160);
        ErrorColor = Color.FromArgb(240, 100, 110);
        WarnColor = Color.FromArgb(255, 180, 80);
        InfoColor = Color.FromArgb(120, 180, 255);

        // ===== 效果元数据（启用毛玻璃 + 极光动画） =====
        GlassEnabled = true;
        GlassOpacity = 200;
        GlassBlur = true;
        AnimationType = "aurora";
        AnimationSpeed = 1.0;
        GradientStops = new List<Color>
        {
            Color.FromArgb(70, 120, 200, 255),
            Color.FromArgb(70, 200, 120, 255),
            Color.FromArgb(70, 255, 120, 200)
        };
    }
}
