using AES_ECB_PKCS5;
using BookManagerModule;
using DataAccessObject;
using PaperLess_Emeeting.App_Code;
using PaperLess_Emeeting.App_Code.ClickOnce;
using PaperLess_Emeeting.App_Code.DownloadItem;
using PaperLess_Emeeting.App_Code.MessageBox;
using PaperLess_Emeeting.App_Code.Socket;
using PaperLess_Emeeting.App_Code.ViewModel;
using PaperLess_Emeeting.App_Code.WS;
using PaperLess_ViewModel;
using PaperlessSync.Broadcast.Service;
using PaperlessSync.Broadcast.Socket;
using ReadPageModule;
using SyncCenterModule;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PaperLess_Emeeting
{
    public delegate void MeetingDataCT_RaiseAllDownload_Function(string LastFileItemID, bool IsAutoUpdate, bool DoNotChangeAlready_RaiseAllDownload);
    public delegate void MeetingDataCT_HangTheDownloadEvent_Function(string LastFileItemID);
    public delegate bool MeetingDataCT_IsAllFileRowFinished_AddInitUIFinished_Function();
    public delegate string MeetingDataCT_GetWatermark_Function();
    
    public delegate Dictionary<string,BookVM> MeetingDataCT_GetBookVMs_ByMeetingFileCate_Function(File_DownloadItemViewModel fileItem);

    public delegate void MeetingDataCT_Counting_Finished_FileCount_Function();
    //public delegate bool FromReaderWindow_ChangeBook_Function(string FileID);

  
    /// <summary>
    /// FileRow.xaml 的互動邏輯
    /// </summary>
    public partial class FileRow : UserControl
    {
        public MeetingDataDownloadFileFile meetingDataDownloadFileFile { get; set; }
        public int index { get; set; }
        public File_DownloadItemViewModel fileItem { get; set; }
        public string UserID { get; set; }
        public string UserName { get; set; }
        public string UserPWD { get; set; }
        public string MeetingID { get; set; }
        public string UserEmail { get; set; }
        public bool IsLastRow { get; set; }
        Storyboard sb;
        MeetingDataCT_RaiseAllDownload_Function MeetingDataCT_RaiseAllDownload_Event;
        MeetingDataCT_HangTheDownloadEvent_Function MeetingDataCT_HangTheDownloadEvent_Event;
        MeetingDataCT_IsAllFileRowFinished_AddInitUIFinished_Function MeetingDataCT_IsAllFileRowFinished_AddInitUIFinished_Event;
        MeetingDataCT_GetBookVMs_ByMeetingFileCate_Function MeetingDataCT_GetBookVMs_ByMeetingFileCate_Event;
        MeetingDataCT_GetWatermark_Function MeetingDataCT_GetWatermark_Event;
        MeetingDataCT_Counting_Finished_FileCount_Function MeetingDataCT_Counting_Finished_FileCount_Event;
        public MeetingRoomButtonType meetingRoomButtonType { get; set; }
        public bool IsAllFileRowFinished = false;
        public bool IsWaitingForDownload = false;
        public string FolderID;
        public bool CanNotCollect = false;
        public FileRow(string UserID,string UserName,string UserPWD,string MeetingID,string UserEmail
                       , int index, bool IsLastRow, MeetingDataDownloadFileFile meetingDataDownloadFileFile
                       , MeetingDataCT_RaiseAllDownload_Function callback1
                       , MeetingDataCT_HangTheDownloadEvent_Function callback2
                       , MeetingDataCT_IsAllFileRowFinished_AddInitUIFinished_Function callback3
                       , MeetingDataCT_GetBookVMs_ByMeetingFileCate_Function callback4
                       , MeetingDataCT_GetWatermark_Function callback5
                       , MeetingRoomButtonType meetingRoomButtonType
                       , MeetingDataCT_Counting_Finished_FileCount_Function callback6)
        {
            //MouseTool.ShowLoading();
            InitializeComponent();
            this.UserID = UserID;
            this.UserName = UserName;
            this.UserPWD = UserPWD;
            this.MeetingID = MeetingID;
            this.UserEmail = UserEmail;
            this.index = index;
            this.IsLastRow = IsLastRow;
            this.meetingDataDownloadFileFile = meetingDataDownloadFileFile;
            this.MeetingDataCT_RaiseAllDownload_Event = callback1;
            this.MeetingDataCT_HangTheDownloadEvent_Event = callback2;
            this.MeetingDataCT_IsAllFileRowFinished_AddInitUIFinished_Event = callback3;
            this.MeetingDataCT_GetBookVMs_ByMeetingFileCate_Event = callback4;
            this.MeetingDataCT_GetWatermark_Event = callback5;
            this.MeetingDataCT_Counting_Finished_FileCount_Event = callback6;
            this.meetingRoomButtonType = meetingRoomButtonType;
            this.fileItem = null;
            this.Loaded += FileRow_Loaded;
            this.Unloaded += FileRow_Unloaded;
            //MouseTool.ShowArrow();
        }

        private void FileRow_Unloaded(object sender, RoutedEventArgs e)
        {
            this.MeetingDataCT_RaiseAllDownload_Event = null;
            this.MeetingDataCT_HangTheDownloadEvent_Event = null;
        }

        private void FileRow_Loaded(object sender, RoutedEventArgs e)
        {
            // [**超重要**]效能瓶頸在這裡。
            // 不要把InitSelectDB()，下面的兩行放到Thread去跑，會打死結。
            // 或是下面兩行用this.Dispatcher.BeginInvoke去跑，其餘的再開Thread
            // FileDownloader fileDownloader = Singleton_FileDownloader.GetInstance(MeetingID);
            // fileItem = fileDownloader.GetInList(meetingDataDownloadFileFile.ID);
            // InitSelectDB();

            InitSelectDB();

            //Task.Factory.StartNew(() =>
            //    {
            //        // 只要是 Row 列表內容畫面，優先權設定為Background => 列舉值為 4。 所有其他非閒置作業都完成之後，就會處理作業。
            //        // 另外這裡比較特別 因為優先權要比AgendaRow高，所以我設定為Input => 列舉值為 5。 做為輸入相同的優先權處理作業。
            //        //Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            //        this.Dispatcher.BeginInvoke(new Action(() =>
            //        {
            //            InitSelectDB();
            //            //InitUI();
            //            // 有下載UI相關的把事件放到主線成
            //            //InitEvent();
            //        }));
            //    });
        }

        private void InitSelectDB()
        {
            //Wayne 20150429
            //從InitUI()移動到這裡
            //if (PaperLess_Emeeting.Properties.Settings.Default.AssemblyName.Contains("TPI4F") == false)
            //{
                txtIndex.Text = index.ToString();
            //}
            //else
            //{
            //    Grid.SetColumn(txtFileName, 0);
            //    Grid.SetColumnSpan(txtFileName, 2);
            //    lineCenter.BorderBrush = Brushes.Transparent;
            //}
            txtFileName.Text = meetingDataDownloadFileFile.FileName;

            // 這裡要確保FileItem已將產生出來
            // 才能做UI
            FileDownloader fileDownloader = Singleton_FileDownloader.GetInstance(MeetingID);
            fileItem = fileDownloader.GetInList(meetingDataDownloadFileFile.ID);
            
            if (fileItem != null)
            {
                IsWaitingForDownload = true;

                Task.Factory.StartNew(() =>
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        InitUI();
                        // 有下載UI相關的把事件放到主線成
                        InitEvent();
                    }));
                });

                return;
            }
             

            // 取不到FileItem才去DB抓FileItem的狀態。
            Task.Factory.StartNew(() =>
            {
                fileItem = FileItemTool.Gen(meetingDataDownloadFileFile, UserID, MeetingID);
                if (fileItem.FileType == MeetingFileType.已下載完成)
                {
                    if (MeetingDataCT_Counting_Finished_FileCount_Event != null)
                    {
                        MeetingDataCT_Counting_Finished_FileCount_Event();
                    }
                }

            }).ContinueWith(task => {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    InitUI();
                    // 有下載UI相關的把事件放到主線成
                    InitEvent();
                }));
            });
                  


