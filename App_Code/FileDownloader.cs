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
    public delegate void MeetingDataCT_DownloadFileStart_Function(File_DownloadItemViewModel obj);
    public delegate void MeetingDataCT_DownloadProgressChanged_Function(File_DownloadItemViewModel obj);
    public delegate void MeetingDataCT_DownloadFileCompleted_Function(File_DownloadItemViewModel obj);
    //public delegate void MeetingDataCT_DownloadFilePause_Function(File_DownloadItemViewModel obj);

    public delegate void MeetingDataCT_UnZip_Function(File_DownloadItemViewModel obj);
    public delegate void MeetingDataCT_UnZipError_Function(File_DownloadItemViewModel obj);
    public delegate void MeetingDataCT_DownloadError_Function(File_DownloadItemViewModel obj);

    public delegate void MeetingRoom_DownloadFileStart_Function(File_DownloadItemViewModel obj);
    public delegate void MeetingRoom_DownloadProgressChanged_Function(File_DownloadItemViewModel obj,bool ForceUpdate=false);
    public delegate void MeetingRoom_DownloadFileToErrorCompleted_Function(File_DownloadItemViewModel obj);


    //public delegate void Home_UnZipError_Function(string message);

    public class FileDownloader
    {
        // 多執行緒，lock 使用
        private static readonly object thisLock = new object();

        public event MeetingDataCT_DownloadFileStart_Function MeetingDataCT_DownloadFileStart_Event;
        public event MeetingDataCT_DownloadProgressChanged_Function MeetingDataCT_DownloadProgressChanged_Event;
        public event MeetingDataCT_DownloadFileCompleted_Function MeetingDataCT_DownloadFileCompleted_Event;

        public event MeetingDataCT_UnZip_Function MeetingDataCT_UnZip_Event;
        public event MeetingDataCT_UnZipError_Function MeetingDataCT_UnZipError_Event;
        public event MeetingDataCT_DownloadError_Function MeetingDataCT_DownloadError_Event;

        public event MeetingRoom_DownloadFileStart_Function MeetingRoom_DownloadFileStart_Event;
        public event MeetingRoom_DownloadProgressChanged_Function MeetingRoom_DownloadProgressChanged_Event;
        public event MeetingRoom_DownloadFileToErrorCompleted_Function MeetingRoom_DownloadFileToErrorCompleted_Event;

        public event Home_UnZipError_Function Home_UnZipError_Event;

        private int buffer = 1024 * 1000; //1MB
        private List<File_DownloadItemViewModel> list = new List<File_DownloadItemViewModel>();

        public File_DownloadItemViewModel NowFileItem { get; private set; }
        public DownloaderType downloaderType { get; private set; }
        //public string MeetingID { get; set; }

        //建構子
        public FileDownloader()
        {
            //this.MeetingID = MeetingID;
            this.downloaderType = DownloaderType.沒有任何檔案下載中;
            this.NowFileItem = null;
            // 依設定檔改變buffer
            int KB = PaperLess_Emeeting.Properties.Settings.Default.DownloadBuffer_KB;
            buffer = 1024 * KB;
        }

        public void ClearHomeEvent()
        {
            Home_UnZipError_Event = null;
        }

        public void ClearMeetingDataCTEvent()
        {
            MeetingDataCT_DownloadFileStart_Event = null;
            MeetingDataCT_DownloadProgressChanged_Event = null;
            MeetingDataCT_DownloadFileCompleted_Event = null;

            MeetingDataCT_UnZip_Event = null;
            MeetingDataCT_UnZipError_Event = null;

        }

        public void ClearMeetingRoomEvent()
        {
            MeetingRoom_DownloadFileStart_Event = null;
            MeetingRoom_DownloadProgressChanged_Event = null;
            MeetingRoom_DownloadFileToErrorCompleted_Event = null;
        }

        public bool HasMeetingRoom_DownloadFileStart_Event()
        {
            if(MeetingRoom_DownloadFileStart_Event != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool HasMeetingRoom_DownloadProgressChanged_Event()
        {
            if (MeetingRoom_DownloadProgressChanged_Event != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool HasMeetingRoom_DownloadFileToErrorCompleted_Event()
        {
            if (MeetingRoom_DownloadFileToErrorCompleted_Event != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public void ClearAllEvent()
        {
             Home_UnZipError_Event = null;

             MeetingDataCT_DownloadFileStart_Event = null;
             MeetingDataCT_DownloadProgressChanged_Event = null;
             MeetingDataCT_DownloadFileCompleted_Event = null;
             MeetingDataCT_UnZip_Event = null;
             MeetingDataCT_UnZipError_Event = null;

             MeetingRoom_DownloadFileStart_Event = null;
             MeetingRoom_DownloadProgressChanged_Event = null;
             MeetingRoom_DownloadFileToErrorCompleted_Event = null;
        }

        //這裡的Start請開執行序去跑
        public void Start()
        {
            downloaderType = DownloaderType.正在下載中;
            try
            {
                if (list.Count > 0)
                {
                    lock (thisLock)
                    {
                        if (list.Count > 0)
                        {
                            NowFileItem = list[0];
                            list.Remove(NowFileItem);
                        }
                    }
                    if (NowFileItem != null)
                    {
                        DownloadFileStart(NowFileItem); //開始

                        HttpWebRequest request = (HttpWebRequest)FileWebRequest.Create(NowFileItem.Url);
                        //Wayne 20150429
                        request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.Revalidate);
                        request.Proxy = null;
                        request.AutomaticDecompression = DecompressionMethods.GZip;
                        // 不要使用這個，取得或設定值，指出是否要分區段傳送資料至網際網路資源。
                        //request.SendChunked = true;
                        if (NowFileItem.DownloadBytes > 0)
                        {
                            if (File.Exists(NowFileItem.StorageFilePath) == true)
                            {
                                NowFileItem.DownloadBytes = new FileInfo(NowFileItem.StorageFilePath).Length;
                                if (NowFileItem.DownloadBytes >= NowFileItem.TotalBytes)
                                {
                                    downloaderType = DownloaderType.檔案下載完成;
                                    // 更新資料
                                    //UpdateToDB(NowFileItem);
                                    goto TypeControl;
                                }
                                else
                                {
                                    request.AddRange(NowFileItem.DownloadBytes);
                                    //UpdateToDB(NowFileItem);
                                }
                            }
                            else
                            {
                                request.AddRange(NowFileItem.DownloadBytes);
                                // 更新資料
                                //UpdateToDB(NowFileItem);
                            }
               
                        }

                        //取得回應
                        WebResponse response = request.GetResponse();
                       
                        if (NowFileItem.TotalBytes==0)
                            NowFileItem.TotalBytes = response.ContentLength;

                        //先建立檔案要存放的資料夾
                        Directory.CreateDirectory(NowFileItem.StorageFileFolder);

                        //FileMode fileMode = FileMode.Create;
                        //if (File.Exists(NowFileItem.StorageFilePath) == true)
                        //{
                        //    fileMode = FileMode.Open;
                        //}

                        using (var writer = new FileStream(NowFileItem.StorageFilePath, FileMode.OpenOrCreate))
                        {
                            try
                            {
                                writer.Seek(0, SeekOrigin.End);
                                using (var stream = response.GetResponseStream())
                                {
                                    while (NowFileItem.DownloadBytes < NowFileItem.TotalBytes)  //暫停條件設定成 fileItem的屬性 IsPause =true 就停止
                                    {
                                        // 暫停和停止動作一定要寫在while裡
                                        if (downloaderType == DownloaderType.暫停 || downloaderType == DownloaderType.停止)
                                        {
                                            // 可存可不存，各有優缺點
                                            // 存起來的話缺點為user暫停後，馬上重整可能會多1%進度
                                            // 不存的話不會有以上缺點，因為AddRange是從FileStream裡面讀取的所以不影響
                                            UpdateToDB(NowFileItem);
                                            break;
                                        }

                                        byte[] data = new byte[buffer];
                                        int readNumber = stream.Read(data, 0, data.Length);
                                      
                                        if (readNumber > 0)
                                        {
                                            writer.Write(data, 0, readNumber);
                                            writer.Flush();
                                            NowFileItem.DownloadBytes += readNumber;
                                        }

                                        if (NowFileItem.DownloadBytes >= NowFileItem.TotalBytes)
                                        {
                                            //先寫入檔案，並且關閉檔案使用權。
                                            downloaderType = DownloaderType.檔案下載完成;
                                            //UpdateToDB(NowFileItem);
                                            break;
                                        }
                                        else
                                        {
                                          

                                            if (downloaderType == DownloaderType.暫停 || downloaderType == DownloaderType.停止)
                                            {
                                                UpdateToDB(NowFileItem);
                                                break;
                                            }
                                            else
                                            {
                                                double percentage = GetPercent(NowFileItem.DownloadBytes, NowFileItem.TotalBytes);

                                                // 加速MeetingRoom的事件接收，才不會讓MeetingRoom的下載事件開始的觸發
                                                // 被下面的1%的限制給延後觸發MeetingRoom的開始下載。
                                                if (MeetingRoom_DownloadFileStart_Event != null)
                                                    MeetingRoom_DownloadFileStart_Event(NowFileItem);

                                                if (percentage - NowFileItem.LastPercentage > PaperLess_Emeeting.Properties.Settings.Default.Downloader_InvokePercent)
                                                {
                                                    NowFileItem.LastPercentage = percentage;
                                                    // 進度條百分比callback
                                                    DownloadProgressChanged(NowFileItem);
                                                    UpdateToDB(NowFileItem);
                                                    //Thread.Sleep(1);
                                                }
                                            }
                                                

                                            
                                        }
                                    }

                                    if (NowFileItem.DownloadBytes >= NowFileItem.TotalBytes)
                                    {
                                        DownloadProgressChanged(NowFileItem);
                                        //先寫入檔案，並且關閉檔案使用權。
                                        downloaderType = DownloaderType.檔案下載完成;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                downloaderType = DownloaderType.下載出錯;
                                DownloadError(NowFileItem);
                                LogTool.Debug(ex);
                            }
                           
                        }
                    }
                    else
                    {
                        downloaderType = DownloaderType.沒有任何檔案下載中;
                    }
                }
                else
                {
                    downloaderType = DownloaderType.沒有任何檔案下載中;
                }

            }
            catch (Exception ex)
            {
                downloaderType = DownloaderType.下載出錯;
                DownloadError(NowFileItem);
                LogTool.Debug(ex);
            }


          
            TypeControl:
            // 在這裡統整所有的控制行為 錯誤,暫停,停止,完成
            switch (downloaderType)
            {
                case DownloaderType.沒有任何檔案下載中:
                    //StartNextFileItemDownload(NowFileItem);
                    break;
                case DownloaderType.停止:
                    // 呼叫 Stop callback
                    // 停止可以再呼叫一次StartNextFileItemDownload(NowFileItem);
                    // 避免下載器的狀態跑到停止後，可能會沒有被重置成 DownloaderType.沒有任何檔案下載中;
                    //NowFileItem = null;
                    //lock (thisLock)
                    //{
                    //    list.Clear();
                    //}
                    //downloaderType = DownloaderType.沒有任何檔案下載中;
                    StartNextFileItemDownload(NowFileItem);
                    // 不要開新的下載
                    break;
                case DownloaderType.下載出錯:
                    // 呼叫 Error callback
                    // 儲存下載狀態
                    downloaderType = DownloaderType.沒有任何檔案下載中;
                    StartNextFileItemDownload(NowFileItem);
                    break;
                case DownloaderType.暫停:
                    // 呼叫 Pause callback
                    StartNextFileItemDownload(NowFileItem);
                    break;
                case DownloaderType.檔案下載完成:
                    // 呼叫 FileCompleted callback
                    DownloadFileCompleted(NowFileItem);
                    StartNextFileItemDownload(NowFileItem);
                    break;

            }
        }

        private void DownloadError(File_DownloadItemViewModel fileItem)
        {
            if (fileItem == null)
                return;

            DeleteFiles(fileItem);
            ResetFileItemDB(fileItem);
          

            if (MeetingDataCT_DownloadError_Event != null)
            {
                MeetingDataCT_DownloadError_Event(fileItem);
            }
            if (Home_UnZipError_Event != null)
            {
                Home_UnZipError_Event(string.Format("檔名: {0}，{1}"
                       , NowFileItem.FileName == null ? "" : NowFileItem.FileName
                       , Enum.GetName(typeof(DownloaderType), DownloaderType.下載出錯)));
                Thread.Sleep(1100);
            }

            // 要記得Check現在的下載佇列List是否為0
            // 也就是最後一個下載，但是下載失敗了，就要呼叫改變MeetingRoom變成暫停的狀態
            bool IsLastFileItemFinishedButError = false;

            lock (thisLock)
            {
                if (list.Count == 0)
                {
                    IsLastFileItemFinishedButError = true;
                }
            }

            if (IsLastFileItemFinishedButError == true)
            {
                if (MeetingRoom_DownloadFileToErrorCompleted_Event != null)
                {
                    MeetingRoom_DownloadFileToErrorCompleted_Event(fileItem);
                }
            }

        }

        private void UpdateToDB(File_DownloadItemViewModel fileItem,bool IsFinished=false)
        {
            if (fileItem == null)
                return;

            string SQL = "";
            int success=0;
            try
            {
                if (IsFinished == true)
                {
                    SQL = @"update FileRow set Url=@1,StorageFileName=@2
                           , DownloadBytes=@3,TotalBytes=@4 ,FileVersion=@5,FinishedFileVersion=@6 where ID=@7 and UserID=@8 and MeetingID=@9";

                    fileItem.CanUpdate = false;
                    success = MSCE.ExecuteNonQuery(SQL
                                                  , fileItem.Url
                                                  , fileItem.StorageFileName
                                                  , fileItem.DownloadBytes.ToString()
                                                  , fileItem.TotalBytes.ToString()
                                                  , fileItem.FileVersion.ToString()
                                                  , fileItem.FileVersion.ToString()
                                                  , fileItem.ID
                                                  , fileItem.UserID
                                                  , fileItem.MeetingID);

                
                }
                else
                {
                    SQL=@"update FileRow set Url=@1,StorageFileName=@2
                           , DownloadBytes=@3,TotalBytes=@4 where ID=@6 and UserID=@7 and MeetingID=@8";
                    success = MSCE.ExecuteNonQuery(SQL
                                                  , fileItem.Url
                                                  , fileItem.StorageFileName
                                                  , fileItem.DownloadBytes.ToString()
                                                  , fileItem.TotalBytes.ToString()
                                                  , fileItem.ID
                                                  , fileItem.UserID
                                                  , fileItem.MeetingID);
                }
            }
            catch(Exception ex)
            {
                LogTool.Debug(ex);
            }

           
            if (success < 1)
                LogTool.Debug(new Exception("DB失敗:" + SQL));
        }

      
        private void StartNextFileItemDownload(File_DownloadItemViewModel fileItem)
        {
            if (downloaderType == DownloaderType.停止)
            {
                // 停止之後不能馬上加入不然
                // 有可能會再開新執行序下載
                // 清空所有列表
                lock (thisLock)
                {
                    list.Clear();
                }
            }
            // 設定成 沒有正在下載 和 沒有正在下載的物件
            //string MeetingID = fileItem.MeetingID;
            NowFileItem = null;
            downloaderType = DownloaderType.沒有任何檔案下載中;
            // 不同線程的回呼，不會造成Recursive堆疊上限
            // 如果不是停止就開始新的檔案物件下載
                

            //FileDownloader fileDownloader = Singleton_FileDownloader.GetInstance(MeetingID);
            if (this.downloaderType == DownloaderType.沒有任何檔案下載中)
            {
                //ThreadPool.QueueUserWorkItem(callback => { this.Start(); });
                Task.Factory.StartNew(() => this.Start(), TaskCreationOptions.LongRunning);
            }
            
        }

        public void AddItem(File_DownloadItemViewModel fileItem)
        {
            lock (this)
            {
                if (fileItem == null)
                    return;
                list.Add(fileItem);
            }

            //FileDownloader fileDownloader = Singleton_FileDownloader.GetInstance(fileItem.MeetingID);
            // 如果沒有在下載中
            if (this.downloaderType == DownloaderType.沒有任何檔案下載中 || this.downloaderType == DownloaderType.停止)
            {
                //ThreadPool.QueueUserWorkItem(callback => { this.Start(); });
                Task.Factory.StartNew(() => this.Start());//, TaskCreationOptions.LongRunning); 
            }   
         
           
        }

        public void AddItem(List<File_DownloadItemViewModel> fileItemList)
        {
            lock (this)
            {
                list.AddRange(fileItemList);
            }

            //FileDownloader fileDownloader = Singleton_FileDownloader.GetInstance(fileItem.MeetingID);
            // 如果沒有在下載中
            if (this.downloaderType == DownloaderType.沒有任何檔案下載中 || this.downloaderType == DownloaderType.停止)
            {
                //ThreadPool.QueueUserWorkItem(callback => { this.Start(); });
                Task.Factory.StartNew(() => this.Start(), TaskCreationOptions.LongRunning);
            }

        }


        public void Pause(string fileItem_ID)
        {
            lock (this)
            {
                // 要先移除還沒下載的才暫停
                list.RemoveAll(x => x.ID.Equals(fileItem_ID));

                if (NowFileItem != null && NowFileItem.ID.Equals(fileItem_ID) == true)
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
            }

            if (this.downloaderType == DownloaderType.正在下載中)
            {
                downloaderType = DownloaderType.停止;
            }    
        }


        public File_DownloadItemViewModel GetInList(string MeetingDataDownloadFileFile_ID)
        {
            //if (NowFileItem == null)
            //    return list.Where(x => x.ID.Equals(MeetingDataDownloadFileFile_ID)).FirstOrDefault();
            File_DownloadItemViewModel rtn = new File_DownloadItemViewModel();
            lock (this)
            {
                if (NowFileItem != null && NowFileItem.ID.Equals(MeetingDataDownloadFileFile_ID))
                {
                    rtn = NowFileItem;
                }
                else //不包含暫停的下載物件，因為已經從list取出了，所以會回傳null
                {
                    rtn = list.Where(x => x.ID.Equals(MeetingDataDownloadFileFile_ID)).FirstOrDefault();
                }
            }
            return rtn;
        }

        public List<File_DownloadItemViewModel> GetNotInList(List<File_DownloadItemViewModel>  fileItemList)
        {
            lock (this)
            {
                if (NowFileItem != null)
                {
                    fileItemList.RemoveAll(x => x.ID.Equals(NowFileItem));
                }

                try
                {
                    //list.ForEach(item => 
                    foreach (File_DownloadItemViewModel item in list)
                    {
                        fileItemList.RemoveAll(x => x.ID.Equals(item.ID));
                    }//);
                }
                catch { }
                return fileItemList;
            }

        }

        private double GetPercent(double downloadBytes, double totalBytes)
        {
            double percentage = 0.0;
            if (totalBytes > 0)
                percentage = downloadBytes * 100 / totalBytes;

            return percentage;
        }

        private void DownloadFileStart(File_DownloadItemViewModel fileItem)
        {
            if (fileItem == null)
                return;


            //呼叫完成event並且在主執行序執行
            if (MeetingDataCT_DownloadFileStart_Event != null)
                MeetingDataCT_DownloadFileStart_Event(fileItem);

            if (MeetingRoom_DownloadFileStart_Event != null)
                MeetingRoom_DownloadFileStart_Event(NowFileItem);
        }


        private void DownloadProgressChanged(File_DownloadItemViewModel fileItem)
        {
            if (fileItem == null)
                return;

            //呼叫完成event並且在主執行序執行
            if (MeetingDataCT_DownloadProgressChanged_Event!=null)
                MeetingDataCT_DownloadProgressChanged_Event(fileItem);

            if (MeetingRoom_DownloadProgressChanged_Event != null)
                MeetingRoom_DownloadProgressChanged_Event(fileItem);

            //  讓事件得以舒緩，加到100毫秒似乎也差異不大。
            //  一樣會卡卡的。
            //Thread.Sleep(1);
        }

        private void DownloadFileCompleted(File_DownloadItemViewModel fileItem)
        {
            if (fileItem == null)
                return;

            //// 儲存下載狀態
            //UpdateToDB(fileItem);

            switch (fileItem.FileCate)
            {
                case MeetingFileCate.電子書:
                case MeetingFileCate.Html5投影片:
                    //解壓縮
                    UnzipTrigger(fileItem);
                    break;
                case MeetingFileCate.影片檔:
                    try
                    {
                        if (File.Exists(fileItem.StorageFilePath) == true)
                        {
                            Directory.CreateDirectory(fileItem.UnZipFilePath);
                            File.Copy(fileItem.StorageFilePath, fileItem.UnZipFilePath + "\\" + fileItem.StorageFileName, true);
                        }
                    }
                    catch(Exception ex)
                    {
                        LogTool.Debug(ex);
                    }

                    // 儲存下載狀態
                    UpdateToDB(fileItem,true);

                  

                    break;
            }

            if (MeetingDataCT_DownloadFileCompleted_Event != null)
                MeetingDataCT_DownloadFileCompleted_Event(fileItem);

            if (MeetingRoom_DownloadProgressChanged_Event != null)
                MeetingRoom_DownloadProgressChanged_Event(fileItem);

            #region 過時
            
//            fileItem.FileType = MeetingFileType.已下載完成;

//            //解壓縮
//            UnzipTrigger(fileItem);


//            string SQL = @"update  FileRow set Url=@1,StorageFileName=@2
//                       , DownloadBytes=@3,TotalBytes=@4,NowPercentage=@5 where ID=@6 and UserID=@7 and MeetingID=@8";

//            int success = MSCE.ExecuteNonQuery(SQL
//                                               , fileItem.Url
//                                               , fileItem.StorageFileName
//                                               , fileItem.DownloadBytes.ToString()
//                                               , fileItem.TotalBytes.ToString()
//                                               , fileItem.NowPercentage.ToString()
//                                               , fileItem.ID
//                                               , fileItem.UserID
//                                               , fileItem.MeetingID);
//            if (success < 1)
//                LogTool.Debug(new Exception("DB失敗:" + SQL));
             

            #endregion

        }

        private void UnzipTrigger(File_DownloadItemViewModel fileItem)
        {

            if (fileItem == null)
                return;

            bool success=false;

            if (MeetingDataCT_UnZip_Event != null)
                MeetingDataCT_UnZip_Event(fileItem);

            // 解壓縮時會自動檢查目的地資料是否存在
            // 所以不用檢查了
            // Directory.CreateDirectory(fileItem.UnZipFilePath);
            try
            {
                UnZipTool uz = new UnZipTool();
                success = uz.UnZip(fileItem.StorageFilePath, fileItem.UnZipFilePath, "", true);
            }
            catch(Exception ex)
            {
                LogTool.Debug(ex);
            }
            if (success == true)
            {
                // 儲存下載狀態
                UpdateToDB(fileItem,true);

                //  解壓縮成功
                fileItem.FileType = MeetingFileType.已下載完成;

                //if (MeetingDataCT_DownloadFileCompleted_Event != null)
                //    MeetingDataCT_DownloadFileCompleted_Event(fileItem);

                //if (MeetingRoom_DownloadProgressChanged_Event != null)
                //    MeetingRoom_DownloadProgressChanged_Event(fileItem, false);
            }
            else
            {

                ResetFileItemDB(fileItem);
               

                fileItem.FileType = MeetingFileType.解壓縮失敗;

                // 解壓縮失敗，要記得把原始檔案和解壓縮資料夾一起刪除
                DeleteFiles(fileItem);
               
                if (MeetingDataCT_UnZipError_Event != null)
                    MeetingDataCT_UnZipError_Event(fileItem);

                if (Home_UnZipError_Event != null)
                {
                    Home_UnZipError_Event(string.Format("檔名: {0}，{1}"
                                                 , fileItem.FileName
                                                 , Enum.GetName(typeof(MeetingFileType), MeetingFileType.解壓縮失敗)));
                }
            }


        }

        private void DeleteFiles(File_DownloadItemViewModel fileItem)
        {
            // 解壓縮失敗，要記得把原始檔案和解壓縮資料夾一起刪除
            if (File.Exists(fileItem.StorageFilePath) == true)
            {
                File.Delete(fileItem.StorageFilePath);
            }

            DirectoryTool.FullDeleteDirectories(fileItem.UnZipFilePath);
        }

        private void ResetFileItemDB(File_DownloadItemViewModel fileItem)
        {
            if (fileItem == null)
                return;

            fileItem.DownloadBytes = 0;
            fileItem.TotalBytes = 0;
            fileItem.LastPercentage = 0;

            if (File.Exists(fileItem.StorageFilePath) == true)
                File.Delete(fileItem.StorageFilePath);
            //寫DB
            string SQL = @"update  FileRow set Url=@1,StorageFileName=@2
                       , DownloadBytes=@3,TotalBytes=@4 where ID=@6 and UserID=@7 and MeetingID=@8";

            int successNum = MSCE.ExecuteNonQuery(SQL
                                           , fileItem.Url
                                           , fileItem.StorageFileName
                                           , fileItem.DownloadBytes.ToString()
                                           , fileItem.TotalBytes.ToString()
                                           , fileItem.ID
                                           , fileItem.UserID
                                           , fileItem.MeetingID);

            if (successNum < 1)
                LogTool.Debug(new Exception("DB失敗:" + SQL));
        }

       
       
    }
}
