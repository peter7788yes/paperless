using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaperlessSync.Broadcast.Service;
using PaperlessSync.Broadcast.Socket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PaperLess_Emeeting.App_Code.Socket
{
    //public delegate void Home_R_InitOpenBook_Function(string BookID, string IEventManager_msg);
    public delegate void Home_OpenBook_Function(string BookID,string InitMsg);
    public delegate void Home_IsInSync_And_IsSyncOwner_Function(JArray jArry);
    public delegate void Home_CloseAllWindow_Function(string AlertMessage,bool fromInit);
    public delegate void Home_TurnOffSyncButton_Function();
    public delegate void Home_SetSocketClientNull_Function();

   
    public class Home_OpenIEventManager : IEventManager
    {
        //public Home_R_InitOpenBook_Function Home_R_InitOpenBook_Event;
        public event Home_OpenBook_Function Home_OpenBook_Event;
        public event Home_IsInSync_And_IsSyncOwner_Function Home_IsInSync_And_IsSyncOwner_Event;
        public event Home_CloseAllWindow_Function Home_CloseAllWindow_Event;
         Dictionary<string, string> dictCache = new Dictionary<string, string>(); 
                   
        public Home_OpenIEventManager()
        {
            this._managerId = this.GetType().Name;
        }

        //public Home_OpenIEventManager(Home_IsInSync_And_IsSyncOwner_Function callback1, Home_CloseAllWindow_Function callback2)
        //{
        //    this._managerId = this.GetType().Name;
        //    this.Home_IsInSync_And_IsSyncOwner_Event = callback1;
        //    this.Home_CloseAllWindow_Event = callback2;
        //}

        #region 實作介面屬性
        private string _managerId = typeof(BroadcastCT_OpenIEventManager).Name;
        public string managerId { get { return _managerId; } set { _managerId = value; } }

        private string _msg;
        public string msg { get { return _msg; } set { _msg = value; } }

        private string _clientId;
        public string clientId { get { return _clientId; } set { _clientId = value; } }
        #endregion

        public void run()
        {
            try
            {
                string cacheMsg = msg;
                Dictionary<string, Object> msgJson = new Dictionary<string, object>();
                long reciveTime;
                string cmd = "";


                if (msg == null)
                    return;
                try
                {
                    Console.WriteLine("Home_OpenIEventManager: " + cacheMsg);
                    msgJson = JsonConvert.DeserializeObject<Dictionary<string, Object>>(cacheMsg);
                    reciveTime = (long)(SocketClient.GetCurrentTimeInUnixMillis() - (ulong)((long)msgJson["sendTime"]));
                    LogTool.Debug(string.Format("EventManagerHome[{0}] recive message->{1}", _managerId, cacheMsg));
                    LogTool.Debug(string.Format("EventManagerHome[{0}] recive msg form {1} time = {2} ms", _managerId, clientId, reciveTime));
                }
                catch (Exception ex)
                {
                    LogTool.Debug(ex);
                }

                try
                {
                    cmd = msgJson["cmd"].ToString();
                }
                catch (Exception ex)
                {
                    LogTool.Debug(ex);
                }

                switch (cmd)
                {
                    case "userList":
                        string UserListJsonArray = msgJson["msg"].ToString();
                        //[{\"clientId\":\"kat\",\"clientName\":\"kat\",\"clientType\":1,\"status\":0,\"joinTime\":1370424638670,\"funcCmd\":[\"userList\",\"broadcast\"],\"func\":\"12\"},{\"clientId\":\"mat\",\"clientName\":\"mat\",\"clientType\":1,\"status\":0,\"joinTime\":1370424637583,\"funcCmd\":[\"userList\",\"broadcast\"]
                        JArray jArry = JsonConvert.DeserializeObject<JArray>(UserListJsonArray);
                        if (Home_IsInSync_And_IsSyncOwner_Event != null)
                            Home_IsInSync_And_IsSyncOwner_Event(jArry);
                        break;
                  

                    case "broadcast":

                        string broadcastMessage = msgJson["msg"].ToString();
                        JObject jo = JObject.Parse(broadcastMessage);
                        string innerCmd = jo["cmd"].ToString().ToUpper();

                        switch (innerCmd)
                        {
                            case "R.OB":
                                string bookId = jo["bookId"].ToString();
                                if (bookId != null && bookId.Equals("") == false)
                                {
                                    if (Home_CloseAllWindow_Event != null)
                                        Home_CloseAllWindow_Event("主控者開啟檔案", false);

                                    if (Home_OpenBook_Event != null)
                                        Home_OpenBook_Event(bookId,"");

                                }
                                break;
                            case "R.CB":
                                if (Home_CloseAllWindow_Event != null)
                                    Home_CloseAllWindow_Event("主控者點選離開",false);
                                break;
                       }
                       break;

                    case "R.init":
                       string initMessage = msgJson["msg"].ToString();
                       if (initMessage.Contains("bookId") == false)
                           return;
                       JObject jo_BookID = JObject.Parse(initMessage);
                       string init_bookId = jo_BookID["bookId"].ToString();

                       lock (this)
                       {
                           if (dictCache.ContainsKey(init_bookId) == true)
                           {
                               //return;
                           }
                           else
                           {
                               dictCache[init_bookId] = cacheMsg;
                               Task.Factory.StartNew(() =>
                               {
                                   Thread.Sleep(5000);
                                   dictCache.Clear();
                               });

                               if (Home_CloseAllWindow_Event != null)
                                   Home_CloseAllWindow_Event(string.Format("加入進行中{0}", PaperLess_Emeeting.Properties.Settings.Default.CourseOrMeeting_String), true);

                               if (Home_OpenBook_Event != null)
                                   Home_OpenBook_Event(init_bookId, cacheMsg);
                           }
                       }
                       break;
                }

            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }
        }

    }



    public class Home_CloseIEventManager : IEventManager
    {
        public event Home_CloseAllWindow_Function Home_CloseAllWindow_Event;
        public event Home_TurnOffSyncButton_Function Home_TurnOffSyncButton_Event;
        public event Home_SetSocketClientNull_Function Home_SetSocketClientNull_Event;

        public Home_CloseIEventManager()
        {
            this._managerId = this.GetType().Name;
        }

        //public Home_CloseIEventManager(Home_CloseAllWindow_Function callback1, Home_TurnOffSyncButton_Function callback2)
        //{
        //    this._managerId = this.GetType().Name;
        //    this.Home_CloseAllWindow_Event = callback1;
        //    this.Home_TurnOffSyncButton_Event = callback2;
        //}

        #region 實作介面屬性
        private string _managerId = typeof(BroadcastCT_OpenIEventManager).Name;
        public string managerId { get { return _managerId; } set { _managerId = value; } }

        private string _msg;
        public string msg { get { return _msg; } set { _msg = value; } }

        private string _clientId;
        public string clientId { get { return _clientId; } set { _clientId = value; } }
        #endregion

        public void run()
        {
            try
            {

                if (Home_CloseAllWindow_Event != null)
                    Home_CloseAllWindow_Event("",false);

                if (Home_TurnOffSyncButton_Event != null)
                    Home_TurnOffSyncButton_Event();

                if (Home_SetSocketClientNull_Event != null)
                    Home_SetSocketClientNull_Event();
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }
        }

    }
}
