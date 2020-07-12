using PaperLess_ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public delegate void MeetingList_Show_HiddenMeetingDayList_Function();
    /// <summary>
    /// MeetingDayList.xaml 的互動邏輯
    /// </summary>
    public partial class MeetingDayList : UserControl
    {
        public event Home_Change2MeetingDataCT_Function Home_Change2MeetingDataCT_Event;
        public event MeetingList_Show_HiddenMeetingDayList_Function MeetingList_Show_HiddenMeetingEvent;
        public string UserID { get; set; }
        public string UserPWD { get; set; }
        public DateTime date { get; set; }
        public List<UserMeeting> meetingList { get; set; }
        public string NewAddMeetingID = "";

        public MeetingDayList(string UserID, string UserPWD, DateTime date, List<UserMeeting> meetingList
                                    , Home_Change2MeetingDataCT_Function callback
                                    , MeetingList_Show_HiddenMeetingDayList_Function callback2
                                    , string NewAddMeetingID)
        {
            //MouseTool.ShowLoading();
            InitializeComponent();
            this.UserID = UserID;
            this.UserPWD = UserPWD;
            this.date = date;
            this.meetingList = meetingList;
            this.Home_Change2MeetingDataCT_Event += callback;
            this.MeetingList_Show_HiddenMeetingEvent += callback2;
            this.NewAddMeetingID = NewAddMeetingID;
            this.Loaded += MeetingDayList_Loaded;
            //MouseTool.ShowArrow();
        }

        private void MeetingDayList_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
             {
                 InitSelectDB();
                 // 這裡為 會議列表畫面 下的 日期列表畫面，優先權設定為Loaded => 列舉值為 6。 當完成版面配置和呈現，但在輸入的優先順序的項目還會服務之前，會處理作業。 引發載入的事件時，會使用明確地說，這項目。
                 //this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
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

        //顯示隱藏會議
        private void InitEvent()
        {
            this.MouseWheel += (sender, e) =>
            {
                if (e.Delta > 0 )
                {
                    if (MeetingList_Show_HiddenMeetingEvent != null)
                    {
                        MeetingList_Show_HiddenMeetingEvent();
                    }
                }
            };
        }

        private void InitUI()
        {
            txtMonth.Text=date.Month.ToString() + "月";
            txtDay.Text = date.Day.ToString();
            txtWeek.Text=DateTool.DayOfWeek(date);

            if (date.Date == DateTime.Today)
            {
                DateSP.Background = ColorTool.HexColorToBrush("#c5f3ff");
                RoomGrid.Background = ColorTool.HexColorToBrush("#53a9ba");
            }
               Task.Factory.StartNew(() =>
                     {
                         //改成用DispatcherPriority，因為如果快速換月份
                         //一直跑UI，馬上又丟棄太浪費資源
                         //改成有空閒再跑會議房間就好
                         this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action(() =>
                         //this.Dispatcher.BeginInvoke(new Action(() =>
                               {
                                   //meetingList.ForEach(item =>
                                   foreach(UserMeeting item in meetingList)
                                   {
                                       var room = new MeetingRoom(UserID, UserPWD, item, this.Home_Change2MeetingDataCT_Event, NewAddMeetingID);
                                       MeetingRoomWP.Children.Add(room);
                                   }//);
                               }));
                     });
        }
    }
}