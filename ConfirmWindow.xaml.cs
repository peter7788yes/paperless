using PaperLess_Emeeting.App_Code.MessageBox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PaperLess_Emeeting
{
    /// <summary>
    /// ConfirmWindow.xaml 的互動邏輯
    /// </summary>
    public partial class ConfirmWindow : Window
    {
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        //public string ResponseText { get; set; }

        public ConfirmWindow()
        {
            MouseTool.ShowLoading();
            InitializeComponent();
            this.Loaded += ConfirmWindow_Loaded;
            this.KeyDown += ConfirmWindow_KeyDown;

            Window Home_Window = App.Current.Windows.OfType<Home>().FirstOrDefault();
            if (Home_Window != null)
            {
                this.Owner = Home_Window;
            }
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void ConfirmWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                this.DialogResult = true; 
                this.Close();
            }
        }

        private void ConfirmWindow_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);

            Task.Factory.StartNew(() =>
            {
                InitSelectDB();
                //this.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        InitUI();
                        InitEvent();
                    }
                    catch (Exception ex)
                    {
                        LogTool.Debug(ex);
                    }
                    MouseTool.ShowArrow();
                }));

            });
        }

        private void InitSelectDB()
        {
        }

        private void InitEvent()
        {
            btnNO.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnNO.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnNO.MouseLeftButtonDown += (sender, e) => { this.DialogResult = false;  tbPinCode.Text = ""; this.Close(); };

            btnYes.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnYes.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnYes.MouseLeftButtonDown += (sender, e) => {

                if (tbPinCode.Text.Trim().Equals("") == true)
                {
                    AutoClosingMessageBox.Show(string.Format("請先輸入{0}識別碼", PaperLess_Emeeting.Properties.Settings.Default.CourseOrMeeting_String));
                    return;
                }
                this.DialogResult = true; this.Close(); 
            };

            tbPinCode.MouseEnter += (sender, e) => { MouseTool.ShowIBeam(); };
            tbPinCode.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };

            tbPinCode.PreviewKeyDown +=tbPinCode_PreviewKeyDown;
            tbPinCode.Focus();

            btnPinCodeClear.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnPinCodeClear.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnPinCodeClear.Click += (sender, e) =>
            {
                tbPinCode.Text = "";
                btnPinCodeClear.Visibility = Visibility.Collapsed;
            };
        }

        private void tbPinCode_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(10);
                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (tbPinCode.Text.Length > 0)
                    {
                        btnPinCodeClear.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        btnPinCodeClear.Visibility = Visibility.Collapsed;
                    }
                }));
            });
        }

      

        private void InitUI()
        {
            txtQuest.Text = string.Format("請輸入{0}識別碼", PaperLess_Emeeting.Properties.Settings.Default.CourseOrMeeting_String);

        }
    }
}
