using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Internet_Explorer
{
    public class Hook
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_HOTKEY = 0x0312;
        private const int WM_COPY = 0x0301;

        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public static Action<Keys> HookCallbackAction;

        public static void SetHook()
        {
            using (Process curProcess = Process.GetCurrentProcess())
            {
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        public static bool Dispose()
        {
            return UnhookWindowsHookEx(_hookID);
        }

        protected static void OnHookCallback(Keys key)
        {
            HookCallbackAction?.Invoke(key);
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                OnHookCallback((Keys)vkCode);
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
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
