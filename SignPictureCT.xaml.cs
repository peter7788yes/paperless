using PaperLess_Emeeting.App_Code.MessageBox;
using PaperLess_ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PaperLess_Emeeting
{
    public delegate void Home_ChangeTogSignPadCT_Function(string UserID,string UserName);
    public delegate void Home_GoBackTogSignPictureCT_Function(string DeptID, string PicUrl);
    /// <summary>
    /// SignPictureCT.xaml 的互動邏輯
    /// </summary>
    public partial class SignPictureCT : UserControl
    {
        public event Home_ChangeTogSignPadCT_Function Home_ChangeTogSignPadCT_Event;
        public event Home_GoBackTogSignPictureCT_Function Home_GoBackTogSignPictureCT_Event;
        public string MeetingID { get; set; }

        public SignPictureCT(Home_ChangeTogSignPadCT_Function callback1, Home_GoBackTogSignPictureCT_Function callback2)
        {
            MouseTool.ShowLoading();
            InitializeComponent();
            this.Home_ChangeTogSignPadCT_Event += callback1;
            this.Home_GoBackTogSignPictureCT_Event += callback2;
            this.Loaded += SignPictureCT_Loaded;
            //MouseTool.ShowArrow();
        }

        private void SignPictureCT_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                InitSelectDB();
                // 只要是 CT 主要畫面，優先權設定為Send，因為設定Normal，按鈕的出現會感覺卡卡的。
                this.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
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
            DataTable dt = MSCE.GetDataTable("select MeetingID from NowLogin");
            if (dt.Rows.Count > 0)
            {
                MeetingID = dt.Rows[0]["MeetingID"].ToString();
            }

        }

        private void InitEvent()
        {
        }

        private void InitUI()
        {

            MouseTool.ShowLoading();
            // 非同步POST方法
            GetSigninData.AsyncPOST(MeetingID, (sid) => { GetSigninData_DoAction(sid); });
                   //, (sid) => { this.Dispatcher.BeginInvoke(new Action<SigninData>(GetSigninData_DoAction), sid); });
            
            
            #region 同步POST
            //SigninData signData = GetSigninData.POST(MeetingID);
            //if (signData != null)
            //{
            //    foreach (SigninDataUser item in signData.UserList)
            //    {
            //        SignRoom sg = new SignRoom(item, this.Home_ChangeTogSignPadCT_Event, this.Home_GoBackTogSignPictureCT_Event);
            //        SignRoomWP.Children.Add(sg);
            //    }
            //}
            //else
            //{
            //    AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
            //}
            #endregion
        }

        private void GetSigninData_DoAction(SigninData sid)
        {
              //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                this.Dispatcher.BeginInvoke(new Action<SigninData>(GetSigninData_DoAction), sid);
            }
            else
            {
                if (sid != null)
                {
                    Task.Factory.StartNew(() =>
                      {
                           this.Dispatcher.BeginInvoke(new Action(() =>
                               {
                                      foreach (SigninDataUser item in sid.UserList)
                                      {
                                          //SignRoom sg = new SignRoom(item, this.Home_ChangeTogSignPadCT_Event, this.Home_GoBackTogSignPictureCT_Event);
                                          SignRoom sg = new SignRoom(item, this.Home_ChangeTogSignPadCT_Event);
                                          SignRoomWP.Children.Add(sg);
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
    }
}
