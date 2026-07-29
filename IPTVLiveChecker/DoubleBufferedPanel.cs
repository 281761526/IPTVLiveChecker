using System.Windows.Forms;

namespace IPTVLiveChecker;

public class DoubleBufferedPanel : Panel
{
	public DoubleBufferedPanel()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, value: true);
	}
}
