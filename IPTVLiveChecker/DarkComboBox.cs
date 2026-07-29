using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace IPTVLiveChecker;

public class DarkComboBox : ComboBox
{
	private const int WM_CTLCOLORLISTBOX = 308;

	private const int TRANSPARENT = 1;

	private Color _borderColor = Color.FromArgb(68, 68, 78);

	private Color _focusBorderColor = Color.FromArgb(88, 101, 242);

	private Color _backColor = Color.FromArgb(44, 44, 52);

	private Color _foreColor = Color.FromArgb(225, 225, 232);

	private Color _hoverBackColor;

	private Color _itemBackColor;

	private Color _itemSelectedBackColor;

	private Color _itemHoverBackColor;

	private bool _isHover;

	private bool _isFocused;

	private int _cornerRadius = 6;

	private Color _dropDownBackColor;

	private IntPtr _dropDownBrush = IntPtr.Zero;

	private Color _dropDownBrushColor = Color.Empty;

	public Color BorderColor
	{
		get
		{
			return _borderColor;
		}
		set
		{
			_borderColor = value;
			Invalidate();
		}
	}

	public Color FocusBorderColor
	{
		get
		{
			return _focusBorderColor;
		}
		set
		{
			_focusBorderColor = value;
			Invalidate();
		}
	}

	public new Color BackColor
	{
		get
		{
			return _backColor;
		}
		set
		{
			_backColor = value;
			base.BackColor = value;
			RecalcDerived();
			InvalidateDropDownBrush();
			Invalidate();
		}
	}

	public new Color ForeColor
	{
		get
		{
			return _foreColor;
		}
		set
		{
			_foreColor = value;
			base.ForeColor = value;
			Invalidate();
		}
	}

	public Color ItemBackColor
	{
		get
		{
			return _itemBackColor;
		}
		set
		{
			_itemBackColor = value;
			InvalidateDropDownBrush();
			Invalidate();
		}
	}

	public Color ItemSelectedBackColor
	{
		get
		{
			return _itemSelectedBackColor;
		}
		set
		{
			_itemSelectedBackColor = value;
			Invalidate();
		}
	}

	public Color ItemHoverBackColor
	{
		get
		{
			return _itemHoverBackColor;
		}
		set
		{
			_itemHoverBackColor = value;
			Invalidate();
		}
	}

	public int CornerRadius
	{
		get
		{
			return _cornerRadius;
		}
		set
		{
			_cornerRadius = value;
			UpdateRegion();
			Invalidate();
		}
	}

