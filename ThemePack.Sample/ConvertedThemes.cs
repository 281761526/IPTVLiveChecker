using System.Collections.Generic;
using System.Drawing;
using IPTVLiveChecker;

namespace ThemePack.Sample;

/// <summary>
/// 从 JSON 主题文件转换而来的 14 个主题，编译进 ThemePack.Sample.dll。
/// 颜色值与原始 JSON 完全一致，使用 ColorTranslator.FromHtml 解析十六进制色值。
/// </summary>
public static class ConvertedThemes
{
    internal static Color H(string hex) => ColorTranslator.FromHtml(hex);
}

// ════════════════════════════════════════════════════════════
// 1. 极光玻璃 — 暗色 + 毛玻璃 + aurora 动画
// ════════════════════════════════════════════════════════════
public class AuroraGlassTheme : AppTheme
{
    public AuroraGlassTheme()
    {
        Name = "极光玻璃";
        Primary = ConvertedThemes.H("#5ac8fa");
        PrimaryDark = ConvertedThemes.H("#3aa0d8");
        Accent = ConvertedThemes.H("#7b8cff");
        Bg = ConvertedThemes.H("#0d1220");
        BgAlt = ConvertedThemes.H("#141b2e");
        Surface = ConvertedThemes.H("#1a2336");
        Border = ConvertedThemes.H("#2c3650");
        TextPrimary = ConvertedThemes.H("#eaf1ff");
        TextSecondary = ConvertedThemes.H("#9fb2d4");
        HeaderBg = ConvertedThemes.H("#141b2e");
        SelectRow = ConvertedThemes.H("#1f2c46");
        SelectRowText = ConvertedThemes.H("#eaf1ff");
        StatusBarBg = ConvertedThemes.H("#141b2e");
        TipBg = ConvertedThemes.H("#141b2e");
        PlayBtnBg = ConvertedThemes.H("#2bb673");
        PlayBtnText = ConvertedThemes.H("#ffffff");
        CopyBtnBg = ConvertedThemes.H("#3a7bd5");
        CopyBtnText = ConvertedThemes.H("#ffffff");
        StatusTagBg = ConvertedThemes.H("#1a2336");
        StatusTagBorder = ConvertedThemes.H("#2bb673");
        LinkTextColor = ConvertedThemes.H("#7cc4ff");
        SuccessColor = ConvertedThemes.H("#34d399");
        ErrorColor = ConvertedThemes.H("#ff6b81");
        WarnColor = ConvertedThemes.H("#f5b94a");
        InfoColor = ConvertedThemes.H("#5ac8fa");
        GlassEnabled = true;
        GlassOpacity = 205;
        GlassBlur = false;
        AnimationType = "aurora";
        AnimationSpeed = 1.0;
        GradientStops = new List<Color>
        {
            ConvertedThemes.H("#0d1220"), ConvertedThemes.H("#1b3a6b"),
            ConvertedThemes.H("#3aa0d8"), ConvertedThemes.H("#7b8cff"),
            ConvertedThemes.H("#151b2e")
        };
    }
}

