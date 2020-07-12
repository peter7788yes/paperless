using System;
using System.Collections.Generic;

using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using System.Diagnostics;
using Newtonsoft.Json;

namespace PaperLess_Emeeting
{
    /// <summary>
    /// MoviePlayer.xaml 的互動邏輯
    /// </summary>    
    public partial class MoviePlayer : Window
    {
        DispatcherTimer timer;
        private List<Image> playButtonImageList;

        public string filePath;

        public MoviePlayer(string FilePath, Boolean IsMovie, Boolean isToolBarEnabled)
        {
            this.filePath = FilePath;

            InitializeComponent();
            playButtonImageList = new List<Image>() 
            {
                new Image(){ Name="pause", Style=(Style)FindResource("PauseImageStyle") },
                new Image(){ Name="play", Style=(Style)FindResource("PlayImageStyle")  }
            };


            IsPlaying(false);
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(200);
            timer.Tick += new EventHandler(timer_Tick);
            MediaEL.Source = new Uri(FilePath);
            if (!IsMovie)
            {
                MediaEL.Visibility = System.Windows.Visibility.Collapsed;
            }

            if (isToolBarEnabled)
            {
                controlPanel.IsEnabled = true;
            }
            else
            {
                controlPanel.IsEnabled = false;
            }
            MediaEL.Play();
            IsPlaying(true);
        }

        #region IsPlaying(bool)
        private void IsPlaying(bool bValue)
        {
            btnStop.IsEnabled = bValue;
            btnMoveBackward.IsEnabled = bValue;
            btnMoveForward.IsEnabled = bValue;
            btnPlay.IsEnabled = bValue;
            //btnScreenShot.IsEnabled = bValue;
            seekBar.IsEnabled = bValue;
        }
        #endregion

        #region Play and Pause
        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            IsPlaying(true);
            string name = ((Image)btnPlay.Content).Name;
            if (name.Equals("play"))
            {
                MediaEL.Play();
                btnPlay.Content = playButtonImageList[0];
            }
            else if (name.Equals("pause"))
            {
                MediaEL.Pause();
                btnPlay.Content = playButtonImageList[1];
            }
        }
        #endregion

        #region Stop
        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            MediaEL.Stop();
            btnPlay.Content = playButtonImageList[1];
            IsPlaying(false);
            btnPlay.IsEnabled = true;

            //直接關掉
            this.Close();
        }
        #endregion

        #region Back and Forward
        private void btnMoveForward_Click(object sender, RoutedEventArgs e)
        {
            MediaEL.Position = MediaEL.Position + TimeSpan.FromSeconds(10);
        }

        private void btnMoveBackward_Click(object sender, RoutedEventArgs e)
        {
            MediaEL.Position = MediaEL.Position - TimeSpan.FromSeconds(10);
        }
        #endregion

        #region Capture Screenshot
        private void btnScreenShot_Click(object sender, RoutedEventArgs e)
        {
            //byte[] screenshot = MediaEL.GetScreenShot(1, 90);
            //FileStream fileStream = new FileStream(@"Capture.jpg", FileMode.Create, FileAccess.ReadWrite);
            //BinaryWriter binaryWriter = new BinaryWriter(fileStream);
            //binaryWriter.Write(screenshot);
            //binaryWriter.Close();
        }
        #endregion

        #region Seek Bar
        private void MediaEL_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (MediaEL.NaturalDuration.HasTimeSpan)
            {
                TimeSpan ts = MediaEL.NaturalDuration.TimeSpan;
                seekBar.Maximum = ts.TotalSeconds;
                seekBar.SmallChange = 1;
                seekBar.LargeChange = Math.Min(10, ts.Seconds / 10);
            }
            timer.Start();
        }

        bool isDragging = false;

        void timer_Tick(object sender, EventArgs e)
        {
            if (!isDragging)
            {
                seekBar.Value = MediaEL.Position.TotalSeconds;
                currentposition = seekBar.Value;
            }
        }

        private void seekBar_DragStarted(object sender, DragStartedEventArgs e)
        {
            isDragging = true;
        }

        private void seekBar_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            isDragging = false;
            MediaEL.Position = TimeSpan.FromSeconds(seekBar.Value);
        }
        #endregion

        #region FullScreen
        [DllImport("user32.dll")]
        static extern uint GetDoubleClickTime();

        System.Timers.Timer timeClick = new System.Timers.Timer((int)GetDoubleClickTime())
        {
            AutoReset = false
        };

        bool fullScreen = false;
        double currentposition = 0;

        private void MediaEL_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!timeClick.Enabled)
            {
                timeClick.Enabled = true;
                return;
            }

            if (timeClick.Enabled)
            {
                if (!fullScreen)
                {
                    LayoutRoot.Children.Remove(MediaEL);
                    this.Background = new SolidColorBrush(Colors.Black);
                    this.Content = MediaEL;
                    this.WindowStyle = WindowStyle.None;
                    this.WindowState = WindowState.Maximized;
                    MediaEL.Position = TimeSpan.FromSeconds(currentposition);
                }
                else
                {
                    this.Content = LayoutRoot;
                    LayoutRoot.Children.Add(MediaEL);
                    this.Background = new SolidColorBrush(Colors.White);
                    this.WindowStyle = WindowStyle.SingleBorderWindow;
                    this.WindowState = WindowState.Normal;
                    MediaEL.Position = TimeSpan.FromSeconds(currentposition);
                }
                fullScreen = !fullScreen;
            }
        }
        #endregion
    }
}
