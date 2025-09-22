using System.Runtime.InteropServices;

namespace Auto_Clicker
{
    public partial class Form1 : Form
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern uint SendInput(uint cInputs, INPUT[] pInputs, int cbSize);

        private const uint INPUT_MOUSE = 0;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;

        private const string LEFT_BUTTON = "Left";
        
        private const string SINGLE_CLICK = "Single";
        private const string DOUBLE_CLICK = "Double";

        private enum Status
        {
            Active = 0,
            Inactive = 1
        }

        private Keys Hotkey = Keys.F8;// Default hotkey
        private readonly KeyboardHook _keyboardHook;
        private string mouseButton = LEFT_BUTTON;
        private string clickType = SINGLE_CLICK;
        private CancellationTokenSource? _cancellationTokenSource;

        public Form1()
        {
            InitializeComponent();
            _keyboardHook = new KeyboardHook();
            _keyboardHook.KeyDown += Form1_KeyDown;

            button1.Text = $"Start ({Hotkey})";
            button2.Text = $"Stop ({Hotkey})";

            SetStatus(Status.Inactive);
        }
        private void StartClicking()
        {
            if (!ValidateInputs())
            {
                return;
            }

            mouseButton = comboBox1.GetItemText(comboBox1.SelectedItem) ?? LEFT_BUTTON;
            clickType = comboBox2.GetItemText(comboBox2.SelectedItem) ?? SINGLE_CLICK;

            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => Clicker(_cancellationTokenSource.Token));
            SetStatus(Status.Active);
        }

        private void StopClicking()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            SetStatus(Status.Inactive);
        }

        private async Task Clicker(CancellationToken Token)
        {
            int timeHour = int.Parse(textBox1.Text);
            int timeMinute = int.Parse(textBox2.Text);
            int timeSecond = int.Parse(textBox3.Text);
            int timeMillisec = int.Parse(textBox4.Text);

            int finalTime = (timeHour * 3600 + timeMinute * 60 + timeSecond) * 1000 + timeMillisec;

            if (radioButton1.Checked)
            {
                while (!Token.IsCancellationRequested)
                {
                    ClickMouse();
                    await Task.Delay(finalTime, Token).ConfigureAwait(false);
                }
            }
            else if (radioButton2.Checked)
            {
                for (int i = 0; i < int.Parse(textBox5.Text); i++)
                {

                    if (Token.IsCancellationRequested)
                    {
                        break;
                    }

                    ClickMouse();
                    await Task.Delay(finalTime, Token).ConfigureAwait(false);
                }
            }
        }

        private bool ValidateInputs()
        {
            if (!int.TryParse(textBox1.Text, out _) ||
                !int.TryParse(textBox2.Text, out _) ||
                !int.TryParse(textBox3.Text, out _) ||
                !int.TryParse(textBox4.Text, out _) ||
                (radioButton2.Checked && !int.TryParse(textBox5.Text, out _)))
            {
                MessageBox.Show("Please enter valid numbers.");
                return false;
            }
            return true;
        }

        private void ClickMouse()
        {
            uint x = radioButton3.Checked ? (uint)Control.MousePosition.X : uint.Parse(textBox6.Text);
            uint y = radioButton3.Checked ? (uint)Control.MousePosition.Y : uint.Parse(textBox7.Text);

            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_MOUSE;
            inputs[0].mi.dx = (int)x;
            inputs[0].mi.dy = (int)y;
            inputs[0].mi.mouseData = 0;

            uint buttonFlag = mouseButton.Equals(LEFT_BUTTON) ?
                              (clickType.Equals(DOUBLE_CLICK) ? MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP : MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP) :
                              (clickType.Equals(DOUBLE_CLICK) ? MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP : MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP);

            if (clickType.Equals(SINGLE_CLICK) || clickType.Equals(DOUBLE_CLICK))
            {
                // First click
                inputs[0].mi.dwFlags = buttonFlag;
                _ = SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));

                // Second click for double click
                if (clickType.Equals(DOUBLE_CLICK))
                {
                    _ = SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
                }
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            StartClicking();
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            StopClicking();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _keyboardHook.KeyDown -= Form1_KeyDown;
            _keyboardHook.Unhook();
            StopClicking();
        }

        private void SetStatus(Status status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetStatus(status)));
                return;
            }

            labelStatus.Text = status == Status.Active ? "Active" : "Inactive";
            labelStatus.ForeColor = status == Status.Active ? Color.Green : Color.Red;
        }

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Hotkey)
            {
                if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
                {
                    StopClicking();
                }
                else
                {
                    StartClicking();
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                StopClicking();
                _keyboardHook.KeyDown -= Form1_KeyDown;
                _keyboardHook.Unhook();
                
                Application.Exit();
            }
        }

        private void TextBoxHotKey_KeyDown(object sender, KeyEventArgs e)
        {
            Hotkey = e.KeyCode;
            textBoxHotKey.Text = Hotkey.ToString();

            button1.Text = $"Start ({Hotkey})";
            button2.Text = $"Stop ({Hotkey})";
        }
    }
}
