using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MouseGridApp
{
    public class OverlayForm : Form
    {
        // --- Configuration ---
        private const int PREDICTION_DEPTH = 5;
        private const float LINE_WIDTH_NORMAL_PX = 2f;
        private const float LINE_WIDTH_THICK_PX = 4f;
        private static readonly Color TRAIL_COLOR = Color.Red;

        // --- Pen Caches for Normal and Faded states ---
        private readonly Pen[] _cachedPensNormal;
        private readonly Pen[] _cachedPensFaded;
        
        // --- Timers and State for Fading ---
        private readonly Timer _updateTimer;
        private readonly Timer _fadeTimer;
        private bool _isFaded = false;
        private Point _lastMousePosition = Point.Empty;

        public OverlayForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            
            _cachedPensNormal = new Pen[PREDICTION_DEPTH];
            _cachedPensFaded = new Pen[PREDICTION_DEPTH];
            int fadedAlpha = (int)(255 * 0.10);

            for (int i = 0; i < PREDICTION_DEPTH; i++)
            {
                float width = (i == 0) ? LINE_WIDTH_THICK_PX : LINE_WIDTH_NORMAL_PX;
                int normalAlpha = 255 / (int)Math.Pow(2, i);
                
                _cachedPensNormal[i] = new Pen(Color.FromArgb(normalAlpha, TRAIL_COLOR), width);
                _cachedPensFaded[i] = new Pen(Color.FromArgb(fadedAlpha, TRAIL_COLOR), width);
            }

            _updateTimer = new Timer { Interval = 8 };
            _updateTimer.Tick += (s, e) => UpdateVisuals();
            
            _fadeTimer = new Timer { Interval = 100 };
            _fadeTimer.Tick += (s, e) => {
                _isFaded = true;
                _fadeTimer.Stop();
            };
            
            _updateTimer.Start();
        }
        
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= NativeMethods.WS_EX_LAYERED;
                return cp;
            }
        }

        private void UpdateVisuals()
        {
            Point currentMousePosition = Cursor.Position;
            if (currentMousePosition == _lastMousePosition) return;
            
            _lastMousePosition = currentMousePosition;
            _isFaded = false;
            _fadeTimer.Stop();
            _fadeTimer.Start();

            using (var bitmap = new Bitmap(this.Width, this.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Point initialStartPoint = this.PointToClient(_lastMousePosition);
                Rectangle currentBounds = MouseGrid.GetCurrentBounds();
                if (!currentBounds.IsEmpty)
                {
                    DrawPredictionLines(g, initialStartPoint, currentBounds, 0);
                }
                
                IntPtr screenDc = NativeMethods.GetDC(IntPtr.Zero);
                IntPtr memDc = NativeMethods.CreateCompatibleDC(screenDc);
                IntPtr hBitmap = IntPtr.Zero;
                IntPtr oldBitmap = IntPtr.Zero;

                try
                {
                    hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
                    oldBitmap = NativeMethods.SelectObject(memDc, hBitmap);
                    var size = new NativeMethods.Size { cx = this.Width, cy = this.Height };
                    var pointSource = new NativeMethods.Point { x = 0, y = 0 };
                    var topPos = new NativeMethods.Point { x = this.Left, y = this.Top };
                    var blend = new NativeMethods.BLENDFUNCTION
                    {
                        BlendOp = 0,
                        BlendFlags = 0,
                        SourceConstantAlpha = 255,
                        AlphaFormat = 1
                    };
                    NativeMethods.UpdateLayeredWindow(this.Handle, screenDc, ref topPos, ref size, memDc, ref pointSource, 0, ref blend, NativeMethods.ULW_ALPHA);
                }
                finally
                {
                    NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
                    if (hBitmap != IntPtr.Zero)
                    {
                        NativeMethods.SelectObject(memDc, oldBitmap);
                        NativeMethods.DeleteObject(hBitmap);
                    }
                    NativeMethods.DeleteDC(memDc);
                }
            }
        }
        
        private void DrawPredictionLines(Graphics g, Point startPoint, Rectangle bounds, int depth)
        {
            if (depth >= PREDICTION_DEPTH) return;
            
            Pen[] activePens = _isFaded ? _cachedPensFaded : _cachedPensNormal;
            var regions = MouseGrid.GetLayoutRegions(bounds);
            var regionRects = new[] { regions.Up, regions.Down, regions.Left, regions.Right };

            foreach (var regionRect in regionRects)
            {
                if (regionRect.IsEmpty) continue;
                Point endPoint = new Point(regionRect.X + regionRect.Width / 2, regionRect.Y + regionRect.Height / 2);
                g.DrawLine(activePens[depth], startPoint, endPoint);
                DrawPredictionLines(g, endPoint, regionRect, depth + 1);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _updateTimer.Dispose();
                _fadeTimer.Dispose();
                foreach (var pen in _cachedPensNormal) { pen.Dispose(); }
                foreach (var pen in _cachedPensFaded) { pen.Dispose(); }
            }
            base.Dispose(disposing);
        }
    }
}