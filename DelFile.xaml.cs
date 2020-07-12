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
    /// <summary>
    /// ConfirmWindow.xaml 的互動邏輯
    /// </summary>
    public partial class DelFile : Window
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
        string FolderID;
        string FolderName;
        Window win;
        string FileID;
        public bool ReConfirm = false;
        public DelFile(Window win,string FolderID ,string FileID)
        {
            MouseTool.ShowLoading();
            InitializeComponent();
            this.Loaded += ConfirmWindow_Loaded;
            this.KeyDown += ConfirmWindow_KeyDown;
            //this.callback = callback;
            this.win = win;
            this.FileID = FileID;
            this.FolderID = FolderID;
            //this.tbPinCode.Text = FolderName;
            Window Home_Window = App.Current.Windows.OfType<Home>().FirstOrDefault();
            if (Home_Window != null)
            {
                this.Owner = win;
            }
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
                //UserName = dt.Rows[0]["UserName"].ToString();
                UserPWD = dt.Rows[0]["UserPWD"].ToString();
                //MeetingID = dt.Rows[0]["MeetingID"].ToString();
            }

            dt = MSCE.GetDataTable("select a.FolderID , b.FolderName  from userfile as a join userFolder as b on a.FolderID = b.folderID where a. userid =@1 and a.fileid=@2"
                                    ,UserID
                                    ,FileID);
            if (dt.Rows.Count > 0)
            {
                FolderID = dt.Rows[0]["FolderID"].ToString();
                FolderName = dt.Rows[0]["FolderName"].ToString();
            }

            if(FolderName==null ||FolderName.Length==0)
            {
                GetUserFolder.AsyncPOST(UserID, UserPWD, (fd) =>
                {
                    if (fd.FolderList.Folder != null)
                        foreach (FolderDataFolderListFolder item in fd.FolderList.Folder)
                        {
                            if (item.ID.Equals(FolderID))
                            {
                                FolderName = item.Name;
                                break;
                            }

                        }
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        txtName.Text = FolderName;
                    }));
                });
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
                    GetUploadUserFile.AsyncPOST(UserID, UserPWD, FolderID, FileID, "del", (fd) => {
                        this.Dispatcher.BeginInvoke(new Action(() => {
                            if (fd != null && fd.Status != null && fd.Status.Success != null && fd.Status.Success.Equals("Y"))
                            {
                                AutoClosingMessageBox.Show("刪除成功");
                                MSCE.ExecuteNonQuery("update userfile set folderid='' where userid=@1 and fileid=@2"
                                    , UserID
                                    , UserPWD
                                    , FileID);
                                   
                                this.DialogResult = true;
                                this.Close();
                            }
                            else
                            {
                                AutoClosingMessageBox.Show("新增失敗");
                            }
                        }));

                    });

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
            txtName.Text = FolderName;
            //txtQuest.Text = string.Format("請輸入{0}識別碼", PaperLess_Emeeting.Properties.Settings.Default.CourseOrMeeting_String);

        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Grid_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            if (ReConfirm)
            {
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show($"確定刪除「{FolderName}」資料夾及所有檔案資料？", "刪除 資料夾", System.Windows.MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                    CallWS();
            }
            else
            {
                CallWS();
            }

            //var win = new NotificationWindow();
            //win.Show();

            e.Handled = true;
        }
    }
}