// ════════════════════════════════════════════════════════════
// 2. 极光流光 — 暗色 + 毛玻璃 + aurora 动画
// ════════════════════════════════════════════════════════════
public class AuroraFlowTheme : AppTheme
{
    public AuroraFlowTheme()
    {
        Name = "极光流光";
        Primary = ConvertedThemes.H("#38e1c6");
        PrimaryDark = ConvertedThemes.H("#1f9e8f");
        Accent = ConvertedThemes.H("#38e1c6");
        Bg = ConvertedThemes.H("#0b1020");
        BgAlt = ConvertedThemes.H("#13203a");
        Surface = ConvertedThemes.H("#13203a");
        Border = ConvertedThemes.H("#1c2c4a");
        TextPrimary = ConvertedThemes.H("#dfeaf5");
        TextSecondary = ConvertedThemes.H("#8fa6c0");
        HeaderBg = ConvertedThemes.H("#0c1426");
        SelectRow = ConvertedThemes.H("#16314f");
        SelectRowText = ConvertedThemes.H("#dfeaf5");
        StatusBarBg = ConvertedThemes.H("#0a0e1c");
        TipBg = ConvertedThemes.H("#0c1426");
        PlayBtnBg = ConvertedThemes.H("#2bb673");
        PlayBtnText = ConvertedThemes.H("#ffffff");
        CopyBtnBg = ConvertedThemes.H("#2f9e8f");
        CopyBtnText = ConvertedThemes.H("#ffffff");
        StatusTagBg = ConvertedThemes.H("#16314f");
        StatusTagBorder = ConvertedThemes.H("#2bb673");
        LinkTextColor = ConvertedThemes.H("#5ad1c4");
        SuccessColor = ConvertedThemes.H("#3ddc97");
        ErrorColor = ConvertedThemes.H("#ff6b81");
        WarnColor = ConvertedThemes.H("#ffd166");
        InfoColor = ConvertedThemes.H("#5ad1ff");
        GlassEnabled = true;
        GlassOpacity = 205;
        GlassBlur = false;
        AnimationType = "aurora";
        AnimationSpeed = 1.0;
        GradientStops = new List<Color>
        {
            ConvertedThemes.H("#0b1020"), ConvertedThemes.H("#102a43"),
            ConvertedThemes.H("#163a5c"), ConvertedThemes.H("#2a8f7a"),
            ConvertedThemes.H("#38e1c6")
        };
    }
}

// ════════════════════════════════════════════════════════════
// 3. 霓光梦境 — 暗色 + 毛玻璃 + neon 动画
// ════════════════════════════════════════════════════════════
public class NeonDreamTheme : AppTheme
{
    public NeonDreamTheme()
    {
        Name = "霓光梦境";
        Primary = ConvertedThemes.H("#ff3d9a");
        PrimaryDark = ConvertedThemes.H("#d62f80");
        Accent = ConvertedThemes.H("#18e0d8");
        Bg = ConvertedThemes.H("#0a0a14");
        BgAlt = ConvertedThemes.H("#14101f");
        Surface = ConvertedThemes.H("#1a1426");
        Border = ConvertedThemes.H("#322445");
        TextPrimary = ConvertedThemes.H("#fbeeff");
        TextSecondary = ConvertedThemes.H("#b389c9");
        HeaderBg = ConvertedThemes.H("#14101f");
        SelectRow = ConvertedThemes.H("#241a33");
        SelectRowText = ConvertedThemes.H("#fbeeff");
        StatusBarBg = ConvertedThemes.H("#14101f");
        TipBg = ConvertedThemes.H("#14101f");
        PlayBtnBg = ConvertedThemes.H("#18b89a");
        PlayBtnText = ConvertedThemes.H("#ffffff");
        CopyBtnBg = ConvertedThemes.H("#9b5cff");
        CopyBtnText = ConvertedThemes.H("#ffffff");
        StatusTagBg = ConvertedThemes.H("#1a1426");
        StatusTagBorder = ConvertedThemes.H("#18e0d8");
        LinkTextColor = ConvertedThemes.H("#ff8ad4");
        SuccessColor = ConvertedThemes.H("#3dffb0");
        ErrorColor = ConvertedThemes.H("#ff5d7a");
        WarnColor = ConvertedThemes.H("#ffcf5d");
        InfoColor = ConvertedThemes.H("#18e0d8");
        GlassEnabled = true;
        GlassOpacity = 205;
        GlassBlur = false;
        AnimationType = "neon";
        AnimationSpeed = 1.1;
        GradientStops = new List<Color>
        {
            ConvertedThemes.H("#0a0a14"), ConvertedThemes.H("#2a1140"),
            ConvertedThemes.H("#ff3d9a"), ConvertedThemes.H("#18e0d8"),
            ConvertedThemes.H("#1a1426")
        };
    }
}

