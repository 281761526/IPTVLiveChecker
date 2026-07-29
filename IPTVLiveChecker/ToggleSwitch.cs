using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using IPTVLiveChecker.Animations;

namespace IPTVLiveChecker;

public class ToggleSwitch : Control
{
	private bool _checked;

	private bool _targetChecked;

	private float _animProgress;

	private Tween _tween;

	public bool Checked
	{
		get
		{
			return _checked;
		}
		set
		{
			if (_checked == value)
			{
				return;
			}
			_targetChecked = value;
			if (!base.IsHandleCreated)
			{
				_checked = value;
				_animProgress = 1f;
				OnCheckedChanged(EventArgs.Empty);
				return;
			}
			EnsureTween();
			float target = (_targetChecked ? 1f : 0f);
			_tween.To(target, 220, Easing.EaseOutCubic, delegate(float v)
			{
				_animProgress = v;
				Invalidate();
			}, delegate
			{
				_checked = _targetChecked;
				_animProgress = target;
				Invalidate();
			});
			OnCheckedChanged(EventArgs.Empty);
		}
	}

	public string OnText { get; set; } = "开";

	public string OffText { get; set; } = "关";

	public Color OnColor { get; set; } = Color.FromArgb(46, 169, 92);

	public Color OffColor { get; set; } = Color.FromArgb(205, 205, 210);

	public event EventHandler<ToggleChangingEventArgs> ToggleChanging;

	public event EventHandler CheckedChanged;

	protected virtual void OnCheckedChanged(EventArgs e)
	{
		this.CheckedChanged?.Invoke(this, e);
	}

	public ToggleSwitch()
	{
		base.Size = new Size(110, 36);
		DoubleBuffered = true;
		Cursor = Cursors.Hand;
		Font = IPTVLiveCheckerMain.GetFont(11.5f);
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		_targetChecked = _checked;
		base.HandleCreated += delegate
		{
			Invalidate();
		};
		base.VisibleChanged += delegate
		{
			if (base.Visible)
			{
				Invalidate();
			}
		};
		base.ParentChanged += ToggleSwitch_ParentChanged;
	}

	private void ToggleSwitch_ParentChanged(object sender, EventArgs e)
	{
		Invalidate();
	}

	protected override void OnVisibleChanged(EventArgs e)
	{
		base.OnVisibleChanged(e);
		if (base.Visible)
		{
			Invalidate();
		}
	}

	protected override void OnLocationChanged(EventArgs e)
	{
		base.OnLocationChanged(e);
		Invalidate();
	}

	private void EnsureTween()
	{
		if (_tween == null)
		{
			_tween = new Tween();
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && _tween != null)
		{
			_tween.Dispose();
			_tween = null;
		}
		base.Dispose(disposing);
	}

	private Color GetRealBackColor()
	{
		for (Control ctrl = base.Parent; ctrl != null; ctrl = ctrl.Parent)
		{
			if (ctrl.BackColor != Color.Transparent)
			{
				return ctrl.BackColor;
			}
		}
		return Color.White;
	}

	protected override void OnPaintBackground(PaintEventArgs pevent)
	{
	}

	protected override void OnClick(EventArgs e)
	{
		ToggleChangingEventArgs args = new ToggleChangingEventArgs(!_checked);
		this.ToggleChanging?.Invoke(this, args);
		if (!args.Cancel)
		{
			Checked = !_checked;
		}
		base.OnClick(e);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.PixelOffsetMode = PixelOffsetMode.HighQuality;
		g.InterpolationMode = InterpolationMode.HighQualityBicubic;
		using (SolidBrush bgBrush = new SolidBrush(GetRealBackColor()))
		{
			g.FillRectangle(bgBrush, base.ClientRectangle);
		}
		int w = base.Width;
		int num = base.Height;
		int pillH = Math.Min(num - 4, 32);
		int pillY = (num - pillH) / 2;
		int pillR = pillH / 2;
		Rectangle pillRect = new Rectangle(0, pillY, w - 1, pillH - 1);
		float t = Math.Max(0f, Math.Min(1f, _animProgress));
		using (SolidBrush br = new SolidBrush(DrawingUtils.LerpColor(OffColor, OnColor, t)))
		{
			using GraphicsPath path = IPTVLiveCheckerMain.GetRoundedPath(pillRect, pillR);
			g.FillPath(br, path);
		}
		int dotMargin = 3;
		int dotSize = pillH - dotMargin * 2;
		int dotY = pillY + dotMargin;
		int dotXOff = dotMargin;
		int dotXOn = w - dotSize - dotMargin - 1;
		int dotX = (int)((float)dotXOff + (float)(dotXOn - dotXOff) * t);
		using SolidBrush dotBrush = new SolidBrush(Color.White);
		g.FillEllipse(dotBrush, dotX, dotY, dotSize, dotSize);
	}
}
