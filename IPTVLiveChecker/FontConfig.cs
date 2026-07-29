using System.Drawing;

namespace IPTVLiveChecker;

internal class FontConfig
{
	public Font Icon;

	public Font Text;

	public Font Title;

	public Font Button;

	public Font Label;

	public Font Input;

	public Font Hint;

	public Font Content;

	public Font Header;

	public Font Pill;

	public Font Url;

	public Font Active;

	public Font Normal;

	public Font Btn;

	public void Initialize(float dpiScale, FontDefaults defaults)
	{
		Icon = defaults.Icon ?? new Font("Segoe UI Symbol", 16f * dpiScale);
		Text = defaults.Text ?? new Font(IPTVLiveCheckerMain.customFontFamily, 9f * dpiScale);
		Title = defaults.Title ?? new Font(IPTVLiveCheckerMain.customFontFamily, 11f * dpiScale, FontStyle.Bold);
		Button = defaults.Button ?? new Font(IPTVLiveCheckerMain.customFontFamily, 8.5f * dpiScale);
		Label = defaults.Label ?? new Font(IPTVLiveCheckerMain.customFontFamily, 9f * dpiScale);
		Input = defaults.Input ?? new Font(IPTVLiveCheckerMain.customFontFamily, 8.5f * dpiScale);
		Hint = defaults.Hint ?? new Font(IPTVLiveCheckerMain.customFontFamily, 8.5f * dpiScale);
		Content = defaults.Content ?? new Font(IPTVLiveCheckerMain.customFontFamily, 9f * dpiScale);
		Header = defaults.Header ?? new Font(IPTVLiveCheckerMain.customFontFamily, 9f * dpiScale);
		Pill = defaults.Pill ?? new Font(IPTVLiveCheckerMain.customFontFamily, 6.7f * dpiScale);
		Url = defaults.Url ?? new Font("Consolas", 6.7f * dpiScale);
		Active = defaults.Active ?? new Font(IPTVLiveCheckerMain.customFontFamily, 8.5f * dpiScale, FontStyle.Bold);
		Normal = defaults.Normal ?? new Font(IPTVLiveCheckerMain.customFontFamily, 8.5f * dpiScale);
		Btn = defaults.Btn ?? new Font(IPTVLiveCheckerMain.customFontFamily, 11f * dpiScale);
	}
}
