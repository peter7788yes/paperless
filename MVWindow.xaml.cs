using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaperLess_Emeeting.App_Code.DownloadItem;
using PaperLess_Emeeting.App_Code.MessageBox;
using PaperLess_Emeeting.App_Code.Socket;
using PaperLess_Emeeting.App_Code.ViewModel;
using PaperlessSync.Broadcast.Socket;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PaperLess_Emeeting
{
    /// <summary>
    /// MVWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MVWindow : Window, IDisposable
    {
        public DispatcherTimer MediaPlayerTimer = null;
        public DispatcherTimer MouseTimer = null;
        public Point lastMousePoint = new Point(0, 0);
        public bool IsSeekBarDragging = false;
        public bool IsAlwaysShowHeaderFooter = false;
        public double moiveTotalMilliseconds = 0;

        public string MeetingID { get; set; }
        public string UserID { get; set; }
        public string UserName { get; set; }
        public string FilePath { get; set; }
        public string FileID { get; set; }
        public MVWindow_IsInSync_And_IsSyncOwner_Function MVWindow_IsInSync_And_IsSyncOwner_Callback;
        public MVWindow_MVAction_Function MVWindow_MVAction_Callback;


        bool IsInSync = false;
        bool IsSyncOwner = false;
        long DeltaUTC = 0;
        public string pageJson = "";

        public Dictionary<string, BookVM> cbBooksData = new Dictionary<string, BookVM>();
        public event Home_OpenBookFromReader_Function Home_OpenBookFromReader_Event;

        public bool HasJoin2Folder = false;
        public string FolderID;
        public bool CanNotCollect;
        public bool cloud = false;
        public bool today = false;
        public MVWindow(Dictionary<string ,BookVM> cbBooksData
                        , Home_OpenBookFromReader_Function callback,string FilePath,string FileID, string pageJson = "")
        {
            MouseTool.ShowLoading();
            InitializeComponent();
            this.cbBooksData = cbBooksData;
            this.Home_OpenBookFromReader_Event = callback;
            this.FilePath = FilePath;
            this.FileID = FileID;
            this.pageJson = pageJson;
            this.Loaded += MVWindow_Loaded;
            this.Unloaded += MVWindow_Unloaded;
            //this.Closing+=MVWindow_Closing;
            //this.Closed+=MVWindow_Closed;
            //MouseTool.ShowArrow();
        }

        private void MVWindow_Closed(object sender, EventArgs e)
        {
             //mediaPlayer.Stop();
             //mediaPlayer.Close();
        }

        private void MVWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //mediaPlayer.Stop();
            //mediaPlayer.Close();
        }

        private void SaveData(string FolderID)
        {

            string count = MSCE.ExecuteScalar("Select count(1) from userfile where userID=@1 and Fileid=@2 "
                              , UserID.Replace("_Sync", "")
                              , FileID);

            if (count.Equals("0"))
            {
                MSCE.ExecuteNonQuery("insert into userfile (folderid,Fileid,userid) values(@1,@2,@3)"
                                      , FolderID
                                      , FileID
                                      , UserID.Replace("_Sync", ""));
            }
            else
            {
                MSCE.ExecuteNonQuery("update userfile set folderid=@1 where fileid=@2 and userid=@3"
                                    , FolderID
                                    , FileID
                                    , UserID.Replace("_Sync", ""));
            }
        }

        public MVWindow(string FilePath, string pageJson = "")
        {
            MouseTool.ShowLoading();
            InitializeComponent();
            this.FilePath = FilePath;
            this.FileID = FileID;
            this.pageJson = pageJson;
            this.Loaded += MVWindow_Loaded;
            this.Unloaded += MVWindow_Unloaded;
            //MouseTool.ShowArrow();
        }

        private void MVWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            Singleton_Socket.mvWindow_OpenIEventManager.MVWindow_IsInSync_And_IsSyncOwner_Event -= MVWindow_IsInSync_And_IsSyncOwner_Callback;
            Singleton_Socket.mvWindow_OpenIEventManager.MVWindow_MVAction_Event -= MVWindow_MVAction_Callback;

        }

        private void MVWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                InitSelectDB();
                // 只要是 Row 列表內容畫面，優先權設定為Background => 列舉值為 4。 所有其他非閒置作業都完成之後，就會處理作業。
                // 另外這裡比較特別 因為優先權要比AgendaRow高，所以我設定為Input => 列舉值為 5。 做為輸入相同的優先權處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
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

        private void InitEvent()
        {
            MVWindow_IsInSync_And_IsSyncOwner_Callback = new MVWindow_IsInSync_And_IsSyncOwner_Function(IsInSync_And_IsSyncOwner);
            MVWindow_MVAction_Callback = new MVWindow_MVAction_Function(MVAction);
            Singleton_Socket.mvWindow_OpenIEventManager.MVWindow_IsInSync_And_IsSyncOwner_Event += MVWindow_IsInSync_And_IsSyncOwner_Callback;
            Singleton_Socket.mvWindow_OpenIEventManager.MVWindow_MVAction_Event += MVWindow_MVAction_Callback;

            // 滑鼠偵測
            MouseTimer = new DispatcherTimer();
            MouseTimer.Interval = TimeSpan.FromMilliseconds(1100);
            MouseTimer.Tick += new EventHandler(MouseTimer_Tick);
            MouseTimer.Start();


            // 播放進度偵測
            MediaPlayerTimer = new DispatcherTimer();
            MediaPlayerTimer.Interval = TimeSpan.FromMilliseconds(200);
            MediaPlayerTimer.Tick += new EventHandler(MediaPlayerTimer_Tick);
            //MediaPlayerTimer.Start();

            btnClose.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnClose.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnClose.MouseLeftButtonDown += btnClose_MouseLeftButtonDown;

            btnSync.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnSync.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnSync.MouseLeftButtonDown += btnSync_MouseLeftButtonDown;

            btnLight.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnLight.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnLight.MouseLeftButtonDown += btnSync_MouseLeftButtonDown;

            btnFunction.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnFunction.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };


            HeaderDP.MouseEnter += (sender, e) => { IsAlwaysShowHeaderFooter = true; };
            HeaderDP.MouseLeave += (sender, e) => { IsAlwaysShowHeaderFooter = false; };
            FooterDP.MouseEnter += (sender, e) => { IsAlwaysShowHeaderFooter = true; };
            FooterDP.MouseLeave += (sender, e) => { IsAlwaysShowHeaderFooter = false; };

            //SeekBar.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            //SeekBar.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            SeekBar.ValueChanged += SeekBar_ValueChanged;
           

            mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            mediaPlayer.Play();

            btnFunction.Source = new BitmapImage(new Uri("images/mv_pause.png", UriKind.Relative));

            img_animation.MouseLeftButtonDown += AnimationController_MouseLeftButtonDown;
            btnFunction.MouseLeftButtonDown += AnimationController_MouseLeftButtonDown;
            mediaPlayer.MouseLeftButtonDown += AnimationController_MouseLeftButtonDown;

            cbBooks.SelectionChanged += cbBooks_SelectionChanged;

            // PageJson
            ParsePageJson();

            
            
        }

        private void SeekBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            pb.Value = SeekBar.Value;
        }

        private void cbBooks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsInSync == true && IsSyncOwner == false)
                return;

            ComboBox cb = (ComboBox)sender;

            BookVM bookVM = ((BookVM)cb.SelectedValue);
            if (bookVM == null)
                return;

            if (Home_OpenBookFromReader_Event != null)
            {
                Home_OpenBookFromReader_Event(MeetingID, bookVM, this.cbBooksData, "");
            }
        }

        private void ParsePageJson()
        {
            // {"scale":null,"animations":null,"bookmark":null,"animation":null,"bookId":"cAF40-V","execTime":"1406304980594","actionTime":"00:00:30.697","pageIndex":null,"annotation":null,"spline":null,"hide":null,"page":null,"action":"pause","y":null,"x":null}
            Dictionary<string, object> dictPageJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(pageJson);
            Dictionary<string, object> dictMsg = JsonConvert.DeserializeObject<Dictionary<string, object>>(dictPageJson["msg"].ToString());

            //play、pause、stop
            string action = dictMsg["action"].ToString();
            string execTime = dictMsg["execTime"].ToString();
            string actionTime = dictMsg["actionTime"].ToString();
            switch (action)
            {
                case "play":
                    long ec = long.Parse(execTime);
                    long nowServerTime = 0;
                    //快慢無所謂，減掉DeltaUTC，就是Server現在時間
                    nowServerTime = DateTool.GetCurrentTimeInUnixMillis() - DeltaUTC;
                   
                    long playTime = nowServerTime - ec;
                    mediaPlayer.Position = TimeSpan.Parse(actionTime) + new TimeSpan(0, 0, 0, 0, (int)playTime);
                    mediaPlayer.Play();
                    IsAlwaysShowHeaderFooter = false;

                    break;
                case "pause":
                    mediaPlayer.Play();
                    mediaPlayer.Position = TimeSpan.Parse(actionTime);
                    mediaPlayer.Pause();
                    IsAlwaysShowHeaderFooter = true;
                    break;
            }
        }


        private void AnimationController_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsInSync == true && IsSyncOwner == false)
            {
               return;
            }

            // 正在播放中
            if (GetMediaState(mediaPlayer) == MediaState.Play)
            {
                btnFunction.Source = new BitmapImage(new Uri("images/mv_right.png", UriKind.Relative));
                mediaPlayer.Pause();
                SentToOther("pause");
                img_animation.Source = new BitmapImage(new Uri("images/MVWindow_Animation_Pause.png", UriKind.Relative));
                ChangeShowHeaderFooterDP(true);
                IsAlwaysShowHeaderFooter = true;
            }
            else
            {
                btnFunction.Source = new BitmapImage(new Uri("images/mv_pause.png", UriKind.Relative));
                mediaPlayer.Play();
                SentToOther("play");
                img_animation.Source = new BitmapImage(new Uri("images/MVWindow_Animation_Play.png", UriKind.Relative));
                IsAlwaysShowHeaderFooter = false;
            }

            ShowAnimation();
        }

        private void ShowAnimation()
        {
           

            //img_animation.Visibility = Visibility.Collapsed;
            img_animation.Opacity = 1;
            img_animation.Width = 128;
            img_animation.Height = 128;
            img_animation.Visibility = Visibility.Visible;

            DoubleAnimation widthAnimation = new DoubleAnimation
            {
                From = 128,
                To = 256,
                Duration = TimeSpan.FromSeconds(0.5)
            };

            DoubleAnimation heightAnimation = new DoubleAnimation
            {
                From = 128,
                To = 256,
                Duration = TimeSpan.FromSeconds(0.5)
            };

            DoubleAnimation opacityAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.5)
            };

            //DoubleAnimation Hide_WidthAnimation = new DoubleAnimation
            //{
            //    From = 0,
            //    To = 0,
            //    Duration = TimeSpan.FromSeconds(0)
            //};

            //DoubleAnimation Hide_HeightAnimation = new DoubleAnimation
            //{
            //    From = 0,
            //    To = 0,
            //    Duration = TimeSpan.FromSeconds(0)
            //};

            //ObjectAnimationUsingKeyFrames Visibility_Animation = new ObjectAnimationUsingKeyFrames();
            //DiscreteObjectKeyFrame keyFrame = new DiscreteObjectKeyFrame();
            //keyFrame.Value = Visibility.Visible;
            //keyFrame.KeyTime = TimeSpan.FromSeconds(1.5);
            //Visibility_Animation.KeyFrames.Add(keyFrame);
            //Storyboard.SetTarget(Visibility_Animation, img_animation);
            //Storyboard.SetTargetProperty(Visibility_Animation, new PropertyPath(Image.VisibilityProperty));

            Storyboard.SetTargetProperty(widthAnimation, new PropertyPath(Image.WidthProperty));
            Storyboard.SetTarget(widthAnimation, img_animation);

            Storyboard.SetTargetProperty(heightAnimation, new PropertyPath(Image.HeightProperty));
            Storyboard.SetTarget(heightAnimation, img_animation);

            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(Image.OpacityProperty));
            Storyboard.SetTarget(opacityAnimation, img_animation);

            //Storyboard.SetTargetProperty(Hide_WidthAnimation, new PropertyPath(Image.OpacityProperty));
            //Storyboard.SetTarget(Hide_WidthAnimation, img_animation);

            //Storyboard.SetTargetProperty(Hide_HeightAnimation, new PropertyPath(Image.OpacityProperty));
            //Storyboard.SetTarget(Hide_HeightAnimation, img_animation);

            Storyboard sb = new Storyboard();
            sb.Children.Add(widthAnimation);
            sb.Children.Add(heightAnimation);
            sb.Children.Add(opacityAnimation);
            //sb.Children.Add(Hide_WidthAnimation);
            //sb.Children.Add(Hide_HeightAnimation);
            //sb.Children.Add(Visibility_Animation);
            //sb.Completed+=sb_Completed;
            sb.Begin();
        }

        private void sb_Completed(object sender, EventArgs e)
        {
            img_animation.Width = 0;
            img_animation.Height = 0;
            img_animation.Visibility = Visibility.Collapsed;
            Storyboard sb = (Storyboard)sender;
            sb.Completed -= sb_Completed;
        }
        private void btnClose_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsInSync == true && IsSyncOwner == false)
                return;

            mediaPlayer.Stop();
            mediaPlayer.Close();
            this.Close();


            if (IsInSync == true && IsSyncOwner == true)
            {
                SocketClient socketClient = Singleton_Socket.GetInstance(MeetingID, UserID, UserName,true);
                Task.Factory.StartNew(() =>
                {
                    if (socketClient != null && socketClient.GetIsConnected() == true)
                    {
                        socketClient.broadcast("{\"cmd\":\"R.CB\"}");
                    }
                    else
                    {
                        //AutoClosingMessageBox.Show("同步伺服器尚未啟動，請聯絡議事管理員開啟同步");
                    }
                });
            }
        }

        private void MVAction(Newtonsoft.Json.Linq.JObject jObject)
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action<JObject>(MVAction), jObject);
                this.Dispatcher.BeginInvoke(new Action<JObject>(MVAction), jObject);
            }
            else
            {
                string action = (string)jObject["action"];
                string execTime = (string)jObject["execTime"];
                string actionTime = (string)jObject["actionTime"];
                switch (action)
                {
                    case "play":
                        mediaPlayer.Position = TimeSpan.Parse(actionTime);
                        mediaPlayer.Pause();
                        Thread.Sleep(1);
                        mediaPlayer.Play();
                        IsAlwaysShowHeaderFooter = false;
                        ChangeShowHeaderFooterDP(false);
                        btnFunction.Source = new BitmapImage(new Uri("images/mv_pause.png", UriKind.Relative));
                        img_animation.Source = new BitmapImage(new Uri("images/MVWindow_Animation_Play.png", UriKind.Relative));
                        break;
                    case "pause":
                        mediaPlayer.Position = TimeSpan.Parse(actionTime);
                        mediaPlayer.Play();
                        Thread.Sleep(1);
                        mediaPlayer.Pause();
                        IsAlwaysShowHeaderFooter = true;
                        ChangeShowHeaderFooterDP(true);
                        btnFunction.Source = new BitmapImage(new Uri("images/mv_right.png", UriKind.Relative));
                        img_animation.Source = new BitmapImage(new Uri("images/MVWindow_Animation_Pause.png", UriKind.Relative));
                        break;
                    case "stop":
                        mediaPlayer.Stop();
                        break;
                }
                ShowAnimation();
            }

           
        }

        private void IsInSync_And_IsSyncOwner(Newtonsoft.Json.Linq.JArray jArry)
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action<JArray>(IsInSync_And_IsSyncOwner), jArry);
                this.Dispatcher.BeginInvoke(new Action<JArray>(IsInSync_And_IsSyncOwner), jArry);
            }
            else
            {
                //if (jArry.ToString().Contains("\"clientId\":\"" + UserID + "\"") == false)
                //{
                //    IsInSync = false;
                //    IsSyncOwner = false;
                //    ChangeSyncButtonLight(IsInSync, IsSyncOwner);
                //    return;
                //}

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
                                IsAlwaysShowHeaderFooter = false;
                                break;
                            case "0": //被同步者
                                IsInSync = true;
                                IsSyncOwner = false;
                                if (GetMediaState(mediaPlayer) == MediaState.Pause)
                                {
                                    IsAlwaysShowHeaderFooter = true;
                                    ChangeShowHeaderFooterDP(true);
                                }
                                else if (GetMediaState(mediaPlayer) == MediaState.Pause)
                                {
                                    IsAlwaysShowHeaderFooter = false;
                                    ChangeShowHeaderFooterDP(false);
                                }
                                break;
                            case "1": //沒有同步
                                IsInSync = false;
                                IsSyncOwner = false;
                                IsAlwaysShowHeaderFooter = false;
                                break;
                            default:
                                IsInSync = false;
                                IsSyncOwner = false;
                                IsAlwaysShowHeaderFooter = false;
                                break;
                        }
                        break;
                    }

                }

                ChangeSyncButtonLight(IsInSync, IsSyncOwner);
            }
        }

        private void ChangeSyncButtonLight(bool IsInSync, bool IsSyncOwner)
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action<bool, bool>(ChangeSyncButtonLight), IsInSync, IsSyncOwner);
                this.Dispatcher.BeginInvoke(new Action<bool, bool>(ChangeSyncButtonLight), IsInSync, IsSyncOwner);
            }
            else
            {
                btnSync.Source = ButtonTool.GetSyncButtonImage(IsInSync, IsSyncOwner);
                btnLight.Source = ButtonTool.GetSyncButtonImage(IsInSync, IsSyncOwner);
            }
        }

        private void btnSync_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
                bool syncSwitch = false;

                // 沒有同步中，改成同步
                if (IsInSync == false)
                {
                    IsInSync = true;
                    syncSwitch = true;
                }
                else   // 同步中，改成沒有同步
                {
                    IsInSync = false;
                    syncSwitch = false;
                }

                btnSync.Source = ButtonTool.GetSyncButtonImage(IsInSync, IsSyncOwner);

                SocketClient socketClient = Singleton_Socket.GetInstance(MeetingID, UserID, UserName,false);
                Task.Factory.StartNew(() =>
                {
                    if (socketClient != null && socketClient.GetIsConnected() == true)
                    {
                        socketClient.syncSwitch(syncSwitch);
                    }
                    else
                    {
                        IsInSync = false;
                        IsSyncOwner = false;
                        this.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            AutoClosingMessageBox.Show("同步伺服器尚未啟動，請聯絡議事管理員開啟同步");
                            btnSync.Source = ButtonTool.GetSyncButtonImage(IsInSync, IsSyncOwner);
                            cbBooks.Visibility = Visibility.Visible;
                        }));
                       
                    }
                });
        }

        private void SeekBar_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            IsSeekBarDragging = true;
        }

        private void SeekBar_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            mediaPlayer.Position = TimeSpan.FromMilliseconds(SeekBar.Value);

            // 正在播放
            if (GetMediaState(mediaPlayer) == MediaState.Play)
            {
                //廣播
                SentToOther("play");
            }
            else // 停止播放
            {
                mediaPlayer.Play();
                
                // 暫停一毫秒
                Thread.Sleep(75);
                mediaPlayer.Pause();
                //廣播
                SentToOther("pause");
            }
            IsSeekBarDragging = false;
        }


        private void SentToOther(string function)
        {
            long ecTime = DateTool.GetCurrentTimeInUnixMillis() - DeltaUTC;
               
            if (IsInSync == true && IsSyncOwner == true)
            {
                DateTime mydate = new DateTime(mediaPlayer.Position.Ticks);

                Task.Factory.StartNew(() =>
                {
                    SocketClient socketClient = Singleton_Socket.GetInstance(MeetingID, UserID, UserName,true);
                    if (socketClient != null && socketClient.GetIsConnected() == true)
                    {
                        socketClient.broadcast("{\"execTime\":" + ecTime.ToString() + ",\"action\":\"" + function + "\",\"actionTime\":\"" + mydate.ToString("HH:mm:ss.fff") + "\",\"cmd\":\"R.SV\"}");
                    }
                    else
                    {
                        //AutoClosingMessageBox.Show("同步伺服器尚未啟動，請聯絡議事管理員開啟同步");
                    }
                });
            }

        }

       

        private void ChangeShowHeaderFooterDP(bool toShowing)
        {
            if (toShowing == true)
            {
                HeaderDP.Visibility = Visibility.Visible;
                FooterDP.Visibility = Visibility.Visible;
                Mouse.OverrideCursor = Cursors.Arrow;
                btnLight.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (IsAlwaysShowHeaderFooter == true)
                    return;
                HeaderDP.Visibility = Visibility.Collapsed;
                FooterDP.Visibility = Visibility.Collapsed;
                btnLight.Visibility = Visibility.Visible;
                Mouse.OverrideCursor = Cursors.None;
            }
        }

        private MediaState GetMediaState(MediaElement myMedia)
        {
            FieldInfo hlp = typeof(MediaElement).GetField("_helper", BindingFlags.NonPublic | BindingFlags.Instance);
            object helperObject = hlp.GetValue(myMedia);
            FieldInfo stateField = helperObject.GetType().GetField("_currentState", BindingFlags.NonPublic | BindingFlags.Instance);
            MediaState state = (MediaState)stateField.GetValue(helperObject);
            return state;
        }

        private void InitUI()
        {
            try
            {
                this.Width = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
                this.Height = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
                this.Top = 0; //移動視窗到左上角
                this.Left = 0;//移動視窗到左上角
                this.WindowState = System.Windows.WindowState.Normal;
                this.WindowStyle = WindowStyle.None;
                this.ResizeMode = System.Windows.ResizeMode.NoResize;


                mediaPlayer.LoadedBehavior = MediaState.Manual;
                mediaPlayer.UnloadedBehavior = MediaState.Manual;


                //mediaPlayer.Source = new Uri(fileItem.UnZipFilePath + "\\" + fileItem.StorageFileName, UriKind.Absolute);
                mediaPlayer.Source = new Uri(FilePath, UriKind.Absolute);

                Home Home_Window = Application.Current.Windows.OfType<Home>().FirstOrDefault();

                if (Home_Window != null)
                {
                    IsInSync = Home_Window.IsInSync;
                    IsSyncOwner = Home_Window.IsSyncOwner;

                    if (pageJson.Equals("") == false)
                    {
                        IsInSync = true;
                        IsSyncOwner = false;
                    }

                    if (IsInSync == true && IsSyncOwner == false)
                        cbBooks.Visibility = Visibility.Collapsed;

                }

                btnSync.Source = ButtonTool.GetSyncButtonImage(IsInSync, IsSyncOwner);
                btnLight.Source = ButtonTool.GetSyncButtonImage(IsInSync, IsSyncOwner);



                cbBooks.ItemsSource = cbBooksData;
                cbBooks.DisplayMemberPath = "Key";
                cbBooks.SelectedValuePath = "Value";
                cbBooks.SelectedIndex = 0;

                int i = 0;
                if (cbBooksData != null)
                {
                    foreach (KeyValuePair<string, BookVM> item in cbBooksData)
                    {

                        if (item.Value.FileID.Equals(this.FileID) == true)
                        {
                            cbBooks.SelectedIndex = i;
                            break;
                        }
                        i++;
                    }
                }
                else
                {
                    cbBooks.Visibility = Visibility.Collapsed;
                }


                string count = MSCE.ExecuteScalar("Select count(1) from userfile where userID=@1 and Fileid=@2 "
                              , UserID.Replace("_Sync", "")
                              , FileID);

                if (count.Equals("0") == false)
                {
                    var dt = MSCE.GetDataTable("select FolderID from userfile  where userid =@1 and fileid=@2"
                                         , UserID.Replace("_Sync", "")
                                         , FileID);

                    if (dt.Rows.Count > 0)
                    {
                        FolderID = dt.Rows[0]["FolderID"].ToString();

                        if (FolderID.Length == 0)
                        {
                            HasJoin2Folder = false;
                            imgJoin.Source = new BitmapImage(new Uri("image/ebTool-toCloud2@2x.png", UriKind.Relative));
                        }
                        else
                        {
                            HasJoin2Folder = true;
                            imgJoin.Source = new BitmapImage(new Uri("image/ebTool-inCloud2@2x.png", UriKind.Relative));
                        }
                    }
                }

                if (this.FolderID == null || this.FolderID.Equals(""))
                {
                    HasJoin2Folder = false;
                    imgJoin.Source = new BitmapImage(new Uri("image/ebTool-toCloud2@2x.png", UriKind.Relative));
                }
                else
                {
                    HasJoin2Folder = true;
                    imgJoin.Source = new BitmapImage(new Uri("image/ebTool-inCloud2@2x.png", UriKind.Relative));
                }


                if (CanNotCollect)
                    imgJoin.Source = new BitmapImage(new Uri("image/ebTool-toCloudDisabled2@2x.png", UriKind.Relative));

                if (cloud && !today)
                {

                    DispatcherTimer timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(1);
                    timer.Tick += (sender, e) =>
                    {
                        btnSync.Visibility = Visibility.Collapsed;
                        btnSync.Width = 0;
                        btnSync.Height = 0;
                        timer.Stop();
                    };
                    timer.Start();
                }
                else
                {
                    btnSync.Visibility = Visibility.Visible;
                }
            }
            catch(Exception ex)
            {

            }
            
        }

        private void InitSelectDB()
        {
            DataTable dt = MSCE.GetDataTable("select MeetingID,UserID,UserName,DeltaUTC from NowLogin");
            if (dt.Rows.Count > 0)
            {
                MeetingID = dt.Rows[0]["MeetingID"].ToString().Trim();
                UserID = dt.Rows[0]["UserID"].ToString().Trim();
                UserName = dt.Rows[0]["UserName"].ToString().Trim();
                long.TryParse(dt.Rows[0]["DeltaUTC"].ToString(), out DeltaUTC);
               
            }
        }

        private void MouseTimer_Tick(object sender, EventArgs e)
        {
            if (Point.Equals(lastMousePoint, MousePosition.GetCurrentMousePosition()) && IsAlwaysShowHeaderFooter == false)
            {

                ChangeShowHeaderFooterDP(false);
            }
            else
            {
                lastMousePoint = MousePosition.GetCurrentMousePosition();

                if (IsInSync == true && IsSyncOwner == false)
                    return;
                ChangeShowHeaderFooterDP(true);
            }
        }


        private void MediaPlayerTimer_Tick(object sender, EventArgs e)
        {
            if (IsSeekBarDragging == false)
            {
                SeekBar.Value = mediaPlayer.Position.TotalMilliseconds;
                pb.Value = mediaPlayer.Position.TotalMilliseconds;
            }

            DateTime mydate = new DateTime(mediaPlayer.Position.Ticks);
            txtCurrentTime.Text = mydate.ToString("HH:mm:ss");
        }

        private void MediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                TimeSpan ts = mediaPlayer.NaturalDuration.TimeSpan;
                SeekBar.Maximum = ts.TotalMilliseconds;
                pb.Maximum = ts.TotalMilliseconds;
                moiveTotalMilliseconds = ts.TotalMilliseconds;
            }
            // 換算成ticks
            DateTime mydate = new DateTime((long)moiveTotalMilliseconds * 10000);
            txtEndTime.Text = mydate.ToString("HH:mm:ss");
            MediaPlayerTimer.Start();
        }



        public void Dispose()
        {
            //mediaPlayer.Stop();
            //mediaPlayer.Close();
            GC.Collect();
        }

        private void imgJoin_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (CanNotCollect)
                return;

            if (HasJoin2Folder)
            {
                imgJoin.Source = new BitmapImage(new Uri("image/ebTool-inCloud-on2@2x.png", UriKind.Relative));
            }
            else
            {
                imgJoin.Source = new BitmapImage(new Uri("image/ebTool-toCloud-on2@2x.png", UriKind.Relative));
            }

            if (HasJoin2Folder)
            {
                DelFile win = new DelFile(this, FolderID, FileID);
                var success = win.ShowDialog();
                if (success == true)
                {
                    SaveData("");
                    HasJoin2Folder = false;
                    imgJoin.Source = new BitmapImage(new Uri("image/ebTool-toCloud2@2x.png", UriKind.Relative));
                }
                else
                {
                    imgJoin.Source = new BitmapImage(new Uri("image/ebTool-inCloud2@2x.png", UriKind.Relative));
                }

            }
            else
            {
                JoinFolder win = new JoinFolder(this, this.FileID, (FolderID, FolderName) =>
                {
                    //OKFolder win2 = new OKFolder(FolderName, this);
                    //win2.ShowDialog();
                    if (FolderID.Length > 0)
                    {
                        AutoClosingMessageBox.Show("加入成功");
                        HasJoin2Folder = true;
                        imgJoin.Source = new BitmapImage(new Uri("image/ebTool-inCloud2@2x.png", UriKind.Relative));
                        this.FolderID = FolderID;
                        SaveData(FolderID);
                    }
                    else
                    {
                        imgJoin.Source = new BitmapImage(new Uri("image/ebTool-toCloud2@2x.png", UriKind.Relative));
                    }
                });
                win.ShowDialog();
            }
        }

        private void DelData(string FolderID)
        {
            MSCE.ExecuteScalar("delete from userfile where userID=@1 and Fileid=@2 "
                             , UserID.Replace("_Sync", "")
                             , FileID);
        }
    }


    public class MousePosition : DependencyObject
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(ref NativePoint pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct NativePoint
        {
            public int X;
            public int Y;
        };

        public static Point GetCurrentMousePosition()
        {
            NativePoint nativePoint = new NativePoint();
            GetCursorPos(ref nativePoint);
            return new Point(nativePoint.X, nativePoint.Y);
        }

        private Dispatcher dispatcher;

        System.Timers.Timer timer = new System.Timers.Timer(100);

        public MousePosition()
        {
            dispatcher = Application.Current.MainWindow.Dispatcher;
            timer.Elapsed += timer_Elapsed;
            timer.Start();
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Point current = GetCurrentMousePosition();
            this.CurrentPosition = current;
        }

        public Point CurrentPosition
        {
            get { return (Point)GetValue(CurrentPositionProperty); }

            set
            {
                dispatcher.Invoke((Action)(() =>
                  SetValue(CurrentPositionProperty, value)));
            }
        }

        public static readonly DependencyProperty CurrentPositionProperty
          = DependencyProperty.Register(
            "CurrentPosition", typeof(Point), typeof(MousePosition));
    }
}
