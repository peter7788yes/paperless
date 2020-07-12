using PaperLess_Emeeting.App_Code.WS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PaperLess_Emeeting.App_Code
{
    //取得各個會議室的下載器
    public class Singleton_FileDownloader
    {
        // 多執行緒，lock 使用
        private static readonly object thisLock = new object();

        // 將唯一實例設為 private static
        // 餓漢方式(Eager initialization)：class 載入時就產生實例，不管後面會不會用到。
        private static  Dictionary<string, FileDownloader> dcit_instance = new Dictionary<string, FileDownloader>();
 
        public static Home_UnZipError_Function Home_UnZipError_Callback;
 
        // 設為 private，外界不能 new
        // 重要
        private Singleton_FileDownloader()
        {
        }

        // 使用靜態方法取得實例，因為載入時就 new 一個實例，所以不用考慮多執行緒的問題
        public static Dictionary<string, FileDownloader> GetInstance()
        {
            return dcit_instance;
        }

        // 使用靜態方法取得實例，因為載入時就 new 一個實例，所以不用考慮多執行緒的問題
        public static FileDownloader GetInstance(string KeyOfMeetingID_GetFileDownloader)
        {
                // [重點] 
                // ref參數在傳入方法之前，要先初始化完畢。
                // out參數在方法結束之前，要先初始化完畢。
                // 總之使用 ref 或 out回傳的值一定會被初始化，所以不會等於null。
                // 所以以後不要再寫 if( obj != null)，方法的構成請多使用 ref 和 out。
                // 使用ref 和 out 是省記憶體資源的一種方式。
                // 另外少寫static，因為寫越多記憶體用越多，[程序]一啟動時就占用一堆記憶體
                // 寫new 物件的方式則是，有用到就分配記憶體空間，沒用到就是放掉記憶體
                // 所以對記憶體利用效率較高，記憶體循環利用率好
                // 以這種方式撰寫的程式，對記憶體大小需求可以較低(記憶體少的PC也可以跑得很順)
                // 所以請多加利用。
                // [補充]
                // 如果一定會使用到的Class Api方法，則可以考慮以static撰寫，
                // [程序]啟動時就有類別方法可以使用，程式碼比較簡短(少了new)，效率可能會比較好

                // (理由一) 雖然使用out 不用初始化，但是避免回傳null 還是先初始化好了 FileDownloader rtn =new FileDownloader;
                // (理由二) 不過後來想想，rtn 一定會被new出來，因為使用out，出來不會等於null，所以也可以不要初始化，省資源。
                FileDownloader rtn ;

                // 是否包含此會議ID的下載器
                // 如果TryGetValue失敗會new 出一個FileDownloader
                // 主執行序先檢查有沒有包含下載器，有的話直接取出就不用lock了
                if(dcit_instance.TryGetValue(KeyOfMeetingID_GetFileDownloader, out rtn)==false)
                {
                    // 沒有的話lock住，再取出下載器
                    // 不過因為是多執行序環境，有可能上面檢查有包含下載器，
                    // 當下一行要取出時下載器時，有可能被搶先一步移除，
                    // 所以要再檢查一遍是否包含下載器。
                    lock(thisLock)
                    {
                        // 檢查是否被搶先移除或加入
                        // 如果沒有包含下載器，就產生一個下載器，並且加入
                        if(dcit_instance.TryGetValue(KeyOfMeetingID_GetFileDownloader, out rtn)==false)
                        {
                            //new 一個新的下載器
                            rtn = new FileDownloader();
                            dcit_instance[KeyOfMeetingID_GetFileDownloader] = rtn;
                        }
                        else
                        {
                            // 如果被搶先一步new 出下載器物件的話，就直接取出
                            rtn =dcit_instance[KeyOfMeetingID_GetFileDownloader];
                        }
                    }
                }
                else
                {
                     // 主執行序當中檢查有下載器物件，就直接取出
                     rtn =dcit_instance[KeyOfMeetingID_GetFileDownloader];
                }

                rtn.ClearHomeEvent();
                rtn.Home_UnZipError_Event += Home_UnZipError_Callback;
                return rtn;
        }

    }
}
