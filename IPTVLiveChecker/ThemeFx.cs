using System;
using System.Drawing;
using System.Windows.Forms;

namespace IPTVLiveChecker;

/// <summary>
/// 主题渲染兼容层 —— 动态动效引擎已彻底移除。
///
/// 历史版本中的极光/流体/脉冲背景、DWM 实时毛玻璃、透明穿透(EnableTransparent)、
/// 鼠标活动感知(GlobalMouseFilter) 等动态特效，因在 WinForms/GDI+ 下无法稳定呈现
/// （依次出现：动效失效 → 整片紫屏 → 变静态 → 界面闪烁抖动），且用户已删除全部动态主题，
/// 现已整体移除。下方仅保留原公共 API 的兼容占位（全部为空操作或常量），
/// 供仍引用 ThemeFx 的静态主题代码编译通过，且不产生任何动效/高频重绘/锁屏风险。
/// </summary>
public static class ThemeFx
{
	/// <summary>当前背景动画相位。动态引擎已移除，恒为 0（静态）。</summary>
	public static double CurrentPhase => 0.0;

	/// <summary>背景动画每帧事件。动态引擎已移除，永不触发（订阅方安全无副作用）。</summary>
	public static event Action PhaseTick
	{
		add { }
		remove { }
	}

	/// <summary>近期是否有鼠标移动。动态引擎已移除，恒为 false。</summary>
	public static bool IsMouseMoving => false;

	/// <summary>是否有鼠标按下。动态引擎已移除，恒为 false。</summary>
	public static bool IsMouseActive => false;

	/// <summary>注册全局鼠标过滤器（兼容占位，空操作）。</summary>
	public static void RegisterMouseFilter() { }

	/// <summary>注销全局鼠标过滤器（兼容占位，空操作）。</summary>
	public static void UnregisterMouseFilter() { }

	/// <summary>把颜色按 alpha 转为半透明玻璃色（保留：仍被静态 UI 描边/辉光使用）。</summary>
	public static Color Glass(Color baseColor, int alpha)
	{
		return DrawingUtils.WithAlpha(baseColor, alpha);
	}

	/// <summary>让控件支持透明背景。动态特效已移除，保留为空操作（静态主题下控件保持实色底，不穿底）。</summary>
	public static void EnableTransparent(Control c) { }

	/// <summary>根据主题配置主窗口动效背景层。动态引擎已移除，空操作。</summary>
	public static void ApplyThemeFx(Form form, AppTheme theme) { }

	/// <summary>根据主题配置动效背景层。动态引擎已移除，空操作。</summary>
	public static void ApplyThemeFx(Control host, AppTheme theme) { }

	/// <summary>DWM 实时毛玻璃。动态引擎已移除，空操作。</summary>
	public static void EnableBlurBehind(IntPtr hwnd, bool enable) { }
}
