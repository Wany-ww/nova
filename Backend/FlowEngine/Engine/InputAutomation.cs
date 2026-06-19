using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace FlowEngine.Engine
{
    /// <summary>
    /// Provides virtual mouse movement, mouse clicks, key presses, and unicode text typing simulation using native Windows APIs.
    /// </summary>
    public static class InputAutomation
    {
        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        private const int MOUSEEVENTF_MIDDLEDOWN = 0x20;
        private const int MOUSEEVENTF_MIDDLEUP = 0x40;

        private const int KEYEVENTF_KEYUP = 0x0002;

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MouseInput mi;
            [FieldOffset(0)]
            public KeyboardInput ki;
            [FieldOffset(0)]
            public HardwareInput hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MouseInput
        {
            public int dx;
            public int dy;
            public int mouseData;
            public int dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KeyboardInput
        {
            public short wVk;
            public short wScan;
            public int dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HardwareInput
        {
            public int uMsg;
            public short wParamL;
            public short wParamH;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public InputUnion u;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SendInput(int nInputs, INPUT[] pInputs, int cbSize);

        private const int INPUT_KEYBOARD = 1;
        private const int KEYEVENTF_UNICODE = 0x0004;

        /// <summary>
        /// Moves the system cursor to absolute screen coordinates (x, y).
        /// </summary>
        public static void MouseMove(int x, int y)
        {
            SetCursorPos(x, y);
        }

        /// <summary>
        /// Triggers a mouse click (left, right, or middle) at the current cursor position.
        /// </summary>
        public static void MouseClick(string button)
        {
            int downFlag = MOUSEEVENTF_LEFTDOWN;
            int upFlag = MOUSEEVENTF_LEFTUP;

            if (button.Equals("right", StringComparison.OrdinalIgnoreCase))
            {
                downFlag = MOUSEEVENTF_RIGHTDOWN;
                upFlag = MOUSEEVENTF_RIGHTUP;
            }
            else if (button.Equals("middle", StringComparison.OrdinalIgnoreCase))
            {
                downFlag = MOUSEEVENTF_MIDDLEDOWN;
                upFlag = MOUSEEVENTF_MIDDLEUP;
            }

            mouse_event(downFlag, 0, 0, 0, 0);
            Thread.Sleep(50);
            mouse_event(upFlag, 0, 0, 0, 0);
        }

        /// <summary>
        /// Simulates a single key press and release of the specified Virtual Key code.
        /// </summary>
        public static void KeyPress(int keyCode)
        {
            keybd_event((byte)keyCode, 0, 0, 0);
            Thread.Sleep(50);
            keybd_event((byte)keyCode, 0, KEYEVENTF_KEYUP, 0);
        }

        /// <summary>
        /// Simulates sequential typing of a string of Unicode characters.
        /// </summary>
        public static void KeyType(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            INPUT[] inputs = new INPUT[text.Length * 2];
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                
                inputs[2 * i] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KeyboardInput
                        {
                            wVk = 0,
                            wScan = (short)c,
                            dwFlags = KEYEVENTF_UNICODE,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                };

                inputs[2 * i + 1] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KeyboardInput
                        {
                            wVk = 0,
                            wScan = (short)c,
                            dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                };
            }
            SendInput(inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}
