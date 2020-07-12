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
    
   

    /// <summary>
    /// SignListCT.xaml 的互動邏輯
    /// </summary>
    public partial class SignListCT : UserControl
    {
        public string UserID { get; set; }
        public string UserName { get; set; }
        public string UserPWD { get; set; }
        public string MeetingID { get; set; }
        public SignListCT()
        {
            MouseTool.ShowLoading();
            InitializeComponent();
            this.Loaded += SignListCT_Loaded;
            //MouseTool.ShowArrow();
        }

        private void SignListCT_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                InitSelectDB();
                // 只要是 CT 主要畫面，優先權設定為Send，因為設定Normal，按鈕的出現會感覺卡卡的。
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

        private void InitEvent()
        {
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

        private void InitUI()
        {

            MouseTool.ShowLoading();
            // 非同步POST方法
            GetSigninData.AsyncPOST(MeetingID, (sid) => { GetSigninData_DoAction(sid); });
                   //, (sid) => { this.Dispatcher.BeginInvoke(new Action<SigninData>(GetSigninData_DoAction), sid); });

            #region 同步POST
            //SigninData sid=GetSigninData.POST(MeetingID);
            //if (sid != null)
            //{
            //    foreach (SigninDataUser item in sid.UserList)
            //    {
            //        SignRowSP.Children.Add(new SignRow(item));
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
                                      SignRowSP.Children.Add(new SignRow(item));
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
