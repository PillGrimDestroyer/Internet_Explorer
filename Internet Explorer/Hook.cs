using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Automation.Text;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

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

        private const uint WM_GETTEXT = 0x000D;
        private const uint WM_GETTEXTLENGTH = 0x000E;

        private static LowLevelKeyboardProc _keyboardProc = KeyboardHookCallback;
        private static LowLevelKeyboardProc _mouseProc = MouseHookCallback;
        private static IntPtr _keyboardHookID = IntPtr.Zero;
        private static IntPtr _mouseHookID = IntPtr.Zero;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public static Action<Keys> HookKeyboardCallbac;
        public static Action<MouseButtons> HookMouseCallback;

        public static bool UseMouseHook = false;

        public static void SetHook()
        {
            using (Process curProcess = Process.GetCurrentProcess())
            {
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    _keyboardHookID = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, GetModuleHandle(curModule.ModuleName), 0);

                    if (UseMouseHook)
                        _mouseHookID = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        public static bool Dispose()
        {
            if (UseMouseHook)
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
            Task.Run(() =>
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
            });

            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
