using PaperLess_Emeeting.App_Code;
using PaperLess_Emeeting.App_Code.ClickOnce;
using PaperLess_Emeeting.App_Code.DownloadItem;
using PaperLess_Emeeting.App_Code.MessageBox;
using PaperLess_Emeeting.App_Code.ViewModel;
using PaperLess_Emeeting.App_Code.WS;
using PaperLess_ViewModel;
//using ReadPageModule;
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

    //public delegate void LawListCT_HangTheDownloadEvent_Function(string LastLawItemID);
    //public delegate bool LawListCT_IsAllLawRowFinished_AddInitUIFinished_Function();

    //public delegate Dictionary<string, BookVM> LawListCT_GetBookVMs_ByMeetingFileCate_Function(Law_DownloadItemViewModel lawItem);
    /// <summary>
    /// LawRow.xaml 的互動邏輯
    /// </summary>
    public partial class FolderRow : UserControl
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
        public FolderDataFolderListFolder VM;
        public event Home_ChangeBtnSP_Function Home_ChangeBtnSP_Event;
        public event EventHandler<DictionaryEventArgas> dictEvent;

        public FolderRow(string UserID, string UserName, FolderDataFolderListFolder VM, EventHandler<DictionaryEventArgas>  callback)
        {
            InitializeComponent();
            this.UserID = UserID;
            this.UserName = UserName;
            this.VM = VM;
            this.dictEvent = callback;
            this.Loaded += LawRow_Loaded;
        }

        private void LawRow_Loaded(object sender, RoutedEventArgs e)
        {

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
           
            
        }

        private void InitEvent()
        {
          
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
                        //ReadWindow rw = new ReadWindow(cbBooksData, Home_Window.OpenBookFromReader, _bookPath, _bookId, _account
                        //                      , _userName, _email, _meetingId
                        //                      , _watermark, _dbPath, _isSync
                        //                      , _isSyncOwner, _webServiceUrl, _socketMessage, null);
                        //rw.WindowStyle = WindowStyle.None;
                        //rw.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                        //rw.WindowState = WindowState.Maximized;
                        //if (MyPL.Properties.Settings.Default.AssemblyName.Contains("TPI4F") == true)
                        //{
                        //    //rw.WindowStyle = WindowStyle.SingleBorderWindow;
                        //}
                        //rw.Show();
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
           
          
        }

        private void InitUI()
        {
            txtName.Text = VM.Name;
        }

        private void HideAll()
        {
            
        }

        private void ShowNeverDownload(bool fromBtnDelete)
        {
            

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

        }

        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (dictEvent != null)
            {
                Dictionary<string, object> dict = new Dictionary<string, object>() { { "FolderID", VM.ID } };
                dictEvent(this, new DictionaryEventArgas() { dict = dict });
            }
        }
    }
}
