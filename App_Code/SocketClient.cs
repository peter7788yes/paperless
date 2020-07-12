using Newtonsoft.Json;
using NLog;
using PaperlessSync.Broadcast.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Configuration;
using System.Reflection;

namespace PaperlessSync.Broadcast.Socket
{
    public class SocketClient
    {
        private static Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private TcpClient _socket;
        private StreamReader _in;
        private StreamWriter _out;
        private String _clientId;
        private String _clientName;
        private bool _isSync;
        private int _clientType;
        private String _func;
        private bool _forceLogin;
        //private IEventManager _eventManager;
        private List<IEventManager> _eventManagerList = new List<IEventManager>();
        private List<IEventManager> _closeEventManagerList = new List<IEventManager>();
        private string meetingId;
        private int maxClient;
        private string clientId;
        private string clientName;
        private bool isSync;
        private int clientType;
        private string func;
        public string SyncServerUrl = SocketTool.GetUrl() + "/JoinSyncServer";
        private bool _joinSuccess = false;
        public bool JoinSuccess
        {
            get
            {
                return _joinSuccess;
            }
        }


        public string ip { get; set; }
        public int port { get; set; }
        //有 syncServerUrl，無 forceLogin
        public SocketClient(string syncServerUrl, string meetingId, int maxClient, string clientId, string clientName, bool isSync, int clientType, string func)
        {
            //OpenLocalConfiguration();
            _joinSuccess = InitSokcetClient(syncServerUrl, meetingId, maxClient, clientId, clientName, isSync, clientType, func, true);
        }

        //有 syncServerUrl，有 forceLogin
        public SocketClient(string syncServerUrl, string meetingId, int maxClient, string clientId, string clientName, bool isSync, int clientType, string func, bool forceLogin = true)
        {
            //OpenLocalConfiguration();
            _joinSuccess = InitSokcetClient(syncServerUrl, meetingId, maxClient, clientId, clientName, isSync, clientType, func, forceLogin);
        }

        //無 syncServerUrl，無 forceLogin
        public SocketClient(string meetingId, int maxClient, string clientId, string clientName, bool isSync, int clientType, string func)
        {
            //OpenLocalConfiguration();
            string syncServerUrl = PaperLess_Emeeting.Properties.Settings.Default["SyncServerUrl"].ToString() + "/JoinSyncServer";
            _joinSuccess = InitSokcetClient(syncServerUrl, meetingId, maxClient, clientId, clientName, isSync, clientType, func, true);
        }

        //無 syncServerUrl，有 forceLogin
        public SocketClient(string meetingId, int maxClient, string clientId, string clientName, bool isSync, int clientType, string func, bool forceLogin = true)
        {
            //OpenLocalConfiguration();
            string syncServerUrl = PaperLess_Emeeting.Properties.Settings.Default["SyncServerUrl"].ToString() + "/JoinSyncServer";
            _joinSuccess = InitSokcetClient(syncServerUrl, meetingId, maxClient, clientId, clientName, isSync, clientType, func, forceLogin);
        }

        // 切換Socket也可以用這個
        public bool InitSokcetClient(string syncServerUrl, string meetingId, int maxClient, string clientId, string clientName, bool isSync, int clientType, string func, bool forceLogin=true)
        {
            bool rtn = true;
            this.SyncServerUrl = syncServerUrl;
            this.meetingId = meetingId;
            this.maxClient = maxClient;
            this.clientId = clientId;
            this.clientName = clientName;
            this.isSync= isSync;
            this.clientType=clientType; 
            this.func=func;
            logger.Debug("syncServerUrl:{0}", syncServerUrl);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(syncServerUrl);

            StringBuilder sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>")
                .Append("<Sync>")
                .Append("<Start ID=\"").Append(meetingId).Append("\" MaxClient=\"").Append(maxClient).Append("\"/>")
                .Append("<Init>").Append(PaperLess_Emeeting.Properties.Settings.Default["InitConfig"].ToString()).Append("</Init>")
                .Append("</Sync>");

            string responseFromServer = "";

            try
            {
                string data = sb.ToString();
                logger.Debug("post data-> {0}", data);
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
                responseFromServer = reader.ReadToEnd();
                logger.Debug("response from server-> {0}", responseFromServer);
                reader.Close();
                dataStream.Close();
                response.Close();

                /*
                    <?xml version="1.0" encoding="UTF-8"?>
                    <Join>
                    <Start ID="meetingId00155" IP="10.10.4.134" Status="失敗|成功"  Port="5226" />
                    </Join>
                 * /
                 
                /*
                 <?xml version="1.0" encoding="UTF-8" ?> 
               - <Sync>
                 <Start ID="832617" IP="211.20.93.195" Port="5226" /> 
                 </Sync>
                */
            }
            catch
            {

            }

            if (responseFromServer == "" || responseFromServer.Contains("失敗"))
            {
                rtn = false;
                return rtn;
            }


            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(responseFromServer);
                XmlNode root = doc.DocumentElement;
                string ip = root.SelectSingleNode("/Join/Start/@IP").Value;
                int port = int.Parse(root.SelectSingleNode("/Join/Start/@Port").Value);
                _socket = new TcpClient(ip, port);

                _clientId = clientId;
                _clientName = clientName;
                _isSync = isSync;
                _clientType = clientType;
                _func = func;
                _forceLogin = forceLogin;

                _eventManagerList = new List<IEventManager>();
                _closeEventManagerList = new List<IEventManager>();
            }
            catch
            {
                rtn = false;
            }
            _joinSuccess = rtn;
            return rtn;
        }

