using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace FakeHover
{
    public partial class Form1 : Form
    {
        const string TARGET_PROCESS = "QQMusic.exe";
        static Random random = new Random();
        static NotifyIcon trayIcon;
        static bool running = true;
        static bool simulate = true;
        Thread workerThread;

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        const uint WM_MOUSEMOVE = 0x0200;

        ContextMenuStrip contextMenu;
        ToolStripMenuItem toggleSimulateItem;

        public Form1()
        {
            InitializeComponent();
            InitializeTray();
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            workerThread = new Thread(new ThreadStart(Worker));
            workerThread.IsBackground = true;
            workerThread.Start();
        }

        void InitializeTray()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Text = "鼠标悬停模拟器";
            trayIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            trayIcon.Visible = true;

            contextMenu = new ContextMenuStrip();
            toggleSimulateItem = new ToolStripMenuItem("暂停模拟");
            toggleSimulateItem.Click += (s, e) => {
                simulate = !simulate;
                toggleSimulateItem.Text = simulate ? "暂停模拟" : "恢复模拟";
            };
            contextMenu.Items.Add(toggleSimulateItem);

            ToolStripMenuItem exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (s, e) => {
                running = false;
                trayIcon.Visible = false;
                Application.Exit();
            };
            contextMenu.Items.Add(exitItem);

            trayIcon.ContextMenuStrip = contextMenu;

            trayIcon.DoubleClick += (s, e) => {
                this.Invoke((Action)(() => {
                    this.WindowState = FormWindowState.Normal;
                    this.ShowInTaskbar = true;
                    this.Show();
                    textBox1.Clear();
                }));
            };
        }

        void Worker()
        {
            while (running)
            {
                if (!simulate)
                {
                    Thread.Sleep(3000);
                    continue;
                }

                bool foundWindow = false;

                EnumWindows(new EnumWindowsProc((hWnd, lParam) =>
                {
                    if (!IsWindowVisible(hWnd)) return true;

                    GetWindowThreadProcessId(hWnd, out uint processId);
                    Process proc = null;
                    try
                    {
                        proc = Process.GetProcessById((int)processId);
                    }
                    catch
                    {
                        return true;
                    }

                    if (proc.ProcessName + ".exe" == TARGET_PROCESS)
                    {
                        Log($"找到窗口：{hWnd}");
                        SimulateMouseHover(hWnd);
                        foundWindow = true;
                    }

                    return true;
                }), IntPtr.Zero);

                if (!foundWindow)
                {
                    Log("没有找到 QQMusic.exe 的窗口。");
                }

                Thread.Sleep(1000);
            }
        }

        void SimulateMouseHover(IntPtr hWnd)
        {
            if (GetWindowRect(hWnd, out RECT rect))
            {
                Log($"窗口位置: Left={rect.Left}, Top={rect.Top}, Right={rect.Right}, Bottom={rect.Bottom}");

                if (GetCursorPos(out POINT cursorPos))
                {
                    if (cursorPos.X >= rect.Left && cursorPos.X <= rect.Right && cursorPos.Y >= rect.Top && cursorPos.Y <= rect.Bottom)
                    {
                        Log("检测到真实鼠标在窗口上，暂停模拟。");
                        return;
                    }
                }

                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                int centerX = width / 2;
                int centerY = height / 2;

                int offsetX = random.Next(-2, 3);
                int offsetY = random.Next(-2, 3);

                int x = centerX + offsetX;
                int y = centerY + offsetY;

                Log($"模拟鼠标位置: x={x}, y={y}");

                int lParam = (y << 16) | (x & 0xFFFF);

                bool result = PostMessage(hWnd, WM_MOUSEMOVE, IntPtr.Zero, new IntPtr(lParam));
                if (result)
                {
                    Log("成功发送带抖动的 WM_MOUSEMOVE 消息。");
                }
                else
                {
                    Log("发送带抖动的 WM_MOUSEMOVE 消息失败。");
                }
            }
            else
            {
                Log("无法获取窗口矩形信息。");
            }
        }

        void Log(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((Action)(() => Log(message)));
            }
            else
            {
                if (this.WindowState != FormWindowState.Minimized && this.Visible)
                {
                    textBox1.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\r\n");
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 当用户点击关闭按钮时，不退出程序，只最小化到托盘
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true; // 取消关闭
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                this.Hide();
            }
            else
            {
                base.OnFormClosing(e);
            }
        }
    }
}
