using PaperLess_ViewModel;
using System;
using System.Collections.Generic;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PaperLess_Emeeting
{
    /// <summary>
    /// SeriesMeetingCT.xaml 的互動邏輯
    /// </summary>
    public partial class SeriesMeetingCT : UserControl
    {
        public string UserID { get; set; }
        public string UserPWD { get; set; }
        public SeriesData seriesData { get; set; }
        public string NowSeriesID = "";
        public SeriesMeetingCT_ChangeMeetingRoomWP_Function SeriesMeetingCT_ChangeMeetingRoomWP_Callback;

        public SeriesMeetingCT(string UserID, string UserPWD, SeriesData seriesData, string NowSeriesID = "")
        {
            MouseTool.ShowLoading();
            InitializeComponent();
            this.UserID = UserID;
            this.UserPWD = UserPWD;
            this.seriesData = seriesData;
            this.NowSeriesID = NowSeriesID;
            this.Loaded += SeriesMeetingCT_Loaded;
            this.Unloaded += SeriesMeetingCT_Unloaded;
            //MouseTool.ShowArrow();
        }

        private void SeriesMeetingCT_Loaded(object sender, RoutedEventArgs e)
        {
           Task.Factory.StartNew(() =>
           {
               InitSelectDB();
               // 只要是 Row 列表內容畫面，優先權設定為Background => 列舉值為 4。 所有其他非閒置作業都完成之後，就會處理作業。
               // 另外這裡比較特別 因為優先權要比AgendaRow高，所以我設定為Input => 列舉值為 5。 做為輸入相同的優先權處理作業。
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

        private void InitEvent()
        {
            txtKeyword.MouseEnter += (sender, e) => { MouseTool.ShowIBeam(); txtKeyword.Focus(); };
            txtKeyword.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); }; //Keyboard.ClearFocus();
            txtKeyword.KeyUp += txtKeyword_KeyUp;
            txtKeyword.Focus();
        }

        private void txtKeyword_KeyUp(object sender, KeyEventArgs e)
        {
            string keyword = txtKeyword.Text.ToLower().Trim();

            if (keyword.Equals("") == false)
            {
                foreach (MeetingRoom item in MeetingRoomWP.Children.OfType<MeetingRoom>())
                {
                    if (NowSeriesID.Equals("") == false && item.userMeeting.SeriesMeetingID.Equals(NowSeriesID) == false)
                    {
                        item.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        if (item.userMeeting.Name.Contains(keyword) == true || item.userMeeting.Location.Contains(keyword) == true)
                            item.Visibility = Visibility.Visible;
                        else
                            item.Visibility = Visibility.Collapsed;
                    }
                };
            }
            else
            {


                foreach (MeetingRoom item in MeetingRoomWP.Children.OfType<MeetingRoom>())
                {
                    if (NowSeriesID.Equals("") == false && item.userMeeting.SeriesMeetingID.Equals(NowSeriesID) == false)
                    {
                        item.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        item.Visibility = Visibility.Visible;
                    }
                };
            }
        }

        private void InitUI()
        {

            SeriesMeetingCT_ChangeMeetingRoomWP_Callback = new SeriesMeetingCT_ChangeMeetingRoomWP_Function(ChangeMeetingRoomWP);
            List<SeriesDataSeriesMeetingSeries> SeriesList = new List<SeriesDataSeriesMeetingSeries>();
            List<SeriesDataSeriesMeetingMeeting> MeetingList = new List<SeriesDataSeriesMeetingMeeting>();

            foreach (SeriesDataSeriesMeeting seriesMeeting in seriesData.SeriesMeeting)
            {
                SeriesList.Add(seriesMeeting.Series);
                MeetingList.AddRange(seriesMeeting.MeetingList.ToList());
            }


            string CourseOrMeeting_String = PaperLess_Emeeting.Properties.Settings.Default.CourseOrMeeting_String;
            txtCount.Text = string.Format("共 {0} 組系列{1}", SeriesList.Count, CourseOrMeeting_String);

            Task.Factory.StartNew(() =>
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    //int i = 0;
                    //SeriesList.ForEach(item =>
                    foreach(SeriesDataSeriesMeetingSeries item in SeriesList)
                    {
                        //i++;
                        SeriesMenu sm = new SeriesMenu(item, SeriesMeetingCT_ChangeMeetingRoomWP_Callback);
                        SeriesMenuSP.Children.Add(sm);
                        if (sm.seriesDataSeriesMeetingSeries.ID.Equals(NowSeriesID)==true)
                        {
                            sm.btnImg.Source = new BitmapImage(new Uri("images/icon_arrow_active.png", UriKind.Relative));
                            //sm.Background = new SolidColorBrush(Color.FromRgb(1, 161, 195));
                            sm.Background = ColorTool.HexColorToBrush("#019fde");
                            sm.txtSeriesName.Foreground = Brushes.White;
                        }
                    }//);
                }));

                this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action(() =>
                //this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    int i = 0;
                    //MeetingList.ForEach(item =>
                    int total = 0;
                    foreach(SeriesDataSeriesMeetingMeeting item in MeetingList)
                    {
                        i++;
                        UserMeeting um = new UserMeeting();
                        um.BeginTime = item.BeginTime;
                        um.EndTime = item.EndTime;
                        um.ID = item.ID;
                        um.isBrowserd = item.isBrowserd;
                        um.isDownload = item.isDownload;
                        um.Location = item.Location;
                        um.Name = item.Name;
                        um.pincode = item.pincode;
                        um.SeriesMeetingID = item.SeriesMeetingID;
                        um.type = item.type;
                     
                        Home Home_Window = App.Current.Windows.OfType<Home>().FirstOrDefault();
                        if (Home_Window != null)
                        {
                            bool invisible = false;
                            if (NowSeriesID.Equals("") == false && um.SeriesMeetingID.Equals(NowSeriesID) == false)
                            {
                                invisible = true;
                            }
                            else
                            {
                                ++total;
                            }
                            var room = new MeetingRoom(UserID, UserPWD, um, Home_Window.Home_Change2MeetingDataCT_Callback, "", invisible);
                            MeetingRoomWP.Children.Add(room);
                            
                        }

                        //string CourseOrMeeting_String = PaperLess_Emeeting.Properties.Settings.Default.CourseOrMeeting_String;
                        //txtCount.Text = string.Format("共 {0} 組系列{1}", total, CourseOrMeeting_String);
                    }//);


                }));
            });
        }

        private void ChangeMeetingRoomWP(string SeriesID)
        {
                NowSeriesID = SeriesID;
               
                //  其他系列會議按鈕不要hightlight
                IEnumerable<SeriesMenu> SeriesMenuList = SeriesMenuSP.Children.OfType<SeriesMenu>();
                foreach (SeriesMenu item in SeriesMenuList)
                {

                    if (item.seriesDataSeriesMeetingSeries.ID.Equals(SeriesID) == false)
                    {
                        item.btnImg.Source = new BitmapImage(new Uri("images/icon_arrow.png", UriKind.Relative));
                        item.Background = Brushes.Transparent;
                        item.txtSeriesName.Foreground = Brushes.Black;
                    }
                }
                

                //下面這一段當會議很多的時候會很費時，要想辦法解決。
                //顯示符合的系列會議
                Task.Factory.StartNew(() =>
                    {
                        //如果沒有用DispatcherPriority，數量一多會卡住
                        //this.Dispatcher.BeginInvoke(new Action(() =>
                        this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle,new Action(() =>
                            {
                                //MeetingRoomWP.Visibility = Visibility.Collapsed;
                                IEnumerable<MeetingRoom> meetingRoomList = MeetingRoomWP.Children.OfType<MeetingRoom>();
                                foreach (MeetingRoom item in meetingRoomList)
                                {
                                    if (item.userMeeting.SeriesMeetingID.Equals(SeriesID) == true)
                                    {
                                        item.Visibility = Visibility.Visible;
                                    }
                                    else
                                    {
                                        item.Visibility = Visibility.Collapsed;
                                    }
                                }
                                //MeetingRoomWP.Visibility = Visibility.Visible;
                                MouseTool.ShowArrow();
                            }));
                    });
              
                            
                   
        }

        private void InitSelectDB()
        {
        }

        private void SeriesMeetingCT_Unloaded(object sender, RoutedEventArgs e)
        {
           
        }

        
    }
}
