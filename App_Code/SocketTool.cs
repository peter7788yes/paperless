using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
public class SocketTool
{
    public static string GetUrl()
    {
        if (PaperLess_Emeeting.Properties.Settings.Default.IsDebugMode == false)
        {
            return PaperLess_Emeeting.Properties.Settings.Default.SyncServerUrl;
        }
        else
        {
            return PaperLess_Emeeting.Properties.Settings.Default.SyncServerUrl_Debug;
        }
    }

    public static string GetUrl_Imp()
    {
        if (PaperLess_Emeeting.Properties.Settings.Default.IsDebugMode == false)
        {
            return PaperLess_Emeeting.Properties.Settings.Default.SyncServerUrl_Imp;
        }
        else
        {
            return PaperLess_Emeeting.Properties.Settings.Default.SyncServerUrl_Debug;
        }
    }
}
