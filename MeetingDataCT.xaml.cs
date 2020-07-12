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
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;

namespace PaperLess_Emeeting
{
    //public delegate void Home_ChangeButton_Function(UserButton[] UserButtonAry, string ActiveButtonID);
    /// <summary>
    /// MeetingDataCT.xaml 的互動邏輯
    /// </summary>
    public partial class MeetingDataCT : UserControl
    {
        DispatcherTimer autoUpdate;
        public event Home_ChangeBtnSP_Function Home_ChangeBtnSP_Event;
        public string UserID { get; set; }
        public string UserName { get; set; }
        public string UserPWD { get; set; }
        public string UserEmail { get; set; }
        MeetingData meetingData { get; set; }
        public bool HasRecordFile { get; set; }
        public bool HasSubjectFile { get; set; }
        public bool Already_RaiseAllDownload { get; set; }

        MeetingDataCT_DownloadFileStart_Function Start_callback;
        MeetingDataCT_DownloadProgressChanged_Function Progress_callback;
        MeetingDataCT_DownloadFileCompleted_Function Finish_callback;
        MeetingDataCT_UnZip_Function UnZip_callback;
        MeetingDataCT_UnZipError_Function UnZipError_callback;
        MeetingDataCT_DownloadError_Function DownloadError_callback;
        MeetingDataCT_GetBookVMs_ByMeetingFileCate_Function GetBookVMs_ByMeetingFileCate_callback;
        MeetingRoomButtonType meetingRoomButtonType;
        MeetingDataCT_Counting_Finished_FileCount_Function MeetingDataCT_Counting_Finished_FileCount_callback;

        public int All_FileCount = 0;
        public int Loaded_FileCount = 0;
        public int Finished_FileCount = 0;
        bool isFirstAutoTurnOnSync = false;
        public  MeetingDataCT(string UserID, string UserName, string UserPWD, string UserEmail,MeetingData meetingData,Home_ChangeBtnSP_Function callback,bool isFirstAutoTurnOnSync)
        {
            MouseTool.ShowLoading();
            InitializeComponent();
            this.UserID = UserID;
            this.UserName = UserName;
            this.UserPWD = UserPWD;
            this.UserEmail = UserEmail;
            this.meetingData = meetingData;
            this.Home_ChangeBtnSP_Event += callback;
            this.Loaded+=MeetingDataCT_Loaded;
            this.Unloaded += MeetingDataCT_Unloaded;
            this.IsVisibleChanged += MeetingDataCT_IsVisibleChanged;
            this.isFirstAutoTurnOnSync = isFirstAutoTurnOnSync;
        }

        private void MeetingDataCT_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // e.NewValue == true 就是顯示Visibile
            // e.NewValue == false 就是隱藏Collapse
            if ((bool)e.NewValue == false)
            {
                Singleton_FileDownloader.GetInstance(meetingData.ID).ClearMeetingDataCTEvent();
            }
                
        }

        private void MeetingDataCT_Unloaded(object sender, RoutedEventArgs e)
        {

            //Singleton_FileDownloader.GetInstance(meetingData.ID).ClearMeetingRoomEvent();
            Singleton_FileDownloader.GetInstance(meetingData.ID).ClearMeetingDataCTEvent();

            //FileDownloader fileDownloader = Singleton_FileDownloader.GetInstance(MeetingID);
            //fileDownloader.MeetingDataCT_DownloadFileStart_Event -= Start_callback;
            //fileDownloader.MeetingDataCT_DownloadProgressChanged_Event -= Progress_callback;
            //fileDownloader.MeetingDataCT_DownloadFileCompleted_Event -= Finish_callback;
            //fileDownloader.MeetingDataCT_UnZip_Event -= UnZip_callback;
            //fileDownloader.MeetingDataCT_UnZipError_Event -= UnZipError_callback;

        }

        private void MeetingDataCT_Loaded(object sender, RoutedEventArgs e)
        {
              Task.Factory.StartNew(() =>
              {
                  InitSelectDB();
                  // 只要是 CT 主要畫面，優先權設定為Send，因為設定Normal，按鈕的出現會感覺卡卡的。
                  //this.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
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
              
              });
        }

        private void InitSelectDB()
        {
            //if (UserID.Equals("") == true || UserName.Equals("") == true || UserPWD.Equals("") == true || UserEmail.Equals("") == true)
            //{
            //    DataTable dt = MSCE.GetDataTable("select UserID,UserName,UserPWD,UserEmail from NowLogin");
            //    if (dt.Rows.Count > 0)
            //    {
            //        UserID = dt.Rows[0]["UserID"].ToString();
            //        UserName = dt.Rows[0]["UserName"].ToString();
            //        UserPWD = dt.Rows[0]["UserPWD"].ToString();
            //        UserEmail = dt.Rows[0]["UserEmail"].ToString();
            //    }
            //}

            DateTime BeginTime = new DateTime(2010, 1, 1);
            DateTime EndTime = new DateTime(2050, 1, 1);
            DateTime.TryParse(meetingData.BeginTime, out BeginTime);
            DateTime.TryParse(meetingData.EndTime, out EndTime);
            if (BeginTime <= new DateTime(2010, 1, 1))
                BeginTime = new DateTime(2010, 1, 1);
            if (EndTime <= new DateTime(2050, 1, 1))
                EndTime = new DateTime(2050, 1, 1);

            //MeetingUserType meetingUserType =MeetingUserType.與會人員;
            //Enum.TryParse<MeetingUserType>(meetingData.LoginResult.LoginState.Type,out meetingUserType);
            string SQL = "update nowlogin set MeetingBeginTime=@1,MeetingEndTime=@2,MeetingUserType=@3";
            int success = MSCE.ExecuteNonQuery(SQL
                                               , BeginTime.ToString("yyyy/MM/dd HH:mm:ss")
                                               , EndTime.ToString("yyyy/MM/dd HH:mm:ss")
                                               , meetingData.LoginResult.LoginState.Type);

            if (success < 1)
            {
                LogTool.Debug(new Exception(@"DB失敗: " + SQL));
                //return;
            }
        }

