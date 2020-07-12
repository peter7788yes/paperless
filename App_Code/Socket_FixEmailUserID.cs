using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PaperLess_Emeeting.App_Code.Socket
{
    public class Socket_FixEmailUserID
    {
        public static string ToSocket(string UserID)
        {
            return UserID.Replace('.', '*');
        }

        public static string FromSocket(string UserID)
        {
            return UserID.Replace('*', '.');
        }
    }
}
