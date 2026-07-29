using System.Drawing;
using System.Reflection;

namespace IPTVLiveChecker;

internal class ColorConfig
{
	public Color Background;

	public Color BackgroundLight;

	public Color BackgroundDark;

	public Color Foreground;

	public Color ForegroundSecondary;

	public Color ForegroundDisabled;

	public Color Border;

	public Color BorderLight;

	public Color Title;

	public Color Primary;

	public Color PrimaryHover;

	public Color PrimaryActive;

	public Color Accent;

	public Color AccentHover;

	public Color Button;

	public Color ButtonHover;

	public Color ButtonActive;

	public Color ButtonText;

	public Color ButtonTextDisabled;

	public Color Label;

	public Color LabelSecondary;

	public Color Input;

	public Color InputFocus;

	public Color InputText;

	public Color InputPlaceholder;

	public Color Success;

	public Color Warning;

	public Color Error;

	public Color Info;

	public Color Pill;

	public Color PillText;

	public Color Selected;

	public Color SelectedHover;

	public Color ScrollBar;

	public Color ScrollBarHover;

	public Color ScrollBarThumb;

	public Color Divider;

	public Color Header;

	public Color HeaderText;

	public Color Row;

	public Color RowHover;

	public Color RowAlternate;

	public void Initialize(ColorDefaults defaults)
	{
		foreach (FieldInfo field in typeof(ColorConfig).GetFields(BindingFlags.Public | BindingFlags.Instance))
		{
			FieldInfo src = typeof(ColorDefaults).GetField(field.Name);
			if (src != null)
			{
				field.SetValue(this, src.GetValue(defaults));
			}
		}
	}

	public void SyncFromTheme(AppTheme theme)
	{
		if (theme == null) return;
		bool isDark = (0.299 * theme.Bg.R + 0.587 * theme.Bg.G + 0.114 * theme.Bg.B) / 255.0 < 0.5;
		Background = theme.Bg;
		BackgroundLight = theme.BgAlt;
		BackgroundDark = theme.Surface;
		Foreground = theme.TextPrimary;
		ForegroundSecondary = theme.TextSecondary;
		ForegroundDisabled = Color.FromArgb(128, theme.TextSecondary);
		Border = theme.Border;
		BorderLight = Color.FromArgb(128, theme.Border);
		Title = theme.TextPrimary;
		Primary = theme.Primary;
		PrimaryHover = theme.PrimaryDark;
		PrimaryActive = theme.PrimaryDark;
		Accent = theme.Accent;
		AccentHover = Color.FromArgb(200, theme.Accent);
		Button = theme.Surface;
		ButtonHover = theme.SelectRow;
		ButtonActive = theme.PrimaryDark;
		ButtonText = theme.TextPrimary;
		ButtonTextDisabled = Color.FromArgb(128, theme.TextSecondary);
		Label = theme.TextPrimary;
		LabelSecondary = theme.TextSecondary;
		Input = theme.Surface;
		InputFocus = theme.Primary;
		InputText = theme.TextPrimary;
		InputPlaceholder = Color.FromArgb(150, theme.TextSecondary);
		Success = theme.SuccessColor;
		Warning = theme.WarnColor;
		Error = theme.ErrorColor;
		Info = theme.InfoColor;
		Pill = theme.SelectRow;
		PillText = theme.SelectRowText;
		Selected = theme.SelectRow;
		SelectedHover = Color.FromArgb(220, theme.SelectRow);
		ScrollBar = theme.BgAlt;
		ScrollBarHover = theme.TextPrimary;
		ScrollBarThumb = theme.TextSecondary;
		Divider = theme.Border;
		Header = theme.HeaderBg;
		HeaderText = theme.TextPrimary;
		Row = theme.Surface;
		RowHover = Color.FromArgb(215, theme.SelectRow);
		RowAlternate = theme.BgAlt;
	}
}
