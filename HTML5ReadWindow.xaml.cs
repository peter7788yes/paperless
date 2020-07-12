using BookManagerModule;
using CefSharp;
using CefSharp.Wpf;
using DataAccessObject;
using MultiLanquageModule;
using Newtonsoft.Json;
using PaperLess_Emeeting.App_Code.ViewModel;
using PaperlessSync.Broadcast.Service;
using PaperlessSync.Broadcast.Socket;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
using BookFormatLoader;
using PaperLess_Emeeting.App_Code.Tools;
using PaperLess_ViewModel;
using PaperLess_Emeeting.App_Code.MessageBox;
using SyncCenterModule;
using PaperLess_Emeeting.App_Code;
using iTextSharp.text;
using iTextSharp.text.pdf;
using PaperLess_Emeeting.App_Code.Socket;
using System.Reflection;
using Wpf_CustomCursor;


namespace PaperLess_Emeeting
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>

    #region PaperlessMeeting




    //public class PemMemoInfos
    //{
    //    public double canvasWidth { get; set; }
    //    public double canvasHeight { get; set; }
    //    public double strokeAlpha { get; set; }
    //    public string points { get; set; }
    //    public string strokeColor { get; set; }
    //    public double strokeWidth { get; set; }

    //}

    //public enum MediaCanvasOpenedBy
    //{
    //    None = 0,
    //    SearchButton = 1,
    //    MediaButton = 2,
    //    CategoryButton = 3,
    //    NoteButton = 4,
    //    ShareButton = 5,
    //    SettingButton = 6,
    //    PenMemo = 7
    //}

    #endregion

    public partial class HTML5ReadWindow : Window, IEventManager, IRequestHandler, IMenuHandler
    {

        private HEJMetadata hejMetadata;

        private string pptPath;
        private SocketClient socket;

        //目前是否在同步中
        private bool isSyncing = true;
        //目前是否是議事管理員
        private bool isSyncOwner = true;


        #region 一堆變數

        private string appPath = Directory.GetCurrentDirectory();

        //private readonly WebView web_view;
        //單頁的寬度, 不含左右 Padding
        public int actual_webkit_column_width;
        //單頁的高度, 不含上下 Padding
        public int actual_webkit_column_height;

        //上下左右 Padding
        const int PADDING_TOP = 0;
        const int PADDING_BOTTOM = 0;
        const int PADDING_LEFT = 0;
        const int PADDING_RIGHT = 0;

        //橫書: false, 直書: true
        const bool VERTICAL_WRITING_MODE = false;
        //文字走向: 左到右: true, 右到左: false
        const bool LEFT_TO_RIGHT = false;
        
        //string text;
        string curHtmlDoc;
        string jsquery;
        string jsrangy_core;
        string jsrangy_cssclassapplier;
        string jsrangy_selectionsaverestore;
        string jsrangy_serializer;
        string jsEPubAddition;
        string jsCustomSearch;
        string jsBackCanvas;
        string tail_JS;

        public static int curLeft;
        public int curPage;
        public int totalPage;
        //文字大小
        public int fontSize = 16; //100%
        //文字比例
        public int perFontFize = 100;
        //目前沒用的 flag
        public bool scaleFlag = false;
        public string scaleRangyRange = "";

        private string basePath = "";

        private List<string> HTMLCode = new List<string>();
        private List<string> resultXMLList = new List<string>();
        private List<int> totalPagesInNodes = new List<int>();

        private bool processing = false;

        bool bookOpened = false;

        public string jsAnimation { get; set; }

        #endregion

        public string bookId { get; set; }

        public string account { get; set; }

        public string userName { get; set; }

        public string email { get; set; }

        public string meetingId { get; set; }

        public string watermark { get; set; }

        public string dbPath { get; set; }

        public string webServiceURL { get; set; }

        public string socketMessage { get; set; }

        public BookManager bookManager { get; set; }

        public MultiLanquageManager langMng { get; set; }

        public int userBookSno { get; set; }

        private double thumbnailWidth;
        private double thumbnailHeight;
        private double thumbnailRatio;

        public DispatcherTimer initTimer;

        private Dictionary<int, BookMarkData> bookMarkDictionary;
        private Dictionary<int, NoteData> bookNoteDictionary;
        private Dictionary<int, List<StrokesData>> bookStrokesDictionary;
        private Dictionary<string, LastPageData> lastViewPage;

        private MoviePlayer mp;


        protected override void OnContentRendered(EventArgs e)
        {
            this.Topmost = true;
            this.Topmost = false;
            base.OnContentRendered(e);
        }


        public Dictionary<string, BookVM> cbBooksData = new Dictionary<string, BookVM>();
        public event Home_OpenBookFromReader_Function Home_OpenBookFromReader_Event;


        public bool HasJoin2Folder = false;
        public string FolderID;
        public bool CanNotCollect;
        public HTML5ReadWindow(Dictionary<string, BookVM> cbBooksData
                                , Home_OpenBookFromReader_Function callback
                                , string _pptPath, string _bookId, string _account, string _userName, string _email, string _meetingId, string _watermark, string _dbPath, bool _isSync, bool _isSyncOwner, string _webServiceURL, string _socketMessage = "", SocketClient _socket = null)
        {
            InitializeComponent();


            // Wayne add
            this.cbBooksData = cbBooksData;
            this.Home_OpenBookFromReader_Event = callback;

            this.socket = _socket;
            this.pptPath = _pptPath;
            this.bookId = _bookId;
            this.account = _account;
            this.userName = _userName;
            this.email = _email;
            this.meetingId = _meetingId;
            this.watermark = _watermark;
            this.dbPath = _dbPath;
            this.isSyncing = _isSync;
            this.webServiceURL = _webServiceURL;
            this.isSyncOwner = _isSyncOwner;
            this.socketMessage = _socketMessage;


            bookManager = new BookManager(dbPath);

            QueryResult rs = null;
            try
            {
                string query = "Select objectId from bookMarkDetail";
                //rs = dbConn.executeQuery(query);
                rs = bookManager.sqlCommandQuery(query);
                if (rs == null)
                {
                    //資料庫尚未更新
                    updateDataBase();
                }

                //Wayne add
                InitSyncCenter();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception@updateDataBase: " + ex.Message);

            }
            rs = null;

            langMng = new MultiLanquageManager("zh-TW");
            alterAccountWhenSyncing(this.isSyncOwner);
            getBookPath();

            //this.Initialized += _InitializedEventHandler;

            InitializeComponent();

            setWindowToFitScreen();
            this.Loaded += HTML5Reader_Loaded;

            // Wayne Add
            if (cbBooksData != null)
            {
                cbBooks.ItemsSource = cbBooksData;
                cbBooks.DisplayMemberPath = "Key";
                cbBooks.SelectedValuePath = "Value";
                cbBooks.SelectedIndex = 0;

                int i = 0;
                foreach (KeyValuePair<string, BookVM> item in cbBooksData)
                {

                    if (item.Value.FileID.Equals(this.bookId) == true)
                    {
                        cbBooks.SelectedIndex = i;
                        break;
                    }
                    i++;
                }
                cbBooks.SelectionChanged += cbBooks_SelectionChanged;
            }
            else
            {
                cbBooks.Width = 0;
                cbBooks.Visibility = Visibility.Collapsed;
            }

            ChangeFlatUI(PaperLess_Emeeting.Properties.Settings.Default.IsFlatUIReader);

            AttachKey();

            ClearSyncOwnerPenLine();


      
         

        }

        private void AttachKey()
        {
            this.PreviewKeyDown += (sender, e) =>
            {
                //e.Handled = true;
                if (isSyncing == true && isSyncOwner == false)
                    return;
                if (MediaTableCanvas.Visibility != Visibility.Visible)
                {
                    switch (e.Key)
                    {
                        case Key.Left:
                            MovePage(MovePageType.上一頁);
                            break;
                        case Key.Right:
                            MovePage(MovePageType.下一頁);
                            break;
                        case Key.Up:
                            MovePage(MovePageType.上一頁);
                            break;
                        case Key.Down:
                            MovePage(MovePageType.下一頁);
                            break;

                        case Key.PageDown:
                            MovePage(MovePageType.下一頁);
                            break;
                        case Key.PageUp:
                            MovePage(MovePageType.上一頁);
                            break;

                        case Key.Home:
                            MovePage(MovePageType.第一頁);
                            break;
                        case Key.End:
                            MovePage(MovePageType.最後一頁);
                            break;
                        case Key.Escape:
                            OpenClosePaint();
                            break;
                        default:
                            break;
                    }
                }
            };






        }

        private void ChangeFlatUI(bool IsFlatUI)
        {
            if (IsFlatUI == true)
            {
                ToolBarInReader.Visibility = Visibility.Collapsed;
                if (isSyncing == true)
                {
                    StatusOnairOff.Visibility = Visibility.Collapsed;
                    if (isSyncOwner == true)
                    {
                        screenBroadcasting.Visibility = Visibility.Visible;
                        screenReceiving.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        screenBroadcasting.Visibility = Visibility.Collapsed;
                        screenReceiving.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    StatusOnairOff.Visibility = Visibility.Visible;
                }


                StatusOnairOff.MouseLeftButtonDown += (sender, e) =>
                {
                    Task.Factory.StartNew(() =>
                    {
                        if (this.socket == null)
                        {
                            Singleton_Socket.ReaderEvent = this;
                            this.socket = Singleton_Socket.GetInstance(meetingId, account, userName, true);
                        }



                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            AutoClosingMessageBox.Show("連線中");
                            syncButton.IsChecked = true;
                            syncButton.RaiseEvent(new RoutedEventArgs(ToggleButton.ClickEvent));

                            //typeof(ToggleButton).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(syncButton, new object[] { true });
                            //typeof(ToggleButton).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(syncButton, new object[] { false });

                        }));
                    });
                };

                screenBroadcasting.MouseLeftButtonDown += (sender, e) =>
                {
                    Task.Factory.StartNew(() =>
                    {

                        if (this.socket == null)
                        {
                            Singleton_Socket.ReaderEvent = this;
                            this.socket = Singleton_Socket.GetInstance(meetingId, account, userName, false);
                        }



                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            AutoClosingMessageBox.Show("連線中");
                            syncButton.IsChecked = false;
                            syncButton.RaiseEvent(new RoutedEventArgs(ToggleButton.ClickEvent));

                            //typeof(ToggleButton).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(syncButton, new object[] { false });
                        }));
                    });
                };

                screenReceiving.MouseLeftButtonDown += (sender, e) =>
                {
                    Task.Factory.StartNew(() =>
                    {
                        if (this.socket == null)
                        {
                            Singleton_Socket.ReaderEvent = this;
                            this.socket = Singleton_Socket.GetInstance(meetingId, account, userName, false);
                        }


                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            AutoClosingMessageBox.Show("連線中");
                            syncButton.IsChecked = false;
                            syncButton.RaiseEvent(new RoutedEventArgs(ToggleButton.ClickEvent));

                            //typeof(ToggleButton).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(syncButton, new object[] { false });
                        }));
                    });
                };
                this.MouseRightButtonDown += (sender, e) =>
                {

                    OpenClosePaint();

                };

                Canvas.SetZIndex(MediaTableCanvas, 201);
                NewUITop.Visibility = Visibility.Visible;
                SearchSP.Visibility = Visibility.Visible;
                thumnailCanvas.Margin = new Thickness(0, 0, 0, 100);
                //ShowListBoxButton.Width = 0;
                //ShowListBoxButton.Height = 0;
                //ShowListBoxButtonNew.Visibility = Visibility.Visible;
                AllImageButtonInListBox.Height = 0;//.Visibility = Visibility.Collapsed;
                AllImageButtonInListBox.Width = 0;
                AllImageButtonInListBox.Margin = new Thickness(0);
                AllImageButtonInListBox.Visibility = Visibility.Collapsed;
                BookMarkButtonInListBox.Height = 0;//.Visibility = Visibility.Collapsed;
                BookMarkButtonInListBox.Width = 0;
                BookMarkButtonInListBox.Margin = new Thickness(0);
                BookMarkButtonInListBox.Visibility = Visibility.Collapsed;
                NoteButtonInListBox.Height = 0;//.Visibility = Visibility.Collapsed;
                NoteButtonInListBox.Width = 0;
                NoteButtonInListBox.Margin = new Thickness(0);
                NoteButtonInListBox.Visibility = Visibility.Collapsed;


                HideListBoxButton.Visibility = Visibility.Collapsed;
                NewUI.Visibility = Visibility.Visible;
                AllImageButtonInListBoxSP.Visibility = Visibility.Visible;
                BookMarkButtonInListBoxSP.Visibility = Visibility.Visible;
                NoteButtonInListBoxSP.Visibility = Visibility.Visible;
                Rect1.Visibility = Visibility.Visible;
                Rect2.Visibility = Visibility.Visible;

                //Border border=MediaTableCanvas.Children.OfType<Border>().First();
                //MediaTableCanvas.Children.Clear();
                //Canvas.SetBottom(myButton2, 50);

                thumbNailListBoxGD.Background = ColorTool.HexColorToBrush("#272727");
                thumbNailListBoxGD.VerticalAlignment = VerticalAlignment.Center;
                thumbNailCanvasGrid.Background = ColorTool.HexColorToBrush("#000000");
                //wrapPanel.Margin = new Thinkness(10);
                //Style style = new Style(typeof (Grid));
                //style.Setters.Add(new Setter(Grid.PaddingProperty, Brushes.Green));
                //style.Setters.Add(new Setter(TextBlock.TextProperty, "Green"));
                //Resources.Add(typeof (TextBlock), style);}

                txtKeyword.MouseEnter += (sender, e) => { MouseTool.ShowIBeam(); txtKeyword.Focus(); };
                txtKeyword.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); }; //Keyboard.ClearFocus();
                txtKeyword.KeyUp += txtKeyword_KeyUp;
                txtKeyword.Focus();

                txtKeyword.PreviewKeyDown += txtKeyword_PreviewKeyDown;
                btnTxtKeywordClear.Click += (sender, e) =>
                {
                    txtKeyword.Text = "";
                    txtKeyword.Focus();
                    btnTxtKeywordClear.Visibility = Visibility.Collapsed;
                    int TotalRecord = 0;
                    int i = 0; ;
                    foreach (ThumbnailImageAndPage item in thumbNailListBox.Items)
                    {
                        ListBoxItem listBoxItem = (ListBoxItem)(thumbNailListBox.ItemContainerGenerator.ContainerFromIndex(i));
                        if (listBoxItem != null)
                            listBoxItem.Visibility = Visibility.Visible;
                        TotalRecord++;
                        i++;
                    }
                    txtFilterCount.Text = string.Format("有 {0} 筆相關資料", TotalRecord.ToString());
                };


                btnBold.Click += (sender, e) =>
                {
                    if (btnPenFuncSP.Height > 0)
                    {
                        btnPenColor.Background = ColorTool.HexColorToBrush("#000000");
                        MyAnimation(btnPenFuncSP, 300, "Height", btnPenFuncSP.ActualHeight, 0);
                    }
                    if (btnFuncSP.Height > 0)
                    {
                        btnBold.Background = ColorTool.HexColorToBrush("#000000");
                        MyAnimation(btnFuncSP, 300, "Height", btnFuncSP.ActualHeight, 0);
                    }
                    else
                    {
                        ShowNowPenBold();
                        btnBold.Background = ColorTool.HexColorToBrush("#F66F00");
                        MyAnimation(btnFuncSP, 300, "Height", 0, btnFuncSP.ActualHeight);
                    }
                };

                btnPenColor.Click += (sender, e) =>
                {
                    if (btnFuncSP.Height > 0)
                    {
                        btnBold.Background = ColorTool.HexColorToBrush("#000000");
                        MyAnimation(btnFuncSP, 300, "Height", btnFuncSP.ActualHeight, 0);
                    }
                    if (btnPenFuncSP.Height > 0)
                    {
                        btnPenColor.Background = ColorTool.HexColorToBrush("#000000");
                        MyAnimation(btnPenFuncSP, 300, "Height", btnPenFuncSP.ActualHeight, 0);
                    }
                    else
                    {
                        ShowNowPenColor();
                        btnPenColor.Background = ColorTool.HexColorToBrush("#F66F00");
                        MyAnimation(btnPenFuncSP, 300, "Height", 0, btnPenFuncSP.ActualHeight);
                    }
                };
            }
            else
            {
                statusBMK.Width = 0;
                statusBMK.Height = 0;
                statusMemo.Width = 0;
                statusMemo.Height = 0;
                StatusOnairOff.Width = 0;
                StatusOnairOff.Height = 0;
                thumnailCanvas.Background = ColorTool.HexColorToBrush("#212020");
            }
        }

        private void OpenClosePaint()
        {
            if (Canvas.GetZIndex(penMemoCanvas) < 900)
            {
                MouseTool.ShowPen();
                //打開
                Canvas.SetZIndex(penMemoCanvas, 900);
                Canvas.SetZIndex(stageCanvas, 2);
                Canvas.SetZIndex(web_view, 850);

                web_view.IsHitTestVisible = false;
                penMemoCanvas.IsHitTestVisible = true;
                stageCanvas.IsHitTestVisible = false;

                penMemoCanvas.Background = System.Windows.Media.Brushes.Transparent;
                penMemoCanvas.EditingMode = InkCanvasEditingMode.Ink;
                ChangeMainPenColor();

                Brush backgroundColor = btnEraserGD.Background;
                if (backgroundColor is SolidColorBrush)
                {
                    string colorValue = ((SolidColorBrush)backgroundColor).Color.ToString();
                    if (colorValue.Equals("#FFF66F00") == true)
                    {
                        Mouse.OverrideCursor = CursorHelper.CreateCursor(new MyCursor());
                        penMemoCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                    }

                }



                penMemoCanvas.Visibility = Visibility.Visible;


                //test
                //penMemoCanvas.EditingMode = InkCanvasEditingMode.None;
                //penMemoCanvas.MouseRightButtonDown += (sender, e) =>
                //    {
                //        MessageBox.Show("111");
                //    };
            }
            else
            {
                MouseTool.ShowArrow();
                //關閉
                Canvas.SetZIndex(web_view, 1);
                Canvas.SetZIndex(penMemoCanvas, 2);
                Canvas.SetZIndex(stageCanvas, 3);

                web_view.IsHitTestVisible = true;
                penMemoCanvas.IsHitTestVisible = false;
                stageCanvas.IsHitTestVisible = false;

                penMemoCanvas.EditingMode = InkCanvasEditingMode.None;
                if (PopupControlCanvas.Visibility.Equals(Visibility.Visible))
                {
                    PopupControlCanvas.Visibility = Visibility.Collapsed;
                }

                if (HiddenControlCanvas.Visibility.Equals(Visibility.Visible))
                {
                    HiddenControlCanvas.Visibility = Visibility.Collapsed;
                }
            }
            penMemoCanvas.Focus();
        }

        private void ShowNowPenColor()
        {
            int index = 1;
            int.TryParse(btnPenColor.Tag.ToString(), out index);
            List<DependencyObject> list = new List<DependencyObject>();
            FindVisualChildTool.ByType<Grid>(PenColorSP, ref list);

            int i = 0;
            foreach (Grid btn in list)
            {
                i++;
                if (i == index)
                {
                    btn.Background = ColorTool.HexColorToBrush("#F66F00");
                }

            }
        }

        private void ShowNowPenBold()
        {
            int index = 1;
            int.TryParse(btnBold.Tag.ToString(), out index);
            IEnumerable<Grid> btns = btnBoldSP.Children.OfType<Grid>();

            int i = 0;
            foreach (Grid btn in btns)
            {
                i++;
                if (i * 100 == index)
                {
                    btn.Background = ColorTool.HexColorToBrush("#F66F00");
                }

            }
        }

        private void MyAnimation(DependencyObject sp, double ms, string property, double from, double to, Action act = null)
        {
            Storyboard sb = new Storyboard();
            DoubleAnimation da = new DoubleAnimation();
            Duration duration = new Duration(TimeSpan.FromMilliseconds(ms));
            da.Duration = duration;
            sb.Children.Add(da);
            Storyboard.SetTarget(da, sp);
            Storyboard.SetTargetProperty(da, new PropertyPath(property));
            da.AccelerationRatio = 0.2;
            da.DecelerationRatio = 0.7;
            da.From = from;
            da.To = to;

            sb.Completed += (sender2, e2) =>
            {
                if (act != null)
                    act();
            };

            sb.Begin();
        }

        private void Grid_MouseEnterTransparent(object sender, MouseEventArgs e)
        {
            btnThin.Background = Brushes.Transparent;
            btnMedium.Background = Brushes.Transparent;
            btnLarge.Background = Brushes.Transparent;

            Grid gd = (Grid)sender;
            gd.Background = ColorTool.HexColorToBrush("#F66F00");
        }

        private void Grid_MouseLeaveTransparent(object sender, MouseEventArgs e)
        {
            Grid gd = (Grid)sender;
            gd.Background = Brushes.Transparent;
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            List<DependencyObject> list = new List<DependencyObject>();
            FindVisualChildTool.ByType<Grid>(PenColorSP, ref list);

            int i = 0;
            foreach (Grid btn in list)
            {
                i++;
                if (Brush.Equals(btn.Background, Brushes.Black) == false)
                {
                    btn.Background = ColorTool.HexColorToBrush("#000000");
                }

            }

            Grid gd = (Grid)sender;
            gd.Background = ColorTool.HexColorToBrush("#F66F00");
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {

            Grid gd = (Grid)sender;
            gd.Background = Brushes.Black;
        }

        private void btnBoldSP_MouseLeave(object sender, MouseEventArgs e)
        {
            ShowNowPenBold();
        }

        private void btnPenFuncSP_MouseLeave(object sender, MouseEventArgs e)
        {
            ShowNowPenColor();
        }

        private void txtKeyword_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(10);
                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (txtKeyword.Text.Length > 0)
                    {
                        btnTxtKeywordClear.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        btnTxtKeywordClear.Visibility = Visibility.Collapsed;
                    }
                }));
            });
        }


        private void txtKeyword_KeyUp(object sender, KeyEventArgs e)
        {
            string keyword = txtKeyword.Text.ToLower().Trim();

            int TotalRecord = 0;
            if (keyword.Equals("") == false)
            {
                ListBox resultLB = hyftdSearch(keyword);
                List<SearchRecord> listS = (List<SearchRecord>)resultLB.ItemsSource;
                List<ThumbnailImageAndPage> listT = (List<ThumbnailImageAndPage>)thumbNailListBox.ItemsSource;
                List<int> listS_Int = listS.Select(x => x.targetPage - 1).ToList();
                //List<int> listT_Int = listT.Select(x=>int.Parse(x.pageIndex)-1).ToList();
                //List<int> joined = (from item1 in listS_Int
                //             join item2 in listT_Int
                //             on item1 equals item2 // join on some property
                //             select item1).ToList();
                //List<ThumbnailImageAndPage> result = new List<ThumbnailImageAndPage>();
                int i = 0;
                foreach (ThumbnailImageAndPage item in thumbNailListBox.Items)
                {
                    int PageIndex = int.Parse(item.pageIndex);
                    ListBoxItem listBoxItem = (ListBoxItem)(thumbNailListBox.ItemContainerGenerator.ContainerFromIndex(i));
                    if (listS_Int.Contains(PageIndex) == false)
                    {

                        listBoxItem.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        if (listBoxItem.Visibility == Visibility.Visible)
                        {
                            listBoxItem.Visibility = Visibility.Visible;
                            TotalRecord++;
                        }
                    }
                    i++;
                }





            }
            else
            {
                int i = 0;
                foreach (ThumbnailImageAndPage item in thumbNailListBox.Items)
                {
                    ListBoxItem listBoxItem = (ListBoxItem)(thumbNailListBox.ItemContainerGenerator.ContainerFromIndex(i));
                    if (listBoxItem != null)
                        listBoxItem.Visibility = Visibility.Visible;
                    TotalRecord++;
                    i++;
                }
            }

            //MoveBoxPage();
            //ShowImageCenter();
            txtFilterCount.Text = string.Format("有 {0} 筆相關資料", TotalRecord.ToString());
        }


        private void InitSyncCenter()
        {
            Task.Factory.StartNew(() =>
            {
                if (PaperLess_Emeeting.Properties.Settings.Default.HasSyncCenterModule == true)
                {
                    try
                    {


                        SyncCenter syncCenter = new SyncCenter();
                        syncCenter.bookManager = new BookManager(this.dbPath);
                        alterAccountWhenSyncing(this.isSyncOwner);
                        getBookPath();
                        Dictionary<String, Object> cloudSyncingClsList = new Dictionary<String, Object>() { { "SBookmark", new BookMarkData() }, { "SAnnotation", new NoteData() }, { "SSpline", new StrokesData() }, { "SLastPage", new LastPageData() } };

                        foreach (KeyValuePair<String, Object> syncType in cloudSyncingClsList)
                        {
                            string className = syncType.Key;
                            Type openType = typeof(SyncManager<>);
                            Type actualType = openType.MakeGenericType(new Type[] { syncType.Value.GetType() });

                            //AbstractSyncManager sm = (AbstractSyncManager)Activator.CreateInstance(actualType, this.account, this.meetingId, this.bookId, this.userBookSno, className, 0, "0", WsTool.GetAbstractSyncCenter_BASE_URL());
                            AbstractSyncManager sm = (AbstractSyncManager)Activator.CreateInstance(actualType, this.account, "free", this.bookId, this.userBookSno, className, 0, "0", WsTool.GetAbstractSyncCenter_BASE_URL());
                            syncCenter.addSyncConditions(className, sm);

                        }

                    }
                    catch (Exception ex)
                    {
                        LogTool.Debug(ex);
                    }
                }
            });
        }

        private void cbBooks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isSyncing == true && isSyncOwner == false)
                return;

            ComboBox cb = (ComboBox)sender;

            BookVM bookVM = ((BookVM)cb.SelectedValue);
            if (bookVM == null)
                return;

            //RecordPage();

            if (Home_OpenBookFromReader_Event != null)
            {
                Home_OpenBookFromReader_Event(this.meetingId, bookVM, this.cbBooksData, this.watermark);
            }
        }


        public HTML5ReadWindow(string _pptPath, string _bookId, string _account, string _userName, string _email, string _meetingId, string _watermark, string _dbPath, bool _isSync, bool _isSyncOwner, string _webServiceURL, string _socketMessage = "", SocketClient _socket = null)
        {
            InitializeComponent();

            this.socket = _socket;
            this.pptPath = _pptPath;
            this.bookId = _bookId;
            this.account = _account;
            this.userName = _userName;
            this.email = _email;
            this.meetingId = _meetingId;
            this.watermark = _watermark;
            this.dbPath = _dbPath;
            this.isSyncing = _isSync;
            this.webServiceURL = _webServiceURL;
            this.isSyncOwner = _isSyncOwner;
            this.socketMessage = _socketMessage;


            bookManager = new BookManager(dbPath);

            QueryResult rs = null;
            try
            {
                string query = "Select objectId from bookMarkDetail";
                //rs = dbConn.executeQuery(query);
                rs = bookManager.sqlCommandQuery(query);
                if (rs == null)
                {
                    //資料庫尚未更新
                    updateDataBase();
                }

                //Wayne add
                InitSyncCenter();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception@updateDataBase: " + ex.Message);

            }
            rs = null;

            langMng = new MultiLanquageManager("zh-TW");
            alterAccountWhenSyncing(this.isSyncOwner);
            getBookPath();

            //this.Initialized += _InitializedEventHandler;

            InitializeComponent();

            setWindowToFitScreen();
            this.Loaded += HTML5Reader_Loaded;

            ClearSyncOwnerPenLine();
        }
        public int getUserBookSno(string bookId, string account, string meetingId)
        {
            string query = "Select sno from bookInfo as bi "
                 + "Where bi.bookId ='" + bookId + "' "
                 + "And bi.account ='" + account + "' "
                 + "And bi.meetingId='" + meetingId + "' ";
            QueryResult rs = null;
            try
            {
                //rs = dbConn.executeQuery(query);
                rs = bookManager.sqlCommandQuery(query);
                int sno = -1;
                if (rs.fetchRow())
                {
                    sno = rs.getInt("sno");
                }
                return sno;
            }
            catch
            {
                return -1;
            }
        }


        private void ClearSyncOwnerPenLine()
        {
            if (PaperLess_Emeeting.Properties.Settings.Default.IsClearSyncOwnerPenLine == true && this.isSyncOwner == true)
            {
                getBookPath();
                int bookId = this.userBookSno;
                Exec_Access_Sql(string.Format("DELETE FROM booklastPage WHERE userbook_sno = {0}", bookId));
                Exec_Access_Sql(string.Format("DELETE FROM bookmarkDetail WHERE userbook_sno = {0}", bookId));
                Exec_Access_Sql(string.Format("DELETE FROM booknoteDetail WHERE userbook_sno = {0}", bookId));
                Exec_Access_Sql(string.Format("DELETE FROM bookStrokesDetail WHERE userbook_sno = {0}", bookId));
            }

        }

        private void Exec_Access_Sql(string SQL)
        {
            try
            {
                bookManager.sqlCommandNonQuery(SQL);
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }
        }

        private void getBookPath()
        {
            userBookSno = getUserBookSno(bookId, account, meetingId);
            if (userBookSno.Equals(-1))
            {
                string query = "Insert into bookInfo( bookId, account, meetingId )";
                query += " values('" + bookId + "', '" + account + "', '" + meetingId + "')";
                bookManager.sqlCommandNonQuery(query);

                //sqlCommandNonQuery(query);
                userBookSno = getUserBookSno(bookId, account, meetingId);
            }
        }

        private void setWindowToFitScreen()
        {

            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight - 40;

            //this.Width = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width;
            //this.Height = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height;
            this.Left = 0;
            this.Top = 0;
            this.WindowState = WindowState.Normal;
        }

        private void updateDataBase()
        {
            List<string> alterAllTable = new List<string>();
            DateTime dt = new DateTime(1970, 1, 1);
            long currentTime = DateTime.Now.ToUniversalTime().Subtract(dt).Ticks / 10000000;


            alterAllTable.Add("CREATE TABLE booklastPage ( [userbook_sno] INTEGER, [page] INTEGER, [objectId] TEXT(50), [createTime] INTEGER, [updateTime] INTEGER, [syncTime] INTEGER, [status] TEXT(50), [device] TEXT(50) )");

            //修改Column名字
            alterAllTable.Add("ALTER TABLE bookmarkDetail DROP CONSTRAINT PrimaryKey");
            alterAllTable.Add("ALTER TABLE bookmarkDetail Add COLUMN userbook_sno INTEGER");
            alterAllTable.Add("UPDATE bookmarkDetail SET sno = userbook_sno");
            alterAllTable.Add("ALTER TABLE bookmarkDetail Drop COLUMN sno");
            alterAllTable.Add("ALTER TABLE bookmarkDetail ADD CONSTRAINT PrimaryKey PRIMARY KEY (userbook_sno,page)");

            //MS Access 不能使用
            //alterAllTable.Add("ALTER TABLE bookmarkDetail RENAME COLUMN [sno] TO [userbook_sno]");

            alterAllTable.Add("ALTER TABLE bookmarkDetail ADD COLUMN [objectId] TEXT(50), [createTime] INTEGER, [updateTime] INTEGER, [syncTime] INTEGER, [status] TEXT(50) ");

            alterAllTable.Add("update bookmarkDetail set objectId='', updateTime=" + currentTime + ", createTime=" + currentTime + ", syncTime=0, status='0' Where TRUE");

            //修改Column名字
            alterAllTable.Add("ALTER TABLE booknoteDetail DROP CONSTRAINT PrimaryKey");
            alterAllTable.Add("ALTER TABLE booknoteDetail Add COLUMN userbook_sno INTEGER");
            alterAllTable.Add("UPDATE booknoteDetail SET sno = userbook_sno");
            alterAllTable.Add("ALTER TABLE booknoteDetail Drop COLUMN sno");
            alterAllTable.Add("ALTER TABLE booknoteDetail ADD CONSTRAINT PrimaryKey PRIMARY KEY (userbook_sno,page)");


            alterAllTable.Add("ALTER TABLE booknoteDetail ADD COLUMN [objectId] TEXT(50), [createTime] INTEGER, [updateTime] INTEGER, [syncTime] INTEGER, [status] TEXT(50)");

            alterAllTable.Add("update booknoteDetail set objectId='', updateTime=" + currentTime + ", createTime=" + currentTime + ", syncTime=0, status='0' Where TRUE");

            alterAllTable.Add("CREATE TABLE [bookStrokesDetail] ( [userbook_sno] INTEGER, [page] INTEGER, [objectId] TEXT(50), [createTime] INTEGER, [updateTime] INTEGER, [syncTime] INTEGER, [status] TEXT(50), [alpha] FLOAT, [canvasHeight] FLOAT, [canvasWidth] FLOAT, [color] TEXT(50), [points] MEMO, [width] FLOAT )");

            alterAllTable.Add("CREATE TABLE [cloudSyncTime](   [classKey] TEXT(100),  [lastSyncTime] INTEGER)");

            bookManager.sqlCommandNonQuery(alterAllTable);
        }

        private bool isHTML5ReaderLoaded = false;

        void HTML5Reader_Loaded(object sender, RoutedEventArgs e)
        {

            DrawingAttributes da = new DrawingAttributes();
            SolidColorBrush sb = (System.Windows.Media.SolidColorBrush)ColorTool.HexColorToBrush(PaperLess_Emeeting.Properties.Settings.Default.ReaderPenColor);
            da.Color = sb.Color;
            penMemoCanvas.DefaultDrawingAttributes = da;

            this.Loaded -= HTML5Reader_Loaded;

            web_view.MenuHandler = this;

            web_view.PropertyChanged += this.model_PropertyChanged;
            web_view.LoadCompleted += this.loadCompleted;
            web_view.RequestHandler = this;
            //web_view.Click += (sender, e) =>
            //{
            //};
            web_view.ConsoleMessage += (sender2, args2) =>
            {
                string err = string.Format(@"Webview {0}({1}): {2}",
                                                   args2.Source,
                                                   args2.Line,
                                                   args2.Message);
                Console.WriteLine(err);
                LogTool.Debug(err);
            };

            web_view.MouseLeftButtonUp += (sender2, e2) =>
            {
                if (PaperLess_Emeeting.Properties.Settings.Default.AssemblyName.Contains("TPI4F") == false)
                {
                    string tagData = web_view.Tag as string;
                    switch (tagData)
                    {
                        case "MoveUp":
                        case "MoveLeft":
                            MovePage(MovePageType.下一頁);
                            break;
                        case "MoveDown":
                        case "MoveRight":
                            MovePage(MovePageType.上一頁);
                            break;
                    }
                }
            };
            //var settings = new CefSharp.BrowserSettings { DefaultEncoding = "UTF-8" };
            // CEF.Initialize(settings);

            initJavaScript();

            tempStrokes = new List<Stroke>();

            initTimer = new DispatcherTimer();
            initTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            initTimer.IsEnabled = true;
            initTimer.Tick += new EventHandler(loadEpubFromPath);
            initTimer.Start();

            watermarkTextBlock.Text = watermark;
            Grid_33.MouseLeftButtonDown += (sender2, e2) =>
            {

                // Wayne Add
                //上方的總頁數及目前頁數顯示
                Task.Factory.StartNew(() =>
                {
                    if (isSyncing == false || (isSyncing == true && isSyncOwner == false))
                        return;

                    Thread.Sleep(700);
                    this.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        txtPage.Text = string.Format("{0} / {1}", (curPageIndex + 1).ToString(), totalPage.ToString());
                    }));
                    //Thread.Sleep(100);
                    ShowAddition(false);
                });
            };

            if (socket != null)
            {
                socket.AddEventManager(this);
            }
            else
            {
                syncButton.Visibility = Visibility.Collapsed;
                diableImg.Visibility = Visibility.Visible;
            }


            this.Closing += HTML5ReadWindow_Closing;

            isHTML5ReaderLoaded = true;

            // ChangeFlatUI(PaperLess_Emeeting.Properties.Settings.Default.IsFlatUIReader);

            //AttachKey();

            // Wayne Add
            // 用意是中途加入同步

            //Task.Factory.StartNew(() =>
            //{
            //    this.Dispatcher.BeginInvoke(new Action(() =>
            //    {
            //        if (this.socketMessage.Equals("") == false)
            //        {
            //            this.parseJSonFromMessage(this.socketMessage);
            //            //this.needToSendBroadCast = false;
            //        }
            //    }));
            //});

            this.ContentRendered += (sender3, e3) =>
            {
                InitPen();
            };

            string count = MSCE.ExecuteScalar("Select count(1) from userfile where userID=@1 and Fileid=@2 "
                            , account.Replace("_Sync", "")
                            , bookId);

            if (count.Equals("0") == false)
            {
                var dt = MSCE.GetDataTable("select FolderID from userfile  where userid =@1 and fileid=@2"
                                     , account.Replace("_Sync", "")
                                     , bookId);

                if (dt.Rows.Count > 0)
                {
                    FolderID = dt.Rows[0]["FolderID"].ToString();

                    if(FolderID.Length==0)
                    {
                        HasJoin2Folder = false;
                        imgJoin.Source = new BitmapImage(new Uri("image/ebTool-toCloud2@2x.png", UriKind.Relative));
                    }
                    else
                    {
                        HasJoin2Folder = true;
                        imgJoin.Source = new BitmapImage(new Uri("image/ebTool-inCloud2@2x.png", UriKind.Relative));
                    }
                }
            }

            if (this.FolderID == null || this.FolderID.Equals(""))
            {
                HasJoin2Folder = false;
                imgJoin.Source = new BitmapImage(new Uri("image/ebTool-toCloud2@2x.png", UriKind.Relative));
            }
            else
            {
                HasJoin2Folder = true;
                imgJoin.Source = new BitmapImage(new Uri("image/ebTool-inCloud2@2x.png", UriKind.Relative));
            }


            if (CanNotCollect)
                imgJoin.Source = new BitmapImage(new Uri("image/ebTool-toCloudDisabled2@2x.png", UriKind.Relative));

            if(cloud && !today)
            {
                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += (sender3, e3) =>
                {
                    syncButton.Visibility = Visibility.Collapsed;
                    syncButton.Width = 0;
                    syncButton.Height = 0;
                    timer.Stop();

                };
                timer.Start();
            }
        }

        bool CanSentLine = false;
        private void InitPen()
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(5000);
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    string tmpMsg = this.socketMessage;
                    if (this.socketMessage.Equals("") == false)
                    {
                        this.parseJSonFromMessage(this.socketMessage);
                        //this.needToSendBroadCast = false;
                        this.socketMessage = "";
                    }

                    if (penMemoCanvas.Strokes.Count <= 0 && tmpMsg.Equals("") == false)
                    {
                        this.parseJSonFromMessage(this.socketMessage);
                    }
                    CanSentLine = true;
                }));
            });
        }


        private void HTML5ReadWindow_Closing(object sender, CancelEventArgs e)
        {

            string myBookPath = this.pptPath;
            float width = (float)System.Windows.SystemParameters.PrimaryScreenWidth;
            float height = (float)System.Windows.SystemParameters.PrimaryScreenHeight;
            float penMemoCanvasWidth = (float)penMemoCanvas.Width;
            float penMemoCanvasHeight = (float)penMemoCanvas.Height;



            //Task.Factory.StartNew(() =>
            //{
            //    Singleton_PDFFactory.AddBookInPDFWork(this.bookId);
            //    Stopwatch sw = new Stopwatch();
            //    sw.Start();
            //    //                    string cmd = string.Format(@"SELECT page,status,alpha,canvasHeight,canvasWidth,color,points,width
            //    //                                                FROM bookStrokesDetail as a inner join bookinfo as b on a.userbook_sno=b.sno 
            //    //                                                where bookid='{0}' and account='{1}' "
            //    //                                             , this.bookId
            //    //                                             , this.account);

            //    //                    QueryResult rs = bookManager.sqlCommandQuery(cmd);

            //    //                    if (rs.fetchRow())
            //    //                    {
            //    //                        width = rs.getInt("canvasWidth");
            //    //                        height = rs.getInt("canvasHeight");

            //    //                    }
            //    //                    SavePDF(myBookPath, totalPage, width, height);
            //    //alterAccountWhenSyncing(this.isSyncOwner);
            //    string UserAccount = this.account;
            //    SavePDF(myBookPath, totalPage, penMemoCanvasWidth, penMemoCanvasHeight, UserAccount);

            //    sw.Stop();
            //    Console.WriteLine(sw.ElapsedMilliseconds);
            //    Singleton_PDFFactory.RemoveBookInPDFWork(this.bookId);
            //});
            noteButton_Click();
            //string AssemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

            if (PaperLess_Emeeting.Properties.Settings.Default.AssemblyName.Contains("TPI4F") == true)
            {
                string bookath = new FileInfo(myBookPath).Directory.FullName;
                string thumbsPath_Msize = System.IO.Path.Combine(bookath, "data", "Thumbnails");
                string thumbsPath_Lsize = System.IO.Path.Combine(bookath, "data", "Thumbnails\\Larger");

                if (penMemoCanvasWidth > 0 == false || penMemoCanvasHeight > 0 == false)
                {

                    //                string cmd = string.Format(@"SELECT page,status,alpha,canvasHeight,canvasWidth,color,points,width
                    //                                                        FROM bookStrokesDetail as a inner join bookinfo as b on a.userbook_sno=b.sno 
                    //                                                        where bookid='{0}' and page={1}  and account='{2}'"
                    //                                                         , bookId, (i - 1).ToString(), this.account);

                    //                QueryResult rs = bookManager.sqlCommandQuery(cmd);
                    //                if (rs != null && rs.fetchRow())
                    //                {
                    //                    penMemoCanvasWidth = rs.getFloat("canvasWidth");
                    //                    penMemoCanvasHeight = rs.getFloat("canvasHeight");
                    //                }
                    //                else
                    //                {
                    AutoClosingMessageBox.Show("轉檔中請稍後");
                    e.Cancel = true;
                    return;
                    //}

                }
                Singleton_PDFFactory.SavePDF(true, bookath, totalPage, penMemoCanvasWidth, penMemoCanvasHeight, this.account, this.bookId, this.dbPath, thumbsPath_Msize, thumbsPath_Lsize);
            }
            InitSyncCenter();
            RecordPage();
        }

        private void SavePDF(string bookPath, int totalPage, float width, float height, string UserAccount)
        {



            float thinWidth = 0.0f;
            float thinHeight = 0.0f;
            float fatWidth = 0.0f;
            float fatHeight = 0.0f;

            if (width > height)
            {
                fatWidth = width;
                fatHeight = height;
                thinWidth = fatHeight;
                thinHeight = fatWidth;
            }
            else
            {
                thinWidth = width;
                thinHeight = height;
                fatWidth = thinHeight;
                fatHeight = thinWidth;
            }



            iTextSharp.text.Rectangle rect = new iTextSharp.text.Rectangle(width, height);
            //將圖檔加入到PDF
            Document myDoc = new Document(rect);
            try
            {
                FileStream fs = new FileStream(System.IO.Path.Combine(bookPath, "PDF.pdf"), FileMode.Create);
                PdfWriter writer = PdfWriter.GetInstance(myDoc, fs);

                string[] files = Directory.GetFiles(bookPath, "*.bmp");

                // sort them by creation time
                Array.Sort<string>(files, delegate(string a, string b)
                {
                    int fi_a = int.Parse(new FileInfo(a).Name.Split('.')[0]);
                    int fi_b = int.Parse(new FileInfo(b).Name.Split('.')[0]);

                    return fi_a.CompareTo(fi_b);
                });

                myDoc.Open();
                int i = 0;
                //Full path to the Unicode Arial file
                //string fontPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "kaiu.ttf");
                ////Create a base font object making sure to specify IDENTITY-H
                //BaseFont bf = BaseFont.CreateFont(fontPath,
                //                                          BaseFont.IDENTITY_H, //橫式中文
                //                                          BaseFont.NOT_EMBEDDED
                //                                      );

                //Create a specific font object
                //iTextSharp.text.Font f = new iTextSharp.text.Font(bf, 12, iTextSharp.text.Font.NORMAL);

                string fontPath = Environment.GetFolderPath(Environment.SpecialFolder.System) +
                        @"\..\Fonts\kaiu.ttf";
                BaseFont bfChinese = BaseFont.CreateFont(
                    fontPath,
                    BaseFont.IDENTITY_H, //橫式中文
                    BaseFont.NOT_EMBEDDED
                );
                iTextSharp.text.Font fontChinese = new iTextSharp.text.Font(bfChinese, 16f, iTextSharp.text.Font.NORMAL);

                string pdfPath = System.IO.Path.Combine(bookPath, "data");
                string thumbsPath = "";
                string thumbsPath_Msize = System.IO.Path.Combine(bookPath, "data", "Thumbnails");
                string thumbsPath_Lsize = System.IO.Path.Combine(bookPath, "data", "Thumbnails\\Larger");
                if (Directory.Exists(thumbsPath_Lsize) == true)
                {
                    thumbsPath = thumbsPath_Lsize;
                }
                else
                {
                    thumbsPath = thumbsPath_Msize;
                }
                string[] pdfFiles = Directory.GetFiles(pdfPath, "*.png");
                string pdfPrefix = "";
                if (pdfFiles.Length > 0)
                {
                    string fName = new FileInfo(pdfFiles[0]).Name;
                    pdfPrefix = fName.Split(new char[] { '_' })[0];
                }
                for (int count = 1; count <= totalPage; count++)
                {
                    try
                    {
                        string pdf = System.IO.Path.Combine(pdfPath, pdfPrefix + "_" + count + ".pdf");
                        string thumb = System.IO.Path.Combine(thumbsPath, pdfPrefix + "_" + count + ".png");
                        string imgPath = System.IO.Path.Combine(bookPath, count + ".bmp");

                        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(thumb));
                        if (File.Exists(thumb) == false)
                        {
                            //GhostscriptWrapper.GeneratePageThumbs(pdf, thumb, 1, 1, 96, 96);

                            //consolePDFtoJPG.exe
                            //在背景執行，無DOS視窗閃爍問題
                            //System.Diagnostics.Process p = new System.Diagnostics.Process();
                            //p.StartInfo.FileName = "consolePDFtoJPG.exe";
                            //p.StartInfo.Arguments = string.Format(" {0} {1} " ,pdf,thumb);
                            //p.StartInfo.UseShellExecute = false;
                            //p.StartInfo.RedirectStandardInput = true;
                            //p.StartInfo.RedirectStandardOutput = true;
                            //p.StartInfo.RedirectStandardError = true;
                            //p.StartInfo.CreateNoWindow = true;
                            //p.Start();

                        }

                        //畫面拍攝
                        //if (File.Exists(imgPath) == false)
                        //圖形標註
                        if (1 == 1 || File.Exists(imgPath) == false)
                        {
                            //GetPdfThumbnail(pdf, imgPath);
                            //ConvertPDF2Image(pdf, imgPath, 1, 1, ImageFormat.Bmp, Definition.Ten);
                            File.Copy(thumb, imgPath, true);
                        }



                    }
                    catch (Exception ex)
                    {
                        LogTool.Debug(ex);
                    }
                }

                foreach (string file in files)
                {
                    try
                    {
                        i++;
                        FileInfo fileInfo = new FileInfo(file);
                        if (fileInfo.Extension.ToLower().Equals(".bmp"))
                        {

                            //myDoc.NewPage();
                            string cmd = string.Format(@"SELECT page,status,alpha,canvasHeight,canvasWidth,color,points,width
                                                FROM bookStrokesDetail as a inner join bookinfo as b on a.userbook_sno=b.sno 
                                                where bookid='{0}' and page={1}  and account='{2}'"
                                                   , this.bookId, (i - 1).ToString(), UserAccount);

                            QueryResult rs = bookManager.sqlCommandQuery(cmd);
                            float xWidth = 0;
                            float xHeight = 0;
                            if (rs.fetchRow())
                            {

                                xWidth = rs.getFloat("canvasWidth");
                                xHeight = rs.getFloat("canvasHeight");

                                if (xWidth > 0 && xHeight > 0)
                                {
                                    if (xWidth > xHeight)
                                    {
                                        if (fatWidth <= 0 || fatHeight <= 0)
                                        {
                                            fatWidth = width;
                                            fatHeight = height;
                                            thinWidth = fatHeight;
                                            thinHeight = fatWidth;
                                        }
                                    }
                                    else
                                    {
                                        if (thinWidth <= 0 || thinHeight <= 0)
                                        {
                                            thinWidth = width;
                                            thinHeight = height;
                                            fatWidth = thinHeight;
                                            fatHeight = thinWidth;
                                        }
                                    }
                                }
                                //myDoc.SetPageSize(new iTextSharp.text.Rectangle(xWidth, xHeight));
                            }
                            //else
                            //{


                            //    if (penMemoCanvasWidth > 0 && penMemoCanvasHeight > 0)
                            //    {
                            //        if (penMemoCanvasWidth > penMemoCanvasHeight)
                            //        {
                            //            if (penMemoCanvasWidth <= 0 || penMemoCanvasHeight <= 0)
                            //            {
                            //                fatWidth = penMemoCanvasWidth;
                            //                fatHeight = penMemoCanvasHeight;
                            //                thinWidth = fatHeight;
                            //                thinHeight = fatWidth;
                            //            }
                            //        }
                            //        else
                            //        {
                            //            if (penMemoCanvasWidth <= 0 || penMemoCanvasHeight <= 0)
                            //            {
                            //                thinWidth = penMemoCanvasWidth;
                            //                thinHeight = penMemoCanvasHeight;
                            //                fatWidth = thinHeight;
                            //                fatHeight = thinWidth;
                            //            }
                            //        }
                            //    }
                            //}


                            //


                            //加入影像
                            iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(file);
                            float WidthRatio = 1;
                            float HeightRatio = 1;


                            if (img.Width > img.Height)
                            {
                                if (fatWidth > 0 && fatHeight > 0)
                                {
                                    rect = new iTextSharp.text.Rectangle(fatWidth, fatHeight);
                                }
                                else
                                {

                                    rect = new iTextSharp.text.Rectangle(thinHeight, thinWidth);
                                }

                            }
                            else
                            {
                                if (thinWidth > 0 && thinHeight > 0)
                                {
                                    rect = new iTextSharp.text.Rectangle(thinWidth, thinHeight);
                                }
                                else
                                {

                                    rect = new iTextSharp.text.Rectangle(fatHeight, fatHeight);
                                }

                            }

                            if (xWidth > 0 && xHeight > 0)
                            {
                                WidthRatio = (rect.Width / xWidth);/// (rect.Height / xHeight);
                                HeightRatio = (rect.Height / xHeight);/// (rect.Width / xWidth);
                            }

                            myDoc.SetPageSize(rect);
                            myDoc.NewPage();
                            img.ScaleToFit(rect.Width, rect.Height);
                            //img.SetAbsolutePosition(0, rect.Height - img.Height);
                            img.SetAbsolutePosition(0, 0);
                            //if (xWidth > 0 && xHeight > 0)
                            //{
                            //    img.ScaleToFit(xWidth, xHeight);
                            //    img.SetAbsolutePosition(0, rect.Height - xHeight);
                            //}
                            //else
                            //{
                            //    img.ScaleToFit(width, height);
                            //    //img.SetAbsolutePosition(0, rect.Height - img.Height);
                            //    img.SetAbsolutePosition(0, rect.Height - img.Height);
                            //}
                            myDoc.Add(img);

                            //myDoc.Add(new iTextSharp.text.Paragraph("第 " + i.ToString() + " 頁", fontChinese));


                            //加註記
                            cmd = string.Format("select notes from booknoteDetail as a inner join bookInfo as b on a.userbook_sno=b.sno   where bookid='{0}' and page='{1}' and account='{2}'"
                                                    , this.bookId
                                                    , (i - 1).ToString()
                                                    , UserAccount);
                            rs = bookManager.sqlCommandQuery(cmd);

                            if (rs.fetchRow())
                            {
                                //myDoc.Add(new iTextSharp.text.Paragraph(rs.getString("notes"), fontChinese));
                                myDoc.Add(new iTextSharp.text.Paragraph("\r\n"));
                                //myDoc.Add(new Annotation("作者", rs.getString("notes")));
                                myDoc.Add(new Annotation("註解", rs.getString("notes")));


                            }




                            //小畫家
                            cmd = string.Format(@"SELECT page,status,alpha,canvasHeight,canvasWidth,color,points,width
                                                FROM bookStrokesDetail as a inner join bookinfo as b on a.userbook_sno=b.sno 
                                                where bookid='{0}' and page={1} and status='0' and account='{2}'"
                                                   , this.bookId
                                                   , (i - 1).ToString()
                                                   , UserAccount);


                            rs = bookManager.sqlCommandQuery(cmd);

                            while (rs.fetchRow())
                            {
                                //float imgWidth = rs.getFloat("canvasWidth");
                                //float imgHeight = rs.getFloat("canvasHeight");
                                //img.ScaleToFit(imgWidth, imgHeight);
                                //myDoc.Add(img);

                                string color = rs.getString("color");
                                float alpha = rs.getFloat("alpha");
                                int red = Convert.ToInt32(color.Substring(1, 2), 16);
                                int green = Convert.ToInt32(color.Substring(3, 2), 16);
                                int blue = Convert.ToInt32(color.Substring(5, 2), 16);
                                float PenWidth = rs.getFloat("width");

                                //PdfContentByte content = writer.DirectContent;
                                //PdfGState state = new PdfGState();
                                //state.StrokeOpacity = rs.getFloat("alpha");
                                // Stroke where the red box will be drawn
                                //content.NewPath();

                                string points = rs.getString("points");
                                string[] pointsAry = points.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                                int ii = 0;
                                float x1 = 0.0f;
                                float y1 = 0.0f;

                                List<float[]> fff = new List<float[]>();
                                List<float> fList = new List<float>();

                                foreach (string xy in pointsAry)
                                {
                                    ++ii;


                                    string x = xy.Split(new char[] { '{', ',', '}' }, StringSplitOptions.RemoveEmptyEntries)[0];
                                    string y = xy.Split(new char[] { '{', ',', '}' }, StringSplitOptions.RemoveEmptyEntries)[1];

                                    //x1 = int.Parse(x);
                                    //y1 = int.Parse(y);
                                    x1 = int.Parse(x) * WidthRatio;
                                    y1 = int.Parse(y) * HeightRatio;

                                    //if (ii == 1)
                                    //    content.MoveTo(x1, y1);
                                    //else
                                    //{
                                    //    content.LineTo(x1, y1);
                                    //}


                                    fList.Add(x1);
                                    fList.Add(rect.Height - y1);

                                }



                                fff.Add(fList.ToArray());

                                PdfAnnotation annotation = PdfAnnotation.CreateInk(writer, rect, "", fff.ToArray());
                                annotation.Color = new iTextSharp.text.BaseColor(red, green, blue, int.Parse(alpha.ToString()));
                                annotation.BorderStyle = new PdfBorderDictionary(PenWidth, PdfBorderDictionary.STYLE_SOLID);
                                //隱藏註釋符號
                                annotation.Flags = PdfAnnotation.FLAGS_PRINT;
                                writer.AddAnnotation(annotation);

                                //content.SetGState(state);
                                //content.SetRGBColorStroke(red, green, blue);
                                //content.SetLineWidth(PenWidth);
                                //content.Stroke();
                            }


                        }
                    }
                    catch (Exception ex)
                    {
                        LogTool.Debug(ex);
                    }
                }
                myDoc.AddTitle("電子書");//文件標題
                myDoc.AddAuthor("Hyweb");//文件作者
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }
            finally
            {
                try
                {
                    if (myDoc.IsOpen())
                        myDoc.Close();
                }
                catch (Exception ex)
                {
                    LogTool.Debug(ex);
                }
            }


        }

        public void RecordPage()
        {
            try
            {
                try
                {
                    sendBroadCast("{\"cmd\":\"R.CB\"}");

                    //切換SyncOwner時把之前的螢光筆清除(下一版換回來)
                    //deleteAllLocalPenmemoData();

                    if (socket != null)
                    {
                        socket.RemoveEventManager(this);
                    }
                }
                catch (Exception ex)
                {
                    LogTool.Debug(ex);
                }
                this.Closing -= HTML5ReadWindow_Closing;

                //checkImageStatusTimer.Tick -= new EventHandler(checkImageStatus);
                //checkImageStatusTimer.Stop();
                //checkImageStatusTimer.IsEnabled = false;
                //checkImageStatusTimer = null;

                saveLastReadingPage();

                //切換SyncOwner時把之前的螢光筆清除(下一版換回來)
                // wayne
                // 爛東西，被下面那句害慘了，不能放在上面，放到這裡就沒問題。
                // 會自動切換成主控，並且刪除主控的筆畫
                deleteAllLocalPenmemoData();
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }
        }

        private void saveLastReadingPage()
        {
            try
            {
                int targetPageIndex = 0;

                //if (viewStatusIndex.Equals(PageMode.SinglePage))
                //{
                //    targetPageIndex = curPageIndex;
                //}
                //else if (viewStatusIndex.Equals(PageMode.DoublePage))
                //{
                //    ReadPagePair item = doubleReadPagePair[curPageIndex];

                //    //取單頁頁數小的那頁
                //    targetPageIndex = Math.Min(item.leftPageIndex, item.rightPageIndex);
                //    if (targetPageIndex == -1)
                //    {
                //        targetPageIndex = Math.Max(item.leftPageIndex, item.rightPageIndex);
                //    }
                //}

                string CName = System.Environment.MachineName;
                DateTime dt = new DateTime(1970, 1, 1);
                long currentTime = DateTime.Now.ToUniversalTime().Subtract(dt).Ticks / 10000000;
                bool isUpdate = false;
                LastPageData blp = null;

                if (lastViewPage == null)
                {
                    blp = new LastPageData();
                    blp.index = curPageIndex + 1;
                    blp.updatetime = currentTime;
                    blp.objectId = "";
                    blp.createtime = currentTime;
                    blp.synctime = 0;
                    blp.status = "0";
                    blp.device = CName;
                    isUpdate = false;
                }
                else if (lastViewPage.ContainsKey(CName))
                {
                    blp = lastViewPage[CName];
                    blp.index = curPageIndex + 1;
                    blp.updatetime = currentTime;
                    isUpdate = true;
                }
                else
                {
                    blp = new LastPageData();
                    blp.index = curPageIndex + 1;
                    blp.updatetime = currentTime;
                    blp.objectId = "";
                    blp.createtime = currentTime;
                    blp.synctime = 0;
                    blp.status = "0";
                    blp.device = CName;
                    isUpdate = false;
                }

                bookManager.saveLastviewPage(userBookSno, isUpdate, blp);

                //bookManager.saveLastviewPage(userBookSno, blp.index, isUpdate,
                //                blp.objectId, blp.createtime, blp.updatetime, blp.synctime, blp.status, blp.device);
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }
        }

        private void deleteAllLocalPenmemoData()
        {
            bookStrokesDictionary = new Dictionary<int, List<StrokesData>>();
        }

        #region Cobra SourceCode

        private void loadCompleted(object sender, EventArgs e)
        {
            WebView thisWebView = (WebView)sender;
            string newUrl = thisWebView.Address;
            if (newUrl.StartsWith("broadcast"))
            {
                return;
                //後面JS送回的event
            }
            else
            {
                if (processing == false && !web_view.IsLoading) //true: is really LOADING, false: LOADED
                {
                    //TURN SCROLL OFF                
                    web_view.ExecuteScript("$(window).scroll(function(){android.selection.scrollTop(0); android.selection.scrollLeft(" + curLeft + "); return false;});");
                    //TURN ANCHOR OFF                
                    web_view.ExecuteScript("$('a').click(function() { return false; });");
                }
            }
        }

        protected void model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        #endregion

        #region wesley ControlCode
        private void initJavaScript()
        {
            try
            {
                fontSize = 20;
                perFontFize = 100;
                jsquery = File.ReadAllText(appPath + @"\jquery-2.0.3.js");
                //jsquery = File.ReadAllText(appPath + @"\jquery.mobile-1.4.5.min.js");
                //jsquery = File.ReadAllText(appPath + @"\jquery.touchSwipe.min.js");
                //jsquery = File.ReadAllText(appPath + @"\app.js");
                jsrangy_core = File.ReadAllText(appPath + @"\rangy-1.3alpha.772\rangy-1.3alpha.772\rangy-core.js");
                jsrangy_cssclassapplier = File.ReadAllText(appPath + @"\rangy-1.3alpha.772\rangy-1.3alpha.772\rangy-cssclassapplier.js");
                jsrangy_selectionsaverestore = File.ReadAllText(appPath + @"\rangy-1.3alpha.772\rangy-1.3alpha.772\rangy-selectionsaverestore.js");
                jsrangy_serializer = File.ReadAllText(appPath + @"\rangy-1.3alpha.772\rangy-1.3alpha.772\rangy-serializer.js");

                jsEPubAddition = File.ReadAllText(appPath + @"\epubJS\epubJS\android.selection.js");
                jsCustomSearch = File.ReadAllText(appPath + @"\epubJS\epubJS\CustomSearch.js");
                jsBackCanvas = File.ReadAllText(appPath + @"\epubJS\epubJS\backcanvas.js");

                string curAppPath = this.pptPath;
                curAppPath = System.IO.Path.GetDirectoryName(curAppPath);


                // wayne point
                string thumbnailPathFolder = curAppPath + "\\data\\Thumbnails";
                if (Directory.Exists(thumbnailPathFolder) == true)
                {
                    DirectoryInfo di = new DirectoryInfo(thumbnailPathFolder);
                    totalPage = di.GetFiles().Count();
                }

                // wayne point
                // 顯示縮圖
                string thumbnailPath = curAppPath + "\\data\\Thumbnails\\Slide1.png";
                if (File.Exists(thumbnailPath))
                {


                    // 下面是列出縮圖
                    System.Windows.Controls.Image thumbNailImageSingle = new System.Windows.Controls.Image();
                    BitmapImage bi = new BitmapImage(new Uri(thumbnailPath));
                    thumbnailWidth = bi.PixelWidth;
                    thumbnailHeight = bi.PixelHeight;
                    thumbnailRatio = thumbnailWidth / (thumbnailHeight);


                    // wayne point
                    // 顯示縮圖
                    InitSmallImage();
                }

                #region wayne point 20150417
                //依左右翻書修正下方縮圖列, 左右翻頁
                //WrapPanel wrapPanel = FindVisualChildByName<WrapPanel>(thumbNailListBox, "wrapPanel");
                //if (hejMetadata.direction.Equals("right"))
                //{
                //    wrapPanel.FlowDirection = FlowDirection.RightToLeft;

                //    RadioButton leftPageButton = FindVisualChildByName<RadioButton>(FR, "leftPageButton");
                //    leftPageButton.CommandBindings.Clear();
                //    leftPageButton.Command = NavigationCommands.NextPage;
                //    var binding = new Binding();
                //    binding.Source = FR;
                //    binding.Path = new PropertyPath("CanGoToNextPage");
                //    BindingOperations.SetBinding(leftPageButton, RadioButton.IsEnabledProperty, binding);

                //    RadioButton rightPageButton = FindVisualChildByName<RadioButton>(FR, "rightPageButton");
                //    rightPageButton.CommandBindings.Clear();
                //    rightPageButton.Command = NavigationCommands.PreviousPage;
                //    var rightbinding = new Binding();
                //    rightbinding.Source = FR;
                //    rightbinding.Path = new PropertyPath("CanGoToPreviousPage");
                //    BindingOperations.SetBinding(rightPageButton, RadioButton.IsEnabledProperty, rightbinding);

                //    KeyBinding leftKeySettings = new KeyBinding();
                //    KeyBinding rightKeySettings = new KeyBinding();

                //    InputBindings.Clear();

                //    leftKeySettings.Command = NavigationCommands.NextPage;
                //    leftKeySettings.Key = Key.Left;
                //    InputBindings.Add(leftKeySettings);

                //    rightKeySettings.Command = NavigationCommands.PreviousPage;
                //    rightKeySettings.Key = Key.Right;
                //    InputBindings.Add(rightKeySettings);
                //}
                //else
                //{
                //    wrapPanel.FlowDirection = FlowDirection.LeftToRight;
                //}

                #endregion

                basePath = curAppPath.Replace("\\", "/");
                basePath = basePath + "/";
                string[] allJS = Directory.GetFiles(curAppPath, "*.js", SearchOption.AllDirectories);

                for (int i = 0; i < allJS.Length; i++)
                {
                    jsAnimation += File.ReadAllText(allJS[i]);
                }

                string[] allCSS = Directory.GetFiles(curAppPath, "*.css", SearchOption.AllDirectories);

                for (int i = 0; i < allCSS.Length; i++)
                {
                    jsAnimation += File.ReadAllText(allCSS[i]);
                }
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }

        }

        private void setLayout()
        {
        }

        private void refreshDocument()
        {
            //TURN MOUSE OFF
            if (!VERTICAL_WRITING_MODE)
            {
                tail_JS = @"$(document).ready(function(){$(document).mousedown(function(e){ if(e.which==1) { android.selection.startTouch(e.pageX, e.pageY);} });
                $(document).keyup(function(e){ window.FORM.keyup(e.keyCode);   });
                $(document).mouseup(function(e){ if(e.which==1) { android.selection.longTouch(e); } }); })";
            }
            //else
            //    tail_JS = @"";

            string web_contents = "<script>" + jsquery + jsrangy_core + jsrangy_cssclassapplier + jsrangy_selectionsaverestore + jsrangy_serializer + jsEPubAddition + tail_JS + jsCustomSearch + jsBackCanvas + jsAnimation + "</script>" + curHtmlDoc + "<span id=\"mymarker\"></span>";
            processing = false;

            web_view.LoadHtml(web_contents, "file:///");
        }

        private string fixHtmlDocument(string showHTML)
        {
            string newHtml = "";
            string[] splitArr = showHTML.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in splitArr)
            {
                string newline = line;

                if (newline.Contains("src="))
                {
                    newline = newline.Replace("../", "");
                    newline = newline.Replace("src=\"", "src=\"" + basePath);
                }

                newHtml += newline;
            }

            string baseUrl = "<base href='" + basePath + "'/>";
            newHtml = newHtml.Replace("<head>", "<head>" + baseUrl);
            return newHtml;
        }

        #endregion

        private void TextBlock_TargetUpdated_1(string page, string animation)
        {
            if (this.socketMessage.Equals("") == false)
                InitPen();

            int pageIndex = 0;
            int animationIndex = 0;
            try
            {
                pageIndex = Convert.ToInt32(page);
                animationIndex = Convert.ToInt32(animation);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error @ TextBlock_TargetUpdated_1 Convert page & animation: " + ex.Message);
                return;
            }


            //系統初始化第一次loading
            if (!isFirstTimeLoaded)
            {


                //this.initUserDataFromDB(true);
                this.initUserDataFromDB();

                if (isSyncing && socket != null)
                {
                    syncButton.IsChecked = true;
                    isSyncing = true;
                    //socket.syncSwitch(true);
                    clearDataWhenSync();

                    //syncButton.Content = "Sync OFF";

                    if (bookMarkDictionary.ContainsKey(curPageIndex))
                    {
                        BookMarkButton.IsChecked = bookMarkDictionary[curPageIndex].status == "0" ? true : false;
                    }
                    else
                    {
                        BookMarkButton.IsChecked = false;
                    }


                    TextBox tb = FindVisualChildByName<TextBox>(MediaTableCanvas, "notePanel");
                    if (tb != null)
                    {
                        tb.Text = bookNoteDictionary[curPageIndex].text;
                    }
                    if (bookNoteDictionary.ContainsKey(curPageIndex))
                    {
                        if (bookNoteDictionary[curPageIndex].text.Equals(""))
                        {
                            NoteButton.IsChecked = false;
                        }
                        else
                        {
                            NoteButton.IsChecked = true;
                        }
                    }
                    else
                    {
                        NoteButton.IsChecked = false;
                    }

                    if (isSyncOwner)
                    {
                        //socket.setSyncOwner(account);
                        buttonStatusWhenSyncing(Visibility.Collapsed, Visibility.Collapsed);
                    }
                    else
                    {
                        buttonStatusWhenSyncing(Visibility.Visible, Visibility.Visible);
                    }
                }
                else
                {
                    syncButton.IsChecked = false;
                }

                isFirstTimeLoaded = true;

                //監聽螢光筆事件, 改由這裡直接存DB
                penMemoCanvas.StrokeCollected += penMemoCanvasStrokeCollected;
                penMemoCanvas.StrokeErasing += penMemoCanvas_StrokeErasing;
                penMemoCanvas.StrokeErased += penMemoCanvas_StrokeErased;

            }
            else
            {
                int tmpPageIndex = pageIndex - 1;

                if (curPageIndex.Equals(tmpPageIndex))
                {
                    return;
                }

                curPageIndex = tmpPageIndex;
                txtPage.Text = string.Format("{0} / {1}", (curPageIndex + 1).ToString(), totalPage.ToString());
            }

            double height = web_view.ContentsHeight;
            double width = height * thumbnailRatio;

            penMemoCanvas.Height = height;
            penMemoCanvas.Width = height * thumbnailRatio;

            openedby = MediaCanvasOpenedBy.None;


            //上方書籤以及註記狀態
            if (curPageIndex < 0)
            {
                return;
            }

            if (!(isSyncing && !isSyncOwner))
            {
                //清除上頁感應框及全文
                if (stageCanvas.Children.Count > 0)
                {
                    stageCanvas.Children.Clear();
                }

                //清除上頁螢光筆
                if (penMemoCanvas.Strokes.Count > 0)
                {
                    penMemoCanvas.Strokes.Clear();
                }

                if (this.splineString != "")
                {
                    try
                    {
                        this.drawStrokeFromJson(this.splineString);
                        this.splineString = "";
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error @TextBlock_TargetUpdated_1 drawStrokeFromJson: " + ex.Message);
                        return;
                    }
                }

                //由資料庫取回書籤資料
                bookMarkDictionary = bookManager.getBookMarkDics(userBookSno);

                //由資料庫取回註記
                bookNoteDictionary = bookManager.getBookNoteDics(userBookSno);

                //由資料庫取回螢光筆
                bookStrokesDictionary = bookManager.getStrokesDics(userBookSno);


                if (bookMarkDictionary.ContainsKey(curPageIndex))
                {
                    if (bookMarkDictionary[curPageIndex].status == "0")
                    {
                        BookMarkButton.IsChecked = true;
                    }
                    else
                    {
                        BookMarkButton.IsChecked = false;
                    }
                }
                else
                {
                    BookMarkButton.IsChecked = false;
                }

                if (bookNoteDictionary.ContainsKey(curPageIndex))
                {
                    if (bookNoteDictionary[curPageIndex].status == "0")
                    {
                        NoteButton.IsChecked = true;
                    }
                    else
                    {
                        NoteButton.IsChecked = false;
                    }
                }
                else
                {
                    NoteButton.IsChecked = false;
                }

                //if (bookStrokesDictionary.ContainsKey(curPageIndex))
                //{
                //loadCurrentStrokes(curPageIndex);
                //}


                if (this.isSyncing == true && CanSentLine == false)
                {
                    loadCurrentStrokes(0);
                    CanSentLine = true;
                }
                else
                {
                    loadCurrentStrokes(curPageIndex);

                }
            }
            //int pageIndex = 0;
            //int animationIndex = 0;
            //try
            //{
            //    pageIndex = Convert.ToInt32(page);
            //    animationIndex = Convert.ToInt32(animation);
            //}
            //catch(Exception ex)
            //{
            //    Debug.WriteLine("Error @ TextBlock_TargetUpdated_1 Convert page & animation: " + ex.Message);
            //    return;
            //}


            ////系統初始化第一次loading
            //if (!isFirstTimeLoaded)
            //{

            //    // Wayne Add
            //    if(isSyncing==false)
            //        initUserDataFromDB();

            //    if (isSyncing && socket != null)
            //    {
            //        syncButton.IsChecked = true;
            //        isSyncing = true;
            //        //socket.syncSwitch(true);
            //        clearDataWhenSync();

            //        //syncButton.Content = "Sync OFF";

            //        if (bookMarkDictionary.ContainsKey(curPageIndex))
            //        {
            //            BookMarkButton.IsChecked = bookMarkDictionary[curPageIndex].status == "0" ? true : false;
            //            TriggerBookMark_NoteButtonOrElse(BookMarkButton);
            //        }
            //        else
            //        {
            //            BookMarkButton.IsChecked = false;
            //            TriggerBookMark_NoteButtonOrElse(BookMarkButton);
            //        }


            //        TextBox tb = FindVisualChildByName<TextBox>(MediaTableCanvas, "notePanel");
            //        if (tb != null)
            //        {
            //            tb.Text = bookNoteDictionary[curPageIndex].text;
            //        }
            //        if (bookNoteDictionary.ContainsKey(curPageIndex))
            //        {
            //            if (bookNoteDictionary[curPageIndex].text.Equals(""))
            //            {
            //                NoteButton.IsChecked = false;
            //                TriggerBookMark_NoteButtonOrElse(NoteButton);
            //            }
            //            else
            //            {
            //                NoteButton.IsChecked = true;
            //                TriggerBookMark_NoteButtonOrElse(NoteButton);
            //            }
            //        }
            //        else
            //        {
            //            NoteButton.IsChecked = false;
            //            TriggerBookMark_NoteButtonOrElse(NoteButton);
            //        }

            //        if (isSyncOwner)
            //        {
            //            //socket.setSyncOwner(account);
            //            buttonStatusWhenSyncing(Visibility.Collapsed, Visibility.Collapsed);
            //        }
            //        else
            //        {
            //            buttonStatusWhenSyncing(Visibility.Visible, Visibility.Visible);
            //        }
            //    }
            //    else
            //    {
            //        syncButton.IsChecked = false;
            //    }

            //    isFirstTimeLoaded = true;

            //    //監聽螢光筆事件, 改由這裡直接存DB
            //    penMemoCanvas.StrokeCollected += penMemoCanvasStrokeCollected;
            //    penMemoCanvas.StrokeErasing += penMemoCanvas_StrokeErasing;
            //    penMemoCanvas.StrokeErased += penMemoCanvas_StrokeErased;

            //}
            //else
            //{
            //    int tmpPageIndex = pageIndex - 1;

            //    if (curPageIndex.Equals(tmpPageIndex))
            //    {
            //        return;
            //    }

            //    curPageIndex = tmpPageIndex;
            //}

            //double height = web_view.ContentsHeight;
            //double width = height * thumbnailRatio;

            //penMemoCanvas.Height = height;
            ////Wayne Marked 20150309
            //penMemoCanvas.Width = height * thumbnailRatio;

            //openedby = MediaCanvasOpenedBy.None;


            ////上方書籤以及註記狀態
            //if (curPageIndex < 0)
            //{
            //    return;
            //}

            //if (!(isSyncing && !isSyncOwner))
            //{
            //    //清除上頁感應框及全文
            //    if (stageCanvas.Children.Count > 0)
            //    {
            //        stageCanvas.Children.Clear();
            //    }

            //    //清除上頁螢光筆
            //    if (penMemoCanvas.Strokes.Count > 0)
            //    {
            //        penMemoCanvas.Strokes.Clear();
            //    }

            //    if (this.splineString != "")
            //    {
            //        try
            //        {
            //            this.drawStrokeFromJson(this.splineString);
            //            this.splineString = "";
            //        }
            //        catch (Exception ex)
            //        {
            //            Debug.WriteLine("Error @TextBlock_TargetUpdated_1 drawStrokeFromJson: " + ex.Message);
            //            return;
            //        }
            //    }

            //    //由資料庫取回書籤資料
            //    bookMarkDictionary = bookManager.getBookMarkDics(userBookSno);

            //    //由資料庫取回註記
            //    bookNoteDictionary = bookManager.getBookNoteDics(userBookSno);

            //    //由資料庫取回螢光筆
            //    bookStrokesDictionary = bookManager.getStrokesDics(userBookSno);


            //    if (bookMarkDictionary.ContainsKey(curPageIndex))
            //    {
            //        if (bookMarkDictionary[curPageIndex].status == "0")
            //        {
            //            BookMarkButton.IsChecked = true;
            //            TriggerBookMark_NoteButtonOrElse(BookMarkButton);
            //        }
            //        else
            //        {
            //            BookMarkButton.IsChecked = false;
            //            TriggerBookMark_NoteButtonOrElse(BookMarkButton);
            //        }
            //    }
            //    else
            //    {
            //        BookMarkButton.IsChecked = false;
            //        TriggerBookMark_NoteButtonOrElse(BookMarkButton);
            //    }

            //    if (bookNoteDictionary.ContainsKey(curPageIndex))
            //    {
            //        if (bookNoteDictionary[curPageIndex].status == "0")
            //        {
            //            NoteButton.IsChecked = true;
            //            TriggerBookMark_NoteButtonOrElse(NoteButton);
            //        }
            //        else
            //        {
            //            NoteButton.IsChecked = false;
            //            TriggerBookMark_NoteButtonOrElse(NoteButton);
            //        }
            //    }
            //    else
            //    {
            //        NoteButton.IsChecked = false;
            //        TriggerBookMark_NoteButtonOrElse(NoteButton);
            //    }

            //    //if (bookStrokesDictionary.ContainsKey(curPageIndex))
            //    //{
            //        loadCurrentStrokes(curPageIndex);
            //    //}
            //}


        }

        private delegate void setImgSourceCallback(string page, string animation);

        private bool ifAskedJumpPage = false;
        private string CName = System.Environment.MachineName;

        private bool isFirstTimeLoaded = false;

        bool IRequestHandler.OnBeforeBrowse(IWebBrowser browser, IRequest request, NavigationType naigationvType, bool isRedirect)
        {
            if (request.Url.StartsWith("broadcast"))
            {
                //"{\"f\" : \"currentStep\", \"msg\" : \"2\"}"
                string newUrl = request.Url.Replace("broadcast://", "");
                try
                {
                    Dictionary<string, string> msgJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(newUrl);

                    if (msgJson.ContainsKey("f"))
                    {
                        string action = msgJson["f"];
                        if (action.Equals("currentStep"))
                        {
                            //{"cmd" : "R.PP", "page" : "5", "animations" : "2"}
                            string command = "{\"cmd\":\"R.PP\", \"page\":" + msgJson["page"] + ", \"animations\":" + msgJson["animations"] + "}";
                            sendBroadCast(command);
                            if (this.isSyncOwner == true)
                                Thread.Sleep(500);
                            setImgSourceCallback setImgCallBack = new setImgSourceCallback(TextBlock_TargetUpdated_1);
                            this.Dispatcher.Invoke(setImgCallBack, msgJson["page"], msgJson["animations"]);

                            //{"cmd" : "R.PP", "page" : "5", "animations" : "2"}
                            //string command = "{\"cmd\":\"R.PP\", \"page\":" + msgJson["page"] + ", \"animations\":" + msgJson["animations"] + "}";
                            //sendBroadCast(command);
                        }
                        else if (action.Equals("videoAction"))
                        {
                            string relativePath = msgJson["source"];
                            string videoPath = basePath + relativePath;

                            setImgSourceCallback setImgCallBack = new setImgSourceCallback(prepareVideoCmd);
                            this.Dispatcher.Invoke(setImgCallBack, relativePath, videoPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("parse Json error @OnBeforeBrowse: " + ex.Message);
                }

                return true;
            }

            return false;
        }

        bool IRequestHandler.OnBeforeResourceLoad(IWebBrowser browser, IRequestResponse requestResponse)
        {
            IRequest request = requestResponse.Request;
            //if (request.Url.StartsWith(resource_url))
            //{
            //    Stream resourceStream = new MemoryStream(Encoding.UTF8.GetBytes(
            //        "<html><body><h1>Success</h1><p>This document is loaded from a System.IO.Stream</p></body></html>"));
            //    requestResponse.RespondWith(resourceStream, "text/html");
            //}

            return false;
        }

        void IRequestHandler.OnResourceResponse(IWebBrowser browser, string url, int status, string statusText, string mimeType, WebHeaderCollection headers)
        {

        }

        bool IRequestHandler.GetDownloadHandler(IWebBrowser browser, string mimeType, string fileName, long contentLength, ref IDownloadHandler handler)
        {
            return true;
        }

        bool IRequestHandler.GetAuthCredentials(IWebBrowser browser, bool isProxy, string host, int port, string realm, string scheme, ref string username, ref string password)
        {
            return false;
        }

        private string _managerId;
        public string managerId
        {
            get
            {
                return _managerId;
            }
            set
            {
                _managerId = value;
            }
        }

        private string _msg;
        public string msg
        {
            get
            {
                return _msg;
            }
            set
            {
                _msg = value;
            }
        }

        private string _clientId;
        public string clientId
        {
            get
            {
                return _clientId;
            }
            set
            {
                _clientId = value;
            }
        }

        public void run()
        {
            Debug.WriteLine("run(): " + msg);
            parseJSonFromMessage(msg);
        }

        //bool IsRinit = false;
        //int RinitCount = 0;
        private void parseJSonFromMessage(string message)
        {
            Dictionary<string, Object> msgJson = JsonConvert.DeserializeObject<Dictionary<string, Object>>(message);
            long reciveTime = (long)(SocketClient.GetCurrentTimeInUnixMillis() - (ulong)((long)msgJson["sendTime"]));

            string cmd = msgJson["cmd"].ToString();

            if (cmd.Equals("broadcast"))
            {
                Dictionary<string, Object> msgStrings = new Dictionary<string, object>();
                foreach (KeyValuePair<string, Object> msgKeyValue in msgJson)
                {
                    string output = JsonConvert.SerializeObject(msgKeyValue.Value);
                    output = output.Substring(1, output.Length - 2).Replace("\\\"", "\"");
                    msgStrings = JsonConvert.DeserializeObject<Dictionary<string, Object>>(output);
                    break;
                }
                if (!msgStrings.Count.Equals(0))
                {
                    setMsgToAction(msgStrings);
                }
            }
            else if (cmd.Equals("R.init"))
            {

                Dictionary<string, Object> msgStrings = new Dictionary<string, object>();
                foreach (KeyValuePair<string, Object> msgKeyValue in msgJson)
                {
                    string output = JsonConvert.SerializeObject(msgKeyValue.Value);
                    output = output.Substring(1, output.Length - 2).Replace("\\\"", "\"");
                    msgStrings = JsonConvert.DeserializeObject<Dictionary<string, Object>>(output);
                    break;
                }
                if (!msgStrings.Count.Equals(0))
                {
                    Task.Factory.StartNew(() =>
                    {
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            web_view.ExecuteScript("goToStep(" + "0" + ", " + "0" + ")");
                            string tmp = msgStrings["spline"].ToString();
                            setMsgToAction(msgStrings);
                            penMemoCanvas.Strokes.Clear();
                            drawStrokeFromJson(tmp);
                        }));
                    });
                }
            }

        }

        //由於和 msg 在不同的Thread, 必須用Dispatcher才有辦法換 User Thread上的東西
        private void setMsgToAction(Dictionary<string, Object> msgStrings)
        {
            setMsgToActionCallback setImgCallBack = new setMsgToActionCallback(setMsgToActionDelegate);
            this.Dispatcher.Invoke(setImgCallBack, msgStrings);
        }

        Dictionary<int, string> dictCache = new Dictionary<int, string>();
        private delegate void setMsgToActionCallback(Dictionary<string, Object> msgStrings);
        private void setMsgToActionDelegate(Dictionary<string, Object> msgStrings)
        {


            if (!msgStrings.ContainsKey("cmd"))
            {
                string page = "";
                string animations = "";
                foreach (KeyValuePair<string, Object> initStatus in msgStrings)
                {
                    if (initStatus.Value != null)
                    {
                        switch (initStatus.Key)
                        {
                            case "bookId":
                                //closeBook = true;
                                break;
                            case "pageIndex":
                                if (msgStrings["pageIndex"] != null)
                                {
                                    string pageIndex = msgStrings["pageIndex"].ToString();
                                    //bringBlockIntoView(Convert.ToInt32(pageIndex));
                                    this.Dispatcher.BeginInvoke((Action)(() =>
                                    {
                                        txtPage.Text = string.Format("{0} / {1}", pageIndex, totalPage.ToString());
                                    }));

                                }
                                break;
                            case "annotation":
                                break;
                            case "bookmark":
                                break;
                            case "spline":
                                try
                                {
                                    splineString = msgStrings["spline"].ToString();

                                    if (splineString != null && splineString.Equals("") == false)
                                        this.drawStrokeFromJson(splineString);
                                }
                                catch
                                {
                                    //spline為null
                                }
                                break;
                            case "page":
                                try
                                {
                                    page = msgStrings["page"].ToString();
                                }
                                catch
                                {
                                    //spline為null
                                }
                                break;
                            case "animations":
                                try
                                {
                                    animations = msgStrings["animations"].ToString();
                                }
                                catch
                                {
                                    //spline為null
                                }
                                break;
                        }
                    }
                }

                if (page != "" && animations != "")
                {
                    try
                    {
                        web_view.ExecuteScript("goToStep(" + page + ", " + animations + ")");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ExecuteScript_Exception@R.PP: " + ex.Message);
                    }
                }
            }
            else
            {
                string msgString = msgStrings["cmd"].ToString();
                string pageIndex = "";

                switch (msgString)
                {
                    case "R.PP.V":
                        string bookId = msgStrings["bookId"].ToString();
                        string path = msgStrings["path"].ToString();
                        string action = msgStrings["action"].ToString();
                        if (action.Equals("start"))
                        {
                            string filePath = basePath + path.Replace("test2", "");
                            Debug.WriteLine("path: " + filePath);



                            if (dictCache.Count > 0)
                            {
                                //return;
                            }
                            else
                            {
                                dictCache[1] = "";
                                Task.Factory.StartNew(() =>
                                {
                                    Thread.Sleep(6000);
                                    dictCache.Clear();
                                });
                                mp = new MoviePlayer(filePath, true, false);
                                mp.ShowDialog();
                            }
                        }
                        else if (action.Equals("stop"))
                        {
                            if (mp != null)
                            {
                                mp.Close();
                                mp = null;
                            }
                        }
                        break;
                    case "R.PP":
                        string page = msgStrings["page"].ToString();
                        string animations = msgStrings["animations"].ToString();


                        // 如果不是當前頁才清除
                        // 有換頁才清除。
                        if (!page.Equals((curPageIndex + 1).ToString()))
                            penMemoCanvas.Strokes.Clear();
                        try
                        {
                            web_view.ExecuteScript("goToStep(" + page + ", " + animations + ")");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("ExecuteScript_Exception@R.PP: " + ex.Message);
                        }
                        break;

                    case "syncOwner":
                        isSyncing = true;
                        string syncClientId = msgStrings["clientId"].ToString();
                        penMemoCanvas.Strokes.Clear();
                        if (clientId.Equals(syncClientId))
                        {
                            //切換SyncOwner時把之前的螢光筆清除(下一版換回來)
                            deleteAllLocalPenmemoData();

                            alterAccountWhenSyncing(true);

                            isSyncOwner = true;
                            buttonStatusWhenSyncing(Visibility.Collapsed, Visibility.Collapsed);
                            loadCurrentStrokes(curPageIndex);
                        }
                        else
                        {
                            //切換SyncOwner時把之前的螢光筆清除(下一版換回來)
                            deleteAllLocalPenmemoData();

                            alterAccountWhenSyncing(false);
                            isSyncOwner = false;
                            buttonStatusWhenSyncing(Visibility.Visible, Visibility.Visible);
                        }
                        break;
                    case "R.SB":
                        pageIndex = msgStrings["pageIndex"].ToString();
                        int pageNumber = Convert.ToInt32(pageIndex);

                        string bookMark = msgStrings["bookmark"].ToString();

                        BookMarkData bmd = new BookMarkData()
                        {
                            index = pageNumber,
                            status = "0"
                        };

                        bool isMarked = false;
                        if (bookMark.Equals("1"))
                        {
                            isMarked = true;
                        }

                        bmd.status = bookMark.Equals("1") ? "0" : "1";

                        if (bookMarkDictionary.ContainsKey(pageNumber))
                        {
                            bookMarkDictionary[pageNumber] = bmd;
                            //if (pageNumber.Equals(curPageIndex))
                            //{
                            //    BookMarkRb.IsChecked = bookMarkDictionary[pageNumber];
                            //}
                        }
                        break;
                    //開啟註記框 Annotate Action
                    //{"msg":"{\"cmd\":\"R.AA\"}","sender":"hyweb001","sendTime":1369031583,"cmd":"broadcast"}
                    case "R.AA":
                        //先關閉
                        //doUpperRadioButtonClicked(MediaCanvasOpenedBy.NoteButton, NoteButton);
                        break;
                    //設定註記文字 Set Annotation
                    //{"msg":"{\"annotation\":\"註記文字\",\"pageIndex\":0,\"cmd\":\"R.SA\"}","sender":"hyweb001","sendTime":1369031585,"cmd":"broadcast"}
                    case "R.SA":
                        pageIndex = msgStrings["pageIndex"].ToString();
                        string annotation = msgStrings["annotation"].ToString();
                        TextBox tb = FindVisualChildByName<TextBox>(MediaTableCanvas, "notePanel");
                        annotation = annotation.Replace("\\n", "\n").Replace("\\t", "\t");
                        tb.Text = annotation;
                        int targetPageIndex = Convert.ToInt32(pageIndex);
                        NoteData nd = new NoteData() { bookid = this.bookId, text = annotation, index = targetPageIndex, status = "0" };


                        bookNoteDictionary[targetPageIndex] = nd;
                        if (tb.Text.Equals(""))
                        {
                            NoteButton.IsChecked = false;
                            TriggerBookMark_NoteButtonOrElse(NoteButton);
                        }
                        else
                        {
                            NoteButton.IsChecked = true;
                            TriggerBookMark_NoteButtonOrElse(NoteButton);
                        }
                        break;
                    //關閉對話框 Dismiss Popover Action
                    //{"msg":"{\"cmd\":\"R.DPA\"}","sender":"hyweb001","sendTime":1369031588,"cmd":"broadcast"}
                    case "R.DPA":
                        if (MediaTableCanvas.Visibility.Equals(Visibility.Visible))
                        {
                            doUpperRadioButtonClicked(MediaCanvasOpenedBy.NoteButton, NoteButton);
                        }
                        break;
                    //關書 Close Book
                    //{"msg":"{\"cmd\":\"R.CB\"}","sender":"hyweb001","sendTime":1369031632,"cmd":"broadcast"}
                    case "R.CB":
                        this.Close();
                        break;
                    //設定螢光筆 Set Spline
                    //收到要畫
                    //{"msg":"{\"spline\":[{\"strokeWidth\":3,\"canvasHeight\":1024,\"strokeAlpha\":1,\"canvasWidth\":722.8235,\"strokeColor\":\"#202BFA\",\"points\":\"{276.07846, 248};{268.07846, 264};{262.74515, 280};{262.74515, 280};{260.07846, 288};{258.74515, 293.33334};{256.07846, 298.66666};{254.74513, 302.66666};{253.4118, 306.66666};{252.07848, 309.33334};{252.07848, 312};{252.07848, 312}\"}],\"pageIndex\":0,\"cmd\":\"R.SS\"}","sender":"hyweb001","sendTime":1369031709,"cmd":"broadcast"}
                    //半徑
                    case "R.SS":
                        penMemoCanvas.Strokes.Clear();
                        pageIndex = msgStrings["pageIndex"].ToString();


                        //try
                        //{
                        //    web_view.ExecuteScript("goToStep(" + pageIndex + ", 0)");
                        //}
                        //catch(Exception ex)
                        //{
                        //    Debug.WriteLine("ExecuteScript_Exception@R.PP: " + ex.Message);
                        //}

                        //bringBlockIntoView(Convert.ToInt32(pageIndex));
                        try
                        {
                            drawStrokeFromJson(msgStrings["spline"].ToString());
                            //if (IsRinit == true)
                            //{
                            //    IsRinit = false;
                            //    drawStrokeFromJson(msgStrings["spline"].ToString());
                            //}
                        }
                        catch
                        {
                            //spline為null
                            break;
                        }
                        break;
                    default:
                        break;
                }
            }

            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(700);
                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    txtPage.Text = string.Format("{0} / {1}", (curPageIndex + 1).ToString(), totalPage.ToString());
                }));
                //Thread.Sleep(100);
                ShowAddition(false);
            });
        }

        private void drawStrokeFromJson(string msgString)
        {
            try
            {
                double height = web_view.ContentsHeight;
                //double width = height;
                double width = height * thumbnailRatio;

                List<PemMemoInfos> resultStrArray = JsonConvert.DeserializeObject<List<PemMemoInfos>>(msgString);
                for (int i = 0; i < resultStrArray.Count; i++)
                {
                    //paintStrokeOnInkCanvas(resultStrArray[i], height, width);
                    paintStrokeOnInkCanvas(resultStrArray[i], width, height);
                    //paintStrokeOnInkCanvas(resultStrArray[i], web_view.ActualWidth, web_view.ActualHeight);
                }
            }
            catch
            {
                //同步格式錯誤
            }
        }

        private void alterAccountWhenSyncing(bool isSyncOwner)
        {
            this.account = this.account.Replace("_Sync", "");
            if (isSyncOwner)
            {
                //改變同步時的帳號, 原account_Sync
                this.account = this.account + "_Sync";
            }


            //改變userBookSno
            getBookPath();
        }

        private void paintStrokeOnInkCanvas(PemMemoInfos strokeJson, double currentInkcanvasWidth, double currentInkcanvasHeight)
        {
            try
            {
                double strokeWidth = strokeJson.strokeWidth;
                double canvasHeight = strokeJson.canvasHeight;
                double canvasWidth = strokeJson.canvasWidth;
                double strokeAlpha = strokeJson.strokeAlpha;
                string strokeColor = strokeJson.strokeColor;

                //double strokeWidth = Convert.ToDouble(strokeJson["strokeWidth"].ToString());
                //double canvasHeight = Convert.ToDouble(strokeJson["canvasHeight"].ToString());
                //double canvasWidth = Convert.ToDouble(strokeJson["canvasWidth"].ToString());
                //double strokeAlpha = Convert.ToDouble(strokeJson["strokeAlpha"].ToString());
                //string strokeColor = strokeJson["strokeColor"].ToString();

                double widthScaleRatio = currentInkcanvasWidth / canvasWidth;
                double heightScaleRatio = currentInkcanvasHeight / canvasHeight;
                //double heightScaleRatio;

                //if (canvasHeight < 250)
                //    heightScaleRatio = currentInkcanvasHeight / canvasHeight;
                //else
                //    heightScaleRatio = (currentInkcanvasHeight-20) / currentInkcanvasHeight;

                String[] points = strokeJson.points.Split(';');
                //String[] points = strokeJson["points"].ToString().Split(';');
                char[] charStr = { '{', '}' };
                StylusPointCollection pointsList = new StylusPointCollection();

                for (int i = 0; i < points.Length; i++)
                {
                    try
                    {
                        System.Windows.Point p = new System.Windows.Point();
                        //StylusPoint p = new StylusPoint();
                        string s = points[i];
                        s = s.TrimEnd(charStr);
                        s = s.TrimStart(charStr);
                        p = System.Windows.Point.Parse(s);
                        StylusPoint sp = new StylusPoint();
                        sp.X = p.X * widthScaleRatio;
                        sp.Y = p.Y * heightScaleRatio;

                        pointsList.Add(sp);
                    }
                    catch (Exception ex)
                    {
                        LogTool.Debug(ex);
                    }
                }

                if (pointsList.Count < 1)
                {
                    StylusPoint sp = new StylusPoint();
                    sp.X = 0;
                    sp.Y = 0;
                    pointsList.Add(sp);
                }

                Stroke targetStroke = new Stroke(pointsList);
                targetStroke.DrawingAttributes.FitToCurve = true;
                if (strokeAlpha != 1)
                {
                    targetStroke.DrawingAttributes.IsHighlighter = true;
                }
                else
                {
                    targetStroke.DrawingAttributes.IsHighlighter = false;
                }
                targetStroke.DrawingAttributes.Width = strokeWidth * 5;
                targetStroke.DrawingAttributes.Height = strokeWidth * 5;
                System.Windows.Media.ColorConverter ccr = new System.Windows.Media.ColorConverter();
                System.Windows.Media.Color clr = ConvertHexStringToColour(strokeColor);
                targetStroke.DrawingAttributes.Color = clr;

                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(5);
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (isSyncing)
                        {
                            if (targetStroke != null)
                            {
                                //penMemoCanvas.DefaultDrawingAttributes = targetStroke.DrawingAttributes;
                                //penMemoCanvas.UpdateLayout();
                                penMemoCanvas.Strokes.Add(targetStroke.Clone());
                                targetStroke = null;
                            }
                        }
                    }));
                });
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }

        }


        //螢光筆同步處理
        private void sendBroadCast(string msg)
        {
            if (isSyncOwner && isSyncing)
            {
                if (!String.IsNullOrEmpty(msg) && socket != null)
                {
                    if (socket != null && socket.GetIsConnected() == true)
                    {
                        //Task.Factory.StartNew(() =>
                        //{

                        //        if (msg.Contains(@"""cmd"":""R.SS""}"))
                        //        {
                        //            Thread.Sleep(200);
                        //        }
                        //        socket.broadcast(msg);
                        //});
                        //Thread.Sleep(1);
                        socket.broadcast(msg);
                    }
                }
            }
        }


        public bool OnBeforeMenu(IWebBrowser browser)
        {
            return true;
        }

        private void loadEpubFromPath(object sender, EventArgs e)
        {
            initTimer.Tick -= new EventHandler(loadEpubFromPath);
            initTimer.Stop();
            initTimer.IsEnabled = false;
            initTimer = null;


            tail_JS = @"$(document).ready(function(){$(document).mousedown(function(e){ if(e.which==1) { android.selection.startTouch(e.pageX, e.pageY);} });
                //$(document).mouseup(function(e){ if(e.which==1) {android.selection.up(e.pageX, e.pageY); window.FORM.showMsg('shit'); return false;} });
                $(document).mouseup(function(e){ if(e.which==1) { android.selection.longTouch(e); } }); })";

            processing = true;


            // wayne point 20141215
            string web_contents = "<script>" + jsquery + jsrangy_core + jsrangy_cssclassapplier + jsrangy_selectionsaverestore + jsrangy_serializer + jsEPubAddition + tail_JS + jsCustomSearch + jsBackCanvas + "</script>" + curHtmlDoc + "<span id=\"mymarker\"></span>";
            web_view.LoadHtml(web_contents, "file:///");

            Thread.Sleep(100);

            string fileContent = "";

            using (FileStream tocStream = new FileStream(pptPath, FileMode.Open))
            {
                StreamReader xr = new StreamReader(tocStream);
                fileContent = xr.ReadToEnd();
                xr.Close();
                xr = null;
                tocStream.Close();
            }

            try
            {
                curHtmlDoc = fixHtmlDocument(fileContent);
                refreshDocument();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ex:" + ex.Message);
            }

        }

        private void syncButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.socket == null)
            {
                Singleton_Socket.ReaderEvent = this;
                this.socket = Singleton_Socket.GetInstance(meetingId, account, userName, syncButton.IsChecked == true ? true : false);
                if (this.socket == null)
                {
                    syncButton.IsChecked = false;
                    MessageBox.Show("無法連接廣播同步系統", "連線失敗", MessageBoxButton.OK);
                    return;
                }
            }


            bool? isChecked = syncButton.IsChecked;
            if ((!isChecked.GetValueOrDefault() ? 0 : (isChecked.HasValue ? 1 : 0)) != 0)
            {
                //改為每畫一筆存一次
                //this.saveCurrentStrokes(this.hejMetadata.LImgList[this.curPageIndex].pageId);
                penMemoCanvas.Strokes.Clear();
                this.isSyncing = true;
                this.socket.syncSwitch(true);
                this.clearDataWhenSync();

                loadCurrentStrokes(curPageIndex);
                this.buttonStatusWhenSyncing(Visibility.Visible, Visibility.Visible);
            }
            else
            {
                //改為每畫一筆存一次
                //this.saveCurrentStrokes(this.hejMetadata.LImgList[this.curPageIndex].pageId);
                penMemoCanvas.Strokes.Clear();
                this.isSyncing = false;
                this.isSyncOwner = false;
                this.socket.syncSwitch(false);
                this.clearDataWhenSync();

                alterAccountWhenSyncing(false);

                this.initUserDataFromDB();

                loadCurrentStrokes(curPageIndex);
                this.buttonStatusWhenSyncing(Visibility.Collapsed, Visibility.Collapsed);
            }

            if (bookMarkDictionary.ContainsKey(curPageIndex))
            {
                BookMarkButton.IsChecked = bookMarkDictionary[curPageIndex].status == "0" ? true : false;
                TriggerBookMark_NoteButtonOrElse(BookMarkButton);
            }
            else
            {
                BookMarkButton.IsChecked = false;
                TriggerBookMark_NoteButtonOrElse(BookMarkButton);
            }

            TextBox tb = FindVisualChildByName<TextBox>(MediaTableCanvas, "notePanel");
            if (tb != null)
            {
                tb.Text = bookNoteDictionary[curPageIndex].text;
            }
            if (bookNoteDictionary.ContainsKey(curPageIndex))
            {
                if (bookNoteDictionary[curPageIndex].text.Equals(""))
                {
                    NoteButton.IsChecked = false;
                    TriggerBookMark_NoteButtonOrElse(NoteButton);
                }
                else
                {
                    NoteButton.IsChecked = true;
                    TriggerBookMark_NoteButtonOrElse(NoteButton);
                }
            }
            else
            {
                NoteButton.IsChecked = false;
                TriggerBookMark_NoteButtonOrElse(NoteButton);
            }


            this.switchNoteBookMarkShareButtonStatusWhenSyncing();
        }



        private void switchNoteBookMarkShareButtonStatusWhenSyncing()
        {
            if (this.isSyncing)
            {
                if (isSyncOwner == true)
                    cbBooks.Visibility = Visibility.Visible;
                else
                    cbBooks.Visibility = Visibility.Collapsed;
                BookMarkButton.Visibility = Visibility.Collapsed;
                NoteButton.Visibility = Visibility.Collapsed;
                ShareButton.Visibility = Visibility.Collapsed;
                this.BookMarkButtonInListBox.Visibility = Visibility.Collapsed;
                this.NoteButtonInListBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                cbBooks.Visibility = Visibility.Visible;
                BookMarkButton.Visibility = Visibility.Visible;
                NoteButton.Visibility = Visibility.Visible;
                ShareButton.Visibility = Visibility.Visible;
                this.BookMarkButtonInListBox.Visibility = Visibility.Visible;
                this.NoteButtonInListBox.Visibility = Visibility.Visible;
            }
        }


        //控制按鈕在同步時是否可用
        private void buttonStatusWhenSyncing(Visibility toolBarVisibility, Visibility syncCanvasVisibility)
        {
            toolbarSyncCanvas.Visibility = toolBarVisibility;
            syncCanvas.Visibility = syncCanvasVisibility;

            if (toolBarVisibility.Equals((object)Visibility.Visible) && syncCanvasVisibility.Equals((object)Visibility.Visible))
            {
                cbBooks.Opacity = 0.5;
                PenMemoButton.Opacity = 0.5;
                BookMarkButton.Opacity = 0.5;
                NoteButton.Opacity = 0.5;
                ShareButton.Opacity = 0.5;
                BackToBookShelfButton.Opacity = 0.5;
                this.ShowListBoxButton.Visibility = Visibility.Collapsed;

                if (PaperLess_Emeeting.Properties.Settings.Default.IsFlatUIReader == true)
                {
                    if (CheckIsNowClick(MemoSP) == true)
                    {
                        noteButton_Click();
                    }
                    NewUITop.Visibility = Visibility.Collapsed;
                    NewUI.Visibility = Visibility.Collapsed;

                    statusBMK.Width = 0;
                    statusBMK.Height = 0;
                    statusMemo.Width = 0;
                    statusMemo.Height = 0;
                }


            }
            else
            {
                cbBooks.Opacity = 1.0;
                PenMemoButton.Opacity = 1.0;
                BookMarkButton.Opacity = 1.0;
                NoteButton.Opacity = 1.0;
                ShareButton.Opacity = 1.0;
                BackToBookShelfButton.Opacity = 1.0;
                this.ShowListBoxButton.Visibility = Visibility.Visible;

                if (PaperLess_Emeeting.Properties.Settings.Default.IsFlatUIReader == true)
                {
                    NewUITop.Visibility = Visibility.Visible;
                    NewUI.Visibility = Visibility.Visible;

                    statusBMK.Width = 56;
                    statusBMK.Height = 56;
                    statusMemo.Width = 56;
                    statusMemo.Height = 56;
                }
            }

            if (this.isSyncOwner)
            {
                StatusOnairOff.Visibility = Visibility.Collapsed;
                screenBroadcasting.Visibility = Visibility.Visible;
                screenReceiving.Visibility = Visibility.Collapsed;
            }
            else if (this.isSyncing)
            {
                StatusOnairOff.Visibility = Visibility.Collapsed;
                screenBroadcasting.Visibility = Visibility.Collapsed;
                screenReceiving.Visibility = Visibility.Visible;
            }
            else
            {
                StatusOnairOff.Visibility = Visibility.Visible;
                screenBroadcasting.Visibility = Visibility.Collapsed;
                screenReceiving.Visibility = Visibility.Collapsed;
            }
        }



        //private void initUserDataFromDB(bool isFirstTimeLoaded = false)
        private void initUserDataFromDB()
        {
            //由資料庫取回上次瀏覽頁
            string CName = System.Environment.MachineName;

            lastViewPage = bookManager.getLastViewPageObj(userBookSno);

            if (lastViewPage.ContainsKey(CName))
            {

                //if (1==1)
                if (lastViewPage[CName].index > 0)
                {
                    //curPageIndex
                    //  第幾頁 ， 第幾個動畫
                    if (!lastViewPage[CName].index.Equals((curPageIndex + 1).ToString()))
                        penMemoCanvas.Strokes.Clear();
                    try
                    {
                        string animations = "0";
                        //if (isFirstTimeLoaded == false)
                        //{

                        if (this.isSyncing == true && CanSentLine == false)
                        {
                            txtPage.Text = string.Format("{0} / {1}", "1", totalPage.ToString());
                        }
                        else
                        {
                            web_view.ExecuteScript("goToStep(" + lastViewPage[CName].index + ", " + animations + ")");
                            // Wayne Add
                            //上方的總頁數及目前頁數顯示
                            txtPage.Text = string.Format("{0} / {1}", lastViewPage[CName].index.ToString(), totalPage.ToString());
                        }

                        //}
                    }
                    catch (Exception ex)
                    {

                        Debug.WriteLine("ExecuteScript_Exception@R.PP: " + ex.Message);
                    }


                }

                //if (lastViewPage[CName].index > 0)
                //{
                //    //lastViewPage[CName].index
                //    //curPageIndex

                //    //  第幾頁 ， 第幾個動畫
                //    web_view.ExecuteScript("goToStep(" + page + ", " + animations + ")");
                //}
            }

            if (!isSyncing)
            {
                //由資料庫取回螢光筆
                bookStrokesDictionary = bookManager.getStrokesDics(userBookSno);

                //由資料庫取回書籤資料
                bookMarkDictionary = bookManager.getBookMarkDics(userBookSno);

                //由資料庫取回註記
                bookNoteDictionary = bookManager.getBookNoteDics(userBookSno);
            }
        }

        private void clearDataWhenSync()
        {
            bookMarkDictionary = new Dictionary<int, BookMarkData>();
            bookNoteDictionary = new Dictionary<int, NoteData>();

            //bookMarkDictionary = new Dictionary<int, bool>();
            //bookNoteDictionary = new Dictionary<int, string>();

            //for (int i = 0; i < hejMetadata.LImgList.Count; i++)
            //{
            //    bookMarkDictionary.Add(i, false);
            //    bookNoteDictionary.Add(i, "");
            //}
        }

        public static T FindVisualChildByName<T>(DependencyObject parent, string name) where T : DependencyObject
        {
            if (parent != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    string controlName = child.GetValue(Control.NameProperty) as string;
                    if (controlName == name)
                    {
                        return child as T;
                    }
                    else
                    {
                        T result = FindVisualChildByName<T>(child, name);
                        if (result != null)
                            return result;
                    }
                }
            }
            return null;
        }

        private void loadCurrentStrokes(int curIndex)
        {
            #region Paperless Meeting

            if (this.isSyncing && !this.isSyncOwner)
                return;

            #endregion


            //penMemoCanvas.Height = web_view.ContentsHeight;
            //penMemoCanvas.Width = web_view.ContentsHeight * thumbnailRatio;


            //penMemoCanvas.Width = web_view.ContentsWidth;
            //penMemoCanvas.Height = web_view.ContentsHeight;

            //由資料庫取回註記
            bookStrokesDictionary = bookManager.getStrokesDics(userBookSno);

            //從DB取資料
            if (bookStrokesDictionary.ContainsKey(curIndex))
            {
                List<StrokesData> curPageStrokes = bookStrokesDictionary[curIndex];
                int strokesCount = curPageStrokes.Count;
                for (int i = 0; i < strokesCount; i++)
                {
                    if (curPageStrokes[i].status == "0")
                    {
                        paintStrokeOnInkCanvas(curPageStrokes[i], penMemoCanvas.Width, penMemoCanvas.Height, 0, 0);
                    }
                }
            }

            preparePenMemoAndSend();
        }

        public string splineString { get; set; }

        public int curPageIndex { get; set; }

        //private void preparePenMemoAndSend()
        private void preparePenMemoAndSend(bool Division3 = true)
        {
            if (isSyncOwner && isSyncing)
            {
                //string totalPenMemoString = "";
                int strokesCount = penMemoCanvas.Strokes.Count;
                List<PemMemoInfos> PemMemoInfosList = new List<PemMemoInfos>();
                for (int i = 0; i < strokesCount; i++)
                {
                    int pointCount = penMemoCanvas.Strokes[i].StylusPoints.Count;
                    DrawingAttributes d = penMemoCanvas.Strokes[i].DrawingAttributes;
                    PemMemoInfos pm = new PemMemoInfos();

                    //pm.strokeWidth = (int)(d.Height / 3);
                    pm.strokeWidth = (int)d.Height;
                    if (pm.strokeWidth < 1)
                        pm.strokeWidth = 1;

                    //wayne 微調
                    //if (isFirstLoad == true)
                    if (Division3 == true && pm.strokeWidth > 1 && d.FitToCurve == true)
                    {
                        pm.strokeWidth = (pm.strokeWidth / 3) * 0.6;
                    }
                    else
                    {
                        pm.strokeWidth = pm.strokeWidth * 0.75;
                    }
                    if (pm.strokeWidth < 1)
                        pm.strokeWidth = 1;

                    //pm.canvasHeight = (int)penMemoCanvas.ActualHeight;
                    //pm.canvasWidth = (int)penMemoCanvas.ActualWidth;
                    pm.canvasHeight = (int)penMemoCanvas.Height;
                    pm.canvasWidth = (int)penMemoCanvas.Width;
                    pm.strokeAlpha = (d.IsHighlighter ? 0.5 : 1);

                    string colorString = d.Color.ToString();
                    colorString = colorString.Remove(1, 2);

                    pm.strokeColor = colorString;




                    string pointsMsg = "";
                    for (int j = 0; j < pointCount; j++)
                    {
                        StylusPoint stylusPoint = penMemoCanvas.Strokes[i].StylusPoints[j];
                        pointsMsg += "{" + stylusPoint.X.ToString() + ", " + stylusPoint.Y.ToString() + "};";
                    }
                    pointsMsg = pointsMsg.Substring(0, pointsMsg.LastIndexOf(';'));
                    pm.points = pointsMsg;
                    PemMemoInfosList.Add(pm);
                    //Dictionary<string, Object> splineDic = new Dictionary<string, Object>();
                    //splineDic.Add("strokeWidth", (int)d.Height);
                    //splineDic.Add("canvasHeight", penMemoCanvas.ActualHeight);
                    //splineDic.Add("canvasWidth", penMemoCanvas.ActualWidth);
                    //splineDic.Add("strokeAlpha", d.IsHighlighter ? 0.5 : 1);
                    //splineDic.Add("strokeColor", d.Color.ToString());
                    //string pointsMsg = "";
                    //for (int j = 0; j < pointCount; j++)
                    //{
                    //    StylusPoint stylusPoint = penMemoCanvas.Strokes[i].StylusPoints[j];
                    //    pointsMsg += "{" + stylusPoint.X.ToString() + ", " + stylusPoint.Y.ToString() + "};";
                    //}
                    //pointsMsg = pointsMsg.Substring(0, pointsMsg.LastIndexOf(';'));
                    //splineDic.Add("points", pointsMsg);

                    //string output = JsonConvert.SerializeObject(splineDic);
                    ////output = output.Substring(1, output.Length - 2).Replace("\\\"", "\"");
                    //output = output.Replace("\\\"", "\"");
                    //totalPenMemoString += output + ",";
                }

                string json = JsonConvert.SerializeObject(PemMemoInfosList);
                json = json.Replace("\r\n", "").Replace("[", "").Replace("]", "");
                string[] stringArray = new string[] { json };
                //totalPenMemoString = totalPenMemoString.Substring(0, totalPenMemoString.LastIndexOf(','));
                //string[] stringArray = new string[] { totalPenMemoString };
                Dictionary<string, Object> msgDic = new Dictionary<string, Object>();
                //msgDic.Add("spline", "[" + output + "]");
                msgDic.Add("spline", stringArray);
                msgDic.Add("pageIndex", curPageIndex);
                msgDic.Add("cmd", "R.SS");

                string result = JsonConvert.SerializeObject(msgDic);
                result = result.Replace("[\"", "[").Replace("\"]", "]").Replace("\\\"", "\"").Replace(" ", "");


                //Task.Factory.StartNew(() =>
                //{
                //        Thread.Sleep(800);
                sendBroadCast(result);
                //});
            }
        }

        private MediaCanvasOpenedBy openedby = MediaCanvasOpenedBy.None;
        private int clickedPage = 0;

        private void doUpperRadioButtonClicked(MediaCanvasOpenedBy whichButton, object sender)
        {
            if (openedby.Equals(whichButton))
            {
                if (MediaTableCanvas.Visibility.Equals(Visibility.Visible))
                {
                    if (!whichButton.Equals(MediaCanvasOpenedBy.NoteButton))
                    {
                        ((RadioButton)sender).IsChecked = false;
                    }
                    else
                    {
                        if (whichButton.Equals(MediaCanvasOpenedBy.NoteButton))
                        {
                            TextBox tb = FindVisualChildByName<TextBox>(MediaTableCanvas, "notePanel");
                            // wayne add 
                            //tb.Width = 200;
                            //tb.TextWrapping = TextWrapping.Wrap;

                            if (tb != null)
                            {
                                int targetPageIndex = curPageIndex;


                                bool isSuccessd = setNotesInMem(tb.Text, targetPageIndex);

                                if (tb.Text.Equals(""))
                                {
                                    NoteButton.IsChecked = false;
                                    TriggerBookMark_NoteButtonOrElse(NoteButton);
                                }
                                else
                                {
                                    NoteButton.IsChecked = true;
                                    TriggerBookMark_NoteButtonOrElse(NoteButton);
                                }
                            }

                        }
                    }
                    MediaTableCanvas.Visibility = Visibility.Collapsed;
                    sendBroadCast("{\"cmd\":\"R.DPA\"}");
                }
                else
                {
                    sendBroadCast("{\"cmd\":\"R.AA\"}");
                    MediaTableCanvas.Visibility = Visibility.Visible;

                    if (whichButton.Equals(MediaCanvasOpenedBy.NoteButton))
                    {
                        TextBox tb = FindVisualChildByName<TextBox>(MediaTableCanvas, "notePanel");
                        if (tb != null)
                        {
                            int targetPageIndex = curPageIndex;

                            bool isSuccessd = setNotesInMem(tb.Text, targetPageIndex);

                            tb.Text = bookNoteDictionary.ContainsKey(targetPageIndex) ? bookNoteDictionary[targetPageIndex].text : "";

                            if (tb.Text.Equals(""))
                            {
                                NoteButton.IsChecked = false;
                                TriggerBookMark_NoteButtonOrElse(NoteButton);
                            }
                            else
                            {
                                NoteButton.IsChecked = true;
                                TriggerBookMark_NoteButtonOrElse(NoteButton);
                                sendBroadCast("{\"annotation\":\"" + tb.Text + "\",\"pageIndex\":" + targetPageIndex.ToString() + ",\"cmd\":\"R.SA\"}");
                            }
                        }

                    }
                }

                if (openedby == MediaCanvasOpenedBy.NoteButton)
                {
                    TextBox tb = FindVisualChildByName<TextBox>(mediaListPanel, "notePanel");
                    tb.Select(tb.Text.Length, 0);
                    tb.Focus();
                    return;
                }

            }

            //if (!whichButton.Equals(MediaCanvasOpenedBy.NoteButton))
            //{
            string childNameInReader = "";
            switch (openedby)
            {
                case MediaCanvasOpenedBy.SearchButton:
                    childNameInReader = "SearchButton";
                    break;
                case MediaCanvasOpenedBy.MediaButton:
                    childNameInReader = "MediaListButton";
                    break;
                case MediaCanvasOpenedBy.CategoryButton:
                    childNameInReader = "TocButton";
                    break;
                case MediaCanvasOpenedBy.NoteButton:
                    childNameInReader = "NoteButton";
                    break;
                case MediaCanvasOpenedBy.ShareButton:
                    childNameInReader = "ShareButton";
                    break;
                case MediaCanvasOpenedBy.SettingButton:
                    childNameInReader = "SettingsButton";
                    break;
                default:
                    break;
            }
            if (!childNameInReader.Equals("") && !childNameInReader.Equals("NoteButton"))
            {
                NoteButton.IsChecked = false;
                TriggerBookMark_NoteButtonOrElse(NoteButton);
            }
            //}

            clickedPage = curPageIndex;
            mediaListPanel.Children.Clear();

            if (RelativePanel.ContainsKey(whichButton) && !whichButton.Equals(MediaCanvasOpenedBy.NoteButton))
            {
                mediaListPanel.Children.Add(RelativePanel[whichButton]);


            }
            else
            {
                StackPanel sp = new StackPanel();
                double panelWidth = mediaListPanel.Width;
                switch (whichButton)
                {
                    // wayne add
                    case MediaCanvasOpenedBy.SearchButton:
                        sp = getSearchPanelSet(panelWidth, "");
                        //showPenToolPanelEventHandler(150, 400);
                        break;

                    case MediaCanvasOpenedBy.NoteButton:
                        //Wayne Mark 20150327
                        sp = getNotesAndMakeNote();
                        //sendBroadCast("{\"cmd\":\"R.AA\"}");
                        //showPenToolPanelEventHandler(150, 400);
                        break;
                    default:
                        break;
                }

                if (RelativePanel.ContainsKey(whichButton))
                {
                    RelativePanel[whichButton] = sp;
                }
                else
                {
                    RelativePanel.Add(whichButton, sp);
                }
                mediaListPanel.Children.Clear();
                mediaListPanel.Children.Add(RelativePanel[whichButton]);

            }



            MediaTableCanvas.Visibility = Visibility.Visible;
            openedby = whichButton;

            resetFocusBackToReader();

            if (openedby == MediaCanvasOpenedBy.NoteButton)
            {
                TextBox tb = FindVisualChildByName<TextBox>(mediaListPanel, "notePanel");
                tb.Select(tb.Text.Length, 0);
                tb.Focus();
            }
        }


        private Dictionary<MediaCanvasOpenedBy, StackPanel> RelativePanel
            = new Dictionary<MediaCanvasOpenedBy, StackPanel>();


        #region 註記


        private StackPanel getNotesAndMakeNote()
        {
            double panelWidth = mediaListPanel.Width;
            double panelHeight = mediaListPanel.Height;
            // mediaListPanel.Height = defaultMediaListHeight;
            //Border mediaListBorder = FindVisualChildByName<Border>(FR, "mediaListBorder");
            //mediaListBorder.Height = defaultMediaListHeight;
            //mediaListPanel.Height = 400;
            double buttonWidth = 100;
            double buttonHeight = 20;

            string textToShow = bookNoteDictionary.ContainsKey(curPageIndex) ? bookNoteDictionary[curPageIndex].text : "";

            StackPanel sp = new StackPanel();

            TextBox noteTB = new TextBox()
            {
                Name = "notePanel",
                // Wayne Add
                // 會失效，改成Wrap
                // TextWrapping = TextWrapping.WrapWithOverflow,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                BorderBrush = System.Windows.Media.Brushes.White,
                Margin = new Thickness(2),
                Width = panelWidth - 4,
                Height = panelHeight - buttonHeight - 8,
                Text = textToShow,
                FontSize = 16
            };
            noteTB.KeyDown += noteTB_KeyDown;
            noteTB.TextChanged += noteTB_TextChanged;

            RadioButton noteButton = new RadioButton()
            {
                Content = new TextBlock()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = System.Windows.Media.Brushes.White,
                    Text = langMng.getLangString("save")       //"儲存"
                },
                Background = System.Windows.Media.Brushes.Black,
                Margin = new Thickness(2),
                Width = buttonWidth,
                Height = buttonHeight
            };


            // Wayne Add
            //Button noteButton = new Button()
            //{
            //    Content = new TextBlock()
            //    {
            //        VerticalAlignment = VerticalAlignment.Center,
            //        HorizontalAlignment = HorizontalAlignment.Center,
            //        Foreground = System.Windows.Media.Brushes.White,
            //        Text = langMng.getLangString("save")       //"儲存"
            //    },
            //    Background = System.Windows.Media.Brushes.Black,
            //    Margin = new Thickness(2),
            //    Width = buttonWidth,
            //    Height = buttonHeight
            //};

            noteButton.Click += noteButton_Click;
            //noteTB.ToolTip = "最多可輸入255個字";
            noteTB.MouseEnter += (sender, e) =>
            {
                //ToolTip tp=(ToolTip)noteTB.ToolTip;
                //tp.IsOpen = true;
                //tp.StaysOpen = true;
            };
            //TextBox tb = new TextBox() { Text = "最多可輸入255個字" };
            //tb.Margin = new Thickness(15);
            //tb.Padding = new Thickness(15);
            //tb.FontSize = 17;
            //tb.FontWeight = FontWeights.Bold;
            //tb.Foreground = ColorTool.HexColorToBrush("#999999");
            //tb.BorderThickness = new Thickness(0);
            //tb.Background = System.Windows.Media.Brushes.White;
            //noteTB.Background = new VisualBrush(tb)
            //{
            //    AlignmentX = AlignmentX.Center,
            //    AlignmentY = AlignmentY.Bottom,
            //    Opacity = 0.7,
            //    Stretch = Stretch.None
            //};
            //noteTB.PreviewKeyDown += (sender, e) =>
            //{
            //    if (noteTB.Text.Length >= 255 && e.Key != Key.Back && e.Key != Key.Enter && e.Key != Key.Home && e.Key != Key.End
            //        && e.Key != Key.Up && e.Key != Key.Down && e.Key != Key.Left && e.Key != Key.Right)
            //    {
            //        e.Handled = true;
            //        AutoClosingMessageBox.Show("已達到註記字數限制");
            //    }
            //};
            sp.Children.Add(noteTB);
            sp.Children.Add(noteButton);

            sp.Orientation = Orientation.Vertical;

            return sp;
        }



        // wayne add
        private StackPanel getSearchPanelSet(double panelWidth, string txtInSearchBar)
        {
            StackPanel sp = new StackPanel();
            sp.Name = "spParent";
            RadioButton searchButton = new RadioButton()
            {
                Content = new TextBlock()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = System.Windows.Media.Brushes.White,
                    Text = langMng.getLangString("search")       //"搜尋"
                },
                Background = System.Windows.Media.Brushes.Black,
                Margin = new Thickness(6),
                Width = 61,
            };

            searchButton.Click += searchButton_Click;

            TextBox searchTB = new TextBox()
            {
                Name = "searchBar",
                Text = txtInSearchBar,
                Margin = new Thickness(6),
                Width = panelWidth - 82
            };
            searchTB.KeyDown += searchTB_KeyDown;

            sp.Children.Add(searchTB);
            sp.Children.Add(searchButton);
            sp.Orientation = Orientation.Horizontal;
            sp.Background = System.Windows.Media.Brushes.LightGray;
            //stackPanel.Children.Add(sp);
            return sp;
        }

        // wayne add
        private StackPanel GetMediaListPanelInReader()
        {
            StackPanel canvas = FindVisualChildByName<StackPanel>(FR, "mediaListPanel");

            //wayne add
            //StackPanel canvas = mediaListPanel;
            return canvas;
        }

        // wayne add
        private ListBox hyftdSearch(string keyWord)
        {
            string curAppPath = System.IO.Path.GetDirectoryName(this.pptPath);
            string hyftdDir = curAppPath + "\\data\\fulltext";

            string[] categoryNameArray = Directory.GetFiles(hyftdDir);

            List<SearchRecord> srList = new List<SearchRecord>();

            for (int i = 0; i < categoryNameArray.Length; i++)
            {
                string htmlCode = "";
                //using (FileStream htmlStream = new FileStream(categoryNameArray[i], FileMode.Open))
                //wayne Add 20140826
                using (FileStream htmlStream = new FileStream(categoryNameArray[i], FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    using (var reader = new StreamReader(htmlStream))
                    {
                        htmlCode = reader.ReadToEnd();
                    }
                }
                string searchResult = searchKeyword(keyWord, htmlCode);
                if (!searchResult.Equals(""))
                {
                    //加入縮圖

                    //得到 "2-9d6f-P_1.txt"
                    //要轉換成 "Slide1.png"
                    string filename = categoryNameArray[i].Replace(hyftdDir + "\\", "");

                    // wayne mark has a bug ,because filename not correct
                    // "2-9d6f-P_1.txt"=> "2-9d6f-P_1"
                    filename = filename.Replace(filename.Substring(filename.LastIndexOf('.')), "");

                    int pageNum = 1;
                    try
                    {
                        pageNum = int.Parse(filename.Split('_')[1]);
                    }
                    catch (Exception ex)
                    {
                        LogTool.Debug(ex);
                    }
                    filename = string.Format("Slide{0}.png", pageNum);

                    // wayne mark has a bug ,because  hejMetadata is null
                    //for (int k = 0; k < hejMetadata.SImgList.Count; k++)
                    //{
                    //    if (hejMetadata.SImgList[k].path.Contains(filename))
                    //    {
                    SearchRecord sr = new SearchRecord(pageNum.ToString(), htmlCode, pageNum + 1);
                    //string bookPath = this.pptPath;
                    string bookPath = curAppPath;
                    sr.imagePath = bookPath + "\\data\\Thumbnails\\" + filename;
                    srList.Add(sr);
                    //    }
                    //}
                }

                //string nodeSource = navigationList[i].value;
                ////取html檔的路徑檔名
                //int innerLinkStart = nodeSource.IndexOf("#");
                //if (innerLinkStart > 0)
                //{
                //    nodeSource = nodeSource.Substring(0, innerLinkStart);
                //}
                //if (HTMLCode[i].Length == 0)
                //{
                //    HTMLCode[i] = getDesFileContents(basePath + nodeSource);
                //}

                //buildTocTree(skey, node, cmbTOC.Items[i].ToString(), HTMLCode[i], i);
            }

            //簡繁互轉, 需加ChineseConverter.dll
            //if (ChineseTraditional)
            //{
            //    htmlCode = ChineseConverter.Convert(htmlCode, ChineseConversionDirection.SimplifiedToTraditional);
            //}
            //else
            //{
            //    htmlCode = ChineseConverter.Convert(htmlCode, ChineseConversionDirection.TraditionalToSimplified);
            //}


            ListBox lb = new ListBox();
            lb.Style = (Style)FindResource("SearchListBoxStyle");

            lb.ItemsSource = srList;
            lb.SelectionChanged += lb_SelectionChanged;
            return lb;
        }

        // wayne add
        private string searchKeyword(string skey, string txtStr)
        {
            string[] splitArr = txtStr.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            short showSearchLen = 30;
            //bool checkFlag =false;

            string sbody = "";
            string showStr = "";
            short matchs = 0;
            short startAt = 0;
            short idx = 0;
            skey = skey.ToUpper();

            //一行一行處理
            foreach (string s in splitArr)
            {
                sbody = sbody.ToUpper();    //關鍵字和內文都轉大寫, 英文書比較好搜尋    
                matchs = 0;
                startAt = 0;
                idx = 0;
                sbody = s;
                //一個字元一個字元比對
                foreach (char ch in sbody)
                {
                    //符合關鍵字
                    if (ch == skey[matchs])
                    {
                        if (matchs == 0)
                        {
                            //記下開始的置
                            startAt = idx;
                        }
                        //'每個符合字元就累加
                        matchs++;
                    }
                    else
                    {
                        //如果不符合就歸零
                        matchs = 0;
                    }

                    //找到內文和關鍵字一樣長, 表示找到了
                    if (matchs == skey.Length)
                    {
                        matchs = 0;
                        //'取出要秀在樹狀結果列的文字
                        showStr = sbody.Substring(startAt);
                        if (showStr.Length > showSearchLen)
                        {
                            showStr = showStr.Substring(0, showSearchLen - 1);
                        }
                    }
                    idx++;
                }
            }

            return showStr;
        }

        // wayne add
        #region wayne add 全文檢索

        // wayne add
        void searchTB_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                // wayne remove
                //StackPanel mediaListPanel = GetMediaListPanelInReader();
                //TextBox tb = FindVisualChildByName<TextBox>(FR, "searchBar");
                // wayne add
                //StackPanel spParent = mediaListPanel.Children.OfType<StackPanel>().FirstOrDefault();
                //TextBox tb = spParent.Children.OfType<TextBox>().FirstOrDefault();
                TextBox tb = (TextBox)sender;
                string txt = tb.Text;
                double panelWidth = mediaListPanel.Width;
                mediaListPanel.Children.Clear();

                StackPanel sp = getSearchPanelSet(panelWidth, txt);
                ListBox resultLB = hyftdSearch(txt);

                StackPanel searchPanel = new StackPanel();
                searchPanel.Children.Add(sp);
                searchPanel.Children.Add(resultLB);
                RelativePanel[MediaCanvasOpenedBy.SearchButton] = searchPanel;
                mediaListPanel.Children.Add(searchPanel);

            }
        }

        // wayne add
        void searchButton_Click(object sender, RoutedEventArgs e)
        {
            // wayne remove
            //StackPanel mediaListPanel = GetMediaListPanelInReader();
            //TextBox tb = FindVisualChildByName<TextBox>(mediaListPanel, "searchBar");
            // wayne add
            //StackPanel spParent = mediaListPanel.Children.OfType<StackPanel>().FirstOrDefault();
            //TextBox tb = spParent.Children.OfType<TextBox>().FirstOrDefault();
            //string txt = "";
            //if (tb != null)
            //{
            //    txt = tb.Text;
            //}

            TextBox tb = FindVisualChildByName<TextBox>(mediaListPanel, "searchBar");
            string txt = tb.Text;
            double panelWidth = mediaListPanel.Width;
            mediaListPanel.Children.Clear();

            StackPanel sp = getSearchPanelSet(panelWidth, txt);
            ListBox resultLB = hyftdSearch(txt);

            StackPanel searchPanel = new StackPanel();
            searchPanel.Children.Add(sp);
            searchPanel.Children.Add(resultLB);
            RelativePanel[MediaCanvasOpenedBy.SearchButton] = searchPanel;
            mediaListPanel.Children.Add(searchPanel);
        }

        // wayne add
        private PageMode viewStatusIndex = PageMode.SinglePage;

        // wayne add
        void lb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = (ListBox)sender;
            if (lb.SelectedIndex != -1)
            {
                //按下跳頁, 等待後面data binding
                int index = ((SearchRecord)(e.AddedItems[0])).targetPage;
                int targetPageIndex = index - 1;

                //if (viewStatusIndex.Equals(PageMode.DoublePage))
                //{
                //    targetPageIndex = getDoubleCurPageIndex(targetPageIndex);
                //}

                if (!targetPageIndex.Equals(-1))
                {
                    //bringBlockIntoView(targetPageIndex);
                    string page = targetPageIndex.ToString();
                    string animations = "0";
                    web_view.ExecuteScript("goToStep(" + page + ", " + animations + ")");
                }
                lb.SelectedIndex = -1;

                // Wayne Add
                //上方的總頁數及目前頁數顯示
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(700);
                    this.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        txtPage.Text = string.Format("{0} / {1}", (curPageIndex + 1).ToString(), totalPage.ToString());
                    }));
                    //Thread.Sleep(100);
                });
            }
        }

        #endregion

        void noteTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = FindVisualChildByName<TextBox>(MediaTableCanvas, "notePanel");
            int targetPageIndex = curPageIndex;
            sendBroadCast("{\"annotation\":\"" + tb.Text + "\",\"pageIndex\":" + targetPageIndex.ToString() + ",\"cmd\":\"R.SA\"}");
        }

        void noteTB_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                TextBox tb = (TextBox)sender;
                tb.Text = tb.Text + "\r\n";
                int targetPageIndex = curPageIndex;
                sendBroadCast("{\"annotation\":\"" + tb.Text + "\",\"pageIndex\":" + targetPageIndex.ToString() + ",\"cmd\":\"R.SA\"}");
            }
        }




        //Wayne 沒用到
        private void btnPen_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            // Do double-click code
            //打開
            Canvas.SetZIndex(penMemoCanvas, 900);
            Canvas.SetZIndex(stageCanvas, 2);
            Canvas.SetZIndex(web_view, 850);

            web_view.IsHitTestVisible = false;
            penMemoCanvas.IsHitTestVisible = true;
            stageCanvas.IsHitTestVisible = false;

            penMemoCanvas.Background = System.Windows.Media.Brushes.Transparent;
            penMemoCanvas.EditingMode = InkCanvasEditingMode.Ink;

            penMemoCanvas.Visibility = Visibility.Visible;

            //strokeToolPanelHorizontal.HorizontalAlignment = HorizontalAlignment.Right;
            //PenMemoToolBar.Children.Add(strokeToolPanelHorizontal);

            //alterPenmemoAnimation(strokeToolPanelHorizontal, 0, strokeToolPanelHorizontal.Width);

            //偵聽換筆畫事件
            //strokeToolPanelHorizontal.strokeChange += new StrokeChangeEvent(strokeChaneEventHandler);
            //strokeToolPanelHorizontal.strokeUndo += new StrokeUndoEvent(strokeUndoEventHandler);
            //strokeToolPanelHorizontal.strokeDelAll += new StrokeDeleteAllEvent(strokeDelAllEventHandler);
            //strokeToolPanelHorizontal.strokeRedo += new StrokeRedoEvent(strokeRedoEventHandler);
            //strokeToolPanelHorizontal.strokeDel += new StrokeDeleteEvent(strokDelEventHandler);
            //strokeToolPanelHorizontal.showPenToolPanel += new showPenToolPanelEvent(showPenToolPanelEventHandler);
            //strokeToolPanelHorizontal.strokeErase += new StrokeEraseEvent(strokeEraseEventHandler);
            //strokeToolPanelHorizontal.strokeCurve += new StrokeCurveEvent(strokeCurveEventHandler);
            //strokeToolPanelHorizontal.strokeLine += new StrokeLineEvent(strokeLineEventHandler);
            //strokeRedoEventHandler
            penMemoCanvas.Focus();

            if (HiddenControlCanvas.Visibility.Equals(Visibility.Collapsed))
            {
                HiddenControlCanvas.Visibility = Visibility.Visible;
            }

            Keyboard.ClearFocus();

            //把其他的按鈕都disable
            //disableAllOtherButtons(true);

            if (isStrokeLine)
            {
                strokeLineEventHandler();
            }
            else
            {
                strokeCurveEventHandler();
            }




        }

        void noteButton_Click(object sender, RoutedEventArgs e)
        {
            noteButton_Click();
        }

        private void noteButton_Click()
        {
            try
            {
                TextBox tb = FindVisualChildByName<TextBox>(MediaTableCanvas, "notePanel");
                int targetPageIndex = curPageIndex;
                bool isSuccessd = setNotesInMem(tb.Text, targetPageIndex);

                bookManager.saveNoteData(userBookSno, targetPageIndex.ToString(), tb.Text);
                if (tb.Text.Equals(""))
                {
                    NoteButton.IsChecked = false;
                    TriggerBookMark_NoteButtonOrElse(NoteButton);
                }
                else
                {
                    NoteButton.IsChecked = true;
                    TriggerBookMark_NoteButtonOrElse(NoteButton);
                }

                MediaTableCanvas.Visibility = Visibility.Collapsed;

                sendBroadCast("{\"annotation\":\"" + tb.Text + "\",\"pageIndex\":" + targetPageIndex.ToString() + ",\"cmd\":\"R.SA\"}");

                sendBroadCast("{\"cmd\":\"R.DPA\"}");


            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }


        }

        private bool setNotesInMem(string text, int targetPageIndex)
        {
            bool setSuccess = false;

            DateTime dt = new DateTime(1970, 1, 1);

            //server上的儲存時間的格式是second, Ticks單一刻度表示千萬分之一秒

            long currentTime = DateTime.Now.ToUniversalTime().Subtract(dt).Ticks / 10000000;
            bool isUpdate = false;

            NoteData bn = null;
            //如果原頁有註記
            if (bookNoteDictionary.ContainsKey(curPageIndex))
            {
                bn = bookNoteDictionary[targetPageIndex];
                //如果原註記相同
                if (bn.text == text)
                {
                    return setSuccess;
                }

                bn.text = text;
                bn.updatetime = currentTime;

                //到這裡如果原頁有紀錄現在為空, 則為刪除, 否則為修改
                if (bn.text != "")
                {
                    bn.status = "0";
                    isUpdate = true;
                }
                else
                {
                    bn.status = "1";
                    isUpdate = false;
                }
            }
            else
            {
                //如果原頁沒有註記
                if (text == "")
                {
                    //且對話框也沒有註記
                    return setSuccess;
                }

                //新增一筆資料
                bn = new NoteData();
                bn.objectId = "";
                bn.createtime = currentTime;
                bn.updatetime = currentTime;
                bn.text = text;
                bn.index = targetPageIndex;
                bn.status = "0";
                bn.synctime = 0;
                bookNoteDictionary.Add(targetPageIndex, bn);

                isUpdate = false;
            }
            bookManager.saveNoteData(userBookSno, isUpdate, bn);

            //bookManager.saveNoteData(userBookSno, bn.index, bn.text, isUpdate, bn.objectId, bn.createtime, bn.updatetime, bn.synctime, bn.status);

            setSuccess = true;

            return setSuccess;
        }

        private void PenMemoButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton PenMemoButton = (RadioButton)sender;

            openedby = MediaCanvasOpenedBy.PenMemo;
            StrokeToolPanelHorizontal strokeToolPanelHorizontal = new StrokeToolPanelHorizontal();
            strokeToolPanelHorizontal.langMng = this.langMng;

            if (PenMemoToolBar.Visibility.Equals(Visibility.Collapsed))
            {
                ToolBarInReader.Visibility = Visibility.Collapsed;
                PenMemoToolBar.Visibility = Visibility.Visible;
                PenMemoButton.IsChecked = false;


                strokeToolPanelHorizontal.determineDrawAtt(penMemoCanvas.DefaultDrawingAttributes, isStrokeLine);

                //打開
                Canvas.SetZIndex(penMemoCanvas, 900);
                Canvas.SetZIndex(stageCanvas, 2);
                Canvas.SetZIndex(web_view, 850);

                web_view.IsHitTestVisible = false;
                penMemoCanvas.IsHitTestVisible = true;
                stageCanvas.IsHitTestVisible = false;

                penMemoCanvas.Background = System.Windows.Media.Brushes.Transparent;
                penMemoCanvas.EditingMode = InkCanvasEditingMode.Ink;

                penMemoCanvas.Visibility = Visibility.Visible;

                strokeToolPanelHorizontal.HorizontalAlignment = HorizontalAlignment.Right;
                PenMemoToolBar.Children.Add(strokeToolPanelHorizontal);

                alterPenmemoAnimation(strokeToolPanelHorizontal, 0, strokeToolPanelHorizontal.Width);

                //偵聽換筆畫事件
                strokeToolPanelHorizontal.strokeChange += new StrokeChangeEvent(strokeChaneEventHandler);
                strokeToolPanelHorizontal.strokeUndo += new StrokeUndoEvent(strokeUndoEventHandler);
                strokeToolPanelHorizontal.strokeDelAll += new StrokeDeleteAllEvent(strokeDelAllEventHandler);
                strokeToolPanelHorizontal.strokeRedo += new StrokeRedoEvent(strokeRedoEventHandler);
                strokeToolPanelHorizontal.strokeDel += new StrokeDeleteEvent(strokDelEventHandler);
                strokeToolPanelHorizontal.showPenToolPanel += new showPenToolPanelEvent(showPenToolPanelEventHandler);
                strokeToolPanelHorizontal.strokeErase += new StrokeEraseEvent(strokeEraseEventHandler);
                strokeToolPanelHorizontal.strokeCurve += new StrokeCurveEvent(strokeCurveEventHandler);
                strokeToolPanelHorizontal.strokeLine += new StrokeLineEvent(strokeLineEventHandler);
                //strokeRedoEventHandler
                penMemoCanvas.Focus();

                if (HiddenControlCanvas.Visibility.Equals(Visibility.Collapsed))
                {
                    HiddenControlCanvas.Visibility = Visibility.Visible;
                }

                Keyboard.ClearFocus();

                //把其他的按鈕都disable
                //disableAllOtherButtons(true);

                if (isStrokeLine)
                {
                    strokeLineEventHandler();
                }
                else
                {
                    strokeCurveEventHandler();
                }
            }
            else
            {
                //關閉
                Canvas.SetZIndex(web_view, 1);
                Canvas.SetZIndex(penMemoCanvas, 2);
                Canvas.SetZIndex(stageCanvas, 3);

                web_view.IsHitTestVisible = true;
                penMemoCanvas.IsHitTestVisible = false;
                stageCanvas.IsHitTestVisible = false;

                ((RadioButton)sender).IsChecked = false;
                penMemoCanvas.EditingMode = InkCanvasEditingMode.None;

                alterPenmemoAnimation(strokeToolPanelHorizontal, strokeToolPanelHorizontal.Width, 0);

                PenMemoToolBar.Children.Remove(PenMemoToolBar.Children[PenMemoToolBar.Children.Count - 1]);
                if (PopupControlCanvas.Visibility.Equals(Visibility.Visible))
                {
                    PopupControlCanvas.Visibility = Visibility.Collapsed;
                }

                if (HiddenControlCanvas.Visibility.Equals(Visibility.Visible))
                {
                    HiddenControlCanvas.Visibility = Visibility.Collapsed;
                }

                PenMemoToolBar.Visibility = Visibility.Collapsed;
                ToolBarInReader.Visibility = Visibility.Visible;
            }
        }

        #endregion


        public bool isStrokeLine { get; set; }


        #region 螢光筆處理

        private double originalCanvasWidth = 1;
        private double originalCanvasHeight = 1;
        private double fullScreenCanvasWidth = 1;
        private double fullScreenCanvasHeight = 1;
        private double baseStrokesCanvasWidth = 0;
        private double baseStrokesCanvasHeight = 0;

        private void paintStrokeOnInkCanvas(StrokesData strokeJson, double currentInkcanvasWidth, double currentInkcanvasHeight, double offsetX, double offsetY)
        {
            double strokeWidth = strokeJson.width;
            double canvasHeight = strokeJson.canvasheight;
            double canvasWidth = strokeJson.canvaswidth;
            double strokeAlpha = strokeJson.alpha;
            string strokeColor = strokeJson.color;

            double widthScaleRatio = currentInkcanvasWidth / canvasWidth;
            double heightScaleRatio = currentInkcanvasHeight / canvasHeight;

            String[] points = strokeJson.points.Split(';');
            //String[] points = strokeJson["points"].ToString().Split(';');
            char[] charStr = { '{', '}' };
            StylusPointCollection pointsList = new StylusPointCollection();

            for (int i = 0; i < points.Length; i++)
            {
                System.Windows.Point p = new System.Windows.Point();
                //StylusPoint p = new StylusPoint();
                string s = points[i];
                s = s.TrimEnd(charStr);
                s = s.TrimStart(charStr);
                p = System.Windows.Point.Parse(s);
                StylusPoint sp = new StylusPoint();
                sp.X = p.X * widthScaleRatio;
                sp.Y = p.Y * heightScaleRatio;

                pointsList.Add(sp);
            }

            Stroke targetStroke = new Stroke(pointsList);
            targetStroke.DrawingAttributes.FitToCurve = true;
            if (strokeAlpha != 1)
            {
                targetStroke.DrawingAttributes.IsHighlighter = true;
            }
            else
            {
                targetStroke.DrawingAttributes.IsHighlighter = false;
            }
            targetStroke.DrawingAttributes.Width = strokeWidth * 3;
            targetStroke.DrawingAttributes.Height = strokeWidth * 3;
            System.Windows.Media.ColorConverter ccr = new System.Windows.Media.ColorConverter();
            System.Windows.Media.Color clr = ConvertHexStringToColour(strokeColor);
            targetStroke.DrawingAttributes.Color = clr;

            Matrix moveMatrix = new Matrix(1, 0, 0, 1, offsetX, 0);
            if (targetStroke != null)
            {
                //把解好的螢光筆畫到inkcanvas上
                targetStroke.Transform(moveMatrix, false);
                penMemoCanvas.Strokes.Add(targetStroke.Clone());
                targetStroke = null;
            }

        }

        void penMemoCanvas_StrokeErasing(object sender, InkCanvasStrokeErasingEventArgs e)
        {
            Stroke thisStroke = e.Stroke;
            if (thisStroke == null)
            {
                return;
            }

            InkCanvas penMemoCanvas = (InkCanvas)sender;

            //此頁有筆畫, 與DB資料庫中比對, 萬一有筆畫少, 則刪除
            List<StrokesData> curPageStrokes = bookManager.getCurPageStrokes(userBookSno, curPageIndex);

            int curPageStrokesCount = curPageStrokes.Count;

            for (int j = 0; j < curPageStrokesCount; j++)
            {
                bool isSamePoint = compareStrokeInDB(thisStroke, curPageStrokes[j], penMemoCanvas.ActualWidth, penMemoCanvas.ActualHeight);

                if (isSamePoint)
                {
                    //server上的儲存時間的格式是second, Ticks單一刻度表示千萬分之一秒
                    DateTime dt = new DateTime(1970, 1, 1);
                    long currentTime = DateTime.Now.ToUniversalTime().Subtract(dt).Ticks / 10000000;

                    List<string> batchCmds = new List<string>();
                    curPageStrokes[j].updatetime = currentTime;
                    curPageStrokes[j].status = "1";

                    string cmd = bookManager.deleteStrokeCmdString(userBookSno, curPageStrokes[j]);
                    if (!batchCmds.Contains(cmd))
                        batchCmds.Add(cmd);

                    if (batchCmds.Count > 0)
                        bookManager.saveBatchData(batchCmds);

                    break;
                }
            }
        }

        private bool compareStrokeInDB(Stroke thisStroke, StrokesData strokeJson, double currentInkcanvasWidth, double currentInkcanvasHeight)
        {
            double canvasHeightInDB = strokeJson.canvasheight;
            double canvasWidthInDB = strokeJson.canvaswidth;

            double widthScaleRatio = canvasWidthInDB / currentInkcanvasWidth;
            double heightScaleRatio = canvasHeightInDB / currentInkcanvasHeight;

            int pointCount = thisStroke.StylusPoints.Count;
            int samePointCount = 0;

            string strokeJsonPoints = strokeJson.points.Replace(" ", "");

            for (int j = 0; j < pointCount; j++)
            {
                StylusPoint stylusPoint = thisStroke.StylusPoints[j];

                string pointPair = "{" + (stylusPoint.X * widthScaleRatio).ToString() + "," + (stylusPoint.Y * heightScaleRatio).ToString() + "}";

                if (strokeJsonPoints.Contains(pointPair))
                {
                    samePointCount++;
                }
            }

            double percent = samePointCount / pointCount * 100;

            if (percent == 100)
            {
                return true;
            }
            else
            {
                return false;
            }

        }


        //wayne 加上lock和i
        //避免每一筆畫時間會相同
        //時間相同，雲端記住會有問題
        int i = 0;
        private void saveStrokeToDB(Stroke thisStroke)
        {
            lock (this)
            {
                i++;
                DateTime dt = new DateTime(1970, 1, 1);

                //server上的儲存時間的格式是second, Ticks單一刻度表示千萬分之一秒

                long currentTime = DateTime.Now.ToUniversalTime().Subtract(dt).Ticks / 10000000;


                int pointCount = thisStroke.StylusPoints.Count;
                DrawingAttributes d = thisStroke.DrawingAttributes;

                string colorString = d.Color.ToString();
                colorString = colorString.Remove(1, 2);

                string pointsMsg = "";
                for (int j = 0; j < pointCount; j++)
                {
                    StylusPoint stylusPoint = thisStroke.StylusPoints[j];
                    pointsMsg += "{" + stylusPoint.X.ToString() + ", " + stylusPoint.Y.ToString() + "};";
                }

                pointsMsg = pointsMsg.Substring(0, pointsMsg.LastIndexOf(';'));

                StrokesData sd = new StrokesData();
                sd.objectId = "";
                sd.alpha = (float)(d.IsHighlighter ? 0.5 : 1);
                sd.bookid = bookId;
                sd.canvasheight = (float)penMemoCanvas.ActualHeight;
                sd.canvaswidth = (float)penMemoCanvas.ActualWidth;
                sd.color = colorString;
                sd.createtime = currentTime + i;
                sd.index = curPageIndex;
                sd.points = pointsMsg;
                sd.status = "0";
                sd.synctime = 0;
                sd.updatetime = currentTime + i;
                sd.userid = account;
                //sd.vendor = vendorId;
                sd.width = (float)d.Height;

                bookManager.saveStrokesData(userBookSno, false, sd);
                //bookManager.saveStrokesData(userBookSno, curPageIndex, false, "", currentTime, currentTime, 0
                //    , "0", penMemoCanvas.ActualWidth, penMemoCanvas.ActualHeight, (d.IsHighlighter ? 0.5 : 1), pointsMsg, colorString, d.Height);
            }
        }

        public void strokeChaneEventHandler(DrawingAttributes d)
        {
            penMemoCanvas.DefaultDrawingAttributes = d;
        }

        public void strokeUndoEventHandler()
        {
            if (penMemoCanvas.Strokes.Count > 0)
            {

                tempStrokes.Add(penMemoCanvas.Strokes[penMemoCanvas.Strokes.Count - 1]);
                penMemoCanvas.Strokes.RemoveAt(penMemoCanvas.Strokes.Count - 1);
            }

            //上一步是畫一筆

            //上一步是清一筆

            //上一步是清除全部

        }

        public void strokeRedoEventHandler()
        {
            while (tempStrokes.Count > 0)
            {
                penMemoCanvas.Strokes.Add(tempStrokes[tempStrokes.Count - 1]);
                tempStrokes.RemoveAt(tempStrokes.Count - 1);
            }
        }

        public void strokeEraseEventHandler()
        {
            penMemoCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
        }

        public void strokeLineEventHandler()
        {
            penMemoCanvas.EditingMode = InkCanvasEditingMode.None;
            penMemoCanvas.MouseLeftButtonDown += inkCanvas1_MouseDown;
            //penMemoCanvas.MouseDown += inkCanvas1_MouseDown;
            penMemoCanvas.MouseUp += inkCanvas1_MouseUp;
            penMemoCanvas.MouseMove += inkCanvas1_MouseMove;
            isStrokeLine = true;
        }

        public void strokeCurveEventHandler()
        {
            penMemoCanvas.MouseDown -= inkCanvas1_MouseDown;
            penMemoCanvas.MouseUp -= inkCanvas1_MouseUp;
            penMemoCanvas.MouseMove -= inkCanvas1_MouseMove;
            penMemoCanvas.EditingMode = InkCanvasEditingMode.Ink;
            isStrokeLine = false;
        }

        private void inkCanvas1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (penMemoCanvas.EditingMode == InkCanvasEditingMode.None)
            {
                stylusPC = new StylusPointCollection();

                System.Windows.Point p = e.GetPosition(penMemoCanvas);

                stylusPC.Add(new StylusPoint(p.X, p.Y));

            }
        }

        private void inkCanvas1_MouseMove(object sender, MouseEventArgs e)
        {
            //if (penMemoCanvas.EditingMode == InkCanvasEditingMode.None)
            //    if (stylusPC != null)
            //    {
            //        System.Windows.Point p = e.GetPosition(penMemoCanvas);
            //        if (stylusPC.Count > 1)
            //        {
            //            stylusPC.RemoveAt(stylusPC.Count - 1);
            //        }
            //        stylusPC.Add(new StylusPoint(p.X, p.Y));
            //        strokeLine = new Stroke(stylusPC, penMemoCanvas.DefaultDrawingAttributes.Clone());

            //        penMemoCanvas.Strokes.Add(strokeLine);

            //        //stylusPC = null;
            //        strokeLine = null;
            //    }

        }

        private void inkCanvas1_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (penMemoCanvas.EditingMode == InkCanvasEditingMode.None)
                if (stylusPC != null)
                {
                    System.Windows.Point p = e.GetPosition(penMemoCanvas);

                    stylusPC.Add(new StylusPoint(p.X, p.Y));
                    strokeLine = new Stroke(stylusPC, penMemoCanvas.DefaultDrawingAttributes);

                    penMemoCanvas.Strokes.Add(strokeLine.Clone());

                    saveStrokeToDB(strokeLine.Clone());

                    stylusPC = null;
                    strokeLine = null;
                }
        }

        public void strokDelEventHandler()
        {
            Button b = FindVisualChildByName<Button>(mediaListPanel, "delClickButton");

            if (penMemoCanvas.EditingMode != InkCanvasEditingMode.EraseByStroke)
            {
                penMemoCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                b.Content = langMng.getLangString("stroke");   // "筆劃";
                penMemoCanvas.MouseDown += penMemoCanvas_MouseDown;
            }
            else
            {
                penMemoCanvas.EditingMode = InkCanvasEditingMode.Ink;
                penMemoCanvas.MouseDown -= penMemoCanvas_MouseDown;
                b.Content = langMng.getLangString("delete");   //"刪除";
            }
        }

        public void alterPenmemoAnimation(StrokeToolPanelHorizontal toolPanel, double f, double t)
        {

            DoubleAnimation a = new DoubleAnimation();
            a.From = f;
            a.To = t;
            a.Duration = new Duration(TimeSpan.FromSeconds(0.3));

            toolPanel.BeginAnimation(StrokeToolPanelHorizontal.WidthProperty, a);
        }

        public void showPenToolPanelEventHandler(bool isCanvasShowed)
        {
            if (isCanvasShowed)
            {
                //PopupControlCanvas open
                Canvas.SetZIndex(PopupControlCanvas, 901);
                if (PopupControlCanvas.Visibility.Equals(Visibility.Collapsed))
                {
                    PopupControlCanvas.Visibility = Visibility.Visible;
                }
            }
            else
            {
                //PopupControlCanvas close
                Canvas.SetZIndex(PopupControlCanvas, 899);
                if (PopupControlCanvas.Visibility.Equals(Visibility.Visible))
                {
                    PopupControlCanvas.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void PopupControlCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Canvas.SetZIndex(PopupControlCanvas, 899);
            if (PopupControlCanvas.Visibility.Equals(Visibility.Visible))
            {
                PopupControlCanvas.Visibility = Visibility.Collapsed;
            }

            StrokeToolPanelHorizontal strokeToolPanelHorizontal = (StrokeToolPanelHorizontal)PenMemoToolBar.Children[PenMemoToolBar.Children.Count - 1];
            strokeToolPanelHorizontal.closePopup();
        }

        void penMemoCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StrokeCollection strokeCol = penMemoCanvas.GetSelectedStrokes();
            if (strokeCol.Count > 0)
            {
                penMemoCanvas.Strokes.Remove(strokeCol);
            }

        }

        public void strokeDelAllEventHandler()
        {
            for (int i = 0; i < penMemoCanvas.Strokes.Count; i++)
            {
                tempStrokes.Add(penMemoCanvas.Strokes[i]);
            }
            //tempStrokes = penMemoCanvas.Strokes;
            //penMemoCanvas.Strokes.CopyTo(tempStrokes, 0);
            penMemoCanvas.Strokes.Clear();

            List<string> delCmds = new List<string>();
            List<StrokesData> curPageStrokes = bookManager.getCurPageStrokes(userBookSno, curPageIndex);
            for (int i = 0; i < curPageStrokes.Count; i++)
            {
                delCmds.Add(bookManager.deleteStrokeCmdString(userBookSno, curPageStrokes[i]));
            }

            bookManager.saveBatchData(delCmds);

        }


        private System.Windows.Media.Color ConvertHexStringToColour(string hexString)
        {
            byte a = 0;
            byte r = 0;
            byte g = 0;
            byte b = 0;
            if (hexString.Length == 7)
            {
                hexString = hexString.Insert(1, "FF");
            }
            if (hexString.StartsWith("#"))
            {
                hexString = hexString.Substring(1, 8);
            }
            a = Convert.ToByte(Int32.Parse(hexString.Substring(0, 2),
                System.Globalization.NumberStyles.AllowHexSpecifier));
            r = Convert.ToByte(Int32.Parse(hexString.Substring(2, 2),
                System.Globalization.NumberStyles.AllowHexSpecifier));
            g = Convert.ToByte(Int32.Parse(hexString.Substring(4, 2),
                System.Globalization.NumberStyles.AllowHexSpecifier));
            b = Convert.ToByte(Int32.Parse(hexString.Substring(6, 2), System.Globalization.NumberStyles.AllowHexSpecifier));
            return System.Windows.Media.Color.FromArgb(a, r, g, b);
        }

        void penMemoCanvas_StrokeErased(object sender, RoutedEventArgs e)
        {
            preparePenMemoAndSend(false);
        }

        //wayne 這裡是畫布
        void penMemoCanvasStrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            //每新增一筆資料就直接存DB
            Stroke thisStroke = e.Stroke;
            if (thisStroke == null)
            {
                return;
            }

            if (!(isSyncing && !isSyncOwner))
            {
                saveStrokeToDB(thisStroke);

                preparePenMemoAndSend();
            }
        }



        #endregion

        public List<Stroke> tempStrokes { get; set; }
        private StylusPointCollection stylusPC;
        private Stroke strokeLine;

        private void MediaTableCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Canvas thisCanvas = (Canvas)sender;
            thisCanvas.Visibility = Visibility.Collapsed;
            string childNameInReader = "";
            switch (openedby)
            {
                case MediaCanvasOpenedBy.SearchButton:
                    childNameInReader = "SearchButton";
                    break;
                case MediaCanvasOpenedBy.MediaButton:
                    childNameInReader = "MediaListButton";
                    break;
                case MediaCanvasOpenedBy.CategoryButton:
                    childNameInReader = "TocButton";
                    break;
                case MediaCanvasOpenedBy.NoteButton:
                    childNameInReader = "NoteButton";
                    break;
                case MediaCanvasOpenedBy.ShareButton:
                    childNameInReader = "ShareButton";
                    break;
                case MediaCanvasOpenedBy.SettingButton:
                    childNameInReader = "SettingsButton";
                    break;
                default:
                    break;
            }
            if (!childNameInReader.Equals("") && !childNameInReader.Equals("NoteButton"))
            {
                NoteButton.IsChecked = false;
                TriggerBookMark_NoteButtonOrElse(NoteButton);
            }
            else if (childNameInReader.Equals("NoteButton"))
            {
                TextBox tb = FindVisualChildByName<TextBox>(MediaTableCanvas, "notePanel");

                int targetPageIndex = curPageIndex;

                bool isSuccess = setNotesInMem(tb.Text, targetPageIndex);

                //bool isUpdate = bookNoteDictionary[targetPageIndex].notes == "" ? false : true;



                //bookNoteDictionary[targetPageIndex].notes = tb.Text;
                //DateTime dt = new DateTime(1970, 1, 1);

                ////server上的儲存時間的格式是second, Ticks單一刻度表示千萬分之一秒

                //long currentTime = DateTime.Now.ToUniversalTime().Subtract(dt).Ticks / 10000000;
                //bookManager.saveNoteData(userBookSno, targetPageIndex, tb.Text,isUpdate, currentTime, currentTime);

                //bookManager.saveNoteData(userBookSno, targetPageIndex.ToString(), tb.Text);
                if (tb.Text.Equals(""))
                {
                    NoteButton.IsChecked = false;
                    TriggerBookMark_NoteButtonOrElse(NoteButton);
                }
                else
                {
                    NoteButton.IsChecked = true;
                    TriggerBookMark_NoteButtonOrElse(NoteButton);
                }
                if (PaperLess_Emeeting.Properties.Settings.Default.IsFlatUIReader == true)
                    NoteButtonInLBIsClicked = false;
                ShowNote();
                ShowAddition();

            }
        }

        private void SearchButton_Checked(object sender, RoutedEventArgs e)
        {
            doUpperRadioButtonClicked(MediaCanvasOpenedBy.SearchButton, sender);
        }

        private void NoteButton_Checked(object sender, RoutedEventArgs e)
        {
            if (PaperLess_Emeeting.Properties.Settings.Default.IsFlatUIReader == true)
            {
                //if (CheckIsNowClick(MemoSP) == true)
                //{
                //    MemoSP.Background = ColorTool.HexColorToBrush("#000000");
                //    NoteButton.IsChecked = false;
                //}
                //else
                //{
                //    MemoSP.Background = ColorTool.HexColorToBrush("#F66F00");
                //    NoteButton.IsChecked = true;
                //}
                Canvas.SetTop(mediaListBorder, Double.NaN);
                Canvas.SetRight(mediaListBorder, Double.NaN);
                Canvas.SetBottom(mediaListBorder, 64);
                Canvas.SetLeft(mediaListBorder, PenSP.Width + 64);
                //thumnailCanvas.Visibility = Visibility.Collapsed;
                //ViewThumbSP.Background = ColorTool.HexColorToBrush("#000000");

            }

            doUpperRadioButtonClicked(MediaCanvasOpenedBy.NoteButton, sender);
            if (MediaTableCanvas.Visibility == Visibility.Visible)
                MemoSP.Background = ColorTool.HexColorToBrush("#F66F00");
        }

        private void BackToBookShelfButton_Click(object sender, RoutedEventArgs e)
        {
            if (isSyncing == true && isSyncOwner == false)
                return;
            this.Close();
        }

        private void BookMarkButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = (RadioButton)sender;

            bool isBookMarkClicked = false;
            int index = 0;

            if (bookMarkDictionary.ContainsKey(curPageIndex))
            {
                //原來DB中有資料
                if (bookMarkDictionary[curPageIndex].status == "0")
                {
                    isBookMarkClicked = true;
                    index = 1;
                }
            }

            setBookMark(curPageIndex, !isBookMarkClicked);

            rb.IsChecked = !isBookMarkClicked;
            BookMarkButton.IsChecked = !isBookMarkClicked;
            btnBookMark.IsChecked = !isBookMarkClicked;

            TriggerBookMark_NoteButtonOrElse(rb);

            if (rb.IsChecked == true)
            {
                BookMarkSP.Background = ColorTool.HexColorToBrush("#F66F00");
            }
            else
            {
                BookMarkSP.Background = ColorTool.HexColorToBrush("#000000");
            }
            if (CheckIsNowClick(BookMarkButtonInListBoxSP) == true)
            {
                ShowBookMark();
                ShowBookMark();
            }
            sendBroadCast("{\"bookmark\":" + index + ",\"pageIndex\":" + curPageIndex.ToString() + ",\"cmd\":\"R.SB\"}");

        }

        private void TriggerBookMark_NoteButtonOrElse(RadioButton rb)
        {
            Brush Orange = ColorTool.HexColorToBrush("#F66F00");
            Brush Black = ColorTool.HexColorToBrush("#000000");
            switch (rb.Name)
            {
                case "btnBookMark":
                case "BookMarkButton":

                    if (rb.IsChecked == true)
                    {
                        BookMarkSP.Background = Orange;
                        statusBMK.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        BookMarkSP.Background = Black;
                        statusBMK.Visibility = Visibility.Collapsed;
                    }
                    break;
                case "NoteButton":
                case "btnNoteButton":
                    if (rb.IsChecked == true)
                    {
                        MemoSP.Background = Orange;
                        statusMemo.Visibility = Visibility.Visible;

                    }
                    else
                    {
                        MemoSP.Background = Black;
                        statusMemo.Visibility = Visibility.Collapsed;
                    }
                    break;
            }

        }





        private void setBookMark(int pageIndex, bool hasBookMark)
        {
            DateTime dt = new DateTime(1970, 1, 1);

            //server上的儲存時間的格式是second, Ticks單一刻度表示千萬分之一秒

            long currentTime = DateTime.Now.ToUniversalTime().Subtract(dt).Ticks / 10000000;
            BookMarkData bm = null;
            //單頁
            if (bookMarkDictionary.ContainsKey(pageIndex))
            {
                bm = bookMarkDictionary[pageIndex];
                bm.updatetime = currentTime;
                //原來DB中有資料
                if (bm.status == "0")
                {
                    bm.status = "1";
                }
                else
                {
                    bm.status = "0";
                }
            }
            else
            {
                //新增一筆資料
                bm = new BookMarkData();
                bm.createtime = currentTime;
                bm.updatetime = currentTime;
                bm.index = pageIndex;
                bm.status = "0";
                bm.synctime = 0;
                bm.objectId = "";
                bookMarkDictionary.Add(pageIndex, bm);
            }

            bookManager.saveBookMarkData(userBookSno, hasBookMark, bm);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //penMemoCanvas.Height = web_view.ContentsHeight;
            //double height = web_view.ContentsHeight - 50;

            //penMemoCanvas.Width = height * thumbnailRatio;
        }


        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            if (isSyncing == true && isSyncOwner == false)
                return;
            if (e.Delta < 0)
            {
                MovePage(MovePageType.下一頁);
            }
            else
            {
                MovePage(MovePageType.上一頁);
            }
        }

        private void MovePage(MovePageType mpt)
        {
            try
            {
                int page = 0;
                int animations = 0;
                //MediaTableCanvas.Visibility = Visibility.Collapsed;
                if (MediaTableCanvas.Visibility == Visibility.Visible)
                {
                    noteButton_Click();
                }

                switch (mpt)
                {
                    case MovePageType.下一頁:
                        // 不是最後一頁才要清除
                        if (curPageIndex + 1 != totalPage)
                        {
                            penMemoCanvas.Strokes.Clear();
                        }
                        web_view.ExecuteScript("goNext();");

                        break;

                    case MovePageType.上一頁:
                        // Wayne add
                        // 不是第一頁才要清除
                        if (curPageIndex != 0)
                        {
                            penMemoCanvas.Strokes.Clear();
                        }
                        web_view.ExecuteScript("goPrevious();");
                        break;
                    case MovePageType.第一頁:
                        // Wayne add
                        // 不是第一頁才要清除
                        if (curPageIndex != 0)
                        {
                            penMemoCanvas.Strokes.Clear();
                        }
                        page = 1;
                        animations = 0;
                        web_view.ExecuteScript("goToStep(" + page + ", " + animations + ")");
                        break;
                    case MovePageType.最後一頁:
                        // 不是最後一頁才要清除
                        if (curPageIndex + 1 != totalPage)
                        {
                            penMemoCanvas.Strokes.Clear();
                        }
                        page = totalPage;
                        animations = 0;
                        web_view.ExecuteScript("goToStep(" + page + ", " + animations + ")");
                        break;

                }


                // Wayne Add
                //上方的總頁數及目前頁數顯示
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(700);
                    this.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        txtPage.Text = string.Format("{0} / {1}", (curPageIndex + 1).ToString(), totalPage.ToString());

                        //string command = "{\"cmd\":\"R.PP\", \"page\":" + (curPageIndex + 1).ToString() + ", \"animations\":" + "0" + "}";
                        //sendBroadCast(command);
                    }));
                    //Thread.Sleep(100);
                    ShowAddition(false);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ExecuteScript_Exception@R.PP: " + ex.Message);
            }
        }

        private void MoveBoxPage()
        {

            return;
            if (this.Dispatcher.CheckAccess() == false)
            {
                this.Dispatcher.BeginInvoke(new Action(MoveBoxPage));
            }
            else
            {
                try
                {

                    List<ThumbnailImageAndPage> list = (List<ThumbnailImageAndPage>)thumbNailListBox.ItemsSource;
                    int index = 0;
                    foreach (ThumbnailImageAndPage item in list)
                    {

                        if (item.pageIndex.Equals((curPageIndex + 1).ToString()))
                        {
                            thumbNailListBox.SelectedIndex = index;
                            break;
                        }
                        index++;

                    }
                    thumbNailListBox.Focus();
                }
                catch (Exception ex)
                {
                    LogTool.Debug(ex);
                }
            }
        }

        private void ShowFilterCount()
        {

            if (PaperLess_Emeeting.Properties.Settings.Default.IsFlatUIReader == false)
                return;
            if (this.Dispatcher.CheckAccess() == false)
            {
                this.Dispatcher.BeginInvoke(new Action(ShowFilterCount));
            }
            else
            {
                int i = 0;
                int total = 0;
                foreach (ThumbnailImageAndPage item in thumbNailListBox.Items)
                {
                    int PageIndex = int.Parse(item.pageIndex);
                    ListBoxItem listBoxItem = (ListBoxItem)(thumbNailListBox.ItemContainerGenerator.ContainerFromIndex(i));
                    if (listBoxItem == null)
                    {
                        total = thumbNailListBox.Items.Count;
                        break;
                    }
                    if (listBoxItem.Visibility == Visibility.Visible)
                    {
                        total++;
                    }
                    i++;
                }
                txtFilterCount.Text = string.Format("有 {0} 筆相關資料", total);

            }
        }

        private void leftPageButton_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                // Wayne add
                // 不是第一頁和最後一頁才要清除
                if (curPageIndex != 0)
                {
                    penMemoCanvas.Strokes.Clear();
                }
                web_view.ExecuteScript("goPrevious();");

                // Wayne Add
                //上方的總頁數及目前頁數顯示
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(700);
                    this.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        txtPage.Text = string.Format("{0} / {1}", (curPageIndex + 1).ToString(), totalPage.ToString());
                    }));
                    //Thread.Sleep(100);
                    ShowAddition(false);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ExecuteScript_Exception@R.PP: " + ex.Message);
            }
        }

        private void rightPageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 不是最後一頁才要清除
                if (curPageIndex + 1 != totalPage)
                {
                    penMemoCanvas.Strokes.Clear();
                }
                web_view.ExecuteScript("goNext();");

                // Wayne Add
                //上方的總頁數及目前頁數顯示
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(700);
                    this.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        txtPage.Text = string.Format("{0} / {1}", (curPageIndex + 1).ToString(), totalPage.ToString());
                    }));
                    //Thread.Sleep(100);
                    ShowAddition(false);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ExecuteScript_Exception@R.PP: " + ex.Message);
            }

        }

        private void testButton_Click(object sender, RoutedEventArgs e)
        {
            //string bookId = msgStrings["bookId"].ToString();
            //string path = msgStrings["path"].ToString();
            //string action = msgStrings["action"].ToString();
            //if (action.Equals("start"))
            //{
            //    var temp = basePath;
            //    string filePath = basePath + "//" + path.Replace(path, bookId);
            //    mp = new MoviePlayer(filePath, true);
            //    mp.ShowDialog();
            //}
            //else if (action.Equals("stop"))
            //{
            //    if (mp != null)
            //    {
            //        mp.Close();
            //        mp = null;
            //    }
            //}
            string relativePath = "data/pres/vs9s8.mp4";
            string videoPath = basePath + relativePath;
            prepareVideoCmd(relativePath, videoPath);
        }

        private void prepareVideoCmd(string relativePath, string videoPath)
        {
            sendBroadCast("{\"cmd\" : \"R.PP.V\", \"bookId\" : \"" + this.bookId + "\", \"path\" : \"test2/" + relativePath + "\", \"action\" : \"start\"}");

            mp = new MoviePlayer(videoPath, true, true);
            mp.Closing += mp_Closing;
            mp.ShowDialog();
        }

        void mp_Closing(object sender, CancelEventArgs e)
        {
            MoviePlayer mp = (MoviePlayer)sender;
            string relativePath = mp.filePath.Replace(basePath, "");
            //string relativePath = "/data/pres/vs9s8.mp4";
            sendBroadCast("{\"cmd\" : \"R.PP.V\", \"bookId\" : \"" + this.bookId + "\", \"path\" : \"" + this.bookId + "/" + relativePath + "\", \"action\" : \"stop\"}");
        }






        private List<ThumbnailImageAndPage> singleThumbnailImageAndPageList;
        private void InitSmallImage()
        {
            try
            {
                Task.Factory.StartNew(() =>
                {
                    singleThumbnailImageAndPageList = new List<ThumbnailImageAndPage>();

                    string curAppPath = this.pptPath;
                    curAppPath = System.IO.Path.GetDirectoryName(curAppPath);
                    // wayne point
                    string thumbnailPathFolder = curAppPath + "\\data\\Thumbnails";
                    if (Directory.Exists(thumbnailPathFolder) == true)
                    {
                        DirectoryInfo di = new DirectoryInfo(thumbnailPathFolder);
                        totalPage = di.GetFiles().Count();

                        int _pageIndex = 0;
                        string _rightImagePath = "";
                        string _leftImagePath = "";
                        foreach (FileInfo fi in di.GetFiles().OrderBy(f => int.Parse(f.Name.ToLower().Replace("slide", "").Replace(".png", ""))))
                        {
                            _pageIndex++;
                            // 下面是列出縮圖
                            //Image thumbNailImageSingle = new Image();
                            //BitmapImage bi = new BitmapImage(new Uri(fi.FullName));
                            _leftImagePath = fi.FullName;
                            _rightImagePath = "";
                            ThumbnailImageAndPage smallImage = new ThumbnailImageAndPage(_pageIndex.ToString(), _rightImagePath, _leftImagePath, true);
                            singleThumbnailImageAndPageList.Add(smallImage);
                        }

                        this.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            if (PaperLess_Emeeting.Properties.Settings.Default.IsFlatUIReader == true)
                            {
                                thumbNailListBox.ItemsSource = singleThumbnailImageAndPageList;
                            }
                            else
                            {


                                Canvas.SetZIndex(thumnailCanvas, -10);
                                thumnailCanvas.Visibility = Visibility.Visible;
                                thumbNailListBox.ItemsSource = singleThumbnailImageAndPageList;
                                thumnailCanvas.Visibility = Visibility.Collapsed;
                                Canvas.SetZIndex(thumnailCanvas, 200);
                            }
                        }));
                    }
                });

            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }

        }

        #region RadioButton in ThumbNailListBox

        private bool BookMarkInLBIsClicked = false;

        private void BookMarkButtonInListBox_Checked(object sender, RoutedEventArgs e)
        {
            if (CheckIsNowClick(BookMarkButtonInListBoxSP) == true)
                return;
            ShowBookMark();
        }

        private void ShowBookMark()
        {
            //切換資料結構
            if (BookMarkInLBIsClicked)
            {
                BookMarkButtonInListBox.IsChecked = false;
                BookMarkInLBIsClicked = false;

                AllImageButtonInListBox.IsChecked = true;
                Task.Factory.StartNew(() =>
                {
                    this.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        thumbNailListBox.ItemsSource = singleThumbnailImageAndPageList;
                    }));
                });
                //thumbNailListBox.ItemsSource = singleThumbnailImageAndPageList;
            }
            else
            {
                if (NoteButtonInLBIsClicked)
                {
                    NoteButtonInListBox.IsChecked = false;
                    NoteButtonInLBIsClicked = false;
                }


                if (PaperLess_Emeeting.Properties.Settings.Default.IsFlatUIReader == false)
                {

                    List<ThumbnailImageAndPage> bookMarkedPage = new List<ThumbnailImageAndPage>();
                    foreach (KeyValuePair<int, BookMarkData> bookMarkPair in bookMarkDictionary)
                    {
                        if (bookMarkPair.Value.status == "0")
                        {
                            bookMarkedPage.Add(singleThumbnailImageAndPageList[bookMarkPair.Key]);
                        }
                    }

                    thumbNailListBox.ItemsSource = bookMarkedPage.OrderBy(x => int.Parse(x.pageIndex)).ToList();
                }
                else
                {
                    int i = 0;
                    //var listK = bookMarkDictionary.Select(x=>x.Key).ToList();
                    //var listV = bookMarkDictionary.Select(x => x.Value.status).ToList();
                    foreach (ThumbnailImageAndPage item in thumbNailListBox.Items)
                    {
                        ListBoxItem listBoxItem = (ListBoxItem)(thumbNailListBox.ItemContainerGenerator.ContainerFromIndex(i));
                        if (bookMarkDictionary.ContainsKey(i) && bookMarkDictionary[i].status.Equals("0"))
                        {
                            listBoxItem.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            listBoxItem.Visibility = Visibility.Collapsed;
                        }
                        i++;
                    }
                }
                BookMarkInLBIsClicked = true;
                BookMarkButtonInListBox.IsChecked = true;
            }

            NoteButtonInListBoxSP.Background = ColorTool.HexColorToBrush("#000000");
            AllImageButtonInListBoxSP.Background = ColorTool.HexColorToBrush("#000000");
            BookMarkButtonInListBoxSP.Background = ColorTool.HexColorToBrush("#F66F00");

            ShowAddition();
            txtKeyword.Text = "";
            txtKeyword.Focus();
        }

        //Wayne mark 20150204
        //下方有作筆記的縮圖
        private void NoteButtonInListBox_Checked(object sender, RoutedEventArgs e)
        {
            if (CheckIsNowClick(NoteButtonInListBoxSP) == true)
                return;

            ShowNote();
        }

        private bool NoteButtonInLBIsClicked = false;




        private void ShowNote()
        {
            //切換資料結構
            if (NoteButtonInLBIsClicked)
            {
                NoteButtonInListBox.IsChecked = false;
                NoteButtonInLBIsClicked = false;
                AllImageButtonInListBox.IsChecked = true;
                Task.Factory.StartNew(() =>
                {
                    this.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        thumbNailListBox.ItemsSource = singleThumbnailImageAndPageList;
                    }));
                });
                //thumbNailListBox.ItemsSource = singleThumbnailImageAndPageList;
            }
            else
            {
                if (BookMarkInLBIsClicked)
                {
                    BookMarkButtonInListBox.IsChecked = false;
                    BookMarkInLBIsClicked = false;
                }

                if (PaperLess_Emeeting.Properties.Settings.Default.IsFlatUIReader == false)
                {

                    List<ThumbnailImageAndPage> notePages = new List<ThumbnailImageAndPage>();
                    foreach (KeyValuePair<int, NoteData> bookNotePair in bookNoteDictionary)
                    {
                        if (!bookNotePair.Value.status.Equals("1"))
                        {
                            notePages.Add(singleThumbnailImageAndPageList[bookNotePair.Key]);
                        }
                    }

                    //Wayne mark 20150204 移除
                    //foreach (KeyValuePair<int, List<StrokesData>> bookStrokePair in bookStrokesDictionary)
                    //{
                    //    List<StrokesData> strokes = bookStrokePair.Value;

                    //    for (int i = 0; i < strokes.Count; i++)
                    //    {
                    //        //只要有一筆紀錄顯示則有, 否則為無
                    //        if (strokes[i].status == "0")
                    //        {
                    //            notePages.Add(singleThumbnailImageAndPageList[bookStrokePair.Key]);
                    //            break;
                    //        }
                    //    }

                    //    //if (File.Exists(bookPath + "/hyweb/strokes/" + hejMetadata.LImgList[bookNotePair.Key].pageId + ".isf"))
                    //    //{
                    //    //    notePages.Add(singleThumbnailImageAndPageList[bookNotePair.Key]);
                    //    //    //list+1
                    //    //}//加入螢光筆
                    //}
                    thumbNailListBox.ItemsSource = notePages.OrderBy(x => int.Parse(x.pageIndex)).ToList();

                }
                else
                {
                    int i = 0;
                    //var listK = bookMarkDictionary.Select(x=>x.Key).ToList();
                    //var listV = bookMarkDictionary.Select(x => x.Value.status).ToList();
                    foreach (ThumbnailImageAndPage item in thumbNailListBox.Items)
                    {
                        ListBoxItem listBoxItem = (ListBoxItem)(thumbNailListBox.ItemContainerGenerator.ContainerFromIndex(i));
                        if (bookNoteDictionary.ContainsKey(i) && bookNoteDictionary[i].status.Equals("0"))
                        {
                            listBoxItem.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            listBoxItem.Visibility = Visibility.Collapsed;
                        }
                        i++;
                    }
                }

                NoteButtonInLBIsClicked = true;
                NoteButtonInListBox.IsChecked = true;

            }

            AllImageButtonInListBoxSP.Background = ColorTool.HexColorToBrush("#000000");
            BookMarkButtonInListBoxSP.Background = ColorTool.HexColorToBrush("#000000");
            NoteButtonInListBoxSP.Background = ColorTool.HexColorToBrush("#F66F00");

            ShowAddition();
            txtKeyword.Text = "";
            txtKeyword.Focus();

        }

        private void AllImageButtonInListBox_Checked(object sender, RoutedEventArgs e)
        {

            if (CheckIsNowClick(AllImageButtonInListBoxSP) == true)
                return;

            ShowAll();
        }

        private bool CheckIsNowClick(StackPanel SP)
        {
            Brush backgroundColor = SP.Background;

            if (backgroundColor is SolidColorBrush)
            {
                string colorValue = ((SolidColorBrush)backgroundColor).Color.ToString();
                if (colorValue.Equals("#FFF66F00"))
                    return true;
            }

            return false;
        }

        private void AllImageButtonInListBoxSP_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (CheckIsNowClick(AllImageButtonInListBoxSP) == true)
                return;

            AllImageButtonInListBox.IsChecked = !AllImageButtonInListBox.IsChecked;
            ShowAll();
        }

        private void NoteButtonInListBoxSP_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (CheckIsNowClick(NoteButtonInListBoxSP) == true)
                return;

            NoteButtonInListBox.IsChecked = !NoteButtonInListBox.IsChecked;
            ShowNote();
        }



        private void BookMarkButtonInListBoxSP_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (CheckIsNowClick(BookMarkButtonInListBoxSP) == true)
                return;
            NoteButtonInListBox.IsChecked = !NoteButtonInListBox.IsChecked;
            ShowBookMark();
        }


        private void ShowAll()
        {
            BookMarkInLBIsClicked = false;
            NoteButtonInLBIsClicked = false;
            AllImageButtonInListBox.IsChecked = true;
            if (PaperLess_Emeeting.Properties.Settings.Default.IsFlatUIReader == false)
            {
                thumbNailListBox.ItemsSource = singleThumbnailImageAndPageList.OrderBy(x => int.Parse(x.pageIndex)).ToList();
            }
            else
            {
                int i = 0;
                foreach (ThumbnailImageAndPage item in thumbNailListBox.Items)
                {
                    ListBoxItem listBoxItem = (ListBoxItem)(thumbNailListBox.ItemContainerGenerator.ContainerFromIndex(i));
                    if (listBoxItem != null)
                        listBoxItem.Visibility = Visibility.Visible;
                    i++;
                }
            }

            NoteButtonInListBoxSP.Background = ColorTool.HexColorToBrush("#000000");
            BookMarkButtonInListBoxSP.Background = ColorTool.HexColorToBrush("#000000");
            AllImageButtonInListBoxSP.Background = ColorTool.HexColorToBrush("#F66F00");

            ShowAddition();
            txtKeyword.Text = "";
            txtKeyword.Focus();
        }

        private void ShowAddition(bool ShowFilter = true)
        {
            MoveBoxPage();
            if (ShowFilter == true)
                ShowFilterCount();
            ShowImageCenter();
        }

        #endregion


        #region 縮圖列及縮圖總覽

        private int thumbNailListBoxStatus = 0;
        // 0->關閉, 1->縮圖列, 2->縮圖總覽

        private bool thumbNailListBoxOpenedFullScreen = false;

        private double thumbnailListBoxHeight = 150;

        private void ChangeThumbNailListBoxRelativeStatus()
        {
            //Canvas MediaTableCanvas = GetMediaTableCanvasInReader();
            if (MediaTableCanvas.Visibility.Equals(Visibility.Visible))
            {
                TextBox tb = FindVisualChildByName<TextBox>(mediaListPanel, "notePanel");
                if (tb != null && tb.Text.Equals("") == true)
                {
                    MemoSP.Background = ColorTool.HexColorToBrush("#000000");
                }
                MediaTableCanvas.Visibility = Visibility.Collapsed;
            }
            //RadioButton ShowAllImageButton = FindVisualChildByName<RadioButton>(FR, "ShowAllImageButton");
            //ScrollViewer sv = FindVisualChildByName<ScrollViewer>(thumbNailListBox, "SVInLV");
            //WrapPanel wrapPanel = FindVisualChildByName<WrapPanel>(FR, "wrapPanel");

            //RadioButton ShowAllImageButton = FindVisualChildByName<RadioButton>(FR, "ShowAllImageButton");
            ScrollViewer sv = FindVisualChildByName<ScrollViewer>(thumbNailListBox, "SVInLV");
            //WrapPanel wrapPanel = FindVisualChildByName<WrapPanel>(FR, "wrapPanel");

            BookMarkInLBIsClicked = false;
            NoteButtonInLBIsClicked = false;
            AllImageButtonInListBox.IsChecked = true;

            if (PaperLess_Emeeting.Properties.Settings.Default.IsFlatUIReader == true)
            {
                Task.Factory.StartNew(() =>
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        thumbNailListBox.ItemsSource = singleThumbnailImageAndPageList;
                    }));
                });
            }

            switch (thumbNailListBoxStatus)
            {
                case 0:
                    thumbNailListBoxOpenedFullScreen = false;
                    thumnailCanvas.Visibility = Visibility.Hidden;
                    ShowListBoxButton.Visibility = Visibility.Visible;

                    //ShowAllImageButton.IsChecked = false;

                    AllImageButtonInListBox.IsChecked = true;
                    //thumbNailListBox.ItemsSource = singleThumbnailImageAndPageList;

                    //if (!downloadProgBar.Visibility.Equals(Visibility.Collapsed))
                    //{
                    //    downloadProgBar.Margin = new Thickness(0, 0, 0, 0);
                    //}

                    //LockButton.Margin = new Thickness(0, 0, 15, 15);

                    break;
                case 1:
                    thumbNailListBoxOpenedFullScreen = false;

                    Binding convertWidthBinding = new Binding();
                    convertWidthBinding.Source = FR;
                    convertWidthBinding.Path = new PropertyPath("ActualWidth");
                    convertWidthBinding.Converter = new thumbNailListBoxWidthHeightConverter();
                    convertWidthBinding.ConverterParameter = 30;
                    if (PaperLess_Emeeting.Properties.Settings.Default.IsFlatUIReader == true)
                    {
                        convertWidthBinding.ConverterParameter = 105;
                    }
                    thumbNailListBox.SetBinding(ListBox.WidthProperty, convertWidthBinding);

                    thumbNailListBox.Height = thumbnailListBoxHeight;
                    thumnailCanvas.Height = thumbnailListBoxHeight;

                    try
                    {
                        //Wayne Add
                        if (sv != null)
                        {
                            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogTool.Debug(ex);
                    }
                    HideListBoxButton.ToolTip = langMng.getLangString("hideThumbnails"); //"隱藏縮圖列";

                    thumbNailCanvasStackPanel.Orientation = Orientation.Horizontal;
                    RadioButtonStackPanel.Orientation = Orientation.Vertical;
                    thumbNailCanvasGrid.HorizontalAlignment = HorizontalAlignment.Center;

                    thumnailCanvas.Visibility = Visibility.Visible;

                    //ShowAllImageButton.IsChecked = false;

                    ShowListBoxButton.Visibility = Visibility.Hidden;

                    //if (!downloadProgBar.Visibility.Equals(Visibility.Collapsed))
                    //{
                    //    downloadProgBar.Margin = new Thickness(0, 0, 0, thumbnailListBoxHeight);
                    //}

                    //LockButton.Margin = new Thickness(0, 0, 15, 15 + thumbnailListBoxHeight);

                    break;
                case 2:
                    //thumbNailListBoxOpenedFullScreen = true;

                    //Binding heightBinding = new Binding();
                    //heightBinding.Source = FR;
                    //heightBinding.Path = new PropertyPath("ActualHeight");
                    //thumnailCanvas.SetBinding(Canvas.HeightProperty, heightBinding);

                    //Binding widthBinding = new Binding();
                    //widthBinding.Source = FR;
                    //widthBinding.Path = new PropertyPath("ActualWidth");

                    //Binding convertBinding = new Binding();
                    //convertBinding.Source = FR;
                    //convertBinding.Path = new PropertyPath("ActualHeight");
                    //convertBinding.Converter = new thumbNailListBoxWidthHeightConverter();
                    //convertBinding.ConverterParameter = 30;

                    //thumbNailListBox.SetBinding(ListBox.HeightProperty, convertBinding);
                    //thumbNailListBox.SetBinding(ListBox.WidthProperty, widthBinding);

                    //sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                    //sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;

                    //thumbNailCanvasStackPanel.Orientation = Orientation.Vertical;
                    //RadioButtonStackPanel.Orientation = Orientation.Horizontal;
                    //thumbNailCanvasGrid.HorizontalAlignment = HorizontalAlignment.Right;

                    //thumnailCanvas.Visibility = Visibility.Visible;

                    //ShowAllImageButton.IsChecked = true;

                    //ShowListBoxButton.Visibility = Visibility.Hidden;

                    //HideListBoxButton.ToolTip = langMng.getLangString("closeThumbnail"); //"關閉縮圖總覽";

                    //if (!downloadProgBar.Visibility.Equals(Visibility.Collapsed))
                    //{
                    //    downloadProgBar.Margin = new Thickness(0, 0, 0, 0);
                    //}

                    //LockButton.Margin = new Thickness(0, 0, 15, 15);

                    break;
            }
        }


        private void ShowListBoxButton_Click(object sender, RoutedEventArgs e)
        {
            ShowListBoxButton.Visibility = Visibility.Collapsed;
            thumnailCanvas.Visibility = Visibility.Visible;
            thumbNailListBoxStatus = 1;
            //Wayne Mark 圖層的抽取，取消便利貼視窗
            ChangeThumbNailListBoxRelativeStatus();
        }

        private void ShowListBoxButtonNew_Click(object sender, RoutedEventArgs e)
        {

            MouseTool.ShowLoading();
            if (thumnailCanvas.Visibility == Visibility.Visible)
            {
                //Wayne Mark 圖層的抽取，取消便利貼視窗
                ChangeThumbNailListBoxRelativeStatus();
                //thumnailCanvas.Visibility = Visibility.Collapsed;
                MyAnimation(thumnailCanvas, 500, "Height", 150, 0, () => { thumnailCanvas.Visibility = Visibility.Collapsed; });
                ViewThumbSP.Background = ColorTool.HexColorToBrush("#000000");
                //((System.Windows.Controls.Image)(btnViewThumb.Content)).Source = new BitmapImage(new Uri("images/tool-viewThumb@2x.png", UriKind.Relative));
            }
            else
            {
                //thumnailCanvas.Visibility = Visibility.Visible;
                MyAnimation(thumnailCanvas, 500, "Height", 0, 150, () => { thumnailCanvas.Visibility = Visibility.Visible; });
                ViewThumbSP.Background = ColorTool.HexColorToBrush("#F66F00");

                ShowAll();
                thumbNailListBoxStatus = 1;
                //Wayne Mark 圖層的抽取，取消便利貼視窗
                ChangeThumbNailListBoxRelativeStatus();


                //((System.Windows.Controls.Image)(btnViewThumb.Content)).Source= new BitmapImage(new Uri("images/tool-viewThumb-active@2x.png", UriKind.Relative));
                txtKeyword.Select(txtKeyword.Text.Length, 0);
                txtKeyword.Focus();



            }



            if (btnPenFuncSP.Height > 0)
            {
                btnPenColor.Background = ColorTool.HexColorToBrush("#000000");
                MyAnimation(btnPenFuncSP, 300, "Height", btnPenFuncSP.ActualHeight, 0);
            }
            if (btnFuncSP.Height > 0)
            {
                btnBold.Background = ColorTool.HexColorToBrush("#000000");
                MyAnimation(btnFuncSP, 300, "Height", btnFuncSP.ActualHeight, 0);
            }

            MouseTool.ShowArrow();
        }

        private void btnThickness_Click(object sender, RoutedEventArgs e)
        {
            RadioButton btnNext = (RadioButton)sender;
            System.Windows.Controls.Image imgNext = (System.Windows.Controls.Image)(btnNext).Content;
            System.Windows.Controls.Image imgNow = (System.Windows.Controls.Image)btnBold.Content;

            MyAnimation(btnFuncSP, 300, "Height", btnFuncSP.ActualHeight, 0);
            btnBold.Background = ColorTool.HexColorToBrush("#000000");
            imgNow.Source = imgNext.Source;
            btnBold.Tag = btnNext.Tag;

            ChangeMainPenColor();
        }

        private void ChangeMainPenColor()
        {
            int PenColorIndex = 1;
            int.TryParse(btnPenColor.Tag.ToString(), out PenColorIndex);
            int PenBoldIndex = 1;
            int.TryParse(btnBold.Tag.ToString(), out PenBoldIndex);

            PenColorType PTC = (PenColorType)Enum.Parse(typeof(PenColorType), (PenColorIndex + PenBoldIndex).ToString());
            ((System.Windows.Controls.Image)btnPen.Content).Source = PenColorTool.GetButtonImage(PTC);

            btnPen.Tag = (int)PTC;
            DrawingAttributes attr = new DrawingAttributes();

            switch (PenBoldIndex)
            {
                case 100:
                    attr.Width = 4;
                    attr.Height = 4;
                    break;
                case 200:
                    attr.Width = 8;
                    attr.Height = 8;
                    break;
                case 300:
                    attr.Width = 16;
                    attr.Height = 16;
                    break;
                default:
                    break;

            }

            int ColorRemainder = PenColorIndex % 10;
            int HightLight = (PenColorIndex + 1) % 2;
            switch (ColorRemainder)
            {
                case 1:
                case 2:
                    attr.Color = Colors.Red;
                    break;
                case 3:
                case 4:
                    attr.Color = Colors.Yellow;
                    break;
                case 5:
                case 6:
                    attr.Color = Colors.Green;
                    break;
                case 7:
                case 8:
                    attr.Color = Colors.Blue;
                    break;
                case 9:
                case 10:
                    attr.Color = Colors.Purple;
                    break;
            }

            if (HightLight == 1)
                attr.IsHighlighter = true;
            penMemoCanvas.DefaultDrawingAttributes = attr;
        }


        private void btnPenColor_Click(object sender, RoutedEventArgs e)
        {
            RadioButton btnNext = (RadioButton)sender;
            System.Windows.Controls.Image imgNext = (System.Windows.Controls.Image)(btnNext).Content;
            System.Windows.Controls.Image imgNow = (System.Windows.Controls.Image)btnPenColor.Content;

            MyAnimation(btnPenFuncSP, 300, "Height", btnPenFuncSP.ActualHeight, 0);
            btnPenColor.Background = ColorTool.HexColorToBrush("#000000");
            imgNow.Source = imgNext.Source;
            btnPenColor.Tag = btnNext.Tag;

            ChangeMainPenColor();
        }

        private void btnPen_Click(object sender, RoutedEventArgs e)
        {
            StartAnimation(PenSP, PenSlideCtrl);
        }

        private void btnEraser_Click(object sender, RoutedEventArgs e)
        {
            Brush backgroundColor = btnEraserGD.Background;

            if (backgroundColor is SolidColorBrush)
            {
                string colorValue = ((SolidColorBrush)backgroundColor).Color.ToString();
                if (colorValue.Equals("#FFF66F00") == true)
                {
                    btnEraserGD.Background = Brushes.Transparent;
                }
                else
                {
                    btnEraserGD.Background = ColorTool.HexColorToBrush("#F66F00");
                }
            }

        }

        private void btnSetting_Click(object sender, RoutedEventArgs e)
        {
            StartAnimation(SettingSP, SettingSlideCtrl);
        }

        private void StartAnimation(StackPanel sp, System.Windows.Controls.Image image)
        {



            if (sp.Name.Equals("PenSP"))
            {

                DoubleAnimation dda = new DoubleAnimation(0, PenSP.Width + 64, TimeSpan.FromMilliseconds(500));
                if (PenSP.Width <= 0)
                {
                    dda = new DoubleAnimation(64, PenSP.ActualWidth + 64, TimeSpan.FromMilliseconds(500));
                }
                else
                {
                    dda = new DoubleAnimation(PenSP.ActualWidth + 64, 64, TimeSpan.FromMilliseconds(500));
                }
                Canvas.SetTop(mediaListBorder, Double.NaN);
                Canvas.SetRight(mediaListBorder, Double.NaN);
                //dda.FillBehavior = FillBehavior.Stop;
                this.mediaListBorder.BeginAnimation(Canvas.LeftProperty, dda);

                if (btnPenFuncSP.Height > 0)
                {
                    btnPenColor.Background = ColorTool.HexColorToBrush("#000000");
                    MyAnimation(btnPenFuncSP, 300, "Height", btnPenFuncSP.ActualHeight, 0);
                    //MyAnimation(mediaListBorder, 300, "Height", btnPenFuncSP.ActualHeight, 0);
                }
                if (btnFuncSP.Height > 0)
                {
                    btnBold.Background = ColorTool.HexColorToBrush("#000000");
                    MyAnimation(btnFuncSP, 300, "Height", btnFuncSP.ActualHeight, 0);
                    //MyAnimation(mediaListBorder, 300, "Height", btnFuncSP.ActualHeight, 0);

                }

            }
            else
            {
                if (thumnailCanvas.Visibility == Visibility.Visible)
                {
                    //Wayne Mark 圖層的抽取，取消便利貼視窗
                    ChangeThumbNailListBoxRelativeStatus();
                    //thumnailCanvas.Visibility = Visibility.Collapsed;
                    MyAnimation(thumnailCanvas, 500, "Height", 150, 0, () => { thumnailCanvas.Visibility = Visibility.Collapsed; });
                    ViewThumbSP.Background = ColorTool.HexColorToBrush("#000000");
                    //((System.Windows.Controls.Image)(btnViewThumb.Content)).Source = new BitmapImage(new Uri("images/tool-viewThumb@2x.png", UriKind.Relative));
                }

                if (CheckIsNowClick(MemoSP) == true)
                {
                    noteButton_Click();
                }
                //ChangeThumbNailListBoxRelativeStatus();r
            }

            //DoubleAnimation rotateAnimation = new DoubleAnimation()
            //{
            //    From = 0,
            //    To = 360,
            //    Duration = storyboard.Duration
            //};
            Storyboard sb = new Storyboard();
            DoubleAnimation myanimation;
            DoubleAnimation da = new DoubleAnimation();
            //DoubleAnimation da2 = new DoubleAnimation();
            Duration duration = new Duration(TimeSpan.FromMilliseconds(500));
            da.Duration = duration;
            //da2.Duration = duration;
            sb.Children.Add(da);
            //sb.Children.Add(da2);
            Storyboard.SetTarget(da, sp);
            Storyboard.SetTargetProperty(da, new PropertyPath("Width"));
            //Storyboard.SetTarget(da2, PenSlideCtrl);
            // Storyboard.SetTargetProperty(da2, new PropertyPath("(Image.RenderTransform).(RotateTransform.Angle)"));
            if (sp.Width > 0)
            {

                //da.AccelerationRatio = 0.4;
                da.To = 0;
                myanimation = new DoubleAnimation(180, 0, duration);
                //myanimation.AccelerationRatio = 0.4;

            }
            else
            {
                //da.AccelerationRatio = 0.6;
                da.To = sp.ActualWidth;
                myanimation = new DoubleAnimation(0, 180, duration);
                //myanimation.AccelerationRatio = 0.6;
            }
            da.AccelerationRatio = 0.2;
            da.DecelerationRatio = 0.7;
            myanimation.AccelerationRatio = 0.2;
            myanimation.DecelerationRatio = 0.7;
            sb.Completed += (sender2, e2) =>
            {

            };





            // TODO: Add event handler implementation here.
            var ease = new PowerEase { EasingMode = EasingMode.EaseOut };

            //DoubleAnimation(FromValue. ToValue, Duration)
            //DoubleAnimation myanimation = new DoubleAnimation(0, 360, new Duration(TimeSpan.FromMilliseconds(3)));

            //Adding Power ease to the animation
            myanimation.EasingFunction = ease;

            RotateTransform rt = new RotateTransform();

            //  "img" is Image added in XAML
            image.RenderTransform = rt;
            image.RenderTransformOrigin = new Point(0.5, 0.5);

            sb.Begin();
            rt.BeginAnimation(RotateTransform.AngleProperty, myanimation);
            //RotateTransform rt = (RotateTransform)PenSlideCtrl.RenderTransform;
            //rt.BeginAnimation(RotateTransform.AngleProperty,da2);



            //Task.Factory.StartNew(() =>
            //{
            //    Thread.Sleep(500);
            //    this.Dispatcher.BeginInvoke((Action)(() =>
            //    {
            //        Canvas.SetTop(mediaListBorder, Double.NaN);
            //        Canvas.SetRight(mediaListBorder, Double.NaN);
            //        Canvas.SetBottom(mediaListBorder, 64);
            //        Canvas.SetLeft(mediaListBorder, PenSP.Width + 64);
            //    }));

            //});
        }





        private void HideListBoxButton_Click(object sender, RoutedEventArgs e)
        {
            ShowListBoxButton.Visibility = Visibility.Visible;
            thumnailCanvas.Visibility = Visibility.Collapsed;
            thumbNailListBoxStatus = 0;
            ChangeThumbNailListBoxRelativeStatus();
        }

        private void ShowAllImageButton_Checked(object sender, RoutedEventArgs e)
        {
            thumbNailListBoxStatus = 2;
            ChangeThumbNailListBoxRelativeStatus();
        }



        #endregion



        private bool isLockButtonLocked = false;

        int firstIndex = 0;
        private void thumbNailListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            try
            {
                //var btn = FindVisualChildByName<RadioButton>(FR, "notePanelButton");
                //btn.RaiseEvent(new RoutedEventArgs(ToggleButton.ClickEvent));
                firstIndex++;
                if (firstIndex > 1 && (isSyncing == false || isSyncOwner == true))
                {
                    //StackPanel mediaListPanel = GetMediaListPanelInReader();
                    //mediaListPanel.Visibility = Visibility.Collapsed;
                    if (NoteButton != null)
                    {
                        doUpperRadioButtonClicked(MediaCanvasOpenedBy.NoteButton, NoteButton);
                        MediaTableCanvas.Visibility = Visibility.Collapsed;
                    }
                }

                //firstIndex++;
                //if (firstIndex > 1)
                //{
                //    RadioButton NoteRB = FindVisualChildByName<RadioButton>(FR, "NoteButton");
                //    if (NoteRB.IsChecked == true)
                //    {
                //        noteButton_Click();
                //    }
                //    else
                //    {
                //        NoteRB.IsChecked = false;

                //    }
                //}
                //ChangeThumbNailListBoxRelativeStatus();
                //TextBox tb = FindVisualChildByName<TextBox>(FR, "notePanel");
                //bookManager.saveNoteData(userBookSno, curPageIndex.ToString(), "");
                //bookManager.saveNoteData(userBookSno, curPageIndex.ToString(), tb.Text);

                //string cmd1 = string.Format("update booknoteDetail set notes ='" + tb.Text + "' where userbook_sno={0} and page='{1}' "
                //                              , userBookSno
                //                              , curPageIndex.ToString());

                //bookManager.sqlCommandNonQuery(cmd1);


                //var item = thumbNailListBox.SelectedItem as ThumbnailImageAndPage;
                //if (item != null)
                //{
                //    try
                //    {
                //        string cmd = string.Format("select notes from  booknoteDetail where userbook_sno={0} and page='{1}'"
                //                                 , userBookSno
                //                                  , (int.Parse(item.pageIndex) - 1).ToString());

                //        var rs = bookManager.sqlCommandQuery(cmd);

                //        if (rs.fetchRow())
                //        {
                //            tb.Text = rs.getString("notes");
                //        }
                //    }
                //    catch(Exception ex)
                //    {
                //        LogTool.Debug(ex);
                //    }
                //}

            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }


            if (thumbNailListBox.SelectedIndex.Equals(-1))
            {
                return;
            }

            //取得點選的縮圖對應的單頁index
            // tempIndex 等於頁碼
            int tempIndex = 0;
            if (NoteButtonInLBIsClicked || BookMarkInLBIsClicked)
            {
                //原本沒有註解掉
                //thumbNailListBox.Focus();
                object tempItem = thumbNailListBox.SelectedItem;
                tempIndex = singleThumbnailImageAndPageList.IndexOf((ThumbnailImageAndPage)thumbNailListBox.SelectedItem);
            }
            else
            {
                tempIndex = thumbNailListBox.SelectedIndex;
            }

            int animations = 0;
            web_view.ExecuteScript("goToStep(" + (tempIndex + 1).ToString() + ", " + animations + ")");
            if (isHTML5ReaderLoaded)
            {
                sendBroadCast("{\"pageIndex\":" + tempIndex + ",\"cmd\":\"R.TP\"}");
            }
            // Wayne Add
            //上方的總頁數及目前頁數顯示
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(700);
                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    txtPage.Text = string.Format("{0} / {1}", (tempIndex + 1).ToString(), totalPage.ToString());
                }));
                //Thread.Sleep(100);
            });


            //跳至該頁
            //if (viewStatusIndex.Equals(PageMode.SinglePage))
            //{
            //bringBlockIntoView(tempIndex);
            //}
            //else if (viewStatusIndex.Equals(PageMode.DoublePage))
            //{
            //    int index = tempIndex;
            //    if (index % 2 == 1)
            //    {
            //        index = index + 1;
            //    }

            //    int leftCurPageIndex = index - 1;
            //    int rightCurPageIndex = index;
            //    if (hejMetadata.direction.Equals("right"))
            //    {
            //        leftCurPageIndex = index;
            //        rightCurPageIndex = index - 1;
            //    }

            //    bringBlockIntoView(index / 2);
            //}

            //如果非第一次進入程式
            if (isFirstTimeLoaded)
            {
                // Wayne 下面的Code不要動它
                // 通通改成鎖定狀態就好
                isLockButtonLocked = true;

                //非鎖定, 還原一倍大小
                //if (!isLockButtonLocked)
                //{
                //    zoomStep = 0;
                //    PDFScale = (float)zoomStepScale[0];
                //    resetTransform();
                //    LockButton.Visibility = Visibility.Collapsed;
                //}
                //else //鎖定, 維持原狀
                //{
                if (tempIndex.Equals(0) || tempIndex.Equals(thumbNailListBox.Items.Count - 1))
                {
                    //第一及最後一頁X軸要變為中心點
                    //setTransformBetweenSingleAndDoublePage();
                }
                //LockButton.Visibility = Visibility.Visible;
                //}
            }

            //縮圖總攬
            if (thumbNailListBoxOpenedFullScreen)
            {
                thumnailCanvas.Visibility = Visibility.Hidden;
                BindingOperations.ClearBinding(thumnailCanvas, Canvas.HeightProperty);
                BindingOperations.ClearBinding(thumbNailListBox, ListBox.HeightProperty);
                RadioButton ShowAllImageButtonRB = FindVisualChildByName<RadioButton>(FR, "ShowAllImageButton");
                ShowAllImageButtonRB.IsChecked = false;
                ShowListBoxButton.Visibility = Visibility.Visible;
            }



            //為讓點選的縮圖在正中間, 計算位移
            ListBoxItem listBoxItem = (ListBoxItem)thumbNailListBox.ItemContainerGenerator.ContainerFromItem(thumbNailListBox.SelectedItem);
            if (listBoxItem != null)
            {
                listBoxItem.Focus();

                if (!thumbNailListBoxOpenedFullScreen)
                {
                    //if (hejMetadata.direction.Equals("right"))
                    //{
                    //    ScrollViewer sv = FindVisualChildByName<ScrollViewer>(thumbNailListBox, "SVInLV");
                    //    sv.ScrollToRightEnd();
                    //    if ((tempIndex + 1) * listBoxItem.ActualWidth > this.ActualWidth / 2)
                    //    {
                    //        double scrollOffset = sv.ScrollableWidth - (tempIndex + 1) * listBoxItem.ActualWidth + this.ActualWidth / 2;
                    //        sv.ScrollToHorizontalOffset(scrollOffset);
                    //    }
                    //}
                    //else
                    //{
                    if ((tempIndex + 1) * listBoxItem.ActualWidth > this.ActualWidth / 2)
                    {
                        ScrollViewer sv = FindVisualChildByName<ScrollViewer>(thumbNailListBox, "SVInLV");
                        double scrollOffset = (tempIndex + 1) * listBoxItem.ActualWidth - this.ActualWidth / 2;
                        sv.ScrollToHorizontalOffset(scrollOffset);
                    }
                    //}
                }

                //恢復可點選狀態
                //thumbNailListBox.SelectedIndex = -1;

                //resetFocusBackToReader();
            }
            //thumbNailListBox.SelectedIndex = -1;


            ShowFilterCount();
        }

        private void ShowImageCenter()
        {

            if (Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                this.Dispatcher.BeginInvoke(new Action(ShowImageCenter));
            }
            else
            {
                int tempIndex = thumbNailListBox.SelectedIndex < 0 ? 0 : thumbNailListBox.SelectedIndex;
                //為讓點選的縮圖在正中間, 計算位移
                ListBoxItem listBoxItem = (ListBoxItem)thumbNailListBox.ItemContainerGenerator.ContainerFromItem(thumbNailListBox.SelectedItem);
                if (listBoxItem != null)
                {
                    listBoxItem.Focus();

                    if (!thumbNailListBoxOpenedFullScreen)
                    {
                        //if (hejMetadata.direction.Equals("right"))
                        //{
                        //    ScrollViewer sv = FindVisualChildByName<ScrollViewer>(thumbNailListBox, "SVInLV");
                        //    sv.ScrollToRightEnd();
                        //    if ((tempIndex + 1) * listBoxItem.ActualWidth > this.ActualWidth / 2)
                        //    {
                        //        double scrollOffset = sv.ScrollableWidth - (tempIndex + 1) * listBoxItem.ActualWidth + this.ActualWidth / 2;
                        //        sv.ScrollToHorizontalOffset(scrollOffset);
                        //    }
                        //}
                        //else
                        //{
                        if ((tempIndex + 1) * listBoxItem.ActualWidth > this.ActualWidth / 2)
                        {
                            ScrollViewer sv = FindVisualChildByName<ScrollViewer>(thumbNailListBox, "SVInLV");
                            double scrollOffset = (tempIndex + 1) * listBoxItem.ActualWidth - this.ActualWidth / 2;
                            sv.ScrollToHorizontalOffset(scrollOffset);
                        }
                        //}
                    }

                    //恢復可點選狀態
                    //thumbNailListBox.SelectedIndex = -1;

                    //resetFocusBackToReader();
                }
                //thumbNailListBox.SelectedIndex = -1;
            }

        }

        //換頁event
        private void bringBlockIntoView(int pageIndex)
        {
            //試閱
            //if (trialPages != 0)
            //{
            //    if (pageIndex > (trialPages - 1))
            //    {
            //        return;
            //    }
            //}

            Block tempBlock = FR.Document.Blocks.FirstBlock;
            if (!pageIndex.Equals(0))
            {
                for (int i = 0; i < pageIndex; i++)
                {
                    try
                    {
                        tempBlock = tempBlock.NextBlock;
                    }
                    catch (Exception ex)
                    {
                        //頁面超過現有頁數
                        Debug.WriteLine(ex.Message + "@bringBlockIntoView");
                    }
                }
            }


            if (tempBlock != null)
            {
                tempBlock.BringIntoView();
            }

            if (isHTML5ReaderLoaded)
            {
                sendBroadCast("{\"pageIndex\":" + pageIndex + ",\"cmd\":\"R.TP\"}");
            }

            ShowAddition();
            //needToSendBroadCast = false;
        }

        private void resetFocusBackToReader()
        {
            //if (pageViewerPager != null)
            //{
            //    if (pageViewerPager.Focusable && pageViewerPager.IsEnabled)
            //    {
            //        if (!pageViewerPager.IsKeyboardFocused)
            //        {
            //            //Debug.WriteLine("pageViewerPager pageViewerPager.Focusable: {0}, pageViewerPager.IsEnabled:{1}, pageViewerPager.IsKeyboardFocused: {2}, pageViewerPager.IsKeyboardFocusWithin:{3}",
            //            //    pageViewerPager.Focusable.ToString(), pageViewerPager.IsEnabled.ToString(),
            //            //    pageViewerPager.IsKeyboardFocused.ToString(), pageViewerPager.IsKeyboardFocusWithin.ToString()
            //            //    );
            //            Keyboard.Focus(pageViewerPager);
            //        }

            //    }
            //}
        }


        private void ShareButton_Checked(object sender, RoutedEventArgs e)
        {
            MouseTool.ShowLoading();
            SentMailSP.Background = ColorTool.HexColorToBrush("#F66F00");
            SendEmail();


        }

        private void SentMailSP_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //    SentMailSP.Background = ColorTool.HexColorToBrush("#F66F00");
            //    SendEmail();
            //    SentMailSP.Background = ColorTool.HexColorToBrush("#FFFFFF");
        }


        private void SendEmail()
        {
            //doUpperRadioButtonClicked(MediaCanvasOpenedBy.ShareButton, sender);
            //string url = "http://140.111.34.15/WebService/MeetingService.asmx/AnnotationUpload";  //正式機
            string url = webServiceURL;
            //string url = "http://web.emeeting.hyweb.com.tw/WebService/MeetingService.asmx/AnnotationUpload";  //post url，之後應該會要改成用正式機的

            string curAppPath = System.IO.Path.GetDirectoryName(this.pptPath);
            string bookPath = curAppPath + "\\imgMail";
            Directory.CreateDirectory(bookPath);
            string imgFullPath = bookPath + "\\" + Guid.NewGuid() + ".jpg";  //完整路徑

            // Set image source.
            string width = web_view.ActualWidth.ToString();
            string height = web_view.ActualHeight.ToString();
            string RectString = string.Format("0,0,{0},{1}", width, height);
            //iii.Source = CaptureScreenshotTool.Capture(Rect.Parse(RectString));
            BitmapSource bs = CaptureScreenshotTool.Capture(Rect.Parse(RectString));

            UseBitmapCodecsTool.WriteJpeg(imgFullPath, 30, bs);

            string sentNote = "";
            if (bookNoteDictionary.ContainsKey(curPageIndex) == true)
                sentNote = bookNoteDictionary[curPageIndex].text;

            GetAnnotationUpload.AsyncPOST(this.meetingId, this.bookId, this.email, sentNote, imgFullPath, (au) => { GetAnnotationUpload_DoAction(au); });





            //using (FileStream stream = new FileStream(imgFullPath, FileMode.Create))
            //{
            //    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            //    //TextBlock myTextBlock = new TextBlock();
            //    //myTextBlock.Text = "Codec Author is: " + encoder.CodecInfo.Author.ToString();
            //    encoder.Save(stream);
            //}


            // wayne marked hejMatadata 是nulll，所以要改掉
            //string imgFileName = hejMetadata.LImgList[curPageIndex].path;  //圖檔檔名
            //string bookPath = appPath + "\\imgMail";
            //Directory.CreateDirectory(bookPath);
            //string imgFullPath = bookPath + "\\" + imgFileName;  //完整路徑
            //string meetingId = this.meetingId;  //會議ID
            //string bookId = this.bookId;  //書檔ID
            //string email = this.email;  //要寄到哪個EMAIL
            //string note = "";

            //if (bookNoteDictionary.ContainsKey(curPageIndex))
            //{
            //    note = bookNoteDictionary[curPageIndex].text;  //該頁的註記內容
            //}

            ////Multipart 變數
            //string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");
            //byte[] boundaryBytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
            //string formDataTemplate = "\r\n--" + boundary + "\r\nContent-Disposition: form-data; name=\"{0}\";\r\n\r\n{1}";
            //string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n Content-Type: application/octet-stream\r\n\r\n";

            ////XML Request Body
            //StringBuilder sb = new StringBuilder();
            //sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>")
            //    .Append("<UserInfo><MeetingID>").Append(meetingId).Append("</MeetingID><AttachID>")
            //    .Append(bookId).Append("</AttachID><Email>")
            //    .Append(email).Append("</Email><Text>").Append(note).Append("</Text></UserInfo>");

            //Stream memStream = new System.IO.MemoryStream();

            //string xmlDoc = string.Format(formDataTemplate, "xmlDoc", sb.ToString());
            //byte[] formItemBytes = System.Text.Encoding.UTF8.GetBytes(xmlDoc);
            //memStream.Write(formItemBytes, 0, formItemBytes.Length);

            //memStream.Write(boundaryBytes, 0, boundaryBytes.Length);

            //string header = string.Format(headerTemplate, "annotationImage", imgFileName);
            //byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
            //memStream.Write(headerbytes, 0, headerbytes.Length);

            ////寫入圖檔，可以改成直接拍畫面的Bitmap後不存檔直接寫入
            //Border border = GetBorderInReader();
            //StackPanel image = (StackPanel)GetImageInReader();
            //double ratio = border.ActualHeight / image.ActualHeight;
            //double startX = (border.ActualWidth - image.ActualWidth * ratio) / 2;
            //double startY = (int)((SystemParameters.PrimaryScreenHeight - border.ActualHeight) / 2);

            //System.Drawing.Bitmap b = new System.Drawing.Bitmap((int)(image.ActualWidth * ratio), (int)border.ActualHeight);

            //using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(b))
            //{
            //    g.CopyFromScreen((int)startX, (int)startY, 0, 0, b.Size, System.Drawing.CopyPixelOperation.SourceCopy);
            //    g.Dispose();
            //}
            //b.Save(bookPath + "\\temp.bmp");
            ////using (MemoryStream memoryStream = new MemoryStream())
            ////{
            ////    FileStream sourceStream = new FileStream(imgFullPath, FileMode.Open);
            ////    sourceStream.CopyTo(memoryStream);
            ////    decodedPDFPages[decodedPageIndex] = memoryStream.ToArray();
            ////}
            ////FileStream fileStream = new FileStream(bookPath + "\\temp.bmp", FileMode.Open, FileAccess.Read);

            //// Wayne Add 20140826
            //FileStream fileStream = new FileStream(bookPath + "\\temp.bmp", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            //byte[] buffer = new byte[1024];
            //int bytesRead = 0;
            //while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
            //{
            //    memStream.Write(buffer, 0, bytesRead);
            //}
            ////memStream.Write(decodedPDFPages[0], 0, decodedPDFPages[0].Length);

            //memStream.Write(boundaryBytes, 0, boundaryBytes.Length);
            ////fileStream.Close();

            //// POST Request
            //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            //request.Method = "POST";
            //request.ContentType = "multipart/form-data; boundary=" + boundary;
            //request.ContentLength = memStream.Length;

            //Stream dataStream = request.GetRequestStream();
            //memStream.Position = 0;
            //byte[] tempBuffer = new byte[memStream.Length];
            //memStream.Read(tempBuffer, 0, tempBuffer.Length);
            //memStream.Close();
            //dataStream.Write(tempBuffer, 0, tempBuffer.Length);
            //dataStream.Close();

            ////Get Response
            //WebResponse response = request.GetResponse();
            //dataStream = response.GetResponseStream();
            //StreamReader reader = new StreamReader(dataStream);
            //string responseFromServer = reader.ReadToEnd();
            ////textBox1.Text = responseFromServer;
            //reader.Close();
            //dataStream.Close();
            //response.Close();
            //request = null;
            //response = null;

            //RadioButton rb = (RadioButton)sender;
            //rb.IsChecked = false;

            //MessageBox.Show("資料已送出");
            //Thread.Sleep(500);
        }

        private void GetAnnotationUpload_DoAction(AnnotationUpload au)
        {
            // 先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                this.Dispatcher.BeginInvoke(new Action<AnnotationUpload>(GetAnnotationUpload_DoAction), au);
            }
            else
            {
                if (au != null)
                {
                    AutoClosingMessageBox.Show("資料已送出");
                }
                else
                {
                    AutoClosingMessageBox.Show("傳送失敗");
                }

                ShareButton.IsChecked = false;
                SentMailSP.Background = ColorTool.HexColorToBrush("#000000");
                MouseTool.ShowArrow();
            }

        }

        public bool cloud = false;
        public bool today = false;
        private Border GetBorderInReader()
        {
            Border border = FindVisualChildByName<Border>(FR, "PART_ContentHost");
            return border;
        }

        private UIElement GetImageInReader()
        {
            int index = curPageIndex;
            Block tempBlock = FR.Document.Blocks.FirstBlock;
            UIElement img = new UIElement();

            if (FR.CanGoToPage(index))
            {
                for (int i = 0; i < index; i++)
                {
                    tempBlock = tempBlock.NextBlock;
                }
            }
            if (tempBlock != null)
            {
                img = (UIElement)(((BlockUIContainer)tempBlock).Child);
                //img = FindVisualChildByName<System.Windows.Controls.Image>(FR, "imageInReader");
            }
            return img;
        }

        private void btnJoin_Click(object sender, RoutedEventArgs e)
        {

            if (CanNotCollect)
                return;

            if(HasJoin2Folder)
            {
                imgJoin.Source = new BitmapImage(new Uri("image/ebTool-inCloud-on2@2x.png", UriKind.Relative));
            }
            else
            {
                imgJoin.Source = new BitmapImage(new Uri("image/ebTool-toCloud-on2@2x.png", UriKind.Relative));
            }

            if (HasJoin2Folder)
            {
                DelFile win = new DelFile(this,FolderID,bookId);
                var success=win.ShowDialog();
                if(success==true)
                {
                    SaveData("");
                    HasJoin2Folder = false;
                    imgJoin.Source = new BitmapImage(new Uri("image/ebTool-toCloud2@2x.png", UriKind.Relative));
                }
                else
                {
                    imgJoin.Source = new BitmapImage(new Uri("image/ebTool-inCloud2@2x.png", UriKind.Relative));
                }
              
            }
            else
            {
                JoinFolder win = new JoinFolder(this, this.bookId, (FolderID, FolderName) =>
                {
                    //OKFolder win2 = new OKFolder(FolderName, this);
                    //win2.ShowDialog();
                    if (FolderID.Length > 0)
                    {
                        AutoClosingMessageBox.Show("加入成功");
                        HasJoin2Folder = true;
                        imgJoin.Source = new BitmapImage(new Uri("image/ebTool-inCloud2@2x.png", UriKind.Relative));
                        this.FolderID = FolderID;
                        SaveData(FolderID);
                    }
                    else
                    {
                        imgJoin.Source = new BitmapImage(new Uri("image/ebTool-toCloud2@2x.png", UriKind.Relative));
                    }
                });
                win.ShowDialog();
            }

        }

        private void DelData(string FolderID)
        {
            MSCE.ExecuteScalar("delete from userfile where userID=@1 and Fileid=@2 "
                             , account.Replace("_Sync", "")
                             , bookId);
        }

        private void SaveData(string FolderID)
        {

            string count = MSCE.ExecuteScalar("Select count(1) from userfile where userID=@1 and Fileid=@2 "
                              , account.Replace("_Sync", "")
                              , bookId);

            if (count.Equals("0"))
            {
                MSCE.ExecuteNonQuery("insert into userfile (folderid,Fileid,userid) values(@1,@2,@3)"
                                      , FolderID
                                      , bookId
                                      , account.Replace("_Sync", ""));
            }
            else
            {
                MSCE.ExecuteNonQuery("update userfile set folderid=@1 where fileid=@2 and userid=@3"
                                    , FolderID
                                    , bookId
                                    , account.Replace("_Sync", ""));
            }

        }
    }
}



