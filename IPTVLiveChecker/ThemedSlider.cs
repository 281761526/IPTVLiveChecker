using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace IPTVLiveChecker;

/// <summary>
/// iOS/macOS 风格的圆角滑块，用于替代系统 TrackBar。
/// 支持主题色填充、圆角轨道、圆形 Thumb 与鼠标拖拽，避免系统 TrackBar 无法主题化的问题。
/// </summary>
public class ThemedSlider : Control
{
    private int _minimum;
    private int _maximum = 100;
    private int _value;
    private bool _dragging;

    public ThemedSlider()
    {
        // 必须包含 SupportsTransparentBackColor，否则给 BackColor 赋透明色时
        // WinForms 会抛 ArgumentException("控件不支持透明的背景色。")
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.Selectable,
            value: true);

        Cursor = Cursors.Hand;
        TabStop = true;
        MinimumSize = new Size(20, ThumbSize + 4);
        base.BackColor = Color.Transparent;
    }

    [Category("Behavior")]
    [DefaultValue(0)]
    public int Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_value < _minimum) Value = _minimum;
            Invalidate();
        }
    }

    [Category("Behavior")]
    [DefaultValue(100)]
    public int Maximum
    {
        get => _maximum;
        set
        {
            _maximum = Math.Max(_minimum, value);
            if (_value > _maximum) Value = _maximum;
            Invalidate();
        }
    }

    [Category("Behavior")]
    [DefaultValue(0)]
    public int Value
    {
        get => _value;
        set
        {
            int clamped = Math.Max(_minimum, Math.Min(_maximum, value));
            if (_value != clamped)
            {
                _value = clamped;
                Invalidate();
                OnValueChanged(EventArgs.Empty);
            }
        }
    }

    [Category("Appearance")]
    [DefaultValue(typeof(Color), "60, 60, 70")]
    public Color TrackColor { get; set; } = Color.FromArgb(60, 60, 70);

    [Category("Appearance")]
    [DefaultValue(typeof(Color), "0, 122, 255")]
    public Color FillColor { get; set; } = Color.FromArgb(0, 122, 255);

    [Category("Appearance")]
    [DefaultValue(typeof(Color), "White")]
    public Color ThumbColor { get; set; } = Color.White;

    [Category("Appearance")]
    [DefaultValue(14)]
    public int ThumbSize { get; set; } = 14;

    [Category("Appearance")]
    [DefaultValue(6)]
    public int TrackHeight { get; set; } = 6;

    [Category("Appearance")]
    [DefaultValue(true)]
    public bool DrawThumb { get; set; } = true;

    [Browsable(true)]
    [Category("Action")]
    public event EventHandler ValueChanged;

    protected virtual void OnValueChanged(EventArgs e)
    {
        ValueChanged?.Invoke(this, e);
    }

    private float ValueToX(int v)
    {
        if (_maximum <= _minimum) return ThumbSize / 2f;
        double ratio = (double)(v - _minimum) / (_maximum - _minimum);
        int usable = Math.Max(0, ClientRectangle.Width - ThumbSize);
        return ThumbSize / 2f + (float)(ratio * usable);
    }

    private int XToValue(int x)
    {
        if (_maximum <= _minimum) return _minimum;
        int usable = Math.Max(0, ClientRectangle.Width - ThumbSize);
        if (usable == 0) return _minimum;
        double ratio = (double)(x - ThumbSize / 2) / usable;
        ratio = Math.Max(0.0, Math.Min(1.0, ratio));
        return (int)Math.Round(_minimum + ratio * (_maximum - _minimum));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        Rectangle rect = ClientRectangle;
        int cy = rect.Height / 2;
        int halfTrack = Math.Max(1, TrackHeight / 2);
        int startX = ThumbSize / 2;
        int endX = rect.Width - ThumbSize / 2;
        if (endX <= startX) endX = startX + 1;

        int thumbX = (int)ValueToX(_value);

        // 轨道背景（未填充部分）
        using (GraphicsPath trackPath = DrawingUtils.RoundedRect(
            new Rectangle(startX, cy - halfTrack, endX - startX, TrackHeight),
            TrackHeight / 2))
        using (SolidBrush trackBrush = new SolidBrush(TrackColor))
        {
            g.FillPath(trackBrush, trackPath);
        }

        // 已填充部分
        if (_value > _minimum)
        {
            int fillW = Math.Max(TrackHeight, thumbX - startX);
            using (GraphicsPath fillPath = DrawingUtils.RoundedRect(
                new Rectangle(startX, cy - halfTrack, fillW, TrackHeight),
                TrackHeight / 2))
            using (SolidBrush fillBrush = new SolidBrush(FillColor))
            {
                g.FillPath(fillBrush, fillPath);
            }
        }

        // Thumb（圆形）
        if (DrawThumb)
        {
            int halfThumb = ThumbSize / 2;
            Rectangle thumbRect = new Rectangle(thumbX - halfThumb, cy - halfThumb, ThumbSize, ThumbSize);
            using (SolidBrush thumbBrush = new SolidBrush(ThumbColor))
            {
                g.FillEllipse(thumbBrush, thumbRect);
            }
            // 极细描边，让 thumb 在不同背景下都有轮廓
            Color edgeColor = DrawingUtils.IsDarkColor(ThumbColor)
                ? Color.FromArgb(40, 255, 255, 255)
                : Color.FromArgb(30, 0, 0, 0);
            using (Pen edgePen = new Pen(edgeColor, 1f))
            {
                g.DrawEllipse(edgePen, thumbRect);
            }
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left && Enabled)
        {
            _dragging = true;
            Value = XToValue(e.X);
            Focus();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging && Enabled)
        {
            Value = XToValue(e.X);
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _dragging = false;
    }

    protected override void OnLeave(EventArgs e)
    {
        base.OnLeave(e);
        _dragging = false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!Enabled) return;
        int step = Math.Max(1, (_maximum - _minimum) / 100);
        if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down)
        {
            Value -= step;
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up)
        {
            Value += step;
            e.Handled = true;
        }
    }
}
