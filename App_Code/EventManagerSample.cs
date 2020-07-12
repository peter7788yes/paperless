using Newtonsoft.Json;
using NLog;
using PaperlessSync.Broadcast.Socket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaperlessSync.Broadcast.Service
{
    public class EventManagerSample : IEventManager
    {
        private static Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private string _managerId;
        public string managerId
        {
            get
            {
                return _managerId;
            }
            set
            {
                _managerId = value;
            }
        }

        private string _msg;
        public string msg
        {
            get
            {
                return _msg;
            }
            set
            {
                _msg = value;
            }
        }

        private string _clientId;
        public string clientId
        {
            get
            {
                return _clientId;
            }
            set
            {
                _clientId = value;
            }
        }

        public void run()
        {
            Dictionary<string, Object> msgJson = JsonConvert.DeserializeObject<Dictionary<string, Object>>(msg);
            long reciveTime = (long)(SocketClient.GetCurrentTimeInUnixMillis() - (ulong)((long)msgJson["sendTime"]));
            logger.Debug("EventManagerSample[{0}] recive message->{1}", _managerId, msg);
            logger.Debug("EventManagerSample[{0}] recive msg form {1} time = {2} ms", _managerId, clientId, reciveTime);
        }

    }
}