// ════════════════════════════════════════════════════════════
// 4. 霓虹脉冲 — 暗色 + 毛玻璃 + neon 动画
// ════════════════════════════════════════════════════════════
public class NeonPulseTheme : AppTheme
{
    public NeonPulseTheme()
    {
        Name = "霓虹脉冲";
        Primary = ConvertedThemes.H("#ff2d95");
        PrimaryDark = ConvertedThemes.H("#c41f72");
        Accent = ConvertedThemes.H("#ff2d95");
        Bg = ConvertedThemes.H("#0d0d12");
        BgAlt = ConvertedThemes.H("#16161d");
        Surface = ConvertedThemes.H("#16161d");
        Border = ConvertedThemes.H("#2a2a36");
        TextPrimary = ConvertedThemes.H("#f0f0f5");
        TextSecondary = ConvertedThemes.H("#b0b0ba");
        HeaderBg = ConvertedThemes.H("#101018");
        SelectRow = ConvertedThemes.H("#221824");
        SelectRowText = ConvertedThemes.H("#f0f0f5");
        StatusBarBg = ConvertedThemes.H("#0a0a0e");
        TipBg = ConvertedThemes.H("#101018");
        PlayBtnBg = ConvertedThemes.H("#34c759");
        PlayBtnText = ConvertedThemes.H("#ffffff");
        CopyBtnBg = ConvertedThemes.H("#7a5cff");
        CopyBtnText = ConvertedThemes.H("#ffffff");
        StatusTagBg = ConvertedThemes.H("#221824");
        StatusTagBorder = ConvertedThemes.H("#ff2d95");
        LinkTextColor = ConvertedThemes.H("#ff5db0");
        SuccessColor = ConvertedThemes.H("#3ddc97");
        ErrorColor = ConvertedThemes.H("#ff453a");
        WarnColor = ConvertedThemes.H("#ffd60a");
        InfoColor = ConvertedThemes.H("#7a5cff");
        GlassEnabled = true;
        GlassOpacity = 205;
        GlassBlur = false;
        AnimationType = "neon";
        AnimationSpeed = 1.1;
        GradientStops = new List<Color>
        {
            ConvertedThemes.H("#0d0d12"), ConvertedThemes.H("#3a1030"),
            ConvertedThemes.H("#ff2d95"), ConvertedThemes.H("#7a5cff"),
            ConvertedThemes.H("#2de2ff")
        };
    }
}

// ════════════════════════════════════════════════════════════
// 5. 呼吸脉冲 — 暗色 + 毛玻璃 + breath 动画
// ════════════════════════════════════════════════════════════
public class BreathPulseTheme : AppTheme
{
    public BreathPulseTheme()
    {
        Name = "呼吸脉冲";
        Primary = ConvertedThemes.H("#5ad1ff");
        PrimaryDark = ConvertedThemes.H("#2f9fcc");
        Accent = ConvertedThemes.H("#5ad1ff");
        Bg = ConvertedThemes.H("#16181d");
        BgAlt = ConvertedThemes.H("#1f232b");
        Surface = ConvertedThemes.H("#262b34");
        Border = ConvertedThemes.H("#36404d");
        TextPrimary = ConvertedThemes.H("#eef3fb");
        TextSecondary = ConvertedThemes.H("#9fb0c3");
        HeaderBg = ConvertedThemes.H("#191c22");
        SelectRow = ConvertedThemes.H("#1f3a4a");
        SelectRowText = ConvertedThemes.H("#eef3fb");
        StatusBarBg = ConvertedThemes.H("#191c22");
        TipBg = ConvertedThemes.H("#191c22");
        PlayBtnBg = ConvertedThemes.H("#46e0a8");
        PlayBtnText = ConvertedThemes.H("#ffffff");
        CopyBtnBg = ConvertedThemes.H("#7c8cff");
        CopyBtnText = ConvertedThemes.H("#ffffff");
        StatusTagBg = ConvertedThemes.H("#1f3a4a");
        StatusTagBorder = ConvertedThemes.H("#46e0a8");
        LinkTextColor = ConvertedThemes.H("#5ad1ff");
        SuccessColor = ConvertedThemes.H("#46e0a8");
        ErrorColor = ConvertedThemes.H("#ff6b81");
        WarnColor = ConvertedThemes.H("#ffd166");
        InfoColor = ConvertedThemes.H("#7c8cff");
        GlassEnabled = true;
        GlassOpacity = 205;
        GlassBlur = false;
        AnimationType = "breath";
        AnimationSpeed = 1.0;
        GradientStops = new List<Color>
        {
            ConvertedThemes.H("#16181d"), ConvertedThemes.H("#1f2f3a"),
            ConvertedThemes.H("#5ad1ff"), ConvertedThemes.H("#7c8cff"),
            ConvertedThemes.H("#101418")
        };
    }
}

