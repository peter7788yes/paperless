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
    /// <summary>
    /// LawListCT.xaml 的互動邏輯
    /// </summary>
    public partial class FolderListCT : UserControl
    {
        LawListCT_DownloadFileStart_Function Start_callback;
        LawListCT_DownloadProgressChanged_Function Progress_callback;
        LawListCT_DownloadFileCompleted_Function Finish_callback;

        LawListCT_UnZip_Function UnZip_callback;
        LawListCT_UnZipError_Function UnZipError_callback;
        LawListCT_GetBookVMs_ByMeetingFileCate_Function GetBookVMs_ByMeetingFileCate_callback;
        public string UserID { get; set; }
        public string UserName { get; set; }
        public string UserPWD { get; set; }
        public string MeetingID { get; set; }
        public int All_FileCount = 0;
        public int Loaded_FileCount = 0;
        public string ButtonName { get; set; }
        public event Home_ChangeBtnSP_Function Home_ChangeBtnSP_Event;
        public bool A2Z = true;
        public event EventHandler<DictionaryEventArgas> dictEvent;
        public FolderListCT(string ButtonName, EventHandler<DictionaryEventArgas> callback)
        {

            this.ButtonName = ButtonName;
            InitializeComponent();
            this.dictEvent = callback;
            this.Loaded += LawListCT_Loaded;
            this.Unloaded += LawListCT_Unloaded;
            this.IsVisibleChanged += LawListCT_IsVisibleChanged;
        }

        private void LawListCT_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // e.NewValue == true 就是顯示Visibile
            // e.NewValue == false 就是隱藏Collapse
            if ((bool)e.NewValue == false)
            {
                Singleton_LawDownloader.GetInstance().ClearLawListCTEvent();
            }
        }

        private void LawListCT_Unloaded(object sender, RoutedEventArgs e)
        {
            Singleton_LawDownloader.GetInstance().ClearLawListCTEvent();
            //LawDownloader lawDownloader = Singleton_LawDownloader.GetInstance();
            //lawDownloader.LawListCT_DownloadFileStart_Event -= Start_callback;
            //lawDownloader.LawListCT_DownloadProgressChanged_Event -= Progress_callback;
            //lawDownloader.LawListCT_DownloadFileCompleted_Event -= Finish_callback;
            //lawDownloader.LawListCT_UnZip_Event -= UnZip_callback;
            //lawDownloader.LawListCT_UnZipError_Event -= UnZipError_callback;
        }

        private void LawListCT_Loaded(object sender, RoutedEventArgs e)
        {
            InitSelectDB();
            // 只要是 CT 主要畫面，優先權設定為Send，因為設定Normal，按鈕的出現會感覺卡卡的。
            //this.Dispatcher.BeginInvoke(DispatcherPriority.Send,new Action(() =>
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

        }


        private void InitSelectDB()
        {
            DataTable dt = MSCE.GetDataTable("select UserID,UserName,UserPWD,MeetingID from NowLogin");
            if (dt.Rows.Count > 0)
            {
                UserID = dt.Rows[0]["UserID"].ToString();
                UserName = dt.Rows[0]["UserName"].ToString();
                UserPWD = dt.Rows[0]["UserPWD"].ToString();
                MeetingID = dt.Rows[0]["MeetingID"].ToString();
            }
        }

        private void LawListCT_UnZipError_Callback(Law_DownloadItemViewModel lawItem)
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle,new Action<Law_DownloadItemViewModel>(LawListCT_UnZipError_Callback), lawItem);
                this.Dispatcher.BeginInvoke(new Action<Law_DownloadItemViewModel>(LawListCT_UnZipError_Callback), lawItem);
            }
            else
            {
                // 在Home的主視窗Show，不要在這裡Show
                //AutoClosingMessageBox.Show("解壓縮失敗");
                //LawRow lawRow = LawRowSP.Children.OfType<LawRow>().Where(x => x.lawDataLaw.ID.Equals(lawItem.ID)).FirstOrDefault();
                //if (lawRow != null)
                //{
                //    Storyboard sb;
                //    if (lawItem.FileType == LawFileType.更新檔解壓縮失敗)
                //    {
                //        sb = (Storyboard)lawRow.TryFindResource("sbUpdate");
                //        if (sb != null)
                //            sb.Stop();

                //        // 記得必須隱藏必須先做，才不會被看見
                //        lawRow.btnUpdate.Visibility = Visibility.Visible;
                //    }
                //    else
                //    {
                //        sb = (Storyboard)lawRow.TryFindResource("sb");
                //        if (sb != null)
                //            sb.Stop();

                //        // 記得必須隱藏必須先做，才不會被看見
                //        lawRow.btnDownload.Visibility = Visibility.Visible;

                //    }


                //}
            }
        }


        private void LawListCT_UnZip_Callback(Law_DownloadItemViewModel lawItem)
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action<Law_DownloadItemViewModel>(LawListCT_UnZip_Callback), lawItem);
                this.Dispatcher.BeginInvoke(new Action<Law_DownloadItemViewModel>(LawListCT_UnZip_Callback), lawItem);
            }
            else
            {

                //LawRow lawRow = LawRowSP.Children.OfType<LawRow>().Where(x => x.lawDataLaw.ID.Equals(lawItem.ID)).FirstOrDefault();
                //if (lawRow != null)
                //{

                //        Storyboard sb;
                //        if (lawItem.FileType == LawFileType.更新檔解壓縮中)
                //        {
                //            //記得不要讓使用者開書
                //            lawRow.txtUpdatePercent.Text = "100 %";
                //            lawRow.pbUpdate.Value = lawRow.pb.Maximum;
                //            lawRow.txtUpdatePercent.Visibility = Visibility.Collapsed;
                //            lawRow.pbUpdate.Visibility = Visibility.Collapsed;

                //            sb = (Storyboard)lawRow.TryFindResource("sbUpdate");
                //            if (sb != null)
                //                sb.Begin();
                //        }
                //        else
                //        {
                //            lawRow.txtPercent.Text = "100 %";
                //            lawRow.pb.Value = lawRow.pb.Maximum;
                //            lawRow.txtPercent.Visibility = Visibility.Collapsed;
                //            lawRow.pb.Visibility = Visibility.Collapsed;

                //            sb = (Storyboard)lawRow.TryFindResource("sb");
                //            if (sb != null)
                //                 sb.Begin();
                //        }

                //}
            }
        }

        private void LawListCT_DownloadFileStart_Callback(Law_DownloadItemViewModel lawItem)
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action<Law_DownloadItemViewModel>(LawListCT_DownloadFileStart_Callback), lawItem);
                this.Dispatcher.BeginInvoke(new Action<Law_DownloadItemViewModel>(LawListCT_DownloadFileStart_Callback), lawItem);
            }
            else
            {
                //LawRow lawRow = LawRowSP.Children.OfType<LawRow>().Where(x => x.lawDataLaw.ID.Equals(lawItem.ID)).FirstOrDefault();
                //if (lawRow !=null )
                //{
                //    lawRow.txtPercent.Text = "0 %";
                //    lawRow.txtPercent.Foreground = Brushes.Black;
                //    lawRow.pb.Value = 0;
                //    lawRow.pb.Foreground = Brushes.Orange;
                //    lawRow.pb.Background = Brushes.Black;
                //}
            }
        }


        private void LawListCT_DownloadFileCompleted_Callback(Law_DownloadItemViewModel lawItem)
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action<Law_DownloadItemViewModel>(LawListCT_DownloadFileCompleted_Callback), lawItem);
                this.Dispatcher.BeginInvoke(new Action<Law_DownloadItemViewModel>(LawListCT_DownloadFileCompleted_Callback), lawItem);
            }
            else
            {
                //LawRow lawRow = LawRowSP.Children.OfType<LawRow>().Where(x => x.lawDataLaw.ID.Equals(lawItem.ID)).FirstOrDefault();

                //if (lawRow != null)
                //{
                //    if (lawItem.FileType == LawFileType.更新檔已下載完成)
                //    {
                //        Storyboard sb = (Storyboard)lawRow.TryFindResource("sb");
                //        if (sb != null)
                //            sb.Stop();

                //        lawRow.txtIsNew.Visibility = Visibility.Visible;

                //    }
                //    else
                //    {
                //        Storyboard sb = (Storyboard)lawRow.TryFindResource("sb");
                //        if (sb != null)
                //            sb.Stop();

                //        lawRow.btnDownload.Visibility = Visibility.Collapsed;
                //        lawRow.btnOpen.Visibility = Visibility.Visible;
                //        lawRow.btnDelete.Visibility = Visibility.Visible;
                //        lawRow.txtIsNew.Visibility = Visibility.Visible;
                //    }


                //}

            }
        }

        private void LawListCT_DownloadProgressChanged_Callback(Law_DownloadItemViewModel lawItem)
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action<Law_DownloadItemViewModel>(LawListCT_DownloadProgressChanged_Callback), lawItem);
                this.Dispatcher.BeginInvoke(new Action<Law_DownloadItemViewModel>(LawListCT_DownloadProgressChanged_Callback), lawItem);
            }
            else
            {
                //LawRow lawRow = LawRowSP.Children.OfType<LawRow>().Where(x => x.lawDataLaw.ID.Equals(lawItem.ID)).FirstOrDefault();
                //if (lawRow != null)
                //{
                //    lawRow.txtPercent.Text = ((int)lawItem.NowPercentage).ToString() +" %";
                //    lawRow.pb.Value = lawItem.NowPercentage;
                //    lawRow.btnDownload.Visibility = Visibility.Collapsed;
                //    lawRow.txtPercent.Visibility = Visibility.Visible;
                //    lawRow.pb.Visibility = Visibility.Visible;
                //}
            }
        }

        private void InitEvent()
        {
            LawDownloader lawDownloader = Singleton_LawDownloader.GetInstance();

            Start_callback += new LawListCT_DownloadFileStart_Function(LawListCT_DownloadFileStart_Callback);
            Progress_callback += new LawListCT_DownloadProgressChanged_Function(LawListCT_DownloadProgressChanged_Callback);
            Finish_callback += new LawListCT_DownloadFileCompleted_Function(LawListCT_DownloadFileCompleted_Callback);
            UnZip_callback += new LawListCT_UnZip_Function(LawListCT_UnZip_Callback);
            UnZipError_callback += new LawListCT_UnZipError_Function(LawListCT_UnZipError_Callback);
            GetBookVMs_ByMeetingFileCate_callback = new LawListCT_GetBookVMs_ByMeetingFileCate_Function(LawListCT_GetBookVMs_ByMeetingFileCate_Callback);

            lawDownloader.LawListCT_DownloadFileStart_Event += Start_callback;
            lawDownloader.LawListCT_DownloadProgressChanged_Event += Progress_callback;
            lawDownloader.LawListCT_DownloadFileCompleted_Event += Finish_callback;
            lawDownloader.LawListCT_UnZip_Event += UnZip_callback;
            lawDownloader.LawListCT_UnZipError_Event += UnZipError_callback;

            //txtKeyword.MouseEnter += (sender, e) => { MouseTool.ShowIBeam(); txtKeyword.Focus(); };
            //txtKeyword.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); }; //Keyboard.ClearFocus();
            //txtKeyword.KeyUp += txtKeyword_KeyUp;
            //txtKeyword.Focus();



        }

        private Dictionary<string, App_Code.ViewModel.BookVM> LawListCT_GetBookVMs_ByMeetingFileCate_Callback(Law_DownloadItemViewModel lawItem)
        {
            Dictionary<string, BookVM> BookVMs = new Dictionary<string, BookVM>();

            //IEnumerable<LawRow> LawRowS = LawRowSP.Children.OfType<LawRow>().Where(x => x.lawItem.FileCate == lawItem.FileCate && x.lawItem.DownloadBytes != 0 && x.lawItem.DownloadBytes >= x.lawItem.TotalBytes);
            //string filePath = ClickOnceTool.GetFilePath();
            //string UnZipFileFolder = MyPL.Properties.Settings.Default.File_UnZipFileFolder;
            //string baseBookPath = filePath + "\\" + UnZipFileFolder + "\\" + UserID;

            //if (LawRowS != null)
            //{
            //    foreach (LawRow item in LawRowS)
            //    {
            //        BookVMs[item.lawItem.Name] = new BookVM(item.lawItem.ID, baseBookPath + "\\" + item.lawItem.ID, lawItem.FileCate);
            //    }
            //}

            return BookVMs;
        }

        private void txtKeyword_KeyUp(object sender, KeyEventArgs e)
        {

            //string keyword = txtKeyword.Text.ToLower().Trim();

            //if (keyword.Equals("") == false)
            //{

            //            foreach (FolderRoom item  in WP.Children.OfType<FolderRoom>())
            //            {
            //                //MessageBox.Show(item.VM.Name);
            //                if (item.VM.Name.ToLower().Contains(keyword) == true )
            //                {
            //                    item.Visibility = Visibility.Visible;
            //                }
            //                else
            //                {
            //                    item.Visibility = Visibility.Collapsed;
            //                }
            //            }


            //            foreach (FolderRow item in WP2.Children.OfType<FolderRow>())
            //            {
            //                if (item.VM.Name.ToLower().Contains(keyword) == true)
            //                {
            //                    item.Visibility = Visibility.Visible;
            //                }
            //                else
            //                {
            //                    item.Visibility = Visibility.Collapsed;
            //                }
            //            }

            //}
            //else
            //{

            //    foreach (FolderRoom item in WP.Children.OfType<FolderRoom>())
            //    {
            //        item.Visibility = Visibility.Visible;
            //    };


            //    foreach (FolderRow item in WP2.Children.OfType<FolderRow>())
            //    {
            //        item.Visibility = Visibility.Visible;
            //    };
            //}
        }

        private void InitUI()
        {
            MouseTool.ShowLoading();
            //LawCT_Title.Text = MyPL.Properties.Settings.Default.LawButtonName;
            txtTitle.Text = ButtonName;
            //Network.HttpRequest hr = new Network.HttpRequest();
            if (1 == 1 || NetworkTool.CheckNetwork() > 0)
            {
                // 非同步POST方法
                //GetLawData.AsyncPOST((ld) => { GetLawData_DoAction(ld); });
                GetUserFolder.AsyncPOST(UserID, UserPWD, (fd) => { GetUserFolder_DoAction(fd); });
                //, () => { this.Dispatcher.BeginInvoke(new Action(() => { AutoClosingMessageBox.Show("無法取得資料，請稍後再試"); })); });
            }
            else
            {
                DataTable dt = MSCE.GetDataTable("select LawJson from LawData");


                if (dt.Rows.Count > 0)
                {
                    LawData ld = JsonConvert.DeserializeObject<LawData>(dt.Rows[0]["LawJson"].ToString());
                    GetLawData_DoAction(ld);
                }
                else
                {
                    AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
                    MouseTool.ShowArrow();
                }

            }





            #region 同步POST
            //LawData lawData = GetLawData.POST();

            //if (lawData != null)
            //{
            //    int i=0;
            //    foreach (LawDataLaw item in lawData.LawList)
            //    {
            //        i++;
            //        bool IsLastRow= (i==lawData.LawList.Length);
            //        LawRowSP.Children.Add(new LawRow(item,IsLastRow, LawListCT_HangTheDownloadEvent_Callback));
            //    }
            //}
            //else
            //{
            //    AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
            //}
            #endregion

        }

        private void GetUserFolder_DoAction(FolderData fd)
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                this.Dispatcher.BeginInvoke(new Action<FolderData>(GetUserFolder_DoAction), fd);
            }
            else
            {
                if (fd != null)
                {
                            txtCount.Text = $"共有 {fd.FolderList.Count}個資料夾";
                            int i = 0;
                            if (fd.FolderList.Folder != null)
                                foreach (FolderDataFolderListFolder item in fd.FolderList.Folder)
                                {

                                    i++;
                                    WP.Children.Add(new FolderRoom(UserID, UserPWD, item, dictEvent,DelCallback,RenameCallback,()=> {
                                        return WP.Children.OfType<FolderRoom>().Select(item2 => item2.VM.Name).ToList();
                                    }));
                                    //WP2.Children.Add(new FolderRow(UserID, UserPWD, item, dictEvent));
                                    //LawRowSP.Children.Add(new LawRow(UserID, UserName, UserPWD
                                    //                                , IsLastRow, item
                                    //                                , LawListCT_HangTheDownloadEvent_Callback
                                    //                                , LawListCT_IsAllLawRowFinished_AddInitUIFinished_Callback
                                    //                                , LawListCT_GetBookVMs_ByMeetingFileCate_Callback));
                                }
                            SaveData(fd);
                }
                else
                {
                    AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
                }
                MouseTool.ShowArrow();
            }
        }

        private void RenameCallback(bool obj)
        {
            if (obj)
                GetUserFolder.AsyncPOST(UserID, UserPWD, (fd) => {
                    this.Dispatcher.BeginInvoke(new Action(() => {
                        WP.Children.Clear();
                        if (fd.FolderList.Folder != null)
                            foreach (FolderDataFolderListFolder item in fd.FolderList.Folder)
                            WP.Children.Add(new FolderRoom(UserID, UserPWD, item, dictEvent, DelCallback, RenameCallback, () => {
                                return WP.Children.OfType<FolderRoom>().Select(item2 => item2.VM.Name).ToList();
                            }));
                      
                    }));
                    SaveData(fd);
                });
        }

        private void GetLawData_DoAction(LawData ld)
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                this.Dispatcher.BeginInvoke(new Action<LawData>(GetLawData_DoAction), ld);
            }
            else
            {
                if (ld != null)
                {
                    Task.Factory.StartNew(() =>
                       {
                           this.Dispatcher.BeginInvoke(new Action(() =>
                               {
                                   int i = 0;
                                   foreach (LawDataLaw item in ld.LawList)
                                   {
                                       i++;
                                       bool IsLastRow = (i == ld.LawList.Length);
                                       //LawRowSP.Children.Add(new LawRow(UserID, UserName, UserPWD
                                       //                                , IsLastRow, item
                                       //                                , LawListCT_HangTheDownloadEvent_Callback
                                       //                                , LawListCT_IsAllLawRowFinished_AddInitUIFinished_Callback
                                       //                                , LawListCT_GetBookVMs_ByMeetingFileCate_Callback));
                                   }
                               }));
                       });

                    DataTable dt = MSCE.GetDataTable("select LawJson from LawData");

                    if (dt.Rows.Count > 0)
                    {
                        MSCE.ExecuteNonQuery(@"UPDATE [LawData] SET [LawJson] = @1"
                                   , JsonConvert.SerializeObject(ld));
                    }
                    else
                    {
                        MSCE.ExecuteNonQuery(@"INSERT INTO [LawData] ([LawJson])
                                                            VALUES (@1)"
                                             , JsonConvert.SerializeObject(ld));
                    }
                }
                else
                {
                    AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
                }
                MouseTool.ShowArrow();
            }

        }

        private bool LawListCT_IsAllLawRowFinished_AddInitUIFinished_Callback()
        {
            return ++Loaded_FileCount == All_FileCount;
        }
        private void LawListCT_HangTheDownloadEvent_Callback(string LastLawItemID)
        {
            // 掛上下載事件
            //FileDownloader fileDownloader = Singleton_FileDownloader.GetInstance(MeetingID);
            //fileDownloader.MeetingDataCT_DownloadFileStart_Event += Start_callback;
            //fileDownloader.MeetingDataCT_DownloadProgressChanged_Event += Progress_callback;
            //fileDownloader.MeetingDataCT_DownloadFileCompleted_Event += Finish_callback;
            //fileDownloader.MeetingDataCT_UnZip_Event += UnZip_callback;
            //fileDownloader.MeetingDataCT_UnZipError_Event += UnZipError_callback;

            // 好了之後把所有FileRow設定成可見
            //LawRowSP.Children.OfType<LawRow>().ToList().ForEach(x =>
            //{
            //    x.Visibility = Visibility.Visible;
            //});
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            WP.Visibility = WP.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            //WP2.Visibility = WP2.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

            //btnChangeGrid.Source = WP.Visibility == Visibility.Visible ? new BitmapImage(new Uri("image/funcIcon_modeThumbnail@2x.png", UriKind.Relative)) : new BitmapImage(new Uri("image/funcIcon_modeList@2x.png", UriKind.Relative));
        }

        private void btnOrder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            List<FolderRoom> list = new List<FolderRoom>();
            List<FolderRow> list2 = new List<FolderRow>();

            foreach (FolderRoom item in WP.Children.OfType<FolderRoom>())
                list.Add(item);

            //foreach (FolderRow item in WP2.Children.OfType<FolderRow>())
            //    list2.Add(item);


            if (A2Z)
            {
                list = list.OrderByDescending((item) => item.VM.Name).ToList();
                list2 = list2.OrderByDescending((item) => item.VM.Name).ToList();
                //btnOrder.Source = new BitmapImage(new Uri("image/funcIcon_sortDescending@2x.png", UriKind.Relative));
            }
            else
            {
                list = list.OrderBy((item) => item.VM.Name).ToList();
                list2 = list2.OrderBy((item) => item.VM.Name).ToList();
                //btnOrder.Source = new BitmapImage(new Uri("image/funcIcon_sortAscending@2x.png", UriKind.Relative));
            }

            A2Z = !A2Z;


            WP.Children.Clear();
            foreach (var item in list)
            {
                //WP.Children.Add(item);
            }

            //WP2.Children.Clear();
            //foreach (var item in list2)
            //{
            //    WP2.Children.Add(item);
            //}


        }

        private void Image_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            var list = WP.Children.OfType<FolderRoom>().Select(item => item.VM.Name).ToList();

            Window Home_Window = App.Current.Windows.OfType<Home>().FirstOrDefault();
            AddFolder win = new AddFolder(UserID, UserPWD, list, Home_Window);

            var success = win.ShowDialog();

            if(success==true)
                GetUserFolder.AsyncPOST(UserID, UserPWD, (fd) => {
                    this.Dispatcher.BeginInvoke(new Action(() => {
                        WP.Children.Clear();
                        if (fd.FolderList.Folder != null)
                            foreach (FolderDataFolderListFolder item in fd.FolderList.Folder)
                                WP.Children.Add(new FolderRoom(UserID, UserPWD, item, dictEvent ,DelCallback, RenameCallback, () => {
                                    return WP.Children.OfType<FolderRoom>().Select(item2 => item2.VM.Name).ToList();
                                }));
                        
                    }));
                    SaveData(fd);
                });

           
        }


        private void DelCallback(bool obj)
        {
            if(obj)
            GetUserFolder.AsyncPOST(UserID, UserPWD, (fd) => {
                this.Dispatcher.BeginInvoke(new Action(() => {
                    WP.Children.Clear();
                    if(fd.FolderList.Folder!=null)
                    foreach (FolderDataFolderListFolder item in fd.FolderList.Folder)
                        WP.Children.Add(new FolderRoom(UserID, UserPWD, item, dictEvent, DelCallback, RenameCallback, () => {
                            return WP.Children.OfType<FolderRoom>().Select(item2 => item2.VM.Name).ToList();
                        }));
                  
                }));
                SaveData(fd);
            });
        }

        private void SaveData(FolderData fd)
        {

            MSCE.ExecuteNonQuery("DELETE FROM [UserFolder] WHERE userid = @1 "
                                  , UserID);

            MSCE.ExecuteNonQuery("DELETE FROM [UserFile] WHERE userid = @1"
                                  , UserID);

            if (fd.FolderList.Folder != null)
            foreach(var item in fd.FolderList.Folder)
            {

                MSCE.ExecuteNonQuery("insert into UserFolder (FolderID,UserID,FolderName) values(@1,@2,@3)"
                                      , item.ID
                                      ,UserID
                                      ,item.Name);
            }
        }
    }
}
