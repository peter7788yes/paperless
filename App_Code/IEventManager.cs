using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaperlessSync.Broadcast.Service
{
    //abstract public class EventManager
    //{
    //    public string msg { get; set; }
    //    public string clientId { get; set; }
    //    public abstract void run();
    //}

    public interface IEventManager
    {
        string managerId { get; set; }
        string msg { get; set; }
        string clientId { get; set; }
        void run();
    }
}
