using PaperLess_Emeeting.App_Code;
using PaperLess_Emeeting.App_Code.ClickOnce;
using PaperLess_Emeeting.App_Code.DownloadItem;
using PaperLess_Emeeting.App_Code.MessageBox;
using PaperLess_Emeeting.App_Code.ViewModel;
using PaperLess_Emeeting.App_Code.WS;
using PaperLess_ViewModel;
using ReadPageModule;
using System;
using System.Collections.Generic;
using System.Data;
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

    public delegate void LawListCT_HangTheDownloadEvent_Function(string LastLawItemID);
    public delegate bool LawListCT_IsAllLawRowFinished_AddInitUIFinished_Function();

    public delegate Dictionary<string, BookVM> LawListCT_GetBookVMs_ByMeetingFileCate_Function(Law_DownloadItemViewModel lawItem);
    /// <summary>
    /// LawRow.xaml 的互動邏輯
    /// </summary>
    public partial class LawRow : UserControl
    {
        public LawDataLaw lawDataLaw { get; set; }
        public Law_DownloadItemViewModel lawItem { get; set; }
        public string UserID { get; set; }
        public string UserName { get; set; }
        public string UserPWD { get; set; }
        public bool IsLastRow { get; set; }
        Storyboard sb;
        LawListCT_HangTheDownloadEvent_Function LawListCT_HangTheDownloadEvent_Event;
        LawListCT_IsAllLawRowFinished_AddInitUIFinished_Function LawListCT_IsAllLawRowFinished_AddInitUIFinished_Event;
        LawListCT_GetBookVMs_ByMeetingFileCate_Function LawListCT_GetBookVMs_ByMeetingFileCate_Event;
        public bool IsAllLawRowFinished = false;
        public LawRow(string UserID, string UserName, string UserPWD
                     , bool IsLastRow, LawDataLaw lawDataLaw
                     , LawListCT_HangTheDownloadEvent_Function callback1
                     , LawListCT_IsAllLawRowFinished_AddInitUIFinished_Function callback2
                     , LawListCT_GetBookVMs_ByMeetingFileCate_Function callback3)
        {
            //MouseTool.ShowLoading();
            InitializeComponent();
            this.UserID = UserID;
            this.UserName = UserName;
            this.UserPWD = UserPWD;
            this.IsLastRow = IsLastRow;
            this.lawDataLaw = lawDataLaw;
            this.Loaded += LawRow_Loaded;
            this.LawListCT_HangTheDownloadEvent_Event = callback1;
            this.LawListCT_IsAllLawRowFinished_AddInitUIFinished_Event = callback2;
            this.LawListCT_GetBookVMs_ByMeetingFileCate_Event = callback3;
            this.lawItem = new Law_DownloadItemViewModel();
            //MouseTool.ShowArrow();
        }

        private void LawRow_Loaded(object sender, RoutedEventArgs e)
        {
             txtDate.Text = lawDataLaw.UpDate.Split(' ')[0];
             txtLawName.Text = lawDataLaw.Name;

              Task.Factory.StartNew(() =>
              {
                  InitSelectDB();
                  // 只要是 Row 列表內容畫面，優先權設定為Background => 列舉值為 4。 所有其他非閒置作業都完成之後，就會處理作業。
                  //Dispatcher.BeginInvoke(DispatcherPriority.Background,new Action(() => 
                  this.Dispatcher.BeginInvoke(new Action(() =>
                  {
                      InitUI();
                      // 有下載UI相關的把事件放到主線成
                      InitEvent();
                  }));
                
              });
            
        }

     
        private void InitSelectDB()
        {
           
            DataTable dt = new DataTable();

            // 初始化User
            // 效能要好請改用從LawListCT傳入建構子
            //dt = MSCE.GetDataTable("select UserID,UserName,UserPWD from NowLogin");
            //if (dt.Rows.Count > 0)
            //{
            //    UserID = dt.Rows[0]["UserID"].ToString();
            //    UserName = dt.Rows[0]["UserName"].ToString();
            //    UserPWD = dt.Rows[0]["UserPWD"].ToString();
            //}

            //更新檔不支援續傳
            //檢查是否再下載當中
            LawDownloader lawDownloader = Singleton_LawDownloader.GetInstance();
            lawItem=lawDownloader.GetInList(lawDataLaw.ID);
            if (lawItem != null)
                return;

            #region DB
            string db_LawRowID = "";
            DateTime db_AtDownloadFinished_XmlUpDate = DateTime.Parse("2010/01/01");
            string db_Link = "";
            string db_StorageFileName = "";
            long db_DownloadBytes = 0;
            long db_TotalBytes = 0;

            dt = MSCE.GetDataTable("SELECT ID,AtDownloadFinished_XmlUpDate,Link,StorageFileName,DownloadBytes,TotalBytes FROM LawRow where ID=@1 and UserID=@2"
                                   , lawDataLaw.ID
                                   , UserID);
            if (dt.Rows.Count > 0)
            {
                db_LawRowID = dt.Rows[0]["ID"].ToString();
                db_AtDownloadFinished_XmlUpDate = (DateTime)dt.Rows[0]["AtDownloadFinished_XmlUpDate"];
                db_Link = dt.Rows[0]["Link"].ToString();
                db_StorageFileName = dt.Rows[0]["StorageFileName"].ToString();
                db_DownloadBytes = long.Parse(dt.Rows[0]["DownloadBytes"].ToString().Equals("") ? "0" : dt.Rows[0]["DownloadBytes"].ToString());
                db_TotalBytes = long.Parse(dt.Rows[0]["TotalBytes"].ToString().Equals("") ? "0" : dt.Rows[0]["TotalBytes"].ToString());
            }
            else
            {
                string SQL=@"INSERT INTO LawRow(ID,AtDownloadFinished_XmlUpDate,DownloadBytes,TotalBytes,UserID) 
                                                    VALUES(@1,'2010/01/01',0,0,@2)";
                int success = MSCE.ExecuteNonQuery(SQL
                                                    , lawDataLaw.ID
                                                    , UserID);
                if (success < 1)
                    LogTool.Debug(new Exception(@"DB失敗: " + SQL));
            }
            #endregion
            
            lawItem = new Law_DownloadItemViewModel();
            lawItem.ID = lawDataLaw.ID;
            lawItem.UserID = UserID;
            DateTime lawDataLaw_UpDate ;
            DateTime.TryParse(lawDataLaw.UpDate, out lawDataLaw_UpDate);
            lawItem.UpDate = lawDataLaw_UpDate;
            lawItem.Name = lawDataLaw.Name;
            lawItem.Link = lawDataLaw.Link;
            lawItem.Status = (LawDataStatus)Enum.Parse(typeof(LawDataStatus), lawDataLaw.Status);
            
            string AppPath = System.AppDomain.CurrentDomain.BaseDirectory;
            string Law_StorageFileFolder = PaperLess_Emeeting.Properties.Settings.Default.Law_StorageFileFolder2;
            lawItem.StorageFileFolder = System.IO.Path.Combine(AppPath,Law_StorageFileFolder);

            #region 取得 Http URL 的檔名
            string fileName = DateTime.Now.ToFileTime().ToString();
            try
            {
                Uri uri = new Uri(lawItem.Link);
                string tempFileName = System.IO.Path.GetFileName(uri.LocalPath);
                if (tempFileName.Equals(@"/") == false)
                    fileName = tempFileName;
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }
            #endregion

            lawItem.StorageFileName = string.Format("{0}_{1}_{2}",UserID, lawDataLaw.ID,fileName);
            lawItem.UnZipFileFolder = System.IO.Path.Combine(ClickOnceTool.GetFilePath() , PaperLess_Emeeting.Properties.Settings.Default.Law_UnZipFileFolder);

            // 續傳才要用到
            //lawItem.DownloadBytes = db_DownloadBytes;
            //lawItem.TotalBytes = db_DownloadBytes;

            //bool Law_ResumeDownload = false;
            ////不續傳
            //if (Law_ResumeDownload == false)
            //{
                //未下載完成的
                if (db_DownloadBytes == 0 || db_DownloadBytes < db_TotalBytes)
                {
                    //刪除未下載完成的
                    if (File.Exists(lawItem.StorageFilePath) == true)
                        File.Delete(lawItem.StorageFilePath);

                    lawItem.DownloadBytes = 0;
                    lawItem.TotalBytes = 0;
                    lawItem.FileType = LawFileType.從未下載;   
                   
                }
                else //有下載完成的
                {
                    //先判斷是否有更新檔
                    //需要更新檔案,UpdDate 日期比較新
                    if (TimeSpan.Compare(new TimeSpan(lawDataLaw_UpDate.Ticks), new TimeSpan(db_AtDownloadFinished_XmlUpDate.Ticks)) > 0)
                    {
                        lawItem.StorageFileName = lawItem.StorageFileName + ".update";
                        lawItem.DownloadBytes = 0;
                        lawItem.TotalBytes = 0;
                        lawItem.FileType = LawFileType.更新檔未下載;
                    }
                    else //不需更新檔案
                    {
                        lawItem.FileType = LawFileType.已下載完成;
                        //結束;
                    }
                }

            //}
            //else //續傳
            //{
            //    //目前不支援
            //}

            
        }

        private void InitEvent()
        {
            btnDownload.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnDownload.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnDownload.MouseLeftButtonDown += btnDownload_MouseLeftButtonDown;

            btnDelete.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnDelete.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnDelete.MouseLeftButtonDown += btnDelete_MouseLeftButtonDown;

            btnOpen.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnOpen.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnOpen.MouseLeftButtonDown += btnOpen_MouseLeftButtonDown;

            btnUpdate.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnUpdate.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnUpdate.MouseLeftButtonDown += btnDownload_MouseLeftButtonDown;

            // 在這裡呼叫並且掛上MeetingDataCT的下載事件
            //if (IsLastRow == true && LawListCT_HangTheDownloadEvent_Event != null)
            if (IsAllLawRowFinished == true && LawListCT_HangTheDownloadEvent_Event != null)
                LawListCT_HangTheDownloadEvent_Event(this.lawItem.ID);
        }

        private void btnOpen_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MouseTool.ShowLoading();

            try
            {
                //string AppPath = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = ClickOnceTool.GetFilePath();

                //string _bookPath = System.IO.Path.Combine(AppPath, lawItem.UnZipFilePath);
                string _bookPath = lawItem.UnZipFilePath; 
                string _bookId = "";
                string _account = "";
                string _userName = "";
                string _email = "";
                string _meetingId = "";
                string _watermark = "";
                string _dbPath = System.IO.Path.Combine(ClickOnceTool.GetDataPath(), PaperLess_Emeeting.Properties.Settings.Default.bookInfo_Path);
                bool _isSync = false;
                bool _isSyncOwner = false;
                string _webServiceUrl = WsTool.GetUrl() +"/AnnotationUpload";
                string _socketMessage = "";


                // 呼叫一個事件取得 BookVMs
                //Dictionary<string, BookVM> cbBooksData = new Dictionary<string, BookVM>();
                //if (LawListCT_GetBookVMs_ByMeetingFileCate_Event != null)
                //    cbBooksData = LawListCT_GetBookVMs_ByMeetingFileCate_Event(lawItem);

                Dictionary<string, BookVM> cbBooksData = null;
                Home Home_Window = Application.Current.Windows.OfType<Home>().FirstOrDefault();
           
                Home_Window.CloseAllWindow("", true);
                Task.Factory.StartNew(() =>
                {
                    this.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        ReadWindow rw = new ReadWindow(cbBooksData, Home_Window.OpenBookFromReader, _bookPath, _bookId, _account
                                              , _userName, _email, _meetingId
                                              , _watermark, _dbPath, _isSync
                                              , _isSyncOwner, _webServiceUrl, _socketMessage, null);
                        rw.HideCollectFile = true;
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
            }
            catch(Exception ex)
            {
                LogTool.Debug(ex);
            }

            MouseTool.ShowArrow();
        }

        private void btnDelete_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            MessageBoxResult result = MessageBox.Show("您確定要刪除檔案?", "系統訊息", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No)
                return;

            string SQL = @"update  LawRow set DownloadBytes=0,TotalBytes=0 where ID=@1 and UserID=@2";
            int success = MSCE.ExecuteNonQuery(SQL, lawDataLaw.ID, UserID);


            if (success < 1)
            {
                AutoClosingMessageBox.Show("刪除失敗");

                LogTool.Debug(new Exception(@"DB失敗: " + SQL));
                return;
            }

            if (File.Exists(lawItem.StorageFilePath) == true)
                File.Delete(lawItem.StorageFilePath);

            ShowNeverDownload(true);
        }

        private void btnDownload_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (lawItem == null)
                return;
            // 記得必須隱藏得先做，才不會被看見
            btnDownload.Visibility = Visibility.Collapsed;

            txtPercent.Text = "等待中";
            txtPercent.Foreground = Brushes.Gray;
            txtPercent.Visibility = Visibility.Visible;
            pb.Foreground = Brushes.Wheat;
            pb.Background = Brushes.Gray;
            pb.Visibility = Visibility.Visible;
          

            if (lawItem.StorageFileName.EndsWith(".update"))
                lawItem.FileType = LawFileType.更新檔排入下載中;
            else
                lawItem.FileType = LawFileType.排入下載中;
            LawDownloader lawDownloader = Singleton_LawDownloader.GetInstance();
            lawDownloader.AddItem(lawItem);

          
        }

        private void InitUI()
        {
            //txtDate.Text = lawDataLaw.UpDate.Split(' ')[0];
            //txtLawName.Text = lawDataLaw.Name;
            switch (lawItem.FileType)
            {
                case LawFileType.從未下載:
                    ShowNeverDownload(false);
                    break;
                case LawFileType.暫停中:
                    break;
                case LawFileType.正在下載中:
                    ShowInDownload();
                    txtPercent.Text = ((int)lawItem.NowPercentage).ToString() + " %";
                    pb.Value = lawItem.NowPercentage;
                    pb.Foreground = Brushes.Orange;
                    pb.Background = Brushes.Black;
                    txtPercent.Visibility = Visibility.Visible;
                    pb.Visibility = Visibility.Visible;
                    break;
                case LawFileType.已下載完成:
                    ShowCanOpen();
                    // 記得必須隱藏必須先做，才不會被看見
                    // 先拿掉，因為本來就是不可見的
                    //btnUpdate.Visibility = Visibility.Collapsed;
                    txtIsNew.Visibility = Visibility.Visible;
                    break;
                case LawFileType.排入下載中:
                    ShowInDownload();
                    txtPercent.Text = "等待中";
                    txtPercent.Foreground = Brushes.Gray;
                    txtPercent.Visibility = Visibility.Visible;
                    pb.Foreground = Brushes.Wheat;
                    pb.Background = Brushes.Gray;
                    pb.Visibility = Visibility.Visible;
                  
                    break;
                case LawFileType.解壓縮中:
                    Storyboard sb = (Storyboard)this.TryFindResource("sb");
                    if (sb != null)
                        sb.Begin();
                    break;
                case LawFileType.解壓縮失敗:
                    // 在Home的主視窗Show，不要在這裡Show
                    //AutoClosingMessageBox.Show("解壓縮失敗");
                    ShowNeverDownload(false);
                    break;
                case LawFileType.更新檔未下載:
                    ShowCanOpen();
                    break;
                case LawFileType.更新檔暫停中:
                    ShowCanOpen();
                    break;
                case LawFileType.更新檔正在下載中:
                    ShowCanOpen();
                    break;
                case LawFileType.更新檔已下載完成:
                    ShowCanOpen();
                    break;
                case LawFileType.更新檔排入下載中:
                    ShowCanOpen();
                    txtUpdatePercent.Text = "等待中";
                    txtIsNew.Foreground = Brushes.Gray;
                    txtUpdatePercent.Visibility = Visibility.Visible;
                    pb.Foreground = Brushes.Wheat;
                    pb.Background = Brushes.Gray;
                    pb.Visibility = Visibility.Visible;
                    break;
                case LawFileType.更新檔解壓縮中:
                    sb = (Storyboard)this.TryFindResource("sbUpdate");
                    if (sb != null)
                        sb.Begin();
                    break;
                case LawFileType.更新檔解壓縮失敗:
                    ShowCanOpen();
                    break;
               
                
            }


            // 判斷是否是最後一個FileRow載入完成的
            // 要注意，並不一定是最後一個才是最後載入完成的
            if (LawListCT_IsAllLawRowFinished_AddInitUIFinished_Event != null)
                IsAllLawRowFinished = LawListCT_IsAllLawRowFinished_AddInitUIFinished_Event();
            
        }

        private void HideAll()
        {
            btnDelete.Visibility = Visibility.Collapsed;
            btnOpen.Visibility = Visibility.Collapsed;
            btnUpdate.Visibility = Visibility.Collapsed;
            pb.Visibility = Visibility.Collapsed;
            txtIsNew.Visibility = Visibility.Collapsed;
            btnDownload.Visibility = Visibility.Visible;
            pbUpdate.Visibility = Visibility.Collapsed;
            txtUpdatePercent.Visibility = Visibility.Collapsed;
        }

        private void ShowNeverDownload(bool fromBtnDelete)
        {
            if (fromBtnDelete)
            {
                btnDelete.Visibility = Visibility.Collapsed;
                btnOpen.Visibility = Visibility.Collapsed;
                btnUpdate.Visibility = Visibility.Collapsed;
                pb.Visibility = Visibility.Collapsed;
                txtIsNew.Visibility = Visibility.Collapsed;
                pbUpdate.Visibility = Visibility.Collapsed;
                txtUpdatePercent.Visibility = Visibility.Collapsed;
            }
            
            btnDownload.Visibility = Visibility.Visible;

        }

        private void ShowInDownload()
        {
            // 記得必須隱藏必須先做，才不會被看見
            // 先拿掉，因為本來就是不可見的
            //btnDownload.Visibility = Visibility.Collapsed;
            //btnDelete.Visibility = Visibility.Collapsed;
            //btnOpen.Visibility = Visibility.Collapsed;
            //btnUpdate.Visibility = Visibility.Collapsed;
        }

        private void ShowCanOpen()
        {
            // 記得必須隱藏得先做，才不會被看見
            // 先拿掉，因為本來就是不可見的
            //btnDownload.Visibility = Visibility.Collapsed;
            //txtPercent.Visibility = Visibility.Collapsed;
            //pb.Visibility = Visibility.Collapsed;

            btnDelete.Visibility = Visibility.Visible;
            btnOpen.Visibility = Visibility.Visible;
           
        }

    
    }
}