// ════════════════════════════════════════════════════════════
// 6. 流体玻璃 — 亮色 + 毛玻璃 + fluid 动画
// ════════════════════════════════════════════════════════════
public class FluidGlassTheme : AppTheme
{
    public FluidGlassTheme()
    {
        Name = "流体玻璃";
        Primary = ConvertedThemes.H("#5e5ce6");
        PrimaryDark = ConvertedThemes.H("#4646c0");
        Accent = ConvertedThemes.H("#5e5ce6");
        Bg = ConvertedThemes.H("#eef1f6");
        BgAlt = ConvertedThemes.H("#ffffff");
        Surface = ConvertedThemes.H("#ffffff");
        Border = ConvertedThemes.H("#d7dde8");
        TextPrimary = ConvertedThemes.H("#1d1d22");
        TextSecondary = ConvertedThemes.H("#6b7385");
        HeaderBg = ConvertedThemes.H("#ffffff");
        SelectRow = ConvertedThemes.H("#e7e9ff");
        SelectRowText = ConvertedThemes.H("#1d1d1f");
        StatusBarBg = ConvertedThemes.H("#e7ebf2");
        TipBg = ConvertedThemes.H("#ffffff");
        PlayBtnBg = ConvertedThemes.H("#1fb56a");
        PlayBtnText = ConvertedThemes.H("#ffffff");
        CopyBtnBg = ConvertedThemes.H("#5e5ce6");
        CopyBtnText = ConvertedThemes.H("#ffffff");
        StatusTagBg = ConvertedThemes.H("#e7e9ff");
        StatusTagBorder = ConvertedThemes.H("#1fb56a");
        LinkTextColor = ConvertedThemes.H("#3a4fd9");
        SuccessColor = ConvertedThemes.H("#1fb56a");
        ErrorColor = ConvertedThemes.H("#e0463b");
        WarnColor = ConvertedThemes.H("#c47d00");
        InfoColor = ConvertedThemes.H("#3a4fd9");
        GlassEnabled = true;
        GlassOpacity = 205;
        GlassBlur = false;
        AnimationType = "fluid";
        AnimationSpeed = 1.0;
        GradientStops = new List<Color>
        {
            ConvertedThemes.H("#eef1f6"), ConvertedThemes.H("#dfe6f5"),
            ConvertedThemes.H("#5e5ce6"), ConvertedThemes.H("#39c2ff"),
            ConvertedThemes.H("#ffffff")
        };
    }
}

// ════════════════════════════════════════════════════════════
// 7. 液态水银 — 暗色 + 毛玻璃 + fluid 动画
// ════════════════════════════════════════════════════════════
public class LiquidMercuryTheme : AppTheme
{
    public LiquidMercuryTheme()
    {
        Name = "液态水银";
        Primary = ConvertedThemes.H("#46e7c8");
        PrimaryDark = ConvertedThemes.H("#2bb39a");
        Accent = ConvertedThemes.H("#46e7c8");
        Bg = ConvertedThemes.H("#12141b");
        BgAlt = ConvertedThemes.H("#1b1f29");
        Surface = ConvertedThemes.H("#222734");
        Border = ConvertedThemes.H("#33405a");
        TextPrimary = ConvertedThemes.H("#eef4ff");
        TextSecondary = ConvertedThemes.H("#9fb2cf");
        HeaderBg = ConvertedThemes.H("#161922");
        SelectRow = ConvertedThemes.H("#1b3340");
        SelectRowText = ConvertedThemes.H("#eef4ff");
        StatusBarBg = ConvertedThemes.H("#161922");
        TipBg = ConvertedThemes.H("#161922");
        PlayBtnBg = ConvertedThemes.H("#46e0a8");
        PlayBtnText = ConvertedThemes.H("#ffffff");
        CopyBtnBg = ConvertedThemes.H("#6fb6ff");
        CopyBtnText = ConvertedThemes.H("#ffffff");
        StatusTagBg = ConvertedThemes.H("#1b3340");
        StatusTagBorder = ConvertedThemes.H("#46e0a8");
        LinkTextColor = ConvertedThemes.H("#46e7c8");
        SuccessColor = ConvertedThemes.H("#46e0a8");
        ErrorColor = ConvertedThemes.H("#ff6b81");
        WarnColor = ConvertedThemes.H("#ffd166");
        InfoColor = ConvertedThemes.H("#6fb6ff");
        GlassEnabled = true;
        GlassOpacity = 205;
        GlassBlur = false;
        AnimationType = "fluid";
        AnimationSpeed = 1.0;
        GradientStops = new List<Color>
        {
            ConvertedThemes.H("#12141b"), ConvertedThemes.H("#1b2f33"),
            ConvertedThemes.H("#46e7c8"), ConvertedThemes.H("#5ad1ff"),
            ConvertedThemes.H("#0e1118")
        };
    }
}

