using Newtonsoft.Json;
using PaperLess_Emeeting.App_Code;
using PaperLess_Emeeting.App_Code.ClickOnce;
using PaperLess_Emeeting.App_Code.DownloadItem;
using PaperLess_Emeeting.App_Code.MessageBox;
using PaperLess_Emeeting.App_Code.ViewModel;
using PaperLess_Emeeting.App_Code.WS;
using PaperLess_ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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
using System.Xml;

namespace PaperLess_Emeeting
{
   
    /// <summary>
    /// MeetingRoom.xaml 的互動邏輯
    /// </summary>
    public partial class FileRoom : UserControl
    {
        public event Home_Change2MeetingDataCT_Function Home_Change2MeetingDataCT_Event;
        public UserMeeting userMeeting { get; set; }

        MeetingRoomButtonType meetingRoomButtonType;
        public string NewAddMeetingID = "";
        public bool invisible = false;


        MeetingRoom_DownloadFileStart_Function Start_callback;
        MeetingRoom_DownloadProgressChanged_Function Progress_callback;
        MeetingRoom_DownloadFileToErrorCompleted_Function ErrorFinish_callback;

        int TotalFiles = 0;
        int FinishedFiles = 0;
        int FinishedPercent = 0;
        public string UserID { get; set; }
        public string UserPWD { get; set; }

        public DispatcherTimer ajaxTimer = null;
        public bool ForceStopAjaxLoader = false;

        public bool HasReceiveStart_callback = false;

        //預載會議資料
        public MeetingData PreLoadMeetingData = null;
        public int CacheMinuteTTL = 0;
        public Thread CacheThread = null;

        BookVM bookVM;
        public Action<bool> DelAction;
        public XML2.FolderDataFolderFileListFile VM { get; set; }
        string FolderID;
        string Bookpth = "";
        public bool hasDownloaded = false;
        public bool HasImage = false;

        public FileRoom(string UserID, string UserPWD, XML2.FolderDataFolderFileListFile VM, Action<bool> DelAction,string FolderID)
        {

            //MouseTool.ShowLoading();
            InitializeComponent();
            this.UserID = UserID;
            this.UserPWD = UserPWD;
            this.userMeeting = userMeeting;
            //this.Home_Change2MeetingDataCT_Event += callback;
            //this.NewAddMeetingID = NewAddMeetingID;
            //this.invisible = invisible;
            this.DelAction = DelAction;
            this.FolderID = FolderID;
            //先判斷是從系列會議到底是否隱藏(1)
            this.VM = VM;
            this.Loaded +=MeetingRoom_Loaded;
            this.Unloaded +=MeetingRoom_Unloaded;
            this.CacheMinuteTTL = PaperLess_Emeeting.Properties.Settings.Default.CacheMinuteTTL;
            //MouseTool.ShowArrow();
        }

      



