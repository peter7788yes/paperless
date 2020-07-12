using PaperLess_Emeeting.App_Code.WS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PaperLess_Emeeting.App_Code
{
    public class Singleton_LawDownloader
    {
         // 多執行緒，lock 使用
        private static readonly object thisLock = new object();
 
        // 將唯一實例設為 private static
        // 懶漢方式(Lazy initialization)：第一次使用時，才產生實例。
        // 改為餓漢方式
        private static  LawDownloader instance = new LawDownloader();

        //public static Home_UnZipError_Function Home_UnZipError_Callback;
        //public int test = 1;
 
        // 設為 private，外界不能 new
        // 重要
        private Singleton_LawDownloader()
        {
        }

        // 使用靜態方法取得實例，因為載入時就 new 一個實例，所以不用考慮多執行緒的問題
        public static LawDownloader GetInstance()
        {
            //instance.ClearHomeEvent();
            //instance.Home_UnZipError_Event += Home_UnZipError_Callback;
            return instance;
        }

        // 懶漢方式
        // 外界只能使用靜態方法取得實例
        //public static LawDownloader GetInstance()
        //{
        //    // 先判斷目前有沒有實例，沒有的話才開始 lock，
        //    // 此次的判斷，是避免在有實例的情況，也執行 lock ，影響效能
        //    if (instance == null)
        //    {
        //        // 避免多執行緒可能會產生兩個以上的實例，所以 lock
        //        lock (thisLock)
        //        {
        //            // lock 後，再判斷一次目前有無實例
        //            // 此次的判斷，是避免多執行緒，同時通過前一次的 null == instance 判斷
        //            if (instance == null)
        //            {
        //                instance = new LawDownloader();
        //            }
        //        }
        //    }
 
        //    return instance;
        //}

    }
}
