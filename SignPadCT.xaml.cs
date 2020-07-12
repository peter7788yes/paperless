using PaperLess_Emeeting.App_Code;
using PaperLess_Emeeting.App_Code.ClickOnce;
using PaperLess_Emeeting.App_Code.MessageBox;
using PaperLess_ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
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
    /// SignPadCT.xaml 的互動邏輯
    /// </summary>
    public partial class SignPadCT : UserControl
    {
        public event Home_GoBackTogSignPictureCT_Function Home_GoBackTogSignPictureCT_Event;

        System.Windows.Point lastPoint = default(System.Windows.Point);
        System.Windows.Point currentPoint = default(System.Windows.Point);
        public Storyboard sb { get; set; }
        public string UserID { get;set; }
        public string UserID_Origin { get; set; }
        public string UserName { get; set; }
        public string MeetingID { get; set; }
        public string DeptID { get; set; }
        public string PicUrl { get; set; }
        public bool FromIndividualSign { get; set; }
        List<Line> tempLine = new List<Line>();
        Stack<List<Line>> StackLines = new Stack<List<Line>>();
        public SignPadCT(string UserID = "", string UserName = "", string DeptID = "", string PicUrl="", Home_GoBackTogSignPictureCT_Function callback1 = null)
        {
            MouseTool.ShowLoading();
            InitializeComponent();
            this.UserID = UserID;
            this.UserName = UserName;
            this.DeptID = DeptID;
            this.PicUrl = PicUrl;
            this.Home_GoBackTogSignPictureCT_Event += callback1;
            sb = (Storyboard)this.TryFindResource("sb");
            this.Loaded += SignPadCT_Loaded;
            this.Unloaded += SignPadCT_Unloaded;
            //MouseTool.ShowArrow();
        }

        private void SignPadCT_Loaded(object sender, RoutedEventArgs e)
        {

            Task.Factory.StartNew(() =>
            {
                InitSelectDB();
                // 只要是 CT 主要畫面，優先權設定為Send，因為設定Normal，按鈕的出現會感覺卡卡的。
                this.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
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

        private void SignPadCT_Unloaded(object sender, RoutedEventArgs e)
        {
            Home Home_Window = Application.Current.Windows.OfType<Home>().FirstOrDefault();
            if(Home_Window!=null)
                Home_Window.KeyDown -= Home_Window_KeyDown;
        }

        private void InitSelectDB()
        {
        }


        private void InitEvent()
        {
            Home Home_Window = Application.Current.Windows.OfType<Home>().FirstOrDefault();
            if (Home_Window != null)
                Home_Window.KeyDown += Home_Window_KeyDown;

            btnYes.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnYes.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnYes.MouseDown += btnYes_MouseDown;

            btnNO.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnNO.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnNO.MouseDown += (sender, e) =>
            {
                if (PicUrl.Equals("") == false || StackLines.Count > 0)
                {
                    MessageBoxResult result =MessageBox.Show("是否要清除簽名檔", "系統訊息", MessageBoxButton.OKCancel);

                    if (result == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                    PicUrl = "";

                }
                imgSignedPicPanel.Visibility = Visibility.Collapsed;
                txtPLS.Visibility = Visibility.Visible;
                SignPadPanel.Visibility = Visibility.Visible;
                SignPad.Children.Clear();
                SignPad.Strokes.Clear();
                StackLines.Clear();
            };

            btnBack.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnBack.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnBack.MouseLeftButtonDown += (sender,e) => { Home_GoBackTogSignPictureCT_Event("",""); };

            SignPad.MouseEnter += (sender, e) => { MouseTool.ShowPen(); };
            SignPad.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            //SignPad.MouseDown += SignPad_MouseDown;
            //SignPad.MouseMove += SignPad_MouseMove;
            //SignPad.MouseUp += SignPad_MouseUp;
            SignPad.PreviewMouseLeftButtonDown += (sender, e) =>
                {
                    txtPLS.Visibility = Visibility.Collapsed;
                };

          

            imgSignedPicPanel.MouseEnter += (sender, e) => { MouseTool.ShowPen(); };
            imgSignedPicPanel.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            imgSignedPicPanel.PreviewMouseLeftButtonDown += (sender, e) => 
            //imgSignedPicPanel.MouseLeftButtonDown += (sender, e) => 
            {
                imgSignedPicPanel.Visibility = Visibility.Collapsed;
                txtPLS.Visibility = Visibility.Visible;
                SignPadPanel.Visibility = Visibility.Visible;
                
            };

            btnSignOut.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnSignOut.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnSignOut.MouseLeftButtonDown += (sender, e) =>
            {
                MessageBoxResult result = MessageBox.Show("您確定要簽退?", "系統訊息", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes)
                    return;

                MouseTool.ShowLoading();

                GetSignOutUpload.AsyncPOST(MeetingID, DeptID, UserID, (so) => { GetSignOutUpload_DoAction(so); });
            };
        }

       

        private void GetSignOutUpload_DoAction(SignOut so)
        {
            // 先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                this.Dispatcher.BeginInvoke(new Action<SignOut>(GetSignOutUpload_DoAction), so);
            }
            else
            {
                if (so != null && so.Reception.Status.ToLower().Trim().Equals("true") == true)
                {
                    AutoClosingMessageBox.Show("簽退成功");
                }
                else
                {
                    UserID = UserID_Origin;
                    AutoClosingMessageBox.Show("簽退失敗");
                }

                MouseTool.ShowArrow();
            }
           
        }

       

     

        private void Home_Window_KeyDown(object sender, KeyEventArgs e)
        {
           
            if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                List<Line> lastLine = StackLines.Pop();
                if (lastLine != null)
                {

                    //lastLine.ForEach(line =>
                    foreach(Line line in lastLine)
                    {
                        if (SignPad.Children.Contains(line))
                            SignPad.Children.Remove(line);
                    }//);

                    if (SignPad.Children.Count < 1)
                    {
                        txtPLS.Visibility = Visibility.Visible;
                    }
                    
                }
               
            }

        }

        private void btnYes_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //if (StackLines.Count < 1 || txtPLS.Visibility == Visibility.Visible)
            if (SignPad.Strokes.Count < 1 || txtPLS.Visibility == Visibility.Visible)
            {
                if (PicUrl.Equals("")==false)
                {
                    MessageBox.Show("已簽名，欲重新簽名請先按x清除");
                    return;
                }
                else
                {
                    MessageBox.Show("請簽名後上傳");
                    return;
                }
            }
           

            string AllowIpRange = "";
            DataTable dt = MSCE.GetDataTable("select AllowIpRange from NowLogin");
            if (dt.Rows.Count > 0)
            {
                AllowIpRange = dt.Rows[0]["AllowIpRange"].ToString();
            }
            if (PaperLess_Emeeting.Properties.Settings.Default.HasIpRangeMode == true && AllowIpRange.Equals("") == false && IpTool.CheckInNowWifi(AllowIpRange) == false)
            {
                string CourseOrMeeting_String =PaperLess_Emeeting.Properties.Settings.Default.CourseOrMeeting_String;
                AutoClosingMessageBox.Show(string.Format("您不在{0}室範圍內，無法使用此功能", CourseOrMeeting_String.Equals("課程") ? "教" : CourseOrMeeting_String));
                return;
            }

            // 系統暫存資料夾
            //string tempPath = System.IO.Path.GetTempPath(); //Environment.GetEnvironmentVariable("TEMP"); 
            string SignInFolder = System.IO.Path.Combine(ClickOnceTool.GetFilePath(),PaperLess_Emeeting.Properties.Settings.Default.SignInFolder);
            SignInFolder = System.IO.Path.Combine(SignInFolder, MeetingID, UserID);
            Directory.CreateDirectory(SignInFolder);
            string GUID = Guid.NewGuid().ToString();
            string tempFileName = GUID + ".png";
            string filePath = System.IO.Path.Combine(SignInFolder,tempFileName);
            Application app = Application.Current;

            //(1) Canvas
            CanvasTool.SaveCanvas(app.Windows[0], this.SignPad, 96, filePath);

            //(2) InkCanvas
            //double width = SignPad.ActualWidth;
            //double height = SignPad.ActualHeight;
            //RenderTargetBitmap bmpCopied = new RenderTargetBitmap((int)Math.Round(width), (int)Math.Round(height), 96, 96, PixelFormats.Default);
            //DrawingVisual dv = new DrawingVisual();
            //using (DrawingContext dc = dv.RenderOpen())
            //{
            //    VisualBrush vb = new VisualBrush(SignPad);
            //    dc.DrawRectangle(vb, null, new Rect(new System.Windows.Point(), new System.Windows.Size(width, height)));
            //}
            //bmpCopied.Render(dv);
            //System.Drawing.Bitmap bitmap;
            //using (MemoryStream outStream = new MemoryStream())
            //{
            //    // from System.Media.BitmapImage to System.Drawing.Bitmap 
            //    BitmapEncoder enc = new BmpBitmapEncoder();
            //    enc.Frames.Add(BitmapFrame.Create(bmpCopied));
            //    enc.Save(outStream);
            //    bitmap = new System.Drawing.Bitmap(outStream);
            //}

            //EncoderParameter qualityParam =new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 85L);

            //// Jpeg image codec
            //ImageCodecInfo jpegCodec = getEncoderInfo("image/jpeg");

            //if (jpegCodec == null)
            //    return;

            //EncoderParameters encoderParams = new EncoderParameters(1);
            //encoderParams.Param[0] = qualityParam;
            //Bitmap btm = new Bitmap(bitmap);
            //bitmap.Dispose();
            //btm.Save(filePath, jpegCodec, encoderParams);
            //btm.Dispose(); 

            //SigninDataUpload sdu = GetSigninDataUpload.POST(MeetingID, "UserID", filePath);
            MouseTool.ShowLoading();

            if (UserID.Equals("guest") == true)
            {
                UserID_Origin = UserID;
                UserID = "";
            }
            else if (UserID.Equals("dept") == true)
            {
                UserID_Origin = UserID;
                UserID = "";
            }
            GetSigninDataUpload.AsyncPOST(MeetingID, UserID, DeptID, filePath, (sdu) => { GetSigninDataUpload_DoAction(sdu); });
           
        }

        private ImageCodecInfo getEncoderInfo(string mimeType)
        {
            // Get image codecs for all image formats
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            // Find the correct image codec
            for (int i = 0; i < codecs.Length; i++)
                if (codecs[i].MimeType == mimeType)
                    return codecs[i];
            return null;
        } 

        private void GetSigninDataUpload_DoAction(SigninDataUpload sdu)
        {
              // 先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                this.Dispatcher.BeginInvoke(new Action<SigninDataUpload>(GetSigninDataUpload_DoAction), sdu);
            }
            else
            {
                if (sdu != null && sdu.File.Status.ToLower().Trim().Equals("true") == true)
                {
                    AutoClosingMessageBox.Show("上傳成功");
                    if (FromIndividualSign == true)
                    {
                        SignPad.IsEnabled = false;
                        btnNO.Visibility = Visibility.Collapsed;
                        btnYes.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        Home_GoBackTogSignPictureCT_Event("","");
                    }
                }
                else
                {
                    UserID = UserID_Origin;
                    AutoClosingMessageBox.Show("上傳失敗");
                }

                MouseTool.ShowArrow();
            }
           
        }



        private void SignPad_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtPLS.Visibility = Visibility.Collapsed;
            if (e.ButtonState == MouseButtonState.Pressed)
                currentPoint = e.GetPosition(this);   
            
        }

        private void SignPad_MouseMove(object sender, MouseEventArgs e)
        {
           
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (System.Windows.Point.Equals(currentPoint, default(System.Windows.Point)) == true)
                    return;
                lastPoint = currentPoint;
                
                //正常像素
                //Line line = new Line();
                //line.StrokeThickness = 15;
                //line.Stroke = Brushes.Black;
                //line.X1 = currentPoint.X - 31;
                //line.Y1 = currentPoint.Y - 53;
                //line.X2 = e.GetPosition(this).X - 31;
                //line.Y2 = e.GetPosition(this).Y - 53;

                int SignPenThickness1 = PaperLess_Emeeting.Properties.Settings.Default.SignPenThickness;
                int SignPenThickness2 = Math.Abs(PaperLess_Emeeting.Properties.Settings.Default.SignPenThickness - 15);
                //正常像素
                Line line = new Line();
                line.StrokeThickness = SignPenThickness1;
                line.Stroke = System.Windows.Media.Brushes.Black;
                line.X1 = currentPoint.X - 31 - SignPenThickness2;
                line.Y1 = currentPoint.Y - 53 - SignPenThickness2 * 2;
                line.X2 = e.GetPosition(this).X - 31 - SignPenThickness2;
                line.Y2 = e.GetPosition(this).Y - 53 - SignPenThickness2 * 2;

                //下面這句是劃出傘型筆畫
                line.StrokeStartLineCap = PenLineCap.Round;

                //線條的起始端(左邊)
                line.StrokeStartLineCap = PenLineCap.Round;
                line.StrokeEndLineCap = PenLineCap.Round;

                line.SnapsToDevicePixels = true;
                //line.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Unspecified);
                SignPad.Children.Add(line);
                tempLine.Add(line);

                //正常像素
                line = new Line();
                line.StrokeThickness = SignPenThickness1;
                line.Stroke = System.Windows.Media.Brushes.Black;
                line.X1 = currentPoint.X - 31 - SignPenThickness2;
                line.Y1 = currentPoint.Y - 53 - SignPenThickness2 * 2;
                line.X2 = e.GetPosition(this).X - 31 - SignPenThickness2;
                line.Y2 = e.GetPosition(this).Y - 53 - SignPenThickness2 * 2;
                currentPoint = e.GetPosition(this);
                line.SnapsToDevicePixels = true;
                SignPad.Children.Add(line);
                tempLine.Add(line);
            }
        }

        private void SignPad_MouseUp(object sender, MouseButtonEventArgs e)
        {
            StackLines.Push(tempLine);
            tempLine = new List<Line>();
        }
        private void InitUI()
        {
            DrawingAttributes da = new DrawingAttributes();
            da.Width = 7;
            da.Height = 7;
            SignPad.DefaultDrawingAttributes = da;

            // 這裡因為要判斷btnBack.Visibility = Visibility.Visible;
            // 所以把DB查詢邏輯放到這裡。
            DataTable dt = MSCE.GetDataTable("select UserID,UserName,MeetingID from NowLogin");
            if (dt.Rows.Count > 0)
            {
                if (UserID.Equals("") == true)
                {
                    FromIndividualSign = true;
                    UserID = dt.Rows[0]["UserID"].ToString();
                    UserName = dt.Rows[0]["UserName"].ToString();
                  
                }
                else
                {
                    FromIndividualSign = false;
                    btnBack.Visibility = Visibility.Visible;
                }
                MeetingID = dt.Rows[0]["MeetingID"].ToString();
                txtName.Text = UserName + " 您好";
   
            }

            if (PicUrl.Equals("") == false)
            {
         
                txtPLS.Visibility = Visibility.Collapsed;
                SignPadPanel.Visibility = Visibility.Collapsed;
                imgSignedPicPanel.Visibility = Visibility.Visible;

                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        var webClient = new WebClient();
                        webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.Revalidate);
                        var buffer = webClient.DownloadData(PicUrl);
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

            if (PaperLess_Emeeting.Properties.Settings.Default.EnableSignOut == true && FromIndividualSign == true)
            {
                btnSignOut.Visibility = Visibility.Visible;
                
                //沒有簽到過，簽退圖示反灰
                if (PicUrl.Equals("") == true)
                {
                    FormatConvertedBitmap newFormatedBitmapSource = new FormatConvertedBitmap();

                    // BitmapSource objects like FormatConvertedBitmap can only have their properties 
                    // changed within a BeginInit/EndInit block.
                    newFormatedBitmapSource.BeginInit();

                    // Use the BitmapSource object defined above as the source for this new  
                    // BitmapSource (chain the BitmapSource objects together).
                    newFormatedBitmapSource.Source = (BitmapSource)imgSignOut.Source;

                    // Set the new format to Gray32Float (grayscale).
                    newFormatedBitmapSource.DestinationFormat = PixelFormats.Gray32Float;
                    newFormatedBitmapSource.EndInit();

                    imgSignOut.Source = newFormatedBitmapSource;
                    txtSignOut.Foreground = System.Windows.Media.Brushes.Gray;
                    btnSignOut.ToolTip = "您尚未簽到，無法簽退";
                    btnSignOut.IsEnabled = false;
                    btnSignOut.Visibility = Visibility.Hidden;
                }
                else
                {
                    btnSignOut.Visibility = Visibility.Visible;
                }
            }
            
               
        }

       

       
    }
}
