using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Keys = System.Windows.Forms.Keys;
using MouseButtons = System.Windows.Forms.MouseButtons;

namespace Internet_Explorer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
/*        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);*/

        private List<QA> QAList = new List<QA>();
        private List<QA> AnswersList = new List<QA>();
        private List<QA>.Enumerator AnswersListEnumerator;

        private bool IsTryingToGetQuestion = false;
        private bool IsCanWeSendCopyMessage = true;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Hook.SetHook();
            Hook.HookKeyboardCallbac += Hook_KeyPressed;
            Hook.HookMouseCallback += Hook_MousePressed;

            LoadTest();
        }

        private void Hook_KeyPressed(Keys key)
        {
            if (passwordTextBox.Visibility == Visibility.Visible)
            {
                if (key == Keys.Enter && CheckPassword())
                {
                    passwordTextBox.Visibility = Visibility.Collapsed;
                    answerTextBlock.Visibility = Visibility.Visible;
                    answerTextBlock.Opacity = 0.05f;
                }

                return;
            }

            KeyboardKeyHandler(key);
        }

        private void Hook_MousePressed(MouseButtons key)
        {
            if (passwordTextBox.Visibility == Visibility.Visible)
            {
                return;
            }

            Task.Run(() => MouseKeyHandler(key));

            //MouseKeyHandler(key);
        }

        private void KeyboardKeyHandler(Keys key)
        {
            switch (key)
            {
                case Keys.A:
                    ClearAnswerBlock();
                    Hide();
                    break;

                case Keys.S:
                    Show();
                    answerTextBlock.Text = SearchQuestion(GetQuestion());
                    break;

                case Keys.D:
                    Show();
                    answerTextBlock.Text = GetNextQuestion();
                    break;

                case Keys.Q:
                    Close();
                    break;

                case Keys.Z:
                    DecreaseOpacity();
                    break;

                case Keys.X:
                    IncreaseOpacity();
                    break;

                /*case Keys.W:
                    HideTaskBar();
                    break;*/
            }
        }

        private void MouseKeyHandler(MouseButtons key)
        {
            switch (key)
            {
                case MouseButtons.Left:
                    passwordTextBox.Dispatcher.InvokeAsync(() => {
                        ClearAnswerBlock();
                        Hide();
                    });
                    break;

                // Прокрутка среднего колёсика мыши (направление неизвестно), а не нажатие на него
                case MouseButtons.Middle:
                    string answer = SearchQuestion(GetQuestion());

                    passwordTextBox.Dispatcher.Invoke(() =>
                    {
                        answerTextBlock.Text = answer;
                        Show();
                    });
                    break;
            }
        }