// ════════════════════════════════════════════════════════════
// 8. 晨曦玻璃 — 亮色 + 毛玻璃 + aurora 动画
// ════════════════════════════════════════════════════════════
public class DawnGlassTheme : AppTheme
{
    public DawnGlassTheme()
    {
        Name = "晨曦玻璃";
        Primary = ConvertedThemes.H("#ff7eb3");
        PrimaryDark = ConvertedThemes.H("#e0528f");
        Accent = ConvertedThemes.H("#ff7eb3");
        Bg = ConvertedThemes.H("#fdf2f6");
        BgAlt = ConvertedThemes.H("#ffffff");
        Surface = ConvertedThemes.H("#ffffff");
        Border = ConvertedThemes.H("#f0d9e2");
        TextPrimary = ConvertedThemes.H("#1f2740");
        TextSecondary = ConvertedThemes.H("#5b678a");
        HeaderBg = ConvertedThemes.H("#ffffff");
        SelectRow = ConvertedThemes.H("#ffe6f0");
        SelectRowText = ConvertedThemes.H("#1f2740");
        StatusBarBg = ConvertedThemes.H("#fbeef4");
        TipBg = ConvertedThemes.H("#ffffff");
        PlayBtnBg = ConvertedThemes.H("#2ea362");
        PlayBtnText = ConvertedThemes.H("#ffffff");
        CopyBtnBg = ConvertedThemes.H("#ff7eb3");
        CopyBtnText = ConvertedThemes.H("#ffffff");
        StatusTagBg = ConvertedThemes.H("#ffe6f0");
        StatusTagBorder = ConvertedThemes.H("#2ea362");
        LinkTextColor = ConvertedThemes.H("#e0528f");
        SuccessColor = ConvertedThemes.H("#1fae7a");
        ErrorColor = ConvertedThemes.H("#e8556b");
        WarnColor = ConvertedThemes.H("#d98300");
        InfoColor = ConvertedThemes.H("#e0528f");
        GlassEnabled = true;
        GlassOpacity = 205;
        GlassBlur = false;
        AnimationType = "aurora";
        AnimationSpeed = 1.0;
        GradientStops = new List<Color>
        {
            ConvertedThemes.H("#fdf2f6"), ConvertedThemes.H("#ffe1ec"),
            ConvertedThemes.H("#ff7eb3"), ConvertedThemes.H("#ffb36b"),
            ConvertedThemes.H("#ffffff")
        };
    }
}

