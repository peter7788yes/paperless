using Newtonsoft.Json;
using PaperLess_Emeeting.App_Code.MessageBox;
using PaperLess_ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Permissions;
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
using System.Xml.Linq;

namespace PaperLess_Emeeting
{
    public delegate void Home_Change2MeetingDataCT_Function(string MeetingID, MeetingData meetingData = null);
    /// <summary>
    /// MeetingList.xaml 的互動邏輯
    /// </summary>
    public partial class MeetingListCT : UserControl
    {
        public event Home_Change2MeetingDataCT_Function Home_Change2MeetingDataCT_Event;

        public string UserID { get; set; }
        public string UserName { get; set; }
        public string UserPWD { get; set; }
        public DateTime MeetingListDate { get; set; }
        public UserMeeting[] UserMeetingAry { get; set; }
        public string NewAddMeetingID = "";

        //預載上一個月和下一個月
        public Dictionary<DateTime, User> PreLoadLastNextMonthDict = new Dictionary<DateTime, User>();
        public int CacheMinuteTTL = 0;
        public Thread CacheThread = null;
        Button btnShowMeetingRooms = null;
        public MeetingListCT(UserMeeting[] UserMeetingAry, DateTime MeetingListDate, Home_Change2MeetingDataCT_Function callback)
        {
            //MouseTool.ShowLoading();
            InitializeComponent();
            this.UserMeetingAry = UserMeetingAry;
            this.MeetingListDate = MeetingListDate;
            this.Home_Change2MeetingDataCT_Event += callback;
            this.Loaded += MeetingListCT_Loaded;
            this.CacheMinuteTTL = PaperLess_Emeeting.Properties.Settings.Default.CacheMinuteTTL;
            //MouseTool.ShowArrow();
           
        }

        private void MeetingListCT_Loaded(object sender, RoutedEventArgs e)
        {
           Task.Factory.StartNew(() =>
           {
               InitSelectDB();
               // 這裡為首頁 Home 下的會議列表畫面，優先權設定為Normal => 列舉值為 9。 一般優先權處理作業。 這是一般的應用程式的優先順序。
               // 這裡優先權設定為跟Home一樣，不然Home首頁會先沒有畫面，再出現
               //this.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action<UserMeeting[], DateTime>((innerUserMetingAry, innerMeetingListDate) =>
               this.Dispatcher.BeginInvoke(new Action<UserMeeting[], DateTime>((innerUserMetingAry, innerMeetingListDate) =>
               {
                   try
                   {
                       InitUI(innerUserMetingAry, innerMeetingListDate);
                       InitEvent();
                   }
                   catch (Exception ex)
                   {
                       LogTool.Debug(ex);
                   }
                   MouseTool.ShowArrow();

               }), UserMeetingAry, MeetingListDate);

               //預載上一個月和下一個月
               PreLoadLastNextMonth();
              
           });


          
        }

