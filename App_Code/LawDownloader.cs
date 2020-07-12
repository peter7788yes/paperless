using PaperLess_Emeeting.App_Code.DownloadItem;
using PaperLess_Emeeting.App_Code.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace PaperLess_Emeeting.App_Code.WS
{
    public delegate void LawListCT_DownloadFileStart_Function(Law_DownloadItemViewModel obj);
    public delegate void LawListCT_DownloadProgressChanged_Function(Law_DownloadItemViewModel obj);
    public delegate void LawListCT_DownloadFileCompleted_Function(Law_DownloadItemViewModel obj);

    public delegate void LawListCT_UnZip_Function(Law_DownloadItemViewModel obj);
    public delegate void LawListCT_UnZipError_Function(Law_DownloadItemViewModel obj);


    public delegate void Home_UnZipError_Function(string message);

    public class LawDownloader
    {
        // 多執行緒，lock 使用
        private static readonly object thisLock = new object();

        public event LawListCT_DownloadFileStart_Function LawListCT_DownloadFileStart_Event;
        public event LawListCT_DownloadProgressChanged_Function LawListCT_DownloadProgressChanged_Event;
        public event LawListCT_DownloadFileCompleted_Function LawListCT_DownloadFileCompleted_Event;

        public event LawListCT_UnZip_Function LawListCT_UnZip_Event;
        public event LawListCT_UnZipError_Function LawListCT_UnZipError_Event;

        public event Home_UnZipError_Function Home_UnZipError_Event;

        // buffer大小會影響進度條的改變頻率
        // 越小改變頻率越高 1024 * 1024; 是 1024 Bytes => (1KB) * 多少 KB =>1KB * 1024 =>1MB
        // 預設 1MB
        private int buffer = 1024 * 1024; //1MB
        private List<Law_DownloadItemViewModel> list = new List<Law_DownloadItemViewModel>();
        
        public Law_DownloadItemViewModel NowLawItem { get; private set; }
        public DownloaderType downloaderType { get; private set; }

        // 建構子
        public LawDownloader()
        {
            this.downloaderType = DownloaderType.沒有任何檔案下載中;
            this.NowLawItem = null;
            // 依設定檔改變buffer
            int KB = PaperLess_Emeeting.Properties.Settings.Default.DownloadBuffer_KB;
            buffer = 1024 * KB;
        }

        public void ClearAllEvent()
        {
                LawListCT_DownloadFileStart_Event = null;
                LawListCT_DownloadProgressChanged_Event = null;
                LawListCT_DownloadFileCompleted_Event = null;

                LawListCT_UnZip_Event = null;
                LawListCT_UnZipError_Event = null;

               // Home的可以不要清除，清除的話要記得加上
               Home_UnZipError_Function Home_UnZipError_Event;
        }

        public void ClearHomeEvent()
        {
             // 清除Home的
             Home_UnZipError_Event =null;
        }

        public void ClearLawListCTEvent()
        {
            // 清除Home的
            LawListCT_DownloadFileStart_Event = null;
            LawListCT_DownloadProgressChanged_Event = null;
            LawListCT_DownloadFileCompleted_Event = null;

            LawListCT_UnZip_Event = null;
            LawListCT_UnZipError_Event = null;
        }

        //這裡的Start請開執行緒去跑
        public void Start()
        {
           downloaderType=DownloaderType.正在下載中;
           try
           {
               if(list.Count>0)
               {
                   lock (thisLock)
                   {
                       if (list.Count > 0)
                       {
                           NowLawItem = list[0];
                           list.Remove(NowLawItem);
                       }
                   }
                   if (NowLawItem != null)
                   {

                       try
                       {
                           DownloadFileStart(NowLawItem); //開始

                           HttpWebRequest request = (HttpWebRequest)FileWebRequest.Create(NowLawItem.Link);

                           //Wayne 20150429
                           request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.Revalidate);
                           request.Proxy = null;
                           request.AutomaticDecompression = DecompressionMethods.GZip;

                           if (NowLawItem.DownloadBytes > 0)
                           {
                               //if (File.Exists(NowLawItem.StorageFilePath) == true)
                               //{
                               //    NowLawItem.DownloadBytes = new FileInfo(NowLawItem.StorageFilePath).Length;
                               //    if (NowLawItem.DownloadBytes >= NowLawItem.TotalBytes)
                               //    {
                               //        downloaderType = DownloaderType.檔案下載完成;
                               //        // 更新資料
                               //        //UpdateToDB(NowFileItem);
                               //        goto TypeControl;
                               //    }
                               //    else
                               //    {
                               //        request.AddRange(NowLawItem.DownloadBytes);
                               //        //UpdateToDB(NowFileItem);
                               //    }
                               //}
                               //else
                               //{
                               //request.AddRange(NowLawItem.DownloadBytes);
                               // 更新資料
                               //UpdateToDB(NowFileItem);
                               //}

                           }

                           //取得回應
                           WebResponse response = request.GetResponse();

                           if (NowLawItem.TotalBytes == 0)
                               NowLawItem.TotalBytes = response.ContentLength;

                           //先建立檔案要存放的資料夾
                           Directory.CreateDirectory(NowLawItem.StorageFileFolder);
                           using (var writer = new FileStream(NowLawItem.StorageFilePath, FileMode.OpenOrCreate))
                           {
                               try
                               {
                                   writer.Seek(0, SeekOrigin.End);
                                   using (var stream = response.GetResponseStream())
                                   {


                                       while (NowLawItem.DownloadBytes < NowLawItem.TotalBytes)  //暫停條件設定成 fileItem的屬性 IsPause =true 就停止
                                       {
                                           // 暫停和停止動作一定要寫在while裡
                                           if (downloaderType == DownloaderType.暫停 || downloaderType == DownloaderType.停止)
                                           {
                                               //UpdateToDB(NowLawItem);
                                               break;
                                           }

                                           byte[] data = new byte[buffer];
                                           int readNumber = stream.Read(data, 0, data.Length);
                                           if (readNumber > 0)
                                           {
                                               writer.Write(data, 0, readNumber);
                                               writer.Flush();
                                               NowLawItem.DownloadBytes += readNumber;
                                           }

                                           if (NowLawItem.DownloadBytes >= NowLawItem.TotalBytes)
                                           {
                                               //先寫入檔案，並且關閉檔案使用權。
                                               downloaderType = DownloaderType.檔案下載完成;
                                               break;
                                           }
                                           else
                                           {

                                               if (downloaderType == DownloaderType.暫停 || downloaderType == DownloaderType.停止)
                                               {
                                                   //UpdateToDB(NowLawItem);
                                                   break;
                                               }
                                               else
                                               {
                                                   double percentage = GetPercent(NowLawItem.DownloadBytes, NowLawItem.TotalBytes);
                                                   if (percentage - NowLawItem.LastPercentage > PaperLess_Emeeting.Properties.Settings.Default.Downloader_InvokePercent)
                                                   {
                                                       NowLawItem.LastPercentage = percentage;
                                                       // 進度條百分比callback
                                                       DownloadProgressChanged(NowLawItem);
                                                       //UpdateToDB(NowLawItem);
                                                       //Thread.Sleep(1);
                                                   }
                                               }
                                           }
                                       }

                                       if (NowLawItem.DownloadBytes >= NowLawItem.TotalBytes)
                                       {
                                           DownloadProgressChanged(NowLawItem);
                                           //先寫入檔案，並且關閉檔案使用權。
                                           downloaderType = DownloaderType.檔案下載完成;
                                       }
                                   }
                               }
                               catch (Exception ex)
                               {
                                   downloaderType = DownloaderType.下載出錯;
                                   try
                                   {
                                       if (Home_UnZipError_Event != null)
                                       {
                                           Home_UnZipError_Event(string.Format("檔名: {0}，{1}"
                                                  , NowLawItem.Name == null ? "" : NowLawItem.Name
                                                  , Enum.GetName(typeof(DownloaderType), DownloaderType.下載出錯)));
                                           Thread.Sleep(1100);
                                       }
                                       LogTool.Debug(ex);
                                   }
                                   catch
                                   {
                                       downloaderType = DownloaderType.下載出錯;
                                   }
                               }

                           }
                       }
                       catch (Exception ex2)
                       {
                           downloaderType = DownloaderType.下載出錯;
                           try
                           {
                               if (Home_UnZipError_Event != null)
                               {
                                   Home_UnZipError_Event(string.Format("檔名: {0}，{1}"
                                          , NowLawItem.Name == null ? "" : NowLawItem.Name
                                          , Enum.GetName(typeof(DownloaderType), DownloaderType.下載出錯)));
                                   Thread.Sleep(1100);
                               }
                               LogTool.Debug(ex2);
                           }
                           catch
                           {
                               downloaderType = DownloaderType.下載出錯;
                           }
                       }
                   }
                   else
                   {
                       downloaderType=DownloaderType.沒有任何檔案下載中;
                   }
               }
               else
               {
                    downloaderType=DownloaderType.沒有任何檔案下載中;
               }
               
           }
           catch(Exception ex)
           {
               downloaderType = DownloaderType.下載出錯;
               try
               {
                   if (Home_UnZipError_Event != null)
                   {
                       Home_UnZipError_Event(string.Format("檔名: {0}，{1}"
                              , NowLawItem.Name == null ? "" : NowLawItem.Name
                              , Enum.GetName(typeof(DownloaderType), DownloaderType.下載出錯)));
                       Thread.Sleep(1100);
                   }
                   LogTool.Debug(ex);
               }
               catch
               {
                   downloaderType = DownloaderType.下載出錯;
               }
           }

            TypeControl:
            // 在這裡統整所有的控制行為 錯誤,暫停,停止,完成
            switch (downloaderType)
            {
                case DownloaderType.沒有任何檔案下載中:
                    //StartNextFileItemDownload(NowFileItem);
                    break;
                case DownloaderType.停止:
                    //NowLawItem = null;
                    //lock (thisLock)
                    //{
                    //    list.Clear();
                    //}
                    //downloaderType = DownloaderType.沒有任何檔案下載中;
                    StartNextFileItemDownload(NowLawItem);
                    // 不要開新的下載
                    break;
                case DownloaderType.下載出錯:
                    // 呼叫 Error callback
                    downloaderType = DownloaderType.沒有任何檔案下載中;
                    StartNextFileItemDownload(NowLawItem);
                    break;
                case DownloaderType.暫停:
                    // 呼叫 Pause callback
                    StartNextFileItemDownload(NowLawItem);
                    break;
                case DownloaderType.檔案下載完成:
                    // 呼叫 FileCompleted callback
                    // 下面會等待到解壓縮完，才會繼續下一個檔案的下載
                    DownloadFileCompleted(NowLawItem); 
                    StartNextFileItemDownload(NowLawItem);
                    break;

            }
        }

        private void UpdateToDB(Law_DownloadItemViewModel lawItem)
        {
//            if (fileItem == null)
//                return;
//            string SQL = @"update  FileRow set Url=@1,StorageFileName=@2
//                       , DownloadBytes=@3,TotalBytes=@4 where ID=@6 and UserID=@7 and MeetingID=@8";

//            int success = MSCE.ExecuteNonQuery(SQL
//                                               , fileItem.Url
//                                               , fileItem.StorageFileName
//                                               , fileItem.DownloadBytes.ToString()
//                                               , fileItem.TotalBytes.ToString()
//                                               , fileItem.ID
//                                               , fileItem.UserID
//                                               , fileItem.MeetingID);
//            if (success < 1)
//                LogTool.Debug(new Exception("DB失敗:" + SQL));
        }
        private void StartNextFileItemDownload(Law_DownloadItemViewModel lawItem)
        {
            lock (this)
            {
                if (downloaderType == DownloaderType.停止)
                {
                    //清空所有列表
                    list.Clear();
                }
                // 設定成 沒有正在下載 和 沒有正在下載的物件
                lawItem = null;
                downloaderType = DownloaderType.沒有任何檔案下載中;
                // 不同線程的回呼，不會造成Recursive堆疊上限
                // 如果不是停止就開始新的檔案物件下載
                //LawDownloader lawDownloader = Singleton_LawDownloader.GetInstance();
                if (this.downloaderType == DownloaderType.沒有任何檔案下載中)
                {
                    //ThreadPool.QueueUserWorkItem(callback => { this.Start(); });
                    Task.Factory.StartNew(() => this.Start(), TaskCreationOptions.LongRunning); 
                }
            }
        }

        public void AddItem(Law_DownloadItemViewModel lawItem)
        {
           

            lock (this)
            {
                if (lawItem == null)
                    return;
                list.Add(lawItem);
                //LawDownloader lawDownloader = Singleton_LawDownloader.GetInstance();
                // 如果沒有在下載中
                if (this.downloaderType == DownloaderType.沒有任何檔案下載中)
                {
                    //ThreadPool.QueueUserWorkItem(callback => { this.Start(); });
                    Task.Factory.StartNew(() => this.Start(), TaskCreationOptions.LongRunning); 
                }
            }

          
        }

        public void AddItem(List<Law_DownloadItemViewModel> lawItemList)
        {
            lock (this)
            {
                list.AddRange(lawItemList);
                //FileDownloader fileDownloader = Singleton_FileDownloader.GetInstance(fileItem.MeetingID);
                // 如果沒有在下載中
                if (this.downloaderType == DownloaderType.沒有任何檔案下載中)
                {
                    //ThreadPool.QueueUserWorkItem(callback => { this.Start(); });
                    Task.Factory.StartNew(() => this.Start());//, TaskCreationOptions.LongRunning); 
                }
            }

        }

        public void Pause(string lawItem_ID)
        {
            lock (this)
            {
                // 要先移除還沒下載的才暫停
                list.RemoveAll(x => x.ID.Equals(lawItem_ID));

                if (NowLawItem != null && NowLawItem.ID.Equals(lawItem_ID) == true)
                {
                    downloaderType = DownloaderType.暫停;
                }
            }

           
        }

        public void Stop()
        {
            lock (this)
            {

                list.Clear();

                if (this.downloaderType != DownloaderType.沒有任何檔案下載中)
                    downloaderType = DownloaderType.停止;
            }
        }

        public Law_DownloadItemViewModel GetInList(string lawDataLaw_ID)
        {
            lock (this)
            {
                if (NowLawItem != null && NowLawItem.ID.Equals(lawDataLaw_ID))
                {
                    return NowLawItem;
                }
                else //不包含暫停的下載物件，因為已經從list取出了，所以會回傳null
                {
                    return list.Where(x => x.ID.Equals(lawDataLaw_ID)).FirstOrDefault();
                }
            }
        }

        private double GetPercent(double downloadBytes, double totalBytes)
        {
            double percentage = 0.0;
            if (totalBytes > 0)
                percentage = downloadBytes * 100 / totalBytes;

            return percentage;
        }

        private void DownloadFileStart(Law_DownloadItemViewModel lawItem)
        {
            if (lawItem == null)
                return;

            if(lawItem.StorageFileName.EndsWith(".update"))
                lawItem.FileType = LawFileType.更新檔正在下載中;
            else
                lawItem.FileType = LawFileType.正在下載中;

            //呼叫完成event並且在主執行序執行
            if (LawListCT_DownloadFileStart_Event != null)
                LawListCT_DownloadFileStart_Event(lawItem);


        }


        private void DownloadProgressChanged(Law_DownloadItemViewModel lawItem)
        {
            if (lawItem == null)
                return;

            //呼叫完成event並且在主執行序執行
            if (LawListCT_DownloadProgressChanged_Event!=null)
                LawListCT_DownloadProgressChanged_Event(lawItem);
           
        }

        private void DownloadFileCompleted(Law_DownloadItemViewModel lawItem)
        {
            if (lawItem == null)
                return;


            if (lawItem.StorageFileName.EndsWith(".update"))
            {
                lawItem.FileType = LawFileType.更新檔已下載完成;
                //複寫原本的檔案
                File.Move(lawItem.StorageFilePath, lawItem.StorageFilePath.Replace(".update",""));
                lawItem.StorageFileName = lawItem.StorageFileName.Replace(".update", "");
            }
            else
                lawItem.FileType = LawFileType.已下載完成;


            // 解壓縮，可以開thread去解壓縮，這樣就會直接下載下一個
            // 不過不要這樣做，因為nowItem會直接變成下一個下載物件，就不能判斷現在的下載狀態了
            // 因為使用者會隨時重新整理頁面
            //ThreadPool.QueueUserWorkItem(callback =>
            //{
            //    UnzipTrigger(lawItem);
            //});

            //解壓縮
            UnzipTrigger(lawItem);

            if (LawListCT_DownloadFileCompleted_Event != null)
                LawListCT_DownloadFileCompleted_Event(lawItem);

            string SQL=@"update  LawRow set AtDownloadFinished_XmlUpDate=@1
                       , Link=@2,StorageFileName=@3
                       , DownloadBytes=@4,TotalBytes=@5 where ID=@6 and UserID=@7";

            int success = MSCE.ExecuteNonQuery( SQL
                                               , lawItem.UpDate.ToString("yyyy/MM/dd HH:mm:ss")
                                               , lawItem.Link
                                               , lawItem.StorageFileName
                                               , lawItem.DownloadBytes.ToString()
                                               , lawItem.TotalBytes.ToString()
                                               , lawItem.ID
                                               , lawItem.UserID);
            if (success < 1 )
                LogTool.Debug(new Exception("DB失敗:" + SQL));

        }

        private void UnzipTrigger(Law_DownloadItemViewModel lawItem)
        {
            if (lawItem == null)
                return;

            if (lawItem.FileType == LawFileType.已下載完成)
            {
                lawItem.FileType = LawFileType.解壓縮中;
            }
            else //LawFileType.更新檔已下載完成
            {
                lawItem.FileType = LawFileType.更新檔解壓縮中;
            }


            bool success=false;

            if (LawListCT_UnZip_Event != null)
                LawListCT_UnZip_Event(lawItem);

            try
            {
                UnZipTool uz = new UnZipTool();
                success = uz.UnZip(lawItem.StorageFilePath, lawItem.UnZipFilePath, "", true);
            }
            catch(Exception ex)
            {
                LogTool.Debug(ex);
                // 解壓縮失敗
            }

            // 解壓縮成功
            if (success == true)
            {
              
                // 一般檔案解壓縮中
                if (lawItem.FileType == LawFileType.解壓縮中)
                    lawItem.FileType = LawFileType.已下載完成;
                else // 更新檔解壓縮中
                    lawItem.FileType = LawFileType.更新檔已下載完成;

               
            }
            else // 解壓縮失敗
            {
                // 一般檔案解壓縮失敗
                // 只有一般檔案解壓縮失敗才要寫DB
                if (lawItem.FileType == LawFileType.解壓縮中)
                {
                    lawItem.DownloadBytes = 0;
                    lawItem.TotalBytes = 0;
                    lawItem.LastPercentage = 0;

                    if (File.Exists(lawItem.StorageFilePath) == true)
                        File.Delete(lawItem.StorageFilePath);
                    //寫DB
                    string SQL = @"update  LawRow set AtDownloadFinished_XmlUpDate=@1
                                           , Link=@2,StorageFileName=@3
                                           , DownloadBytes=@4,TotalBytes=@5 where ID=@6 and UserID=@7";
                    int successNum = MSCE.ExecuteNonQuery(SQL
                                                       , lawItem.UpDate.ToString("yyyy/MM/dd HH:mm:ss")
                                                       , lawItem.Link
                                                       , lawItem.StorageFileName
                                                       , lawItem.DownloadBytes.ToString()
                                                       , lawItem.TotalBytes.ToString()
                                                       , lawItem.ID
                                                       , lawItem.UserID);

                    if (successNum < 1)
                        LogTool.Debug(new Exception("DB失敗:" + SQL));
                }
                else
                {
                    // 更新檔解壓縮失敗
                    // 不寫DB
                }

                // 一般檔案解壓縮失敗
                if (lawItem.FileType == LawFileType.解壓縮中)
                {
                    lawItem.FileType = LawFileType.解壓縮失敗;
                    if (LawListCT_UnZipError_Event != null)
                        LawListCT_UnZipError_Event(lawItem);

                    if (Home_UnZipError_Event != null)
                    {
                        Home_UnZipError_Event(string.Format("{0} {1}"
                                                , PaperLess_Emeeting.Properties.Settings.Default.LawButtonName
                                                , Enum.GetName(typeof(LawFileType), LawFileType.解壓縮失敗)));
                    }
                    lawItem.FileType = LawFileType.從未下載;
                }
                else // 更新的檔案解壓縮失敗
                {
                    lawItem.FileType = LawFileType.更新檔解壓縮失敗;
                    if (LawListCT_UnZipError_Event != null)
                        LawListCT_UnZipError_Event(lawItem);

                    if (Home_UnZipError_Event != null)
                    {
                        Home_UnZipError_Event(string.Format("檔名: {0}，{1}"
                                                , PaperLess_Emeeting.Properties.Settings.Default.LawButtonName
                                                , Enum.GetName(typeof(LawFileType), LawFileType.更新檔解壓縮失敗)));
                        
                    }
                    lawItem.FileType = LawFileType.更新檔未下載;
                }
               
            }


        }
       

       
    }
}
