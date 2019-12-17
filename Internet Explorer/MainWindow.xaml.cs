using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Keys = System.Windows.Forms.Keys;

namespace Internet_Explorer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private WebBrowser2 WebBrowser;
        //private bool catchKeys = false;

        private List<QA> QAList = new List<QA>();
        private List<QA> AnswersList = new List<QA>();
        private List<QA>.Enumerator AnswersListEnumerator;

        //private KeyboardHook keyboardHook;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //WebBrowser2.OnProgramStart();
            ////WebBrowser2.UserAgent = @"Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 5.1; Trident/4.0; FDM; .NET4.0C; .NET4.0E; .NET CLR 2.0.50727)";

            //WebBrowser = new WebBrowser2();
            //WebBrowser.ScriptErrorsSuppressed = true;
            //WebBrowser.Height = (int)Height - 40;

            //var formsHost = new WindowsFormsHost { Child = WebBrowser };
            //stackPanel.Children.Add(formsHost);
            //stackPanel.CanVerticallyScroll = true;

            //WebBrowser.Navigate(@"http://platonus.turan-edu.kz/");

            Hook.SetHook();
            Hook.HookCallbackAction += Hook_KeyPressed;

            //keyboardHook = new KeyboardHook();

            //keyboardHook.RegisterHotKey(ModifierKeys.Alt, System.Windows.Forms.Keys.S);
            //keyboardHook.RegisterHotKey(ModifierKeys.Alt, System.Windows.Forms.Keys.A);
            //keyboardHook.RegisterHotKey(ModifierKeys.Alt, System.Windows.Forms.Keys.X);
            //keyboardHook.RegisterHotKey(ModifierKeys.Alt, System.Windows.Forms.Keys.D);

            //keyboardHook.KeyPressed += KeyboardHook_KeyPressed;

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
                }

                return;
            }

            KeyHandler(key);
        }

        private void KeyHandler(Keys key)
        {
            switch (key)
            {
                case Keys.A:
                    ClearAnswerBlock();
                    break;

                case Keys.S:
                    answerTextBlock.Text = SearchQuestion(GetQuestion());
                    break;

                case Keys.D:
                    answerTextBlock.Text = GetNextQuestion();
                    break;

                case Keys.Q:
                    Close();
                    break;

                    //case Keys.Q:
                    //    DeleteMySelf();
                    //    break;
            }
        }

        //private void DeleteMySelf()
        //{
        //    ProcessStartInfo Info = new ProcessStartInfo();
        //    Info.Arguments = "/C choice /C Y /N /D Y /T 3 & Del " + Process.GetCurrentProcess().MainModule.FileName;
        //    Info.WindowStyle = ProcessWindowStyle.Hidden;
        //    Info.CreateNoWindow = true;
        //    Info.FileName = "cmd.exe";
        //    Process.Start(Info);

        //    CloseThisApp();

        //    //string path = Process.GetCurrentProcess().MainModule.FileName;
        //    //string appName = Path.GetFileNameWithoutExtension(path);
        //    //string batName = "~.bat";

        //    //string data = string.Format("@echo off{0}:loop{0}del {1}{0}if exist {1} goto loop{0}del {2}", Environment.NewLine, appName, batName);

        //    //using (StreamWriter writer = new StreamWriter(batName, false))
        //    //{
        //    //    writer.Write(data);
        //    //}

        //    //Process.Start(batName);
        //}

        private bool CheckPassword()
        {
            return passwordTextBox.Password.ToLower() == Properties.Resources.Password || passwordTextBox.Password.ToLower() == Properties.Resources.Password_rus;
        }

        private void ClearAnswerBlock()
        {
            answerTextBlock.Text = "";
        }

        private string GetQuestion()
        {
            return Clipboard.GetText()?.ToLower();
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
            AnswersList = QAList.Where(qa => qa.question.ToLower().Contains(question))?.ToList();
            AnswersListEnumerator = AnswersList.GetEnumerator();

            if (AnswersListEnumerator.MoveNext())
                return AnswersListEnumerator.Current?.answer?.Trim() ?? "Не нашёл";
            else
                return "Не нашёл";
        }

        private void DeleteMySelf()
        {
            Process cmd = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();

            string dir = Directory.GetCurrentDirectory();
            string path = Process.GetCurrentProcess().MainModule.FileName;
            string appName = Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location).Split(' ')[0] + @"**.exe";  //Path.GetFileName(path).Replace(@".", @"*.");

            startInfo.FileName = "cmd";
            startInfo.Arguments = $@"/c cd ""{dir}"" & del /F {appName}";
            startInfo.RedirectStandardOutput = false;
            startInfo.UseShellExecute = true;
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            cmd.StartInfo = startInfo;
            cmd.Start();
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
        }

        //private void KeyboardHook_KeyPressed(object sender, KeyPressedEventArgs e)
        //{
        //    if (passwordTextBox.Visibility == Visibility.Visible)
        //    {
        //        passwordTextBox.Focus();
        //        return;
        //    }

        //    KeyHandler(e.Key);
        //}

        private void LoadTest()
        {
            string json = Test.GetJsonTest();

            QAList = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<List<QA>>(json);
        }

        private void Window_SourceInitialized(object sender, System.EventArgs e)
        {
            //IconHelper.RemoveIcon(this);

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
            //keyboardHook.Dispose();
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            DeleteChrome();
            DeleteMySelf();
        }
    }
}
