using System;

namespace IPTVLiveChecker;

/// <summary>
/// 一维弹簧求解器：临界阻尼比（damping ratio）+ 响应时间（秒）。
/// 采用半隐式欧拉 + 子步进，保证数值稳定；切换目标时保留当前速度（速度交接）。
/// 默认 damping=1.0（临界阻尼，无过冲）、response=0.4 秒 —— 与 HTML 演示稿一致。
///
/// 物理模型（单位质量）：
///   omega = 2*pi / response
///   a = -omega^2 * (x - target) - 2*zeta*omega * v
/// </summary>
public sealed class Spring1D
{
	public double Value;
	public double Velocity;
	public double Target;
	public double DampingRatio = 1.0;
	public double Response = 0.4;

	public Spring1D(double initial = 0, double response = 0.4, double damping = 1.0)
	{
		Value = initial;
		Target = initial;
		Response = response;
		DampingRatio = damping;
	}

	/// <summary>推进 dt 秒。返回是否仍在运动（可据此停止计时器）。</summary>
	public bool Step(double dt)
	{
		if (dt <= 0)
		{
			return Math.Abs(Velocity) > 1e-3 || Math.Abs(Value - Target) > 1e-3;
		}

		// 子步进：单步不超过 1/120 秒，保证大 dt 下也稳定
		const double maxStep = 1.0 / 120.0;
		int steps = Math.Max(1, (int)Math.Ceiling(dt / maxStep));
		double h = dt / steps;
		double omega = (2.0 * Math.PI) / Response;
		double k = omega * omega;
		double c = 2.0 * DampingRatio * omega;
		for (int i = 0; i < steps; i++)
		{
			double a = -k * (Value - Target) - c * Velocity;
			Velocity += a * h;
			Value += Velocity * h;
		}

		return Math.Abs(Velocity) > 1e-3 || Math.Abs(Value - Target) > 1e-3;
	}

	/// <summary>立即吸附到目标（减少动态效果时调用）。</summary>
	public void SnapTo(double target)
	{
		Target = target;
		Value = target;
		Velocity = 0;
	}
}
