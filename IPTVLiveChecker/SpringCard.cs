using System;
using System.Drawing;
using System.Windows.Forms;

namespace IPTVLiveChecker;

/// <summary>
/// 可橡皮筋拖拽的卡片：在父容器内小幅「信封」内自由跟手，超出信封渐进阻尼，
/// 松手按指尖速度回弹归位。用于「频道详情」信息卡，演示 HTML 演示稿的
/// 速度交接 + 橡皮筋交互——收敛为面板局部弹性微交互，不做整窗拖拽。
///
/// 弹簧参数与演示稿一致：damping 1.0 / response 0.45。
/// </summary>
public class SpringCard : Panel
{
	private readonly Spring1D _sx = new Spring1D(0.0, 0.45, 1.0);
	private readonly Spring1D _sy = new Spring1D(0.0, 0.45, 1.0);
	private Rectangle _home;
	private bool _drag;
	private Point _grab;       // 抓取点相对卡片左上角
	private Point _last;       // 上一帧屏幕指针位置（估算速度）
	private DateTime _lastT;
	private double _vx, _vy;   // 指尖速度（px/s）
	private readonly System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer { Interval = 16 };
	private Control _grip;     // 拖拽握把（标题栏）

	private const double Envelope = 26;   // 自由拖拽信封半径（px）
	private const double Rubber = 0.32;   // 信封外渐进阻尼系数

	public SpringCard()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
		_timer.Tick += Tick;
	}

	/// <summary>设置归位矩形（屏幕上卡片停靠的“家”位置）。非拖拽时立即应用。</summary>
	public void SetHome(int x, int y, int w, int h)
	{
		_home = new Rectangle(x, y, w, h);
		Size = new Size(w, h);
		if (!_drag)
		{
			Location = new Point(x, y);
		}
	}

	/// <summary>指定拖拽握把（通常是标题栏），仅从其上按下才可拖动。</summary>
	public void SetGrip(Control grip)
	{
		_grip = grip;
		grip.MouseDown += OnGripDown;
		grip.Cursor = Cursors.SizeAll;
	}

	private void OnGripDown(object sender, MouseEventArgs e)
	{
		if (AnimationSettings.ReduceMotion)
		{
			return;
		}
		_drag = true;
		_grab = e.Location;
		_last = Cursor.Position;
		_lastT = DateTime.Now;
		_vx = _vy = 0;
		_sx.Velocity = 0;
		_sy.Velocity = 0;
		Capture = true;
		_timer.Start();
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (!_drag)
		{
			return;
		}
		Point cur = Cursor.Position;
		double dt = (DateTime.Now - _lastT).TotalSeconds;
		if (dt > 0)
		{
			_vx = (cur.X - _last.X) / dt;
			_vy = (cur.Y - _last.Y) / dt;
		}
		_last = cur;
		_lastT = DateTime.Now;

		// 当前指针相对“家”左上角的偏移
		Point homeScreen = Parent.PointToScreen(new Point(_home.X, _home.Y));
		double ox = (cur.X - _grab.X) - homeScreen.X;
		double oy = (cur.Y - _grab.Y) - homeScreen.Y;
		_sx.Value = RubberClamp(ox);
		_sy.Value = RubberClamp(oy);
		_sx.Target = _sx.Value;
		_sy.Target = _sy.Value;
		ApplyLocation();
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		if (!_drag)
		{
			return;
		}
		_drag = false;
		Capture = false;
		// 速度交接：把指尖速度交给弹簧，目标归零 → 自然回弹归位
		_sx.Velocity = _vx;
		_sy.Velocity = _vy;
		_sx.Target = 0;
		_sy.Target = 0;
		_timer.Start();
	}

	private void Tick(object sender, EventArgs e)
	{
		bool a = _sx.Step(0.016);
		bool b = _sy.Step(0.016);
		ApplyLocation();
		if (!a && !b && !_drag)
		{
			_sx.SnapTo(0);
			_sy.SnapTo(0);
			ApplyLocation();
			_timer.Stop();
		}
	}

	private void ApplyLocation()
	{
		int x = _home.X + (int)Math.Round(_sx.Value);
		int y = _home.Y + (int)Math.Round(_sy.Value);
		if (Location.X != x || Location.Y != y)
		{
			Location = new Point(x, y);
		}
	}

	/// <summary>橡皮筋：信封内自由跟随，信封外渐进阻尼（越拉越“重”）。</summary>
	private static double RubberClamp(double v)
	{
		double s = Math.Sign(v);
		v = Math.Abs(v);
		if (v <= Envelope)
		{
			return s * v;
		}
		double over = v - Envelope;
		return s * (Envelope + over * Rubber);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_timer.Stop();
			_timer.Dispose();
		}
		base.Dispose(disposing);
	}
}
