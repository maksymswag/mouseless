using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MouseGridApp.Properties; // THIS IS THE ONLY CHANGE IN THIS FILE

namespace MouseGridApp
{
    public static class MouseGrid
    {
        // --- Configuration ---
        private const int ANIMATION_DURATION_MS = 150;
        private const int NUDGE_PIXELS = 25;
        private const int MOVES_UNTIL_NUDGE_MODE = 5;

        // --- State ---
        private static bool s_isGridActive = false;
        private static Rectangle s_currentBounds;
        private static int s_volume; // Volume percentage (0-100)
        private static OverlayForm? s_overlayForm;
        private static NotifyIcon? s_trayIcon;
        private static System.Threading.Timer? s_animationTimer;
        private static Stopwatch s_anim_stopwatch = new Stopwatch();

        // --- New State ---
        private static int s_moveCount = 0;
        private static bool s_isNudgeMode = false;
        private static System.Windows.Forms.Timer s_inactivityTimer = new System.Windows.Forms.Timer();

        // --- Hooking ---
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static IntPtr s_keyboardHookID = IntPtr.Zero;
        private static NativeMethods.LowLevelKeyboardProc s_keyboardProc = KeyboardHookCallback;

        // --- Public Accessors & Shared Logic ---
        public static Rectangle GetCurrentBounds() => s_currentBounds;
        public class LayoutRegions { public Rectangle Up, Down, Left, Right; }
        public enum GridRegion { Up, Down, Left, Right }

        public static LayoutRegions GetLayoutRegions(Rectangle bounds)
        {
            if (bounds.Width < 2 || bounds.Height < 3) return new LayoutRegions();

            int thirdH = bounds.Height / 3;
            int midY = bounds.Y + thirdH;
            int bottomY = bounds.Y + (2 * thirdH);
            int halfW = bounds.Width / 2;

            return new LayoutRegions
            {
                Up = new Rectangle(bounds.X, bounds.Y, bounds.Width, thirdH),
                Down = new Rectangle(bounds.X, bottomY, bounds.Width, thirdH),
                Left = new Rectangle(bounds.X, midY, halfW, thirdH),
                Right = new Rectangle(bounds.X + halfW, midY, halfW, thirdH)
            };
        }

        [STAThread]
        public static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Initialize();
            Application.Run();
        }

        public static void Initialize()
        {
            s_volume = Settings.Default.Volume;

            try
            {
                NativeMethods.mciSendString("open \"cursor.wav\" alias cursor_sound", null, 0, IntPtr.Zero);
                SetVolume(s_volume);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not initialize sound. Make sure 'cursor.wav' is present. Error: {ex.Message}");
            }

            s_keyboardHookID = SetKeyboardHook(s_keyboardProc);

            var contextMenu = new ContextMenuStrip();
            
            contextMenu.Items.Add(new ToolStripLabel("Volume"));
            var volumeTrackBar = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 10,
                Value = s_volume,
                AutoSize = false,
                Width = 120,
                Height = 20
            };
            volumeTrackBar.ValueChanged += OnVolumeChanged;
            contextMenu.Items.Add(new ToolStripControlHost(volumeTrackBar));
            contextMenu.Items.Add(new ToolStripSeparator());

            contextMenu.Items.Add("Exit", null, (s, a) => Application.Exit());

            s_trayIcon = new NotifyIcon { Icon = new Icon("app.ico"), Text = "Mouse Grid", Visible = true, ContextMenuStrip = contextMenu };

            Application.ApplicationExit += OnApplicationExit;

