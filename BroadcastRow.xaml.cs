using PaperLess_Emeeting.App_Code.MessageBox;
using PaperLess_Emeeting.App_Code.Socket;
using PaperlessSync.Broadcast.Socket;
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

namespace PaperLess_Emeeting
{
    /// <summary>
    /// BroadcastRow.xaml 的互動邏輯
    /// </summary>
    public partial class BroadcastRow : UserControl
    {
        public string clientId { get; set; }
        public string clientName { get; set; }
        public string clientType { get; set; }
        public string status { get; set; }
        public string UserID { get; set; }
        public string UserName { get; set; }
        public string MeetingID { get; set; }
        // 帳號,姓名,裝置,燈號
        public BroadcastRow(string clientId, string clientName, string clientType,string status,string UserID,string UserName,string MeetingID)
        {
            //MouseTool.ShowLoading();
            InitializeComponent();
            this.clientId = clientId;
            this.clientName = clientName;
            this.clientType = clientType;
            this.status = status;
            this.UserID = UserID;
            this.UserName = UserName;
            this.MeetingID = MeetingID;
            this.Loaded += BroadcastRow_Loaded;
            //MouseTool.ShowArrow();
        }

        private void BroadcastRow_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                InitSelectDB();
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    InitUI();
                    InitEvent();
                }));
               
            });
           
        }


        private void InitSelectDB()
        {
            //DataTable dt = MSCE.GetDataTable("select UserID,UserName,UserPWD,MeetingID from NowLogin");
            //if (dt.Rows.Count > 0)
            //{
            //    UserID = dt.Rows[0]["UserID"].ToString();
            //    UserName = dt.Rows[0]["UserName"].ToString();
            //    MeetingID = dt.Rows[0]["MeetingID"].ToString();
            //}
        }

        private void InitEvent()
        {
            LightGrid.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            LightGrid.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            LightGrid.MouseLeftButtonDown += LightGrid_MouseLeftButtonDown;
        }

        private void LightGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            string SyncOwnerID = "";
            if (txtLight.Text.Equals("關"))
                SyncOwnerID = clientId;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    SocketClient socketClient = Singleton_Socket.GetInstance(MeetingID, UserID, UserName,false);
                    if (socketClient != null && socketClient.GetIsConnected() == true)
                    {
                        socketClient.setSyncOwner(Socket_FixEmailUserID.ToSocket(SyncOwnerID));
                        return;
                    }
                    else
                    {
                        //AutoClosingMessageBox.Show("同步伺服器尚未啟動，請聯絡議事管理員開啟同步");
                        return;
                    }

                }
                catch (Exception ex)
                {
                    LogTool.Debug(ex);
                }
            });
           

        }

        private void InitUI()
        {
            txtUserID.Text = clientId;
            txtUserName.Text = clientName;

            switch (clientType)
            {
                case "1":
                    txtUserDevice.Text = "Android";
                    break;
                case "2":
                    txtUserDevice.Text = "PC";
                    break;
                default:
                    txtUserDevice.Text = "iOS";
                    break;

            }

            if (status.Equals("-1"))
            {
                btnLight.Source =new BitmapImage(new Uri("images/btn_On@2x.png",UriKind.Relative));
                txtLight.Text="開";
            }
            else
            {
                btnLight.Source = new BitmapImage(new Uri("images/btn_Off@2x.png", UriKind.Relative));
                txtLight.Text="關";
            }
            LightGrid.Visibility = Visibility.Visible;
        }

       
    }
}
