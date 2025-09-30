using System.Runtime.InteropServices;
using System.Threading;

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

        private enum MouseEvent
        {
            F_LEFTDOWN = 0x0002,
            F_LEFTUP = 0x0004,
            F_RIGHTDOWN = 0x0008,
            F_RIGHTUP = 0x0010
        }

        private const string LEFT_BUTTON = "Left";
        
        private const string SINGLE_CLICK = "Single";
        private const string DOUBLE_CLICK = "Double";

        private int _finalTime = 0;

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

        private (int hours, int minutes, int seconds, int milliseconds) ParseTimeInputs()
        {
            int hours = int.TryParse(textBox1.Text, out var h) ? h : 0;
            int minutes = int.TryParse(textBox2.Text, out var m) ? m : 0;
            int seconds = int.TryParse(textBox3.Text, out var s) ? s : 0;
            int milliseconds = int.TryParse(textBox4.Text, out var ms) ? ms : 0;

            return (hours, minutes, seconds, milliseconds);
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
            
            var (timeHour, timeMinute, timeSecond, timeMillisec) = ParseTimeInputs();
            _finalTime = (timeHour * 3600 + timeMinute * 60 + timeSecond) * 1000 + timeMillisec;

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
            if (radioButton1.Checked)
            {
                while (!Token.IsCancellationRequested)
                {
                    ClickMouse();
                    await Task.Delay(_finalTime, Token).ConfigureAwait(false);
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
                    await Task.Delay(_finalTime, Token).ConfigureAwait(false);
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
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_MOUSE;
            inputs[0].mi.dx = radioButton3.Checked ? Control.MousePosition.X : int.Parse(textBox6.Text);
            inputs[0].mi.dy = radioButton3.Checked ? Control.MousePosition.Y : int.Parse(textBox7.Text);
            inputs[0].mi.mouseData = 0;

            uint buttonFlag = (uint)(mouseButton.Equals(LEFT_BUTTON)
                    ? (clickType.Equals(DOUBLE_CLICK) ? MouseEvent.F_LEFTDOWN | MouseEvent.F_LEFTUP : MouseEvent.F_LEFTDOWN | MouseEvent.F_LEFTUP)
                    : (clickType.Equals(DOUBLE_CLICK) ? MouseEvent.F_RIGHTDOWN | MouseEvent.F_RIGHTUP : MouseEvent.F_RIGHTDOWN | MouseEvent.F_RIGHTUP));

            // Send first click
            inputs[0].mi.dwFlags = buttonFlag;
            _ = SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));

            if (clickType.Equals(DOUBLE_CLICK))
            {
                _ = SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
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

            toolStripStatusLabel1.Text = status == Status.Active ? "Active" : "Inactive";
            toolStripStatusLabel1.ForeColor = status == Status.Active ? Color.Green : Color.Red;
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

            else if (e.KeyCode == Keys.End)// Panic Hotkey
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
