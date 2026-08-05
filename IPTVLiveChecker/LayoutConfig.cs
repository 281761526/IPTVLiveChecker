using System.Reflection;

namespace IPTVLiveChecker;

internal class LayoutConfig
{
	public int Width = 0;

	public int Height = 0;

	public int MinWidth = 0;

	public int MinHeight = 0;

	public int Padding = 0;

	public int Margin = 0;

	public int Gap = 0;

	public int Left = 0;

	public int Top = 0;

	public int Right = 0;

	public int Bottom = 0;

	public int IconSize = 0;

	public int IconGap = 0;

	public int TitleHeight = 0;

	public int TitleGap = 0;

	public int BtnHeight = 0;

	public int BtnWidth = 0;

	public int BtnGap = 0;

	public int LabelWidth = 0;

	public int LabelGap = 0;

	public int InputHeight = 0;

	public int CornerRadius = 0;

	public int RowHeight = 0;

	public int HeaderHeight = 0;

	public int DividerWidth = 0;

	public void Initialize(LayoutDefaults defaults)
	{
		foreach (FieldInfo field in typeof(LayoutConfig).GetFields(BindingFlags.Public | BindingFlags.Instance))
		{
			FieldInfo src = typeof(LayoutDefaults).GetField(field.Name);
			if (src != null)
			{
				field.SetValue(this, src.GetValue(defaults));
			}
		}
	}
}