/*        private void HideTaskBar()
        {
            IntPtr hWnd = FindWindow("Shell_TrayWnd", String.Empty);

            if (hWnd != IntPtr.Zero)
            {
                ShowWindow(hWnd, 0);
                Thread.Sleep(1000);
                ShowWindow(hWnd, 1);
            }
        }

        private void OpenClippers()
        {
            // snippingtool.exe

            string batchName = "openClippers.bat";
            string batchCommands = string.Empty;

            batchCommands += "@ECHO OFF\n";
            batchCommands += "chcp 1251 > nul \n";
            batchCommands += "snippingtool > nul \n";
            batchCommands += $"del {batchName}";

            File.WriteAllText(batchName, batchCommands, Encoding.GetEncoding("Windows-1251"));

            Process bat = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.FileName = "cmd";
            startInfo.Arguments = $@"/c snippingtool";
            startInfo.RedirectStandardOutput = false;
            startInfo.UseShellExecute = true;
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            bat.StartInfo = startInfo;
            bat.Start();
        }*/

        private bool CheckPassword()
        {
            return passwordTextBox.Password.ToLower() == Properties.Resources.Password || passwordTextBox.Password.ToLower() == Properties.Resources.Password_rus;
        }

        private void DecreaseOpacity()
        {
            answerTextBlock.Opacity -= 0.05f;

            if (answerTextBlock.Opacity <= 0)
                answerTextBlock.Opacity = 0.05f;
        }

        private void IncreaseOpacity()
        {
            answerTextBlock.Opacity += 0.05f;

            if (answerTextBlock.Opacity >= 1)
                answerTextBlock.Opacity = 1f;
        }

        private void ClearAnswerBlock()
        {
            answerTextBlock.Text = "";
        }

        private string GetQuestion()
        {
            if (IsTryingToGetQuestion || !IsCanWeSendCopyMessage)
                return String.Empty;

            IsTryingToGetQuestion = true;
            IsCanWeSendCopyMessage = false;

            Task.Run(() =>
            {
                Thread.Sleep(2000);
                IsCanWeSendCopyMessage = true;
            });

            System.Windows.Forms.SendKeys.SendWait("^(c)");

            Stopwatch stopwatch = Stopwatch.StartNew();
            string anser = passwordTextBox.Dispatcher.Invoke(() => Clipboard.GetText()?.ToLower());

            while (!passwordTextBox.Dispatcher.Invoke(() => Clipboard.ContainsText()) && stopwatch.Elapsed.TotalMilliseconds <= 1300)
            {
                Thread.Sleep(50);
                anser = passwordTextBox.Dispatcher.Invoke(() => Clipboard.GetText()?.ToLower());
            }

            passwordTextBox.Dispatcher.Invoke(() => Clipboard.Clear());
            stopwatch.Reset();

            IsTryingToGetQuestion = false;
            return anser;

            //return Hook.GetSelectedText();
            //return Clipboard.GetText()?.ToLower();
        }

        private string GetNextQuestion()
        {
            if ((AnswersList?.Count ?? 0) == 0)
                return answerTextBlock.Text;

            if (AnswersListEnumerator.MoveNext())
            {
                return AnswersListEnumerator.Current?.answer?.Trim() ?? "Не нашёл";
            }
            else
            {
                AnswersListEnumerator = AnswersList.GetEnumerator();
                AnswersListEnumerator.MoveNext();
                return AnswersListEnumerator.Current?.answer?.Trim() ?? "Не нашёл";
            }
        }

        private string SearchQuestion(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                return passwordTextBox.Dispatcher.Invoke(() => answerTextBlock.Text);

            AnswersList = QAList.Where(qa => qa.question.ToLower().Contains(question))?.ToList();
            AnswersListEnumerator = AnswersList.GetEnumerator();

            if (AnswersListEnumerator.MoveNext())
                return AnswersListEnumerator.Current?.answer?.Trim() ?? "Не нашёл";
            else
                return "Не нашёл";
        }

        private void DeleteMySelf()
        {
            string batchName = "deleteMyProgram.bat";
            string batchCommands = string.Empty;
            string appName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);

            batchCommands += "@ECHO OFF\n";
            batchCommands += "chcp 1251 > nul \n";
            batchCommands += "ping 127.0.0.1 > nul\n";
            batchCommands += $@"del /F ""{appName}**.exe"" {Environment.NewLine}";
            batchCommands += $"del {batchName}";

            File.WriteAllText(batchName, batchCommands, Encoding.GetEncoding("Windows-1251"));

            Process bat = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.FileName = batchName;
            startInfo.RedirectStandardOutput = false;
            startInfo.UseShellExecute = true;
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            bat.StartInfo = startInfo;
            bat.Start();
        }

        private void DeleteChrome()
        {
            Process cmd = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();

            string dir = Directory.GetCurrentDirectory();
            string appName = @"chrome**.exe";

            startInfo.FileName = "cmd";
            startInfo.Arguments = $@"/c cd ""{dir}"" & del /F {appName}";
            startInfo.RedirectStandardOutput = false;
            startInfo.UseShellExecute = true;
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            cmd.StartInfo = startInfo;
            cmd.Start();
            cmd.WaitForExit();
        }

        private void LoadTest()
        {
            string json = Test.GetJsonTest();

            QAList = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<List<QA>>(json);
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            Topmost = true;
            Background = new SolidColorBrush(Color.FromArgb(0, 34, 34, 34));
            passwordTextBox.Background = new SolidColorBrush(Color.FromArgb(0, 34, 34, 34));
            passwordTextBox.BorderBrush = new SolidColorBrush(Color.FromArgb(0, 34, 34, 34));

            double screenHeight = SystemParameters.FullPrimaryScreenHeight;
            double screenWidth = SystemParameters.FullPrimaryScreenWidth;

            Width = screenWidth - 300;

            Top = (screenHeight - Height);
            Left = (screenWidth - Width) / 2;

            passwordTextBox.Focus();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Hook.Dispose();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            string appName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);

            if (!appName.Contains("chrome"))
                DeleteChrome();

            DeleteMySelf();
        }
    }
}
