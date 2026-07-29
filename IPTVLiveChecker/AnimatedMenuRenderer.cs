using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace IPTVLiveChecker;

/// <summary>
/// 带悬停高亮过渡动效的菜单渲染器。监听每个菜单项的 Selected 状态，
/// 用 Timer 将高亮 alpha 在 0~255 间平滑过渡，呈现淡入淡出效果。
/// </summary>
internal class AnimatedMenuRenderer : ToolStripProfessionalRenderer, IDisposable
{
	private readonly AppTheme _theme;
	private readonly Dictionary<ToolStripItem, byte> _alpha = new Dictionary<ToolStripItem, byte>();
	private readonly HashSet<ToolStripItem> _items = new HashSet<ToolStripItem>();
	private readonly Timer _timer;
	private bool _disposed;

	public AnimatedMenuRenderer(AppTheme theme) : base(new MenuColorTable(theme))
	{
		_theme = theme;
		int tickInterval = AnimationSettings.ReduceMotion ? 0 : 24;
		if (tickInterval > 0)
		{
			_timer = new Timer { Interval = tickInterval };
			_timer.Tick += (s, e) => Tick();
			_timer.Start();
		}
	}

	/// <summary>
	/// 注册一个 ContextMenuStrip，使其所有菜单项（含子菜单）具备悬停动效。
	/// 一次性菜单（如播放菜单）在关闭时释放内部计时器，避免泄漏。
	/// </summary>
	public void Register(ContextMenuStrip cms)
	{
		cms.Opening += (s, e) => Collect(cms.Items);
		cms.Closed += (s, e) => Dispose();
		Collect(cms.Items);
	}

	private void Collect(ToolStripItemCollection items)
	{
		foreach (ToolStripItem item in items)
		{
			if (_items.Add(item))
			{
				_alpha[item] = 0;
			}
			if (item is ToolStripMenuItem mi && mi.DropDownItems.Count > 0)
			{
				mi.DropDown.Opening += (s, e) => Collect(mi.DropDownItems);
				Collect(mi.DropDownItems);
			}
		}
	}

	private void Tick()
	{
		foreach (ToolStripItem item in _items)
		{
			byte target = item.Selected ? (byte)255 : (byte)0;
			byte cur = _alpha[item];
			if (cur == target)
			{
				continue;
			}
			int step = target > cur ? 30 : -30;
			int next = cur + step;
			if (target > cur && next > target)
			{
				next = target;
			}
			else if (target < cur && next < target)
			{
				next = target;
			}
			_alpha[item] = (byte)next;
			item.Owner?.Invalidate(item.Bounds);
		}
	}

	protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
	{
		ToolStripItem item = e.Item;
		if (_alpha.TryGetValue(item, out byte a) && a > 0)
		{
			using Brush brush = new SolidBrush(Color.FromArgb(a, _theme.SelectRow));
			e.Graphics.FillRectangle(brush, item.Bounds);
		}
		else
		{
			base.OnRenderMenuItemBackground(e);
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		if (_timer != null)
		{
			_timer.Stop();
			_timer.Dispose();
		}
	}
}
