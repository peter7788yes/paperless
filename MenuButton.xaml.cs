using Newtonsoft.Json;
using PaperLess_Emeeting.App_Code.MessageBox;
using PaperLess_Emeeting.App_Code.Socket;
using PaperLess_ViewModel;
using PaperlessSync.Broadcast.Service;
using PaperlessSync.Broadcast.Socket;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
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
    public delegate Tuple<bool,bool> Home_ReturnSyncStatus_Function();
    //public delegate void Home_ChangeCC_Function(string ButtonID);
    public delegate void Home_ChangeCC_Function(UserButton userButton);
    public delegate void Home_ChangeBtnSP_Function(UserButton[] UserButtonAry, string ActiveButtonID);
    public delegate void Home_PopUpButtons_Function(string ButtonID);

    /// <summary>
    /// Menu.xaml 的互動邏輯
    /// </summary>
    public partial class MenuButton : UserControl
    {
        public event Home_ReturnSyncStatus_Function Home_ReturnSyncStatus_Event;
        public event Home_ChangeCC_Function Home_ChangeCC_Event;
        public event Home_ChangeBtnSP_Function Home_ChangeBtnSP_Event;
        public event Home_PopUpButtons_Function Home_PopUpButtons_Event;

        public UserButton userButton { get; set; }
        public string UserID { get; set; }
        public string UserName { get; set; }
        public string MeetingID { get; set; }
        public string AllowIpRange { get; set; }

        // 按鈕不要非同步載入，不然會被延後載入的按鈕
        // 把按鈕變成彈起的狀態。
        public MenuButton(UserButton userButton, Home_ChangeCC_Function callback1 ,Home_PopUpButtons_Function callback2 ,Home_ChangeBtnSP_Function callback3 = null)
        {
            //MouseTool.ShowLoading();
            InitializeComponent();
            this.Home_ChangeCC_Event += callback1;
            this.Home_PopUpButtons_Event += callback2;
            this.Home_ChangeBtnSP_Event += callback3;
            this.userButton = userButton;
            this.Loaded += MenuButton_Loaded;
            this.Unloaded += MenuButton_Unloaded;
            InitSelectDB();
            InitUI();
            InitEvent();
            //MouseTool.ShowArrow();
        }

        private void MenuButton_Unloaded(object sender, RoutedEventArgs e)
        {
           
        }

        private void MenuButton_Loaded(object sender, RoutedEventArgs e)
        {
            //InitSelectDB();

            ////這裡為 按鈕 畫面，優先權設定為Normal => 列舉值為 9。 一般優先權處理作業。 這是一般的應用程式的優先順序。
            //Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            //{
            //    InitUI();
            //    InitEvent();
            //}));
        }
        
        private void InitSelectDB()
        {
            if (userButton.ID.Equals("BtnSync") || userButton.ID.Equals("BtnIndividualSign") || userButton.ID.Equals("BtnBroadcast"))
            {
                DataTable dt = MSCE.GetDataTable("select UserID,UserName,UserPWD,MeetingID,AllowIpRange from NowLogin");
                if (dt.Rows.Count > 0)
                {
                    UserID = dt.Rows[0]["UserID"].ToString();
                    UserName = dt.Rows[0]["UserName"].ToString();
                    MeetingID = dt.Rows[0]["MeetingID"].ToString();
                    AllowIpRange = dt.Rows[0]["AllowIpRange"].ToString();
                }
            }

        }

        private void InitEvent()
        {
            this.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            this.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            this.MouseLeftButtonDown += UserControl_MouseLeftButtonDown;
        }

        private void InitUI()
        {
            btnImg.Source = ButtonTool.GetButtonImage(userButton.ID);
            txtBtnName.Text = userButton.Name;

            if (userButton.ID.Equals("BtnSync"))
            {
                this.Height = 55;
                this.Width = 60;
                btnImg.Height = 55;
                btnImg.Width = 60;
               
                txtBtnName.Text = "";
                //InitializeComponent();
            }
        }

        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            if (userButton.ID.Equals("BtnSync"))
            {
                if (PaperLess_Emeeting.Properties.Settings.Default.HasIpRangeMode==true && AllowIpRange.Equals("") == false && IpTool.CheckInNowWifi(AllowIpRange) == false)
                {
                    string CourseOrMeeting_String = PaperLess_Emeeting.Properties.Settings.Default.CourseOrMeeting_String;
                    AutoClosingMessageBox.Show( string.Format("您不在{0}室範圍內，無法使用此功能",CourseOrMeeting_String.Equals("課程")?"教":CourseOrMeeting_String ));
                    return;
                }

                if (Home_ReturnSyncStatus_Event != null)
                {
                    Tuple<bool, bool> SyncStatus = Home_ReturnSyncStatus_Event();

                    bool syncSwitch = false;
                    // 沒同步，按下去要變成同步且不是主控
                    if (SyncStatus.Item1 == false)
                    {
                        int FileNotFinished = 0;
                        DataTable dt = MSCE.GetDataTable(@"select count(ID) as FileNotFinished from NowLogin as nl
                                                       inner join FileRow as fr on nl.UserID=fr.UserID and nl.MeetingID=fr.MeetingID
                                                       where DownloadBytes=0 or DownloadBytes<TotalBytes");
                        if (dt.Rows.Count > 0)
                        {
                            FileNotFinished = (int)dt.Rows[0]["FileNotFinished"];
                        }
                        if (FileNotFinished > 0)
                        {
                            AutoClosingMessageBox.Show(string.Format("請將{0}資料下載完成後，再同步", PaperLess_Emeeting.Properties.Settings.Default.CourseOrMeeting_String));
                            return;
                        }
                        syncSwitch = true;
                    }
                    else  //有同步，不是主控，按下去要變成沒有同步
                    {
                        syncSwitch = false;
                    }
                    btnImg.Source = ButtonTool.GetSyncButtonImage(SyncStatus.Item1, SyncStatus.Item2);

                    //string UserID = "";
                    //string UserName = "";
                    //string MeetingID = "";
                    //DataTable dt = MSCE.GetDataTable("select UserID,UserName,UserPWD,MeetingID from NowLogin");
                    //if (dt.Rows.Count > 0)
                    //{
                    //    UserID = dt.Rows[0]["UserID"].ToString();
                    //    UserName = dt.Rows[0]["UserName"].ToString();
                    //    MeetingID = dt.Rows[0]["MeetingID"].ToString();
                    //}

                   
                    Task.Factory.StartNew(() =>
                    {
                       

                        AutoClosingMessageBox.Show("連線中");
                        int i = 1;
                        while (i <= 10)
                        {
                            SocketClient socketClient = Singleton_Socket.GetInstance(MeetingID, UserID, UserName, syncSwitch);
                            Thread.Sleep(1);
                            if (socketClient != null && socketClient.GetIsConnected() == true)
                            {
                                socketClient.syncSwitch(syncSwitch);
                                break;
                            }
                            else
                            {
                                Singleton_Socket.ClearInstance();
                                if (i == 10)
                                {
                                    AutoClosingMessageBox.Show("同步伺服器尚未啟動，請聯絡議事管理員開啟同步");
                                }
                               
                            }

                            Thread.Sleep(10);
                            i++;
                        }
                            
                    });

                }
            }
            else 
            {
                if (userButton.ID.Equals("BtnIndividualSign") || userButton.ID.Equals("BtnBroadcast"))
                {
                    if (PaperLess_Emeeting.Properties.Settings.Default.HasIpRangeMode == true && AllowIpRange.Equals("") == false && IpTool.CheckInNowWifi(AllowIpRange) == false)
                    {
                        string CourseOrMeeting_String = PaperLess_Emeeting.Properties.Settings.Default.CourseOrMeeting_String;
                        AutoClosingMessageBox.Show(string.Format("您不在{0}室範圍內，無法使用此功能", CourseOrMeeting_String.Equals("課程") ? "教" : CourseOrMeeting_String));
                        //AutoClosingMessageBox.Show("您不在會議室範圍內，無法使用此功能");
                        return;
                    }
                }

                btnImg.Source = ButtonTool.GetButtonImage(userButton.ID, true);

                if (Home_PopUpButtons_Event != null)
                {
                    Home_PopUpButtons_Event(userButton.ID);
                }   

            }


           
               

            //if(userButton.ID.Equals("BtnQuit")==true)
            //{
            //        DataTable dt = MSCE.GetDataTable("select HomeUserButtonAryJSON from NowLogin");
            //        if (dt.Rows.Count > 0)
            //        {
            //           string HomeUserButtonAryJSON = dt.Rows[0]["HomeUserButtonAryJSON"].ToString();
            //           Task.Factory.StartNew(() =>
            //               {
            //                   Home_ChangeBtnSP_Event(JsonConvert.DeserializeObject<UserButton[]>(HomeUserButtonAryJSON), "BtnHome");
            //               });
                       
            //        }
            //}
            
            //改變按鈕列表
            Task.Factory.StartNew(() =>
                          {
                              Home_ChangeCC_Event(userButton);
                          });
        }

      
    }


  
}
