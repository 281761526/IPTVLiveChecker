using System;
using System.Windows.Forms;

namespace IPTVLiveChecker.Animations;

public sealed class Tween
{
	private readonly Timer _timer;

	private float _from;

	private float _to;

	private float _value;

	private int _elapsed;

	private int _duration;

	private Easing _easing;

	private Action<float> _onUpdate;

	private Action _onComplete;

	public float Value => _value;

	public bool IsRunning => _timer.Enabled;

	public Tween()
	{
		_timer = new Timer
		{
			Interval = 16
		};
		_timer.Tick += delegate
		{
			Step();
		};
	}

	public void To(float to, int durationMs, Easing easing, Action<float> onUpdate, Action onComplete = null)
	{
		_onUpdate = onUpdate;
		_onComplete = onComplete;
		if (AnimationSettings.ReduceMotion)
		{
			_value = to;
			_onUpdate?.Invoke(to);
			_onComplete?.Invoke();
			return;
		}
		_from = _value;
		_to = to;
		_duration = Math.Max(1, durationMs);
		_elapsed = 0;
		_easing = easing;
		if (!_timer.Enabled)
		{
			_timer.Start();
		}
	}

	public void Stop()
	{
		_timer.Stop();
	}

	private void Step()
	{
		_elapsed += _timer.Interval;
		float t = Math.Min(1f, (float)_elapsed / (float)_duration);
		_value = _from + (_to - _from) * Ease(t, _easing);
		_onUpdate?.Invoke(_value);
		if (t >= 1f)
		{
			_timer.Stop();
			_onComplete?.Invoke();
		}
	}

	private static float Ease(float t, Easing e)
	{
		switch (e)
		{
		case Easing.EaseOutCubic:
			return 1f - (float)Math.Pow(1f - t, 3.0);
		case Easing.EaseInOutCubic:
			if (!(t < 0.5f))
			{
				return 1f - (float)Math.Pow(-2f * t + 2f, 3.0) / 2f;
			}
			return 4f * t * t * t;
		case Easing.EaseOutBack:
			return 1f + 2.70158f * (float)Math.Pow(t - 1f, 3.0) + 1.70158f * (float)Math.Pow(t - 1f, 2.0);
		default:
			return t;
		}
	}

	public void Dispose()
	{
		_timer.Stop();
		_timer.Dispose();
	}
}