        public bool GetIsConnected()
        {
            bool rtn = false;

            if (_in != null && _out != null && _socket != null && JoinSuccess == true)
                rtn = true;

            return rtn;
        }

        public bool ReConnect(bool isSync)
        {
            InitSokcetClient(SyncServerUrl, meetingId, maxClient, clientId, clientName, isSync, clientType, func, true);

            return GetIsConnected();
        }   
        
        //private void OpenLocalConfiguration()
        //{
        //    try
        //    {
        //        string codebase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
        //        Uri p = new Uri(codebase);
        //        string localPath = p.LocalPath;
        //        string executingFilename = System.IO.Path.GetFileNameWithoutExtension(localPath);
        //        string sectionGroupName = "applicationSettings";
        //        string sectionName = executingFilename + ".Properties.Settings";
        //        string configName = localPath + ".config";

        //        if (File.Exists(configName))
        //        {
        //            XmlDocument xmlDoc = new XmlDocument();
        //            xmlDoc.Load(configName);

        //            XmlNodeList configNodes = xmlDoc.SelectNodes("/configuration/" + sectionGroupName + "/" + sectionName + "/setting");

        //            Dictionary<string, string> configSettings = new Dictionary<string, string>();
        //            if (configNodes.Count > 0)
        //            {
        //                foreach (XmlNode configNode in configNodes)
        //                {
        //                    if (configNode.Attributes.Count > 0)
        //                    {
        //                        for (int i = 0; i < configNode.Attributes.Count; i++)
        //                        {
        //                            if (configNode.Attributes[i].Name == "name")
        //                            {
        //                                configSettings.Add(configNode.Attributes[i].Value, configNode.InnerText);
        //                            }
        //                        }
        //                    }
        //                }

        //                if (configSettings.ContainsKey("SyncServerUrl"))
        //                {
        //                    Properties.Settings.Default.SyncServerUrl = configSettings["SyncServerUrl"];
        //                }

        //                if (configSettings.ContainsKey("InitConfig"))
        //                {
        //                    Properties.Settings.Default.InitConfig = configSettings["InitConfig"];
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        //Load local config fail, use default value
        //        logger.Debug("Load local config fail, use default value: {0}", e.ToString());
        //    }
        //}

        //public IEventManager SyncEventManager
        //{
        //    set
        //    {
        //        _eventManager = value;
        //    }
        //}

        public void AddEventManager(IEventManager eventManager)
        {
            RemoveEventManager(eventManager);

            bool notFound = true;
            foreach (IEventManager mgr in _eventManagerList)
            {
                if (eventManager == mgr)
                {
                    notFound = true;
                    break;
                }
            }

            if (notFound)
            {
                _eventManagerList.Add(eventManager);
            }
        }


        public void AddEventManager(List<IEventManager> list)
        {
            _eventManagerList.AddRange(list);
        }


        public void AddCloseEventManager(List<IEventManager> list)
        {
            _closeEventManagerList.AddRange(list);
        }

        public void AddCloseEventManager(IEventManager eventManager)
        {
            RemoveCloseEventManager(eventManager);
            bool notFound = true;
            foreach (IEventManager mgr in _closeEventManagerList)
            {
                if (eventManager == mgr)
                {
                    notFound = true;
                    break;
                }
            }

            if (notFound)
            {
                _closeEventManagerList.Add(eventManager);
            }
        }
        public void RemoveEventManager(IEventManager eventManager)
        {
            try
            {
                foreach (IEventManager mgr in _eventManagerList)
                {
                    if (eventManager.managerId.Equals(mgr.managerId)==true)
                    {
                        _eventManagerList.Remove(mgr);
                        break;
                    }
                }
            }
            catch
            {
            }
        }

        public void RemoveCloseEventManager(IEventManager eventManager)
        {
            try
            {
                foreach (IEventManager mgr in _closeEventManagerList)
                {
                    if (eventManager.managerId.Equals(mgr.managerId) == true)
                    {
                        _closeEventManagerList.Remove(mgr);
                        break;
                    }
                }
            }
            catch
            {
            }
        }

