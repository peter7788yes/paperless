using PaperLess_Emeeting.App_Code.MessageBox;
using PaperLess_ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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

namespace PaperLess_Emeeting
{
    /// <summary>
    /// SignRow.xaml 的互動邏輯
    /// </summary>
    public partial class SignRow_Mix : UserControl
    {
        public SigninDataUser signinDataUser { get; set; }
        public Storyboard sb { get; set; }
        //public int index { get; set; }
        public MeetingUserType meetingUserType;
        public bool EnableTxtPLSSigned;
        public SignRow_Mix(SigninDataUser signinDataUser, MeetingUserType meetingUserType, bool EnableTxtPLSSigned)
        {
            //MouseTool.ShowLoading();
            InitializeComponent();
            //this.index = index;
            this.meetingUserType = meetingUserType;
            this.EnableTxtPLSSigned = EnableTxtPLSSigned;
            sb = (Storyboard)this.TryFindResource("sb");
            this.signinDataUser = signinDataUser;
            this.Loaded += SignRow_Loaded;
            //MouseTool.ShowArrow();
        }

        private void SignRow_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                InitSelectDB();
                // 這裡是 Room 畫面，優先權設定跟Row一樣為Background => 列舉值為 4。 所有其他非閒置作業都完成之後，就會處理作業。
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    InitUI();
                    InitEvent();
                }));
              
            });
           
        }

        private void InitSelectDB()
        {
        }

        private void InitEvent()
        {

            switch (meetingUserType)
            {
                case MeetingUserType.議事管理人員:
                    if (this.EnableTxtPLSSigned == true)
                    {
                        txtPLSSigned.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
                        txtPLSSigned.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
                        txtPLSSigned.MouseLeftButtonDown += txtUnSigned_MouseLeftButtonDown;

                        imgSignedPic.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
                        imgSignedPic.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
                        imgSignedPic.MouseLeftButtonDown += txtUnSigned_MouseLeftButtonDown;
                    }

                    break;
                case MeetingUserType.與會人員:
                    break;
                case MeetingUserType.代理人:
                    break;
                case MeetingUserType.其它:
                    break;
            }

         
          
        }

        private void txtUnSigned_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            string AllowIpRange = "";
            DataTable dt = MSCE.GetDataTable("select AllowIpRange from NowLogin");
            if (dt.Rows.Count > 0)
            {
                AllowIpRange = dt.Rows[0]["AllowIpRange"].ToString();
            }

            if (PaperLess_Emeeting.Properties.Settings.Default.HasIpRangeMode == true && AllowIpRange.Equals("") == false && IpTool.CheckInNowWifi(AllowIpRange) == false)
            {
                AutoClosingMessageBox.Show("您不在會議室範圍內，無法使用此功能");
                return;
            }

            if (PaperLess_Emeeting.Properties.Settings.Default.HasIpRangeMode == true && AllowIpRange.Equals("") == false && IpTool.CheckInNowWifi(AllowIpRange) == false)
            {
                AutoClosingMessageBox.Show("您不在會議室範圍內，無法使用此功能");
                return;
            }


            Home Home_Window = Application.Current.Windows.OfType<Home>().FirstOrDefault();
            if (Home_Window != null)
            {
                string DeptID = signinDataUser.DeptID == null ? "" : signinDataUser.DeptID;
                if (signinDataUser.ID.Trim().Equals("") == false)
                {
                    Home_Window.CC.Content = new SignPadCT(signinDataUser.ID, signinDataUser.Name, DeptID,signinDataUser.SignedPic, (x, y) => { Home_Window.CC.Content = new SignListCT_Mix(); });
                }
                else
                {
                    Home_Window.CC.Content = new SignPadCT("dept", string.Format("{0} 來賓", signinDataUser.Dept), DeptID, signinDataUser.SignedPic, (x, y) => { Home_Window.CC.Content = new SignListCT_Mix(); });
                }
            }
        }

        private void InitUI()
        {
            string SignListEmptyDash = PaperLess_Emeeting.Properties.Settings.Default.SignListEmptyDash;
            if (signinDataUser.Rank != null)
            {
                txtIndex.Text = signinDataUser.Rank.Equals("") ? SignListEmptyDash : signinDataUser.Rank;
            }
            else
            {
                txtIndex.Text = SignListEmptyDash;
            }

            txtUserName.Text = signinDataUser.Name.Equals("") ? SignListEmptyDash : signinDataUser.Name;
            txtDept.Text = signinDataUser.Dept.Equals("") ? SignListEmptyDash : signinDataUser.Dept;
            txtTitle.Text = signinDataUser.Title.Equals("") ? SignListEmptyDash : signinDataUser.Title;
            

            bool IsSigned=bool.Parse(signinDataUser.IsSigned.Equals("") ? "" : signinDataUser.IsSigned);
            string Attend=signinDataUser.Attend.Trim();
            string Agent = signinDataUser.AgentName.Trim();
            string imgUrl=signinDataUser.SignedPic.Trim();
            if (Attend.Equals("0") || Attend.Equals("2"))
            {
                if (Agent.Equals("")==false)
                    txtAgent.Text = string.Format("(指派代表，由{0}出席)", signinDataUser.AgentName);
                else
                    txtAgent.Text = "(請假)";
            }
            else
            {
                txtAgent.Visibility = Visibility.Collapsed;
            }


            GetHttpImage();
            //if (imgUrl.Equals("") == false)
            //{
            //    txtUserName.FontWeight = FontWeights.Bold;

            //    if (sb != null)
            //        sb.Begin();

            //    //imgSignedPic.Source = new BitmapImage(new Uri(imgUrl, UriKind.Absolute));

            //    //ThreadPool.QueueUserWorkItem(callback =>
            //    Task.Factory.StartNew(()=>
            //    {
            //        try
            //        {
            //            var webClient = new WebClient();
            //            //Wayne Add 20150423
            //            //一直使用快取，除非快取快取過期，在快取期間如果Server有變更會抓不到
            //            //webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.CacheIfAvailable);
            //            //會判斷伺服器是否有更新版來使用快取
            //            //如果沒有更新版則使用快取
            //            //webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.Default);
            //            //Stopwatch sw = new Stopwatch();
            //            //sw.Reset();
            //            //sw.Start();
            //            //var s=HttpTool.GetResponseLength(imgUrl);
            //            //sw.Stop();
            //            //Console.WriteLine(s + "," + sw.ElapsedMilliseconds);
            //            //sw.Reset();
            //            webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.Revalidate);
            //            var buffer = webClient.DownloadData(imgUrl);
            //            var bitmapImage = new BitmapImage();

            //            using (var stream = new MemoryStream(buffer))
            //            {
            //                //初始化圖片到記憶體
            //                bitmapImage.BeginInit();
            //                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            //                bitmapImage.StreamSource = stream;
            //                bitmapImage.EndInit();
            //                bitmapImage.Freeze();
            //            }

            //            //this.Dispatcher.BeginInvoke((Action)(() => imgSignedPic.Source = bitmapImage));

            //            this.Dispatcher.BeginInvoke((Action)(() => 
            //            {
                            
            //                if (sb != null)
            //                    sb.Stop();
            //                imgSignedPic.Source = bitmapImage; 
            //            }));
            //        }
            //        catch(Exception ex)
            //        {
            //            LogTool.Debug(ex);
            //        }
            //    });
            //}
            //else
            //{
            //    if (EnableTxtPLSSigned == true && meetingUserType == MeetingUserType.議事管理人員)
            //    {
            //        txtPLSSigned.Visibility = Visibility.Visible;
            //    }
            //    else 
            //    {
            //        txtUnSigned.Visibility = Visibility.Visible;
            //    }
            //    //txtUnSigned.Visibility = Visibility.Visible;
            //}
             
            
            //if (IsSigned == true)
            //{
            //    txtUserName.FontWeight = FontWeights.Bold;

            //    if (sb != null)
            //        sb.Begin();
            //}
            //else
            //{
            //    txtUnSigned.Visibility = Visibility.Visible;
            //}

        }

        private void imgSignedPic_SizeChanged(object sender, SizeChangedEventArgs e)
        {
           //GetHttpImage();
        }

        private void GetHttpImage()
        {
            string imgUrl = signinDataUser.SignedPic.Trim();
            if (imgUrl.Equals("") == false)
            {
                txtUserName.FontWeight = FontWeights.Bold;

                if (sb != null)
                    sb.Begin();

                //imgSignedPic.Source = new BitmapImage(new Uri(imgUrl, UriKind.Absolute));

                //ThreadPool.QueueUserWorkItem(callback =>
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        var webClient = new WebClient();
                        //Wayne Add 20150423
                        //一直使用快取，除非快取快取過期，在快取期間如果Server有變更會抓不到
                        //webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.CacheIfAvailable);
                        //會判斷伺服器是否有更新版來使用快取
                        //如果沒有更新版則使用快取
                        //webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.Default);
                        //Stopwatch sw = new Stopwatch();
                        //sw.Reset();
                        //sw.Start();
                        //var s=HttpTool.GetResponseLength(imgUrl);
                        //sw.Stop();
                        //Console.WriteLine(s + "," + sw.ElapsedMilliseconds);
                        //sw.Reset();
                        webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.Revalidate);
                        var buffer = webClient.DownloadData(imgUrl);
                        var bitmapImage = new BitmapImage();

                        using (var stream = new MemoryStream(buffer))
                        {
                            //初始化圖片到記憶體
                            bitmapImage.BeginInit();
                            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            bitmapImage.StreamSource = stream;
                            bitmapImage.EndInit();
                            bitmapImage.Freeze();
                        }

                        //this.Dispatcher.BeginInvoke((Action)(() => imgSignedPic.Source = bitmapImage));

                        this.Dispatcher.BeginInvoke((Action)(() =>
                        {

                            if (sb != null)
                                sb.Stop();
                            imgSignedPic.Source = bitmapImage;
                        }));
                    }
                    catch (Exception ex)
                    {
                        LogTool.Debug(ex);
                    }
                });
            }
            else
            {
                if (EnableTxtPLSSigned == true && meetingUserType == MeetingUserType.議事管理人員)
                {
                    txtPLSSigned.Visibility = Visibility.Visible;
                }
                else
                {
                    txtUnSigned.Visibility = Visibility.Visible;
                }
                //txtUnSigned.Visibility = Visibility.Visible;
            }
        }

       
    }
}
