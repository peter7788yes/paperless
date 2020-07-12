using PaperlessSync.Broadcast.Service;
using PaperlessSync.Broadcast.Socket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PaperLess_Emeeting.App_Code.Socket
{
    public class Singleton_Socket
    {
         // 多執行緒，lock 使用
        private static readonly object thisLock = new object();
        
        // 將唯一實例設為 private static
        private static  volatile SocketClient instance;
 
        public int test = 1;

        public static string MeetingID { get; set; }
        public static string UserID { get; set; }
        public static string UserName { get; set; }

        public static Thread thread { get; set; }

        public static IEventManager ReaderEvent = default(IEventManager);

        public static List<IEventManager> OpenEventList= new List<IEventManager>();
        public static List<IEventManager> CloseEventList = new List<IEventManager>();

        public static BroadcastCT_OpenIEventManager broadcastCT_OpenIEventManager = new BroadcastCT_OpenIEventManager();
        public static BroadcastCT_CloseIEventManager broadcastCT_CloseIEventManager = new BroadcastCT_CloseIEventManager();

        public static Home_OpenIEventManager home_OpenIEventManager = new Home_OpenIEventManager();
        public static Home_CloseIEventManager home_CloseIEventManager = new Home_CloseIEventManager();

        public static MVWindow_OpenIEventManager mvWindow_OpenIEventManager = new MVWindow_OpenIEventManager();

        // 設為 private，外界不能 new
        // 重要
        private Singleton_Socket()
        {
           
          
        }

        public static void Init()
        {
            OpenEventList.Add(broadcastCT_OpenIEventManager);
            CloseEventList.Add(broadcastCT_CloseIEventManager);

            OpenEventList.Add(home_OpenIEventManager);
            CloseEventList.Add(home_CloseIEventManager);

            OpenEventList.Add(mvWindow_OpenIEventManager);
        }
 
        // 外界只能使用靜態方法取得實例
        public static SocketClient GetInstance()
        {
            return instance;
        }

        // 外界只能使用靜態方法取得實例
        public static SocketClient GetInstance(string _MeetingID, string _UserID, string _UserName ,bool InitToSync)
        {
            
            bool SyncServerIsIP=false;

            IPAddress ip =default(IPAddress);
            IPAddress.TryParse(new Uri(WsTool.GetSyncServer_URL()).DnsSafeHost, out ip);
            if (IPAddress.Equals(default(IPAddress),ip) == false)
            {
                SyncServerIsIP = true;
            }



            if (SyncServerIsIP==false && NetworkTool.GetDomainNameIP(WsTool.GetSyncServer_URL(), 1000).Equals("") == true && NetworkTool.GetDomainNameIP(WsTool.GetSyncServerImp_URL(), 1000).Equals("") == true)
            {
                return null;
            }

            // 先判斷目前有沒有實例，沒有的話才開始 lock，
            // 此次的判斷，是避免在有實例的情況，也執行 lock ，影響效能
            if (instance == null)
            {
                // 避免多執行緒可能會產生兩個以上的實例，所以 lock
                lock (thisLock)
                {
                    // lock 後，再判斷一次目前有無實例
                    // 此次的判斷，是避免多執行緒，同時通過前一次的 null == instance 判斷
                    if (instance == null)
                    {
                        Init_Instance(_MeetingID, _UserID, _UserName, InitToSync);
                    }
                    else
                    {
                        if (MeetingID.Equals(_MeetingID) == false)
                        {
                            Init_Instance(_MeetingID, _UserID, _UserName, InitToSync);
                        }
                        else
                        {

                            int i = 1;
                            while (i <= 10)
                            {
                                bool IsConnected = instance.GetIsConnected();
                                if (instance != null && IsConnected == true)
                                {
                                    break;
                                }
                                else if (i == 10)
                                {
                                    Init_Instance(_MeetingID, _UserID, _UserName, InitToSync);
                                }

                                Thread.Sleep(10);
                                i++;
                            }
                           
                        }
                        
                    }
                }
            }
            else
            {
                lock (thisLock)
                {
                    // lock 後，再判斷一次目前有無實例
                    // 此次的判斷，是避免多執行緒，同時通過前一次的 null == instance 判斷
                    if (instance == null)
                    {
                        Init_Instance(_MeetingID, _UserID, _UserName, InitToSync);
                    }
                    else
                    {
                        if (MeetingID.Equals(_MeetingID) == false)
                        {
                            Init_Instance(_MeetingID, _UserID, _UserName, InitToSync);
                        }
                        else
                        {
                            //if (instance.GetIsConnected() == false)
                            //    Init_Instance(_MeetingID, _UserID, _UserName, InitToSync);

                            int i = 1;
                            while (i <= 10)
                            {
                                bool IsConnected = instance.GetIsConnected();
                                if (instance != null && IsConnected == true)
                                {
                                    break;
                                }
                                else if (i == 10)
                                {
                                    Init_Instance(_MeetingID, _UserID, _UserName, InitToSync);
                                }

                                Thread.Sleep(10);
                                i++;
                            }
                        }
                    }
                }
            }

            //Thread.Sleep(100);
            return instance;
        }

        private static void Init_Instance(string _MeetingID, string _UserID, string _UserName, bool InitToSync)
        {
            UserName = _UserName;
            MeetingID = _MeetingID;
            _UserID = Socket_FixEmailUserID.ToSocket(_UserID);
            UserID = _UserID;
            ClearInstance();
            //if (thread != null)
            //{
            //    thread.Abort();
            //}
            //if (instance != null)
            //{
            //    instance = null;
            //}

            // new SocketClient(syncServerUrl, meetingId, maxClient, clientId, clientName, isSync, clientType, func, true);
            // clientType 2是PC
            string SyncServerUrl = SocketTool.GetUrl();
            string SyncServerUrl_Imp = SocketTool.GetUrl_Imp();
            if (_MeetingID.ToLower().StartsWith("i") == true)
            {
                SyncServerUrl = SyncServerUrl_Imp;
            }
           instance = new SocketClient(SyncServerUrl + "/JoinSyncServer", _MeetingID, 100, _UserID, _UserName, InitToSync, 2, "12");
           if (ReaderEvent!=null)
                instance.AddEventManager(ReaderEvent);
           instance.AddEventManager(OpenEventList);
           instance.AddCloseEventManager(CloseEventList);


           thread = new Thread(delegate()
            {
                try
                {
                    //run()方法是無窮迴圈，所以要開Thread去跑
                    if (instance!=null)
                        instance.run();
                }
                catch (Exception ex)
                {
                    LogTool.Debug(ex);
                }
            });
            thread.IsBackground = true;
            thread.Start();

            // 延遲一下，等待IEventManager.run 開始跑。
            //Thread.Sleep(100);

            int i = 0;
            while (i<10)
            {
                if (instance!=null && instance.GetIsConnected() == true)
                {
                    return;
                }

                Thread.Sleep(10);

                i++;
            }

            if (instance == null || instance.GetIsConnected() == false)
                instance = null;
        }


        public static void ClearInstance()
        {
            try
            {
                //20150421
                //不要Abort因為Line:188~189
                //直接instance=null
                //Thread就會結束。
                //if (thread != null)
                //{
                //    thread.Abort();
                //}
                //instance = null;
                if (thread != null)
                {
                    thread.Abort();
                    //thread = null;
                }
                
            }
            catch(Exception ex)
            {
                LogTool.Debug(ex);
            }
            Thread.Sleep(10);
            thread = null;
            instance = null;
        }



        //public static void Reset()
        //{
        //    if (thread != null)
        //    {
        //        thread.Abort();
        //    }
        //    instance = null;

        //    lock (thisLock)
        //    {
        //        OpenEventList.Clear();
        //        CloseEventList.Clear();
        //    }
        //}

     
    }
}