        //public static DateTime ConvertFromUnixTimestamp(double timestamp)
        //{
        //    DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        //    return origin.AddSeconds(timestamp);
        //}

        //public static double ConvertToUnixTimestamp(DateTime date)
        //{
        //    DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        //    TimeSpan diff = date.ToUniversalTime() - origin;
        //    return Math.Floor(diff.TotalSeconds);
        //}

        //把.net的時間轉成java, obj-c用的時間
        public static ulong GetCurrentTimeInUnixMillis()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            ulong nMilliseconds = (ulong)ts.Days * 86400000UL + //24 * 60 * 60 * 1000 
                                   (ulong)ts.Hours * 3600000UL + //60 * 60 * 1000 +
                                   (ulong)ts.Minutes * 60000UL + // 60 * 1000 +
                                   (ulong)ts.Seconds * 1000UL +
                                   (ulong)ts.Milliseconds;

            return nMilliseconds;
        }

        public void run()
        {
            try
            {
                NetworkStream ns = _socket.GetStream();
                _in = new StreamReader(ns, Encoding.UTF8);
                _out = new StreamWriter(ns, Encoding.UTF8);

                if (_clientId != null)
                {
                    Dictionary<String, Object> initMap = new Dictionary<String, Object>();
                    initMap.Add("cmd", "init");
                    //clientId在同場會議中要唯一,可帶帳號
                    initMap.Add("clientId", _clientId);
                    initMap.Add("clientName", _clientName);
                    initMap.Add("isSync", _isSync);
                    initMap.Add("clientType", _clientType);
                    initMap.Add("func", _func);
                    initMap.Add("forceLogin", _forceLogin);
                    string msg = JsonConvert.SerializeObject(initMap);
                    logger.Debug("initMap-> {0}", msg);
                    _out.WriteLine(msg);
                    _out.Flush();
                }
                Thread t = new Thread(() =>
                {
                    while (true)
                    {
                        try
                        {
                            _out.WriteLine("_chk");
                            _out.Flush();
                            Console.WriteLine("_out => _chk");
                        }
                        catch
                        {
                            if (_closeEventManagerList != null)
                            {
                                foreach (IEventManager mgr in _closeEventManagerList)
                                {
                                    //Thread eventThread = new Thread(mgr.run);
                                    Thread eventThread = new Thread(() =>
                                    {
                                        try
                                        {
                                            if (mgr != null)
                                            {
                                                mgr.run();
                                            }
                                        }
                                        catch (Exception ex3)
                                        {
                                            LogTool.Debug(ex3);
                                        }
                                    });
                                    eventThread.Start();
                                }
                            }

                            try
                            {
                                _out = null;
                                _in = null;
                                _socket = null;

                            }
                            catch (Exception e)
                            {
                                logger.Debug("socket close: {0}", e.ToString());
                            }
                            break;
                        }
                        Thread.Sleep(5000);
                    }
                });
                t.IsBackground = true;
                t.Start();
                while (true)
                {
                    string msg = _in.ReadLine();

                    if (msg != null)
                        Console.WriteLine("_in => " + msg);

                    if (msg == null)
                    {
                        logger.Debug("InStream got null...socket may be closed...");
                        break;
                    }
                    else if ("_chk".Equals(msg))
                    {
                        //_out.WriteLine("_chk");
                        //Console.WriteLine("_out => _chk");
                        continue;
                    }


                    //處理自server接收到的資訊
                    logger.Debug("Receiving message from Server, pass to EventManager");
                    lock (this)
                    {
                        try
                        {
                            foreach (IEventManager mgr in _eventManagerList)
                            {
                                if (mgr != null)
                                {
                                    mgr.clientId = _clientId;
                                    mgr.msg = msg;
                                    //Thread eventThread = new Thread(mgr.run);
                                    Thread eventThread = new Thread(() =>
                                    {
                                        try
                                        {
                                            if (mgr != null)
                                            {
                                                mgr.run();
                                            }
                                        }
                                        catch (Exception ex3)
                                        {
                                            LogTool.Debug(ex3);
                                        }
                                    });
                                    eventThread.Start();
                                }
                            }
                        }
                        catch(Exception ex)
                        {
                            LogTool.Debug(ex);

                            try
                            {
                                if (_closeEventManagerList != null)
                                {
                                    foreach (IEventManager mgr in _eventManagerList)
                                    {
                                        if (mgr != null)
                                        {
                                            mgr.clientId = _clientId;
                                            mgr.msg = msg;
                                            //Thread eventThread = new Thread(mgr.run);
                                            Thread eventThread = new Thread(() =>
                                            {
                                                try
                                                {
                                                    if (mgr != null)
                                                    {
                                                        mgr.run();
                                                    }
                                                }
                                                catch (Exception ex3)
                                                {
                                                    LogTool.Debug(ex3);
                                                }
                                            });
                                            eventThread.Start();
                                        }
                                    }
                                }
                            }
                            catch (Exception ex2)
                            {
                                LogTool.Debug(ex2);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Debug("socket runtime exception: {0}", e.ToString());

                try
                {
                    try
                    {
                        foreach (IEventManager mgr in _closeEventManagerList)
                        {
                            if (mgr != null)
                            {
                                //Thread eventThread = new Thread(mgr.run);
                                Thread eventThread = new Thread(() =>
                                {
                                    try
                                    {
                                        if (mgr != null)
                                        {
                                            mgr.run();
                                        }
                                    }
                                    catch (Exception ex3)
                                    {
                                        LogTool.Debug(ex3);
                                    }
                                });
                                eventThread.Start();
                            }
                        }
                    }
                    catch (Exception e2)
                    {
                        logger.Debug("socket close: {0}", e2.ToString());
                        if (_closeEventManagerList != null)
                        {
                            foreach (IEventManager mgr in _closeEventManagerList)
                            {
                                Thread eventThread = new Thread(() => {
                                    try
                                    {
                                        if (mgr != null)
                                        {
                                            mgr.run();
                                        }
                                    }
                                    catch (Exception ex3)
                                    {
                                        LogTool.Debug(ex3);
                                    }
                                });
                                eventThread.Start();
                            }
                        }
                    }

               
                    _out = null;
                    _in = null;
                    _socket = null;

                }
                catch (Exception e1)
                {
                    logger.Debug("socket close: {0}", e1.ToString());
                }

                return;


                //-------------------------------
                try
                {
                    _out.Close();
                    _in.Close();
                    _socket.Close();

                }
                catch (IOException e2)
                {
                    logger.Debug("socket close: {0}", e2.ToString());
                }
            }
        }

        public void syncSwitch(bool isSync)
        {
            try
            {
                if (_socket != null)
                {
                    Dictionary<String, Object> jsonMap = new Dictionary<String, Object>();
                    jsonMap.Add("cmd", isSync ? "syncOn" : "syncOff");
                    string msg = JsonConvert.SerializeObject(jsonMap);
                    logger.Debug("syncSwitch-> {0}", msg);
                    _out.WriteLine(msg);
                    _out.Flush();
                }
            }
            catch (Exception ex)
            {
                 Console.WriteLine(ex.Message);
            }    
        }

        public void getUserList()
        {
            try
            {
                Dictionary<String, Object> jsonMap = new Dictionary<String, Object>();
                jsonMap.Add("cmd", "userList");
                string msg = JsonConvert.SerializeObject(jsonMap);
                logger.Debug("getUserList-> {0}", JsonConvert.SerializeObject(jsonMap));
                _out.WriteLine(msg);
                _out.Flush();
            }
            catch(Exception ex)
            {
                 Console.WriteLine(ex.Message);
            }
        }

        public void setSyncOwner(String ownerId)
        {
            try
            {
                Dictionary<String, Object> jsonMap = new Dictionary<String, Object>();
                jsonMap.Add("cmd", "syncOwner");
                jsonMap.Add("clientId", ownerId == null ? "" : ownerId);
                string msg = JsonConvert.SerializeObject(jsonMap);
                logger.Debug("setSyncOwner-> {0}", JsonConvert.SerializeObject(jsonMap));
                _out.WriteLine(msg);
                _out.Flush();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void broadcast(String msg)
        {
            try
            {
                Dictionary<String, Object> jsonMap = new Dictionary<String, Object>();
                msg = msg.Replace(Environment.NewLine, "\\n");
                jsonMap.Add("msg", msg);
                jsonMap.Add("sender", _clientId);
                jsonMap.Add("sendTime", SocketClient.GetCurrentTimeInUnixMillis());
                jsonMap.Add("cmd", "broadcast");
                string jsonmsg = JsonConvert.SerializeObject(jsonMap);
                logger.Debug("broadcast-> {0}", jsonmsg);
                _out.WriteLine(jsonmsg);
                _out.Flush();
            }
            catch
            {
            }
        }

        public void logout()
        {
            try
            {
                Dictionary<String, Object> jsonMap = new Dictionary<String, Object>();
                jsonMap.Add("cmd", "offline");
                string msg = JsonConvert.SerializeObject(jsonMap);
                logger.Debug("logout-> {0}", JsonConvert.SerializeObject(jsonMap));
                _out.WriteLine(msg);
                _out.Flush();
            }
            catch
            {
            }
        }

        public bool isClosed()
        {
            bool rtn = false;

            try
            {
                return !_socket.Connected;
            }
            catch
            {

            }

            return rtn;
        }

        //public static void Main()
        //{
        //    Console.WriteLine("ha ha");
        //}


     
    }
}