//            // 更新檔不支援續傳
//            // 檢查是否再下載當中
//            FileDownloader fileDownloader = Singleton_FileDownloader.GetInstance(MeetingID);
//            fileItem = fileDownloader.GetInList(meetingDataDownloadFileFile.ID);
//            if (fileItem != null)
//                return;

//            #region DB
//            string db_FileRowID = "";
//            string db_Url = "";
//            string db_StorageFileName = "";
//            long db_DownloadBytes = 0;
//            long db_TotalBytes = 0;
//            string SQL = "";
//            int success = 0;
//            DataTable dt = MSCE.GetDataTable("SELECT ID,Url,StorageFileName,DownloadBytes,TotalBytes FROM FileRow where ID=@1 and UserID=@2 and MeetingID=@3"
//                                   , meetingDataDownloadFileFile.ID
//                                   , UserID
//                                   , MeetingID);
//            if (dt.Rows.Count > 0)
//            {
//                db_FileRowID = dt.Rows[0]["ID"].ToString();
//                db_Url = dt.Rows[0]["Url"].ToString();
//                db_StorageFileName = dt.Rows[0]["StorageFileName"].ToString();
//                db_DownloadBytes = long.Parse(dt.Rows[0]["DownloadBytes"].ToString().Equals("") ? "0" : dt.Rows[0]["DownloadBytes"].ToString());
//                db_TotalBytes = long.Parse(dt.Rows[0]["TotalBytes"].ToString().Equals("") ? "0" : dt.Rows[0]["TotalBytes"].ToString());
//            }
//            else
//            {
//                SQL = @"INSERT INTO FileRow(ID,DownloadBytes,TotalBytes,UserID,MeetingID) 
//                                                    VALUES(@1,0,0,@2,@3)";
//                success = MSCE.ExecuteNonQuery(SQL
//                                                   , meetingDataDownloadFileFile.ID
//                                                   , UserID
//                                                   , MeetingID);
//                if (success < 1)
//                    LogTool.Debug(new Exception(@"DB失敗: " + SQL));
//            }
//            #endregion

//            fileItem = new File_DownloadItemViewModel();
//            fileItem.MeetingID = MeetingID;
//            fileItem.ID = meetingDataDownloadFileFile.ID;
//            fileItem.UserID = UserID;
//            fileItem.FileName = meetingDataDownloadFileFile.FileName;
//            fileItem.Url = meetingDataDownloadFileFile.Url;

