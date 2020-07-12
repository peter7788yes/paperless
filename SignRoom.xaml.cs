using PaperLess_Emeeting.App_Code.MessageBox;
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

namespace PaperLess_Emeeting
{
    /// <summary>
    /// SignRoom.xaml 的互動邏輯
    /// </summary>
    public partial class SignRoom : UserControl
    {
        public SigninDataUser signinDataUser { get; set; }
        public event Home_ChangeTogSignPadCT_Function Home_ChangeTogSignPadCT_Event;
        //public event Home_GoBackTogSignPictureCT_Function Home_GoBackTogSignPictureCT_Event;
        public Storyboard sb { get; set; }

        public SignRoom(SigninDataUser signinDataUser, Home_ChangeTogSignPadCT_Function callback1)
        {
            //MouseTool.ShowLoading();
            InitializeComponent();
            sb = (Storyboard)this.TryFindResource("sb");
            this.signinDataUser = signinDataUser;
            this.Home_ChangeTogSignPadCT_Event += callback1;
            //this.Home_GoBackTogSignPictureCT_Event += callback2;
            this.Loaded += SignRoom_Loaded;
            //MouseTool.ShowArrow();
        }

        //public SignRoom(SigninDataUser signinDataUser,Home_ChangeTogSignPadCT_Function callback1,Home_GoBackTogSignPictureCT_Function callback2)
        //{
        //    //MouseTool.ShowLoading();
        //    InitializeComponent();
        //    sb = (Storyboard)this.TryFindResource("sb");
        //    this.signinDataUser = signinDataUser;
        //    this.Home_ChangeTogSignPadCT_Event += callback1;
        //    this.Home_GoBackTogSignPictureCT_Event += callback2;
        //    this.Loaded += SignRoom_Loaded;
        //    //MouseTool.ShowArrow();
        //}

        private void SignRoom_Loaded(object sender, RoutedEventArgs e)
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
            btnSign.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnSign.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnSign.MouseLeftButtonDown += btnSign_MouseLeftButtonDown; 
        }

        private void btnSign_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            string AllowIpRange = "";
            DataTable dt = MSCE.GetDataTable("select AllowIpRange from NowLogin");
            if (dt.Rows.Count > 0)
            {
                AllowIpRange = dt.Rows[0]["AllowIpRange"].ToString();
            }
            if (PaperLess_Emeeting.Properties.Settings.Default.HasIpRangeMode == true && AllowIpRange.Equals("") == false && IpTool.CheckInNowWifi(AllowIpRange) == false)
            {
                string CourseOrMeeting_String = PaperLess_Emeeting.Properties.Settings.Default.CourseOrMeeting_String;
                AutoClosingMessageBox.Show(string.Format("您不在{0}室範圍內，無法使用此功能", CourseOrMeeting_String.Equals("課程") ? "教" : CourseOrMeeting_String));
                //AutoClosingMessageBox.Show("您不在會議室範圍內，無法使用此功能");
                return;
            }

            Home_ChangeTogSignPadCT_Event(signinDataUser.ID, signinDataUser.Name);
        }

        private void InitUI()
        {
            txtName.Text = signinDataUser.Name;

            if (signinDataUser.Dept.Equals("") == false)
                txtDept.Text = string.Format("({0})", signinDataUser.Dept);
            else
                txtDept.Text = "";

            txtAgent.Text = signinDataUser.AgentName;

            if (signinDataUser.SignedPic.Equals("") == false)
            {
                if (PaperLess_Emeeting.Properties.Settings.Default.AssemblyName.Contains("PaperLess_Emeeting_EDU"))
                {
                    btnSign.Visibility = Visibility.Collapsed;
                }
                txtAgent.Visibility = Visibility.Collapsed;
                //btnSign.Visibility = Visibility.Collapsed;
                
                //imgSignedPic.Source = new BitmapImage(new Uri(signinDataUser.SignedPic, UriKind.Absolute));
                if (sb != null)
                    sb.Begin();
                //ThreadPool.QueueUserWorkItem(callback =>
                Task.Factory.StartNew(()=>
                {
                    try
                    {
                        var webClient = new WebClient();
                        webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.Revalidate);
                        var buffer = webClient.DownloadData(new Uri(signinDataUser.SignedPic, UriKind.Absolute));
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
               

                if (signinDataUser.AgentName.Equals("") == true)
                    txtAgent.Visibility = Visibility.Collapsed;
            }
        }
    }
}
