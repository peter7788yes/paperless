using PaperLess_Emeeting.App_Code.MessageBox;
using System;
using System.Collections.Generic;
using System.Data;
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
    public partial class JoinFolder : Window
    {
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private List<string> list;
        //private Action<string> callback;
        //public string ResponseText { get; set; }
        private string UserID;
        private string UserPWD;
        Action<string> rtnName;
        string FolderName;
        string UserName;
        private Window win;
        string MeetingID;

        FolderDataFolderListFolder nowVM;
        string FileID;
        Action<string, string> callback;
        public JoinFolder(Window win,string FileID,Action<string,string> callback)
        {
            MouseTool.ShowLoading();
            this.win = win;
            InitializeComponent();
            this.Loaded += ConfirmWindow_Loaded;
            this.KeyDown += ConfirmWindow_KeyDown;
            //this.callback = callback;
            //this.tbPinCode.Text = FolderName;
            this.FileID = FileID;
            Window Home_Window = App.Current.Windows.OfType<Home>().FirstOrDefault();
            if (Home_Window != null)
            {
                this.Owner = win;
            }
            this.callback = callback;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            //tbPinCode.SelectAll();
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
            DataTable dt = MSCE.GetDataTable("select UserID,UserName,UserPWD,MeetingID from NowLogin");
            if (dt.Rows.Count > 0)
            {
                UserID = dt.Rows[0]["UserID"].ToString();
                UserName = dt.Rows[0]["UserName"].ToString();
                UserPWD = dt.Rows[0]["UserPWD"].ToString();
                MeetingID = dt.Rows[0]["MeetingID"].ToString();
            }
        }

        private void InitEvent()
        {
            //btnNO.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            //btnNO.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            //btnNO.MouseLeftButtonDown += (sender, e) => { this.DialogResult = false; tbPinCode.Text = ""; this.Close(); };

            //btnYes.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            //btnYes.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            //btnYes.MouseLeftButtonDown += (sender, e) =>
            //{
            //    CallWS();
            //};

            //tbPinCode.MouseEnter += (sender, e) => { MouseTool.ShowIBeam(); };
            //tbPinCode.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };

            //tbPinCode.PreviewKeyDown += tbPinCode_PreviewKeyDown;
            //tbPinCode.Focus();

            //btnPinCodeClear.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            //btnPinCodeClear.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            //btnPinCodeClear.Click += (sender, e) =>
            //{
            //    tbPinCode.Text = "";
            //    btnPinCodeClear.Visibility = Visibility.Collapsed;
            //};
        }

        private void CallWS()
        {
            //if (tbPinCode.Text.Trim().Equals("") == true)
            //{
            //    AutoClosingMessageBox.Show("請輸入資料夾名稱");
            //}
            //else
            //{
            //    if (list.Contains(tbPinCode.Text.Trim()))
            //    {
            //        AutoClosingMessageBox.Show("資料夾名稱重複，請重新命名。");
            //    }
            //    else
            //    {
            //        var folderName=tbPinCode.Text.Trim();
            //        GetUploadUserFolder.AsyncPOST(UserID, UserPWD,"", folderName,"rename", (fd) => {
            //            this.Dispatcher.BeginInvoke(new Action(() => {
            //                if(fd!=null && fd.Status!=null && fd.Status.Success != null && fd.Status.Success.Equals("Y"))
            //                {
            //                    AutoClosingMessageBox.Show("修改成功");
            //                    this.DialogResult = true;
            //                    this.Close();
            //                }
            //                else
            //                {
            //                    AutoClosingMessageBox.Show("修改失敗");
            //                }
            //            }));

            //        });
            //    }
            //}
           
        }

        private void tbPinCode_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.Enter)
            //{
            //    CallWS();
            //    e.Handled = true;
            //    return;
            //}
            //Task.Factory.StartNew(() =>
            //{
            //    Thread.Sleep(10);
            //    this.Dispatcher.BeginInvoke((Action)(() =>
            //    {
            //        if(tbPinCode.Text.Length > 0)
            //        {
            //            btnPinCodeClear.Visibility = Visibility.Visible;
            //        }
            //        else
            //        {
            //            btnPinCodeClear.Visibility = Visibility.Collapsed;
            //        }
            //    }));
            //});

           
        }

      

        private void InitUI()
        {
            //txtQuest.Text = string.Format("請輸入{0}識別碼", PaperLess_Emeeting.Properties.Settings.Default.CourseOrMeeting_String);
            //SP.Children.Add(new JoinRow("", "", null, ChangeBackground));
            //SP.Children.Add(new JoinRow("", "", null, ChangeBackground));
            //SP.Children.Add(new JoinRow("", "", null, ChangeBackground));
            //SP.Children.Add(new JoinRow("", "", null, ChangeBackground));
            //SP.Children.Add(new JoinRow("", "", null, ChangeBackground));
            //SP.Children.Add(new JoinRow("", "", null, ChangeBackground));

            GetUserFolder.AsyncPOST(UserID, UserPWD, (fd) => { GetUserFolder_DoAction(fd); });
        }

        private void GetUserFolder_DoAction(FolderData fd)
        {
            if (this.Dispatcher.CheckAccess() == false)
            {
                this.Dispatcher.BeginInvoke(new Action<FolderData>(GetUserFolder_DoAction), fd);
            }
            else
            {
                if (fd != null)
                {
                    Task.Factory.StartNew(() =>
                    {
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (fd.FolderList.Folder != null)
                                foreach (FolderDataFolderListFolder item in fd.FolderList.Folder)
                                {
                                    SP.Children.Add(new JoinRow(UserID, UserPWD, item, ChangeBackground));
                                }
                        }));
                    });

                }
                else
                {
                    AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
                }
                MouseTool.ShowArrow();
            }
        }

        private void ChangeBackground(object sender, DictionaryEventArgas e)
        {
            nowVM = e.dict["FolderVM"] as FolderDataFolderListFolder;

            foreach (var item in SP.Children.OfType<JoinRow>())
            {
                item.Background = ColorTool.HexColorToBrush("#00A2C2");
            }
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            callback("", "");
            this.Close();
        }

        private void Image_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            var list = SP.Children.OfType<JoinRow>().Select(item => item.VM.Name).ToList();

            Window Home_Window = App.Current.Windows.OfType<Home>().FirstOrDefault();
            AddFolder win = new AddFolder(UserID, UserPWD, list, this.win);

            var success = win.ShowDialog();

            if (success == true)
                GetUserFolder.AsyncPOST(UserID, UserPWD, (fd) => {
                    this.Dispatcher.BeginInvoke(new Action(() => {
                        SP.Children.Clear();
                        if (fd.FolderList.Folder != null)
                            foreach (FolderDataFolderListFolder item in fd.FolderList.Folder)
                            {
                                SP.Children.Add(new JoinRow(UserID, UserPWD, item, ChangeBackground));
                            }

                    }));

                });
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            GetUploadUserFile.AsyncPOST(UserID, UserPWD, nowVM.ID, FileID, "new", (fd) =>
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (fd.Status.Success.Equals("Y"))
                        {
                            callback(nowVM.ID, nowVM.Name);
                            this.Close();
                        }
                    }
                    catch
                    { }

                }));

            });
            
        }
    }
}