        private void InitEvent()
        {
            if (PaperLess_Emeeting.Properties.Settings.Default.DetectAutoUpdateOn15Minutes == true)
            {
                autoUpdate = new DispatcherTimer();
                autoUpdate.Interval = TimeSpan.FromSeconds(5);
                autoUpdate.Tick += (sender, e) =>
                    {
                        try
                        {
                            var dt = DateTime.Parse(meetingData.BeginTime).AddMinutes(-15);
                            var ts = DateTime.Now.Subtract(dt);
                            if (ts >= TimeSpan.FromMilliseconds(0) && ts <= TimeSpan.FromMilliseconds(5))
                            {
                                //觸發更新
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    MouseButtonEventArgs args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 100, MouseButton.Left);
                                    args.RoutedEvent = UIElement.MouseLeftButtonDownEvent;
                                    btnAllFileRowsUpdate.RaiseEvent(args);
                                }));
                                autoUpdate.Stop();

                            }

                        }
                        catch
                        {

                        }
                    };
                autoUpdate.Start();
            }

            // 下載事件不能這麼快掛上去，要等全部FileRow產生完成，在掛上去
            // 不然如果在下載中，會阻塞到UI的產生
            FileDownloader fileDownloader = Singleton_FileDownloader.GetInstance(meetingData.ID);
            Start_callback = new MeetingDataCT_DownloadFileStart_Function(MeetingDataCT_DownloadFileStart_Callback);
            Progress_callback = new MeetingDataCT_DownloadProgressChanged_Function(MeetingDataCT_DownloadProgressChanged_Callback);
            Finish_callback = new MeetingDataCT_DownloadFileCompleted_Function(MeetingDataCT_DownloadFileCompleted_Callback);
            UnZip_callback = new MeetingDataCT_UnZip_Function(MeetingDataCT_UnZip_Callback);
            UnZipError_callback = new MeetingDataCT_UnZipError_Function(MeetingDataCT_UnZipError_Callback);
            DownloadError_callback = new MeetingDataCT_DownloadError_Function(MeetingDataCT_DownloadError_Callback);
            GetBookVMs_ByMeetingFileCate_callback = new MeetingDataCT_GetBookVMs_ByMeetingFileCate_Function(MeetingDataCT_GetBookVMs_ByMeetingFileCate_Callback);

            MeetingDataCT_Counting_Finished_FileCount_callback = new MeetingDataCT_Counting_Finished_FileCount_Function(MeetingDataCT_Counting_Finished_FileCount_Callback);

            fileDownloader.MeetingDataCT_DownloadFileStart_Event += Start_callback;
            fileDownloader.MeetingDataCT_DownloadProgressChanged_Event += Progress_callback;
            fileDownloader.MeetingDataCT_DownloadFileCompleted_Event += Finish_callback;
            fileDownloader.MeetingDataCT_UnZip_Event += UnZip_callback;
            fileDownloader.MeetingDataCT_UnZipError_Event += UnZipError_callback;
            fileDownloader.MeetingDataCT_DownloadError_Event += DownloadError_callback;


            txtMeetingName.MouseEnter += (sender,e) => { MouseTool.ShowHand(); };
            txtMeetingName.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            txtMeetingName.MouseLeftButtonDown += txtMeetingName_MouseLeftButtonDown;

            btnRecord.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnRecord.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnRecord.MouseLeftButtonDown += btnRecord_MouseLeftButtonDown;


            txtSubject.MouseEnter += (sender, e) => {
                //if (HasSubjectFile == true)
                    MouseTool.ShowHand(); 
            };
            txtSubject.MouseLeave += (sender, e) =>
            {
                //if (HasSubjectFile == true)
                    MouseTool.ShowArrow();
            };
            txtSubject.MouseLeftButtonDown += txtSubject_MouseLeftButtonDown;

            btnAllFileRowsUpdate.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnAllFileRowsUpdate.MouseLeave += (sender, e) => { MouseTool.ShowHand(); };
            btnAllFileRowsUpdate.MouseLeftButtonDown += btnAllFileRowsUpdate_MouseLeftButtonDown;

            btnSeries.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnSeries.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnSeries.MouseLeftButtonDown += btnSeries_MouseLeftButtonDown;

            Home Home = Application.Current.Windows.OfType<Home>().First();
            //double Height = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height;
            //double Width = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width;

            int devicePixelWidth = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width;
            int devicePixelHeight = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height;

            //double Height = this.ActualHeight;
            //double Width = this.ActualWidth;

            double Height = Home.ActualHeight;
            double Width = Home.ActualWidth;
            GridLength g2;
            if (Width >= 1920)
            {
                LeftDP.Margin = new Thickness(80, 120, 25, 40);
                g2 = new GridLength(1.38, GridUnitType.Star);
                C1.Width = g2;
                g2 = new GridLength(1.05, GridUnitType.Star);
                C2.Width = g2;
                FileGrid.Margin = new Thickness(10, 50, 40, 45);
            }
            else if (devicePixelWidth == 2160 && devicePixelHeight ==1440 && Width >= 960)
            {
                LeftDP.Margin = new Thickness(100, 140, 25, 40);
                g2 = new GridLength(1.38, GridUnitType.Star);
                C1.Width = g2;
                g2 = new GridLength(1.05, GridUnitType.Star);
                C2.Width = g2;
                FileGrid.Margin = new Thickness(10, 50, 40, 45);
            }
            else
            {
                LeftDP.Margin = new Thickness(60, 80, 5, 40);
                g2 = new GridLength(1.38, GridUnitType.Star);
                C1.Width = g2;
                g2 = new GridLength(1.1, GridUnitType.Star);
                C2.Width = g2;
                FileGrid.Margin = new Thickness(20, 30, 30, 30);
            }

            this.SizeChanged += (sender, e) =>
                {
                    double Height2 = this.ActualHeight;
                    double Width2 = this.ActualWidth;
                    if (Width2 >= 1920)
                    {
                        LeftDP.Margin = new Thickness(80, 120, 25, 40);
                        g2 = new GridLength(1.38, GridUnitType.Star);
                        C1.Width = g2;
                        g2 = new GridLength(1.05, GridUnitType.Star);
                        FileGrid.Margin = new Thickness(30, 50, 40, 45);
                    }
                    else if (devicePixelWidth == 2160 && devicePixelHeight == 1440 && Width >= 960)
                    {
                        LeftDP.Margin = new Thickness(100, 140, 25, 40);
                        g2 = new GridLength(1.38, GridUnitType.Star);
                        C1.Width = g2;
                        g2 = new GridLength(1.05, GridUnitType.Star);
                        C2.Width = g2;
                        FileGrid.Margin = new Thickness(10, 50, 40, 45);
                    }
                    else
                    {
                        LeftDP.Margin = new Thickness(60, 80, 5, 40);
                        g2 = new GridLength(1.38, GridUnitType.Star);
                        C1.Width = g2;
                        g2 = new GridLength(1.1, GridUnitType.Star);
                        C2.Width = g2;
                        FileGrid.Margin = new Thickness(20, 30, 30, 30);
                    }
                };
        }

        private void MeetingDataCT_Counting_Finished_FileCount_Callback()
        {
            if (this.Dispatcher.CheckAccess() == false)
            {
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action(MeetingDataCT_Counting_Finished_FileCount_Callback));
                this.Dispatcher.BeginInvoke(new Action(MeetingDataCT_Counting_Finished_FileCount_Callback));
                
            }
            else
            {
                if (PaperLess_Emeeting.Properties.Settings.Default.HasAutoTodaySync == false)
                    return;

                ++Finished_FileCount;

                if (Finished_FileCount == All_FileCount && this.isFirstAutoTurnOnSync==true)
                {
                    Home home=Application.Current.Windows.OfType<Home>().First();
                    if (home != null && home.IsInSync == false)
                    {
                        IEnumerable<MenuButton> mbs = home.btnSP.Children.OfType<MenuButton>().Where(x => x.userButton.ID.Equals("BtnSync"));

                        if (mbs != null)
                        {
                            MenuButton mb =null;
                            try
                            {
                                mb = mbs.First();
                            }
                            catch(Exception ex)
                            {
                                LogTool.Debug(ex);
                            }

                            if (mb != null)
                            {
                                //Task.Factory.StartNew(() =>
                                //    {
                                //        Thread.Sleep(1000);
                                //        this.Dispatcher.BeginInvoke(new Action(() =>
                                //            {
                                //syncButton.RaiseEvent(new RoutedEventArgs(ToggleButton.ClickEvent));
                                //if (mb.btnImg.Source.ToString().Equals("pack://application:,,,/PaperLess_Emeeting_EDU;component/images/status-onair-off@2x.png") == true)
                                if (mb.btnImg.Source.ToString().Contains("status-onair-off@2x.png") == true)
                                {
                                    Task.Factory.StartNew(() =>
                                        {
                                            Thread.Sleep(1000);
                                            this.Dispatcher.BeginInvoke(new Action(()=>
                                                {
                                                    MouseButtonEventArgs args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 100, MouseButton.Left);
                                                    args.RoutedEvent = UIElement.MouseLeftButtonDownEvent;
                                                    mb.RaiseEvent(args);
                                                }));
                                        });
                                }
                                //mb.RaiseEvent(new MouseButtonEventArgs(UIElement.MouseLeftButtonDownEvent));
                                //             }));
                                //     });
                            }

                        }
                    }
                }
            }

        }

        private void txtSubject_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //if (HasSubjectFile == false)
            //    return;
            SetColorBlackOn_TxtName_TxtRecord_TxtSubjectOnGray_AgendaRow();
            MeetingDataCT_ShowAgendaFile_Callback("", "", true);
            txtSubject.Foreground = ColorTool.HexColorToBrush("#0093b0");
        }

        private Dictionary<string,BookVM> MeetingDataCT_GetBookVMs_ByMeetingFileCate_Callback(File_DownloadItemViewModel fileItem)
        {
            Dictionary<string, BookVM> BookVMs = new Dictionary<string, BookVM>();

            // 塞選影片或書籍
            // (特定分類)x.fileItem.FileCate == fileItem.FileCate
            // (特定議程附檔)x.Visibility == Visibility.Visible
            //IEnumerable<FileRow> FileRowS = FileRowSP.Children.OfType<FileRow>().Where(x => x.Visibility == Visibility.Visible && x.fileItem.DownloadBytes != 0 && x.fileItem.DownloadBytes >= x.fileItem.TotalBytes);
            //AgendaRow AgendaRow_First = AgendaRowSP.Children.OfType<AgendaRow>().Where(x => x.meetingDataAgenda.ID.Equals(fileItem.AgendaID)).FirstOrDefault();
            //string ParentAgendaID="";
            List<File_DownloadItemViewModel> FileItemS = new List<File_DownloadItemViewModel>();
            string childernIDsString = "";
            List<File_DownloadItemViewModel> FileItemS_FromFilter = new List<File_DownloadItemViewModel>();

            //挑選議程附件
            if (PaperLess_Emeeting.Properties.Settings.Default.GetBookVMs_ByMeetingFileCate_ByAgenda==false)
            {
                FileItemS = FileRowSP.Children.OfType<FileRow>().Select(x => x.fileItem).Where(x => x!=null && x.DownloadBytes != 0 && x.DownloadBytes >= x.TotalBytes).ToList();
                if (FileItemS == null)
                    FileItemS = new List<File_DownloadItemViewModel>();
            }
            else
            {
                if (fileItem.AgendaID != null && fileItem.AgendaID.Equals("record") == false && fileItem.AgendaID.Equals("") == false && fileItem.AgendaID.Equals("c") == false && fileItem.AgendaID.Equals("i") == false)
                {
                    childernIDsString = FindChildernIDsString(fileItem.AgendaID);
                    FileItemS_FromFilter = FileRowSP.Children.OfType<FileRow>().Select(x => x.fileItem).Where(x => x.DownloadBytes != 0 && x.DownloadBytes >= x.TotalBytes && childernIDsString.Contains("," + x.AgendaID + ",") == true).ToList();
                }
                else if (fileItem.AgendaID.Equals("record") == true)
                {
                    FileItemS_FromFilter = FileRowSP.Children.OfType<FileRow>().Select(x => x.fileItem).Where(x => x.DownloadBytes != 0 && x.DownloadBytes >= x.TotalBytes && x.AgendaID.Equals("record") == true).ToList();
                }
                else
                {
                    FileItemS_FromFilter = FileRowSP.Children.OfType<FileRow>().Select(x => x.fileItem).Where(x => x.DownloadBytes != 0 && x.DownloadBytes >= x.TotalBytes && (x.AgendaID.Equals("") || x.AgendaID.Equals("c") || x.AgendaID.Equals("i")) == true).ToList();
                }

                FileItemS.AddRange(FileItemS_FromFilter);
            }

          
            

            string filePath = ClickOnceTool.GetFilePath();
            string UnZipFileFolder = PaperLess_Emeeting.Properties.Settings.Default.File_UnZipFileFolder;
            string baseBookPath = filePath + "\\" + UnZipFileFolder + "\\" + UserID + "\\" + meetingData.ID;

            foreach (File_DownloadItemViewModel item in FileItemS)
            {
                DataTable dt = MSCE.GetDataTable("SELECT FinishedFileVersion FROM FileRow where ID=@1 and UserID=@2 and MeetingID=@3"
                             , fileItem.ID
                             , UserID
                             , meetingData.ID);

                string _bookPath = baseBookPath + "\\" + item.ID;
                string FinishedFileVersion = "1";
                if (dt.Rows.Count > 0)
                {
                    FinishedFileVersion = dt.Rows[0]["FinishedFileVersion"].ToString();
                }
                BookVMs[item.FileName] = new BookVM(item.ID, baseBookPath + "\\" + item.ID + "\\" + FinishedFileVersion, item.FileCate);
            }
            return BookVMs;
        }

        // 遞迴尋找所有ID
        private string FindChildernIDsString(string fileItem_AgendaID)
        {
            string childernIDsString = "";
            try
            {
                AgendaRow AgendaRow_First = AgendaRowSP.Children.OfType<AgendaRow>().Where(x => x.meetingDataAgenda.ID.Equals(fileItem_AgendaID)).FirstOrDefault();

                if (AgendaRow_First != null)
                {
                    if (AgendaRow_First.IsParent == true )
                    {
                        childernIDsString = GetChildernAgendaIDs(fileItem_AgendaID, AgendaFilter.顯示父議題和子議題附件);
                    }
                    else
                    {
                        childernIDsString = FindChildernIDsString(AgendaRow_First.meetingDataAgenda.ParentID);
                    }

                }
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }
            return childernIDsString;
        }

        private void btnSeries_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Home Home_Window = Application.Current.Windows.OfType<Home>().FirstOrDefault();
            Home_Window.ShowBtnSeriesCT(meetingData.SeriesMeetingID);

            
        }

        private void MeetingDataCT_DownloadError_Callback(File_DownloadItemViewModel fileItem)
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action<File_DownloadItemViewModel>(MeetingDataCT_UnZipError_Callback), fileItem);
                this.Dispatcher.BeginInvoke(new Action<File_DownloadItemViewModel>(MeetingDataCT_UnZipError_Callback), fileItem);
            }
            else
            {
                // 在Home的主視窗Show，不要在這裡Show
                //AutoClosingMessageBox.Show("解壓縮失敗");
                FileRow fileRow = FileRowSP.Children.OfType<FileRow>().Where(x => x.meetingDataDownloadFileFile.ID.Equals(fileItem.ID)).FirstOrDefault();
                if (fileRow != null)
                {
                    fileRow.txtPercent.Visibility = Visibility.Collapsed;
                    fileRow.pb.Visibility = Visibility.Collapsed;
                    fileRow.btnPause.Visibility = Visibility.Collapsed;
                    fileRow.btnPausing.Visibility = Visibility.Collapsed;
                    // 記得必須隱藏必須先做，才不會被看見
                    fileRow.btnDownload.Visibility = Visibility.Visible;

                }
            }
        }

        private void btnRecord_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetColorBlackOn_TxtName_TxtRecord_TxtSubjectOnGray_AgendaRow();
            MeetingDataCT_ShowAgendaFile_Callback("record", "record", true);
            txtRecord.Foreground = ColorTool.HexColorToBrush("#0093b0");
          
        }

        private void btnAllFileRowsUpdate_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            List<File_DownloadItemViewModel> list =new List<File_DownloadItemViewModel>();
            //FileRowSP.Children.OfType<FileRow>().ToList().ForEach(x =>
            foreach (FileRow x in FileRowSP.Children.OfType<FileRow>())
            {
                if (x.fileItem.DownloadBytes == 0 || x.fileItem.DownloadBytes < x.fileItem.TotalBytes)
                {
                    // 記得必須隱藏得先做，才不會被看見
                    x.btnOpen.Visibility = Visibility.Collapsed;
                    x.btnDownload.Visibility = Visibility.Collapsed;
                    x.btnPausing.Visibility = Visibility.Collapsed;
                    x.txtPercent.Text = "等待中";
                    x.txtPercent.Foreground = Brushes.Gray;
                    x.txtPercent.Visibility = Visibility.Visible;
                    x.pb.Foreground = Brushes.Wheat;
                    x.pb.Background = Brushes.Gray;
                    x.pb.Value = x.fileItem.NowPercentage;
                    x.pb.Visibility = Visibility.Visible;
                    x.btnPause.Visibility = Visibility.Visible;

                    list.Add(x.fileItem);
                }

                #region 過時
                //MouseButtonEventArgs args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 100, MouseButton.Left);
                //args.RoutedEvent = UIElement.MouseLeftButtonDownEvent;
                //x.btnDownload.RaiseEvent(args);
                #endregion
            }
            //});

            //List<File_DownloadItemViewModel> list = FileRowSP.Children.OfType<FileRow>().Select(x => x.fileItem).Where(x => x.DownloadBytes == 0 || x.DownloadBytes < x.TotalBytes).ToList();
            
            //ThreadPool.QueueUserWorkItem(callback =>
            Task.Factory.StartNew(() =>
            {
                FileDownloader fileDownloader = Singleton_FileDownloader.GetInstance(meetingData.ID);
                fileDownloader.ClearMeetingDataCTEvent();

                fileDownloader.Stop();
                // 停止之後不能馬上開始。
                AutoClosingMessageBox.Show("更新檢查中");
               
                fileDownloader.AddItem(list);

                fileDownloader.ClearMeetingDataCTEvent();
                fileDownloader.MeetingDataCT_DownloadFileStart_Event += Start_callback;
                fileDownloader.MeetingDataCT_DownloadProgressChanged_Event += Progress_callback;
                fileDownloader.MeetingDataCT_DownloadFileCompleted_Event += Finish_callback;
            });
        }

        private void MeetingDataCT_UnZipError_Callback(File_DownloadItemViewModel fileItem)
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle,new Action<File_DownloadItemViewModel>(MeetingDataCT_UnZipError_Callback), fileItem);
                this.Dispatcher.BeginInvoke(new Action<File_DownloadItemViewModel>(MeetingDataCT_UnZipError_Callback), fileItem);
            }
            else
            {
                // 在Home的主視窗Show，不要在這裡Show
                //AutoClosingMessageBox.Show("解壓縮失敗");
                FileRow fileRow = FileRowSP.Children.OfType<FileRow>().Where(x => x.meetingDataDownloadFileFile.ID.Equals(fileItem.ID)).FirstOrDefault();
                if (fileRow != null)
                {
                        Storyboard sb = (Storyboard)fileRow.TryFindResource("sb");
                        if (sb != null)
                            sb.Stop();

                        fileRow.txtPercent.Visibility = Visibility.Collapsed;
                        fileRow.pb.Visibility = Visibility.Collapsed;
                        fileRow.btnPause.Visibility = Visibility.Collapsed;
                        fileRow.btnPausing.Visibility = Visibility.Collapsed;
                        // 記得必須隱藏必須先做，才不會被看見
                        fileRow.btnDownload.Visibility = Visibility.Visible;
                   
                }
            }
        }

        private void MeetingDataCT_UnZip_Callback(File_DownloadItemViewModel fileItem)
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為SystemIdle 列舉值為 1; //ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action<File_DownloadItemViewModel>(MeetingDataCT_UnZip_Callback), fileItem);
                this.Dispatcher.BeginInvoke(new Action<File_DownloadItemViewModel>(MeetingDataCT_UnZip_Callback), fileItem);
            }
            else
            {
                FileRow fileRow = FileRowSP.Children.OfType<FileRow>().Where(x => x.meetingDataDownloadFileFile.ID.Equals(fileItem.ID)).FirstOrDefault();
                if (fileRow != null)
                {
                    Storyboard sb = (Storyboard)fileRow.TryFindResource("sb");
                    if (sb != null)
                    {
                        fileRow.txtPercent.Visibility = Visibility.Collapsed;
                        fileRow.pb.Visibility = Visibility.Collapsed;
                        fileRow.btnPause.Visibility = Visibility.Collapsed;
                        fileRow.btnPausing.Visibility = Visibility.Collapsed;
                        fileRow.btnDownload.Visibility = Visibility.Collapsed;
                        sb.Begin();
                    }

                }
            }
        }

        private void MeetingDataCT_DownloadFileCompleted_Callback(File_DownloadItemViewModel fileItem)
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle,new Action<File_DownloadItemViewModel>(MeetingDataCT_DownloadFileCompleted_Callback), fileItem);
                this.Dispatcher.BeginInvoke(new Action<File_DownloadItemViewModel>(MeetingDataCT_DownloadFileCompleted_Callback), fileItem);
            }
            else
            {
                FileRow fileRow = FileRowSP.Children.OfType<FileRow>().Where(x => x.meetingDataDownloadFileFile.ID.Equals(fileItem.ID)).FirstOrDefault();

                if (fileRow != null)
                {
                    Storyboard sb = (Storyboard)fileRow.TryFindResource("sb");
                    if (sb != null)
                        sb.Stop();

                    fileRow.txtPercent.Visibility = Visibility.Collapsed;
                    fileRow.pb.Visibility = Visibility.Collapsed;
                    fileRow.btnPause.Visibility = Visibility.Collapsed;
                    fileRow.btnPausing.Visibility = Visibility.Collapsed;
                    fileRow.btnDownload.Visibility = Visibility.Collapsed;
                    fileRow.btnUpdate.Visibility = Visibility.Collapsed;
                    switch(fileItem.FileCate)
                    {
                        case MeetingFileCate.電子書:
                        case MeetingFileCate.Html5投影片:
                            fileRow.btnOpen.Visibility = Visibility.Visible;
                            break;
                        case MeetingFileCate.影片檔:
                            fileRow.btnOpen.Visibility = Visibility.Visible;
                            break;
                  
                    }

                    if (PaperLess_Emeeting.Properties.Settings.Default.HasSyncCenterModule == true)
                        fileRow.InitSyncCenter(System.IO.Path.Combine(ClickOnceTool.GetDataPath(), PaperLess_Emeeting.Properties.Settings.Default.bookInfo_Path),fileRow.fileItem.ID,fileRow.UserID,fileRow.MeetingID);

                    MeetingDataCT_Counting_Finished_FileCount_Callback();

                }

            }
        }

      

        private void MeetingDataCT_DownloadProgressChanged_Callback(File_DownloadItemViewModel fileItem)
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle,new Action<File_DownloadItemViewModel>(MeetingDataCT_DownloadProgressChanged_Callback), fileItem);
                this.Dispatcher.BeginInvoke(new Action<File_DownloadItemViewModel>(MeetingDataCT_DownloadProgressChanged_Callback), fileItem);
            }
            else
            {
                FileRow fileRow = FileRowSP.Children.OfType<FileRow>().Where(x => x.meetingDataDownloadFileFile.ID.Equals(fileItem.ID)).FirstOrDefault();
                if (fileRow != null)
                {
                    fileRow.btnDownload.Visibility = Visibility.Collapsed;
                    fileRow.btnPausing.Visibility = Visibility.Collapsed;
                    fileRow.txtPercent.Text = ((int)fileItem.NowPercentage).ToString() + " %";
                    fileRow.txtPercent.Foreground = Brushes.Black;
                    fileRow.txtPercent.Visibility = Visibility.Visible;
                    fileRow.pb.Value = fileItem.NowPercentage;
                    fileRow.pb.Foreground = Brushes.Orange;
                    fileRow.pb.Background = Brushes.Black;
                    fileRow.pb.Visibility = Visibility.Visible;
                    fileRow.btnPause.Visibility = Visibility.Visible;
                }
            }
        }

        private void MeetingDataCT_DownloadFileStart_Callback(File_DownloadItemViewModel fileItem)
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle,new Action<File_DownloadItemViewModel>(MeetingDataCT_DownloadFileStart_Callback), fileItem);
                this.Dispatcher.BeginInvoke(new Action<File_DownloadItemViewModel>(MeetingDataCT_DownloadFileStart_Callback), fileItem);
            }
            else
            {
                FileRow fileRow = FileRowSP.Children.OfType<FileRow>().Where(x => x.meetingDataDownloadFileFile.ID.Equals(fileItem.ID)).FirstOrDefault();
                if (fileRow != null)
                {
                    fileRow.btnDownload.Visibility = Visibility.Collapsed;
                    fileRow.btnOpen.Visibility = Visibility.Collapsed;
                    fileRow.btnPausing.Visibility = Visibility.Collapsed;
                    FileDownloader fileDownloader = Singleton_FileDownloader.GetInstance(meetingData.ID);
                    File_DownloadItemViewModel _InListFileItem = fileDownloader.GetInList(meetingData.ID);
                    if (_InListFileItem != null)
                        fileRow.txtPercent.Text = ((int)_InListFileItem.NowPercentage).ToString();
                    fileRow.txtPercent.Foreground = Brushes.Black;
                    fileRow.pb.Foreground = Brushes.Orange;
                    fileRow.pb.Background = Brushes.Black;
                    fileRow.txtPercent.Visibility = Visibility.Visible;
                    fileRow.pb.Visibility = Visibility.Visible;
                    fileRow.btnPause.Visibility = Visibility.Visible;
                }
            }
        }

        private void txtMeetingName_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetColorBlackOn_TxtName_TxtRecord_TxtSubjectOnGray_AgendaRow();
            MeetingDataCT_ShowAgendaFile_Callback("All", "", true);
            txtMeetingName.Foreground = ColorTool.HexColorToBrush("#0093b0");
        }

        private void InitUI()
        {
            string CourseOrMeeting_String = PaperLess_Emeeting.Properties.Settings.Default.CourseOrMeeting_String;
            txtRecord.Text = string.Format("{0}記錄", CourseOrMeeting_String);
            txtData.Text = string.Format("{0}資料", CourseOrMeeting_String);
            txtRecord.Visibility = Visibility.Visible;
            txtData.Visibility = Visibility.Visible;

            // 非同步POST方法
            if (meetingData == null)
            {
                // this.Dispatcher.BeginInvoke寫在這裡
                // 比寫在GetMeetingData_DoAction效能好
                MouseTool.ShowLoading();
                GetMeetingData.AsyncPOST(meetingData.ID, UserID, UserPWD, (md) => { GetMeetingData_DoAction(md); });
                       //, (md) => { this.Dispatcher.BeginInvoke(new Action<MeetingData>(GetMeetingData_DoAction), md); });
                if (NetworkTool.CheckNetwork() > 0)
                {
                    GetMeetingData.AsyncPOST(meetingData.ID, UserID, UserPWD, (md) => { GetMeetingData_DoAction(md); });
                }
                else
                {
                    //DB查詢登入
                    DataTable dt = MSCE.GetDataTable("select MeetingJson from MeetingData where MeetingID=@1 and UserID =@2"
                                                    , meetingData.ID
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
            else
            {
                GetMeetingData_DoAction(meetingData);
            }
            //, () => { this.Dispatcher.BeginInvoke(new Action(() => { AutoClosingMessageBox.Show("無法取得資料，請稍後再試"); })); });

            #region 同步POST
            ////要資料
            //meetingData = GetMeetingData.POST(MeetingID, UserID, UserPWD);


            //// 請記得檔案放在議程後面
            //if (meetingData != null)
            //{

            //    // (1)更新按鈕列表
            //    List<UserButton> list = new List<UserButton>();
            //    foreach (var item in meetingData.LoginResult.EnableButtonList)
            //    {
            //        UserButton ub = new UserButton();
            //        ub.ID = item.ID;
            //        ub.Name = item.Name;
            //        list.Add(ub);
            //    }
            //    UserButton[] UserBtnAry = list.ToArray();
            //    Home_ChangeBtnSP_Event(UserBtnAry, "BtnMeeting");

            //    // (2)會議名稱
            //    txtMeetingName.Text = meetingData.Name;


            //    // (3)加入檔案，改成7了

            //    // (4) 是否有會議記錄，
            //    // if(meetingData.MeetingsFile.FileList)

            //    // (5) 會議副標題，
            //    if (meetingData.Subject.Trim().Equals("") == true)
            //        txtSubject.Visibility = Visibility.Collapsed;
            //    else
            //        txtSubject.Text = meetingData.Subject;


            //    // (6)產生議程
            //    var Files_AgendaIDs = meetingData.DownloadFile.DownloadFileList.Select(x => x.AgendaID).ToList();
            //    var ParentIDs = meetingData.AgendaList.Select(x => x.ParentID).ToList();
            //    foreach (MeetingDataAgenda item in meetingData.AgendaList)
            //    {
            //        bool IsHasFile = Files_AgendaIDs.Contains(item.ID);
            //        bool IsHasChildren = ParentIDs.Contains(item.ID);
            //        bool IsParent = item.ParentID.Equals("c0") || item.ParentID.Equals("i0");
            //        AgendaRowSP.Children.Add(new AgendaRow(IsHasFile, IsHasChildren, IsParent, item
            //                                            , MeetingDataCT_ShowAgendaFile_Callback
            //                                            , MeetingDataCT_GetAgendaInwWorkCount_Callback));
            //    }


            //    // (7)加入檔案
            //    int i = 0;
            //    foreach (MeetingDataDownloadFileFile item in meetingData.DownloadFile.DownloadFileList)
            //    {

            //        i++;
            //        bool IsLastRow = (i == meetingData.DownloadFile.DownloadFileList.Length);
            //        FileRowSP.Children.Add(new FileRow(i, IsLastRow, item, MeetingDataCT_FirstTimeDownload_Callback, MeetingDataCT_HangTheDownloadEvent_Callback));
            //    }

            //}
            //else
            //{
            //    AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
            //}
            #endregion

            MSCE.ExecuteNonQuery("DELETE FROM [UserFolder] WHERE userid = @1 "
                             , UserID);
            MSCE.ExecuteNonQuery("DELETE FROM [UserFile] WHERE userid=@1"
                                 , UserID);



            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (sender3, e3) =>
            {

                try
                {
                    string url = WsTool.GetUrl();
                    string xmlData = "<?xml version=\"1.0\"?><UserInfo><MeetingID>{0}</MeetingID><UserID><![CDATA[{1}]]></UserID><UserPW><![CDATA[{2}]]></UserPW></UserInfo>";
                    xmlData = string.Format(xmlData, meetingData.ID, UserID, UserPWD);
                    Dictionary<string, string> postData = new Dictionary<string, string>();
                    postData["XmlDoc"] = xmlData;
                    HttpWebRequest request = HttpTool.GetHttpWebRequest(url + "/MeetingData", "POST", postData);

                    HttpTool.DoWithResponse(request, (response) =>
                    {
                        //這裡面是不同的執行序;
                        MeetingData md = null;

                        try
                        {
                            //Wayne 20150423 modify
                            //string data = new StreamReader(response.GetResponseStream()).ReadToEnd();
                            string data = "";
                            using (Stream stream = response.GetResponseStream())
                            {
                                using (StreamReader sr = new StreamReader(stream, System.Text.Encoding.UTF8))
                                {
                                    data = sr.ReadToEnd();
                                }
                            }
                            if (data.Equals("") == false)
                            {
                                if (nowXML.Length == 0)
                                    nowXML = data;
                                else
                                {
                                    if (!nowXML.Equals(data))
                                    {
                                        Home Home_Window = Application.Current.Windows.OfType<Home>().FirstOrDefault();
                                        Home_Window.Change2MeetingDataCT(meetingData.ID, null);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                        }

                    }, () => {  });
                }
                catch (Exception ex)
                {
                }
                //try
                //{
                //    string url = WsTool.GetUrl();
                //    string xmlData = "<?xml version=\"1.0\"?><UserInfo><MeetingID>{0}</MeetingID><UserID><![CDATA[{1}]]></UserID><UserPW><![CDATA[{2}]]></UserPW></UserInfo>";
                //    xmlData = string.Format(xmlData, meetingData.ID, UserID, UserPWD);
                //    Dictionary<string, string> postData = new Dictionary<string, string>();
                //    postData["XmlDoc"] = xmlData;
                //    string data = HttpTool.CreateRequest(url + "/MeetingData", "POST", postData);
                  
                //}
                //catch (Exception ex)
                //{
                //    LogTool.Debug(ex);
                //}

            };
            timer.Start();
        }

        public string nowXML = "";
        public Action refresh;
        private void GetMeetingData_DoAction(MeetingData md)
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                this.Dispatcher.BeginInvoke(new Action<MeetingData>(GetMeetingData_DoAction), md);
            }
            else
            {
                meetingData = md;
                if (meetingData != null)
                {
                    // 寫入IP範圍
                    string SQL ="update NowLogin set AllowIpRange=@1 ";
                    int success = MSCE.ExecuteNonQuery(SQL, meetingData.IP);

                    if (success < 1)
                    {
                        LogTool.Debug(new Exception(@"DB失敗: " + SQL));
                        return;
                    }
                    // (1)更新按鈕列表
                    List<UserButton> list = new List<UserButton>();
                    foreach (MeetingDataLoginResultButton item in meetingData.LoginResult.EnableButtonList)
                    {
                        UserButton ub = new UserButton();
                        ub.ID = item.ID;
                        ub.Name = item.Name;
                        list.Add(ub);
                    }
                    UserButton[] UserBtnAry = list.ToArray();
                    Home_ChangeBtnSP_Event(UserBtnAry, "BtnMeeting");
                    //Task.Factory.StartNew(() =>
                    //{
                    //    this.Dispatcher.BeginInvoke(new Action(() =>
                    //    {
                    //        Home_ChangeBtnSP_Event(UserBtnAry, "BtnMeeting");
                    //    }));
                    //});

                    // (2)會議名稱
                    txtMeetingName.Text = meetingData.Name;

                    if (meetingData.SeriesMeetingID!=null && meetingData.SeriesMeetingID.Equals("") == false)
                        btnSeries.Visibility = Visibility.Visible;

                    // (3)加入檔案，改成7了

                    // (4) 是否有會議記錄，
                    // if(meetingData.MeetingsFile.FileList)

                    // (5) 會議副標題，
                    if (meetingData.Subject.Trim().Equals("") == true)
                        txtSubject.Visibility = Visibility.Collapsed;
                    else
                        txtSubject.Text = meetingData.Subject;


                    Task.Factory.StartNew(() =>
                    {

                        var Files_AgendaIDs = meetingData.DownloadFile == null ? new List<string>() : meetingData.DownloadFile.DownloadFileList.Select(x => x.AgendaID).ToList();
                        var ParentIDs = meetingData.AgendaList == null ? new List<string>() : meetingData.AgendaList.Select(x => x.ParentID).ToList();

                        //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action(() =>
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            // (6)產生議程
                            foreach (MeetingDataAgenda item in meetingData.AgendaList)
                            {
                                bool IsHasFile = Files_AgendaIDs.Contains(item.ID);
                                bool IsHasChildren = ParentIDs.Contains(item.ID);
                                bool IsParent = item.ParentID.Equals("0") || item.ParentID.Equals("c0") || item.ParentID.Equals("i0");
                                AgendaRowSP.Children.Add(new AgendaRow(meetingData.ID, UserID, IsHasFile, IsHasChildren, IsParent, item
                                                                        , MeetingDataCT_ShowAgendaFile_Callback
                                                                        , MeetingDataCT_GetAgendaInwWorkCount_Callback));

                            }
                        }));
                    });



                    string isDownload = "N";
                    string isBrowserd = "N";

                    DateTime DownloadTime_BeginTime = DateTime.MinValue;
                    DateTime DownloadTime_EndTime = DateTime.MaxValue;
                    DateTime BrowseTime_BeginTime = DateTime.MinValue;
                    DateTime BrowseTime_EndTime = DateTime.MaxValue;

                    if (meetingData.DownloadFile.DownloadTime.BeginTime.Equals("") == false)
                        DateTime.TryParse(meetingData.DownloadFile.DownloadTime.BeginTime, out DownloadTime_BeginTime);
                    if (meetingData.DownloadFile.DownloadTime.EndTime.Equals("") == false)
                        DateTime.TryParse(meetingData.DownloadFile.DownloadTime.EndTime, out DownloadTime_EndTime);
                    if (meetingData.DownloadFile.BrowseTime.BeginTime.Equals("") == false)
                        DateTime.TryParse(meetingData.DownloadFile.BrowseTime.BeginTime, out BrowseTime_BeginTime);
                    if (meetingData.DownloadFile.BrowseTime.BeginTime.Equals("") == false)
                        DateTime.TryParse(meetingData.DownloadFile.BrowseTime.EndTime, out BrowseTime_EndTime);

                    // 在下載時間之內
                    if (DownloadTime_BeginTime <= DateTime.Now && DateTime.Now < DownloadTime_EndTime)
                    {
                        isDownload = "Y";
                    }
                    else if (DateTime.Now > DownloadTime_EndTime)
                    {
                        isDownload = "O";
                    }

                    // 在瀏覽時間之內
                    if (BrowseTime_BeginTime <= DateTime.Now && DateTime.Now < BrowseTime_EndTime)
                    {
                        isBrowserd = "Y";
                    }
                    else if (DateTime.Now > BrowseTime_EndTime)
                    {
                        isBrowserd = "O";
                    }

                    Enum.TryParse(isDownload + isBrowserd, out meetingRoomButtonType);

                    switch (meetingRoomButtonType)
                    {
                        case MeetingRoomButtonType.NN:
                            // 沒有圖示
                            break;
                        case MeetingRoomButtonType.YY:
                            // 未下載檔案: 下載圖示
                            // 已下載檔案: 垃圾埇圖示
                            btnAllFileRowsUpdate.Visibility = Visibility.Visible;
                            break;
                        case MeetingRoomButtonType.ON:
                        case MeetingRoomButtonType.OY:
                            // 未下載檔案: 不可下載圖示
                            // 已下載檔案: 垃圾埇圖示
                            break;
                        case MeetingRoomButtonType.NO:
                        case MeetingRoomButtonType.YO:
                        case MeetingRoomButtonType.OO:
                            // 未下載檔案: 不可下載圖示
                            // 已下載檔案: 不可瀏覽圖示
                            break;
                    }


                    Task.Factory.StartNew(() =>
                        {
                            //this.Dispatcher.BeginInvoke(new Action(() =>
                            //{
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
                                            HasRecordFile = true;
                                        }
                                        if (HasRecordFile == true)
                                        {
                                           this.Dispatcher.BeginInvoke(new Action(() =>
                                           {
                                                btnRecord.Visibility = Visibility.Visible;
                                            }));
                                        }
                                              
                                       
                                    }
                                    catch(Exception ex)
                                    {
                                        // 這裡不要寫Log好了
                                        //LogTool.Debug(ex);
                                    }
                                    //foreach (MeetingDataMeetingsFileFile item in meetingData.MeetingsFile.FileList)
                                    //{
                                    //    MeetingDataDownloadFileFile recordFile = new MeetingDataDownloadFileFile();
                                    //    recordFile.AgendaID = "record";
                                    //    recordFile.FileName = item.FileName;
                                    //    recordFile.ID = item.ID;
                                    //    recordFile.Url = item.Url;
                                    //    recordFile.version = item.version;
                                    //    FileList.Add(recordFile);
                                    //}

                                    FileList.AddRange(meetingData.DownloadFile.DownloadFileList.ToList());
                                    All_FileCount = FileList.Count;
                                    
                                    //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action(() =>
                                    this.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        // (7)加入檔案
                                        int i = 0;
                                    //foreach (MeetingDataDownloadFileFile item in meetingData.DownloadFile.DownloadFileList)
                                    foreach (MeetingDataDownloadFileFile item in FileList)
                                    {

                                        i++;
                                        bool IsLastRow = (i == FileList.Count);
                                        int mutiThreadIndex = i;

                                        if (item.AgendaID.Equals("") == true || item.AgendaID.Equals("c") == true || item.AgendaID.Equals("i") == true)
                                        {
                                            HasSubjectFile = true;
                                            imgSubject.Visibility = Visibility.Visible;
                                        }
                                        var fr = new FileRow(UserID, UserName, UserPWD, meetingData.ID, UserEmail
                                                                           , mutiThreadIndex, IsLastRow, item
                                                                           , MeetingDataCT_RaiseAllDownload_Callback
                                                                           , MeetingDataCT_HangTheDownloadEvent_Callback
                                                                           , MeetingDataCT_IsAllFileRowFinished_AddInitUIFinished_Callback
                                                                           , MeetingDataCT_GetBookVMs_ByMeetingFileCate_Callback
                                                                           , MeetingDataCT_GetWatermark_Callback
                                                                           , meetingRoomButtonType
                                                                           , MeetingDataCT_Counting_Finished_FileCount_Callback);
                                        fr.FolderID = item.FolderID;
                                            
                                            if(!md.Type.Equals("1"))
                                                fr.CanNotCollect = true;


                                            if (meetingData.DownloadFile.DownloadTime.BeginTime.Length > 0 ||
                                            meetingData.DownloadFile.DownloadTime.EndTime.Length > 0 ||
                                            meetingData.DownloadFile.BrowseTime.BeginTime.Length > 0 ||
                                            meetingData.DownloadFile.BrowseTime.EndTime.Length > 0)
                                            {
                                                fr.CanNotCollect = true;
                                            }

                                            if(meetingData.DownloadFile.BrowseTime.BeginTime.Length>0)
                                            {
                                                var date = DateTime.ParseExact(meetingData.DownloadFile.BrowseTime.BeginTime, "yyyy-MM-dd HH:mm:ss", null);

                                                if (DateTime.Now < date)
                                                    fr.CanNotCollect = true;
                                            }


                                            FileRowSP.Children.Add(fr);

                                        }
                                    }));
                                //}));

                        });
                }
                else
                {
                    AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
                }
                MouseTool.ShowArrow();
            }
         
        }

        private string MeetingDataCT_GetWatermark_Callback()
        {

            string watermark = "";
            try
            {
                if (meetingData != null && meetingData.watermark!=null && meetingData.watermark.Equals("Y"))
                {
                    watermark = UserID + "-" + UserName;
                }

                // 正在開會議的會議
                if (DateTime.Parse(meetingData.BeginTime) <= DateTime.Now && DateTime.Now < DateTime.Parse(meetingData.EndTime))
                {
                    watermark = "";
                }

                Home Home_Window = Application.Current.Windows.OfType<Home>().FirstOrDefault();
                //正在同步中的會議
                if (Home_Window.IsInSync == true)
                {
                    watermark = "";
                }
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }
            return watermark;
        }

        private bool MeetingDataCT_IsAllFileRowFinished_AddInitUIFinished_Callback()
        {
            return ++Loaded_FileCount == All_FileCount;
        }

        private void MeetingDataCT_HangTheDownloadEvent_Callback(string LastFileItemID)
        {

            // 掛上下載事件
            //FileDownloader fileDownloader = Singleton_FileDownloader.GetInstance(MeetingID);
            //fileDownloader.MeetingDataCT_DownloadFileStart_Event += Start_callback;
            //fileDownloader.MeetingDataCT_DownloadProgressChanged_Event += Progress_callback;
            //fileDownloader.MeetingDataCT_DownloadFileCompleted_Event += Finish_callback;
            //fileDownloader.MeetingDataCT_UnZip_Event += UnZip_callback;
            //fileDownloader.MeetingDataCT_UnZipError_Event += UnZipError_callback;


                bool AutoDownload=PaperLess_Emeeting.Properties.Settings.Default.MeetingDataCT_AutoDownload;
                bool AutoUpdate =PaperLess_Emeeting.Properties.Settings.Default.MeetingDataCT_AutoUpdate;

                // 控制自動下載，如果是自動下載
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
                        // 超過下載要返回
                        return; //<== 這裡重要
                        break;
                    case MeetingRoomButtonType.NO:
                    case MeetingRoomButtonType.YO:
                    case MeetingRoomButtonType.OO:
                        // 未下載檔案: 不可下載圖示
                        // 已下載檔案: 不可瀏覽圖示
                        // 超過瀏覽要返回
                        return; //<== 這裡重要
                        break;
                }


                // 控制自動下載，如果是自動下載
                if (AutoDownload == true)
                {
                    // 自動下載包含全部
                    MeetingDataCT_RaiseAllDownload_Callback(LastFileItemID);
                }
                else if (AutoUpdate == true)  // 控制自動更新
                {
                    // 自動下載包含全部
                    MeetingDataCT_RaiseAllDownload_Callback(LastFileItemID, true,true);
                    // 修正回來，把Already_RaiseAllDownload = true;
                    // 因為自動更新的部分並不算是第一次觸發所有下載。
                  
                }
               
                


            //MouseButtonEventArgs args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 100, MouseButton.Left);
            //args.RoutedEvent = UIElement.MouseLeftButtonDownEvent;
            //btnAllFileRowsUpdate.RaiseEvent(args);

            // 好了之後把所有FileRow設定成可見
            //FileRowSP.Children.OfType<FileRow>().ToList().ForEach(x =>
            //{
            //    x.Visibility = Visibility.Visible;
            //});
        }

        private void MeetingDataCT_RaiseAllDownload_Callback(string LastFileItemID, bool IsAutoUpdate = false, bool DoNotChangeAlready_RaiseAllDownload=false)
        {
              //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                this.Dispatcher.BeginInvoke(new Action<string, bool, bool>(MeetingDataCT_RaiseAllDownload_Callback), LastFileItemID, IsAutoUpdate, DoNotChangeAlready_RaiseAllDownload);
            }
            else
            {
                if (Already_RaiseAllDownload == false)
                {

                    if (DoNotChangeAlready_RaiseAllDownload == false)
                        Already_RaiseAllDownload = true;
                    

                    //FileRow LastFileRow = null;

                    List<File_DownloadItemViewModel> list = new List<File_DownloadItemViewModel>();
                    //FileRowSP.Children.OfType<FileRow>().ToList().ForEach(x =>
                    foreach (FileRow x in FileRowSP.Children.OfType<FileRow>())
                    {
                        // 多執行緒環境可能會造成FileItem為Null
                        // 但是FileRow已經產生
                        // 所以要先確保fileItem已經產生。
                        if (x.fileItem!=null && (x.fileItem.DownloadBytes == 0 || x.fileItem.DownloadBytes < x.fileItem.TotalBytes))
                        {
                            if (IsAutoUpdate == true)
                            {
                                if (x.fileItem.CanUpdate == false)
                                    continue;
                            }

                            // 記得必須隱藏得先做，才不會被看見
                            x.btnOpen.Visibility = Visibility.Collapsed;
                            x.btnDownload.Visibility = Visibility.Collapsed;
                            x.btnPausing.Visibility = Visibility.Collapsed;
                            x.txtPercent.Text = "等待中";
                            x.txtPercent.Foreground = Brushes.Gray;
                            x.txtPercent.Visibility = Visibility.Visible;
                            x.pb.Foreground = Brushes.Wheat;
                            x.pb.Background = Brushes.Gray;
                            x.pb.Value = x.fileItem.NowPercentage;
                            x.pb.Visibility = Visibility.Visible;
                            x.btnPause.Visibility = Visibility.Visible;

                            
                            list.Add(x.fileItem);
                        }

                        #region 過時
                        //MouseButtonEventArgs args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 100, MouseButton.Left);
                        //args.RoutedEvent = UIElement.MouseLeftButtonDownEvent;
                        //x.btnDownload.RaiseEvent(args);
                        #endregion
                    }

                    //List<File_DownloadItemViewModel> list = FileRowSP.Children.OfType<FileRow>().Where(x => x.fileItem.ID.Equals(FileRow_ID) == false).Select(x => x.fileItem).ToList();

                    Task.Factory.StartNew(() =>
                    {
                        FileDownloader fileDownloader = Singleton_FileDownloader.GetInstance(meetingData.ID);
                        list = fileDownloader.GetNotInList(list);
                        list.RemoveAll(x => x.ID.Equals(LastFileItemID));
                        fileDownloader.AddItem(list);
                    });
                }
            }
    
        }

        private int MeetingDataCT_GetAgendaInwWorkCount_Callback(string AgendaID)
        {

            return AgendaRowSP.Children.OfType<AgendaRow>().Where(x => x.meetingDataAgenda.Progress.Equals("U") && 
                                                                       x.meetingDataAgenda.ID.Equals(AgendaID) == false).Count();

        }

        private void MeetingDataCT_ShowAgendaFile_Callback(string AgendaID,string ParentID, bool IsDbClick)
        {
            SetColorBlackOn_TxtName_TxtRecord_TxtSubjectOnGray_AgendaRow();
            string childernIDsString="";
            
            if(AgendaID.Equals("All"))
                childernIDsString = GetChildernAgendaIDs(AgendaID, AgendaFilter.顯示全部附件);
            else if (IsDbClick == true && (ParentID.Equals("c0") || ParentID.Equals("i0") || ParentID.Equals("0")))
                childernIDsString=GetChildernAgendaIDs(AgendaID,AgendaFilter.顯示父議題和子議題附件);
            else
                childernIDsString = GetChildernAgendaIDs(AgendaID, AgendaFilter.顯示當前議題附件);

            // 其他議程附件
            if (AgendaID.Equals("") == false && AgendaID.Equals("c") == false && AgendaID.Equals("i") == false)
            {
                int i = 0;
                //FileRowSP.Children.OfType<FileRow>().ToList().ForEach(fileRow =>
                foreach (FileRow fileRow in FileRowSP.Children.OfType<FileRow>())
                {
                    string fileItem_AgendaID = fileRow.meetingDataDownloadFileFile.AgendaID;
                    if ((AgendaID.Equals("All") == true || fileItem_AgendaID.Equals("") == false || fileItem_AgendaID.Equals("c") == false || fileItem_AgendaID.Equals("i") == false))
                    {
                                                
                        if (AgendaID.Equals("All") == true)
                        {
                          i++;
                          fileRow.Visibility = Visibility.Visible;
                        }
                        else if (childernIDsString.Contains(","+fileItem_AgendaID+","))
                        {
                             i++;
                            fileRow.txtIndex.Text = i.ToString();
                            fileRow.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            fileRow.Visibility = Visibility.Collapsed;
                        }
                       
                    }
                    else
                    {
                        fileRow.Visibility = Visibility.Collapsed;
                    }
                }
                //});
            }
            else   // 標題附件
            {
                int i = 0;
                foreach (FileRow fileRow in FileRowSP.Children.OfType<FileRow>())
                {
                    string fileItem_AgendaID = fileRow.meetingDataDownloadFileFile.AgendaID;
                    if (fileItem_AgendaID.Equals("") == true || fileItem_AgendaID.Equals("c") == true || fileItem_AgendaID.Equals("i") == true)
                    {
                        i++;
                        fileRow.txtIndex.Text = i.ToString();
                        fileRow.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        fileRow.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private string GetChildernAgendaIDs(string AgendaID, AgendaFilter agendaFilter)
        {
            List<string> childrenIDs = new List<string>();
            childrenIDs.Add(AgendaID);

            if (meetingData == null)
                return "";

            switch (agendaFilter)
            {
                case AgendaFilter.顯示當前議題附件:
                    break;
                case AgendaFilter.顯示父議題和子議題附件:
                        List<string> children = meetingData.AgendaList.Where(x => x.ParentID.Equals(AgendaID)).Select(x => x.ID).ToList();
                        childrenIDs.AddRange(children);
                    break;
                case AgendaFilter.顯示全部附件:
                        List<string> all = meetingData.AgendaList.Select(x => x.ID).ToList();
                        childrenIDs.AddRange(all);
                    break;
            }
            
            return string.Format(",{0},",string.Join(",",childrenIDs));
        }

        private void SetColorBlackOn_TxtName_TxtRecord_TxtSubjectOnGray_AgendaRow()
        {
            txtMeetingName.Foreground = Brushes.Black;
            txtSubject.Foreground = ColorTool.HexColorToBrush("#A1a19d");
            txtRecord.Foreground = Brushes.Black;

            //AgendaRowSP.Children.OfType<AgendaRow>().ToList().ForEach( agendaRow =>
            //{ 
            //    agendaRow.txtName.Foreground = Brushes.Black;
            //});    
            foreach (AgendaRow agendaRow in AgendaRowSP.Children.OfType<AgendaRow>()) 
            {
                agendaRow.txtAgendaName.Foreground = Brushes.Black;
                agendaRow.txtAgendaName.Inlines.LastInline.Foreground = new SolidColorBrush(Color.FromRgb(161, 161, 157));
                agendaRow.txtCaption.Foreground = new SolidColorBrush(Color.FromRgb(161, 161, 157));
            };
        }

       

        
    }
}
