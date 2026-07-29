using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace IPTVLiveChecker;

public class DarkScrollBar : Control
{
	private int _minimum;

	private int _maximum = 100;

	private int _value;

	private int _largeChange = 10;

	private int _smallChange = 1;

	private bool _dragging;

	private int _dragStartY;

	private int _dragStartValue;

	private Rectangle _thumbRect = Rectangle.Empty;

	private bool _thumbHovered;

	public int Minimum
	{
		get
		{
			return _minimum;
		}
		set
		{
			_minimum = value;
			ClampValue();
			UpdateThumb();
			Invalidate();
		}
	}

	public int Maximum
	{
		get
		{
			return _maximum;
		}
		set
		{
			_maximum = value;
			ClampValue();
			UpdateThumb();
			Invalidate();
		}
	}

	public int Value
	{
		get
		{
			return _value;
		}
		set
		{
			int newValue = Math.Max(_minimum, Math.Min(_maximum, value));
			if (newValue != _value)
			{
				_value = newValue;
				UpdateThumb();
				Invalidate();
				OnValueChanged(EventArgs.Empty);
			}
		}
	}

	public int LargeChange
	{
		get
		{
			return _largeChange;
		}
		set
		{
			_largeChange = Math.Max(1, value);
			UpdateThumb();
			Invalidate();
		}
	}

	public int SmallChange
	{
		get
		{
			return _smallChange;
		}
		set
		{
			_smallChange = Math.Max(1, value);
		}
	}

	public Color TrackColor { get; set; } = Color.FromArgb(45, 45, 55);

	public Color ThumbColor { get; set; } = Color.FromArgb(120, 120, 130);

	public Color ThumbHoverColor { get; set; } = Color.FromArgb(150, 150, 160);

	public Color ThumbPressedColor { get; set; } = Color.FromArgb(170, 170, 180);

	public event EventHandler ValueChanged;

	public DarkScrollBar()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		base.Width = SystemInformation.VerticalScrollBarWidth;
		MinimumSize = new Size(8, 30);
		Cursor = Cursors.Arrow;
	}

	protected virtual void OnValueChanged(EventArgs e)
	{
		this.ValueChanged?.Invoke(this, e);
	}

	private void ClampValue()
	{
		if (_value < _minimum)
		{
			_value = _minimum;
		}
		if (_value > _maximum)
		{
			_value = _maximum;
		}
	}

	private void UpdateThumb()
	{
		if (_maximum <= _minimum || base.ClientSize.Height <= 0)
		{
			_thumbRect = Rectangle.Empty;
			return;
		}
		int trackH = base.ClientSize.Height;
		int range = Math.Max(1, _maximum - _minimum + _largeChange);
		int thumbH = Math.Max(30, (int)((double)_largeChange / (double)range * (double)trackH));
		int usableH = Math.Max(1, trackH - thumbH);
		int thumbY = (int)((double)(_value - _minimum) / (double)Math.Max(1, _maximum - _minimum) * (double)usableH);
		int thumbW = Math.Max(4, base.ClientSize.Width - 4);
		int thumbX = (base.ClientSize.Width - thumbW) / 2;
		_thumbRect = new Rectangle(thumbX, thumbY, thumbW, thumbH);
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		UpdateThumb();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		using (SolidBrush trackBrush = new SolidBrush(TrackColor))
		{
			g.FillRectangle(trackBrush, base.ClientRectangle);
		}
		if (_thumbRect.Height <= 0)
		{
			return;
		}
		using SolidBrush thumbBrush = new SolidBrush(_dragging ? ThumbPressedColor : (_thumbHovered ? ThumbHoverColor : ThumbColor));
		int radius = Math.Min(_thumbRect.Width, _thumbRect.Height) / 2;
		using GraphicsPath path = RoundedRect(_thumbRect, radius);
		g.FillPath(thumbBrush, path);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (e.Button == MouseButtons.Left)
		{
			if (_thumbRect.Contains(e.Location))
			{
				_dragging = true;
				_dragStartY = e.Y;
				_dragStartValue = _value;
				Invalidate();
			}
			else if (e.Y < _thumbRect.Top)
			{
				Value = Math.Max(_minimum, _value - _largeChange);
			}
			else if (e.Y > _thumbRect.Bottom)
			{
				Value = Math.Min(_maximum, _value + _largeChange);
			}
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		bool wasHovered = _thumbHovered;
		_thumbHovered = _thumbRect.Contains(e.Location);
		if (_thumbHovered != wasHovered && !_dragging)
		{
			Invalidate();
		}
		if (_dragging)
		{
			int num = e.Y - _dragStartY;
			int trackH = base.ClientSize.Height;
			int thumbH = _thumbRect.Height;
			int usableH = Math.Max(1, trackH - thumbH);
			int deltaValue = (int)((double)num / (double)usableH * (double)Math.Max(1, _maximum - _minimum));
			Value = _dragStartValue + deltaValue;
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		if (_dragging)
		{
			_dragging = false;
			Invalidate();
		}
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		if (_thumbHovered)
		{
			_thumbHovered = false;
			if (!_dragging)
			{
				Invalidate();
			}
		}
	}

	private static GraphicsPath RoundedRect(Rectangle rect, int radius)
	{
		return DrawingUtils.RoundedRect(rect, radius);
	}
}
