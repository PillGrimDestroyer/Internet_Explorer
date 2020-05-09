using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Internet_Explorer
{
    public class Hook
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_HOTKEY = 0x0312;
        private const int WM_COPY = 0x0301;

        private static LowLevelKeyboardProc _keyboardProc = KeyboardHookCallback;
        private static LowLevelKeyboardProc _mouseProc = MouseHookCallback;
        private static IntPtr _keyboardHookID = IntPtr.Zero;
        private static IntPtr _mouseHookID = IntPtr.Zero;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public static Action<Keys> HookKeyboardCallbac;
        public static Action<MouseButtons> HookMouseCallback;

        public static void SetHook()
        {
            using (Process curProcess = Process.GetCurrentProcess())
            {
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    _keyboardHookID = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, GetModuleHandle(curModule.ModuleName), 0);
                    _mouseHookID = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        public static bool Dispose()
        {
            UnhookWindowsHookEx(_mouseHookID);
            return UnhookWindowsHookEx(_keyboardHookID);
        }

        protected static void OnHookKeyboardCallback(Keys key)
        {
            HookKeyboardCallbac?.Invoke(key);
        }

        protected static void OnHookMouseCallback(MouseButtons key)
        {
            HookMouseCallback?.Invoke(key);
        }

        private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                OnHookKeyboardCallback((Keys)vkCode);
            }

            return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
        }

        private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            bool isMouseLButtonDown = wParam == (IntPtr)WM_LBUTTONDOWN;
            bool isMouseRButtonDown = wParam == (IntPtr)WM_RBUTTONDOWN;
            bool isMouseWheel = wParam == (IntPtr)WM_MOUSEWHEEL;

            if (nCode >= 0 && (isMouseLButtonDown || isMouseRButtonDown || isMouseWheel))
            {
                if (isMouseLButtonDown)
                    OnHookMouseCallback(MouseButtons.Left);
                else if (isMouseRButtonDown)
                    OnHookMouseCallback(MouseButtons.Right);
                else if (isMouseWheel)
                    OnHookMouseCallback(MouseButtons.Middle);
            }

            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }

        //public static string GetSelectedText()
        //{
        //    Type whatDoesContainClipboard = null;
        //    object clipboardData = null;

        //    if (Clipboard.ContainsAudio())
        //    {
        //        whatDoesContainClipboard = typeof(Stream);
        //        clipboardData = Clipboard.GetAudioStream();
        //    }
        //    else if (Clipboard.ContainsImage())
        //    {
        //        whatDoesContainClipboard = typeof(BitmapSource);
        //        clipboardData = Clipboard.GetImage();
        //    }
        //    else if (Clipboard.ContainsText())
        //    {
        //        whatDoesContainClipboard = typeof(string);
        //        clipboardData = Clipboard.GetText();
        //    }

        //    int handle = GetForegroundWindow();
        //    int ProcessID;
        //    int SelectedThreadId = GetWindowThreadProcessId(handle, out ProcessID);
        //    int CurrentThreadId = GetCurrentThreadId();
        //    Process SelectedProcess = Process.GetProcessById(ProcessID);

        //    AttachThreadInput(SelectedThreadId, CurrentThreadId, true);
        //    IntPtr FocusedWindowEx = GetFocus();
        //    SendMessage(FocusedWindowEx, WM_COPY, IntPtr.Zero, IntPtr.Zero);

        //    IDataObject data = Clipboard.GetDataObject();
        //    string[] formats = data.GetFormats();

        //    var selectedText = "";
        //    if (formats.Contains<string>("System.String"))
        //        selectedText = (string)data.GetData("System.String");

        //    AttachThreadInput(SelectedThreadId, CurrentThreadId, false);

        //    if (whatDoesContainClipboard != null)
        //    {
        //        if (whatDoesContainClipboard == typeof(Stream))
        //            Clipboard.SetAudio((Stream)clipboardData);
        //        else if (whatDoesContainClipboard == typeof(BitmapSource))
        //            Clipboard.SetImage((BitmapSource)clipboardData);
        //        else if (whatDoesContainClipboard == typeof(string))
        //            Clipboard.SetText((string)clipboardData);
        //    }
        //    else
        //        Clipboard.Clear();

        //    return selectedText;
        //}

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern int GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(int hWnd, out int lpdwProcessId);

        [DllImport("User32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern int GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern int AttachThreadInput(int idAttach, int idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern IntPtr GetFocus();
    }
}