//            string AppPath = System.AppDomain.CurrentDomain.BaseDirectory;
//            string File_StorageFileFolder = PaperLess_Emeeting.Properties.Settings.Default.File_StorageFileFolder;
//            fileItem.StorageFileFolder = System.IO.Path.Combine(AppPath, File_StorageFileFolder);

//            #region 取得 Http URL 的檔名
//            string fileName = DateTime.Now.ToFileTime().ToString();
//            try
//            {
//                Uri uri = new Uri(fileItem.Url);
//                string tempFileName = System.IO.Path.GetFileName(uri.LocalPath);
//                if (tempFileName.Equals(@"/") == false)
//                    fileName = tempFileName;
//            }
//            catch (Exception ex)
//            {
//                LogTool.Debug(ex);
//            }
//            #endregion

//            fileItem.StorageFileName = string.Format("{0}_{1}_{2}_{3}", UserID, MeetingID, meetingDataDownloadFileFile.ID, fileName);
//            fileItem.UnZipFileFolder = ClickOnceTool.GetFilePath()+"\\"+PaperLess_Emeeting.Properties.Settings.Default.File_UnZipFileFolder;
//            fileItem.DownloadBytes = db_DownloadBytes;
//            fileItem.TotalBytes = db_TotalBytes;

//            // 先檢查檔案存不存在
//            if (File.Exists(fileItem.StorageFilePath) == true)
//            {
//                // 未下載完成的
//                if (db_DownloadBytes == 0)
//                {
//                    // 刪除未下載完成但是檔案存在的，但是DB紀錄為沒有下載過的，或是被刪除的
//                    if (File.Exists(fileItem.StorageFilePath) == true)
//                        File.Delete(fileItem.StorageFilePath);

//                    fileItem.DownloadBytes = 0;
//                    fileItem.TotalBytes = 0;
//                    fileItem.FileType = MeetingFileType.從未下載;

//                }
//                else if (db_DownloadBytes < db_TotalBytes) //未下載完成的，有下載過的
//                {
//                    fileItem.DownloadBytes = db_DownloadBytes;
//                    fileItem.TotalBytes =  db_TotalBytes;
//                    fileItem.FileType = MeetingFileType.已下載過但是未完成的檔案;
//                }
//                else
//                {
//                    fileItem.FileType = MeetingFileType.已下載完成;
//                    //結束;
//                }
//            }
//            else
//            {
//                fileItem.DownloadBytes = 0;
//                fileItem.TotalBytes = 0;
//                fileItem.FileType = MeetingFileType.從未下載;
//            }