        //預載上一個月和下一個月
        private void PreLoadLastNextMonth()
        {
            //小於0為沒有Cache
            //等於0為Cache不會過期
            //大於0為多少分鐘後清掉Cache
            if (this.CacheMinuteTTL >= 0)
            {
                Task.Factory.StartNew(() =>
                {
                   
                        GetUserData.AsyncPOST(UserID, UserPWD, MeetingListDate.AddMonths(-1)
                                              , (userObj1, dateTime1) =>
                        {
                             try
                             {
                                                  PreLoadLastNextMonthDict[dateTime1] = userObj1;

                                                  GetUserData.AsyncPOST(UserID, UserPWD, MeetingListDate.AddMonths(1)
                                                                       , (userObj2, dateTime2) => {
                                                                           try
                                                                           {
                                                                               PreLoadLastNextMonthDict[dateTime2] = userObj2;
                                                                           }
                                                                           catch(Exception ex)
                                                                           {
                                                                               LogTool.Debug(ex);
                                                                           }
                                                                        });
                                                  if (this.CacheMinuteTTL > 0)
                                                  {
                                                      if (CacheThread != null)
                                                          CacheThread.Abort();
                                                      CacheThread = new Thread(delegate()
                                                      {
                                                          Thread.Sleep(this.CacheMinuteTTL * 60 * 1000);
                                                          PreLoadLastNextMonthDict.Clear();

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
            DataTable dt = MSCE.GetDataTable("select UserID,UserName,UserPWD,MeetingListDate from NowLogin");
            if (dt.Rows.Count > 0)
            {
                UserID = dt.Rows[0]["UserID"].ToString().Trim();
                UserName = dt.Rows[0]["UserName"].ToString().Trim();
                UserPWD = dt.Rows[0]["UserPWD"].ToString().Trim();
            }
        }

        //TouchPoint TouchStart=default(TouchPoint);
        //bool AlreadySwiped = false;
        private void InitEvent()
        {
            //this.TouchDown += new EventHandler<TouchEventArgs>((sender,e) => 
            //{ 
            //    TouchStart = e.GetTouchPoint(this);
            //    txtPinCode.Text = "111";
            //});

            //this.TouchMove += new EventHandler<TouchEventArgs>((sender, e) =>
            //{
            //    if (!AlreadySwiped)
            //    {
            //        var Touch = e.GetTouchPoint(this);
            //        //right now a swipe is 200 pixels 
            //        //Swipe Left
            //        if (TouchStart != null && Touch.Position.X > (TouchStart.Position.X + 200))
            //        {
            //            SwipeLeft();
            //            AlreadySwiped = true;
            //        }
            //        //Swipe Right
            //        if (TouchStart != null && Touch.Position.X < (TouchStart.Position.X - 200))
            //        {
            //            SwipeRight();
            //            AlreadySwiped = true;
            //        }
            //    }
            //    e.Handled = true;
            //});

            btnAdd.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnAdd.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnAdd.MouseLeftButtonDown += btnAdd_MouseLeftButtonDown;

            btnLast.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnLast.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnLast.MouseLeftButtonDown += btnLastNext_MouseLeftButtonDown;

            btnNext.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnNext.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnNext.MouseLeftButtonDown += btnLastNext_MouseLeftButtonDown;

            txtPinCode.MouseEnter += (sender, e) => { MouseTool.ShowIBeam(); txtPinCode.Focus(); };
            txtPinCode.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            txtPinCode.KeyDown += txtPinCode_KeyDown;
            txtPinCode.Focus();

            SV.ScrollToVerticalOffset(1);
            SV.ScrollChanged += (sender, e) =>
                {
                    if (SV.CanContentScroll == false)
                    {
                        Show_HiddenMeetingDayList();
                    }
                };
           
            SV.MouseLeftButtonUp += (sender, e) =>
            {
                if (PaperLess_Emeeting.Properties.Settings.Default.AssemblyName.Contains("TPI4F") == false)
                {
                    string tagData = SV.Tag as string;

                    if (tagData!=null && tagData.Equals("MoveRight"))
                    {

                        MouseButtonEventArgs args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 100, MouseButton.Left);
                        args.RoutedEvent = UIElement.MouseLeftButtonDownEvent;
                        btnLast.RaiseEvent(args);

                        //UIElement container = VisualTreeHelper.GetParent(SV) as UIElement;
                        //Point MeetingDaySP_Point = MeetingDaySP.TranslatePoint(new Point(0, 0), container);
                        //Point SPP_Point = SPP.TranslatePoint(new Point(0, 0), container);

                        //double MeetingDaySP_From = MeetingDaySP_Point.X;
                        //double MeetingDaySP_To = MeetingDaySP_Point.X - SPP_Point.X;

                        //double SPP_From = SPP_Point.X;
                        //double SPP_To = MeetingDaySP_Point.X;

                        //TranslateTransform trans = new TranslateTransform();
                        //MeetingDaySP.RenderTransform = trans;
                        //DoubleAnimation ani = new DoubleAnimation(MeetingDaySP_From, MeetingDaySP_To, TimeSpan.FromMilliseconds(1000));
                        //trans.BeginAnimation(TranslateTransform.XProperty, ani);

                        //TranslateTransform trans = new TranslateTransform();
                        //SPP.RenderTransform = trans;
                        //DoubleAnimation ani = new DoubleAnimation(SPP_From, SPP_To, TimeSpan.FromMilliseconds(1000));
                        //trans.BeginAnimation(TranslateTransform.XProperty, ani);

                    }


                    if (tagData != null && tagData.Equals("MoveLeft"))
                    {
                        MouseButtonEventArgs args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 100, MouseButton.Left);
                        args.RoutedEvent = UIElement.MouseLeftButtonDownEvent;
                        btnNext.RaiseEvent(args);

                    }
                }
            };
            
            //SV.MouseWheel += (sender, e) =>
            //    {
            //        MessageBox.Show("11");
            //    };
            //SV.ScrollChanged += (sender, e) => 
            //{
            //    //if (SV.CanContentScroll == true)
            //    //{
            //        if (e.VerticalOffset == 0 && e.VerticalChange <0 )
            //        {
            //            //MessageBox.Show(e.VerticalOffset.ToString());
            //            IEnumerable<MeetingDayList> list =MeetingDaySP.Children.OfType<MeetingDayList>();
            //            foreach(MeetingDayList mdl in list)
            //            {
            //                mdl.Visibility=Visibility.Visible;
            //            }
            //        }
            //    //}
            //};

            //this.MouseWheel += MeetingListCT_MouseWheel;



        }

        //private void SwipeLeft()
        //{
        //    txtPinCode.Text = "Left";
        //}

        //private void SwipeRight()
        //{
        //    txtPinCode.Text = "Right";
        //}

       

        private void Show_HiddenMeetingDayList()
        {
            if (SV.VerticalOffset == 0 || (btnShowMeetingRooms!=null && btnShowMeetingRooms.Visibility == Visibility.Collapsed) ) 
            {
                IEnumerable<MeetingDayList> list = MeetingDaySP.Children.OfType<MeetingDayList>();
                if (list != null)
                {
                    IEnumerable<Button> btnS = MeetingDaySP.Children.OfType<Button>();
                    foreach (Button btn in btnS)
                    {
                        btn.Visibility = Visibility.Collapsed;
                    }
                    foreach (MeetingDayList mdl in list)
                    {
                        mdl.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private void txtPinCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (txtPinCode.Visibility == Visibility.Visible && txtPinCode.Text.Equals("")==false)
                    CallAddNewMeeting();
            }
        }

        private void btnAdd_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            CallAddNewMeeting();
            
        }

        private void CallAddNewMeeting()
        {
            MouseTool.ShowLoading();

            string txtPinCode = "";
            //在這裡show出視窗
            if (PaperLess_Emeeting.Properties.Settings.Default.IsNewMeeting_PopupDialog == true)
            {
                ConfirmWindow cw = new ConfirmWindow();
                if (cw.ShowDialog() == true)
                {
                    txtPinCode = cw.tbPinCode.Text.Trim();
                }
                else
                {
                    return;
                }
            }
            else
            {
                txtPinCode = this.txtPinCode.Text.Trim();
            }

            if (txtPinCode.Equals(""))
            {
                AutoClosingMessageBox.Show(string.Format("請先輸入{0}識別碼", PaperLess_Emeeting.Properties.Settings.Default.CourseOrMeeting_String));
                return;
            }

            // 非同步POST方法
            GetNewMeeting.AsyncPOST(UserID, txtPinCode, (XmlDataString) => { GetNewMeeting_DoAction(XmlDataString); });
        }

        private void GetNewMeeting_DoAction(string dataString)
        {
            // 先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                this.Dispatcher.BeginInvoke(new Action<string>(GetNewMeeting_DoAction), dataString);
            }
            else
            {
                try
                {
                    string CourseOrMeeting_String = PaperLess_Emeeting.Properties.Settings.Default.CourseOrMeeting_String;
                    XDocument xml = null;
                    string State = "";
                    try
                    {
                        xml = XDocument.Parse(dataString);
                        State = xml.Element("User").Attribute("State").Value.Trim();
                    }
                    catch (Exception ex)
                    {
                        LogTool.Debug(ex);
                    }
                    switch (State)
                    {
                        case "0":
                            string NewAddMeetingID = xml.Element("User").Element("MeetingData").Attribute("ID").Value.Trim();
                            string BeginTime = xml.Element("User").Element("MeetingData").Attribute("BeginTime").Value.Trim();
                            DateTime date = DateTime.Now;
                            bool IsValid = DateTime.TryParse(BeginTime, out date);
                            if (IsValid == false)
                                date = DateTime.Now;
                            // 先做UI，再把按鈕的JSON存下來
                            //string SQL = @"update NowLogin Set MeetingListDate=@1,NewAddMeetingID=@2";//,HomeUserButtonAryJSON=@2
                            //int success = MSCE.ExecuteNonQuery(SQL, date.ToString("yyyy/MM/dd"),NewAddMeetingID);//, HomeUserButtonAryJSON);
                            //if (success < 1)
                            //    LogTool.Debug(new Exception(@"DB失敗: " + SQL));
                            this.NewAddMeetingID = NewAddMeetingID;
                            // 非同步POST方法
                            MouseTool.ShowLoading();
                            //GetUserData.AsyncPOST(UserID, UserPWD
                            //   , date
                            //   , (userObj, dateTime) => GetUserData_DoAction(userObj, dateTime));

                            if (NetworkTool.CheckNetwork() > 0)
                            {
                                GetUserData.AsyncPOST(UserID, UserPWD
                                                       , date
                                                       , (userObj, dateTime) => GetUserData_DoAction(userObj, dateTime));
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

                            AutoClosingMessageBox.Show(string.Format("成功加入{0}", CourseOrMeeting_String));

                            //重整列表
                            break;
                        case "1":
                            //AutoClosingMessageBox.Show(string.Format("該機關非{0}人員", CourseOrMeeting_String));
                            AutoClosingMessageBox.Show(string.Format("本{0}未邀請貴機關單位參與", CourseOrMeeting_String));
                            //AutoClosingMessageBox.Show("該機關非與會人員");
                            break;
                        case "2":
                            AutoClosingMessageBox.Show("已加入過");
                            break;
                        case "3":
                            AutoClosingMessageBox.Show(string.Format("{0}不存在", CourseOrMeeting_String));
                            break;
                        case "4":
                            AutoClosingMessageBox.Show(string.Format("{0}尚未發佈", CourseOrMeeting_String));
                            break;
                        case "5":
                            AutoClosingMessageBox.Show("無此使用者");
                            break;
                        case "6":
                            AutoClosingMessageBox.Show("加入失敗");
                            break;
                        case "7":
                            AutoClosingMessageBox.Show("機密會議");
                            break;
                        case "8":
                            AutoClosingMessageBox.Show("會議已取消");
                            break;
                        default:
                            AutoClosingMessageBox.Show("新增錯誤，請聯絡系統管理人員");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    AutoClosingMessageBox.Show("新增錯誤，請聯絡系統管理人員");
                    LogTool.Debug(ex);
                }
                txtPinCode.Text = "";
                MouseTool.ShowArrow();
            }
        }

        // 
        private void btnLastNext_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SV.ScrollToVerticalOffset(1);
            Image img=sender as Image;

            if (img.Name.Equals("btnLast"))
                MeetingListDate = MeetingListDate.AddMonths(-1);
            else
                MeetingListDate = MeetingListDate.AddMonths(1);


            // 非同步POST方法
            MouseTool.ShowLoading();

           
            //檢查是否有網路連線
            Network.HttpRequest hr = new Network.HttpRequest();
            if (NetworkTool.CheckNetwork() > 0)
            {
                Task.Factory.StartNew(() =>
                {
                    //無快取機制
                    //GetUserData.AsyncPOST(UserID, UserPWD
                    //                          , MeetingListDate
                    //                          , (userObj, dateTime) => GetUserData_DoAction(userObj, dateTime));

                    //有快取機制
                    if (PreLoadLastNextMonthDict.ContainsKey(MeetingListDate) == true)
                    {
                        GetUserData_DoAction(PreLoadLastNextMonthDict[MeetingListDate], MeetingListDate);
                        //預載上一個月和下一個月
                        PreLoadLastNextMonth();
                    }
                    else
                    {
                        GetUserData.AsyncPOST(UserID, UserPWD
                                          , MeetingListDate
                                          , (userObj, dateTime) =>
                                          {
                                              GetUserData_DoAction(userObj, dateTime);
                                              //預載上一個月和下一個月
                                              PreLoadLastNextMonth();
                                          });
                    }
                });
                //}).ContinueWith(task =>
                //{
                //    //預載上一個月
                //    //Thread.Sleep(100);
                //    //GetUserData.AsyncPOST(UserID, UserPWD
                //    //                                 , MeetingListDate.AddMonths(-1)
                //    //                                 , (userObj, dateTime) => { LastNextDict[dateTime] = userObj; });

                //}).ContinueWith(task =>
                //{
                //    //預載下一個月
                //    //Thread.Sleep(100);
                //    //GetUserData.AsyncPOST(UserID, UserPWD
                //    //                                 , MeetingListDate.AddMonths(1)
                //    //                                 , (userObj, dateTime) => { LastNextDict[dateTime] = userObj; });
                //});
            }
            else
            {
                //DB查詢日期
                DataTable dt = MSCE.GetDataTable("select UserJson from UserData where UserID =@1 and ListDate=@2" 
                                                 , UserID
                                                 , DateTool.MonthFirstDate(MeetingListDate).ToString("yyyyMMdd"));

                User user =new User();
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
           
               //, () => { this.Dispatcher.BeginInvoke(new Action(() => { AutoClosingMessageBox.Show("無法取得資料，請稍後再試"); })); });


            #region 同步POST方法
            //User user=GetUserData.POST(UserID, UserPWD,
            //                           DateTool.MonthFirstDate(MeetingListDate).ToString("yyyyMMdd"),
            //                           DateTool.MonthLastDate(MeetingListDate).ToString("yyyyMMdd"));
            //if (user != null)
            //{
            //    if (user.MeetingList.Length < 1)
            //        txtNothing.Visibility = Visibility.Visible;
            //    InitUI(user.MeetingList, MeetingListDate);
            //    // 會議列表的上下一頁不要複寫Buton的JSON了
            //    // HomeUserButtonAryJSON = JsonConvert.SerializeObject(user.EnableButtonList);
            //}
            //else
            //{
            //    AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
            //}
            #endregion

            // 先做UI，再把按鈕的JSON存下來
            string SQL = @"update NowLogin Set MeetingListDate=@1";//,HomeUserButtonAryJSON=@2
            int success = MSCE.ExecuteNonQuery(SQL, MeetingListDate.ToString("yyyy/MM/dd"));//, HomeUserButtonAryJSON);
            if (success < 1)
                LogTool.Debug(new Exception(@"DB失敗: " + SQL));

          
        }


        private void GetUserData_DoAction(User user, DateTime date)
        {
             // 先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                this.Dispatcher.BeginInvoke(new Action<User, DateTime>(GetUserData_DoAction), user, date);
            }
            else
            {
                if (user != null)
                {
                    //if (user.MeetingList.Length < 1)
                    //    txtNothing.Visibility = Visibility.Visible;
                    InitUI(user.MeetingList, date);

                    DataTable dt = MSCE.GetDataTable("select ListDate from UserData where UserID =@1 and ListDate =@2"
                                                    , user.ID
                                                    , DateTool.MonthFirstDate(date).ToString("yyyyMMdd"));

                    if (dt.Rows.Count > 0)
                    {
                        MSCE.ExecuteNonQuery(@"UPDATE [UserData] SET 
                                                 [ListDate] = @1
		                                        ,[UserJson] = @2
		                                         where UserID = @3 and ListDate =@4"
                                   , DateTool.MonthFirstDate(date).ToString("yyyyMMdd")
                                   , JsonConvert.SerializeObject(user)
                                   , user.ID
                                   , DateTool.MonthFirstDate(date).ToString("yyyyMMdd"));
                    }
                    else
                    {
                        MSCE.ExecuteNonQuery(@"INSERT INTO [UserData] ([UserID],[ListDate],UserJson)
                                                            VALUES (@1,@2,@3)"
                                               , user.ID
                                               , DateTool.MonthFirstDate(date).ToString("yyyyMMdd")
                                               , JsonConvert.SerializeObject(user));
                    }

                    // 會議列表的上下一頁不要複寫Buton的JSON了
                    // HomeUserButtonAryJSON = JsonConvert.SerializeObject(user.EnableButtonList);
                }
                else
                {
                    AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
                }
                MouseTool.ShowArrow();
            }
        }

        private void InitUI(UserMeeting[] userMeetingAry, DateTime date)
        {
            string CourseOrMeeting_String= PaperLess_Emeeting.Properties.Settings.Default.CourseOrMeeting_String;
            txtNothing.Text = string.Format("本月無{0}", CourseOrMeeting_String);
            txtPinCodeHint.Text = string.Format("請輸入{0}識別碼", CourseOrMeeting_String);
            btnAddHint.Text = string.Format("加入{0}", CourseOrMeeting_String);
            btnAddHint.Visibility = Visibility.Visible;

            if (userMeetingAry.Length > 0)
                txtNothing.Visibility = Visibility.Collapsed;
            else
                txtNothing.Visibility = Visibility.Visible;

            MeetingListDate = date;
            txtCount.Text = string.Format("共 {0} 個{1}", userMeetingAry.Length.ToString(), CourseOrMeeting_String);
            txtDate.Text = date.ToString("yyyy年MM月");

            if (PaperLess_Emeeting.Properties.Settings.Default.IsNewMeeting_PopupDialog == false)
            {
                txtPinCodeHint.Visibility = Visibility.Visible;
                txtPinCode.Visibility = Visibility.Visible;
            }
           

            Task.Factory.StartNew(() =>
                     {
                         Dictionary<DateTime, List<UserMeeting>> dict =new Dictionary<DateTime,List<UserMeeting>>();

                         //dict = GetUserMeetingDict_ByOrder(userMeetingAry, PaperLess_Emeeting.Properties.Settings.Default.UserMeeting_Reverse);

                         if (PaperLess_Emeeting.Properties.Settings.Default.UserMeeting_Reverse == true)
                         {

                             dict = userMeetingAry
                                                   .OrderBy(item => DateTool.StringToDate(item.BeginTime))
                                                   .GroupBy(item => DateTool.StringToDate(item.BeginTime).Date)
                                                   .Reverse() // 倒著排序，最新的在最上面
                                                   .ToDictionary(IGrouping => IGrouping.Key, IGrouping => IGrouping.ToList());
                         }
                         else
                         {
                             dict = userMeetingAry
                                                 .OrderBy(item => DateTool.StringToDate(item.BeginTime))
                                                 .GroupBy(item => DateTool.StringToDate(item.BeginTime).Date)
                                                  //.Reverse() // 倒著排序，最新的在最上面
                                                 .ToDictionary(IGrouping => IGrouping.Key, IGrouping => IGrouping.ToList());
                         }


                         //改成用DispatcherPriority，因為如果快速換月份
                         //一直跑UI，馬上又丟棄太浪費資源
                         //改成有空閒再跑會議日期就好
                         //不過後來想一想，會議日期先跑出來
                         //會有馬上就撈到資料的感覺，停頓感比較小，所以再改回去。
                         //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action(() =>
                         this.Dispatcher.BeginInvoke(new Action(() =>
                               {

                                   try
                                   {
                                       MeetingDaySP.Children.Clear();


                                       ////添加一些空白會議
                                       //dict.Add(new DateTime(2001 ,1 ,1), new List<UserMeeting>());
                                       //dict.Add(new DateTime(2002 ,1 ,1), new List<UserMeeting>());
                                       //dict.Add(new DateTime(2003, 1, 1), new List<UserMeeting>());
                                       //dict.Add(new DateTime(2004, 1, 1), new List<UserMeeting>());
                                       //dict.Add(new DateTime(2005, 1, 1), new List<UserMeeting>());
                                       //dict.Add(new DateTime(2006, 1, 1), new List<UserMeeting>());
                                       //dict.Add(new DateTime(2007, 1, 1), new List<UserMeeting>());
                                       //dict.Add(new DateTime(2008, 1, 1), new List<UserMeeting>());
                                       //dict.Add(new DateTime(2009, 1, 1), new List<UserMeeting>());

                                       if (PaperLess_Emeeting.Properties.Settings.Default.UserMeeting_Reverse == false)
                                       {
                                           foreach (KeyValuePair<DateTime, List<UserMeeting>> item in dict)
                                           {
                                               MeetingDayList meetingDayList = new MeetingDayList(UserID, UserPWD, item.Key, item.Value as List<UserMeeting>, this.Home_Change2MeetingDataCT_Event, new MeetingList_Show_HiddenMeetingDayList_Function(Show_HiddenMeetingDayList), NewAddMeetingID);
                                               MeetingDaySP.Children.Add(meetingDayList);
                                           }
                                       }
                                       else
                                       {
                                           bool HasToday = dict.Any(x => x.Key.Day == DateTime.Now.Day);
                                           int NearByTodayIndex = 0;

                                           if (MeetingListDate.Month == DateTime.Now.Month)
                                           {
                                               if (HasToday == true)
                                               {
                                                   NearByTodayIndex = dict.ToList().FindIndex(x => x.Key.Day == DateTime.Now.Day);
                                               }
                                               else
                                               {
                                                   //if (NearByTodayIndex > 0)
                                                   //    NearByTodayIndex--;
                                                   Dictionary<DateTime, List<UserMeeting>> tempDict = new Dictionary<DateTime, List<UserMeeting>>(dict);
                                                   tempDict.Add(DateTime.Now, new List<UserMeeting>());
                                                   NearByTodayIndex = tempDict.OrderBy((x) => x.Key.Day).ToList().FindIndex(x => x.Key.Day == DateTime.Now.Day);


                                                   //if (NearByTodayIndex == dict.Count - 1)
                                                   //{
                                                   //    NearByTodayIndex--;
                                                   //}
                                               }
                                           }

                                           MeetingDayList MeetingDayList_ToScroll = null;
                                           int i = 0;
                                           //Button btnShowMeetingRooms = null;

                                           foreach (KeyValuePair<DateTime, List<UserMeeting>> item in dict)
                                           {
                                               MeetingDayList meetingDayList = new MeetingDayList(UserID, UserPWD, item.Key, item.Value as List<UserMeeting>, this.Home_Change2MeetingDataCT_Event, new MeetingList_Show_HiddenMeetingDayList_Function(Show_HiddenMeetingDayList), NewAddMeetingID);
                                               MeetingDaySP.Children.Add(meetingDayList);


                                               if (i == NearByTodayIndex || NearByTodayIndex == dict.Count - 1 || int.Parse(meetingDayList.date.ToString("yyyyMMdd")) <= int.Parse(DateTime.Now.ToString("yyyyMMdd")))
                                               {
                                                   MeetingDayList_ToScroll = meetingDayList;
                                               }
                                               else if (MeetingDayList_ToScroll == null)
                                               {
                                                   meetingDayList.Visibility = Visibility.Collapsed;

                                                   if (btnShowMeetingRooms == null)
                                                   {
                                                       btnShowMeetingRooms = new Button()
                                                       {
                                                           Content = "^",
                                                           Height = 0, //現在已經隱藏起來了，需要的話把高度調高
                                                           BorderThickness = new Thickness(0),
                                                           Background = new SolidColorBrush(Colors.Yellow)
                                                       };
                                                       btnShowMeetingRooms.Margin = new Thickness(12, 0, 12, 0);
                                                       btnShowMeetingRooms.Click += (sender, e) =>
                                                       {
                                                           //AutoClosingMessageBox.Show("xxx");
                                                           btnShowMeetingRooms.Visibility = Visibility.Collapsed;
                                                           Show_HiddenMeetingDayList();
                                                       };
                                                       btnShowMeetingRooms.MouseEnter += (sender, e) =>
                                                       {
                                                           btnShowMeetingRooms.Visibility = Visibility.Collapsed;
                                                           Show_HiddenMeetingDayList();
                                                       };
                                                       MeetingDaySP.Children.Add(btnShowMeetingRooms);
                                                   }
                                               }
                                               i++;
                                           }

                                           //double height = System.Windows.SystemParameters.PrimaryScreenHeight;
                                           Rectangle rect = new Rectangle();
                                           rect.Height = 1080;
                                           //rect.Height = height - MeetingDaySP.Children.Count * 125;
                                           rect.Margin = new Thickness(12, 0, 12, 0);
                                           rect.Fill = new SolidColorBrush(Colors.Transparent);
                                           MeetingDaySP.Children.Add(rect);

                                       }

                                   }
                                   catch(Exception ex)
                                   {
                                       LogTool.Debug(ex);
                                   }
                                  

                               }));
                     });
        }

        //private void meetingDayList_MouseEnter(object sender, MouseEventArgs e)
        //{
        //    MeetingDayList item = (MeetingDayList)sender;
        //    //foreach(MeetingDayList item in MeetingDaySP.Children.OfType<MeetingDayList>())
        //    //{
        //        // How to scroll the uiElement to the mouse position?
        //        //var sv = (ScrollViewer)Template.FindName("PART_MyScrollViewer", this); // If you do not already have a reference to it somewhere.
        //        //var ip = (ItemsPresenter)SV.Content;
        //        var point = item.TranslatePoint(new Point() - (Vector)e.GetPosition(SV), ip);
        //        SV.ScrollToVerticalOffset(point.Y + (item.ActualHeight / 2));
        //        //break;
                    
        //    //}
           
        //}

        private Dictionary<DateTime, List<UserMeeting>> GetUserMeetingDict_ByOrder(UserMeeting[] userMeetingAry, bool IsReverse)
        {
            Dictionary<DateTime, List<UserMeeting>> dict = new Dictionary<DateTime, List<UserMeeting>>();

            if (IsReverse == true)
            {
                dict = userMeetingAry
                                      .OrderBy(item => DateTool.StringToDate(item.BeginTime))
                                      .GroupBy(item => DateTool.StringToDate(item.BeginTime).Date)
                                      .Reverse() // 倒著排序，最新的在最上面
                                      .ToDictionary(IGrouping => IGrouping.Key, IGrouping => IGrouping.ToList());
            }
            else
            {
                dict = userMeetingAry
                                    .OrderBy(item => DateTool.StringToDate(item.BeginTime))
                                    .GroupBy(item => DateTool.StringToDate(item.BeginTime).Date)
                                    //.Reverse() // 倒著排序，最新的在最上面
                                    .ToDictionary(IGrouping => IGrouping.Key, IGrouping => IGrouping.ToList());
            }


            return dict;
        }

    }
}