// ════════════════════════════════════════════════════════════
// 9. 星河 — 暗色 + 毛玻璃 + aurora 动画
// ════════════════════════════════════════════════════════════
public class StarRiverTheme : AppTheme
{
    public StarRiverTheme()
    {
        Name = "星河";
        Primary = ConvertedThemes.H("#5ad1ff");
        PrimaryDark = ConvertedThemes.H("#2f9fcc");
        Accent = ConvertedThemes.H("#5ad1ff");
        Bg = ConvertedThemes.H("#1c1c22");
        BgAlt = ConvertedThemes.H("#26262e");
        Surface = ConvertedThemes.H("#2f2f38");
        Border = ConvertedThemes.H("#3c3c46");
        TextPrimary = ConvertedThemes.H("#f5f5f7");
        TextSecondary = ConvertedThemes.H("#a1a1a6");
        HeaderBg = ConvertedThemes.H("#22222a");
        SelectRow = ConvertedThemes.H("#2b3a52");
        SelectRowText = ConvertedThemes.H("#f5f5f7");
        StatusBarBg = ConvertedThemes.H("#20202a");
        TipBg = ConvertedThemes.H("#22222a");
        PlayBtnBg = ConvertedThemes.H("#46e0a8");
        PlayBtnText = ConvertedThemes.H("#ffffff");
        CopyBtnBg = ConvertedThemes.H("#7c8cff");
        CopyBtnText = ConvertedThemes.H("#ffffff");
        StatusTagBg = ConvertedThemes.H("#2b3a52");
        StatusTagBorder = ConvertedThemes.H("#46e0a8");
        LinkTextColor = ConvertedThemes.H("#5ad1ff");
        SuccessColor = ConvertedThemes.H("#46e0a8");
        ErrorColor = ConvertedThemes.H("#ff6b81");
        WarnColor = ConvertedThemes.H("#ffd166");
        InfoColor = ConvertedThemes.H("#7c8cff");
        GlassEnabled = true;
        GlassOpacity = 205;
        GlassBlur = false;
        AnimationType = "aurora";
        AnimationSpeed = 1.0;
        GradientStops = new List<Color>
        {
            ConvertedThemes.H("#1c1c22"), ConvertedThemes.H("#222a44"),
            ConvertedThemes.H("#5ad1ff"), ConvertedThemes.H("#bf5af2"),
            ConvertedThemes.H("#101018")
        };
    }
}

// ════════════════════════════════════════════════════════════
// 10. 蜜桃薄暮 — 亮色 + 毛玻璃 + fluid 动画
// ════════════════════════════════════════════════════════════
public class PeachDuskTheme : AppTheme
{
    public PeachDuskTheme()
    {
        Name = "蜜桃薄暮";
        Primary = ConvertedThemes.H("#ff7a59");
        PrimaryDark = ConvertedThemes.H("#e0543a");
        Accent = ConvertedThemes.H("#ff7a59");
        Bg = ConvertedThemes.H("#fff4ee");
        BgAlt = ConvertedThemes.H("#ffffff");
        Surface = ConvertedThemes.H("#ffffff");
        Border = ConvertedThemes.H("#f3ddd0");
        TextPrimary = ConvertedThemes.H("#3a2a22");
        TextSecondary = ConvertedThemes.H("#9a7c6c");
        HeaderBg = ConvertedThemes.H("#fff0e8");
        SelectRow = ConvertedThemes.H("#ffe3d8");
        SelectRowText = ConvertedThemes.H("#3a2a22");
        StatusBarBg = ConvertedThemes.H("#fff0e8");
        TipBg = ConvertedThemes.H("#fff0e8");
        PlayBtnBg = ConvertedThemes.H("#34c759");
        PlayBtnText = ConvertedThemes.H("#ffffff");
        CopyBtnBg = ConvertedThemes.H("#ff7a59");
        CopyBtnText = ConvertedThemes.H("#ffffff");
        StatusTagBg = ConvertedThemes.H("#ffe3d8");
        StatusTagBorder = ConvertedThemes.H("#34c759");
        LinkTextColor = ConvertedThemes.H("#e0522f");
        SuccessColor = ConvertedThemes.H("#2ea362");
        ErrorColor = ConvertedThemes.H("#e0463b");
        WarnColor = ConvertedThemes.H("#d98300");
        InfoColor = ConvertedThemes.H("#e0522f");
        GlassEnabled = true;
        GlassOpacity = 205;
        GlassBlur = false;
        AnimationType = "fluid";
        AnimationSpeed = 1.0;
        GradientStops = new List<Color>
        {
            ConvertedThemes.H("#fff4ee"), ConvertedThemes.H("#ffe3d8"),
            ConvertedThemes.H("#ff7a59"), ConvertedThemes.H("#ff5d8f"),
            ConvertedThemes.H("#ffffff")
        };
    }
}