//            // 把DB的檔案資訊更新
//            SQL = @"update FileRow set DownloadBytes=@1,TotalBytes=@2,UserID=@3,MeetingID=@4 where ID=@5";
//            success = MSCE.ExecuteNonQuery(SQL
//                                           , fileItem.DownloadBytes.ToString()
//                                           , fileItem.TotalBytes.ToString()
//                                           , UserID
//                                           , MeetingID
//                                           , fileItem.ID);
//            if (success < 1)
//                LogTool.Debug(new Exception(@"DB失敗: " + SQL));

        }

        private void wc_FileRow_ChangeProgress_Event(int ProgressPercentage)
        {
            pb.Value = ProgressPercentage;
        }

        private void InitEvent()
        {
            btnDownload.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnDownload.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnDownload.MouseLeftButtonDown += btnDownload_MouseLeftButtonDown;

            btnOpen.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnOpen.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnOpen.MouseLeftButtonDown += btnOpen_MouseLeftButtonDown;

            btnPause.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnPause.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnPause.MouseLeftButtonDown += btnPause_MouseLeftButtonDown;

            btnPausing.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnPausing.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnPausing.MouseLeftButtonDown += btnPausing_MouseLeftButtonDown;

            if (fileItem.CanUpdate == true)
            {
                txtFileName.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
                txtFileName.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
                txtFileName.MouseLeftButtonDown += txtFileName_MouseLeftButtonDown;
            }

            //if (PaperLess_Emeeting.Properties.Settings.Default.AssemblyName.Contains("TPI4F") == true)
            //{
            //        txtIndex.ToolTip = "開啟PDF";
            //        txtIndex.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            //        txtIndex.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            //        txtIndex.MouseLeftButtonDown += (sender, e) =>
            //        {

            //            if (Singleton_PDFFactory.IsPDFInWork(fileItem.ID) == true)
            //            {
            //                AutoClosingMessageBox.Show("PDF轉檔中，請稍後");
            //                return;
            //            }

            //            string filePath = ClickOnceTool.GetFilePath();

            //            //string _bookPath = System.IO.Path.Combine(filePath, fileItem.UnZipFilePath);
            //            string UnZipFileFolder = PaperLess_Emeeting.Properties.Settings.Default.File_UnZipFileFolder;
            //            //string _bookPath = filePath + "\\" + UnZipFileFolder + "\\" + UserID + "\\" + MeetingID + "\\"+ fileItem.ID +"\\"+fileItem.FileVersion.ToString();
            //            // 等於上面那個路徑
            //            string _bookPath = fileItem.UnZipFilePath;

            //            // 從資料庫查詢上一次完成的檔案版本
            //            DataTable dt = MSCE.GetDataTable("SELECT FinishedFileVersion FROM FileRow where ID=@1 and UserID=@2 and MeetingID=@3"
            //                                    , meetingDataDownloadFileFile.ID
            //                                    , UserID
            //                                    , MeetingID);
            //            if (dt.Rows.Count > 0)
            //            {
            //                _bookPath = filePath + "\\" + UnZipFileFolder + "\\" + UserID + "\\" + MeetingID + "\\" + fileItem.ID + "\\" + dt.Rows[0]["FinishedFileVersion"].ToString();
            //            }

            //            string PdfPath = System.IO.Path.Combine(_bookPath, "PDF.pdf"); ;
            //            if (File.Exists(PdfPath) == false)
            //            {
            //                AutoClosingMessageBox.Show("請先瀏覽過書籍");
            //                return;
            //            }
            //            //在背景執行，無DOS視窗閃爍問題
            //            //System.Diagnostics.Process p = new System.Diagnostics.Process();
            //            //p.StartInfo.FileName = "cmd.exe";
            //            //p.StartInfo.Arguments = "/c "+ PdfPath;
            //            //p.StartInfo.UseShellExecute = false;
            //            //p.StartInfo.RedirectStandardInput = true;
            //            //p.StartInfo.RedirectStandardOutput = true;
            //            //p.StartInfo.RedirectStandardError = true;
            //            //p.StartInfo.CreateNoWindow = true;
            //            //p.Start();

            //            Process.Start(PdfPath);

            //        };
            //}

            Task.Factory.StartNew(() =>
                {
                   
                    // 重要觀念
                    // 這裡已經是非同步了
                    // 所以這裡的IsLastRow，雖然是最後一個
                    // 但是不一定是最後才載入完成的
                    // 所以會有狀態不一的問題發生。
                    // 最簡單的方法就是在下面加上Thread.Sleep(100);
                    // 或是Callback 回 MeetingDataCT
                    // 判斷已經載入完成InitUI的數量等於FileRow數量
                    // 不過要記得寫Callback
                    // 在這裡呼叫並且掛上MeetingDataCT的下載事件
                    //if (IsLastRow == true && MeetingDataCT_HangTheDownloadEvent_Event != null)
                    if (IsAllFileRowFinished == true && MeetingDataCT_HangTheDownloadEvent_Event != null)
                    {
                        // 先暫停一下，
                        // 慢一點再把自動下載的事件，加上去
                        //Thread.Sleep(100);
                        // 如果是自動下載，在這裡面判斷。

                        MeetingDataCT_HangTheDownloadEvent_Event(this.fileItem.ID);
                    }
                });

        }

        private void btnOpen_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CallOpenBook();
        }

        private void txtFileName_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CallOpenBook(true);
        }

        private int getBookPath(string dbPath, string bookId, string account, string meetingId)
        {
            int userBookSno = 0;
            try
            {
                BookManager bookManager = new BookManager(dbPath);
                userBookSno = getUserBookSno(dbPath, bookId, account, meetingId);
                if (userBookSno.Equals(-1))
                {
                    string query = "Insert into bookInfo( bookId, account, meetingId )";
                    query += " values('" + bookId + "', '" + account + "', '" + meetingId + "')";
                    bookManager.sqlCommandNonQuery(query);
                    
                    //sqlCommandNonQuery(query);
                    userBookSno = getBookPath(dbPath, bookId, account, meetingId);
                }
            }
            catch(Exception ex)
            {
                LogTool.Debug(ex);
            }


            return userBookSno;
        }

        public int getUserBookSno(string dbPath,string bookId, string account, string meetingId)
        {
          
            string query = "Select sno from bookInfo as bi "
                 + "Where bi.bookId ='" + bookId + "' "
                 + "And bi.account ='" + account + "' "
                 + "And bi.meetingId='" + meetingId + "' ";
            QueryResult rs = null;
            try
            {
                BookManager bookManager = new BookManager(dbPath);
                //rs = dbConn.executeQuery(query);
                rs = bookManager.sqlCommandQuery(query);
                int sno = -1;
                if (rs.fetchRow())
                {
                    sno = rs.getInt("sno");
                }
                return sno;
            }
            catch
            {
                return -1;
            }
        }

        public void InitSyncCenter(string dbPath, string bookId, string account, string meetingId)
        {
            if (PaperLess_Emeeting.Properties.Settings.Default.HasSyncCenterModule == true)
            {
                Task.Factory.StartNew(() =>
                         {
                             try
                             {

                                 SyncCenter syncCenter = new SyncCenter();
                                 syncCenter.bookManager = new BookManager(dbPath);
                                 int userBookSno = getBookPath(dbPath, bookId, account, meetingId);
                                 Dictionary<String, Object> cloudSyncingClsList = new Dictionary<String, Object>() { { "SBookmark", new BookMarkData() }, { "SAnnotation", new NoteData() }, { "SSpline", new StrokesData() }, { "SLastPage", new LastPageData() } };

                                 foreach (KeyValuePair<String, Object> syncType in cloudSyncingClsList)
                                 {
                                     string className = syncType.Key;
                                     Type openType = typeof(SyncManager<>);
                                     Type actualType = openType.MakeGenericType(new Type[] { syncType.Value.GetType() });

                                     //AbstractSyncManager sm = (AbstractSyncManager)Activator.CreateInstance(actualType, account, meetingId, bookId, userBookSno, className, 0, "0", WsTool.GetAbstractSyncCenter_BASE_URL());
                                     AbstractSyncManager sm = (AbstractSyncManager)Activator.CreateInstance(actualType, account, "free", bookId, userBookSno, className, 0, "0", WsTool.GetAbstractSyncCenter_BASE_URL());
                                     syncCenter.addSyncConditions(className, sm);
                                 }
                             }
                             catch (Exception ex)
                             {
                                 LogTool.Debug(ex);
                             }
                         });
            }
        }

        private void CallOpenBook(bool HasOpenFinishedFileVersion=false)
        {
            MouseTool.ShowLoading();
            try
            {

                Home Home_Window = Application.Current.Windows.OfType<Home>().FirstOrDefault();

                if (Home_Window.IsInSync == true && Home_Window.IsSyncOwner == false)
                {
                    AutoClosingMessageBox.Show("同步中需由主控人員進行操作");
                    return;
                }


                // 

                string filePath = ClickOnceTool.GetFilePath();

                //string _bookPath = System.IO.Path.Combine(filePath, fileItem.UnZipFilePath);
                string UnZipFileFolder = PaperLess_Emeeting.Properties.Settings.Default.File_UnZipFileFolder;
                //string _bookPath = filePath + "\\" + UnZipFileFolder + "\\" + UserID + "\\" + MeetingID + "\\"+ fileItem.ID +"\\"+fileItem.FileVersion.ToString();
                // 等於上面那個路徑
                string _bookPath = fileItem.UnZipFilePath;

                // 從資料庫查詢上一次完成的檔案版本
                if (HasOpenFinishedFileVersion == true)
                {
                    DataTable dt = MSCE.GetDataTable("SELECT FinishedFileVersion FROM FileRow where ID=@1 and UserID=@2 and MeetingID=@3"
                                        , meetingDataDownloadFileFile.ID
                                        , UserID
                                        , MeetingID);
                    if (dt.Rows.Count > 0)
                    {
                        _bookPath = filePath + "\\" + UnZipFileFolder + "\\" + UserID + "\\" + MeetingID + "\\" + fileItem.ID + "\\" + dt.Rows[0]["FinishedFileVersion"].ToString();
                    }
                }
                string _bookId = fileItem.ID;
                string _account = UserID;
                string _userName = UserName;
                string _email = UserEmail;
                string _meetingId = MeetingID;
                string _watermark = "";
                if (MeetingDataCT_GetWatermark_Event != null)
                    _watermark = MeetingDataCT_GetWatermark_Event();
                string _dbPath = System.IO.Path.Combine(ClickOnceTool.GetDataPath(), PaperLess_Emeeting.Properties.Settings.Default.bookInfo_Path);
                bool _isSync = Home_Window.IsInSync;
                bool _isSyncOwner = Home_Window.IsSyncOwner;
                string _webServiceUrl = WsTool.GetUrl() + "/AnnotationUpload";
                string _socketMessage = "";

            
                SocketClient _socket = null;
                SocketClient tmpSocket = Singleton_Socket.GetInstance(MeetingID, UserID, UserName, Home_Window.IsInSync);

                if (tmpSocket != null && tmpSocket.GetIsConnected() == true)
                    _socket = tmpSocket;

                //if (_socket.GetIsConnected() == false)
                //    _socket = null;
                //if (Home_Window.IsInSync == false)
                //    _socket = null;

                // 呼叫一個事件取得 BookVMs
                Dictionary<string, BookVM> cbBooksData = new Dictionary<string, BookVM>();
                if (MeetingDataCT_GetBookVMs_ByMeetingFileCate_Event != null)
                    cbBooksData = MeetingDataCT_GetBookVMs_ByMeetingFileCate_Event(fileItem);
                // debug
                //BooksData["cAF6-P"] = new BookVM("cAF6-P", filePath + "\\" + UnZipFileFolder + "\\" + UserID + "\\" + MeetingID + "\\" + "cAF6-P");
                //BooksData["cAF3-P"] = new BookVM("cAF3-P", filePath + "\\" + UnZipFileFolder + "\\" + UserID + "\\" + MeetingID + "\\" + "cAF3-P");

                Home_Window.CloseAllWindow("", true);

                switch (fileItem.FileCate)
                {
                    case MeetingFileCate.電子書:
                     
                        InitSyncCenter(_dbPath, _bookId, _account, _meetingId);
                      
                        Task.Factory.StartNew(() =>
                        {
                            this.Dispatcher.BeginInvoke((Action)(() =>
                            {

                                byte[] ReaderKey = new byte[1];

                                try
                                {
                                    if (fileItem.EncryptionKey.Equals("") == false)
                                    {
                                        ReaderKey = ReaderDecodeTool.GetReaderKey(fileItem.EncryptionKey);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogTool.Debug(ex);
                                }
                                ReadWindow rw = new ReadWindow(cbBooksData, Home_Window.OpenBookFromReader, _bookPath, _bookId, _account
                                                      , _userName, _email, _meetingId
                                                      , _watermark, _dbPath, _isSync
                                                      , _isSyncOwner, _webServiceUrl, ReaderKey, _socketMessage, _socket);

                                //ReadWindow rw = new ReadWindow(cbBooksData, Home_Window.OpenBookFromReader, _bookPath, _bookId, _account
                                //                      , _userName, _email, _meetingId
                                //                      , _watermark, _dbPath, _isSync
                                //                      , _isSyncOwner, _webServiceUrl, _socketMessage, _socket);
                                rw.FolderID = this.FolderID;
                                rw.WindowStyle = WindowStyle.None;
                                rw.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                                rw.WindowState = WindowState.Maximized;
                                if (PaperLess_Emeeting.Properties.Settings.Default.AssemblyName.Contains("TPI4F") == true)
                                {
                                    //rw.WindowStyle = WindowStyle.SingleBorderWindow;
                                }
                                rw.Show();
                            }));
                        });


                        break;
                    case MeetingFileCate.Html5投影片:
                        _bookPath = _bookPath + @"\" + new FileInfo(Directory.GetFiles(_bookPath)[0]).Name;
                       
                        InitSyncCenter(_dbPath, _bookId, _account, _meetingId);
                      
                        Task.Factory.StartNew(() =>
                        {
                            this.Dispatcher.BeginInvoke((Action)(() =>
                            {
                                HTML5ReadWindow Html5rw = new HTML5ReadWindow(cbBooksData, Home_Window.OpenBookFromReader, _bookPath, _bookId, _account
                                                       , _userName, _email, _meetingId
                                                       , _watermark, _dbPath, _isSync
                                                       , _isSyncOwner, _webServiceUrl, _socketMessage, _socket);
                                Html5rw.FolderID = this.FolderID;
                                Html5rw.WindowStyle = WindowStyle.None;
                                Html5rw.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                                Html5rw.WindowState = WindowState.Maximized;
                                if(PaperLess_Emeeting.Properties.Settings.Default.AssemblyName.Contains("TPI4F")==true)
                                {
                                    //Html5rw.WindowStyle = WindowStyle.SingleBorderWindow;
                                }
                                Html5rw.Show();
                            }));
                        });


                        break;
                    case MeetingFileCate.影片檔:
                        _bookPath = _bookPath + @"\" + new FileInfo(Directory.GetFiles(_bookPath)[0]).Name;

                        Task.Factory.StartNew(() =>
                        {
                            this.Dispatcher.BeginInvoke((Action)(() =>
                            {

                                MVWindow mvWindow = new MVWindow(cbBooksData, Home_Window.OpenBookFromReader, _bookPath, _bookId);
                                mvWindow.WindowStyle = WindowStyle.None;
                                mvWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                                mvWindow.WindowState = WindowState.Maximized;
                                if (PaperLess_Emeeting.Properties.Settings.Default.AssemblyName.Contains("TPI4F") == true)
                                {
                                    //mvWindow.WindowStyle = WindowStyle.SingleBorderWindow;
                                }
                                mvWindow.Show();
                            }));
                        });

                        break;
                }

                if (Home_Window.IsInSync == true && Home_Window.IsSyncOwner == true)
                {
                    SocketClient socketClient = Singleton_Socket.GetInstance(MeetingID, UserID, UserName, Home_Window.IsInSync);
                    Task.Factory.StartNew(() =>
                    {
                        if (socketClient!=null && socketClient.GetIsConnected() == true)
                        {
                            string OB = "{\"bookId\":\"" + meetingDataDownloadFileFile.ID + "\",\"cmd\":\"R.OB\"}";
                            socketClient.broadcast(OB);
                        }
                        else
                        {
                            //AutoClosingMessageBox.Show("同步伺服器尚未啟動，請聯絡議事管理員開啟同步");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }

            MouseTool.ShowArrow();
        }


      

        // 準備做開啟下載，UI物件的顯示順序和btnPause是相反的
        private void btnPausing_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 隱藏要先做
            btnPausing.Visibility = Visibility.Collapsed;

            FileDownloader fileDownloader = Singleton_FileDownloader.GetInstance(MeetingID);
            File_DownloadItemViewModel _InListFileItem = fileDownloader.GetInList(MeetingID);

            fileDownloader.AddItem(fileItem);
            // 等待下載器自己開始。
            //Thread.Sleep(500);

            // 有排入下載，不用再開新下載
            if (_InListFileItem != null) 
            {
                // 正在下載的檔案物件，正在下載的物件跟現在的檔案ID相同的話。
                // 正在下載的ID相同，就讓它繼續下載吧
                if (fileDownloader.NowFileItem != null && fileDownloader.NowFileItem.ID.Equals(fileItem.ID))
                {
                    // 進度和進度條是黑色可見
                    // 進度條文字顯示百分比
                    // 藍色暫停可見
                    // 橘色暫停不可見
                    btnPausing.Visibility = Visibility.Collapsed;
                    txtPercent.Text = ((int)_InListFileItem.LastPercentage).ToString() + " %";
                    txtPercent.Foreground = Brushes.Black;
                    txtPercent.Visibility = Visibility.Visible;
                    pb.Value = _InListFileItem.NowPercentage;
                    pb.Foreground = Brushes.Orange;
                    pb.Background = Brushes.Black;
                    pb.Visibility = Visibility.Visible;
                    btnPause.Visibility = Visibility.Visible;
                    //goto TriggerFirst;
                    return;
                }
                else // 排入下載的檔案物件，但是未下載，所以藍色的 btnPause還是不能顯示
                {
                    // 進度和進度條是灰色可見
                    // 進度條文字顯示等待中
                    // 藍色暫停可見
                    // 橘色暫停不可見
                    btnPausing.Visibility = Visibility.Collapsed;
                    txtPercent.Text = "等待中";
                    txtPercent.Foreground = Brushes.Gray;
                    txtPercent.Visibility = Visibility.Visible;
                    pb.Value = _InListFileItem.NowPercentage;
                    pb.Foreground = Brushes.Wheat;
                    pb.Background = Brushes.Gray;
                    pb.Visibility = Visibility.Visible;
                    btnPause.Visibility = Visibility.Visible;
                    //goto TriggerFirst;
                    return;
                }
            }
            else // 沒有下載中的物件可以判斷，只能以自身的進度判斷
            {
                // 要先變換
                // 進度和進度條是灰色可見
                // 進度條文字顯示等待中
                // 藍色暫停可見
                // 橘色暫停不可見
                btnPausing.Visibility = Visibility.Collapsed;
                txtPercent.Text = "等待中";
                txtPercent.Foreground = Brushes.Gray;
                txtPercent.Visibility = Visibility.Visible;
                //pb.Value = fileItem.NowPercentage;
                pb.Foreground = Brushes.Wheat;
                pb.Background = Brushes.Gray;
                pb.Visibility = Visibility.Visible;
                btnPause.Visibility = Visibility.Visible;
                //goto TriggerFirst;
                return;
            }

            //TriggerFirst:
            //MeetingDataCT_FirstTimeDownload_Event(this.meetingDataDownloadFileFile.ID);
        }

        // 準備做暫停
        private void btnPause_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 隱藏要先做
            btnPause.Visibility = Visibility.Collapsed;

            FileDownloader fileDownloader = Singleton_FileDownloader.GetInstance(MeetingID);
            File_DownloadItemViewModel _InListFileItem = fileDownloader.GetInList(MeetingID);

            fileDownloader.Pause(meetingDataDownloadFileFile.ID);
            // 等待下載器自己暫停。
            //Thread.Sleep(500);

            // 進度和進度條是灰色可見
            // 進度條文字顯示進度
            int percent = 0;
            if (_InListFileItem != null)
                percent = (int)_InListFileItem.NowPercentage;
            else
                percent =(int)this.fileItem.NowPercentage;
            // 進度和進度條是灰色可見
            // 進度條文字顯示進度
            // 藍色暫停不可見
            // 橘色暫停可見
            btnPause.Visibility = Visibility.Collapsed;
            txtPercent.Text = percent.ToString() + " %";
            txtPercent.Foreground = Brushes.Gray;
            txtPercent.Visibility = Visibility.Visible;
            pb.Value = percent;
            pb.Foreground = Brushes.Wheat;
            pb.Background = Brushes.Gray;
            pb.Visibility = Visibility.Visible;
            btnPausing.Visibility = Visibility.Visible;
            return;

          
        }

        private void btnDownload_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
           
            FileDownloader fileDownloader = Singleton_FileDownloader.GetInstance(MeetingID);
            
            fileDownloader.AddItem(this.fileItem);

            // 記得必須隱藏得先做，才不會被看見
            btnDownload.Visibility = Visibility.Collapsed;
            txtPercent.Text = "等待中";
            txtPercent.Foreground = Brushes.Gray;
            txtPercent.Visibility = Visibility.Visible;
            pb.Foreground = Brushes.Wheat;
            pb.Background = Brushes.Gray;
            pb.Visibility = Visibility.Visible;
            btnPause.Visibility = Visibility.Visible;

            //觸發第一次下載
            if (MeetingDataCT_RaiseAllDownload_Event != null)
                MeetingDataCT_RaiseAllDownload_Event(this.fileItem.ID,false,false);

        }


        private void InitUI()
        {
            //txtIndex.Text = index.ToString();
            //txtFileName.Text = meetingDataDownloadFileFile.FileName;

            //Task.Factory.StartNew(() =>
            //    {
            //        this.Dispatcher.BeginInvoke(new Action(() =>
            //            {
                            // 改變btnOpen的圖
                            switch (fileItem.FileCate)
                            {
                                case MeetingFileCate.電子書:
                                case MeetingFileCate.Html5投影片:
                                    break;
                                case MeetingFileCate.影片檔:
                                    btnOpen.Source = new BitmapImage(new Uri("images/icon_video@2x.png", UriKind.Relative));
                                    break;
                            }

                            switch (fileItem.FileType)
                            {
                                case MeetingFileType.從未下載:
                                    ShowNeverDownload();
                                    break;
                                case MeetingFileType.暫停中:
                                    ShowInPause(true);
                                    break;
                                case MeetingFileType.正在下載中:
                                    txtPercent.Text = ((int)fileItem.NowPercentage).ToString() + " %";
                                    ShowInPause(false);
                                    break;
                                case MeetingFileType.已下載完成:
                                    ShowCanOpen();
                                    break;
                                case MeetingFileType.排入下載中:
                                    int percent = (int)fileItem.NowPercentage;
                                    if (percent > 0)
                                        txtPercent.Text = percent + " %";
                                    else
                                        txtPercent.Text = "等待中";
                                    ShowInPause(false);
                                    break;
                                case MeetingFileType.已下載過但是未完成的檔案:
                                    txtPercent.Text = ((int)fileItem.NowPercentage).ToString() + " %";
                                    ShowInPause(true);
                                    break;
                                case MeetingFileType.解壓縮中:
                                    Storyboard sb = (Storyboard)this.TryFindResource("sb");
                                    if (sb != null)
                                        sb.Begin();
                                    break;
                                case MeetingFileType.解壓縮失敗:
                                    ShowNeverDownload();
                                    break;
                                case MeetingFileType.已經下載過一次且可以更新版本的檔案_目前下載未完成:
                                    //顯示可已更新的背景圖片
                                    txtPercent.Text = ((int)fileItem.NowPercentage).ToString() + " %";
                                    ShowInPause(true);
                                    btnUpdate.Visibility = Visibility.Visible;
                                    break;
                                case MeetingFileType.已經下載過一次且可以更新版本的檔案_目前下載已完成:
                                    //顯示可已更新的背景圖片
                                    ShowCanOpen();
                                    btnUpdate.Visibility = Visibility.Visible;
                                    break;
                            }

                            if (IsWaitingForDownload == true)
                            {
                                btnOpen.Visibility = Visibility.Collapsed;
                                btnDownload.Visibility = Visibility.Collapsed;
                                btnPausing.Visibility = Visibility.Collapsed;
                                txtPercent.Text = "等待中";
                                txtPercent.Foreground = Brushes.Gray;
                                txtPercent.Visibility = Visibility.Visible;
                                pb.Foreground = Brushes.Wheat;
                                pb.Background = Brushes.Gray;
                                pb.Value = this.fileItem.NowPercentage;
                                pb.Visibility = Visibility.Visible;
                                btnPause.Visibility = Visibility.Visible;
                            }

                            switch (meetingRoomButtonType)
                            {
                                case MeetingRoomButtonType.NN:
                                    // 沒有圖示
                                    break;
                                case MeetingRoomButtonType.YY:
                                    // 未下載檔案: 下載圖示
                                    // 已下載檔案: 垃圾埇圖示
                                    break;
                                case MeetingRoomButtonType.ON:
                                case MeetingRoomButtonType.OY:
                                    // 未下載檔案: 不可下載圖示
                                    // 已下載檔案: 垃圾埇圖示
                                    if (fileItem.DownloadBytes == 0 || fileItem.DownloadBytes < fileItem.TotalBytes)
                                    {
                                        pb.Visibility = Visibility.Collapsed;
                                        btnOpen.Visibility = Visibility.Collapsed;
                                        btnDownload.Visibility = Visibility.Collapsed;
                                        btnPause.Visibility = Visibility.Collapsed;
                                        btnPausing.Visibility = Visibility.Collapsed;
                                        
                                        txtPercent.Text = "下載過期";
                                        txtPercent.Foreground = Brushes.Gray;
                                        txtPercent.Visibility = Visibility.Visible;
                                        btnUpdate.Visibility = Visibility.Collapsed;
                                    }
                                    break;
                                case MeetingRoomButtonType.NO:
                                case MeetingRoomButtonType.YO:
                                case MeetingRoomButtonType.OO:
                                    // 未下載檔案: 不可下載圖示
                                    // 已下載檔案: 不可瀏覽圖示
                                    pb.Visibility = Visibility.Collapsed;
                                    btnOpen.Visibility = Visibility.Collapsed;
                                    btnDownload.Visibility = Visibility.Collapsed;
                                    btnPause.Visibility = Visibility.Collapsed;
                                    btnPausing.Visibility = Visibility.Collapsed;

                                    txtPercent.Text = "瀏覽過期";
                                    txtPercent.Foreground = Brushes.Gray;
                                    txtPercent.Visibility = Visibility.Visible;
                                    btnUpdate.Visibility = Visibility.Collapsed;
                                    break;
                            }

                            // 判斷是否是最後一個FileRow載入完成的
                            // 要注意，並不一定是最後一個才是最後載入完成的
                            if (MeetingDataCT_IsAllFileRowFinished_AddInitUIFinished_Event != null)
                                IsAllFileRowFinished = MeetingDataCT_IsAllFileRowFinished_AddInitUIFinished_Event();
                //        }));
                //});

        }


        private void ShowInPause(bool IsPausing)
        {
            txtPercent.Foreground = Brushes.Gray;
            txtPercent.Visibility = Visibility.Visible;
            pb.Value = fileItem.NowPercentage;
            pb.Foreground = Brushes.Wheat;
            pb.Background = Brushes.Gray;
            pb.Visibility = Visibility.Visible;
            if (IsPausing == true)
                btnPausing.Visibility = Visibility.Visible;
            else
                btnPause.Visibility = Visibility.Visible;
        }

        private void ShowNeverDownload()
        {
            // 記得必須隱藏必須先做，才不會被看見
            // 先拿掉，因為本來就是不可見的
            txtPercent.Text = "0 %";
            pb.Value = pb.Minimum;

            pb.Visibility = Visibility.Collapsed;
            txtPercent.Visibility = Visibility.Collapsed;
            btnOpen.Visibility = Visibility.Collapsed;
            btnDownload.Visibility = Visibility.Visible;
        }

        private void ShowInDownload()
        {
            // 記得必須隱藏必須先做，才不會被看見
            // 先拿掉，因為本來就是不可見的
            //btnDownload.Visibility = Visibility.Collapsed;
            //btnOpen.Visibility = Visibility.Collapsed;
        }

        private void ShowCanOpen()
        {
            // 記得必須隱藏必須先做，才不會被看見
            // 先拿掉，因為本來就是不可見的
            //btnDownload.Visibility = Visibility.Collapsed;
            //txtPercent.Visibility = Visibility.Collapsed;
            //pb.Visibility = Visibility.Collapsed;

            btnOpen.Visibility = Visibility.Visible;
        }

       
    }
}