            s_inactivityTimer.Interval = 2000;
            s_inactivityTimer.Tick += (s, e) => EndGridSelection();
        }

        private static void OnVolumeChanged(object? sender, EventArgs e)
        {
            if (sender is TrackBar trackBar)
            {
                SetVolume(trackBar.Value);
                Settings.Default.Volume = trackBar.Value;
                Settings.Default.Save();
            }
        }

        private static void SetVolume(int volume)
        {
            s_volume = Math.Clamp(volume, 0, 100);
            int mciVolume = s_volume * 10;
            string command = $"setaudio cursor_sound volume to {mciVolume}";
            NativeMethods.mciSendString(command, null, 0, IntPtr.Zero);
        }
        
        private static void PlaySound()
        {
            if (s_volume > 0)
            {
                NativeMethods.mciSendString("play cursor_sound from 0", null, 0, IntPtr.Zero);
            }
        }

        private static void OnApplicationExit(object? sender, EventArgs e)
        {
            EndGridSelection();
            NativeMethods.mciSendString("close cursor_sound", null, 0, IntPtr.Zero);
            NativeMethods.UnhookWindowsHookEx(s_keyboardHookID);
            s_trayIcon?.Dispose();
        }

        private static IntPtr SetKeyboardHook(NativeMethods.LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            {
                ProcessModule? curModule = curProcess.MainModule;
                string moduleName = curModule?.ModuleName ?? throw new InvalidOperationException("Module name is null.");
                return NativeMethods.SetWindowsHookEx(WH_KEYBOARD_LL, proc, NativeMethods.GetModuleHandle(moduleName), 0);
            }
        }

        private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                Keys key = (Keys)Marshal.ReadInt32(lParam);
                if (key == Keys.CapsLock)
                {
                    if (s_isGridActive) EndGridSelection();
                    else StartGridSelection();
                    return (IntPtr)1;
                }

                if (s_isGridActive)
                {
                    s_inactivityTimer.Stop();
                    s_inactivityTimer.Start();

                    bool keyHandled = true;
                    if (s_isNudgeMode)
                    {
                        switch (key)
                        {
                            case Keys.W: NudgeMouse(0, -NUDGE_PIXELS); break;
                            case Keys.S: NudgeMouse(0, NUDGE_PIXELS); break;
                            case Keys.A: NudgeMouse(-NUDGE_PIXELS, 0); break;
                            case Keys.D: NudgeMouse(NUDGE_PIXELS, 0); break;
                            default: keyHandled = false; break;
                        }
                    }
                    else
                    {
                        switch (key)
                        {
                            case Keys.W: SelectAndMove(GridRegion.Up); break;
                            case Keys.S: SelectAndMove(GridRegion.Down); break;
                            case Keys.A: SelectAndMove(GridRegion.Left); break;
                            case Keys.D: SelectAndMove(GridRegion.Right); break;
                            default: keyHandled = false; break;
                        }
                    }

                    if (!keyHandled)
                    {
                        switch (key)
                        {
                            case Keys.LControlKey: case Keys.RControlKey: case Keys.Control:
                            case Keys.Escape:
                                EndGridSelection();
                                keyHandled = true;
                                break;
                        }
                    }

                    if (keyHandled) return (IntPtr)1;
                }
            }
            return NativeMethods.CallNextHookEx(s_keyboardHookID, nCode, wParam, lParam);
        }

        private static void StartGridSelection()
        {
            if (s_isGridActive) return;

            s_isGridActive = true;
            s_moveCount = 0;
            s_isNudgeMode = false;

            s_currentBounds = Screen.PrimaryScreen.Bounds;
            s_overlayForm = new OverlayForm();
            s_overlayForm.Show();
            s_inactivityTimer.Start();
        }

        private static void EndGridSelection()
        {
            if (!s_isGridActive) return;

            s_isGridActive = false;
            s_inactivityTimer.Stop();
            s_animationTimer?.Dispose();
            PlaySound();
            s_overlayForm?.Close();
            s_overlayForm = null;
            s_currentBounds = Rectangle.Empty;
        }

        private static void NudgeMouse(int dx, int dy)
        {
            Point pos = Cursor.Position;
            NativeMethods.SetCursorPos(pos.X + dx, pos.Y + dy);
        }

        private static void SelectAndMove(GridRegion region)
        {
            s_moveCount++;
            if (s_moveCount >= MOVES_UNTIL_NUDGE_MODE)
            {
                s_isNudgeMode = true;
                s_overlayForm?.Close();
                s_overlayForm = null;
            }

            LayoutRegions nextRegions = GetLayoutRegions(s_currentBounds);
            switch (region)
            {
                case GridRegion.Up: s_currentBounds = nextRegions.Up; break;
                case GridRegion.Down: s_currentBounds = nextRegions.Down; break;
                case GridRegion.Left: s_currentBounds = nextRegions.Left; break;
                case GridRegion.Right: s_currentBounds = nextRegions.Right; break;
            }

            s_overlayForm?.Refresh();

            Point targetPoint = new Point(
                s_currentBounds.X + s_currentBounds.Width / 2,
                s_currentBounds.Y + s_currentBounds.Height / 2
            );
            AnimateMouseMoveSetup(targetPoint);
        }

        private static void AnimateMouseMoveSetup(Point endPoint)
        {
            s_animationTimer?.Dispose();
            s_anim_stopwatch.Restart();
            s_animationTimer = new System.Threading.Timer(
                (state) => AnimateTick(Cursor.Position, endPoint), null, 0, 10);
        }

        private static void AnimateTick(Point startPoint, Point endPoint)
        {
            long elapsed = s_anim_stopwatch.ElapsedMilliseconds;
            double progress = (double)elapsed / ANIMATION_DURATION_MS;
            if (progress >= 1.0)
            {
                NativeMethods.SetCursorPos(endPoint.X, endPoint.Y);
                s_animationTimer?.Dispose();
                s_animationTimer = null;
                return;
            }
            double easedProgress = 1 - Math.Pow(1 - progress, 3);
            int newX = (int)Math.Round(startPoint.X + (endPoint.X - startPoint.X) * easedProgress);
            int newY = (int)Math.Round(startPoint.Y + (endPoint.Y - startPoint.Y) * easedProgress);
            NativeMethods.SetCursorPos(newX, newY);
        }
    }
}