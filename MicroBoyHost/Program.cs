// MicroBoyHost - WinForms Host ohne Designer
// Start: MicroBoyHost.exe [Pfad\zu\DeinerCartridge.dll]

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MicroBoy;

namespace MicroBoyHost
{
    internal static class HostSpec
    {
        public const int Scale = 4;
        public const int TargetFps = 60;

        public static readonly Color[] Palette =
        {
            Color.FromArgb(0xFF, 15,  56,  15),  // 0: tiefer Waldgrün-Ton
            Color.FromArgb(0xFF, 48,  98,  48),  // 1: mittleres Grün
            Color.FromArgb(0xFF, 139, 172, 15), // 2: helles Grün
            Color.FromArgb(0xFF, 155, 188, 15), // 3: gelbliches Highlight
            Color.FromArgb(0xFF, 217, 212, 180),// 4: heller Sand-/Pflasterton
            Color.FromArgb(0xFF, 154, 107, 63), // 5: erdiger Braunton
            Color.FromArgb(0xFF, 29,  87,  166),// 6: tiefes Wasserblau
            Color.FromArgb(0xFF, 111, 181, 255),// 7: helles Wasserblau
            Color.FromArgb(0xFF, 162, 160, 144),// 8: neutrales Stein-/Wandgrau
            Color.FromArgb(0xFF, 184, 48,  80), // 9: Akzentfarbe (z.B. Teppich)
        };
    }

    internal sealed class HostForm : Form
    {
        private readonly ICartridge cart;
        private readonly byte[] frame = new byte[MicroBoySpec.W * MicroBoySpec.H]; // Indizes in HostSpec.Palette
        private readonly Bitmap bmp = new Bitmap(MicroBoySpec.W, MicroBoySpec.H, PixelFormat.Format32bppArgb);
        private Buttons buttons = Buttons.None;
        private readonly Stopwatch sw = Stopwatch.StartNew();
        private long lastTicks;
        private readonly double targetDelta = 1.0 / HostSpec.TargetFps;

        public HostForm(ICartridge cart)
        {
            this.cart = cart;

            // Form-Basics (keine Designer-Datei)
            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true;
            ClientSize = new Size(MicroBoySpec.W * HostSpec.Scale, MicroBoySpec.H * HostSpec.Scale);
            Text = $"MicroBoy - {cart.Title} by {cart.Author}";

            KeyPreview = true;
            KeyDown += (_, e) => UpdateButtons(e.KeyCode, true);
            KeyUp += (_, e) => UpdateButtons(e.KeyCode, false);

            Application.Idle += GameLoop; // sauberer Game-Loop
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            while (IsAppIdle())
            {
                var now = sw.ElapsedTicks;
                double dt = (now - lastTicks) / (double)Stopwatch.Frequency;
                if (dt < targetDelta)
                {
                    int ms = (int)Math.Max(0, (targetDelta - dt) * 1000.0 - 0.2);
                    if (ms > 0) System.Threading.Thread.Sleep(ms);
                    else System.Threading.Thread.Yield();
                    continue;
                }
                lastTicks = now;

                cart.Update(new Input(buttons), dt);
                cart.Render(frame);
                BlitFrameToBitmap();

                Invalidate();
                Update();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            e.Graphics.DrawImage(
                bmp,
                new Rectangle(0, 0, MicroBoySpec.W * HostSpec.Scale, MicroBoySpec.H * HostSpec.Scale),
                new Rectangle(0, 0, MicroBoySpec.W, MicroBoySpec.H),
                GraphicsUnit.Pixel);
        }

        private void BlitFrameToBitmap()
        {
            var data = bmp.LockBits(
                new Rectangle(0, 0, MicroBoySpec.W, MicroBoySpec.H),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* basePtr = (byte*)data.Scan0;
                int stride = data.Stride;

                for (int y = 0; y < MicroBoySpec.H; y++)
                {
                    byte* dst = basePtr + y * stride;
                    int rowOff = y * MicroBoySpec.W;
                    for (int x = 0; x < MicroBoySpec.W; x++)
                    {
                        int idx = frame[rowOff + x];
                        if ((uint)idx >= (uint)HostSpec.Palette.Length)
                        {
                            idx = 0;
                        }
                        var c = HostSpec.Palette[idx];
                        dst[x * 4 + 0] = c.B;
                        dst[x * 4 + 1] = c.G;
                        dst[x * 4 + 2] = c.R;
                        dst[x * 4 + 3] = 255;
                    }
                }
            }

            bmp.UnlockBits(data);
        }

        private void UpdateButtons(Keys key, bool down)
        {
            Buttons bit = key switch
            {
                Keys.Z => Buttons.A,
                Keys.X => Buttons.B,
                Keys.Enter => Buttons.Start,
                Keys.RShiftKey => Buttons.Select,
                Keys.Up => Buttons.Up,
                Keys.Down => Buttons.Down,
                Keys.Left => Buttons.Left,
                Keys.Right => Buttons.Right,
                _ => Buttons.None
            };
            if (bit == Buttons.None) return;

            if (down) buttons |= bit;
            else buttons &= ~bit;
        }

        private static bool IsAppIdle()
        {
            return !PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
        }

        // Win32 PeekMessage für Idle-Loop
        [DllImport("user32.dll")]
        private static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public System.Drawing.Point pt;
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            var cart = (args.Length > 0 ? LoadCartridge(args[0]) : null) ?? new BuiltInDemo();
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new HostForm(cart));
        }

