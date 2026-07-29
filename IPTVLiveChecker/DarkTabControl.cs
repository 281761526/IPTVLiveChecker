using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace IPTVLiveChecker;

public class DarkTabControl : TabControl
{
	private Color _headerBg = Color.FromArgb(35, 35, 35);

	private Color _tabBg = Color.FromArgb(35, 35, 35);

	private Color _tabSelectedBg = Color.FromArgb(50, 50, 50);

	private Color _tabHoverBg = Color.FromArgb(60, 60, 60);

	private Color _tabText = Color.FromArgb(180, 180, 180);

	private Color _tabTextSelected = Color.White;

	private int _hoverIndex = -1;

	private int _tabHeight;

	public int[] TabWidths { get; set; }

	public int TabHeight
	{
		get
		{
			return _tabHeight;
		}
		set
		{
			_tabHeight = value;
			if (_tabHeight > 0 && base.IsHandleCreated)
			{
				base.ItemSize = new Size(base.ItemSize.Width, _tabHeight + 2);
			}
		}
	}

	public int TabXOffset { get; set; }

	public int TabSpacing { get; set; }

	public DarkTabControl()
	{
		base.DrawMode = TabDrawMode.OwnerDrawFixed;
		base.SizeMode = TabSizeMode.Fixed;
		base.Padding = new Point(10, 4);
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		if (_tabHeight > 0)
		{
			base.ItemSize = new Size(base.ItemSize.Width, _tabHeight + 2);
		}
	}

	private Rectangle GetCustomTabRect(int index)
	{
		if (TabHeight <= 0)
		{
			TabHeight = GetTabRect(0).Height;
		}
		int x = TabXOffset;
		for (int i = 0; i < index; i++)
		{
			x = ((TabWidths == null || i >= TabWidths.Length || TabWidths[i] <= 0) ? (x + (GetTabRect(i).Width + TabSpacing)) : (x + (TabWidths[i] + TabSpacing)));
		}
		int width = ((TabWidths == null || index >= TabWidths.Length || TabWidths[index] <= 0) ? GetTabRect(index).Width : TabWidths[index]);
		return new Rectangle(x, 0, width, TabHeight);
	}

	public void ApplyTheme(bool isDark)
	{
		if (isDark)
		{
			_headerBg = Color.FromArgb(35, 35, 35);
			_tabBg = Color.FromArgb(35, 35, 35);
			_tabSelectedBg = Color.FromArgb(50, 50, 50);
			_tabHoverBg = Color.FromArgb(60, 60, 60);
			_tabText = Color.FromArgb(180, 180, 180);
			_tabTextSelected = Color.White;
		}
		else
		{
			_headerBg = Color.FromArgb(248, 248, 252);
			_tabBg = Color.FromArgb(248, 248, 252);
			_tabSelectedBg = Color.FromArgb(243, 232, 252);
			_tabHoverBg = Color.FromArgb(240, 240, 245);
			_tabText = Color.FromArgb(100, 100, 115);
			_tabTextSelected = Color.FromArgb(55, 55, 65);
		}
		Invalidate();
	}

	public void ApplyTheme(AppTheme theme)
	{
		if (theme == null)
		{
			ApplyTheme(true);
			return;
		}
		SetColors(
			theme.HeaderBg,
			theme.BgAlt,
			theme.SelectRow,
			Color.FromArgb(210, theme.SelectRow),
			theme.TextSecondary,
			theme.TextPrimary);
	}

	public void SetColors(Color headerBg, Color tabBg, Color tabSelectedBg, Color tabHoverBg, Color tabText, Color tabTextSelected)
	{
		_headerBg = headerBg;
		_tabBg = tabBg;
		_tabSelectedBg = tabSelectedBg;
		_tabHoverBg = tabHoverBg;
		_tabText = tabText;
		_tabTextSelected = tabTextSelected;
		Invalidate();
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
		using SolidBrush br = new SolidBrush(_headerBg);
		e.Graphics.FillRectangle(br, e.ClipRectangle);
	}

	private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
	{
		return DrawingUtils.RoundedRect(rect, radius);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		if (base.TabPages.Count > 0)
		{
			int headerH = ((TabHeight > 0) ? TabHeight : GetTabRect(0).Height) + 4;
			using SolidBrush br = new SolidBrush(_headerBg);
			e.Graphics.FillRectangle(br, 0, 0, base.Width, headerH);
		}
		int cornerRadius = 6;
		for (int i = 0; i < base.TabPages.Count; i++)
		{
			Rectangle tabRect = GetCustomTabRect(i);
			bool isSelected = i == base.SelectedIndex;
			bool isHover = i == _hoverIndex;
			Color tabBg = (isSelected ? _tabSelectedBg : ((!isHover) ? _tabBg : _tabHoverBg));
			using (SolidBrush br2 = new SolidBrush(tabBg))
			{
				using (GraphicsPath path = GetRoundedRect(tabRect, cornerRadius))
				{
					e.Graphics.FillPath(br2, path);
				}
			}
			using SolidBrush br3 = new SolidBrush(isSelected ? _tabTextSelected : _tabText);
			StringFormat sf = new StringFormat
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
			e.Graphics.DrawString(base.TabPages[i].Text, Font, br3, tabRect, sf);
		}
		if (base.TabPages.Count > 0 && base.SelectedIndex >= 0)
		{
			Rectangle displayRect = DisplayRectangle;
			using SolidBrush br4 = new SolidBrush(_headerBg);
			e.Graphics.FillRectangle(br4, displayRect);
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		int newHover = -1;
		for (int i = 0; i < base.TabPages.Count; i++)
		{
			if (GetCustomTabRect(i).Contains(e.Location))
			{
				newHover = i;
				break;
			}
		}
		if (newHover != _hoverIndex)
		{
			_hoverIndex = newHover;
			Invalidate();
		}
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		if (_hoverIndex != -1)
		{
			_hoverIndex = -1;
			Invalidate();
		}
	}
}
