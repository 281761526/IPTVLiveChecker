using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace IPTVLiveChecker
{
    /// <summary>
    /// 预览窗「未播放」占位层：叠在视频控件之上，空闲时绘制与主窗同相、同色的流动极光 +
    /// 提示文字；开始播放时自动隐藏、露出视频。替代原本死黑的视频表面，让动态主题在预览窗完整落地。
    /// </summary>
    internal class PlayerIdleOverlay : Panel
    {
        private AppTheme _theme;
        private bool _immersive;
        private bool _playing;
        private readonly Timer _animTimer;

        public PlayerIdleOverlay()
        {
            Dock = DockStyle.Fill;
            Visible = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            DoubleBuffered = true;
            _animTimer = new Timer { Interval = 110 };
            _animTimer.Tick += delegate
            {
                // 仅在可见 + 空闲 + 沉浸式 + 未减少动态 + 鼠标静止时重绘（与网格节流一致，避免卡顿）
                if (Visible && !_playing && _immersive && !AnimationSettings.ReduceMotion && !ThemeFx.IsMouseMoving)
                {
                    Invalidate();
                }
            };
            _animTimer.Start();
        }

        /// <summary>换肤时调用：传入当前主题与是否沉浸式（玻璃/动效主题）。</summary>
        public void Configure(AppTheme theme, bool immersive)
        {
            _theme = theme;
            _immersive = immersive;
            Invalidate();
        }

        /// <summary>播放状态切换：播放中隐藏占位、露出视频；空闲时显示占位。</summary>
        public void SetPlaying(bool playing)
        {
            if (_playing == playing)
            {
                return;
            }
            _playing = playing;
            Visible = !playing;
            if (Visible)
            {
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_theme == null)
            {
                return;
            }
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.CompositingQuality = CompositingQuality.Default;
            int w = ClientSize.Width;
            int h = ClientSize.Height;
            if (w <= 0 || h <= 0)
            {
                return;
            }

            bool hasStops = _immersive && _theme.GradientStops != null && _theme.GradientStops.Count >= 2;
            if (hasStops)
            {
                DrawAuroraBackdrop(g, w, h);
            }
            else
            {
                // 非沉浸式：主题实色渐变占位，仍避免死黑
                using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(0, 0, w, h), _theme.Bg, _theme.BgAlt, LinearGradientMode.Vertical))
                {
                    g.FillRectangle(b, 0, 0, w, h);
                }
            }

            DrawHint(g, w, h);
        }

        private void DrawAuroraBackdrop(Graphics g, int w, int h)
        {
            var stops = _theme.GradientStops;
            double phase = AnimationSettings.ReduceMotion ? 0 : ThemeFx.CurrentPhase;

            // 同主窗极光：竖向基础渐变（暗化色阶）
            Color bg0 = DrawingUtils.Darken(stops[0], 0.55);
            Color bg1 = DrawingUtils.Darken(stops[stops.Count - 1], 0.25);
            int baseAlpha = _theme.GlassEnabled ? Math.Min(180, _theme.GlassOpacity > 0 ? _theme.GlassOpacity : 180) : 255;
            using (LinearGradientBrush baseBrush = new LinearGradientBrush(new Rectangle(0, 0, w, h),
                Color.FromArgb(baseAlpha, bg0.R, bg0.G, bg0.B),
                Color.FromArgb(baseAlpha, bg1.R, bg1.G, bg1.B),
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(baseBrush, 0, 0, w, h);
            }

            // 三个流动光斑（与主窗 DrawAurora 同相）
            Color col0 = stops[1];
            Color col1 = stops[Math.Min(2, stops.Count - 1)];
            Color col2 = stops[Math.Min(3, stops.Count - 1)];

            float radBase = Math.Min(w, h) * 0.45f;

            double off0 = phase;
            float cx0 = w * (0.5f + 0.36f * (float)Math.Sin(off0));
            float cy0 = h * (0.5f + 0.30f * (float)Math.Cos(off0 * 0.8));
            DrawBlob(g, cx0, cy0, radBase * (0.45f + 0.08f * (float)Math.Sin(off0 * 1.3)), col0, 150);

            double off1 = phase + Math.PI * 2.0 / 3.0;
            float cx1 = w * (0.5f + 0.36f * (float)Math.Sin(off1));
            float cy1 = h * (0.5f + 0.30f * (float)Math.Cos(off1 * 0.8 + 1));
            DrawBlob(g, cx1, cy1, radBase * (0.45f + 0.08f * (float)Math.Sin(off1 * 1.3)), col1, 140);

            double off2 = phase + Math.PI * 4.0 / 3.0;
            float cx2 = w * (0.5f + 0.36f * (float)Math.Sin(off2));
            float cy2 = h * (0.5f + 0.30f * (float)Math.Cos(off2 * 0.8 + 2));
            DrawBlob(g, cx2, cy2, radBase * (0.45f + 0.08f * (float)Math.Sin(off2 * 1.3)), col2, 130);

            // 磨砂罩：压暗保证提示文字清晰，同时保留极光流动感
            using (SolidBrush frost = new SolidBrush(Color.FromArgb(150, _theme.Bg.R, _theme.Bg.G, _theme.Bg.B)))
            {
                g.FillRectangle(frost, 0, 0, w, h);
            }
        }

        private void DrawBlob(Graphics g, float cx, float cy, float rad, Color col, int alpha)
        {
            if (rad < 1f)
            {
                return;
            }
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(cx - rad, cy - rad, rad * 2, rad * 2);
                using (PathGradientBrush brush = new PathGradientBrush(path))
                {
                    brush.CenterColor = Color.FromArgb(alpha, col.R, col.G, col.B);
                    brush.SurroundColors = new[] { Color.FromArgb(0, col.R, col.G, col.B) };
                    g.FillPath(brush, path);
                }
            }
        }

        private void DrawHint(Graphics g, int w, int h)
        {
            // 以继承字体尺寸为基准做 DPI 相对缩放（避免依赖主窗 GetFont 实例方法）
            float scale = this.Font.Size / 11f;
            if (scale <= 0) scale = 1f;

            // 播放圆环图标
            int iconR = (int)(Math.Max(18, Math.Min(w, h) / 7) * scale);
            int iconCx = w / 2;
            int iconCy = (int)(h / 2 - 18 * scale);
            using (GraphicsPath circ = new GraphicsPath())
            {
                circ.AddEllipse(iconCx - iconR, iconCy - iconR, iconR * 2, iconR * 2);
                using (SolidBrush fill = new SolidBrush(Color.FromArgb(40, _theme.TextPrimary.R, _theme.TextPrimary.G, _theme.TextPrimary.B)))
                {
                    g.FillPath(fill, circ);
                }
                using (Pen pen = new Pen(_theme.TextPrimary, 1.6f * scale))
                {
                    g.DrawPath(pen, circ);
                }
            }
            // 三角
            PointF[] tri = new PointF[]
            {
                new PointF(iconCx - iconR * 0.32f, iconCy - iconR * 0.5f),
                new PointF(iconCx - iconR * 0.32f, iconCy + iconR * 0.5f),
                new PointF(iconCx + iconR * 0.5f, iconCy)
            };
            using (SolidBrush triBrush = new SolidBrush(_theme.TextPrimary))
            {
                g.FillPolygon(triBrush, tri);
            }

            string line1 = "未播放";
            string line2 = "选择频道开始预览";
            using (Font f1 = new Font(this.Font.FontFamily, this.Font.Size * (13f / 11f), FontStyle.Bold))
            using (Font f2 = new Font(this.Font.FontFamily, this.Font.Size * (10f / 11f)))
            {
                TextRenderer.DrawText(g, line1, f1,
                    new Rectangle(0, (int)(h / 2 - 2 * scale), w, (int)(24 * scale)),
                    _theme.TextPrimary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, line2, f2,
                    new Rectangle(0, (int)(h / 2 + 22 * scale), w, (int)(20 * scale)),
                    _theme.TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animTimer?.Stop();
                _animTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