        private static ICartridge? LoadCartridge(string path)
        {
            try
            {
                // Wichtig: LoadFrom, damit die Cartridge die gemeinsame Abstractions-Assembly nutzt.
                var asm = Assembly.LoadFrom(System.IO.Path.GetFullPath(path));

                var cartType = Array.Find(asm.GetTypes(), t =>
                    typeof(ICartridge).IsAssignableFrom(t) &&
                    !t.IsAbstract &&
                    t.GetConstructor(Type.EmptyTypes) != null);

                if (cartType == null)
                {
                    MessageBox.Show("Keine ICartridge-Implementierung gefunden.", "MicroBoy");
                    return null;
                }

                var cart = (ICartridge?)Activator.CreateInstance(cartType);
                cart!.Init();
                return cart;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cartridge laden fehlgeschlagen:\n" + ex, "MicroBoy");
                return null;
            }
        }
    }

    // Kleine eingebaute Demo, wenn keine DLL übergeben wurde
    internal sealed class BuiltInDemo : ICartridge
    {
        public string Title => "Bouncer Demo";
        public string Author => "You";

        private int px = 10, py = 10, vx = 1, vy = 1;
        private byte color = 2;

        public void Init() { px = 10; py = 10; vx = 1; vy = 1; color = 2; }

        public void Update(Input input, double dt)
        {
            int speed = 60;
            px += (int)(vx * speed * dt);
            py += (int)(vy * speed * dt);

            if (px < 0) { px = 0; vx = +1; }
            if (py < 0) { py = 0; vy = +1; }
            if (px > MicroBoySpec.W - 8) { px = MicroBoySpec.W - 8; vx = -1; }
            if (py > MicroBoySpec.H - 8) { py = MicroBoySpec.H - 8; vy = -1; }

            if (input.IsDown(Buttons.A)) color = 3;
            if (input.IsDown(Buttons.B)) color = 1;

            if (input.IsDown(Buttons.Left)) px = Math.Max(0, px - 1);
            if (input.IsDown(Buttons.Right)) px = Math.Min(MicroBoySpec.W - 8, px + 1);
            if (input.IsDown(Buttons.Up)) py = Math.Max(0, py - 1);
            if (input.IsDown(Buttons.Down)) py = Math.Min(MicroBoySpec.H - 8, py + 1);
        }

        public void Render(Span<byte> frame)
        {
            frame.Fill(0);
            for (int y = 0; y < 8; y++)
            {
                int ry = py + y;
                if ((uint)ry >= MicroBoySpec.H) continue;
                int rowOff = ry * MicroBoySpec.W;
                for (int x = 0; x < 8; x++)
                {
                    int rx = px + x;
                    if ((uint)rx >= MicroBoySpec.W) continue;
                    frame[rowOff + rx] = color;
                }
            }
            for (int x = 0; x < MicroBoySpec.W; x++) frame[x] = 1; // HUD-Zeile
        }
    }
}
