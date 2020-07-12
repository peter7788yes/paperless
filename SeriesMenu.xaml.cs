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

namespace PaperLess_Emeeting
{

    public delegate void SeriesMeetingCT_ChangeMeetingRoomWP_Function(string SeriesID);
    /// <summary>
    /// SeriesMenu.xaml 的互動邏輯
    /// </summary>
    public partial class SeriesMenu : UserControl
    {
        public SeriesDataSeriesMeetingSeries seriesDataSeriesMeetingSeries { get; set; }
        public event SeriesMeetingCT_ChangeMeetingRoomWP_Function SeriesMeetingCT_ChangeMeetingRoomWP_Event;
        public SeriesMenu(SeriesDataSeriesMeetingSeries seriesDataSeriesMeetingSeries,SeriesMeetingCT_ChangeMeetingRoomWP_Function callback)
        {
            //MouseTool.ShowLoading();
            InitializeComponent();
            this.seriesDataSeriesMeetingSeries = seriesDataSeriesMeetingSeries;
            this.SeriesMeetingCT_ChangeMeetingRoomWP_Event = callback;
            this.Loaded +=SeriesMenu_Loaded;
            this.Unloaded +=SeriesMenu_Unloaded;
            //MouseTool.ShowArrow();
        }

        private void SeriesMenu_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
             {
                 InitSelectDB();

                 this.Dispatcher.BeginInvoke(new Action(() =>
                 {
                     InitUI();
                     InitEvent();
                 }));
                
             });
        }
      

     
        private void SeriesMenu_Unloaded(object sender, RoutedEventArgs e)
        {
        }

        private void InitSelectDB()
        {
        }


        private void InitEvent()
        {
            this.MouseEnter += (sender, e) => { 
                MouseTool.ShowHand();
                if (this.btnImg.Source.ToString().Contains("images/icon_arrow_active.png")==false)
                    this.Background = ColorTool.HexColorToBrush("#f1f5f6");
                //lblMousehover.Visibility = Visibility.Visible;
            };
            this.MouseLeave += (sender, e) => { 
                MouseTool.ShowArrow();
                if (this.btnImg.Source.ToString().Contains("images/icon_arrow_active.png") == false)
                    this.Background = Brushes.Transparent;
                //lblMousehover.Visibility = Visibility.Collapsed;
            };

            this.MouseLeftButtonDown += SeriesMenu_MouseLeftButtonDown;
        }

        private void SeriesMenu_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MouseTool.ShowLoading();
            this.btnImg.Source = new BitmapImage(new Uri("images/icon_arrow_active.png", UriKind.Relative));
            this.Background = ColorTool.HexColorToBrush("#019fde");
            this.txtSeriesName.Foreground = Brushes.White;


            if (SeriesMeetingCT_ChangeMeetingRoomWP_Event != null)
                SeriesMeetingCT_ChangeMeetingRoomWP_Event(this.seriesDataSeriesMeetingSeries.ID);        
              

        }

        private void InitUI()
        {
            txtSeriesName.Text = seriesDataSeriesMeetingSeries.Name;

        }
    }
}