// ════════════════════════════════════════════════════════════
// 11. 绛紫 — 暗色，无动画
// ════════════════════════════════════════════════════════════
public class DeepPurpleTheme : AppTheme
{
    public DeepPurpleTheme()
    {
        Name = "绛紫";
        Primary = ConvertedThemes.H("#bf5af2");
        PrimaryDark = ConvertedThemes.H("#9a3fd0");
        Accent = ConvertedThemes.H("#bf5af2");
        Bg = ConvertedThemes.H("#1a1726");
        BgAlt = ConvertedThemes.H("#241f33");
        Surface = ConvertedThemes.H("#2e2842");
        Border = ConvertedThemes.H("#3c3454");
        TextPrimary = ConvertedThemes.H("#f2eefb");
        TextSecondary = ConvertedThemes.H("#b3a8cf");
        HeaderBg = ConvertedThemes.H("#241f33");
        SelectRow = ConvertedThemes.H("#2e2842");
        SelectRowText = ConvertedThemes.H("#f2eefb");
        StatusBarBg = ConvertedThemes.H("#241f33");
        TipBg = ConvertedThemes.H("#241f33");
        PlayBtnBg = ConvertedThemes.H("#30d158");
        PlayBtnText = ConvertedThemes.H("#ffffff");
        CopyBtnBg = ConvertedThemes.H("#bf5af2");
        CopyBtnText = ConvertedThemes.H("#ffffff");
        StatusTagBg = ConvertedThemes.H("#2e2842");
        StatusTagBorder = ConvertedThemes.H("#30d158");
        LinkTextColor = ConvertedThemes.H("#c89bff");
        SuccessColor = ConvertedThemes.H("#30d158");
        ErrorColor = ConvertedThemes.H("#ff453a");
        WarnColor = ConvertedThemes.H("#ffd60a");
        InfoColor = ConvertedThemes.H("#64a8ff");
        GlassEnabled = false;
        GlassOpacity = 210;
        GlassBlur = false;
        AnimationType = "";
        AnimationSpeed = 1.0;
    }
}

// ════════════════════════════════════════════════════════════
// 12. 暖砂 — 亮色，无动画
// ════════════════════════════════════════════════════════════
public class WarmSandTheme : AppTheme
{
    public WarmSandTheme()
    {
        Name = "暖砂";
        Primary = ConvertedThemes.H("#e8820c");
        PrimaryDark = ConvertedThemes.H("#b56400");
        Accent = ConvertedThemes.H("#e8820c");
        Bg = ConvertedThemes.H("#fbf6f0");
        BgAlt = ConvertedThemes.H("#ffffff");
        Surface = ConvertedThemes.H("#ffffff");
        Border = ConvertedThemes.H("#e7ddd0");
        TextPrimary = ConvertedThemes.H("#2c2620");
        TextSecondary = ConvertedThemes.H("#8a7d6e");
        HeaderBg = ConvertedThemes.H("#ffffff");
        SelectRow = ConvertedThemes.H("#fdeede");
        SelectRowText = ConvertedThemes.H("#2c2620");
        StatusBarBg = ConvertedThemes.H("#ffffff");
        TipBg = ConvertedThemes.H("#ffffff");
        PlayBtnBg = ConvertedThemes.H("#34c759");
        PlayBtnText = ConvertedThemes.H("#ffffff");
        CopyBtnBg = ConvertedThemes.H("#e8820c");
        CopyBtnText = ConvertedThemes.H("#ffffff");
        StatusTagBg = ConvertedThemes.H("#fdeede");
        StatusTagBorder = ConvertedThemes.H("#34c759");
        LinkTextColor = ConvertedThemes.H("#c66a00");
        SuccessColor = ConvertedThemes.H("#34c759");
        ErrorColor = ConvertedThemes.H("#ff3b30");
        WarnColor = ConvertedThemes.H("#ff9500");
        InfoColor = ConvertedThemes.H("#e8820c");
        GlassEnabled = false;
        GlassOpacity = 210;
        GlassBlur = false;
        AnimationType = "";
        AnimationSpeed = 1.0;
    }
}

