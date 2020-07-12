using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaperLess_Emeeting.App_Code.MessageBox;
using PaperLess_Emeeting.App_Code.Socket;
using PaperlessSync.Broadcast.Service;
using PaperlessSync.Broadcast.Socket;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
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
using System.Xml;
using System.Xml.Linq;

namespace PaperLess_Emeeting
{
    /// <summary>
    /// BroadcastCT.xaml 的互動邏輯
    /// </summary>
    public partial class BroadcastCT : UserControl
    {
        public string UserID { get; set; }
        public string UserName { get; set; }
        public string MeetingID { get; set; }
        public BroadcastCT_ChangeList_Function BroadcastCT_ChangeList_Callback;
        public BroadcastCT_ClearList_Function BroadcastCT_ClearList_Callback;
        public CancellationTokenSource tokenSource { get;set; }

        public bool CanDetectServerState = true;

        public DispatcherTimer dTimer {get;set;}
        public BroadcastCT()
        {
           
            MouseTool.ShowLoading();
            InitializeComponent();
            tokenSource = new CancellationTokenSource();
            this.Loaded += BroadcastCT_Loaded;
            this.Unloaded += BroadcastCT_Unloaded;
            
        }

        private void BroadcastCT_Unloaded(object sender, RoutedEventArgs e)
        {
            Singleton_Socket.broadcastCT_OpenIEventManager.BroadcastCT_ChangeList_Event -= BroadcastCT_ChangeList_Callback;
            Singleton_Socket.broadcastCT_CloseIEventManager.BroadcastCT_ClearList_Event -= BroadcastCT_ClearList_Callback;
            
            //if (tokenSource != null)
            //    tokenSource.Cancel();
            CanDetectServerState = false;
        }

        private void BroadcastCT_Loaded(object sender, RoutedEventArgs e)
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

        private void InitSelectDB()
        {
            DataTable dt = MSCE.GetDataTable("select UserID,UserName,UserPWD,MeetingID from NowLogin");
            if (dt.Rows.Count > 0)
            {
                UserID = dt.Rows[0]["UserID"].ToString();
                UserName = dt.Rows[0]["UserName"].ToString();
                MeetingID = dt.Rows[0]["MeetingID"].ToString();
            }
        }

        private void InitEvent()
        {
            
            txtKeyword.MouseEnter += (sender, e) => { MouseTool.ShowIBeam(); txtKeyword.Focus(); };
            txtKeyword.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); }; //Keyboard.ClearFocus();
            txtKeyword.KeyUp += txtKeyword_KeyUp;
            txtKeyword.Focus();

            btnServerCtrl.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnServerCtrl.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnServerCtrl.MouseLeftButtonDown +=btnServerCtrl_MouseLeftButtonDown;

            BroadcastCT_ChangeList_Callback = new BroadcastCT_ChangeList_Function(ChangeList);
            BroadcastCT_ClearList_Callback = new BroadcastCT_ClearList_Function(ClearList);

            Singleton_Socket.broadcastCT_OpenIEventManager.BroadcastCT_ChangeList_Event += BroadcastCT_ChangeList_Callback;
            Singleton_Socket.broadcastCT_CloseIEventManager.BroadcastCT_ClearList_Event += BroadcastCT_ClearList_Callback;

