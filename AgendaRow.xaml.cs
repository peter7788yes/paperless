using PaperLess_Emeeting.App_Code.MessageBox;
using PaperLess_ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public delegate void MeetingDataCT_ShowAgendaFile_Function(string AgendaID, string ParentID, bool IsDbClick);
    public delegate int MeetingDataCT_GetAgendaInwWorkCount_Function(string AgendaID);

    /// <summary>
    /// AgendaRow.xaml 的互動邏輯
    /// </summary>
    public partial class AgendaRow : UserControl
    {
        public event MeetingDataCT_ShowAgendaFile_Function MeetingDataCT_ShowAgendaFile_Event;
        public event MeetingDataCT_GetAgendaInwWorkCount_Function MeetingDataCT_GetAgendaInwWorkCount_Event;

        public MeetingDataAgenda meetingDataAgenda { get;set; }
        public string MeetingID { get; set; }
        public string UserID { get; set; }
        public bool IsHasFile { get; set; }
        public bool IsHasChildren { get; set; }
        public bool IsParent { get; set; }
        private Dictionary<string, string> cbData = new Dictionary<string, string>()
            {
                {"未開始", "N"},
                {"進行中", "U"},
                {"已結束", "D"}
            };
        public AgendaRow(string MeetingID, string UserID,bool IsHasFile, bool IsHasChildren,bool IsParent, MeetingDataAgenda meetingDataAgenda, MeetingDataCT_ShowAgendaFile_Function callback1,MeetingDataCT_GetAgendaInwWorkCount_Function callback2)
        {
            //MouseTool.ShowLoading();
            InitializeComponent();
            this.MeetingID = MeetingID;
            this.UserID = UserID;
            this.IsHasFile = IsHasFile;
            this.IsHasChildren = IsHasChildren;
            this.IsParent = IsParent;
            this.meetingDataAgenda = meetingDataAgenda;
            this.MeetingDataCT_ShowAgendaFile_Event += callback1;
            this.MeetingDataCT_GetAgendaInwWorkCount_Event += callback2;
            //this.DataContext = meetingDataAgenda;
            this.Loaded += AgendaRow_Loaded;
            //MouseTool.ShowArrow();
        }

        private void AgendaRow_Loaded(object sender, RoutedEventArgs e)
        {
            InitUI_Part1();
            Task.Factory.StartNew(() =>
              {
                  //InitSelectDB();
                  // 只要是 Row 列表內容畫面，優先權設定為Background => 列舉值為 4。 所有其他非閒置作業都完成之後，就會處理作業。
                  //Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                  this.Dispatcher.BeginInvoke(new Action(() =>
                  {
                      InitUI_Part2();
                      InitEvent();
                  }));
              });
        }

        private void InitEvent()
        {
            btnProgress.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnProgress.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnProgress.Click +=btnProgress_Click;

            //cbProgress.DropDownClosed += cbProgress_DropDownClosed;
            //cbProgress.SelectionChanged += cbProgress_SelectionChanged;
            //cbProgress.SelectionChanged += SelectionChangeCommitted;
            cbProgress.MouseLeave += (sender, e) =>
                {
                    cbProgress_SelectionChanged(cbProgress, new EventArgs());
                };
            txtAgendaName.MouseEnter += (sender, e) => { 
                //if (IsHasFile == true  || (IsHasChildren==true && IsParent==true)) 
                    MouseTool.ShowHand(); 
            };
            txtAgendaName.MouseLeave += (sender, e) => { 
                //if (IsHasFile == true  || (IsHasChildren == true && IsParent == true)) 
                    MouseTool.ShowArrow(); 
            };
            txtAgendaName.MouseLeftButtonDown += txtName_MouseLeftButtonDown;


            //notUse
            //txtName.AddHandler(TextBlock.MouseLeftButtonDownEvent
            //                , new MouseButtonEventHandler(txtName_MouseLeftButtonDown)
            //                , true);
        }

        private void SelectionChangeCommitted(object sender, SelectionChangedEventArgs e)
        {
            //meetingDataAgenda.Progress = cbProgress.SelectedValue.ToString();
            ////btnProgress.Content = cbProgress.Text;
            //btnProgress.Content = cbData.Where(x => x.Value.Equals(cbProgress.SelectedValue)).Select(x => x.Key).First();
            //ChangeColor(btnProgress.Content.ToString());
            //cbProgress.Visibility = Visibility.Collapsed;
            //btnProgress.Visibility = Visibility.Visible;
            //GetProgressUpload.AsyncPOST(MeetingID, UserID, meetingDataAgenda.ID, cbProgress.SelectedValue.ToString()
            //                        , (x) => { });
        }

        private void txtName_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //if (IsHasFile == false && IsHasChildren == false )
            //    return;

            Brush BlueBrush=ColorTool.HexColorToBrush("#0093b0");
            if (txtAgendaName.Foreground.ToString().Equals(BlueBrush.ToString()))
                MeetingDataCT_ShowAgendaFile_Event(meetingDataAgenda.ID, meetingDataAgenda.ParentID, true);
            else
                MeetingDataCT_ShowAgendaFile_Event(meetingDataAgenda.ID, meetingDataAgenda.ParentID, false);

            txtAgendaName.Foreground = BlueBrush;
            txtAgendaName.Inlines.LastInline.Foreground = BlueBrush;
            txtCaption.Foreground = BlueBrush;
        }

        //private void cbProgress_DropDownClosed(object sender, EventArgs e)
        private void cbProgress_SelectionChanged(object sender, EventArgs e)
        {
            cbProgress.Visibility = Visibility.Collapsed;
            meetingDataAgenda.Progress = cbProgress.SelectedValue.ToString();
            //btnProgress.Content = cbProgress.Text;
            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            btnProgress.Content = cbData.Where(x => x.Value.Equals(cbProgress.SelectedValue)).Select(x => x.Key).First();
            //sw.Stop();
            //MessageBox.Show(sw.ElapsedMilliseconds.ToString());
            
            ChangeColor(btnProgress.Content.ToString());
            btnProgress.Visibility = Visibility.Visible;
         
            GetProgressUpload.AsyncPOST(MeetingID, UserID, meetingDataAgenda.ID, cbProgress.SelectedValue.ToString()
                                    , (x) => { });

        }

        private void btnProgress_Click(object sender, RoutedEventArgs e)
        {
            // 此功能先拿掉
            if (this.MeetingDataCT_GetAgendaInwWorkCount_Event(meetingDataAgenda.ID) > 0)
            {
                AutoClosingMessageBox.Show("請先完成進行中的議程");
                return;
            }
            btnProgress.Visibility = Visibility.Collapsed;
            cbProgress.Visibility = Visibility.Visible;
            cbProgress.IsDropDownOpen = true;
        }

        private void InitUI_Part1()
        {
            //txtAgendaName.Text = meetingDataAgenda.Agenda;
            txtAgendaName.Inlines.Add(new Run(meetingDataAgenda.Agenda));
            if (meetingDataAgenda.Caption != null && meetingDataAgenda.Caption.Equals("") == false)
            {
                txtCaption.Text = meetingDataAgenda.Caption;
                txtCaption.Foreground = new SolidColorBrush(Color.FromRgb(161, 161, 157));
                txtCaption.Visibility = Visibility.Visible;
            }
            string ProposalUnit = "";
            if (meetingDataAgenda.ProposalUnit != null && meetingDataAgenda.ProposalUnit.Trim().Equals("") == false)
                ProposalUnit = string.Format(" ({0})", meetingDataAgenda.ProposalUnit);
            txtAgendaName.Inlines.Add(new Run(ProposalUnit) { Foreground = new SolidColorBrush(Color.FromRgb(161, 161, 157)) });
            string ParentID = meetingDataAgenda.ParentID;
            if (IsParent == false)
                txtAgendaName.Margin = new Thickness(txtAgendaName.Margin.Left + 23
                                              , txtAgendaName.Margin.Top
                                              , txtAgendaName.Margin.Right
                                              , txtAgendaName.Margin.Bottom);

            if (IsHasFile == true)
            {
                imgHasFile.Visibility = Visibility.Visible;
                btnProgress.Visibility = Visibility.Visible;
            }
            // 是否顯示進度按鈕 XOR
            if (IsParent ^ IsHasChildren)
                btnProgress.Visibility = Visibility.Visible;

            if (meetingDataAgenda.Progress == null || meetingDataAgenda.Progress.Equals("") == true)
                btnProgress.Visibility = Visibility.Collapsed;

        }

        private void InitUI_Part2()
        {
            ////txtAgendaName.Text = meetingDataAgenda.Agenda;
            //txtAgendaName.Inlines.Add(new Run(meetingDataAgenda.Agenda));
            //if (meetingDataAgenda.Caption!=null && meetingDataAgenda.Caption.Equals("") == false)
            //{
            //    txtCaption.Text = meetingDataAgenda.Caption;
            //    txtCaption.Foreground = new SolidColorBrush(Color.FromRgb(161, 161, 157));
            //    txtCaption.Visibility = Visibility.Visible;
            //}
            //string ProposalUnit = "";
            //if (meetingDataAgenda.ProposalUnit!=null && meetingDataAgenda.ProposalUnit.Trim().Equals("") == false)
            //    ProposalUnit=string.Format(" ({0})", meetingDataAgenda.ProposalUnit);
            //txtAgendaName.Inlines.Add(new Run(ProposalUnit) { Foreground = new SolidColorBrush(Color.FromRgb(161, 161, 157)) });
            //string ParentID=meetingDataAgenda.ParentID;
            //if (IsParent == false)
            //    txtAgendaName.Margin = new Thickness(txtAgendaName.Margin.Left + 23
            //                                  , txtAgendaName.Margin.Top
            //                                  , txtAgendaName.Margin.Right
            //                                  , txtAgendaName.Margin.Bottom);

            //cbData= new Dictionary<string, string>()
            //{
            //    {"未開始", "N"},
            //    {"已結束", "D"},
            //    {"進行中", "U"},
            //};


            //Task.Factory.StartNew(() =>
            //   {
            //       this.Dispatcher.BeginInvoke((Action)(() =>
            //       {
            //           try
            //           {
            //               cbProgress.ItemsSource = cbData;
            //               cbProgress.DisplayMemberPath = "Key";
            //               cbProgress.SelectedValuePath = "Value";
            //               cbProgress.SelectedValue = meetingDataAgenda.Progress;
            //               btnProgress.Content = cbProgress.Text;
            //               ChangeColor(btnProgress.Content.ToString());
            //           }
            //           catch (Exception ex)
            //           {
            //               LogTool.Debug(ex);
            //           }
            //       }));
            //   });

            cbProgress.ItemsSource = cbData;
            cbProgress.DisplayMemberPath = "Key";
            cbProgress.SelectedValuePath = "Value";
            cbProgress.SelectedValue = meetingDataAgenda.Progress;
            btnProgress.Content = cbProgress.Text;
            ChangeColor(btnProgress.Content.ToString());

            //if (IsHasFile == true)
            //{
            //    imgHasFile.Visibility = Visibility.Visible;
            //    btnProgress.Visibility = Visibility.Visible;
            //}
            //// 是否顯示進度按鈕 XOR
            //if (IsParent ^ IsHasChildren)
            //    btnProgress.Visibility = Visibility.Visible;

            //if (meetingDataAgenda.Progress==null || meetingDataAgenda.Progress.Equals("") == true)
            //    btnProgress.Visibility = Visibility.Collapsed;

        }

        private void ChangeColor(string cbDataKey)
        {
            switch (cbDataKey)
            {
                case "未開始":
                    btnProgress.Foreground = ColorTool.HexColorToBrush("#3746db");
                    break;
                case "已結束":
                    btnProgress.Foreground = ColorTool.HexColorToBrush("#000000");
                    break;
                case "進行中":
                    btnProgress.Foreground = ColorTool.HexColorToBrush("#ff1a1a");
                    break;

            }
        }

        private void InitSelectDB()
        {
            
        }
    }

}