	[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
	private static extern IntPtr CreateSolidBrush(int crColor);

	[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
	private static extern bool DeleteObject(IntPtr hObject);

	[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
	private static extern int SetBkColor(IntPtr hdc, int crColor);

	[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
	private static extern int SetTextColor(IntPtr hdc, int crColor);

	[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
	private static extern int SetBkMode(IntPtr hdc, int iBkMode);

	public DarkComboBox()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		base.DrawMode = DrawMode.OwnerDrawFixed;
		base.DropDownStyle = ComboBoxStyle.DropDownList;
		base.FlatStyle = FlatStyle.Flat;
		base.BackColor = _backColor;
		base.ForeColor = _foreColor;
		RecalcDerived();
		_itemBackColor = _backColor;
		_itemSelectedBackColor = Color.FromArgb(Math.Min(255, _backColor.R + 25), Math.Min(255, _backColor.G + 25), Math.Min(255, _backColor.B + 30));
		_itemHoverBackColor = Color.FromArgb(Math.Min(255, _backColor.R + 18), Math.Min(255, _backColor.G + 18), Math.Min(255, _backColor.B + 22));
		_borderColor = Color.FromArgb(Math.Max(0, _backColor.R - 20), Math.Max(0, _backColor.G - 20), Math.Max(0, _backColor.B - 18));
	}

	private void RecalcDerived()
	{
		_hoverBackColor = Color.FromArgb(Math.Min(255, _backColor.R + 12), Math.Min(255, _backColor.G + 12), Math.Min(255, _backColor.B + 12));
		_dropDownBackColor = _backColor;
	}

	private void InvalidateDropDownBrush()
	{
		if (_dropDownBrush != IntPtr.Zero)
		{
			DeleteObject(_dropDownBrush);
			_dropDownBrush = IntPtr.Zero;
		}
	}

	private void EnsureDropDownBrush()
	{
		if (_dropDownBrush == IntPtr.Zero || _dropDownBrushColor != _itemBackColor)
		{
			if (_dropDownBrush != IntPtr.Zero)
			{
				DeleteObject(_dropDownBrush);
			}
			_dropDownBrushColor = _itemBackColor;
			_dropDownBrush = CreateSolidBrush(ColorTranslator.ToWin32(_itemBackColor));
		}
	}

	protected override void OnBackColorChanged(EventArgs e)
	{
		base.OnBackColorChanged(e);
		if (_backColor != base.BackColor)
		{
			_backColor = base.BackColor;
			RecalcDerived();
			_itemBackColor = _backColor;
			InvalidateDropDownBrush();
			Invalidate();
		}
	}

	protected override void OnForeColorChanged(EventArgs e)
	{
		base.OnForeColorChanged(e);
		if (_foreColor != base.ForeColor)
		{
			_foreColor = base.ForeColor;
			Invalidate();
		}
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		UpdateRegion();
	}

	protected override void Dispose(bool disposing)
	{
		if (_dropDownBrush != IntPtr.Zero)
		{
			DeleteObject(_dropDownBrush);
			_dropDownBrush = IntPtr.Zero;
		}
		base.Dispose(disposing);
	}

	protected override void WndProc(ref Message m)
	{
		if (m.Msg == 308)
		{
			IntPtr wParam = m.WParam;
			SetBkColor(wParam, ColorTranslator.ToWin32(_itemBackColor));
			SetTextColor(wParam, ColorTranslator.ToWin32(_foreColor));
			SetBkMode(wParam, 1);
			EnsureDropDownBrush();
			m.Result = _dropDownBrush;
		}
		else
		{
			base.WndProc(ref m);
		}
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		UpdateRegion();
		Invalidate();
	}

	private void UpdateRegion()
	{
		if (base.IsHandleCreated && base.Width > 0 && base.Height > 0)
		{
			using (GraphicsPath path = GetRoundedRectPath(new Rectangle(0, 0, base.Width - 1, base.Height - 1), _cornerRadius))
			{
				base.Region = new Region(path);
			}
		}
	}

	private static GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
	{
		return DrawingUtils.RoundedRect(rect, radius);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
		Color bg = (_isHover ? _hoverBackColor : _backColor);
		Color bc = (_isFocused ? _focusBorderColor : _borderColor);
		float penWidth = (_isFocused ? 1.5f : 1f);
		using (GraphicsPath path = GetRoundedRectPath(new Rectangle(0, 0, base.Width - 1, base.Height - 1), _cornerRadius))
		{
			using (SolidBrush br = new SolidBrush(bg))
			{
				g.FillPath(br, path);
			}
			using Pen pen = new Pen(bc, penWidth);
			g.DrawPath(pen, path);
		}
		if (!string.IsNullOrEmpty(Text))
		{
			TextRenderer.DrawText(g, Text, Font, new Rectangle(8, 0, base.Width - 28, base.Height), _foreColor, TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
		}
		DrawArrow(g, bc);
	}

	private void DrawArrow(Graphics g, Color arrowColor)
	{
		using SolidBrush arrow = new SolidBrush(arrowColor);
		int ax = base.Width - 18;
		int ay = base.Height / 2;
		if (base.DroppedDown)
		{
			Point[] tri = new Point[3]
			{
				new Point(ax, ay + 3),
				new Point(ax + 8, ay + 3),
				new Point(ax + 4, ay - 2)
			};
			g.FillPolygon(arrow, tri);
		}
		else
		{
			Point[] tri2 = new Point[3]
			{
				new Point(ax, ay - 2),
				new Point(ax + 8, ay - 2),
				new Point(ax + 4, ay + 3)
			};
			g.FillPolygon(arrow, tri2);
		}
	}

	protected override void OnDrawItem(DrawItemEventArgs e)
	{
		if (e.Index >= 0)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
			bool num = (e.State & DrawItemState.Selected) != 0;
			bool isHover = (e.State & DrawItemState.HotLight) != 0;
			Color bgColor = _itemBackColor;
			if (num)
			{
				bgColor = _itemSelectedBackColor;
			}
			else if (isHover)
			{
				bgColor = _itemHoverBackColor;
			}
			using (SolidBrush br = new SolidBrush(bgColor))
			{
				e.Graphics.FillRectangle(br, e.Bounds);
			}
			string text = base.Items[e.Index].ToString();
			TextRenderer.DrawText(e.Graphics, text, e.Font, new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 16, e.Bounds.Height), _foreColor, TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
		}
	}

	protected override void OnMouseEnter(EventArgs e)
	{
		base.OnMouseEnter(e);
		_isHover = true;
		Invalidate();
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		_isHover = false;
		Invalidate();
	}

	protected override void OnEnter(EventArgs e)
	{
		base.OnEnter(e);
		_isFocused = true;
		Invalidate();
	}

	protected override void OnLeave(EventArgs e)
	{
		base.OnLeave(e);
		_isFocused = false;
		_isHover = false;
		Invalidate();
	}

	protected override void OnDropDown(EventArgs e)
	{
		base.OnDropDown(e);
		Invalidate();
	}

	protected override void OnDropDownClosed(EventArgs e)
	{
		base.OnDropDownClosed(e);
		Invalidate();
	}
}
