using System.Drawing;
using System.Windows.Forms;

namespace IPTVLiveChecker;

internal class MenuColorTable : ProfessionalColorTable
{
	private readonly AppTheme _theme;

	public override Color MenuBorder => _theme.Border;

	public override Color MenuItemBorder => Color.Transparent;

	// 选中高亮交由 AnimatedMenuRenderer 以动画方式绘制，这里置透明避免双重绘制
	public override Color MenuItemSelected => Color.Transparent;

	public override Color MenuItemSelectedGradientBegin => Color.Transparent;

	public override Color MenuItemSelectedGradientEnd => Color.Transparent;

	public override Color MenuStripGradientBegin => _theme.Surface;

	public override Color MenuStripGradientEnd => _theme.Surface;

	public override Color ToolStripBorder => _theme.Border;

	public override Color ToolStripDropDownBackground => _theme.Surface;

	public override Color ImageMarginGradientBegin => _theme.Surface;

	public override Color ImageMarginGradientMiddle => _theme.Surface;

	public override Color ImageMarginGradientEnd => _theme.Surface;

	public override Color SeparatorDark => _theme.Border;

	public override Color SeparatorLight => Color.Transparent;

	public MenuColorTable(AppTheme theme)
	{
		_theme = theme;
	}
}
