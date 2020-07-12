using PaperLess_ViewModel;
using System;
using System.Collections.Generic;
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
    public partial class SignRow : UserControl
    {
        public SigninDataUser signinDataUser { get; set; }
        public Storyboard sb { get; set; }
        public SignRow(SigninDataUser signinDataUser)
        {
            //MouseTool.ShowLoading();
            InitializeComponent();
            sb = (Storyboard)this.TryFindResource("sb");
            this.signinDataUser = signinDataUser;
            this.Loaded += SignRow_Loaded;
            //MouseTool.ShowArrow();
        }

        private void SignRow_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                //InitSelectDB();
                // 這裡是 Room 畫面，優先權設定跟Row一樣為Background => 列舉值為 4。 所有其他非閒置作業都完成之後，就會處理作業。
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    InitUI();
                    //InitEvent();
                }));
              
            });
           
        }

        private void InitSelectDB()
        {
        }

        private void InitEvent()
        {
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

            txtUserName.Text = signinDataUser.Name;
            txtDept.Text = signinDataUser.Dept;
            txtTitle.Text = signinDataUser.Title;
            

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
            //    txtUnSigned.Visibility = Visibility.Visible;
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
                txtUnSigned.Visibility = Visibility.Visible;
            }
        }

        private void imgSignedPic_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //GetHttpImage();
        }

       
    }
}