// ════════════════════════════════════════════════════════════
// 13. 晴空蓝 — 亮色，无动画
// ════════════════════════════════════════════════════════════
public class ClearSkyBlueTheme : AppTheme
{
    public ClearSkyBlueTheme()
    {
        Name = "晴空蓝";
        Primary = ConvertedThemes.H("#0071e3");
        PrimaryDark = ConvertedThemes.H("#0058b0");
        Accent = ConvertedThemes.H("#0071e3");
        Bg = ConvertedThemes.H("#f5f5f7");
        BgAlt = ConvertedThemes.H("#ffffff");
        Surface = ConvertedThemes.H("#ffffff");
        Border = ConvertedThemes.H("#d2d2d7");
        TextPrimary = ConvertedThemes.H("#1d1d1f");
        TextSecondary = ConvertedThemes.H("#6e6e73");
        HeaderBg = ConvertedThemes.H("#ffffff");
        SelectRow = ConvertedThemes.H("#e8f1fd");
        SelectRowText = ConvertedThemes.H("#1d1d1f");
        StatusBarBg = ConvertedThemes.H("#ffffff");
        TipBg = ConvertedThemes.H("#ffffff");
        PlayBtnBg = ConvertedThemes.H("#34c759");
        PlayBtnText = ConvertedThemes.H("#ffffff");
        CopyBtnBg = ConvertedThemes.H("#0071e3");
        CopyBtnText = ConvertedThemes.H("#ffffff");
        StatusTagBg = ConvertedThemes.H("#e8f1fd");
        StatusTagBorder = ConvertedThemes.H("#34c759");
        LinkTextColor = ConvertedThemes.H("#0066cc");
        SuccessColor = ConvertedThemes.H("#34c759");
        ErrorColor = ConvertedThemes.H("#ff3b30");
        WarnColor = ConvertedThemes.H("#ff9500");
        InfoColor = ConvertedThemes.H("#0071e3");
        GlassEnabled = false;
        GlassOpacity = 210;
        GlassBlur = false;
        AnimationType = "";
        AnimationSpeed = 1.0;
    }
}

// ════════════════════════════════════════════════════════════
// 14. 石墨灰 — 暗色，无动画
// ════════════════════════════════════════════════════════════
public class GraphiteGrayTheme : AppTheme
{
    public GraphiteGrayTheme()
    {
        Name = "石墨灰";
        Primary = ConvertedThemes.H("#2997ff");
        PrimaryDark = ConvertedThemes.H("#1c6fd0");
        Accent = ConvertedThemes.H("#2997ff");
        Bg = ConvertedThemes.H("#1c1c1e");
        BgAlt = ConvertedThemes.H("#2c2c2e");
        Surface = ConvertedThemes.H("#3a3a3c");
        Border = ConvertedThemes.H("#48484a");
        TextPrimary = ConvertedThemes.H("#f5f5f7");
        TextSecondary = ConvertedThemes.H("#aeaeb2");
        HeaderBg = ConvertedThemes.H("#2c2c2e");
        SelectRow = ConvertedThemes.H("#3a3a3c");
        SelectRowText = ConvertedThemes.H("#f5f5f7");
        StatusBarBg = ConvertedThemes.H("#2c2c2e");
        TipBg = ConvertedThemes.H("#2c2c2e");
        PlayBtnBg = ConvertedThemes.H("#30d158");
        PlayBtnText = ConvertedThemes.H("#ffffff");
        CopyBtnBg = ConvertedThemes.H("#2997ff");
        CopyBtnText = ConvertedThemes.H("#ffffff");
        StatusTagBg = ConvertedThemes.H("#3a3a3c");
        StatusTagBorder = ConvertedThemes.H("#30d158");
        LinkTextColor = ConvertedThemes.H("#64a8ff");
        SuccessColor = ConvertedThemes.H("#32d74b");
        ErrorColor = ConvertedThemes.H("#ff453a");
        WarnColor = ConvertedThemes.H("#ff9f0a");
        InfoColor = ConvertedThemes.H("#64a8ff");
        GlassEnabled = false;
        GlassOpacity = 210;
        GlassBlur = false;
        AnimationType = "";
        AnimationSpeed = 1.0;
    }
}
