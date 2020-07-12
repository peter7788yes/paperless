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
    public delegate void BroadcastCT_ChangeList_Function(JArray jArry);
    public delegate void BroadcastCT_ClearList_Function();

    public class BroadcastCT_OpenIEventManager : IEventManager
    {
        public event BroadcastCT_ChangeList_Function BroadcastCT_ChangeList_Event;

        public BroadcastCT_OpenIEventManager()
        {
            this._managerId = this.GetType().Name;
        }

        //public BroadcastCT_OpenIEventManager(BroadcastCT_ChangeList_Function callback)
        //{
        //    this._managerId = this.GetType().Name;
        //    this.BroadcastCT_ChangeList_Event = callback;
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
                    LogTool.Debug(string.Format("EventManagerBroadcastCT[{0}] recive message->{1}", _managerId, cacheMsg));
                    LogTool.Debug(string.Format("EventManagerBroadcastCT[{0}] recive msg form {1} time = {2} ms", _managerId, clientId, reciveTime));
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
                        if (BroadcastCT_ChangeList_Event!=null)
                            BroadcastCT_ChangeList_Event(jArry);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }

        }

    }



    //public delegate void BroadcastCT_Close_Function();
    public class BroadcastCT_CloseIEventManager : IEventManager
    {
        public event BroadcastCT_ClearList_Function BroadcastCT_ClearList_Event;

        public BroadcastCT_CloseIEventManager()
        {
            this._managerId = this.GetType().Name;
        }

        //public BroadcastCT_CloseIEventManager(BroadcastCT_ClearList_Function callback)
        //{
        //    this._managerId = this.GetType().Name;
        //    this.BroadcastCT_ClearList_Event = callback;
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
                if (BroadcastCT_ClearList_Event != null)
                    BroadcastCT_ClearList_Event();
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }
        }

    }
}
