using System.Reflection;

namespace IPTVLiveChecker;

internal class LayoutConfig
{
	public int Width;

	public int Height;

	public int MinWidth;

	public int MinHeight;

	public int Padding;

	public int Margin;

	public int Gap;

	public int Left;

	public int Top;

	public int Right;

	public int Bottom;

	public int IconSize;

	public int IconGap;

	public int TitleHeight;

	public int TitleGap;

	public int BtnHeight;

	public int BtnWidth;

	public int BtnGap;

	public int LabelWidth;

	public int LabelGap;

	public int InputHeight;

	public int CornerRadius;

	public int RowHeight;

	public int HeaderHeight;

	public int DividerWidth;

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
