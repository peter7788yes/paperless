using Newtonsoft.Json;
using PaperLess_Emeeting.App_Code.MessageBox;
using PaperLess_Emeeting.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Deployment.Application;
using System.Data;
using PaperLess_ViewModel;
using System.Threading.Tasks;
using ModernWPF.Win8TouchKeyboard.Desktop;
using System.Windows.Interop;
using PaperLess_Emeeting.App_Code.ClickOnce;

namespace PaperLess_Emeeting
{
    /// <summary>
    /// Login.xaml 的互動邏輯
    /// </summary>
    public partial class Login : Window
    {
        private bool canLogin = true;
        private bool RemeberLogin = false;
        public string UserID { get; set; }
        public Login()
        {
            MouseTool.ShowLoading();
            App.IsChangeWindow = false;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();

            // Disables inking in the WPF application and enables us to track touch events to properly trigger the touch keyboard
            InkInputHelper.DisableWPFTabletSupport();

            this.Loaded += Login_Loaded;
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
            //InitSelectDB();
            //InitUI();
            //InitEvent();

            //for test
            //NetworkTool.GetNetWork();

        }
        private void CopyLog()
        {
            if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
            {
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        string AppDir = AppDomain.CurrentDomain.BaseDirectory;
                        string dir = Directory.GetDirectories(System.IO.Path.Combine(AppDir, "Logs")).OrderByDescending(f => f).First();
                        DirectoryTool.FullCopyDirectories(dir, ClickOnceTool.GetFilePath());
                    }
                    catch (Exception ex)
                    {
                        LogTool.Debug(ex);
                    }

                });
            }
        }

        private void Login_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Enables WPF to mark edit field as supporting text pattern (Automation Concept)
                //System.Windows.Automation.AutomationElement asForm = System.Windows.Automation.AutomationElement.FromHandle(new WindowInteropHelper(this).Handle);

                // Windows 8 API to enable touch keyboard to monitor for focus tracking in this WPF application
                //InputPanelConfigurationLib.InputPanelConfiguration inputPanelConfig = new InputPanelConfigurationLib.InputPanelConfiguration();
                //inputPanelConfig.EnableFocusTracking();
            }
            catch(Exception ex)
            {
            }

           Task.Factory.StartNew(() =>
           {
               InitSelectDB();
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

               //先發出一個request，加快登入時間
               //不然啟動時登入會很慢
               HttpTool.GetResponseLength(WsTool.GetUrl()+"/UserData?XmlDoc=");
           });
        }

        private void InitSelectDB()
        {
            DataTable dt = MSCE.GetDataTable("select UserID,RemeberLogin from NowLogin");
            if (dt.Rows.Count > 0)
            {
                UserID = dt.Rows[0]["UserID"].ToString().Trim();
                bool.TryParse(dt.Rows[0]["RemeberLogin"].ToString().Trim(), out RemeberLogin);
            }
        }

        private void InitUI()
        {
            if (PaperLess_Emeeting.Properties.Settings.Default.IsDebugMode == false)
                this.Title = Settings.Default.AppName;
            else
                this.Title = Settings.Default.AppName_Debug;
            

            imgHeader.Source = new BitmapImage(new Uri(PaperLess_Emeeting.Properties.Settings.Default.Login_Header_Image,UriKind.Relative));

            try
            {
                if (ApplicationDeployment.IsNetworkDeployed)
                {
                    var s = ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString().TrimEnd('0').TrimEnd('.');
                    var ary = s.Split('.');
                    txtVersion.Text = "版本: " + ary[0] + '.' + ary[1] + '.' + int.Parse(ary[2]).ToString("X2");
                }
                    //txtVersion.Text = "版本: " + ApplicationDeployment.CurrentDeployment.CurrentVersion.Major.ToString()+
                    //                             ApplicationDeployment.CurrentDeployment.CurrentVersion.Minor.ToString("00")+
                    //                             ApplicationDeployment.CurrentDeployment.CurrentVersion.Revision.ToString();
            }
            catch { }

            if (RemeberLogin == true)
            {
                tbUserID.Text = UserID;
                tbUserPWD.Password = "";
                cbRemeberLogin.IsChecked = true;

                if (tbUserID.Text.Length > 0)
                {
                    //tbUserID.CaretIndex = tbUserID.Text.Length;
                    //tbUserID.ScrollToEnd();
                    tbUserPWD.Focus();
                    btnUserIDClear.Visibility = Visibility.Visible;
                }
                else
                {
                    tbUserID.Focus();
                    btnUserIDClear.Visibility = Visibility.Collapsed;
                }

                if (tbUserPWD.Password.Length > 0)
                {
                    btnUserPWDClear.Visibility = Visibility.Visible;
                }
                else
                {
                    btnUserPWDClear.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                //if (PaperLess_Emeeting.Properties.Settings.Default.IsDebugMode == true)
                //{
                //    this.tbUserID.Text = PaperLess_Emeeting.Properties.Settings.Default.DebugUserID;
                //    this.tbUserPWD.Password = PaperLess_Emeeting.Properties.Settings.Default.DebugUserPWD;
                //}
                //else
                //{
                    tbUserID.Text = "";
                    tbUserPWD.Password = "";
                    cbRemeberLogin.IsChecked = false;
                //}
            }

            if (PaperLess_Emeeting.Properties.Settings.Default.HasRemeberLogin == true)
            {
                RemeberLoginDP.Visibility = Visibility.Visible;
            }

            if(PaperLess_Emeeting.Properties.Settings.Default.IsDebugMode==true)
            {
                imgBeta.Visibility = Visibility.Visible;
            }

            if (PaperLess_Emeeting.Properties.Settings.Default.AssemblyName.Contains("PaperLess_Emeeting_EDU"))
            {
                UserHint.Visibility=Visibility.Visible;
            }
        }

        private void InitEvent()
        {
            tbUserID.MouseEnter += (sender, e) => { if (canLogin == true) { MouseTool.ShowIBeam(); /*tbUserID.Focus();*/ } };
            tbUserID.MouseLeave += (sender, e) => { if (canLogin == true) MouseTool.ShowArrow(); };
            tbUserID.PreviewKeyDown += tbUserID_PreviewKeyDown;
            tbUserID.KeyDown += tbUserID_KeyDown;

            btnUserIDClear.MouseEnter += (sender, e) => { if (canLogin == true) { MouseTool.ShowHand(); /*tbUserID.Focus();*/ } };
            btnUserIDClear.MouseLeave += (sender, e) => { if (canLogin == true) MouseTool.ShowArrow(); };
            btnUserIDClear.Click += (sender, e) => { tbUserID.Text = ""; btnUserIDClear.Visibility = Visibility.Collapsed; };

            tbUserPWD.MouseEnter += (sender, e) => { if (canLogin == true) { MouseTool.ShowIBeam(); /*tbUserPWD.Focus();*/ } };
            tbUserPWD.MouseLeave += (sender, e) => { if (canLogin == true) MouseTool.ShowArrow(); };
            tbUserPWD.PreviewKeyDown += tbUserID_PreviewKeyDown;
            tbUserPWD.KeyDown += tbUserID_KeyDown;

            btnUserPWDClear.MouseEnter += (sender, e) => { if (canLogin == true) { MouseTool.ShowHand(); /*tbUserID.Focus();*/ } };
            btnUserPWDClear.MouseLeave += (sender, e) => { if (canLogin == true) MouseTool.ShowArrow(); };
            btnUserPWDClear.Click += (sender, e) => { tbUserPWD.Password = ""; btnUserPWDClear.Visibility = Visibility.Collapsed; };

            btnSubmit.MouseEnter += (sender, e) => { if (canLogin == true) MouseTool.ShowHand(); };
            btnSubmit.MouseLeave += (sender, e) => { if (canLogin == true) MouseTool.ShowArrow(); };
            btnSubmit.MouseLeftButtonDown +=btnSubmit_MouseLeftButtonDown;

            this.KeyDown += tbUserID_KeyDown;
         
        }

        private void tbUserID_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(10);
                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (tbUserID.Text.Length > 0)
                    {
                        btnUserIDClear.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        btnUserIDClear.Visibility = Visibility.Collapsed;
                    }

                    if (tbUserPWD.Password.Length > 0)
                    {
                        btnUserPWDClear.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        btnUserPWDClear.Visibility = Visibility.Collapsed;
                    }
                }));
            });
        }

        private void tbUserID_KeyDown(object sender, KeyEventArgs e)
        {
             if (e.Key == Key.Return)
             {
                  if (tbUserID.Text.Trim().Equals("")) { AutoClosingMessageBox.Show("請輸入帳號"); return; }
                  if (tbUserPWD.Password.Trim().Equals("")) { AutoClosingMessageBox.Show("請輸入密碼"); return; }
   
                  if (canLogin == true)
                        CallLigon();
             }


            

        }

        private void btnSubmit_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (canLogin == true)
               CallLigon();
        }

        private void CallLigon()
        {
            canLogin = false;
            MouseTool.ShowLoading();
            try
            {
       

                string UserID = tbUserID.Text.Trim();
                string UserPWD = tbUserPWD.Password.Trim();
                string UserDateBegin = DateTool.MonthFirstDate(DateTime.Today).ToString("yyyyMMdd"); //"20190101";
                string UserDateEnd = DateTool.MonthFirstDate(DateTime.Today.AddMonths(1)).ToString("yyyyMMdd"); //"20190101";


                //Network.HttpRequest hr = new Network.HttpRequest();
                if (NetworkTool.CheckNetwork() > 0)
                {
                    string url = WsTool.GetUrl();
                    string xmlData = "<?xml version=\"1.0\"?><UserInfo><UserID><![CDATA[{0}]]></UserID><UserPW><![CDATA[{1}]]></UserPW><UserDevice>1</UserDevice><UserDateBegin>{2}</UserDateBegin><UserDateEnd>{3}</UserDateEnd></UserInfo>";
                    xmlData =string.Format(xmlData, UserID, UserPWD, UserDateBegin, UserDateEnd);
                    Dictionary<string, string> postData=new Dictionary<string, string>();
                    postData["XmlDoc"]=xmlData;
                    //LogTool.Debug(xmlData);
                    HttpWebRequest request = HttpTool.GetHttpWebRequest(url + "/UserData", "POST", postData);

                    // init your request...then:   //lambda
                    //呼叫方法，第二個是lambda匿名方法
                    DoWithResponse(request, (response) =>
                    {
                        //這裡面是不同的執行序;
                        User user = null;

                        try
                        {
                            string data = new StreamReader(response.GetResponseStream()).ReadToEnd();
                            //LogTool.Debug(data);
                            user = XmlHelper.XmlDeserialize<User>(data, Encoding.UTF8);

                        
                        }
                        catch (Exception ex)
                        {
                            this.Dispatcher.BeginInvoke(new Action(() => { AutoClosingMessageBox.Show("登入失敗，請重新登入"); }));
                            LogTool.Debug(ex);
                        }
                        this.Dispatcher.BeginInvoke(new Action<User>(CheckLogin), user);
                    });
                }
                else
                {
                    //DB查詢登入
                    DataTable dt = MSCE.GetDataTable("select UserID from LoginInfo where UserID =@1" , UserID);
                    if (dt.Rows.Count > 0)
                    {
                        dt = MSCE.GetDataTable("select UserJson from LoginInfo where UserID =@1 and UserPWD=@2"
                                              , UserID
                                              , UserPWD);

                        if (dt.Rows.Count > 0)
                        {
                            User user =JsonConvert.DeserializeObject<User>(dt.Rows[0]["UserJson"].ToString());
                            string HomeUserButtonAryJSON = JsonConvert.SerializeObject(user.EnableButtonList);
                            string UTC = user.UTC.ToString();
                            long deltaUTC = 0;
                            try
                            {
                                deltaUTC = DateTool.GetCurrentTimeInUnixMillis() - long.Parse(UTC);
                            }
                            catch (Exception ex)
                            {
                                LogTool.Debug(ex);
                            }
                            string SQL = @"Update NowLogin set UserID=@1,UserName=@2,UserPWD=@3,MeetingListDate=getdate(),HomeUserButtonAryJSON=@4,UserEmail=@5,UTC=@6,DeltaUTC=@7,RemeberLogin=@8";
                            int success = MSCE.ExecuteNonQuery(SQL
                                                             , user.ID
                                                             , user.Name
                                                             , tbUserPWD.Password.Trim()
                                                             , HomeUserButtonAryJSON
                                                             , user.Email
                                                             , DateTool.GetCurrentTimeInUnixMillis().ToString()
                                                             , deltaUTC.ToString()
                                                             , cbRemeberLogin.IsChecked == true ? "true" : "false");


                            if (success < 1)
                            {
                                LogTool.Debug(new Exception(@"DB失敗: " + SQL));
                                return;
                            }
                            this.Dispatcher.BeginInvoke(new Action<User>(CheckLogin), user);
                        }
                        else
                        {
                            MouseTool.ShowArrow();
                            AutoClosingMessageBox.Show("您的密碼錯誤");
                            canLogin = true;
                        }
                    }
                    else
                    {
                        MouseTool.ShowArrow();
                        AutoClosingMessageBox.Show("無此使用者帳號，請重新輸入");
                        canLogin = true;
                    }

                  
                }
               
            }
            catch(Exception ex)
            {
                MouseTool.ShowArrow();
                AutoClosingMessageBox.Show("登入失敗");
                canLogin = true;
            }
          
        }

        private void CheckLogin(User user)
        {
            try
            {
                if (user != null)
                {
                    switch (user.State)
                    {
                        case "0":
                            string HomeUserButtonAryJSON = JsonConvert.SerializeObject(user.EnableButtonList);
                            string UTC = user.UTC == null ? DateTool.GetCurrentTimeInUnixMillis().ToString() : user.UTC.ToString();
                            long deltaUTC = 0;
                            try
                            {
                                deltaUTC = DateTool.GetCurrentTimeInUnixMillis() - long.Parse(UTC);
                            }
                            catch (Exception ex)
                            {
                                LogTool.Debug(ex);
                            }
                            string SQL = @"Update NowLogin set UserID=@1,UserName=@2,UserPWD=@3,MeetingListDate=getdate(),HomeUserButtonAryJSON=@4,UserEmail=@5,UTC=@6,DeltaUTC=@7,RemeberLogin=@8";
                            int success = MSCE.ExecuteNonQuery(SQL
                                                             , user.ID
                                                             , user.Name
                                                             , tbUserPWD.Password.Trim()
                                                             , HomeUserButtonAryJSON
                                                             , user.Email
                                                             , UTC
                                                             , deltaUTC.ToString()
                                                             , cbRemeberLogin.IsChecked==true ?"true":"false");


                                 if (success < 1)
                                 {
                                    LogTool.Debug(new Exception(@"DB失敗: " + SQL));
                                    return;
                                 }


                                 try
                                 {
                                     //DB操作更新
                                     DataTable dt = MSCE.GetDataTable("select UserID from LoginInfo where UserID =@1"
                                                         , user.ID);

                                     if (dt.Rows.Count > 0)
                                     {
                                         MSCE.ExecuteNonQuery(@"UPDATE [LoginInfo] SET 
                                                 [UserID] = @1
		                                        ,[UserPWD] = @2
                                                ,UserJson = @3
		                                         where UserID=@4"
                                                    , user.ID
                                                    , tbUserPWD.Password.Trim()
                                                    , JsonConvert.SerializeObject(user)
                                                    , user.ID);
                                     }
                                     else
                                     {
                                         MSCE.ExecuteNonQuery(@"INSERT INTO [LoginInfo] ([UserID],[UserPWD],UserJson)
                                                            VALUES (@1,@2,@3)"
                                                                , user.ID
                                                                , tbUserPWD.Password.Trim()
                                                                , JsonConvert.SerializeObject(user));
                                     }

                                     dt = MSCE.GetDataTable("select ListDate from UserData where UserID =@1 and ListDate =@2"
                                                        , user.ID
                                                        , DateTool.MonthFirstDate(DateTime.Now).ToString("yyyyMMdd"));

                                     if (dt.Rows.Count > 0)
                                     {
                                         MSCE.ExecuteNonQuery(@"UPDATE [UserData] SET 
                                                             [ListDate] = @1
		                                                    ,[UserJson] = @2
		                                                     where UserID = @3 and ListDate =@4"
                                                    , DateTool.MonthFirstDate(DateTime.Now).ToString("yyyyMMdd")
                                                    , JsonConvert.SerializeObject(user)
                                                    , user.ID
                                                    , DateTool.MonthFirstDate(DateTime.Now).ToString("yyyyMMdd"));
                                     }
                                     else
                                     {
                                         MSCE.ExecuteNonQuery(@"INSERT INTO [UserData] ([UserID],[ListDate],UserJson)
                                                                        VALUES (@1,@2,@3)"
                                                                , user.ID
                                                                , DateTool.MonthFirstDate(DateTime.Now).ToString("yyyyMMdd")
                                                                , JsonConvert.SerializeObject(user));
                                     }
                                 }
                                 catch(Exception ex)
                                 {
                                     LogTool.Debug(ex);
                                 }


                                 

                                 this.Hide();
                                 Home f2 = new Home(user, tbUserPWD.Password.Trim());
                                 f2.Show();
                                 App.IsChangeWindow = true;
                                 this.Close();
                           
                            break;
                        case "1":
                            AutoClosingMessageBox.Show("無此使用者帳號，請重新輸入");
                            break;
                        case "2":
                            AutoClosingMessageBox.Show("帳號密碼錯誤或帳號已被鎖定");
                            break;
                    }

                }
            }
            catch(Exception ex)
            {
                LogTool.Debug(ex);
            }
            canLogin = true;
            MouseTool.ShowArrow();
        }

       
        //傳入request 和一個處理response的方法
        //方法的格式為 function(HttpWebResponse response)
        //方法會傳入一個參數 function(HttpWebResponse response)
        private void DoWithResponse(HttpWebRequest request, Action<HttpWebResponse> responseAction)
        {
            //定義一個無名方法 function() 無參數
            //作用是發起BeginGetResponse(callback,使用者定義物件)
            //這裡的callback也是lambda匿名方法，傳入使用者定義物件，
            //(主要處理回傳的使用用者定義物件)代表已經取得response後應該要做的事情
            //取得response的資料後，利用callback去做事件處理
            //以上是包裝成非同步的一系列方法
            Action wrapperAction = () =>
            {
                //完整版
                request.BeginGetResponse(new AsyncCallback((iar) =>
                {
                    var response = (HttpWebResponse)((HttpWebRequest)iar.AsyncState).EndGetResponse(iar);
                    responseAction(response);
                }), request);

                //簡化版
                //request.BeginGetResponse(iar =>
                //{
                //    var response = (HttpWebResponse)((HttpWebRequest)iar.AsyncState).EndGetResponse(iar);
                //    responseAction(response);
                //}, request);
            };

            //開始把包裝後的方法做非同步操作
            //因為已經排入invoke當中
            //做完後記得結束invoke
            wrapperAction.BeginInvoke(new AsyncCallback((iar) =>
            {
                var action = (Action)iar.AsyncState;
                try
                {
                    action.EndInvoke(iar);
                }
                catch(Exception ex)
                {
                    //AutoClosingMessageBox.Show(ex.Message);
                    AutoClosingMessageBox.Show("登入失敗，請重新登入");
                    LogTool.Debug(ex);
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                                                canLogin = true;
                                                MouseTool.ShowArrow();
                    }));
                }
            }), wrapperAction);
        }
      
    }
}


