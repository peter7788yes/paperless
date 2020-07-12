using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaperlessSync.Broadcast.Service;
using PaperlessSync.Broadcast.Socket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PaperLess_Emeeting.App_Code.Socket
{

    public delegate void MVWindow_IsInSync_And_IsSyncOwner_Function(JArray jArry);
    public delegate void MVWindow_MVAction_Function(JObject jObject);
    public class MVWindow_OpenIEventManager : IEventManager
    {
        public event MVWindow_IsInSync_And_IsSyncOwner_Function MVWindow_IsInSync_And_IsSyncOwner_Event;
        public event MVWindow_MVAction_Function MVWindow_MVAction_Event;

        public MVWindow_OpenIEventManager()
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

                try
                {
                    Console.WriteLine("Home_OpenIEventManager: " + cacheMsg);
                    msgJson = JsonConvert.DeserializeObject<Dictionary<string, Object>>(cacheMsg);
                    reciveTime = (long)(SocketClient.GetCurrentTimeInUnixMillis() - (ulong)((long)msgJson["sendTime"]));
                    LogTool.Debug(string.Format("EventManagerMVWindow[{0}] recive message->{1}", _managerId, cacheMsg));
                    LogTool.Debug(string.Format("EventManagerMVWindow[{0}] recive msg form {1} time = {2} ms", _managerId, clientId, reciveTime));
                }
                catch (Exception ex)
                {
                }
               


                //{"msg":"[{\"clientId\":\"kat\",\"clientName\":\"kat\",\"clientType\":1,\"status\":0,\"joinTime\":1370424638670,\"funcCmd\":[\"userList\",\"broadcast\"],\"func\":\"12\"},{\"clientId\":\"mat\",\"clientName\":\"mat\",\"clientType\":1,\"status\":0,\"joinTime\":1370424637583,\"funcCmd\":[\"userList\",\"broadcast\"],\"func\":\"12\"}]","sender":"69","sendTime":1370424638831,"cmd":"userList"}
                try
                {
                    cmd = msgJson["cmd"].ToString();
                }
                catch (Exception ex)
                {
                }

                switch (cmd)
                {
                    case "userList":
                        string UserListJsonArray = msgJson["msg"].ToString();
                        //[{\"clientId\":\"kat\",\"clientName\":\"kat\",\"clientType\":1,\"status\":0,\"joinTime\":1370424638670,\"funcCmd\":[\"userList\",\"broadcast\"],\"func\":\"12\"},{\"clientId\":\"mat\",\"clientName\":\"mat\",\"clientType\":1,\"status\":0,\"joinTime\":1370424637583,\"funcCmd\":[\"userList\",\"broadcast\"]
                        JArray jArry = JsonConvert.DeserializeObject<JArray>(UserListJsonArray);
                        if (MVWindow_IsInSync_And_IsSyncOwner_Event != null)
                            MVWindow_IsInSync_And_IsSyncOwner_Event(jArry);
                        break;
                    case "broadcast":
                        string outterCmd = msgJson["msg"].ToString();
                        JObject jo = JObject.Parse(outterCmd);
                        string innerCmd = jo["cmd"].ToString().ToUpper();

                        switch (innerCmd)
                        {
                            case "R.SV":
                                //{"execTime":1396839823,"action":"play","actionTime":"120:32:12.642","cmd":"R.SV"}
                                if (MVWindow_MVAction_Event != null)
                                    MVWindow_MVAction_Event(jo);
                                break;
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



    public class MVWindow_CloseIEventManager : IEventManager
    {


        public MVWindow_CloseIEventManager()
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

               
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }
        }

    }
}