            Task.Factory.StartNew(() => { return SyncServerAlreadyStarted(MeetingID); }).ContinueWith(task =>
            {
                try
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ChangeServerCtrl(task.Result);
                    }));

                    if (task.Result == false)
                    {
                        //AutoClosingMessageBox.Show("同步伺服器尚未啟動，請聯絡議事管理員開啟同步");
                        return;
                    }

                    try
                    {
                        SocketClient socketClient = Singleton_Socket.GetInstance(MeetingID, UserID, UserName,false);
                        Task.Factory.StartNew(() =>
                        {
                            if (socketClient != null && socketClient.GetIsConnected() == true)
                            {
                                socketClient.getUserList();
                            }
                            else
                            {
                               // AutoClosingMessageBox.Show("同步伺服器尚未啟動，請聯絡議事管理員開啟同步");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        LogTool.Debug(ex);
                    }
                    
                }
                catch (Exception ex)
                {
                    //AutoClosingMessageBox.Show("同步伺服器尚未啟動，請聯絡議事管理員開啟同步");
                    LogTool.Debug(ex);
                }

            }).ContinueWith(task => {

                while (CanDetectServerState)
                {
                    bool inList = SyncServerAlreadyStarted(MeetingID);
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ChangeServerCtrl(inList);
                    }));

                    SocketClient socketClient = Singleton_Socket.GetInstance(MeetingID, UserID, UserName,false);
                    //20150708 Add 
                    Task.Factory.StartNew(() =>
                    {
                        if (socketClient != null && socketClient.GetIsConnected() == true)
                        {
                            socketClient.getUserList();
                        }
                        else
                        {
                            //AutoClosingMessageBox.Show("同步伺服器尚未啟動，請聯絡議事管理員開啟同步");
                        }
                    });
                    //socketClient.getUserList();
                    Console.WriteLine("Detect Server Alive => IsCompleted: {0} IsCanceled: {1} IsFaulted: {2}",
                                      task.IsCompleted, task.IsCanceled, task.IsFaulted);

                    Thread.Sleep(1000* PaperLess_Emeeting.Properties.Settings.Default.DetectSyncServerSeconds);
                }

            }, tokenSource.Token); 
         
        }

        private void txtKeyword_KeyUp(object sender, KeyEventArgs e)
        {
            string keyword = txtKeyword.Text.ToLower().Trim();

            if (keyword.Equals("") == false)
            {
                foreach (BroadcastRow item in BroadcastRowSP.Children.OfType<BroadcastRow>())
                {
                    if (item.clientId.Contains(keyword) == true || item.clientName.Contains(keyword) == true
                        || item.txtLight.Text.Contains(keyword) == true || item.txtUserDevice.Text.Contains(keyword) == true)
                    {
                        item.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        item.Visibility = Visibility.Collapsed;
                    }

                };
            }
            else
            {
                foreach (BroadcastRow item in BroadcastRowSP.Children.OfType<BroadcastRow>())
                {
                   item.Visibility = Visibility.Visible;
                };
            }
           
        }

        private void ClearList()
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action(ClearList));
                this.Dispatcher.BeginInvoke(new Action(ClearList));
            }
            else
            {
                BroadcastRowSP.Children.Clear();
            }
        }


        private void ChangeServerCtrl(bool Online)
        {
            if (Online == true)
            {
                txtStatus.Text = "連線中";
                txtStatus.Foreground = ColorTool.HexColorToBrush("#E2F540");
                txtStatus.HorizontalAlignment = HorizontalAlignment.Left;
                btnStatus.Source = new BitmapImage(new Uri("images/btn_broadcast_connected.png", UriKind.Relative));
            }
            else
            {
                txtStatus.Text = "未啟動";
                txtStatus.Foreground = ColorTool.HexColorToBrush("#707A82");
                txtStatus.HorizontalAlignment = HorizontalAlignment.Center;
                btnStatus.Source = new BitmapImage(new Uri("images/btn_broadcast_broken.png", UriKind.Relative));
            }
        }

        

        private void btnServerCtrl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            // 做啟動Server動作
            if (txtStatus.Text.Equals("未啟動") == true)
            {

                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        bool success = StartSyncServer(MeetingID);

                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            ChangeServerCtrl(success);
                        }));
                        if (success == true)
                        {
                            AutoClosingMessageBox.Show("啟動成功");
                            try
                            {

                                SocketClient socketClient = Singleton_Socket.GetInstance(MeetingID, UserID, UserName, false);
                                Task.Factory.StartNew(() =>
                                {
                                    if (socketClient != null && socketClient.GetIsConnected() == true)
                                    {
                                        socketClient.getUserList();
                                    }
                                    else
                                    {
                                        //AutoClosingMessageBox.Show("同步伺服器尚未啟動，請聯絡議事管理員開啟同步");
                                    }
                                });

                            }
                            catch (Exception ex)
                            {
                                LogTool.Debug(ex);
                            }

                        }
                        else
                        {
                            AutoClosingMessageBox.Show("啟動失敗");
                        }
                    }
                    catch(Exception ex)
                    {
                        LogTool.Debug(ex);
                    }
                   
                });
            }
            else // 做停止Server動作
            {
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        bool success = StopSyncServer(MeetingID);
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            ChangeServerCtrl(!success);
                        }));
                        if (success == true)
                        {
                            AutoClosingMessageBox.Show("停止成功");
                            this.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                BroadcastRowSP.Children.Clear();
                            }));
                        }
                        else
                        {
                            AutoClosingMessageBox.Show("停止失敗");
                        }
                    }
                    catch(Exception ex)
                    {
                        LogTool.Debug(ex);
                    }
                   
                });
            }
        }

        public void ChangeList(JArray jArry)
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action<JArray>(ChangeList), jArry);
                this.Dispatcher.BeginInvoke(new Action<JArray>(ChangeList), jArry);
            }
            else
            {
                BroadcastRowSP.Children.Clear();
                foreach (JToken item in jArry)
                {
                    // [{\"clientId\":\"kat\",\"clientName\":\"kat\",\"clientType\":1,\"status\":0,\
                    Dictionary<string, object> dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(item.ToString());
                    string clientId = dict["clientId"].ToString();
                    string clientName = dict["clientName"].ToString();
                    string clientType = dict["clientType"].ToString();
                    string status = dict["status"].ToString();
                    if(status.Equals("1")==false) //沒有再同步的時候
                        BroadcastRowSP.Children.Add(new BroadcastRow(Socket_FixEmailUserID.FromSocket(clientId), clientName, clientType, status
                                                                   ,UserID,UserName,MeetingID));
                }
            }
                
        }

        private void InitUI()
        {

        }


        public bool StopSyncServer(string meetingID)
        {
            bool rtn = false;
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("<?xml version=\"1.0\" encoding=\"UTF-8\"?>")
                  .AppendFormat("<Sync>")
                  .AppendFormat("<Stop ID=\"{0}\" />",MeetingID)
                  .AppendFormat("</Sync>");

                if (PostToSyncServer("/StopSyncServer", sb.ToString()).Contains("成功"))
                    rtn = true;
            }
            catch (Exception ex)
            {
                rtn = false;
                LogTool.Debug(ex);
            }

            return rtn;
        }


        public bool SyncServerAlreadyStarted(string meetingID)
        {
            bool rtn = false;
            try
            {
                 StringBuilder sb = new StringBuilder();
                 sb.AppendFormat("<?xml version=\"1.0\" encoding=\"UTF-8\"?>")
                   .AppendFormat("<MeetingList date=\"{0}\" >", DateTime.Now.ToString("yyyyMMddHHmmss"))
                   .AppendFormat("</MeetingList>");

                 XDocument xml = XDocument.Parse(PostToSyncServer("/GetMeetingList", sb.ToString()));
                 var q = from x in xml.Element("MeetingList").Elements("Meeting")
                         select new
                         {
                             ID = x.Attribute("ID").Value.Trim(),
                         };
                 foreach (var item in q)
                 {
                     if (item.ID.Equals(meetingID) == true)
                         return true;
                 }
               
            }
            catch(Exception ex)
            {
                rtn = false;
                LogTool.Debug(ex);
            }

            return rtn;
        }

        private string PostToSyncServer(string subUrl,string sentXml )
        {
            string getXml = "";
            try
            {
                string SyncServerUrl = SocketTool.GetUrl();
                string SyncServerUrl_Imp = SocketTool.GetUrl_Imp();
                if (MeetingID.ToLower().StartsWith("i") == true)
                {
                    SyncServerUrl = SyncServerUrl_Imp;
                }
                
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(SyncServerUrl + subUrl);
                string data = sentXml;
                byte[] postData = Encoding.UTF8.GetBytes(data);

                request.Method = "POST";
                request.ContentType = "text/xml; encoding='utf-8'";
                request.ContentLength = postData.Length;

                Stream dataStream = request.GetRequestStream();
                dataStream.Write(postData, 0, postData.Length);
                dataStream.Close();

                WebResponse response = request.GetResponse();
                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                getXml = reader.ReadToEnd();
                reader.Close();
                dataStream.Close();
                response.Close();
            }
            catch(Exception ex)
            {
                LogTool.Debug(ex);
            }

            return getXml;
        }

        public  bool StartSyncServer(string meetingID)
        {
            bool rtn = false;
            //string getXml = "";

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>")
                   .Append("<Sync>")
                   .AppendFormat("<Start ID=\"{0}\" MaxClient=\"{1}\" />", meetingID, 100)
                   .AppendFormat("<Init>{0}</Init>", PaperLess_Emeeting.Properties.Settings.Default.InitConfig)
                   .Append("</Sync>");
      
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(PostToSyncServer("/StartSyncServer", sb.ToString()));
                XmlNode root = doc.DocumentElement;
                string ip = root.SelectSingleNode("/Sync/Start/@IP").Value;
                int port = int.Parse(root.SelectSingleNode("/Sync/Start/@Port").Value);
                if (ip.Equals("") == false && port >= 1 && port <= 65535)
                    rtn = true;
            }
            catch(Exception ex)
            {
                rtn = false;
                LogTool.Debug(ex);
            }

            return rtn;
        }
    }

    
}
