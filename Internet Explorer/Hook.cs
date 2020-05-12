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

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr i);

        /// <summary>
        /// Returns a list of child windows
        /// </summary>
        /// <param name="parent">Parent of the windows to return</param>
        /// <returns>List of child windows</returns>
        public static List<IntPtr> GetChildWindows(IntPtr parent)
        {
            List<IntPtr> result = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(result);

            try
            {
                EnumWindowProc childProc = new EnumWindowProc(EnumWindow);
                EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }

            if (result.Count > 0)
            {
                List<IntPtr> childrensOfChildrens = new List<IntPtr>();

                foreach (IntPtr childHandle in result)
                {
                    var res = GetChildWindows(childHandle);
                    childrensOfChildrens.AddRange(res);
                }

                result.AddRange(childrensOfChildrens);
            }

            return result;
        }

        /// <summary>
        /// Callback method to be used when enumerating windows.
        /// </summary>
        /// <param name="handle">Handle of the next window</param>
        /// <param name="pointer">Pointer to a GCHandle that holds a reference to the list to fill</param>
        /// <returns>True to continue the enumeration, false to bail</returns>
        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointer);
            List<IntPtr> list = gch.Target as List<IntPtr>;
            if (list == null)
            {
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            }
            list.Add(handle);
            //  You can modify this to check to see if you want to cancel the operation, then return a null here
            return true;
        }

        /// <summary>
        /// Delegate for the EnumChildWindows method
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <param name="parameter">Caller-defined variable; we use it for a pointer to our list</param>
        /// <returns>True to continue enumerating, false to bail.</returns>
        private delegate bool EnumWindowProc(IntPtr hWnd, IntPtr parameter);

        private static string GetTextFromHandle(IntPtr handle)
        {
            // Allocate string length 
            int length = (int)SendMessage(handle, WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);
            StringBuilder sb = new StringBuilder(length + 1);

            //Get window text
            SendMessage(handle, WM_GETTEXT, (IntPtr)sb.Capacity, sb);

            string answer = sb.ToString();
            return answer;
        }

        [DllImport("user32", EntryPoint = "RegisterWindowMessage")]
        private static extern int RegisterWindowMessage(string lpString);

        [DllImport("oleacc.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr LresultFromObject(ref Guid refiid, IntPtr wParam, IntPtr pAcc);

        [DllImport("oleacc.dll", SetLastError = true)]
        internal static extern int AccessibleObjectFromWindow(IntPtr hwnd, uint id, ref Guid iid,
                                                [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object ppvObject);

        private static HtmlDocument GetHtmlDocument(IntPtr hwnd)
        {
            Guid IID_IDispatch = new Guid("{00020400-0000-0000-C000-000000000046}");
            const uint OBJID_NATIVEOM = 0xFFFFFFF0;

            //object app = null;

            //if (AccessibleObjectFromWindow(hwnd, OBJID_NATIVEOM, ref IID_IDispatch, ref app) == 0)
            //{
            //    dynamic appWindow = app;
            //    //appRetVal = appWindow.Application;
            //    return app as HtmlDocument;
            //}

            //return null;



            object domObject = new object();
            IntPtr tempInt = IntPtr.Zero;
            Guid guidIEDocument2 = new Guid();

            int WM_Html_GETOBJECT = RegisterWindowMessage("WM_Html_GETOBJECT");

            IntPtr W = SendMessage(hwnd, 0x003D, IntPtr.Zero, ref tempInt);
            IntPtr lreturn = LresultFromObject(ref guidIEDocument2, IntPtr.Zero, hwnd);

            HtmlDocument doc = (HtmlDocument)domObject;

            return doc;
        }

        private static string TextFromSelection(AutomationElement target, int length = -1)
        {
            // Specify the control type we're looking for, in this case 'Document'
            PropertyCondition cond = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Pane);

            AutomationPattern[] targetSupported = target.GetSupportedPatterns();
            AutomationProperty[] targetProperties = target.GetSupportedProperties();

            List<string> propertyNames = new List<string>();

            foreach (AutomationProperty property in targetProperties)
            {
                propertyNames.Add(property.ProgrammaticName);
            }

            // target --> The root AutomationElement.
            AutomationElement textProvider = target.FindFirst(TreeScope.Subtree, cond);
            AutomationElementCollection collection = target.FindAll(TreeScope.Subtree, cond);

            AutomationElement pageContainer = textProvider;

            for (int i = 0; i < collection.Count; i++)
            {
                AutomationElement element = collection[i];

                if (element.Current.Name.ToLower().Trim().Contains("контейнер страницы"))
                {
                    pageContainer = element;
                    break;
                }
            }

            AutomationPattern[] pageSupported = pageContainer?.GetSupportedPatterns();
            AutomationProperty[] pageProperties = pageContainer?.GetSupportedProperties();

            AutomationPattern[] supported = textProvider?.GetSupportedPatterns();
            AutomationProperty[] properties = textProvider?.GetSupportedProperties();

            var test1 = pageContainer.FindAll(TreeScope.Subtree, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text));

            AutomationElementCollection docList = pageContainer.FindAll(TreeScope.Subtree, new PropertyCondition(AutomationElement.IsControlElementProperty, true));

            AutomationPattern[] docSupported;
            AutomationProperty[] docProperties;

            for (int i = 0; i < docList.Count; i++)
            {
                AutomationElement element = docList[i];

                docSupported = element?.GetSupportedPatterns();
                docProperties = element?.GetSupportedProperties();

                if (docSupported.Length != 0)
                    break;
            }

            var focused = AutomationElement.FocusedElement;

            var test2 = focused.FindAll(TreeScope.Subtree, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text));

            AutomationPattern[] focusedSupported = focused?.GetSupportedPatterns();
            AutomationProperty[] focusedProperties = focused?.GetSupportedProperties();

            List<string> focusedPropertyNames = new List<string>();

            foreach (AutomationProperty property in focusedProperties)
            {
                focusedPropertyNames.Add(property.ProgrammaticName);
            }

            var ll = focused.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.IsControlElementProperty, true));

            TextPattern textpatternPattern = focused.GetCurrentPattern(TextPattern.Pattern) as TextPattern;

            //object curPattern = textProvider.GetCurrentPattern(supported[0]);

            if (textpatternPattern == null)
            {
                Console.WriteLine("Root element does not contain a descendant that supports TextPattern.");
                return String.Empty;
            }

            return textpatternPattern.DocumentRange.GetText(length);

            //TextPatternRange[] currentSelection = textpatternPattern.GetSelection();

            //// GetText(-1) retrieves all characters but can be inefficient
            //return currentSelection[0].GetText(length);
        }

        public static string GetSelectedText()
        {
            Type whatDoesContainClipboard = null;
            object clipboardData = null;

            if (Clipboard.ContainsAudio())
            {
                whatDoesContainClipboard = typeof(Stream);
                clipboardData = Clipboard.GetAudioStream();
            }
            else if (Clipboard.ContainsImage())
            {
                whatDoesContainClipboard = typeof(BitmapSource);
                clipboardData = Clipboard.GetImage();
            }
            else if (Clipboard.ContainsText())
            {
                whatDoesContainClipboard = typeof(string);
                clipboardData = Clipboard.GetText();
            }

            IntPtr handle = GetForegroundWindow();
            int ProcessID;
            int SelectedThreadId = GetWindowThreadProcessId(handle, out ProcessID);
            int CurrentThreadId = GetCurrentThreadId();
            Process SelectedProcess = Process.GetProcessById(ProcessID);


            //SendKeys.SendWait("^a");
            //SendKeys.SendWait("{ESC}");


            //// Get the ThreadInfo structure
            //GuiThreadInfo threadInfo = new GUITHREADINFO();
            //threadInfo.cbSize = (uint)Marshal.SizeOf(threadInfo);
            //GetGUIThreadInfo(0, out threadInfo);

            //// Send WM_COPY message to the control with focus
            //SendMessage(threadInfo.hwndFocus, WM_COPY, 0, 0);



            IntPtr windowHandle = SelectedProcess.MainWindowHandle;

            List<IntPtr> childrens = GetChildWindows(windowHandle);

            string totalData = String.Empty;

            for (int i = -1; i < childrens.Count; i++)
            {
                try
                {
                    string answer = string.Empty;

                    if (i == -1)
                    {
                        AutomationElement ae = AutomationElement.FromHandle(windowHandle);
                        answer = TextFromSelection(ae);
                        //answer = GetTextFromHandle(windowHandle);
                    }
                    else
                    {
                        AutomationElement ae = AutomationElement.FromHandle(childrens[i]);
                        answer = TextFromSelection(ae);
                        //answer = GetTextFromHandle(childrens[i]);
                    }

                    totalData += answer + Environment.NewLine + Environment.NewLine;
                }
                catch (Exception ignore)
                {
                    var someError = ignore;
                }
            }

            return totalData;



            //AutomationElement ae = AutomationElement.FromHandle(SelectedProcess.MainWindowHandle);
            //string answer = TextFromSelection(ae);

            //return answer;



            //List<IntPtr> childrens = GetChildWindows(SelectedProcess.MainWindowHandle);

            //string totalData = String.Empty;

            //for (int i = 0; i < childrens.Count; i++)
            //{
            //    var doc = GetHtmlDocument(childrens[i]);

            //    totalData += doc?.ToString() + Environment.NewLine + Environment.NewLine;
            //}

            //return totalData;



            //var doc = GetHtmlDocument(SelectedProcess.Handle);

            //string html = doc?.Body?.InnerHtml ?? "DOM элемент не найден";

            //return html;



            //SelectedProcess.StartInfo.RedirectStandardOutput = true;

            //using (StreamReader reader = SelectedProcess.StandardOutput)
            //{
            //    string result = reader.ReadToEnd();
            //    return result;
            //}



            //List<IntPtr> childrens = GetChildWindows(SelectedProcess.MainWindowHandle);

            //string totalData = String.Empty;

            //for (int i = 0; i < childrens.Count; i++)
            //{
            //    totalData += GetTextFromHandle(childrens[i]) + Environment.NewLine + Environment.NewLine;
            //}

            //return totalData;




            //int length = (int)SendMessage(SelectedProcess.MainWindowHandle, WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);
            //StringBuilder sb = new StringBuilder(length + 1);

            //GetWindowText(SelectedProcess.MainWindowHandle, sb, sb.Capacity);

            //String answer = sb.ToString();
            //return answer;


            //// Allocate string length 
            //int length = (int)SendMessage(SelectedProcess.Handle, WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);
            //StringBuilder sb = new StringBuilder(length + 1);

            // Get window text
            //SendMessage(SelectedProcess.Handle, WM_GETTEXT, (IntPtr)sb.Capacity, sb);

            //String answer = sb.ToString();
            //return answer;


            //AutomationElement ae = AutomationElement.FromHandle(SelectedProcess.MainWindowHandle);
            //AutomationElement npEdit = ae.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ClassNameProperty, "Edit"));

            //TextPattern tp = npEdit.GetCurrentPattern(TextPattern.Pattern) as TextPattern;

            //TextPatternRange[] trs;

            //if (tp.SupportedTextSelection == SupportedTextSelection.None)
            //{
            //    return "No selected";
            //}
            //else
            //{
            //    trs = tp.GetSelection();
            //    return trs[0].GetText(-1);
            //}



            //AttachThreadInput(SelectedThreadId, CurrentThreadId, true);
            //IntPtr FocusedWindowEx = GetFocus();
            //SendMessage(FocusedWindowEx, WM_COPY, IntPtr.Zero, IntPtr.Zero);

            //IDataObject data = Clipboard.GetDataObject();
            //string[] formats = data.GetFormats();

            //var selectedText = "";
            //if (formats.Contains<string>("System.String"))
            //    selectedText = (string)data.GetData("System.String");

            //AttachThreadInput(SelectedThreadId, CurrentThreadId, false);

            //if (whatDoesContainClipboard != null)
            //{
            //    if (whatDoesContainClipboard == typeof(Stream))
            //        Clipboard.SetAudio((Stream)clipboardData);
            //    else if (whatDoesContainClipboard == typeof(BitmapSource))
            //        Clipboard.SetImage(BitmapSourceToImage((BitmapSource)clipboardData));
            //    else if (whatDoesContainClipboard == typeof(string))
            //        Clipboard.SetText((string)clipboardData);
            //}
            //else
            //    Clipboard.Clear();

            //Thread.Sleep(TimeSpan.FromSeconds(1));

            //selectedText = Clipboard.GetText();

            //return selectedText;
        }

        private static System.Drawing.Image BitmapSourceToImage(BitmapSource bitmapSource)
        {
            System.Drawing.Bitmap bitmap;

            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();

                enc.Frames.Add(BitmapFrame.Create(bitmapSource));
                enc.Save(outStream);

                bitmap = new System.Drawing.Bitmap(outStream);
            }

            return bitmap;
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

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, StringBuilder lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("kernel32.dll")]
        private static extern int GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern int AttachThreadInput(int idAttach, int idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern IntPtr GetFocus();
    }
}