        private void MeetingRoom_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
             {
                 InitSelectDB();
                 // 這裡為 日期列表畫面 下的 會議房間畫面，優先權設定為Background => 列舉值為 4。 所有其他非閒置作業都完成之後，就會處理作業。
                 //this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                 this.Dispatcher.BeginInvoke(new Action(() =>
                 {
                     InitUI();
                     // 有下載UI相關的把事件放到主線成
                     InitEvent();
                 }));

                
                 // 正在開會議的會議
                 //if (DateTime.Parse(userMeeting.BeginTime) <= DateTime.Now && DateTime.Now < DateTime.Parse(userMeeting.EndTime))
                 //if (DateTool.IsSameDate(DateTime.Parse(userMeeting.BeginTime),DateTime.Now))
                 //{
                 //    //預載會議資料
                 //    PreLoadMeeting();
                 //}
             });
        }

        //預載會議資料
        private void PreLoadMeeting()
        {

            
            //小於0為沒有Cache
            //等於0為Cache不會過期
            //大於0為多少分鐘後清掉Cache
            if (this.CacheMinuteTTL >= 0)
            {
                Task.Factory.StartNew(() =>
                {
                       GetMeetingData.AsyncPOST(userMeeting.ID, UserID, UserPWD, (md) => 
                        { 
                            try
                            {
                                PreLoadMeetingData = md; 
                                //[過時]因為CacheMinuteTTL是以分鐘為計算，如果是以秒鐘為計算的話
                                //[過時]最好放在上面的AsyncPOST裡面
                                //已改為放在AsyncPOST裡
                                if (this.CacheMinuteTTL > 0)
                                {
                                    if (CacheThread != null)
                                        CacheThread.Abort();
                                    CacheThread = new Thread(delegate()
                                    {
                                        Thread.Sleep(this.CacheMinuteTTL * 60 * 1000);
                                        PreLoadMeetingData = null;
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

        private void MeetingRoom_Unloaded(object sender, RoutedEventArgs e)
        {
            //Singleton_FileDownloader.GetInstance(userMeeting.ID).ClearMeetingRoomEvent();
        }

        private void InitSelectDB()
        {

            //DataTable dt = MSCE.GetDataTable(@"select count(ID) as TotalFiles from FileRow where UserID =@1 and MeetingID=@2;"
            //                                  ,UserID,userMeeting.ID);
            //if (dt.Rows.Count > 0)
            //    int.TryParse(dt.Rows[0]["TotalFiles"].ToString(), out TotalFiles);
   

            //FinishedPercent=GetFinishedPercent();

            //if (userMeeting.isDownload != null && userMeeting.isBrowserd != null)
            //    Enum.TryParse(userMeeting.isDownload + userMeeting.isBrowserd, out meetingRoomButtonType);
            //else
            //    meetingRoomButtonType = MeetingRoomButtonType.YY;

            //DataTable dt = MSCE.GetDataTable("select NewAddMeetingID from NowLogin");
            //if (dt.Rows.Count > 0)
            //{
            //    UserID = dt.Rows[0]["UserID"].ToString().Trim();
            //    UserName = dt.Rows[0]["UserName"].ToString().Trim();
            //    UserPWD = dt.Rows[0]["UserPWD"].ToString().Trim();
            //}
        }

        private void InitEvent()
        {

            FileDownloader fileDownloader = Singleton_FileDownloader.GetInstance(VM.Meeting.ID);

            var downloadVM = fileDownloader.GetInList(VM.ID);

            if(downloadVM!=null)
            {
                Storyboard sb = (Storyboard)this.TryFindResource("sb");
                if (sb != null)
                {
                    txtDownloading.Visibility = Visibility.Visible;
                    imgCover.Visibility = Visibility.Collapsed;
                    imgCover2.Visibility = Visibility.Collapsed;
                    sb.Begin();
                }

                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += (sender, e) => {
                    var downloadVM2 = fileDownloader.GetInList(VM.ID);

                    if (downloadVM2 == null)
                    {
                        imgCover.Source = null;
                        sb.Stop();

                        Storyboard sb2 = (Storyboard)this.TryFindResource("sb2");
                        sb2.Begin();

                        hasDownloaded = false;
                        imgCover.Visibility = Visibility.Collapsed;
                        txtDownloading.Visibility = Visibility.Collapsed;
                        imgCover.Visibility = Visibility.Collapsed;
                        imgCover2.Visibility = Visibility.Collapsed;
                        txtUnZip.Visibility = Visibility.Visible;

                        DispatcherTimer timer2 = new DispatcherTimer();
                        timer2.Interval = TimeSpan.FromSeconds(1);
                        timer2.Tick += (sender3, e3) =>
                        {
                            string filePath = ClickOnceTool.GetFilePath();
                            string UnZipFileFolder = PaperLess_Emeeting.Properties.Settings.Default.File_UnZipFileFolder;
                            string baseBookPath = filePath + "\\" + UnZipFileFolder + "\\" + UserID + "\\" + VM.Meeting.ID;

                            DataTable dt = MSCE.GetDataTable("SELECT FinishedFileVersion FROM FileRow where ID=@1 and UserID=@2 and MeetingID=@3"
                                            , VM.ID
                                            , UserID
                                            , VM.Meeting.ID);

                            string _bookPath = baseBookPath + "\\" + VM.ID;

                            string FinishedFileVersion = "1";
                            MeetingFileCate cate;

                            if (VM.ID.EndsWith("P"))
                                cate = MeetingFileCate.電子書;
                            else
                                cate = MeetingFileCate.Html5投影片;

                            if (dt.Rows.Count > 0)
                            {
                                FinishedFileVersion = dt.Rows[0]["FinishedFileVersion"].ToString();
                                Bookpth = baseBookPath + "\\" + VM.ID + "\\" + FinishedFileVersion;
                                bookVM = new BookVM(VM.ID, baseBookPath + "\\" + VM.ID + "\\" + FinishedFileVersion, cate);
                            }
                            else
                            {
                                bookVM = new BookVM(VM.ID, "", cate);
                            }




                            if (bookVM.BookPath.Equals("") == false)
                            {

                                if (bookVM.FileCate == MeetingFileCate.Html5投影片)
                                {

                                    if (File.Exists(bookVM.BookPath + "\\data\\Thumbnails\\Slide1.png"))
                                    {
                                        imgCover.Source = new BitmapImage(new Uri(bookVM.BookPath + "\\data\\Thumbnails\\Slide1.png"));
                                        hasDownloaded = true;
                                        txtDownloading.Visibility = Visibility.Collapsed;
                                        imgCover.Visibility = Visibility.Visible;
                                        imgCover2.Visibility = Visibility.Collapsed;
                                        txtUnZip.Visibility = Visibility.Collapsed;
                                        HasImage = true;
                                        timer.Stop();
                                        timer2.Stop();



                                    }
                                }
                                else if (bookVM.FileCate == MeetingFileCate.電子書)
                                {
                                    if (File.Exists(bookVM.BookPath + $"\\HYWEB\\thumbs\\{bookVM.BookPath.Split('\\').Last()}-463a-P_1.jpg"))
                                    {
                                        imgCover.Source = new BitmapImage(new Uri(bookVM.BookPath + $"\\HYWEB\\thumbs\\{bookVM.BookPath.Split('\\').Last()}-463a-P_1.jpg"));
                                        hasDownloaded = true;
                                        txtDownloading.Visibility = Visibility.Collapsed;
                                        imgCover.Visibility = Visibility.Visible;
                                        imgCover2.Visibility = Visibility.Collapsed;
                                        txtUnZip.Visibility = Visibility.Collapsed;
                                        sb2.Stop();
                                        HasImage = true;
                                        timer.Stop();
                                        timer2.Stop();
                                    }

                                }
                                else
                                {
                                    if (Directory.Exists(bookVM.BookPath))
                                    {
                                        var i = Directory.GetFiles(bookVM.BookPath).Count();
                                        if (i > 0)
                                        {

                                            imgCover.Source = new BitmapImage(new Uri("images/icon_video@2x.png", UriKind.Relative));
                                            hasDownloaded = true;
                                            txtDownloading.Visibility = Visibility.Collapsed;
                                            imgCover.Visibility = Visibility.Visible;
                                            imgCover2.Visibility = Visibility.Collapsed;
                                            txtUnZip.Visibility = Visibility.Collapsed;
                                            sb2.Stop();
                                            HasImage = true;
                                            timer.Stop();
                                            timer2.Stop();
                                        }
                                    }
                                }
                            }

                            if (HasImage)
                            {
                                hasDownloaded = true;
                                txtDownloading.Visibility = Visibility.Collapsed;
                                imgCover.Visibility = Visibility.Visible;
                                imgCover2.Visibility = Visibility.Collapsed;
                                txtUnZip.Visibility = Visibility.Collapsed;
                            }
                            else
                            {
                                hasDownloaded = false;
                                imgCover.Visibility = Visibility.Collapsed;
                                txtDownloading.Visibility = Visibility.Collapsed;
                                imgCover.Visibility = Visibility.Collapsed;
                                imgCover2.Visibility = Visibility.Collapsed;
                                txtUnZip.Visibility = Visibility.Visible;

                            }
                        };

                        timer2.Start();

                    }
                };
                timer.Start();
            }

            // 下載事件不能這麼快掛上去，要等全部FileRow產生完成，在掛上去
            // 不然如果在下載中，會阻塞到UI的產生
            //FileDownloader fileDownloader = Singleton_FileDownloader.GetInstance(userMeeting.ID);
            //Start_callback = new MeetingRoom_DownloadFileStart_Function(MeetingRoom_DownloadFileStart_Callback);
            //Progress_callback = new MeetingRoom_DownloadProgressChanged_Function(MeetingRoom_DownloadProgressChanged_Callback);
            //ErrorFinish_callback = new MeetingRoom_DownloadFileToErrorCompleted_Function(MeetingRoom_DownloadFileToErrorCompleted_Callback);

            //fileDownloader.MeetingRoom_DownloadFileStart_Event += Start_callback;
            //fileDownloader.MeetingRoom_DownloadProgressChanged_Event += Progress_callback;
            //fileDownloader.MeetingRoom_DownloadFileToErrorCompleted_Event += ErrorFinish_callback;


            //this.MouseLeftButtonDown += MeetingRoom_MouseLeftButtonDown;

            //btnSeries.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            //btnSeries.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            //btnSeries.MouseLeftButtonDown += btnSeries_MouseLeftButtonDown;

            //btnDelete.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            //btnDelete.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            //btnDelete.MouseLeftButtonDown += btnDelete_MouseLeftButtonDown;

            //btnRefresh.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            //btnRefresh.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            //btnRefresh.MouseLeftButtonDown += btnDownload_btnPausing_MouseLeftButtonDown;

            //btnDownload.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            //btnDownload.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            //btnDownload.MouseLeftButtonDown += btnDownload_btnPausing_MouseLeftButtonDown;

            // 要變成暫停下載
            //btnPause.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            //btnPause.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            //btnPause.MouseLeftButtonDown += btnPause_MouseLeftButtonDown;

            // 要變成繼續下載
            //btnPausing.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            //btnPausing.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            //btnPausing.MouseLeftButtonDown += btnDownload_btnPausing_MouseLeftButtonDown;
        }


        private void btnDownload_btnPausing_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            // 先撈一次MeetingData
            MouseTool.ShowLoading();
            //GetMeetingData.AsyncPOST(userMeeting.ID, UserID, UserPWD, (md) => { GetMeetingData_DoAction(md,(Image)sender); });
            if (NetworkTool.CheckNetwork() > 0)
            {
                GetMeetingData.AsyncPOST(userMeeting.ID, UserID, UserPWD, (md) => { GetMeetingData_DoAction(md, (Image)sender); });
            }
            else
            {
                //DB查詢登入
                DataTable dt = MSCE.GetDataTable("select MeetingJson from MeetingData where MeetingID=@1 and UserID =@2"
                                                , userMeeting.ID
                                                , UserID);

                if (dt.Rows.Count > 0)
                {
                    MeetingData md = JsonConvert.DeserializeObject<MeetingData>(dt.Rows[0]["MeetingJson"].ToString());
                    GetMeetingData_DoAction(md,(Image)sender);
                }
                else
                {
                    AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
                    MouseTool.ShowArrow();
                }

              
            }
        }

        private void GetMeetingData_DoAction(MeetingData md,Image btnImage)
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                this.Dispatcher.BeginInvoke(new Action<MeetingData, Image>(GetMeetingData_DoAction), md, btnImage);
            }
            else
            {
                if (md != null)
                {
                    Task.Factory.StartNew(() =>
                    {
                        FileDownloader fileDownloader = Singleton_FileDownloader.GetInstance(md.ID);
                        fileDownloader.Stop();

                        List<MeetingDataDownloadFileFile> FileList = new List<MeetingDataDownloadFileFile>();
                        try
                        {
                            // <File ID="cAS66-P" Url="http://com-meeting.ntpc.hyweb.com.tw/Public/MeetingAttachFile/2/2-b167-P.phej" FileName="ae717047" version="1"/>

                            // 如果meetingData.MeetingsFile.FileList沒有子節點，就會轉型失敗
                            //XmlNode[] FileListXml = (XmlNode[])md.MeetingsFile.FileList;
                            //foreach (XmlNode item in FileListXml)
                            foreach (MeetingDataMeetingsFileFile item in md.MeetingsFile.FileList)
                            {
                                MeetingDataDownloadFileFile recordFile = new MeetingDataDownloadFileFile();
                                recordFile.AgendaID = "record";
                                //recordFile.FileName = item.Attributes["FileName"].Value;
                                //recordFile.ID = item.Attributes["ID"].Value;
                                //recordFile.Url = item.Attributes["Url"].Value;
                                //recordFile.version = byte.Parse(item.Attributes["version"].Value);
                                recordFile.FileName = item.FileName;
                                recordFile.ID = item.ID;
                                recordFile.Url = item.Url;
                                recordFile.version = item.version;
                                FileList.Add(recordFile);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogTool.Debug(ex);
                        }

                        FileList.AddRange(md.DownloadFile.DownloadFileList.ToList());
                        TotalFiles = FileList.Count;
                        List<File_DownloadItemViewModel> fileItemList = new List<File_DownloadItemViewModel>();
                        foreach (MeetingDataDownloadFileFile meetingDataDownloadFileFile in FileList)
                        {
                            File_DownloadItemViewModel fileItem = FileItemTool.Gen(meetingDataDownloadFileFile, UserID, md.ID);
                            if(fileItem.ID.Equals(VM.ID))
                            if (fileItem.DownloadBytes == 0 || fileItem.DownloadBytes < fileItem.TotalBytes)
                                fileItemList.Add(fileItem);
                        }

                        
                        if (fileDownloader.HasMeetingRoom_DownloadFileStart_Event() == false)
                        {
                            fileDownloader.MeetingRoom_DownloadFileStart_Event += Start_callback;
                        }

                        if (fileDownloader.HasMeetingRoom_DownloadProgressChanged_Event() == false)
                        {
                            fileDownloader.MeetingRoom_DownloadProgressChanged_Event += Progress_callback;
                        }

                        if (fileDownloader.HasMeetingRoom_DownloadFileToErrorCompleted_Event() == false)
                        {
                            fileDownloader.MeetingRoom_DownloadFileToErrorCompleted_Event += ErrorFinish_callback;
                        }

                      

                        fileDownloader.AddItem(fileItemList);
                    });

                    //btnImage.Visibility = Visibility.Collapsed;
                    //if (btnImage.Name.Equals(btnDownload.Name) == true)
                    //{
                    //    txtPercent.Text = "0 %";
                    //    pb.Value = pb.Minimum;
                    //}
                    //txtPercent.Foreground = Brushes.Black;
                    //pb.Foreground = Brushes.Orange;
                    //pb.Background = Brushes.Black;
                      
                    //txtPercent.Visibility = Visibility.Visible;
                    //pb.Visibility = Visibility.Visible;
                    //btnPause.Visibility = Visibility.Visible;
                }
                else
                {
                    AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
                }
                MouseTool.ShowArrow();
            }
        }
        private void btnDelete_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            string QuestSring = "您確定要刪除檔案?";
            // userMeeting.isDownload + userMeeting.isBrowserd
            switch (meetingRoomButtonType)
            {
                case MeetingRoomButtonType.NN:
                    break;
                case MeetingRoomButtonType.YY:
                    break;
                case MeetingRoomButtonType.OY:
                    QuestSring = string.Format("{0}附件資料下載時間已過期，是否確認刪除？", PaperLess_Emeeting.Properties.Settings.Default.CourseOrMeeting_String);
                    break;
                case MeetingRoomButtonType.YO:
                    QuestSring = string.Format("{0}附件資料下載時間已過期，是否確認刪除？", PaperLess_Emeeting.Properties.Settings.Default.CourseOrMeeting_String);
                    break;
            }

            MessageBoxResult result = MessageBox.Show(QuestSring, "系統訊息", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes)
                return;

            MouseTool.ShowLoading();
            bool HasDelete = false;
            try
            {
                //找出資料庫所有的檔案並且重置和刪除。
                // 多少檔案完成

                // 抓取檔案路徑
                DataTable dt = MSCE.GetDataTable(@"select ID,StorageFileName from FileRow where UserID =@1 and MeetingID=@2 and DownloadBytes!=0 and DownloadBytes >= TotalBytes;"
                                , UserID, userMeeting.ID);

                // 多少檔案要被刪除
                foreach (DataRow dr in dt.Rows)
                {
                    string filePath = ClickOnceTool.GetFilePath();
                    string StorageFileFolder = PaperLess_Emeeting.Properties.Settings.Default.File_StorageFileFolder;
                    string UnZipFileFolder = PaperLess_Emeeting.Properties.Settings.Default.File_UnZipFileFolder;
                    string StorageFilePath = filePath + "\\" + StorageFileFolder + "\\" + dr["StorageFileName"].ToString();
                    string UnZipFilePath = filePath + "\\" + UnZipFileFolder + "\\" + UserID + "\\" + userMeeting.ID + "\\" + dr["ID"].ToString();

                    if (File.Exists(StorageFilePath) == true)
                        File.Delete(StorageFilePath);
                    try
                    {
                        DirectoryTool.FullDeleteDirectories(UnZipFilePath);

                    }
                    catch { }
                }

                string SQL = @"update FileRow set DownloadBytes=0,TotalBytes=0,FinishedFileVersion=0 where UserID =@1 and MeetingID=@2 ";
                int success = MSCE.ExecuteNonQuery(SQL
                                                    , UserID, userMeeting.ID);

                if (success < 1)
                {
                    LogTool.Debug(new Exception(@"DB失敗: " + SQL));
                }
                else
                {
                    HasDelete = true;
                }
            }
            catch(Exception ex)
            {
                LogTool.Debug(ex);
            }
            MouseTool.ShowArrow();
            if (HasDelete == false)
            {
                AutoClosingMessageBox.Show("刪除失敗");
            }
            else
            {
                // userMeeting.isDownload + userMeeting.isBrowserd
                //switch (meetingRoomButtonType)
                //{
                //    case MeetingRoomButtonType.NN:
                //            btnDelete.Visibility = Visibility.Collapsed;
                //            pb.Value = pb.Minimum;
                //        break;
                //    case MeetingRoomButtonType.YY:
                //            // 一般都會顯示這個，除非下載瀏覽過期之類的
                //            btnDelete.Visibility = Visibility.Collapsed;
                //            btnDownload.Visibility = Visibility.Visible;
                //            pb.Value = pb.Minimum;
                //        break;
                //    case MeetingRoomButtonType.OY:
                //            btnDelete.Visibility = Visibility.Collapsed;
                //            btnDownloadForbidden.Visibility = Visibility.Visible;
                //            pb.Value = pb.Minimum;
                //        break;
                //    case MeetingRoomButtonType.YO:
                //            btnDelete.Visibility = Visibility.Collapsed;
                //            btnRead2Forbidden.Visibility = Visibility.Visible;
                //            pb.Value = pb.Minimum;
                //        break;
                //}
                //btnDelete.Visibility = Visibility.Collapsed;
                //btnRefresh.Visibility = Visibility.Visible;
                //pb.Value = pb.Minimum;
                //txtPercent.Text = "0 %";
            }
           
        }

        private void btnPause_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

           Task.Factory.StartNew(() =>
           {
               FileDownloader fileDownloader = Singleton_FileDownloader.GetInstance(userMeeting.ID);
               fileDownloader.ClearMeetingRoomEvent();
               fileDownloader.Stop();
           });


           //this.btnPause.Visibility = Visibility.Collapsed;
           //this.txtPercent.Foreground = Brushes.Gray;
           //this.pb.Foreground = Brushes.Wheat;
           //this.pb.Background = Brushes.Gray;
           //this.txtPercent.Visibility = Visibility.Visible;
           //this.btnPausing.Visibility = Visibility.Visible;
          
        }

        private void MeetingRoom_DownloadFileToErrorCompleted_Callback(File_DownloadItemViewModel fileItem)
        {
          //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action<File_DownloadItemViewModel>(MeetingRoom_DownloadFileToErrorCompleted_Callback), fileItem);
            }
            else
            {
                ForceStopAjaxLoader = true;
                double totalPercent = GetNowMeetingFilesTotalPercent(fileItem);

                // 全部都已經下載完成或失敗，要變成暫停狀態，一定不會是100%
                //this.btnDownloadForbidden.Visibility = Visibility.Collapsed;
                //this.btnRead2Forbidden.Visibility = Visibility.Collapsed;
                //this.btnDelete.Visibility = Visibility.Collapsed;
                //this.btnDownload.Visibility = Visibility.Collapsed;
                //this.btnPause.Visibility = Visibility.Collapsed;
                //this.txtPercent.Text = ((int)totalPercent).ToString() + " %";
                //this.txtPercent.Foreground = Brushes.Black;
                //this.txtPercent.Visibility = Visibility.Visible;
                //this.pb.Value = totalPercent;
                //this.pb.Foreground = Brushes.Orange;
                //this.pb.Background = Brushes.Black;
                //this.pb.Visibility = Visibility.Visible;
                //this.btnPausing.Visibility = Visibility.Visible;
                   
             
            }
        }

        private void MeetingRoom_DownloadProgressChanged_Callback(File_DownloadItemViewModel fileItem,bool ForceUpdate)
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action<File_DownloadItemViewModel, bool>(MeetingRoom_DownloadProgressChanged_Callback), fileItem, ForceUpdate);
                this.Dispatcher.BeginInvoke(new Action<File_DownloadItemViewModel, bool>(MeetingRoom_DownloadProgressChanged_Callback), fileItem, ForceUpdate);
            }
            else
            {
                ForceStopAjaxLoader = true;

                double totalPercent = GetNowMeetingFilesTotalPercent(fileItem);
               
                if (totalPercent>=100)
                {
                    // 下載完了，顯示可以刪除的圖示
                    //this.btnDownloadForbidden.Visibility = Visibility.Collapsed;
                    //this.btnRead2Forbidden.Visibility = Visibility.Collapsed;
                    //this.btnDownload.Visibility = Visibility.Collapsed;
                    //this.btnPausing.Visibility = Visibility.Collapsed;
                    //this.txtPercent.Visibility = Visibility.Collapsed;
                    //this.pb.Visibility = Visibility.Collapsed;
                    //this.btnPause.Visibility = Visibility.Collapsed;

                    //this.btnDelete.Visibility = Visibility.Visible;
                }
                //else if (totalPercent - pb.Value > 1 || ForceUpdate==true)
                //{
                    // 還沒下載完了，持續更新進度
                    //this.btnDownloadForbidden.Visibility = Visibility.Collapsed;
                    //this.btnRead2Forbidden.Visibility = Visibility.Collapsed;
                    //this.btnDelete.Visibility = Visibility.Collapsed;
                    //this.btnDownload.Visibility = Visibility.Collapsed;
                    //this.btnPausing.Visibility = Visibility.Collapsed;
                    //this.txtPercent.Text = ((int)totalPercent).ToString() + " %";
                    //this.txtPercent.Foreground = Brushes.Black;
                    //this.txtPercent.Visibility = Visibility.Visible;
                    //this.pb.Value = totalPercent;
                    //this.pb.Foreground = Brushes.Orange;
                    //this.pb.Background = Brushes.Black;
                    //this.pb.Visibility = Visibility.Visible;
                    //this.btnPause.Visibility = Visibility.Visible;
                //}
               
                    
               
            }
        }

        private double GetNowMeetingFilesTotalPercent(File_DownloadItemViewModel fileItem)
        {
            double totalPercent = 0;

            DataTable dt = MSCE.GetDataTable(@"select count(ID) as FinishedFiles from FileRow where UserID =@1 and MeetingID=@2 and DownloadBytes!=0 and DownloadBytes >= TotalBytes;"
                                  , UserID, userMeeting.ID);

            if (dt.Rows.Count > 0)
                int.TryParse(dt.Rows[0]["FinishedFiles"].ToString(), out FinishedFiles);

            if (TotalFiles > 0)
            {
                totalPercent = ((double)FinishedFiles / (double)TotalFiles) * 100.0;

                if(fileItem.NowPercentage < 100)
                {
                    //小於100趴才需要，後面這一段+ (fileItem.NowPercentage / 100.0) * (100.0 / (double)TotalFiles)
                    totalPercent += (fileItem.NowPercentage / 100.0) * (100.0 / (double)TotalFiles);
                }
            }
           

            return totalPercent;
        }

        private void MeetingRoom_DownloadFileStart_Callback(File_DownloadItemViewModel fileItem)
        {
            if (HasReceiveStart_callback == false)
            {
                HasReceiveStart_callback = true;
                MeetingRoom_DownloadProgressChanged_Callback(fileItem, true);
            }
               
        }

        private void btnSeries_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Home Home_Window = Application.Current.Windows.OfType<Home>().FirstOrDefault();
            Home_Window.ShowBtnSeriesCT(userMeeting.SeriesMeetingID);
          
        }

        private void MeetingRoom_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            string SQL = @"Update NowLogin set MeetingID=@1,AllowIpRange='' ";
            int success = MSCE.ExecuteNonQuery(SQL, userMeeting.ID);
            if (success < 1)
            {
                LogTool.Debug(new Exception(@"DB失敗: " + SQL));
                return;
            }
           

            //20141203
            //Home_Change2MeetingDataCT_Event(userMeeting.ID);

            //20150422
            Home_Change2MeetingDataCT_Event(userMeeting.ID,PreLoadMeetingData);
           
        }

        private Size MeasureString(string candidate)
        {
            var formattedText = new FormattedText(
                candidate,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(this.txtName.FontFamily, this.txtName.FontStyle, this.txtName.FontWeight, this.txtName.FontStretch),
                this.txtName.FontSize,
                Brushes.Black);

            return new Size(formattedText.Width, formattedText.Height);
        }

        private void InitUI()
        {
            var date = DateTime.ParseExact(VM.Meeting.BeginTime, "yyyyMMddHHmmss",null);
            txtLocation.Text = $"開會日期:{date.ToString("yyyy/MM/dd")}";
            txtTime.Text = VM.Meeting.Name;
            txtName.Text = VM.Name;
            var ss = MeasureString(VM.Name);
            if (ss.Width > 282)
                txtName.FontSize = 13;
            string filePath = ClickOnceTool.GetFilePath();
            string UnZipFileFolder = PaperLess_Emeeting.Properties.Settings.Default.File_UnZipFileFolder;
            string baseBookPath = filePath + "\\" + UnZipFileFolder + "\\" + UserID + "\\" + VM.Meeting.ID;

            DataTable dt = MSCE.GetDataTable("SELECT FinishedFileVersion FROM FileRow where ID=@1 and UserID=@2 and MeetingID=@3"
                            , VM.ID
                            , UserID
                            , VM.Meeting.ID);

            string _bookPath = baseBookPath + "\\" + VM.ID;
          
            string FinishedFileVersion = "1";
            MeetingFileCate cate;

            if (VM.ID.EndsWith("P"))
                cate = MeetingFileCate.電子書;
            else if (VM.ID.EndsWith("H"))
                cate = MeetingFileCate.Html5投影片;
            else
                cate = MeetingFileCate.影片檔;

            if (dt.Rows.Count > 0)
            {
                FinishedFileVersion = dt.Rows[0]["FinishedFileVersion"].ToString();
                Bookpth = baseBookPath + "\\" + VM.ID + "\\" + FinishedFileVersion;
                bookVM = new BookVM(VM.ID, baseBookPath + "\\" + VM.ID + "\\" + FinishedFileVersion, cate);
            }
            else
            {
                bookVM = new BookVM(VM.ID, "", cate);
            }




            if (bookVM.BookPath.Equals("") == false)
            {

                if (bookVM.FileCate == MeetingFileCate.Html5投影片)
                {
                   
                    if (File.Exists(bookVM.BookPath + "\\data\\Thumbnails\\Slide1.png"))
                    {
                        hasDownloaded = true;
                        imgCover.Source = new BitmapImage(new Uri(bookVM.BookPath + "\\data\\Thumbnails\\Slide1.png"));
                    }
                  

                }
                else if(bookVM.FileCate == MeetingFileCate.電子書)
                {
                    string folderpath = bookVM.BookPath + $"\\HYWEB\\thumbs\\";
                    if (Directory.Exists(folderpath))
                    {
                        var file = Directory.GetFiles(folderpath).ToList().OrderBy(item => item).FirstOrDefault();

                        if (file != null)
                        {
                            hasDownloaded = true;
                            imgCover.Source = new BitmapImage(new Uri(file));
                        }
                    }
                   
                }
                else
                {
                    if (Directory.Exists(bookVM.BookPath))
                    {
                        var i = Directory.GetFiles(bookVM.BookPath).Count();
                        if (i > 0)
                        {

                            hasDownloaded = true;
                            imgCover.Source = new BitmapImage(new Uri("images/icon_video@2x.png", UriKind.Relative));
                        }
                    }
                }
            }

            MeetingRoomButtonType meetingRoomButtonType;

            string isBrowserd = "N";
            string isDownload = "N";

            DateTime BrowseS = DateTime.Now ;
            DateTime BrowseE = DateTime.Now;
            DateTime DownloadS = DateTime.Now;
            DateTime DownloadE = DateTime.Now;

            if (VM.BrowseTime.BeginTime.Length == 0)
                VM.BrowseTime.BeginTime = DateTime.MinValue.ToString("yyyyMMddHHmmss");

            if (VM.BrowseTime.EndTime.Length == 0)
                VM.BrowseTime.EndTime = DateTime.MaxValue.ToString("yyyyMMddHHmmss");

            if (VM.DownloadTime.BeginTime.Length == 0)
                VM.DownloadTime.BeginTime = DateTime.MinValue.ToString("yyyyMMddHHmmss");

            if (VM.DownloadTime.EndTime.Length == 0)
                VM.DownloadTime.EndTime = DateTime.MaxValue.ToString("yyyyMMddHHmmss");

            BrowseS = DateTime.ParseExact(VM.BrowseTime.BeginTime, "yyyyMMddHHmmss", null);

            BrowseE = DateTime.ParseExact(VM.BrowseTime.EndTime, "yyyyMMddHHmmss", null);

            DownloadS = DateTime.ParseExact(VM.DownloadTime.BeginTime, "yyyyMMddHHmmss", null);

            DownloadE = DateTime.ParseExact(VM.DownloadTime.EndTime, "yyyyMMddHHmmss", null);

            if (DateTime.Now > BrowseS && DateTime.Now < BrowseE)
                isBrowserd = "Y";

            if (DateTime.Now > DownloadS && DateTime.Now < DownloadE)
                isDownload = "Y";



            Enum.TryParse(isDownload + isBrowserd, out meetingRoomButtonType);

            switch (meetingRoomButtonType)
            {
                case MeetingRoomButtonType.YN:
                    imgCover2.Source = new BitmapImage(new Uri("images/icon_read2_forbidden@2x.png", UriKind.Relative));
                    imgCover2.Visibility = Visibility.Visible;
                    txtView.Visibility = Visibility.Visible;
                    txtDownload.Visibility = Visibility.Collapsed;
                    txtDownloading.Visibility = Visibility.Collapsed;
                    imgCover.Visibility = Visibility.Collapsed;
                    // 沒有圖示
                    break;
                case MeetingRoomButtonType.NN:
                    imgCover2.Source = new BitmapImage(new Uri("images/icon_read2_forbidden@2x.png", UriKind.Relative));
                    imgCover2.Visibility = Visibility.Visible;
                    txtView.Visibility = Visibility.Visible;
                    txtDownload.Visibility = Visibility.Collapsed;
                    txtDownloading.Visibility = Visibility.Collapsed;
                    imgCover.Visibility = Visibility.Collapsed;
                    // 沒有圖示
                    break;
                case MeetingRoomButtonType.YY:
                    // 未下載檔案: 下載圖示
                    // 已下載檔案: 垃圾埇圖示
                    imgCover.Visibility = Visibility.Visible;
                    break;
                case MeetingRoomButtonType.ON:
                    //if(hasDownloaded)
                    //{
                    //    imgCover2.Source = new BitmapImage(new Uri("images/icon_download_forbidden@2x.png", UriKind.Relative));
                    //    imgCover2.Visibility = Visibility.Visible;
                    //    txtDownload.Visibility = Visibility.Visible;
                    //    txtDownloading.Visibility = Visibility.Collapsed;
                    //    txtView.Visibility = Visibility.Collapsed;
                    //    imgCover.Visibility = Visibility.Collapsed;
                    //}
                    //else
                    //{
                    //    imgCover2.Source = new BitmapImage(new Uri("images/icon_download_forbidden@2x.png", UriKind.Relative));
                    //    imgCover2.Visibility = Visibility.Visible;
                    //    txtDownload.Visibility = Visibility.Visible;
                    //    txtDownloading.Visibility = Visibility.Collapsed;
                    //    txtView.Visibility = Visibility.Collapsed;
                    //    imgCover.Visibility = Visibility.Collapsed;
                    //}
                    imgCover2.Source = new BitmapImage(new Uri("images/icon_read2_forbidden@2x.png", UriKind.Relative));
                    imgCover2.Visibility = Visibility.Visible;
                    txtView.Visibility = Visibility.Visible;
                    txtDownload.Visibility = Visibility.Collapsed;
                    txtDownloading.Visibility = Visibility.Collapsed;
                    imgCover.Visibility = Visibility.Collapsed;
                    break;
                case MeetingRoomButtonType.OY:
                    if (!hasDownloaded)
                    {
                        imgCover2.Source = new BitmapImage(new Uri("images/icon_download_forbidden@2x.png", UriKind.Relative));
                        imgCover2.Visibility = Visibility.Visible;
                        txtDownload.Visibility = Visibility.Visible;
                        txtDownload.Visibility = Visibility.Collapsed;
                        txtView.Visibility = Visibility.Collapsed;
                        imgCover.Visibility = Visibility.Collapsed;
                    }
                    break;
                case MeetingRoomButtonType.NO:
                    //if (!hasDownloaded)
                    //{
                    //    imgCover2.Source = new BitmapImage(new Uri("images/icon_download_forbidden@2x.png", UriKind.Relative));
                    //    imgCover2.Visibility = Visibility.Visible;
                    //    txtDownload.Visibility = Visibility.Visible;
                    //    txtDownloading.Visibility = Visibility.Collapsed;
                    //    txtView.Visibility = Visibility.Collapsed;
                    //    imgCover.Visibility = Visibility.Collapsed;
                    //}
                    //else
                    //{
                    //    imgCover2.Source = new BitmapImage(new Uri("images/icon_read2_forbidden@2x.png", UriKind.Relative));
                    //    imgCover2.Visibility = Visibility.Visible;
                    //    txtView.Visibility = Visibility.Visible;
                    //}
                    imgCover2.Source = new BitmapImage(new Uri("images/icon_read2_forbidden@2x.png", UriKind.Relative));
                    imgCover2.Visibility = Visibility.Visible;
                    txtView.Visibility = Visibility.Visible;
                    txtDownload.Visibility = Visibility.Collapsed;
                    txtDownloading.Visibility = Visibility.Collapsed;
                    imgCover.Visibility = Visibility.Collapsed;
                    break;
                case MeetingRoomButtonType.YO:
                        imgCover2.Source = new BitmapImage(new Uri("images/icon_read2_forbidden@2x.png", UriKind.Relative));
                        imgCover2.Visibility = Visibility.Visible;
                        txtView.Visibility = Visibility.Visible;
                    break;
                case MeetingRoomButtonType.OO:
                    //if (!hasDownloaded)
                    //{
                    //    imgCover2.Source = new BitmapImage(new Uri("images/icon_download_forbidden@2x.png", UriKind.Relative));
                    //    imgCover2.Visibility = Visibility.Visible;
                    //    txtDownload.Visibility = Visibility.Visible;
                    //    txtDownloading.Visibility = Visibility.Collapsed;
                    //    txtView.Visibility = Visibility.Collapsed;
                    //    imgCover.Visibility = Visibility.Collapsed;
                    //}
                    //else
                    //{
                    //    imgCover2.Source = new BitmapImage(new Uri("images/icon_read2_forbidden@2x.png", UriKind.Relative));
                    //    imgCover2.Visibility = Visibility.Visible;
                    //    txtView.Visibility = Visibility.Visible;
                    //}
                    imgCover2.Source = new BitmapImage(new Uri("images/icon_read2_forbidden@2x.png", UriKind.Relative));
                    imgCover2.Visibility = Visibility.Visible;
                    txtView.Visibility = Visibility.Visible;
                    txtDownload.Visibility = Visibility.Collapsed;
                    txtDownloading.Visibility = Visibility.Collapsed;
                    imgCover.Visibility = Visibility.Collapsed;
                    break;
                case MeetingRoomButtonType.NY:
                    if (!hasDownloaded)
                    {
                        imgCover2.Source = new BitmapImage(new Uri("images/icon_download_forbidden@2x.png", UriKind.Relative));
                        imgCover2.Visibility = Visibility.Visible;
                        txtDownload.Visibility = Visibility.Visible;
                        txtDownloading.Visibility = Visibility.Collapsed;
                        txtView.Visibility = Visibility.Collapsed;
                        imgCover.Visibility = Visibility.Collapsed;
                    }
                    break;

            }

            //imgCover.Visibility = Visibility.Visible;






            // (1)背景顏色處理好之後，就直接顯示
            //try
            //{


            //    // 如果 parse 失敗 會維持白色
            //    // 正在開會議的會議
            //    if (DateTime.Parse(userMeeting.BeginTime) <= DateTime.Now && DateTime.Now < DateTime.Parse(userMeeting.EndTime))
            //    {
            //        //txtMeetingName.Foreground = Brushes.White;
            //        txtLocation.Foreground = Brushes.White;
            //        txtTime.Foreground = Brushes.White;

            //        // 新增的會議
            //        if (NewAddMeetingID.Equals(userMeeting.ID) == true)
            //        {
            //        }
            //    }
            //    else
            //    {
            //        //txtMeetingName.Foreground = Brushes.Black;
            //        txtLocation.Foreground = Brushes.Black;
            //        txtTime.Foreground = Brushes.Black;

            //        // 新增的會議
            //        if (NewAddMeetingID.Equals(userMeeting.ID) == true)
            //        {
            //        }
            //    }    
            //}
            //catch (Exception ex)
            //{
            //    LogTool.Debug(ex);
            //}


            //txtMeetingName.Text = userMeeting.Name;
            //txtLocation.Text=userMeeting.Location;
            //txtTime.Text = DateTime.Parse(userMeeting.BeginTime).ToString("HH:mm");

            //if (userMeeting.SeriesMeetingID!=null && userMeeting.SeriesMeetingID.Equals("") == false)
            //if (userMeeting.SeriesMeetingID != null && userMeeting.SeriesMeetingID.Length > 0)
            //    btnSeries.Visibility = Visibility.Visible;

            //如果不要隱形，就顯示出來
            //if (invisible == false)
            //{
            // (2)背景顏色處理好之後，就直接顯示
            //this.Visibility = Visibility.Visible;
            //}
            //else
            //{
            //    this.Visibility = Visibility.Collapsed;
            //}

            //switch (meetingRoomButtonType)
            //{
            //    case MeetingRoomButtonType.NN:
            //        // 沒有圖示
            //        break;
            //    case MeetingRoomButtonType.YY:
            //        // 未下載檔案: 下載圖示，包含續傳到一半的檔案
            //        // 已下載檔案: 垃圾埇圖示
            //        // 都要先檢查所有檔案的下載進度

            //            // 下載完了
            //            if (FinishedPercent >= 100)
            //            {
            //                //btnDelete.Visibility = Visibility.Visible;
            //            }
            //            else //尚未下載完
            //            {
            //                // 沒有下載過
            //                if (FinishedPercent < 1)
            //                {
            //                    //btnDownload.Visibility = Visibility.Visible;
            //                }
            //                else
            //                {
            //                    //  這裡慢一點再顯示出來，沒關係
            //                    // 可以先給Ajax動畫
            //                    // 選擇(一)，有動畫
            //                    ajaxTimer = new DispatcherTimer();
            //                    ajaxTimer.Interval = TimeSpan.FromMilliseconds(30);
            //                    ajaxTimer.Tick += new EventHandler(ajaxLoader_ChangeImage);
            //                    ajaxTimer.Start();

            //                    // 選擇(二)，沒有動畫
            //                    //txtPercent.Text = FinishedPercent + " %";
            //                    //pb.Value = FinishedPercent;
            //                    //pb.Foreground = Brushes.Wheat;
            //                    //pb.Background = Brushes.Gray;
            //                    //txtPercent.Visibility = Visibility.Visible;
            //                    //pb.Visibility = Visibility.Visible;
            //                    //btnPausing.Visibility = Visibility.Visible;   

            //                }
            //            }
            //        break;
            //    case MeetingRoomButtonType.OY:
            //        // 未下載檔案: 不可下載圖示，直接顯示不可下載圖示
            //        // 已下載檔案: 垃圾埇圖示
            //        // 都要先檢查所有檔案的下載進度

            //            // 下載完了
            //            if (FinishedPercent >= 100)
            //            {
            //                //btnDelete.Visibility = Visibility.Visible;
            //            }
            //            else //尚未下載完
            //            {
            //                ////btnDownloadForbidden.Visibility = Visibility.Visible;
            //            }
            //        break;
            //    case MeetingRoomButtonType.YO:
            //        //不可瀏覽圖示
            //        //btnRead2Forbidden.Visibility = Visibility.Visible;
            //        break;
            //    case MeetingRoomButtonType.OO:
            //        // 未下載檔案: 不可下載圖示
            //        // 已下載檔案: 不可瀏覽圖示
            //        //btnRead2Forbidden.Visibility = Visibility.Visible;
            //        break;
            //}


            ////最後判斷是從系列會議到底是否隱藏(2)
            //if (this.invisible == true)
            //{
            //    this.Visibility = Visibility.Collapsed;
            //}


        }

        private void ShowBtnPercent(int Percent)
        {
            //btnDelete.Visibility = Visibility.Collapsed;  
            //btnDownload.Visibility = Visibility.Collapsed;
            //btnPause.Visibility = Visibility.Collapsed;
            //txtPercent.Text = FinishedPercent + " %";
            //pb.Value = FinishedPercent;
            //pb.Foreground = Brushes.Wheat;
            //pb.Background = Brushes.Gray;
            //txtPercent.Visibility = Visibility.Visible;
            //pb.Visibility = Visibility.Visible;
            //btnPausing.Visibility = Visibility.Visible;   
        }

       

        int i = 0;
        //int ajaxTimes = 1;
        private void ajaxLoader_ChangeImage(object sender, EventArgs e)
        {
            string fileName=string.Format("images/ajaxLoader/{0}.gif", i);
            //ajaxLoader.Source = new BitmapImage(new Uri(fileName,UriKind.Relative));
            i++;
            if (ForceStopAjaxLoader==true || i >= 11 )
            {
                //i = 0;
                //if (ForceStopAjaxLoader==true || ajaxTimes > 1 )
                //{
                    ajaxTimer.Stop();
                    //ajaxLoader.Visibility = Visibility.Collapsed;
                    i = 1;	// Display first image after the last image

                    FileDownloader fileItem = Singleton_FileDownloader.GetInstance(userMeeting.ID);
                    if (fileItem.downloaderType != DownloaderType.正在下載中)
                        ShowBtnPercent(this.FinishedPercent);
                //}
                //ajaxTimes++;
               
            }
        }
      

        private int GetFinishedPercent()
        {
            int Rtn_FinishedPercent = 0;
            try
            {
                
                // 多少檔案完成
                DataTable dt = MSCE.GetDataTable(@"select count(ID) as FinishedFiles from FileRow where UserID =@1 and MeetingID=@2 and DownloadBytes!=0 and DownloadBytes >= TotalBytes;"
                                     , UserID, userMeeting.ID);

                if (dt.Rows.Count > 0)
                {
                    int.TryParse(dt.Rows[0]["FinishedFiles"].ToString(), out FinishedFiles);
                }

                //// 抓取檔案路徑
                //dt = MSCE.GetDataTable(@"select StorageFileName from FileRow where UserID =@1 and MeetingID=@2 and DownloadBytes!=0 and DownloadBytes >= TotalBytes;"
                //                , UserID, userMeeting.ID);

                //// 多少檔案被刪除
                //foreach (DataRow dr in dt.Rows)
                //{
                //    string filePath = ClickOnceTool.GetFilePath();
                //    string StorageFileFolder = MyPL.Properties.Settings.Default.File_StorageFileFolder;
                //    string StorageFilePath = filePath + "\\" + StorageFileFolder + "\\" + dr["StorageFileName"].ToString();
                //    if (File.Exists(StorageFilePath) == false)
                //        FinishedFiles--;

                //}

                // 全部完成。
                if (FinishedFiles > 0 && FinishedFiles >= TotalFiles)
                {
                    Rtn_FinishedPercent = 100;
                }
                else
                {

                    // 檢查百分比
                    if (TotalFiles > 0)
                        Rtn_FinishedPercent = (int)(((double)FinishedFiles / (double)TotalFiles) * 100.0);

                    double SumDownloadBytes = 0;
                    double SumTotalBytes = 0;

                    dt = MSCE.GetDataTable(@"select sum(DownloadBytes) as sumDownloadBytes from FileRow where UserID =@1 and MeetingID=@2 and DownloadBytes!=0 and DownloadBytes < TotalBytes;"
                                           , UserID, userMeeting.ID);
                    if (dt.Rows.Count > 0)
                        double.TryParse(dt.Rows[0]["SumDownloadBytes"].ToString(), out SumDownloadBytes);

                    dt = MSCE.GetDataTable(@"select sum(TotalBytes) as SumTotalBytes from FileRow where UserID =@1 and MeetingID=@2 and DownloadBytes!=0 and DownloadBytes < TotalBytes;"
                                         , UserID, userMeeting.ID);
                    if (dt.Rows.Count > 0)
                        double.TryParse(dt.Rows[0]["SumTotalBytes"].ToString(), out SumTotalBytes);

                    if (SumTotalBytes > 0)
                        Rtn_FinishedPercent += (int)( (SumDownloadBytes / SumTotalBytes) / (TotalFiles - FinishedFiles) * (100.0 / (double)TotalFiles) );
                    //dt = MSCE.GetDataTable(@"select DownloadBytes from FileRow where UserID =@3 and MeetingID=@4;"
                    //                        , UserID, userMeeting.ID);
                    //foreach(DataRow dr in dt.Rows)
                    //{
                    //    double DownloadBytes = 0;
                    //    double.TryParse(dr["DownloadBytes"].ToString(), out DownloadBytes);
                    //    SumDownloadBytes += DownloadBytes;
                    //}

                    ////(10MB)
                    //int NotHasTotalBytes = 1024 * 1024 * 10; 
                    //dt = MSCE.GetDataTable(@"select TotalBytes from FileRow where UserID =@3 and MeetingID=@4;"
                    //                      , UserID, userMeeting.ID);

                    //int.TryParse(dt.Compute("max(TotalBytes)", "").ToString(),out NotHasTotalBytes);

                    //foreach (DataRow dr in dt.Rows)
                    //{

                    //    double TotalBytes = 0;
                    //    double.TryParse(dr["TotalBytes"].ToString(), out TotalBytes);
                    //    if (TotalBytes==0)
                    //        SumTotalBytes += NotHasTotalBytes;
                    //    else
                    //        SumTotalBytes += TotalBytes;
                    //}

                    //if (SumTotalBytes > 0)
                    //    Rtn_FinishedPercent += (int)(SumDownloadBytes / SumTotalBytes * 100.0) / (TotalFiles - FinishedFiles);
                    
                }
            }
            catch(Exception ex)
            {
                LogTool.Debug(ex);
            }


            return Rtn_FinishedPercent;
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Window Home_Window = App.Current.Windows.OfType<Home>().FirstOrDefault();
            DelFile win = new DelFile(Home_Window,FolderID, VM.ID);
            win.ReConfirm = true;
            var success = win.ShowDialog();
            if (success == true)
                DelAction(true);

            e.Handled = true;
        }

        private void imgCover_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            if (hasDownloaded == false)
            {
                Storyboard sb = (Storyboard)this.TryFindResource("sb");
                txtDownloading.Visibility = Visibility.Visible;
                imgCover.Visibility = Visibility.Collapsed;
                imgCover2.Visibility = Visibility.Collapsed;
                sb.Begin();

                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += (sender2, e2) => {
                       
                        imgCover.Source = null;
                        sb.Stop();

                        Storyboard sb2 = (Storyboard)this.TryFindResource("sb2");
                        sb2.Begin();

                        hasDownloaded = false;
                        imgCover.Visibility = Visibility.Collapsed;
                        txtDownloading.Visibility = Visibility.Collapsed;
                        imgCover.Visibility = Visibility.Collapsed;
                        imgCover2.Visibility = Visibility.Collapsed;
                        txtUnZip.Visibility = Visibility.Visible;

                    DispatcherTimer timer2 = new DispatcherTimer();
                        timer2.Interval = TimeSpan.FromSeconds(1);
                        timer2.Tick += (sender3, e3) =>
                        {
                            string filePath = ClickOnceTool.GetFilePath();
                            string UnZipFileFolder = PaperLess_Emeeting.Properties.Settings.Default.File_UnZipFileFolder;
                            string baseBookPath = filePath + "\\" + UnZipFileFolder + "\\" + UserID + "\\" + VM.Meeting.ID;

                            DataTable dt = MSCE.GetDataTable("SELECT FinishedFileVersion FROM FileRow where ID=@1 and UserID=@2 and MeetingID=@3"
                                            , VM.ID
                                            , UserID
                                            , VM.Meeting.ID);

                            string _bookPath = baseBookPath + "\\" + VM.ID;

                            string FinishedFileVersion = "1";
                            MeetingFileCate cate;

                            if (VM.ID.EndsWith("P"))
                                cate = MeetingFileCate.電子書;
                            else if (VM.ID.EndsWith("H"))
                                cate = MeetingFileCate.Html5投影片;
                            else
                                cate = MeetingFileCate.影片檔;

                            if (dt.Rows.Count > 0)
                            {
                                FinishedFileVersion = dt.Rows[0]["FinishedFileVersion"].ToString();
                                Bookpth = baseBookPath + "\\" + VM.ID + "\\" + FinishedFileVersion;
                                bookVM = new BookVM(VM.ID, baseBookPath + "\\" + VM.ID + "\\" + FinishedFileVersion, cate);
                            }
                            else
                            {
                                bookVM = new BookVM(VM.ID, "", cate);
                            }




                            if (bookVM.BookPath.Equals("") == false)
                            {

                                if (bookVM.FileCate == MeetingFileCate.Html5投影片)
                                {

                                    if (File.Exists(bookVM.BookPath + "\\data\\Thumbnails\\Slide1.png"))
                                    {
                                        imgCover.Source = new BitmapImage(new Uri(bookVM.BookPath + "\\data\\Thumbnails\\Slide1.png"));
                                        hasDownloaded = true;
                                        txtDownloading.Visibility = Visibility.Collapsed;
                                        imgCover.Visibility = Visibility.Visible;
                                        imgCover2.Visibility = Visibility.Collapsed;
                                        txtUnZip.Visibility = Visibility.Collapsed;
                                        sb2.Stop();
                                        HasImage = true;
                                        timer.Stop();
                                        timer2.Stop();
                                    }
                                }
                                else if(bookVM.FileCate == MeetingFileCate.電子書)
                                {
                                    if (File.Exists(bookVM.BookPath + $"\\HYWEB\\thumbs\\{bookVM.BookPath.Split('\\').Last()}-463a-P_1.jpg"))
                                    {
                                        imgCover.Source = new BitmapImage(new Uri(bookVM.BookPath + $"\\HYWEB\\thumbs\\{bookVM.BookPath.Split('\\').Last()}-463a-P_1.jpg"));
                                        hasDownloaded = true;
                                        txtDownloading.Visibility = Visibility.Collapsed;
                                        imgCover.Visibility = Visibility.Visible;
                                        imgCover2.Visibility = Visibility.Collapsed;
                                        txtUnZip.Visibility = Visibility.Collapsed;
                                        sb2.Stop();
                                        HasImage = true;
                                        timer.Stop();
                                        timer2.Stop();
                                    }

                                }
                                else
                                {
                                    if (Directory.Exists(bookVM.BookPath))
                                    {
                                        var i = Directory.GetFiles(bookVM.BookPath).Count();
                                        if (i > 0)
                                        {

                                            imgCover.Source = new BitmapImage(new Uri("images/icon_video@2x.png", UriKind.Relative));
                                            hasDownloaded = true;
                                            txtDownloading.Visibility = Visibility.Collapsed;
                                            imgCover.Visibility = Visibility.Visible;
                                            imgCover2.Visibility = Visibility.Collapsed;
                                            txtUnZip.Visibility = Visibility.Collapsed;
                                            sb2.Stop();
                                            HasImage = true;
                                            timer.Stop();
                                            timer2.Stop();
                                        }
                                    }
                                   
                                }
                            }

                            if (HasImage)
                            {

                                hasDownloaded = true;
                                txtDownloading.Visibility = Visibility.Collapsed;
                                imgCover.Visibility = Visibility.Visible;
                                imgCover2.Visibility = Visibility.Collapsed;
                                txtUnZip.Visibility = Visibility.Collapsed;
                            }
                            else
                            {
                                hasDownloaded = false;
                                imgCover.Visibility = Visibility.Collapsed;
                                txtDownloading.Visibility = Visibility.Collapsed;
                                imgCover.Visibility = Visibility.Collapsed;
                                imgCover2.Visibility = Visibility.Collapsed;
                                txtUnZip.Visibility = Visibility.Visible;

                            }
                        };

                        timer2.Start();

                    };
                timer.Start();
                GetMeetingData.AsyncPOST(VM.Meeting.ID, UserID, UserPWD, (md) => { GetMeetingData_DoAction(md); });
            }
        }


        private void GetMeetingData_DoAction(MeetingData md)
        {
            
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                this.Dispatcher.BeginInvoke(new Action<MeetingData, Image>(GetMeetingData_DoAction), md, null);
            }
            else
            {
                if (md != null)
                {
                    

                    Task.Factory.StartNew(() =>
                    {
                        FileDownloader fileDownloader = Singleton_FileDownloader.GetInstance(md.ID);
                        fileDownloader.Stop();

                        List<MeetingDataDownloadFileFile> FileList = new List<MeetingDataDownloadFileFile>();
                        try
                        {
                            // <File ID="cAS66-P" Url="http://com-meeting.ntpc.hyweb.com.tw/Public/MeetingAttachFile/2/2-b167-P.phej" FileName="ae717047" version="1"/>

                            // 如果meetingData.MeetingsFile.FileList沒有子節點，就會轉型失敗
                            //XmlNode[] FileListXml = (XmlNode[])md.MeetingsFile.FileList;
                            //foreach (XmlNode item in FileListXml)
                            foreach (MeetingDataMeetingsFileFile item in md.MeetingsFile.FileList)
                            {
                                MeetingDataDownloadFileFile recordFile = new MeetingDataDownloadFileFile();
                                recordFile.AgendaID = "record";
                                //recordFile.FileName = item.Attributes["FileName"].Value;
                                //recordFile.ID = item.Attributes["ID"].Value;
                                //recordFile.Url = item.Attributes["Url"].Value;
                                //recordFile.version = byte.Parse(item.Attributes["version"].Value);
                                recordFile.FileName = item.FileName;
                                recordFile.ID = item.ID;
                                recordFile.Url = item.Url;
                                recordFile.version = item.version;
                                FileList.Add(recordFile);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogTool.Debug(ex);
                        }

                        FileList.AddRange(md.DownloadFile.DownloadFileList.ToList());
                        TotalFiles = FileList.Count;
                        List<File_DownloadItemViewModel> fileItemList = new List<File_DownloadItemViewModel>();
                        foreach (MeetingDataDownloadFileFile meetingDataDownloadFileFile in FileList)
                        {
                            if (meetingDataDownloadFileFile.ID.Equals(VM.ID))
                            {
                                File_DownloadItemViewModel fileItem = FileItemTool.Gen(meetingDataDownloadFileFile, UserID, md.ID);
                                if(fileItem.ID.Equals(VM.ID))
                                if (fileItem.DownloadBytes == 0 || fileItem.DownloadBytes < fileItem.TotalBytes)
                                    fileItemList.Add(fileItem);
                            }
                        }


                        if (fileDownloader.HasMeetingRoom_DownloadFileStart_Event() == false)
                        {
                            fileDownloader.MeetingRoom_DownloadFileStart_Event += Start_callback;
                        }

                        if (fileDownloader.HasMeetingRoom_DownloadProgressChanged_Event() == false)
                        {
                            fileDownloader.MeetingRoom_DownloadProgressChanged_Event += Progress_callback;
                        }

                        if (fileDownloader.HasMeetingRoom_DownloadFileToErrorCompleted_Event() == false)
                        {
                            fileDownloader.MeetingRoom_DownloadFileToErrorCompleted_Event += ErrorFinish_callback;
                        }



                        fileDownloader.AddItem(fileItemList);
                    });

                   
                }
                else
                {
                    AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
                }
                MouseTool.ShowArrow();
            }
        }

        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (hasDownloaded == false)
                return;
            MouseTool.ShowLoading();

            if (txtView.Visibility == Visibility.Visible || txtDownload.Visibility == Visibility.Visible)
                return;

            try
            {
                //string AppPath = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = ClickOnceTool.GetFilePath();

                //string _bookPath = System.IO.Path.Combine(AppPath, lawItem.UnZipFilePath);
                string _bookPath = Bookpth;
                string _bookId = "";
                string _account = "";
                string _userName = "";
                string _email = "";
                string _meetingId = "";
                string _watermark = "";
                string _dbPath = System.IO.Path.Combine(ClickOnceTool.GetDataPath(), PaperLess_Emeeting.Properties.Settings.Default.bookInfo_Path);
                bool _isSync = false;
                bool _isSyncOwner = false;
                string _webServiceUrl = WsTool.GetUrl() + "/AnnotationUpload";
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
                        string  year = VM.Meeting.BeginTime.Substring(0, 4);
                        string MM = VM.Meeting.BeginTime.Substring(4,2);
                        string d = VM.Meeting.BeginTime.Substring(6, 2);

                        bool today = false;
                        if(DateTime.Now.ToString("yyyyMMdd").Equals($"{year}{MM}{d}"))
                        {
                            today = true;
                        }
                        if (VM.ID.EndsWith("H"))
                        {
                            Bookpth = _bookPath + @"\" + new FileInfo(Directory.GetFiles(Bookpth)[0]).Name;
                            HTML5ReadWindow Html5rw = new HTML5ReadWindow(cbBooksData, Home_Window.OpenBookFromReader, Bookpth, _bookId, _account
                                                      , _userName, _email, _meetingId
                                                      , _watermark, _dbPath, _isSync
                                                      , _isSyncOwner, _webServiceUrl, _socketMessage, null);
                            Html5rw.FolderID = this.FolderID;
                            Html5rw.WindowStyle = WindowStyle.None;
                            Html5rw.cloud = true;
                            Html5rw.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                            Html5rw.WindowState = WindowState.Maximized;
                            Html5rw.today = today;
                            if (PaperLess_Emeeting.Properties.Settings.Default.AssemblyName.Contains("TPI4F") == true)
                            {
                                //Html5rw.WindowStyle = WindowStyle.SingleBorderWindow;
                            }
                           
                            Html5rw.Show();
                        }
                        else if (VM.ID.EndsWith("P"))
                        {
                            ReadWindow rw = new ReadWindow(cbBooksData, Home_Window.OpenBookFromReader, Bookpth, _bookId, _account
                                             , _userName, _email, _meetingId
                                             , _watermark, _dbPath, _isSync
                                             , _isSyncOwner, _webServiceUrl, _socketMessage, null);
                            rw.WindowStyle = WindowStyle.None;
                            rw.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                            rw.WindowState = WindowState.Maximized;
                            rw.cloud = true;
                            rw.today = today;
                            if (PaperLess_Emeeting.Properties.Settings.Default.AssemblyName.Contains("TPI4F") == true)
                            {
                                //rw.WindowStyle = WindowStyle.SingleBorderWindow;
                            }
                            rw.Show();
                        }
                        else
                        {
                            MVWindow mvWindow = new MVWindow(cbBooksData, Home_Window.OpenBookFromReader, _bookPath, _bookId);
                            mvWindow.WindowStyle = WindowStyle.None;
                            mvWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                            mvWindow.WindowState = WindowState.Maximized;
                            mvWindow.cloud = true;
                            mvWindow.today = today;
                            if (PaperLess_Emeeting.Properties.Settings.Default.AssemblyName.Contains("TPI4F") == true)
                            {
                                //mvWindow.WindowStyle = WindowStyle.SingleBorderWindow;
                            }
                            mvWindow.Show();
                        }
                        
                    }));
                });
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }

            MouseTool.ShowArrow();
        }
    }
}
