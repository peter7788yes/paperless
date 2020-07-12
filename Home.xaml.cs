using AES_ECB_PKCS5;
using AutoLogOffInWPF;
using BookManagerModule;
using DataAccessObject;
using ModernWPF.Win8TouchKeyboard.Desktop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaperLess_Emeeting.App_Code;
using PaperLess_Emeeting.App_Code.ClickOnce;
using PaperLess_Emeeting.App_Code.MessageBox;
using PaperLess_Emeeting.App_Code.Socket;
using PaperLess_Emeeting.App_Code.ViewModel;
using PaperLess_Emeeting.App_Code.WS;
using PaperLess_Emeeting.Properties;
using PaperLess_ViewModel;
using PaperlessSync.Broadcast.Service;
using PaperlessSync.Broadcast.Socket;
using SyncCenterModule;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PaperLess_Emeeting
{

    public delegate void Home_ChangeCC_Event_Function(UserButton userButton);
    public delegate void Home_OpenBookFromReader_Function(string MeetingID, BookVM bookVM, Dictionary<string, BookVM> cbBooksData, string watermark = "");
    /// <summary>
    /// Home.xaml 的互動邏輯
    /// </summary>
    public partial class Home : Window
    {
        Home_UnZipError_Function UnZipError_callback;


        public User user { get; set; }

        public string UserID { get; set; }
        public string UserName { get; set; }
        public string UserPWD { get; set; }
        //public string MeetingID { get; set; }
        public string UserEmail { get; set; }
        public DateTime MeetingListDate { get; set; }
        public UserButton[] UserButtonAry { get; set; }
        public UserMeeting[] UserMeetingAry { get; set; }


        public string SyncMeetingID { get; set; }
        public bool IsInSync { get; set; }
        public bool IsSyncOwner { get; set; }

        public Home_OpenBook_Function Home_OpenBook_Callback;
        public Home_IsInSync_And_IsSyncOwner_Function Home_IsInSync_And_IsSyncOwner_Callback;
        public Home_CloseAllWindow_Function Home_CloseAllWindow_Callback;
        public Home_TurnOffSyncButton_Function Home_TurnOffSyncButton_Callback;
        public Home_SetSocketClientNull_Function Home_SetSocketClientNull_Callback;
        public Home_Change2MeetingDataCT_Function Home_Change2MeetingDataCT_Callback;
        public Home_OpenBookFromReader_Function Home_OpenBookFromReader_Callback;
        DisplayUserNameMode displayUserNameMode;

        public string NowPressButtonID = "BtnHome";

        //預載系列會議,文件庫
        public Dictionary<string,SeriesData> PreLoadSeriesDataDict = new Dictionary<string, SeriesData>();
        public int CacheMinuteTTL=0;
        public Thread CacheThread = null;

        public Home(User user,string UserPWD)
        {
            MouseTool.ShowLoading();
            App.IsChangeWindow = false;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            this.user = user;
            this.UserPWD = UserPWD;
            this.MeetingListDate = DateTime.Now;
            this.UserButtonAry = user.EnableButtonList;
            this.UserMeetingAry = user.MeetingList;

            // Disables inking in the WPF application and enables us to track touch events to properly trigger the touch keyboard
            InkInputHelper.DisableWPFTabletSupport();

            this.CacheMinuteTTL = PaperLess_Emeeting.Properties.Settings.Default.CacheMinuteTTL;

            this.Loaded += Home_Loaded;
            this.Unloaded += Home_Unloaded;
            this.Closing += (sender, e) =>
                {
                    if (App.IsChangeWindow == false)
                    {
                        App.CopyLog();
                        //關閉應用程式
                        Application.Current.Shutdown();
                        //關閉處理序
                        Environment.Exit(0);
                    }
                };
            //MouseTool.ShowArrow();
        }

       

        //public Home(UserButton[] UserButtonAry, UserMeeting[] UserMeetingAry)
        //{
        //    MouseTool.ShowLoading();
        //    this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        //    InitializeComponent();
        //    this.UserButtonAry = UserButtonAry;
        //    this.UserMeetingAry = UserMeetingAry;
        //    this.Loaded += Home_Loaded;
        //    this.Unloaded += Home_Unloaded;
        //    //MouseTool.ShowArrow();
        //}

        private void Home_Unloaded(object sender, RoutedEventArgs e)
        {

             StopAllBackground();

             //LawDownloader lawDownloader = Singleton_LawDownloader.GetInstance();
             //lawDownloader.Home_UnZipError_Event -= UnZipError_callback;

             //Singleton_FileDownloader.Home_UnZipError_Callback = null;


             ////DataTable dt= MSCE.GetDataTable("select MeetingID from NowLogin");
             ////if (dt.Rows.Count > 0)
             ////{
             ////    MeetingID = dt.Rows[0]["MeetingID"].ToString().Trim();
             ////}



             //Singleton_Socket.home_OpenIEventManager.Home_OpenBook_Event -= Home_OpenBook_Callback;
             //Singleton_Socket.home_OpenIEventManager.Home_IsInSync_And_IsSyncOwner_Event -= Home_IsInSync_And_IsSyncOwner_Callback;
             //Singleton_Socket.home_OpenIEventManager.Home_CloseAllWindow_Event -= Home_CloseAllWindow_Callback;

             //Singleton_Socket.home_CloseIEventManager.Home_CloseAllWindow_Event -= Home_CloseAllWindow_Callback;
             //Singleton_Socket.home_CloseIEventManager.Home_TurnOffSyncButton_Event -= Home_TurnOffSyncButton_Callback;
             //Singleton_Socket.home_CloseIEventManager.Home_SetSocketClientNull_Event -= Home_SetSocketClientNull_Callback;

             //Singleton_Socket.ClearInstance();

        }

        private void Home_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Enables WPF to mark edit field as supporting text pattern (Automation Concept)
                //System.Windows.Automation.AutomationElement asForm = System.Windows.Automation.AutomationElement.FromHandle(new WindowInteropHelper(this).Handle);

                // Windows 8 API to enable touch keyboard to monitor for focus tracking in this WPF application
                //InputPanelConfigurationLib.InputPanelConfiguration inputPanelConfig = new InputPanelConfigurationLib.InputPanelConfiguration();
                //inputPanelConfig.EnableFocusTracking();
            }
            catch
            {
            }

            Task.Factory.StartNew(() =>
            {
                InitSelectDB();
                // 這裡為 Home畫面，優先權設定為Normal => 列舉值為 9。 一般優先權處理作業。 這是一般的應用程式的優先順序。
                // 預設值為 DispatcherPriority.Normal
                // 所以不寫
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        InitUI();
                        InitEvent();
                    }
                    catch (Exception ex)
                    {
                        LogTool.Debug(ex);
                    }
                    MouseTool.ShowArrow();
                }));

                //預載系列會議
                PreLoadSeriesData();
            });


            try
            {
                InitializeAutoLogoffFeature();
            }
            catch(Exception ex)
            {
                LogTool.Debug(ex);
            }
        }

        //預載系列會議
        private void PreLoadSeriesData()
        {
            //小於0為沒有Cache
            //等於0為Cache不會過期
            //大於0為多少分鐘後清掉Cache
            if (this.CacheMinuteTTL >= 0)
            {
                Task.Factory.StartNew(() =>
                 {
                     
                         GetSeriesData.AsyncPOST(UserID, (sd) => 
                         {
                             try
                             {
                                PreLoadSeriesDataDict["BtnSeries"] = sd; 
                                if (this.CacheMinuteTTL > 0)
                                {
                                     if (CacheThread != null)
                                         CacheThread.Abort();
                                     CacheThread = new Thread(delegate()
                                     {
                                         Thread.Sleep(this.CacheMinuteTTL * 60 * 1000);
                                         if (PreLoadSeriesDataDict.ContainsKey("BtnSeries") == true)
                                         {
                                             PreLoadSeriesDataDict.Remove("BtnSeries");
                                         }
                                     });
                                     CacheThread.IsBackground = true;
                                     CacheThread.Start();
                                 }
                             }
                             catch (Exception ex)
                             {
                                 LogTool.Debug(ex);
                             }
                         });
                        
                 });
            }
        }

        private void InitSelectDB()
        {
            UserID = user.ID;
            UserName = user.Name;
            UserEmail = user.Email;

            //DataTable dt = MSCE.GetDataTable("select UserID,UserName,UserPWD,MeetingListDate,UserEmail from NowLogin");
            //if (dt.Rows.Count > 0)
            //{
            //    UserID = dt.Rows[0]["UserID"].ToString().Trim();
            //    UserName = dt.Rows[0]["UserName"].ToString().Trim();
            //    UserPWD = dt.Rows[0]["UserPWD"].ToString().Trim();
            //    MeetingListDate = (DateTime)dt.Rows[0]["MeetingListDate"];
            //    UserEmail = dt.Rows[0]["UserEmail"].ToString();
            //}
        }

        private void InitEvent()
        {
            LawDownloader lawDownloader = Singleton_LawDownloader.GetInstance();
            UnZipError_callback = new Home_UnZipError_Function(Home_UnZipError_Callback);
            lawDownloader.Home_UnZipError_Event += UnZipError_callback;
            //Singleton_LawDownloader.Home_UnZipError_Callback = UnZipError_callback;

            Singleton_FileDownloader.Home_UnZipError_Callback = UnZipError_callback;

            Home_OpenBook_Callback = new Home_OpenBook_Function(OpenBook);
            Home_IsInSync_And_IsSyncOwner_Callback = new Home_IsInSync_And_IsSyncOwner_Function(IsInSync_And_IsSyncOwner);
            Home_CloseAllWindow_Callback = new Home_CloseAllWindow_Function(CloseAllWindow);
            Home_TurnOffSyncButton_Callback = new Home_TurnOffSyncButton_Function(TurnOffSyncButton);
            Home_SetSocketClientNull_Callback = new Home_SetSocketClientNull_Function(SetSocketClientNull);

            Home_Change2MeetingDataCT_Callback = new Home_Change2MeetingDataCT_Function(Change2MeetingDataCT);
            Home_OpenBookFromReader_Callback = new Home_OpenBookFromReader_Function(OpenBookFromReader);

            Singleton_Socket.home_OpenIEventManager.Home_OpenBook_Event += Home_OpenBook_Callback;
            Singleton_Socket.home_OpenIEventManager.Home_IsInSync_And_IsSyncOwner_Event += Home_IsInSync_And_IsSyncOwner_Callback;
            Singleton_Socket.home_OpenIEventManager.Home_CloseAllWindow_Event += Home_CloseAllWindow_Callback;

            Singleton_Socket.home_CloseIEventManager.Home_CloseAllWindow_Event += Home_CloseAllWindow_Callback;
            Singleton_Socket.home_CloseIEventManager.Home_TurnOffSyncButton_Event += Home_TurnOffSyncButton_Callback;
            Singleton_Socket.home_CloseIEventManager.Home_SetSocketClientNull_Event += Home_SetSocketClientNull_Callback;

            if (PaperLess_Emeeting.Properties.Settings.Default.IsDebugMode == true)
            {
                txtUserName.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
                txtUserName.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
                txtUserName.MouseLeftButtonDown += (sender, e) => { CC.Content = new BroadcastCT(); };
            }
        }

        private void SetSocketClientNull()
        {
            Singleton_Socket.ClearInstance();
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
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }

            return userBookSno;
        }

        public int getUserBookSno(string dbPath, string bookId, string account, string meetingId)
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

        private void InitSyncCenter(string dbPath, string bookId, string account, string meetingId)
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

       
        private void OpenBook(string BookID,string InitMsg)
        {
            
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action<string,string>(OpenBook), BookID, InitMsg);
                this.Dispatcher.BeginInvoke(new Action<string, string>(OpenBook), BookID, InitMsg);
            }
            else
            {
                try
                {
                   
                    DataTable dt = MSCE.GetDataTable(@"select fr.DownloadBytes, fr.TotalBytes, fr.MeetingID ,fr.FileVersion ,fr.FinishedFileVersion from FileRow as fr 
                                                inner join NowLogin as nl on  nl.MeetingID = fr.MeetingID
                                                where  fr.id=@1 and  fr.UserID=@2"
                                                    , BookID
                                                    , UserID);
                    if (dt.Rows.Count < 0)
                    {
                        AutoClosingMessageBox.Show(string.Format("尚未下載{0}", BookID));
                        return;
                    }

                    double DownloadBytes = Double.Parse(dt.Rows[0]["DownloadBytes"].ToString());
                    double TotalBytes = Double.Parse(dt.Rows[0]["TotalBytes"].ToString());
                    string MeetingID = dt.Rows[0]["MeetingID"].ToString();
                    int FileVersion = int.Parse(dt.Rows[0]["FileVersion"].ToString().Equals("") || dt.Rows[0]["FileVersion"].ToString().Equals("0") ? "1" : dt.Rows[0]["FileVersion"].ToString());
                    int FinishedFileVersion = int.Parse(dt.Rows[0]["FinishedFileVersion"].ToString().Equals("") ? "0" : dt.Rows[0]["FileVersion"].ToString());

                    if (DownloadBytes < TotalBytes)
                    {
                        string AlertMessage = "";
                        if (FinishedFileVersion > 0)
                        {
                            AlertMessage = string.Format("尚未更新檔案: {0}", BookID);
                        }
                        else
                        {
                            AlertMessage = string.Format("尚未下載檔案: {0}", BookID);
                        }
                        AutoClosingMessageBox.Show(AlertMessage);
                        return;
                    }

                    //string AppPath = AppDomain.CurrentDomain.BaseDirectory;
                    string filePath = ClickOnceTool.GetFilePath();
                    string UnZipFileFolder = PaperLess_Emeeting.Properties.Settings.Default.File_UnZipFileFolder;
                    //string _bookPath = System.IO.Path.Combine(filePath, UnZipFileFolder + "\\" + UserID + "\\" + MeetingID + "\\" + BookID);
                    string _bookPath = filePath + "\\" + UnZipFileFolder + "\\" + UserID + "\\" + MeetingID + "\\" + BookID;
                    // 從資料庫查詢上一次完成的檔案版本
                    _bookPath = filePath + "\\" + UnZipFileFolder + "\\" + UserID + "\\" + MeetingID + "\\" + BookID + "\\" + FinishedFileVersion;

                    string _bookId = BookID;
                    string _account = UserID;
                    string _userName = UserName;
                    string _email = UserEmail;
                    string _meetingId = MeetingID;
                    string _watermark = "";
                    string _dbPath = System.IO.Path.Combine(ClickOnceTool.GetDataPath(), PaperLess_Emeeting.Properties.Settings.Default.bookInfo_Path);
                    // 如果有InitMsg的訊息就直接把 _isSync 設定成同步中。
                    bool _isSync = InitMsg.Equals("") == false ? true : IsInSync;
                    bool _isSyncOwner = IsSyncOwner;
                    string _webServiceUrl = WsTool.GetUrl() + "/AnnotationUpload";
                    string _socketMessage = InitMsg;
                    SocketClient _socket = Singleton_Socket.GetInstance(MeetingID, UserID, UserName, true);

                    Dictionary<string, BookVM> cbBooksData = new Dictionary<string, BookVM>();
                    dt = MSCE.GetDataTable("select ID,DisplayFileName from FileRow where id=@1 and UserID=@2 and MeetingID=@3"
                                           , BookID
                                           , UserID
                                           , MeetingID);

                    MeetingFileCate fileCate = MeetingFileCate.電子書;
                    string typeChar = BookID.Split('-').Last();
                    switch (typeChar)
                    {
                        case "P":
                            fileCate = MeetingFileCate.電子書;
                            break;
                        case "H":
                            fileCate = MeetingFileCate.Html5投影片;
                            break;
                        case "V":
                            fileCate = MeetingFileCate.影片檔;
                            break;

                    }

                    if (dt.Rows.Count > 0)
                    {
                        string base_bookPath = filePath + "\\" + UnZipFileFolder + "\\" + UserID + "\\" + MeetingID; //  +"\\" + BookID;
                        foreach (DataRow dr in dt.Rows)
                        {

                            cbBooksData[dr["DisplayFileName"].ToString()] = new BookVM(BookID, base_bookPath + "\\" + dr["ID"].ToString(), fileCate);
                        }
                    }

                    switch (fileCate)
                    {
                        case MeetingFileCate.電子書:
                            InitSyncCenter(_dbPath, _bookId, _account, _meetingId);
                            Task.Factory.StartNew(() =>
                            {
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                    {

                                        byte[] ReaderKey = new byte[1];
                                        try
                                        {
                                            DataTable dt2 = MSCE.GetDataTable("SELECT EncryptionKey FROM FileRow where ID=@1 and UserID=@2 and MeetingID=@3"
                                                                            , _bookId
                                                                            , _account
                                                                            , _meetingId);

                                            if (dt2.Rows.Count > 0)
                                            {
                                                ReaderKey = ReaderDecodeTool.GetReaderKey(dt2.Rows[0]["EncryptionKey"].ToString());
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            LogTool.Debug(ex);
                                        }


                                        ReadWindow rw = new ReadWindow(cbBooksData, OpenBookFromReader, _bookPath, _bookId, _account
                                                       , _userName, _email, _meetingId
                                                       , _watermark, _dbPath, _isSync
                                                       , _isSyncOwner, _webServiceUrl, ReaderKey, _socketMessage, _socket);

                                        //ReadWindow rw = new ReadWindow(cbBooksData, OpenBookFromReader, _bookPath, _bookId, _account
                                        //                      , _userName, _email, _meetingId
                                        //                      , _watermark, _dbPath, _isSync
                                        //                      , _isSyncOwner, _webServiceUrl, _socketMessage, _socket);

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
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    HTML5ReadWindow Html5rw = new HTML5ReadWindow(cbBooksData, OpenBookFromReader, _bookPath, _bookId, _account
                                                          , _userName, _email, _meetingId
                                                          , _watermark, _dbPath, _isSync
                                                          , _isSyncOwner, _webServiceUrl, _socketMessage, _socket);
                                    Html5rw.WindowStyle = WindowStyle.None;
                                    Html5rw.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                                    Html5rw.WindowState = WindowState.Maximized;
                                    if (PaperLess_Emeeting.Properties.Settings.Default.AssemblyName.Contains("TPI4F") == true)
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
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    MVWindow mvWindow = new MVWindow(cbBooksData, OpenBookFromReader, _bookPath, InitMsg);
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
                        default:
                            break;
                    }
                }
                catch(Exception ex)
                {
                    LogTool.Debug(ex);
                }

            }
        }

        private void TurnOffSyncButton()
        {
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action(TurnOffSyncButton));
                this.Dispatcher.BeginInvoke(new Action(TurnOffSyncButton));
            }
            else
            {
                //btnSP.Children.OfType<MenuButton>().ToList().ForEach(x =>
                foreach(MenuButton x in btnSP.Children.OfType<MenuButton>())
                {
                    if (x.userButton.ID.Equals("BtnSync") == true)
                    {
                        IsInSync = false;
                        IsSyncOwner = false;
                        x.btnImg.Source = ButtonTool.GetSyncButtonImage(IsInSync, IsSyncOwner);
                    }
                }
                //});
            }
        }

        public void CloseAllWindow(string AlertMessage,bool fromInit=false)
        {
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action<string, bool>(CloseAllWindow), AlertMessage, fromInit);
                this.Dispatcher.BeginInvoke(new Action<string, bool>(CloseAllWindow), AlertMessage, fromInit);
            }
            else
            {
                try
                {
                    if (IsInSync == false && fromInit == false)
                        return;
                    Application app = Application.Current;

                    IEnumerable<ReadWindow> rws = app.Windows.OfType<ReadWindow>();
                    IEnumerable<HTML5ReadWindow> html5rws = app.Windows.OfType<HTML5ReadWindow>();
                    IEnumerable<MVWindow> mvWindows = app.Windows.OfType<MVWindow>();

                    //Task.Factory.StartNew(() =>
                    //{
                    //this.Dispatcher.BeginInvoke(new Action(() => {
                    // 效能不好
                    //rws.Concat(html5rws).Concat(mvWindows).ToList().ForEach(x =>
                    //{
                    //    if (x != null)
                    //        x.Close(); 
                    //});

                    // 為了效能好一點，多寫一點Code
                    int totalWindow = rws.Count() + html5rws.Count() + mvWindows.Count();

                    if (AlertMessage.Equals("NoConsole") == true && totalWindow > 0)
                        AlertMessage = "現在沒有主控者";
                    else
                        AlertMessage = "";
                    if (AlertMessage.Equals("") == false)
                        AutoClosingMessageBox.Show(AlertMessage);

                    // 為了效能好一點，多寫一點Code
                    foreach (ReadWindow item in rws) { if (item != null) { item.RecordPage(); item.Hide(); item.Close(); } };
                    foreach (HTML5ReadWindow item in html5rws) { if (item != null) { item.RecordPage(); item.Hide(); item.Close(); } };
                    foreach (MVWindow item in mvWindows)
                    {
                        if (item != null)
                        {
                            item.mediaPlayer.Stop();
                            item.mediaPlayer.Close();
                            item.Close();
                        }
                    };
                    //}));
                    //});
                }
                catch(Exception ex)
                {
                    LogTool.Debug(ex);
                }
             
            }
        }

        private void IsInSync_And_IsSyncOwner(JArray jArry)
        {
            //先判斷是否要invoke
            //if (this.Dispatcher.CheckAccess() == false)
            //{
            //    // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
            //    this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action<JArray>(IsInSync_And_IsSyncOwner), jArry);
            //}
            //else
            //{
                //if (jArry.ToString().Contains("\"clientId\":\"" + UserID + "\"") == false)
                //{
                //    IsInSync = false;
                //    IsSyncOwner = false;
                //    ChangeSyncButtonLight(IsInSync, IsSyncOwner);
                //    return;
                //}

                //Task.Factory.StartNew(() =>
                //{
                    foreach (JToken item in jArry)
                    {

                        // [{\"clientId\":\"kat\",\"clientName\":\"kat\",\"clientType\":1,\"status\":0,\
                        Dictionary<string, object> dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(item.ToString());
                        string clientId = dict["clientId"].ToString();
                        clientId = Socket_FixEmailUserID.FromSocket(clientId);
                        string clientName = dict["clientName"].ToString();
                        string clientType = dict["clientType"].ToString();
                        string status = dict["status"].ToString();

                        if (clientId.Equals(UserID))
                        {
                            switch (status)
                            {
                                case "-1": //主控者
                                    IsInSync = true;
                                    IsSyncOwner = true;
                                    break;
                                case "0": //被同步者
                                    IsInSync = true;
                                    IsSyncOwner = false;
                                    break;
                                case "1": //沒有同步
                                    IsInSync = false;
                                    IsSyncOwner = false;
                                    break;
                                default:
                                    IsInSync = false;
                                    IsSyncOwner = false;
                                    break;
                            }
                            break;
                        }

                    }
                    ChangeSyncButtonLight(IsInSync,IsSyncOwner);
                    
                    // 判斷沒有主控的時候
                    if (jArry.ToString().Replace(" ", "").Contains("\"status\":-1,") == false  && IsInSync ==true)
                    {
                        CloseAllWindow("NoConsole");
                    }
                  
                //});

               

                
            //}
        }

        private void ChangeSyncButtonLight(bool IsInSync, bool IsSyncOwner)
        {

            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action<bool,bool>(ChangeSyncButtonLight), IsInSync, IsSyncOwner);
                this.Dispatcher.BeginInvoke(new Action<bool, bool>(ChangeSyncButtonLight), IsInSync, IsSyncOwner);
            }
            else
            {
                //btnSP.Children.OfType<MenuButton>().ToList().ForEach(item =>
                foreach(MenuButton item in btnSP.Children.OfType<MenuButton>())
                {
                    if (item.userButton.ID.Equals("BtnSync") == true)
                    {
                        item.btnImg.Source = ButtonTool.GetSyncButtonImage(IsInSync, IsSyncOwner);
                    }
                } 
                //});
            }
           
        }

        private void Home_UnZipError_Callback(string message)
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 解壓縮失敗的事件處理，有空閒在Show出就好，優先權設定為ApplicationIdle => 列舉值為 2。 當應用程式處於閒置狀態時，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action<string>(Home_UnZipError_Callback), message);
                this.Dispatcher.BeginInvoke(new Action<string>(Home_UnZipError_Callback), message);
            }
            else
            {
                AutoClosingMessageBox.Show(message);
            }
        }
        private void InitUI()
        {
            this.Title = Settings.Default.AppName;

            if (PaperLess_Emeeting.Properties.Settings.Default.AssemblyName.Contains("TPI4F") == true)
            {
                Row1.Height = new GridLength(100);
                imgBeta.Height = 100 - 2;
                imgBeta.Width = 100 - 2;
                imgLogo.Width = 340;
                imgLogo.Height = 85;
                imgLogo.Stretch = Stretch.Uniform;

                //Row1.Height = new GridLength(120);
                //imgBeta.Height = 120 - 2;
                //imgBeta.Width = 120 - 2;
                //imgLogo.Width = 420;
                //imgLogo.Height = 95;
                //imgLogo.Stretch = Stretch.Uniform;
            }

            imgLogo.Source = new BitmapImage(new Uri(PaperLess_Emeeting.Properties.Settings.Default.Home_Logo_Image, UriKind.Relative));

            Enum.TryParse(PaperLess_Emeeting.Properties.Settings.Default.DisplayUserNameMode, out displayUserNameMode);

            switch(displayUserNameMode)
            {
                case DisplayUserNameMode.None:
                   
                    break;
                case DisplayUserNameMode.UserID_UserName:
                    txtUserName.Text = string.Format("{0}\r\n{1}",UserID,UserName);
                    blockUserName.Visibility = Visibility.Visible;
                    break;
                case DisplayUserNameMode.UserName:
                    txtUserName.Text = UserName;
                    blockUserName.Visibility = Visibility.Visible;
                    break;
            }
            
            ShowMeetingListCT(true);

            // 這裡需要花一點時間，所以開線程去做
            // 直接略過，產生Button的時間
            Task.Factory.StartNew(() =>
                {
                    ChangeBtnSP(UserButtonAry, "BtnHome");
                });

            if (PaperLess_Emeeting.Properties.Settings.Default.IsDebugMode == true)
            {
                imgBeta.Visibility = Visibility.Visible;
            }
        }

        private void ChangeBtnSP(UserButton[] UserButtonAry, string ActiveButtonID)
        {
             //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                this.Dispatcher.BeginInvoke(new Action<UserButton[], string>(ChangeBtnSP), UserButtonAry, ActiveButtonID);
            }
            else
            {
                btnSP.Children.Clear();

               
                foreach (UserButton item in UserButtonAry)
                {
                    if (item.ID.Equals("BtnVote"))
                    {
                        continue;
                    }

                    if (PaperLess_Emeeting.Properties.Settings.Default.AssemblyName.Contains("TPI4F") == true)
                    {
                        if (item.ID.Equals("BtnAttendance") || item.ID.Equals("BtnIndividualSign"))
                            continue;
                    }
                    MenuButton mb = null;
                    if (item.ID.Equals("BtnQuit") == true)
                    {
                        mb = new MenuButton(item, Home_ChangeCC_Event_Callback, PopUpButtons, ChangeBtnSP);
                    }
                    else
                    {
                        mb = new MenuButton(item, Home_ChangeCC_Event_Callback, PopUpButtons);

                        if (item.ID.Equals("BtnSync") == true)
                        {
                            mb.Home_ReturnSyncStatus_Event += () => { return new Tuple<bool, bool>(IsInSync, IsSyncOwner); };
                            mb.btnImg.Source = ButtonTool.GetSyncButtonImage(IsInSync, IsSyncOwner);
                        }
                    }

                    if (item.ID.Equals("BtnSync") == false)
                    {
                        bool IsActived = false;
                        if (item.ID.Equals(ActiveButtonID))
                            IsActived = true;

                        mb.btnImg.Source = ButtonTool.GetButtonImage(item.ID, IsActived);
                        NowPressButtonID = ActiveButtonID;
                    }
                    else
                    {
                        mb.btnImg.Source = ButtonTool.GetSyncButtonImage(IsInSync, IsSyncOwner);
                    }

                    btnSP.Children.Add(mb);

                }

                //按鈕debug用
                //MenuButton mb2 = new MenuButton(new UserButton() { ID = "BtnExportPDF", Name = "匯出附件" }, Home_ChangeCC_Event_Callback);
                //mb2.btnImg.Source = ButtonTool.GetButtonImage("BtnExportPDF");
                //btnSP.Children.Add(mb2);
            }
        }

        private void ShowMeetingListCT(bool GotoToday,Action Callback=null)
        {
           
            //InitSelectDB();
            DateTime date=DateTime.Today;
            if (GotoToday == false)
                date = MeetingListDate;

            MouseTool.ShowLoading();

          
             
             Network.HttpRequest hr = new Network.HttpRequest();
             if (NetworkTool.CheckNetwork() > 0)
             {
                 // 非同步POST方法
                 //GetUserData.AsyncPOST(UserID, UserPWD
                 //       , date
                 //       , (userObj, dateTime) => { GetUserData_DoAction(userObj, dateTime); });
                 //, () => { this.Dispatcher.BeginInvoke(new Action(() => { AutoClosingMessageBox.Show("無法取得資料，請稍後再試"); })); });

                 if (NetworkTool.CheckNetwork() > 0)
                 {
                     GetUserData.AsyncPOST(UserID, UserPWD
                                       , date
                                       , (userObj, dateTime) =>
                                       {
                                           GetUserData_DoAction(userObj, dateTime);
                                           if (Callback != null)
                                           {
                                               this.Dispatcher.BeginInvoke( new Action(()=> { Callback(); }));
                                           }
                                       }
                                       , () => { this.Dispatcher.BeginInvoke(new Action(() => { PopUpButtons(NowPressButtonID); })); }
                                        );
                 }
                 else
                 {
                     //DB查詢日期
                     DataTable dt = MSCE.GetDataTable("select UserJson from UserData where UserID =@1 and ListDate=@2"
                                                      , UserID
                                                      , DateTool.MonthFirstDate(MeetingListDate).ToString("yyyyMMdd"));

                     User user = new User();
                     if (dt.Rows.Count > 0)
                     {
                         user = JsonConvert.DeserializeObject<User>(dt.Rows[0]["UserJson"].ToString());
                     }
                     else
                     {
                         dt = MSCE.GetDataTable("select top 1 UserJson from UserData where UserID =@1"
                                               , UserID);

                         if (dt.Rows.Count > 0)
                         {
                             user = JsonConvert.DeserializeObject<User>(dt.Rows[0]["UserJson"].ToString());
                         }
                         user.MeetingList = new UserMeeting[0];

                     }

                     GetUserData_DoAction(user, MeetingListDate);
                 }
             }
             else
             {
                 GetUserData_DoAction(user,date);
             }

            #region 同步POST方法
            //User user = GetUserData.POST(UserID, UserPWD,
            //                             DateTool.MonthFirstDate(date).ToString("yyyyMMdd"),
            //                             DateTool.MonthLastDate(date).ToString("yyyyMMdd"));

            //if (user != null)
            //{
            //    CC.Content = new MeetingListCT(user.MeetingList, date, () => { CC.Content = new MeetingDataCT(ChangeBtnSP); });
            //}
            //else
            //{
            //    AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
            //}
            #endregion

        }

        private void GetUserData_DoAction(User user,DateTime date)
        {
              //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action<User, DateTime>(GetUserData_DoAction), user, date);
                this.Dispatcher.BeginInvoke(new Action<User, DateTime>(GetUserData_DoAction), user, date);
            }
            else
            {
                if (user != null)
                {

                    // callback可能尚未new出來，
                    // 因為，InitUI(); 比 InitEvent();先執行
                    // 所以可能會傳到null進去
                    // 現在改成直接傳方法過去
                    //CC.Content = new MeetingListCT(user.MeetingList, date, Home_Change2MeetingDataCT_Callback);
                    CC.Content = new MeetingListCT(user.MeetingList, date, Change2MeetingDataCT);
                }
                else
                {
                    CC.Content = new MeetingListCT(null, date, Change2MeetingDataCT);
                    AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
                }
                MouseTool.ShowArrow();
            }
        }


        // 20141203
        // 依賴傳入的MeetingID
        // 這個callback方法是給MeetingRoom的滑鼠左鍵點擊所呼叫的
        // 會傳入MeetingID
        public void Change2MeetingDataCT(string MeetingID, MeetingData meetingData = null)
        {
             MouseTool.ShowLoading();
             //GetMeetingData.AsyncPOST(MeetingID, UserID, UserPWD, (md) => { GetMeetingData_DoAction(UserID, UserName, UserPWD, UserEmail,md); });
             
             Network.HttpRequest hr = new Network.HttpRequest();
             if (NetworkTool.CheckNetwork() > 0)
             {
                 if (meetingData==null)
                     GetMeetingData.AsyncPOST(MeetingID, UserID, UserPWD, (md) => { GetMeetingData_DoAction(md, true); });
                 else
                     GetMeetingData_DoAction(meetingData,true);
             }
             else
             {
                   //DB查詢登入
                    DataTable dt = MSCE.GetDataTable("select MeetingJson from MeetingData where MeetingID=@1 and UserID =@2" 
                                                    , MeetingID
                                                    , UserID);

                    if (dt.Rows.Count > 0)
                    {
                        MeetingData md = JsonConvert.DeserializeObject<MeetingData>(dt.Rows[0]["MeetingJson"].ToString());
                        GetMeetingData_DoAction(md);
                    }
                    else
                    {
                        AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
                        MouseTool.ShowArrow();
                    }
             }
            
        }

        // 不包含MeetingID
        //private void GetMeetingData_DoAction(string UserID, string UserName, string UserPWD, string  UserEmail, MeetingData md)
        //{

        //    //先判斷是否要invoke
        //    if (this.Dispatcher.CheckAccess() == false)
        //    {
        //        // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
        //        this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action<string, string, string,  string, MeetingData>(GetMeetingData_DoAction), UserID, UserName, UserPWD, UserEmail, md);
        //    }
        //    else
        //    {
                
        //        if (md != null)
        //        {
        //           MouseTool.ShowLoading();
        //           CC.Content = new MeetingDataCT(UserID, UserName, UserPWD, UserEmail, md, ChangeBtnSP);
        //        }
        //        else
        //        {
        //            AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
                   
        //        }
        //        MouseTool.ShowArrow();
        //    }
            
        //}
       
       

        //private void Home_ChangeCC_Event_Callback(string ButtonID)
        private void Home_ChangeCC_Event_Callback(UserButton userButton)
        {

            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action<string>(Home_ChangeCC_Event_Callback), ButtonID);
                //this.Dispatcher.BeginInvoke(new Action<string>(Home_ChangeCC_Event_Callback), ButtonID);
                this.Dispatcher.BeginInvoke(new Action<UserButton>(Home_ChangeCC_Event_Callback), userButton);
            }
            else
            {
                //if (ButtonID.Equals("BtnSync") == true)
                if (userButton.ID.Equals("BtnSync") == true)
                {
                    return;
                }

                //PopUpButtons(ButtonID);
                ////btnSP.Children.OfType<MenuButton>().ToList().ForEach(mb =>
                //foreach (MenuButton mb in btnSP.Children.OfType<MenuButton>())
                //{
                //    if (ButtonID.Equals("BtnQuit") == true && mb.userButton.ID.Equals("BtnHome") == true)
                //    {
                //        mb.btnImg.Source = ButtonTool.GetButtonImage(mb.userButton.ID, true);
                //    }
                //    else
                //    {
                //        if (mb.userButton.ID.Equals("BtnSync") == false)
                //        {
                //            bool IsActived = false;
                //            if (mb.userButton.ID.Equals(ButtonID) == true)
                //                IsActived = true;
                //            mb.btnImg.Source = ButtonTool.GetButtonImage(mb.userButton.ID, IsActived);
                //        }
                //    }
                //}
                ////});

                //ThreadPool.QueueUserWorkItem(callback =>
                //Task.Factory.StartNew(() =>
                //{
                //    Clear_FileDownloaderEvent();
                //    Clear_LawDownloaderEvent();
                //});

                switch (userButton.ID)
                {
                    case "BtnHome":
                        string SQL = @"update NowLogin Set MeetingListDate=@1";
                        int success = MSCE.ExecuteNonQuery(SQL, DateTime.Today.ToString("yyyy/MM/dd"));
                        ShowMeetingListCT(true);
                        break;
                    case "BtnSeries":
                        ShowBtnSeriesCT();
                        break;
                    case "BtnLaw":
                    case "BtnFile":
                        //採取先new 畫面再抓資料，所以按鈕不會停頓
                        CC.Content = new LawListCT(userButton.Name);
                        break;
                    case "go2Back":
                    case "BtnFolder":
                        //採取先new 畫面再抓資料，所以按鈕不會停頓
                        CC.Content = new FolderListCT(userButton.Name, (sender, dictArgs) => {
                            string FolderID = (string)dictArgs.dict["FolderID"];
                            CC.Content = new FileListCT(userButton.Name, FolderID, Home_ChangeCC_Event_Callback);
                        });
                        break;
                    case "BtnExportPDF":
                        ShowPDFFactoryCT();
                        break;
                    case "BtnSignin":
                        ShowSignPictureCT();
                        break;
                    case "BtnSigninList":
                        //採取先new 畫面再抓資料，所以按鈕不會停頓
                        CC.Content = new SignListCT();
                        break;
                    case "BtnAttendance":
                        CC.Content = new SignListCT_Mix();
                        break;
                    case "BtnMeeting":
                        //為了加快速度，採取先抓資料再new 畫面，所以有資料才會有畫面
                        ShowMeetingDataCT();
                        break;
                    case "BtnIndividualSign":
                        DataTable dt2 = MSCE.GetDataTable("select MeetingID from NowLogin");
                        if (dt2.Rows.Count > 0)
                        {
                            string MeetingID = dt2.Rows[0]["MeetingID"].ToString();
                            // 非同步POST方法
                            GetSigninData.AsyncPOST(MeetingID, (sid) => { GetSigninData_DoAction(sid); });
                        }
                        //CC.Content = new SignPadCT();
                        break;
                    case "BtnVote":
                        //CC.Content = null;
                        AutoClosingMessageBox.Show("敬請期待");
                        PopUpButtons(NowPressButtonID);
                        break;
                    case "BtnQuit":

                        DataTable dt = MSCE.GetDataTable("select MeetingListDate,HomeUserButtonAryJSON from NowLogin");
                        if (dt.Rows.Count > 0)
                        {
                            MeetingListDate = (DateTime)dt.Rows[0]["MeetingListDate"];
                            //string HomeUserButtonAryJSON = dt.Rows[0]["HomeUserButtonAryJSON"].ToString();
                            ShowMeetingListCT(false, () => { ShowMeetingListCT_Callback(); });
                            //ChangeBtnSP(JsonConvert.DeserializeObject<UserButton[]>(HomeUserButtonAryJSON), "BtnHome");
                        }
                        break;
                    case "BtnBroadcast":
                        CC.Content = new BroadcastCT();
                        break;
                    case "BtnLogout":
                        LogOut();
                        ////Login f2 = new Login();
                        //Login f2 =Application.Current.Windows.OfType<Login>().First();
                        //f2.Show();
                        //this.Close();

                        break;
                }

                Task.Factory.StartNew(() =>
                {
                    Clear_FileDownloaderEvent();
                    Clear_LawDownloaderEvent();
                });
            }
        }

        private void ShowMeetingListCT_Callback()
        {

            DataTable dt = MSCE.GetDataTable("select MeetingListDate,HomeUserButtonAryJSON from NowLogin");
            if (dt.Rows.Count > 0)
            {
                //MeetingListDate = (DateTime)dt.Rows[0]["MeetingListDate"];
                string HomeUserButtonAryJSON = dt.Rows[0]["HomeUserButtonAryJSON"].ToString();
                //ShowMeetingListCT(false);
                ChangeBtnSP(JsonConvert.DeserializeObject<UserButton[]>(HomeUserButtonAryJSON), "BtnHome");
                NowPressButtonID = "BtnQuit";
            }

            Task.Factory.StartNew(() =>
            {
                try
                {
                    SocketClient socketClient = Singleton_Socket.GetInstance();
                    if (socketClient != null && socketClient.GetIsConnected() == true && IsInSync == true)
                    {
                        socketClient.syncSwitch(false);
                        socketClient.logout();
                    }

                    //Thread.Sleep(100);
                    //if (IsInSync == true)
                    //{
                    //    IEnumerable<MenuButton> mbs = this.btnSP.Children.OfType<MenuButton>().Where(x => x.userButton.ID.Equals("BtnSync"));
                    //    if (mbs != null)
                    //    {
                    //        MenuButton mb = mbs.First();
                    //        if (mb != null)
                    //        {
                    //            MouseButtonEventArgs args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 100, MouseButton.Left);
                    //            args.RoutedEvent = UIElement.MouseLeftButtonDownEvent;
                    //            mb.RaiseEvent(args);
                    //        }
                    //    }
                    //}
                }
                catch (Exception ex)
                {
                    LogTool.Debug(ex);
                }

                IsInSync = false;
                IsSyncOwner = false;
                Singleton_Socket.ClearInstance();

            });
        }

        public void PopUpButtons(string ButtonID)
        {
            //btnSP.Children.OfType<MenuButton>().ToList().ForEach(mb =>
            foreach (MenuButton mb in btnSP.Children.OfType<MenuButton>())
            {
                if (ButtonID.Equals("BtnQuit") == true && mb.userButton.ID.Equals("BtnHome") == true)
                {
                    mb.btnImg.Source = ButtonTool.GetButtonImage(mb.userButton.ID, true);
                   
                }
                else
                {
                    if (mb.userButton.ID.Equals("BtnSync") == false)
                    {
                       
                            bool IsActived = false;
                            if (mb.userButton.ID.Equals(ButtonID) == true)
                                IsActived = true;
                            mb.btnImg.Source = ButtonTool.GetButtonImage(mb.userButton.ID, IsActived);
                    }
                }

                if (ButtonID.Equals("BtnMeeting") == false && ButtonID.Equals("BtnQuit") == false && ButtonID.Equals("BtnSeries") == false && ButtonID.Equals("BtnVote") == false)
                {
                    NowPressButtonID = ButtonID;
                }
            }
            //});
        }

        private void ShowPDFFactoryCT()
        {
            PDFFactoryCT PDFCT = new PDFFactoryCT();
            CC.Content = PDFCT;
        }

        private void StopAllBackground()
        {
            try
            {
                Singleton_Socket.home_OpenIEventManager.Home_OpenBook_Event -= Home_OpenBook_Callback;
                Singleton_Socket.home_OpenIEventManager.Home_IsInSync_And_IsSyncOwner_Event -= Home_IsInSync_And_IsSyncOwner_Callback;
                Singleton_Socket.home_OpenIEventManager.Home_CloseAllWindow_Event -= Home_CloseAllWindow_Callback;

                Singleton_Socket.home_CloseIEventManager.Home_CloseAllWindow_Event -= Home_CloseAllWindow_Callback;
                Singleton_Socket.home_CloseIEventManager.Home_TurnOffSyncButton_Event -= Home_TurnOffSyncButton_Callback;
                Singleton_Socket.home_CloseIEventManager.Home_SetSocketClientNull_Event -= Home_SetSocketClientNull_Callback;

                Singleton_Socket.ClearInstance();
                IsInSync = false;
                IsSyncOwner = false;

                //ThreadPool.QueueUserWorkItem(callback =>
                Task.Factory.StartNew(() =>
                {
                    // 暫停法規下載
                    LawDownloader lawDownloader = Singleton_LawDownloader.GetInstance();
                    lawDownloader.ClearAllEvent();
                    lawDownloader.Stop();


                    // 暫停所有會議ID的檔案下載
                    Dictionary<string, FileDownloader> dict = Singleton_FileDownloader.GetInstance();
                    //dict.ToList().ForEach(item => item.Value.Stop() );
                    foreach (KeyValuePair<string, FileDownloader> item in dict)
                    {
                        item.Value.ClearAllEvent();
                        item.Value.Stop();
                    }
                    dict.Clear();


                    //清除PDF工作列表
                    Singleton_PDFFactory.ClearInstance();
                });
            }
            catch(Exception ex)
            {
                LogTool.Debug(ex);
            }
        }

        private void GetSigninData_DoAction(SigninData sid)
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                this.Dispatcher.BeginInvoke(new Action<SigninData>(GetSigninData_DoAction), sid);
            }
            else
            {
                if (sid != null)
                {
                    Task.Factory.StartNew(() =>
                    {
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            foreach (SigninDataUser item in sid.UserList)
                            {
                                if (item.ID.Equals(UserID) == true)
                                {
                                    CC.Content = new SignPadCT("","","",item.SignedPic,null);
                                }
                            }
                        }));
                    });
                }
                else
                {
                    AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
                }

                MouseTool.ShowArrow();
            }
        }

        // 這個事件是給已經在會議室裡面
        // 但是是由BtnMeeting所觸發的
        // 因為傳入MeetingID比較困難
        // 所以用查詢DB的方式
        // 查詢目前會議的MeetingID
        private void ShowMeetingDataCT()
        {
          
            string MeetingID="";
            DataTable dt= MSCE.GetDataTable("select MeetingID from NowLogin");
            if (dt.Rows.Count > 0)
            {
                MeetingID = dt.Rows[0]["MeetingID"].ToString().Trim();
            }
            MouseTool.ShowLoading();

            // GetMeetingData.AsyncPOST(MeetingID, UserID, UserPWD, (md) => { GetMeetingData_DoAction(md); });
            if (NetworkTool.CheckNetwork() > 0)
            {
                GetMeetingData.AsyncPOST(MeetingID, UserID, UserPWD, (md) =>
                    {
                        GetMeetingData_DoAction(md);
                        NowPressButtonID = "BtnMeeting";
                    }
                    ,() => { this.Dispatcher.BeginInvoke(new Action(() => 
                    {
                        PopUpButtons(NowPressButtonID);
                      
                    }));
                });
            }
            else
            {
                //DB查詢登入
                dt = MSCE.GetDataTable("select MeetingJson from MeetingData where MeetingID=@1 and UserID =@2"
                                                , MeetingID
                                                , UserID);

                if (dt.Rows.Count > 0)
                {
                    MeetingData md = JsonConvert.DeserializeObject<MeetingData>(dt.Rows[0]["MeetingJson"].ToString());
                    GetMeetingData_DoAction(md);
                }
                else
                {
                    AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
                    MouseTool.ShowArrow();
                }

          
            }
        }

        private void GetMeetingData_DoAction(MeetingData md, bool isFirstAutoTurnOnSync=false)
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                this.Dispatcher.BeginInvoke(new Action<MeetingData, bool>(GetMeetingData_DoAction), md, isFirstAutoTurnOnSync);
            }
            else
            {
                if (md != null)
                {

                    CC.Content = new MeetingDataCT(UserID, UserName, UserPWD, UserEmail, md, ChangeBtnSP, isFirstAutoTurnOnSync);

                    //DB操作更新
                    DataTable dt = MSCE.GetDataTable("select MeetingID from MeetingData where MeetingID=@1 and UserID =@2"
                                                    , md.ID
                                                    , user.ID);

                    if (dt.Rows.Count > 0)
                    {
                        MSCE.ExecuteNonQuery(@"UPDATE [MeetingData] SET 
                                                 [MeetingJson] = @1 where MeetingID=@2 and UserID =@3"
                                            , JsonConvert.SerializeObject(md)
                                            , md.ID
                                            , user.ID);
                    }
                    else
                    {
                        MSCE.ExecuteNonQuery(@"INSERT INTO [MeetingData] ([MeetingID],[MeetingJson],UserID)
                                                            VALUES (@1,@2,@3)"
                                            , md.ID
                                            , JsonConvert.SerializeObject(md)
                                            , user.ID);
                    }

                }
                else
                {
                    AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
                    MouseTool.ShowArrow();
                }
              
            }
        }
        public void ShowBtnSeriesCT(string NowSeriesID="")
        {
            MouseTool.ShowLoading();


            Network.HttpRequest hr = new Network.HttpRequest();
            if (NetworkTool.CheckNetwork() > 0)
            {
                //無快取機制
                // 非同步POST方法
                //GetSeriesData.AsyncPOST(UserID, (sd) => { GetSeriesData_DoAction(sd, NowSeriesID); });

                //有快取機制
                if (PreLoadSeriesDataDict.ContainsKey("BtnSeries") == true)
                {
                    GetSeriesData_DoAction(PreLoadSeriesDataDict["BtnSeries"], NowSeriesID);

                    //預載一次就好
                    //PreLoadSeriesDataDict.Remove("BtnSeries");
                    //預載下一次
                    PreLoadSeriesData();
                }
                else
                {
                    GetSeriesData.AsyncPOST(UserID, (sd) => 
                                            { 
                                                GetSeriesData_DoAction(sd, NowSeriesID);
                                                PreLoadSeriesData();
                                            });
                }
            }
            else
            {
                //DB查詢日期
                DataTable dt = MSCE.GetDataTable("select SeriesJson from SeriesData where UserID =@1 "
                                                 , UserID
                                                 , DateTool.MonthFirstDate(MeetingListDate).ToString("yyyyMMdd"));

               
                if (dt.Rows.Count > 0)
                {
                    SeriesData sd = JsonConvert.DeserializeObject<SeriesData>(dt.Rows[0]["SeriesJson"].ToString());
                    GetSeriesData_DoAction(sd, NowSeriesID);
                }
                else
                {
                        AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
                        MouseTool.ShowArrow();
                }
                
            }
            
        }

        private void GetSeriesData_DoAction(SeriesData sd,string NowSeriesID="")
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                this.Dispatcher.BeginInvoke(new Action<SeriesData, string>(GetSeriesData_DoAction), sd, NowSeriesID);
            }
            else
            {
                if(sd!=null)
                {
                    //debug
                    //if (1==1)
                    if (sd.SeriesMeeting == null)
                    {
                        AutoClosingMessageBox.Show(string.Format("現在尚無系列{0}", PaperLess_Emeeting.Properties.Settings.Default.CourseOrMeeting_String));
                        PopUpButtons(NowPressButtonID);
                        MouseTool.ShowArrow();
                        return;
                    }
                    else
                    {
                        NowPressButtonID = "BtnSeries";
                    }

                    // 傳入預設的SeriesMenu的ID
                    if (NowSeriesID.Equals("")==true && sd.SeriesMeeting.Length > 0)
                    {
                        NowSeriesID=sd.SeriesMeeting[0].Series.ID;
                    }

                    //CC.Content = new SeriesMeetingCT(UserID, UserPWD, sd, NowSeriesID);
                    Task.Factory.StartNew(() =>
                        {
                                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action(() =>
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    CC.Content = new SeriesMeetingCT(UserID, UserPWD, sd, NowSeriesID);
                                }));
                        });

                    DataTable dt = MSCE.GetDataTable("select SeriesJson from SeriesData where UserID =@1"
                                                   , user.ID);

                    if (dt.Rows.Count > 0)
                    {
                        MSCE.ExecuteNonQuery(@"UPDATE [SeriesData] SET 
		                                         [SeriesJson] = @1
		                                         where UserID = @2"
                                   , JsonConvert.SerializeObject(sd)
                                   , user.ID);
                    }
                    else
                    {
                        MSCE.ExecuteNonQuery(@"INSERT INTO [SeriesData] ([SeriesJson],[UserID])
                                                            VALUES (@1,@2)"
                                             , JsonConvert.SerializeObject(sd)
                                             , user.ID);
                    }
                }
                else
                {
                    AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
                }
                 MouseTool.ShowArrow();
            }
        }



        public void OpenBookFromReader(string MeetingID, BookVM bookVM, Dictionary<string, BookVM> cbBooksData, string watermark = "")
        {
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                this.Dispatcher.BeginInvoke(new Action<string, BookVM, Dictionary<string, BookVM>, string>(OpenBookFromReader), MeetingID, bookVM, cbBooksData, watermark);
            }
            else
            {
                MouseTool.ShowLoading();
                // 關閉全部
                try
                {
                    IEnumerable<ReadWindow> rws = Application.Current.Windows.OfType<ReadWindow>();
                    IEnumerable<HTML5ReadWindow> html5rws = Application.Current.Windows.OfType<HTML5ReadWindow>();
                    IEnumerable<MVWindow> mvWindows = Application.Current.Windows.OfType<MVWindow>();
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // 為了效能好一點，多寫一點Code
                        foreach (ReadWindow item in rws) { if (item != null) { item.RecordPage(); item.Hide(); item.Close(); } };
                        foreach (HTML5ReadWindow item in html5rws) { if (item != null) { item.RecordPage(); item.Hide(); item.Close(); } };
                        foreach (MVWindow item in mvWindows) { if (item != null) { item.Hide(); item.Close(); }};
                    }));
                    

                    string dataPath = ClickOnceTool.GetDataPath();
                    string filePath = ClickOnceTool.GetFilePath();

                    string _bookPath = bookVM.BookPath;
                    string _bookId = bookVM.FileID;
                    string _account = UserID;
                    string _userName = UserName;
                    string _email = UserEmail;
                    string _meetingId = MeetingID;
                    string _watermark = "";
                    string _dbPath = System.IO.Path.Combine(dataPath, PaperLess_Emeeting.Properties.Settings.Default.bookInfo_Path);
                    bool _isSync = IsInSync;
                    bool _isSyncOwner = IsSyncOwner;
                    string _webServiceUrl = WsTool.GetUrl() + "/AnnotationUpload";
                    string _socketMessage = "";


                    SocketClient _socket = Singleton_Socket.GetInstance(MeetingID, UserID, UserName, IsInSync);
                    #region 後來想想 cbBooksData一定不會是null，因為一定是從Reader裡面打開的，所以去掉
                    //if (cbBooksData == null)
                    //{
                    //    // 要先查詢要打開的影片或書籍
                    //    cbBooksData = new Dictionary<string, BookVM>();

                    //}
                    //else
                    //{


                    //}
                    #endregion
                    switch (bookVM.FileCate)
                    {
                        case MeetingFileCate.電子書:
                            Task.Factory.StartNew(() =>
                            {
                                this.Dispatcher.BeginInvoke((Action)(() =>
                                {
                                   
                                    byte[] ReaderKey = new byte[1];
                                    try
                                    {
                                        DataTable dt = MSCE.GetDataTable("SELECT EncryptionKey FROM FileRow where ID=@1 and UserID=@2 and MeetingID=@3"
                                                                        , _bookId
                                                                        , _account
                                                                        , _meetingId);

                                        if (dt.Rows.Count > 0)
                                        {
                                            ReaderKey = ReaderDecodeTool.GetReaderKey(dt.Rows[0]["EncryptionKey"].ToString());
                                        }
                                    }
                                    catch(Exception ex)
                                    {
                                        LogTool.Debug(ex);
                                    }

                                    ReadWindow rw = new ReadWindow(cbBooksData, OpenBookFromReader, _bookPath, _bookId, _account
                                                  , _userName, _email, _meetingId
                                                  , _watermark, _dbPath, _isSync
                                                  , _isSyncOwner, _webServiceUrl, ReaderKey, _socketMessage, _socket);

                                    //ReadWindow rw = new ReadWindow(cbBooksData, OpenBookFromReader, _bookPath, _bookId, _account
                                    //                      , _userName, _email, _meetingId
                                    //                      , _watermark, _dbPath, _isSync
                                    //                      , _isSyncOwner, _webServiceUrl, _socketMessage, _socket);

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

                            Task.Factory.StartNew(() =>
                            {
                                this.Dispatcher.BeginInvoke((Action)(() =>
                                {
                                    HTML5ReadWindow Html5rw = new HTML5ReadWindow(cbBooksData, OpenBookFromReader, _bookPath, _bookId, _account
                                                           , _userName, _email, _meetingId
                                                           , _watermark, _dbPath, _isSync
                                                           , _isSyncOwner, _webServiceUrl, _socketMessage, _socket);
                                    Html5rw.WindowStyle = WindowStyle.None;
                                    Html5rw.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                                    Html5rw.WindowState = WindowState.Maximized;
                                    if (PaperLess_Emeeting.Properties.Settings.Default.AssemblyName.Contains("TPI4F") == true)
                                    {
                                       // Html5rw.WindowStyle = WindowStyle.SingleBorderWindow;
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

                                    MVWindow mvWindow = new MVWindow(cbBooksData, OpenBookFromReader, _bookPath,_bookId);
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

                    if (IsInSync == true && IsSyncOwner == true)
                    {
                        SocketClient socketClient = Singleton_Socket.GetInstance(MeetingID, UserID, UserName, IsInSync);
                        Task.Factory.StartNew(() =>
                        {
                            if (socketClient != null && socketClient.GetIsConnected() == true)
                            {
                                string OB = "{\"bookId\":\"" + MeetingID + "\",\"cmd\":\"R.OB\"}";
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
           
        }

        private void Clear_LawDownloaderEvent()
        {
            
            LawDownloader lawDownloader = Singleton_LawDownloader.GetInstance();
            lawDownloader.ClearHomeEvent();
        }

        private void Clear_FileDownloaderEvent()
        {
            Dictionary<string, FileDownloader> dict_FileDownloader = Singleton_FileDownloader.GetInstance();
            //dict_FileDownloader.ToList().ForEach(x => x.Value.ClearAllEvent() );

            foreach (KeyValuePair<string, FileDownloader> item in dict_FileDownloader) 
            { 
                item.Value.ClearAllEvent(); 
            }
        }

        public void ShowSignPictureCT(string DeptID="",string PicUrl="")
        {

            SignPictureCT spc = new SignPictureCT((UserID, UserName) => { CC.Content = new SignPadCT(UserID, UserName, DeptID, PicUrl, ShowSignPictureCT); }
                                                 , ShowSignPictureCT);
            CC.Content = spc;
        }




        //自動登出模組(A)，請配合AutoLogOffHelper.cs
        private void InitializeAutoLogoffFeature()
        {
            try
            {
                HwndSource windowSpecificOSMessageListener = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
                windowSpecificOSMessageListener.AddHook(new HwndSourceHook(CallBackMethod));

                //debug自動登出時間為1分鐘
                //AutoLogOffHelper.LogOffTime = 1;
                AutoLogOffHelper.LogOffTime = PaperLess_Emeeting.Properties.Settings.Default.AutoLogoutMinutes;

                AutoLogOffHelper.MakeAutoLogOffEvent += new AutoLogOffHelper.MakeAutoLogOff(AutoLogOffHelper_MakeAutoLogOffEvent);
                AutoLogOffHelper.StartAutoLogoffOption();
                //string time = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt");
                //tblStatus.Text = "Timer is started at " + ": " + time;
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }

        }
        //自動登出模組(B)，請配合AutoLogOffHelper.cs
        private IntPtr CallBackMethod(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {

            try
            {
                //判斷是否在同步中，要重置時間用
                if (IsInSync==true)
                {
                     AutoLogOffHelper.ResetLogoffTimer();
                }
              
                //  Listening OS message to test whether it is a user activity
                if ((msg >= 0x0200 && msg <= 0x020A) || (msg <= 0x0106 && msg >= 0x00A0) || msg == 0x0021)
                {
                    AutoLogOffHelper.ResetLogoffTimer();
                    //string time = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt");
                    //tblStatus.Text = "Timer is reseted on user activity at " + ": " + time;
                }
                else
                {
                    // For debugging purpose
                    // If this auto logoff does not work for some user activity, you can detect the integer code of that activity  using the following line.
                    //Then All you need to do is adding this integer code to the above if condition.


                    //Debug用拿掉省資源
                    //System.Diagnostics.Debug.WriteLine(msg.ToString());
                }
                return IntPtr.Zero;
            }
            catch(Exception ex)
            {
                LogTool.Debug(ex);
            }

            return IntPtr.Zero;
        }

        void AutoLogOffHelper_MakeAutoLogOffEvent()
        {
            try
            {
                lock (this)
                {
                    AutoLogOffHelper.StopAutoLogoffOption();
                    Thread.Sleep(3 * 1000);

                    MessageBoxResult result = 0;

                    result = MessageBox.Show("您已閒置超過" + PaperLess_Emeeting.Properties.Settings.Default.AutoLogoutMinutes + "分鐘，是否登出", "系統訊息", MessageBoxButton.YesNo, MessageBoxImage.Information);
                     

                    if (result == MessageBoxResult.Yes)
                    {
                        LogOut();
                    }
                    else
                    {
                        AutoLogOffHelper.StartAutoLogoffOption();
                    }
                    return;
                }

            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }

        }

        private void LogOut()
        {
            try
            {
                this.Hide();
                StopAllBackground();
                CloseAllWindow("",true);
                Login f2 = new Login();
                f2.Show();
                //Login f2 = Application.Current.Windows.OfType<Login>().First();
                //if (f2.cbRemeberLogin.IsChecked == false)
                //{
                //     f2.tbUserID.Text = "";
                //}
                //f2.tbUserPWD.Password = "";
                //f2.Show();
                App.IsChangeWindow = true;
                this.Close();


                //登出
                //System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                //Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }

        }
      
    }
   
}
