using BookFormatLoader;
using BookManagerModule;
using CACodec;
using DataAccessObject;
using DownloadManagerModule;
using iTextSharp.text;
using iTextSharp.text.pdf;
using LocalFilesManagerModule;
using MultiLanquageModule;
using Network;
using Newtonsoft.Json;
using O2S.Components.PDFRender4NET;
using PaperLess_Emeeting.App_Code;
using PaperLess_Emeeting.App_Code.MessageBox;
using PaperLess_Emeeting.App_Code.Tools;
using PaperLess_Emeeting.App_Code.ViewModel;
using PaperlessSync.Broadcast.Service;
using PaperlessSync.Broadcast.Socket;
using PXCView36;
using SyncCenterModule;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using Utility;
using System.Linq;
using PaperLess_Emeeting.App_Code.Socket;
using PaperLess_ViewModel;
using Wpf_CustomCursor;

namespace PaperLess_Emeeting
{


    /// <summary>
    /// ReadWindow.xaml 的互動邏輯
    /// </summary>

    #region PaperlessMeeting

    public class PemMemoInfos
    {
        public double canvasWidth { get; set; }
        public double canvasHeight { get; set; }
        public double strokeAlpha { get; set; }
        public string points { get; set; }
        public string strokeColor { get; set; }
        public double strokeWidth { get; set; }

    }

    #endregion


    public partial class ReadWindow : Window, IDisposable, IEventManager
    {

        private FlowDocument _FlowDocument;
        private FlowDocument _FlowDocumentDouble;
        private CACodecTools caTool = new CACodecTools();

        private List<ThumbnailImageAndPage> singleThumbnailImageAndPageList;
        private List<ThumbnailImageAndPage> doubleThumbnailImageAndPageList;

        private List<ImageStatus> singleImgStatus;
        private List<ImageStatus> doubleImgStatus;

        private Dictionary<int, ReadPagePair> singleReadPagePair;
        private Dictionary<int, ReadPagePair> doubleReadPagePair;

        private TransformGroup tfgForImage;
        private TransformGroup tfgForHyperLink;

        private System.Windows.Point start;
        private System.Windows.Point imageOrigin;
        private System.Windows.Point hyperlinkOrigin;

        private int curPageIndex = 0;
        private int offsetOfImage = 0;

        private int trialPages = 0;
        private object selectedBook;
        private BookType bookType;
        private string bookId;
        private string account;
        private string vendorId;

        private ConfigurationManager configMng;


        private static DispatcherTimer checkImageStatusTimer;
        private string bookPath;

        private bool isFirstTimeLoaded = false;
        private int userBookSno; //用來做書籤、註記...等資料庫存取的索引
        private int PDFdpi = 96;
        public double DpiX = 0;
        public double DpiY = 0;
        private float PDFScale = 1.0F;
        private double baseScale = 1;
        private int zoomStep = 0;
        //private double[] zoomStepScale = { 1, 1.25, 1.6, 2, 2.5, 3 }; //放大倍率
        private double[] zoomStepScale = { 1, 1.25, 1.5, 1.75, 2, 2.25, 2.5, 2.75, 3 }; //放大倍率

        //private double[] zoomStepScale = { 1, 1.5, 2, 2.5, 3 }; //放大倍率      
        private bool canPrint = false;

        private byte[][] decodedPDFPages = new byte[2][]; //放已解密的PDF byte array, [0] 單頁或左頁、[1] 右頁

        private XmlDocument XmlDocNcx;

        private HEJMetadata hejMetadata;
        private PageInfoManager pageInfoManager;
        private PageInfoMetadata pageInfo;

        //private static byte[] defaultKey;
        private byte[] defaultKey;

        private Dictionary<int, BookMarkData> bookMarkDictionary;
        private Dictionary<int, NoteData> bookNoteDictionary;
        private Dictionary<int, List<StrokesData>> bookStrokesDictionary;
        private Dictionary<string, LastPageData> lastViewPage;

        private LocalFilesManager localFileMng;

        private DateTime lastTimeOfChangingPage;

        private List<Stroke> tempStrokes;

        private StylusPointCollection stylusPC;
        private Stroke strokeLine;

        private int lastPageMode = 2;
        private bool isStrokeLine = false;
        private string bookRightsDRM = "";
        private bool isSharedButtonShowed = false;

        private FileSystemWatcher fsw;
        private bool isWindowsXP = false;
        private bool needPreload = false;
        private TimeSpan checkInterval = new TimeSpan(0, 0, 0, 0, 200);
        private string CName = System.Environment.MachineName;
        private BookManager bookManager;
        private MultiLanquageManager langMng;

        #region Paperless

        private string userName;
        private string email;
        private string meetingId;
        private string watermark;
        private string dbPath;
        private string webServiceURL;
        private string socketMessage;

        //目前是否在同步中
        private bool isSyncing = false;
        //目前是否是議事管理員
        private bool isSyncOwner = false;

        private SocketClient socket;
        private bool needToSendBroadCast;

        #endregion

        private bool IsSmallDisplaySize = false;
        private bool IsFirstCapture = true;
        public void Dispose()
        {
            GC.Collect();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            this.Topmost = true;
            this.Topmost = false;
            base.OnContentRendered(e);
        }

        //檢查作業系統
        private void CheckOSVersion()
        {
            System.OperatingSystem osInfo = System.Environment.OSVersion;
            switch (osInfo.Version.Major)
            {
                case 5:
                    if (osInfo.Version.Minor == 0)
                    {
                        //Console.WriteLine("Windows 2000");
                    }
                    else
                    {
                        isWindowsXP = true;
                        Console.WriteLine("Windows XP");
                    }
                    break;
            }
        }

        private void savePageMode()
        {
            string query = "";

            if (viewStatusIndex.Equals(PageMode.SinglePage))
            {
                query = "update userbook_metadata set pdfPageMode = 1 Where Sno= " + userBookSno;
            }
            else if (viewStatusIndex.Equals(PageMode.DoublePage))
            {
                query = "update userbook_metadata set pdfPageMode = 2 Where Sno= " + userBookSno;
            }

            bookManager.sqlCommandNonQuery(query);
        }

        private void saveLastReadingPage()
        {
            int targetPageIndex = 0;

            if (viewStatusIndex.Equals(PageMode.SinglePage))
            {
                targetPageIndex = curPageIndex;
            }
            else if (viewStatusIndex.Equals(PageMode.DoublePage))
            {
                ReadPagePair item = doubleReadPagePair[curPageIndex];

                //取單頁頁數小的那頁
                targetPageIndex = Math.Min(item.leftPageIndex, item.rightPageIndex);
                if (targetPageIndex == -1)
                {
                    targetPageIndex = Math.Max(item.leftPageIndex, item.rightPageIndex);
                }
            }

            string CName = System.Environment.MachineName;
            DateTime dt = new DateTime(1970, 1, 1);
            long currentTime = DateTime.Now.ToUniversalTime().Subtract(dt).Ticks / 10000000;
            bool isUpdate = false;
            LastPageData blp = null;
            if (lastViewPage == null)
            {
                blp = new LastPageData();
                blp.index = targetPageIndex;
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
                blp.index = targetPageIndex;
                blp.updatetime = currentTime;
                isUpdate = true;
            }
            else
            {
                blp = new LastPageData();
                blp.index = targetPageIndex;
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

        private void clearReadPagePairData(Dictionary<int, ReadPagePair> pagePair)
        {
            int totalPageCount = pagePair.Count;
            for (int i = 0; i < totalPageCount; i++)
            {
                ReadPagePair item = pagePair[i];

                if (item.leftImageSource != null)
                {
                    item.leftImageSource = null;
                    item.decodedPDFPages = new byte[2][];
                }
            }

            pagePair.Clear();
            pagePair = null;
        }

        private bool isAllBookPageChecked = false;

        //進看書頁第一次檢查下載進度條以及多媒體感應框, 萬一沒有下載完, 交給FileSystemWatcher處理
        private bool checkThumbnailBorderAndMediaListStatus()
        {
            //檢查目前確實有的檔案
            int totalFilesCount = hejMetadata.allFileList.Count;
            int downloadedFilesCount = 0;

            for (int i = 0; i < totalFilesCount; i++)
            {
                string filePath = bookPath + "\\HYWEB\\" + hejMetadata.allFileList[i];
                if (File.Exists(filePath))
                {
                    downloadedFilesCount++;
                }
            }

            //找出資料夾目前所有的書檔
            string[] tempNum = Directory.GetFiles(bookPath + "\\HYWEB\\", "*.pdf");
            if (bookType.Equals(BookType.HEJ))
            {
                tempNum = Directory.GetFiles(bookPath + "\\HYWEB\\", "*.jpg");
            }

            //更改已下載的書檔狀態
            for (int i = 0; i < tempNum.Length; i++)
            {
                for (int j = 0; j < hejMetadata.LImgList.Count; j++)
                {
                    if (tempNum[i].Substring(tempNum[i].LastIndexOf("\\") + 1).Equals(hejMetadata.LImgList[j].path.Replace("HYWEB\\", "")))
                    {
                        if (!singleThumbnailImageAndPageList[j].isDownloaded)
                        {
                            singleThumbnailImageAndPageList[j].isDownloaded = true;
                        }
                    }
                }
            }

            if (tempNum.Length.Equals(hejMetadata.LImgList.Count))
            {
                //頁面全部下載完畢
                isAllBookPageChecked = true;
            }

            if (ObservableMediaList != null)
            {
                //更改已下載的多媒體檔狀態
                for (int k = 0; k < ObservableMediaList.Count; k++)
                {
                    for (int i = 0; i < ObservableMediaList[k].mediaList.Count; i++)
                    {
                        if (File.Exists(ObservableMediaList[k].mediaList[i].mediaSourcePath))
                        {
                            if (!ObservableMediaList[k].mediaList[i].downloadStatus)
                            {
                                ObservableMediaList[k].mediaList[i].downloadStatus = true;
                            }
                        }
                    }
                }
            }

            tempNum = null;

            if (!totalFilesCount.Equals(downloadedFilesCount))
            {
                //未下載完成, 顯示目前進度
                downloadProgBar.Value = downloadedFilesCount;
                return false;
            }
            else
            {
                //下載完成
                downloadProgBar.Visibility = Visibility.Collapsed;
                return true;
            }
        }

        //檢查檔案下載狀態, 在檔案下載完改變檔名的時候更換狀態
        void fsw_Changed(object sender, FileSystemEventArgs e)
        {
            if (hejMetadata == null)
            {
                return;
            }

            string tmpFileName = e.Name;
            string filename = System.IO.Path.GetFileName(tmpFileName.Replace(".tmp", ""));

            int downloadedFilesCount = 0;
            int LimgListCount = hejMetadata.LImgList.Count;
            if (!isAllBookPageChecked)
            {
                for (int i = 0; i < LimgListCount; i++)
                {
                    if (!singleThumbnailImageAndPageList[i].isDownloaded)
                    {
                        if (hejMetadata.LImgList[i].path.Contains(filename))
                        {
                            singleThumbnailImageAndPageList[i].isDownloaded = true;
                            downloadedFilesCount++;
                            break;
                        }
                    }
                    else
                    {
                        downloadedFilesCount++;
                    }
                }
                isAllBookPageChecked = LimgListCount == downloadedFilesCount ? true : false;
            }
            else
            {
                downloadedFilesCount = hejMetadata.LImgList.Count;
                isAllBookPageChecked = LimgListCount == downloadedFilesCount ? true : false;
            }

            int totalMediaCount = 0;
            if (isAllBookPageChecked)
            {
                int obMedialistCount = ObservableMediaList.Count;
                for (int k = 0; k < obMedialistCount; k++)
                {
                    int medialistCount = ObservableMediaList[k].mediaList.Count;
                    totalMediaCount += medialistCount;
                    for (int i = 0; i < medialistCount; i++)
                    {
                        if (!ObservableMediaList[k].mediaList[i].downloadStatus)
                        {
                            string targetName = System.IO.Path.GetFileName(ObservableMediaList[k].mediaList[i].mediaSourcePath);
                            if (targetName == filename)
                            {
                                ObservableMediaList[k].mediaList[i].downloadStatus = true;
                                downloadedFilesCount++;
                                break;
                            }
                        }
                        else
                        {
                            downloadedFilesCount++;
                        }
                    }
                }
            }

            int totalFilesCount = LimgListCount + totalMediaCount;

            try
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    Debug.WriteLine("downloadedFilesCount / totalFilesCount:" + downloadedFilesCount.ToString() + " / " + totalFilesCount.ToString());
                    if (!totalFilesCount.Equals(downloadedFilesCount))
                    {
                        if (downloadProgBar.Value < downloadedFilesCount)
                            downloadProgBar.Value = downloadedFilesCount;
                    }
                    else
                    {
                        //下載完成
                        downloadProgBar.Visibility = Visibility.Collapsed;
                        fsw.EnableRaisingEvents = false;
                        fsw.IncludeSubdirectories = false;
                        fsw.Changed -= new FileSystemEventHandler(fsw_Changed);
                        fsw = null;
                        //isWindowsXP = false;
                    }
                }));
            }
            catch
            { }
        }

        #region Preparation Work

        private ObservableCollection<MediaList> ObservableMediaList;

        private void _InitializedEventHandler(System.Object sender, EventArgs e)
        {
            getBookPath();

            //defaultKey = getCipherKey();

            byte[] curKey = defaultKey;

            loadBookXMLFiles();

            curKey = null;

            initializeTransFromGroup();

            GetDpiSetting(out DpiX, out DpiY);

            PDFdpi = Convert.ToInt32(Math.Max(DpiX, DpiY));

            prepareReadingPageDataSource();


            setDirection();

            //快速鍵
            AddHotKeys();

            this.Initialized -= this._InitializedEventHandler;

        }

        private void AddHotKeys()
        {
            RoutedCommand zoomInSettings = new RoutedCommand();
            zoomInSettings.InputGestures.Add(new KeyGesture(Key.OemMinus, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(zoomInSettings, RepeatButton_Click_1));

            RoutedCommand zoomOutSettings = new RoutedCommand();
            zoomOutSettings.InputGestures.Add(new KeyGesture(Key.OemPlus, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(zoomOutSettings, RepeatButton_Click_2));
        }


        const int LOGPIXELSX = 88;
        const int LOGPIXELSY = 90;

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int Index);

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr Hwnd);

        void GetDpiSetting(out double DpiX, out double DpiY)
        {
            /// get desktop dc
            IntPtr h = GetDC(IntPtr.Zero);

            /// get dpi from dc
            DpiX = GetDeviceCaps(h, LOGPIXELSX);
            DpiY = GetDeviceCaps(h, LOGPIXELSY);
        }
        //void GetDpiSetting(out double DpiX, out double DpiY)
        //{
        //    const double DEFAULT_DPI = 96.0;
        //    /// get transform matrix from current main window
        //    /// 
        //    Matrix m = PresentationSource
        //        .FromVisual(Application.Current.MainWindow)
        //        .CompositionTarget.TransformToDevice;

        //    /// scale default dpi
        //    DpiX = m.M11 * DEFAULT_DPI;
        //    DpiY = m.M22 * DEFAULT_DPI;
        //}

        private void setDirection()
        {
            //default 右翻書，有的書可能沒有註明
            if (hejMetadata.direction.Equals("right"))
            {
                _FlowDocument.FlowDirection = FlowDirection.RightToLeft;
                _FlowDocumentDouble.FlowDirection = FlowDirection.RightToLeft;
            }
            else
            {
                _FlowDocument.FlowDirection = FlowDirection.LeftToRight;
                _FlowDocumentDouble.FlowDirection = FlowDirection.LeftToRight;
            }
        }

        //開燈箱時已由service取回DRM並存到資料庫，直接由資料庫取出就好了
        public void getBookRightsAsync(string bookId)
        {
            if (bookRightsDRM != null && bookRightsDRM != "")
            {
                try
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    string drmStr = caTool.stringDecode(bookRightsDRM, true);
                    xmlDoc.LoadXml(drmStr);
                    XmlNodeList baseList = xmlDoc.SelectNodes("/drm/functions");
                    foreach (XmlNode node in baseList)
                    {
                        if (node.InnerText.Contains("canPrint"))
                        {
                            canPrint = true;
                            break;
                        }
                    }
                }
                catch { }
            }
        }


        private void initializeTransFromGroup()
        {
            tfgForImage = new TransformGroup();
            ScaleTransform xform = new ScaleTransform();
            tfgForImage.Children.Add(xform);

            TranslateTransform tt = new TranslateTransform();
            tfgForImage.Children.Add(tt);

            tfgForHyperLink = new TransformGroup();
            ScaleTransform stf = new ScaleTransform();
            tfgForHyperLink.Children.Add(stf);

            TranslateTransform ttf = new TranslateTransform();
            tfgForHyperLink.Children.Add(ttf);

            xform = null;
            tt = null;
            stf = null;
            ttf = null;
        }

        public string getCipherValue(string encryptionFile)
        {
            string cValue = "";
            if (!File.Exists(encryptionFile))
                return cValue;

            XmlDocument xDoc = new XmlDocument();
            try
            {
                xDoc.Load(encryptionFile);
                XmlNodeList ValueNode = xDoc.GetElementsByTagName("enc:CipherValue");
                cValue = ValueNode[0].InnerText;
            }
            catch (Exception ex)
            {
                Console.WriteLine("getCipherValue error=" + ex.ToString());
            }

            return cValue;
        }

        #endregion

        #region Reading Pages

        #region PDF Reading Pages

        public class NativeMethods
        {
            [DllImport("ole32.dll")]
            public static extern void CoTaskMemFree(IntPtr pv);

            [DllImport("ole32.dll")]
            public static extern IntPtr CoTaskMemAlloc(IntPtr cb);

            [DllImport("libpdf2jpg.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
            public static extern IntPtr pdfLoadFromMemory(int dpi, float scale, IntPtr ibuf, int ilen, IntPtr obptr, IntPtr olptr, int pgs);

            [DllImport("libpdf2jpg.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
            public static extern int pdfNumberOfPages(IntPtr ibuf, int pgs);

            [DllImport("libpdf2jpg.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
            public static extern int pdfPageSize(int dpi, float scale, IntPtr ibuf, int ilen, IntPtr pWidth, IntPtr pHeight, int pgs);

            [DllImport("libpdf2jpg.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
            public static extern IntPtr pdfLoadFromMemoryPartial(int dpi, float scale, IntPtr ibuf, int ilen, IntPtr obptr, IntPtr olptr, int x0, int y0, int x1, int y1,
            int pgs);
        }

        //將 PDF ren 成 BitmapSource
        //private BitmapSource renPdfToBitmapSource(string pageFile, byte[] key, int pg, int dpi, float scal, int decodedPageIndex)
        //{
        //    return CreateBitmapSourceFromBitmap(renPdfToBitmap(pageFile, key, pg, dpi, scal, decodedPageIndex));
        //}



        //將 PDF ren 成 Bitmap
        private Bitmap renPdfToBitmap(string pageFile, byte[] key, int pg, int dpi, float scal, int decodedPageIndex, bool isSinglePage)
        {
            //Mutex mLoad = new Mutex(requestInitialOwnership, "LoadMutex", out loadMutexWasCreated);
            //if (!(requestInitialOwnership & loadMutexWasCreated))
            //{
            //    mLoad.WaitOne();
            //}

            System.Drawing.Color bgColor = System.Drawing.Color.White; //背景白色
            Bitmap bmp = null;
            try
            {
                if (decodedPDFPages[decodedPageIndex] == null) //如果此頁已經解密過，就直接拿來ren，不用再重新解密一次
                    decodedPDFPages[decodedPageIndex] = caTool.fileAESDecode(pageFile, key);
            }
            catch (Exception e)
            {
                //TODO: 萬一檔案解析失敗, 判定為壞檔, 重新下載
                decodedPDFPages[decodedPageIndex] = null;
                LogTool.Debug(e);
                //throw e;
            }

            try
            {   //TODO: 改成把PDF實體拉出來變global的
                PDFDoc pdfDoc = new PDFDoc();
                pdfDoc.Init("PVD20-M4IRG-QYZK9-MNJ2U-DFTK1-MAJ4L", "PDFX3$Henry$300604_Allnuts#");
                pdfDoc.OpenFromMemory(decodedPDFPages[decodedPageIndex], (uint)decodedPDFPages[decodedPageIndex].Length, 0);
                PXCV_Lib36.PXV_CommonRenderParameters commonRenderParam = prepareCommonRenderParameter(pdfDoc, dpi, pg, scal, 0, 0, isSinglePage);
                pdfDoc.DrawPageToDIBSection(IntPtr.Zero, pg, bgColor, commonRenderParam, out bmp);
                pdfDoc.ReleasePageCachedData(pg, (int)PXCV_Lib36.PXCV_ReleaseCachedDataFlags.pxvrcd_ReleaseDocumentImages);
                pdfDoc.Delete();
            }
            catch (Exception e)
            {
                LogTool.Debug(e);
                //throw e;
            }
            //bmp.Save("c:\\Temp\\test.bmp");
            return bmp;
        }

        //產生 PDF 元產所需的參數 (改用Thread的方式ren)
        private PXCV_Lib36.PXV_CommonRenderParameters prepareCommonRenderParameter(PDFDoc pdfDoc, int dpi, int pageNumber, float zoom, int offsetX, int offsetY, Border border, bool isSinglePage)
        {
            IntPtr p1 = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(PXCV_Helper.RECT)));
            IntPtr p2 = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(PXCV_Helper.RECT)));
            System.Drawing.Point m_Offset = new System.Drawing.Point(offsetX, offsetY);
            System.Drawing.Size aPageSize = System.Drawing.Size.Empty;
            PXCV_Helper.RECT aWholePage = new PXCV_Helper.RECT();
            PXCV_Helper.RECT aDrawRect = new PXCV_Helper.RECT();
            PXCV_Lib36.PXV_CommonRenderParameters commonRenderParam = new PXCV_Lib36.PXV_CommonRenderParameters();
            PageDimension aPageDim;
            pdfDoc.GetPageDimensions(pageNumber, out aPageDim.w, out aPageDim.h);

            //Border bd = border; //可視範圍
            double borderHeight = (border.ActualHeight / (double)96) * dpi;
            double borderWidth = (border.ActualWidth / (double)96) * dpi;

            if (zoomStep == 0)
            {
                //PDF原尺吋
                aPageSize.Width = (int)((aPageDim.w / 72.0 * dpi) * zoom);
                aPageSize.Height = (int)((aPageDim.h / 72.0 * dpi) * zoom);

                double borderRatio = borderWidth / borderHeight;
                double renderImageRatio = 0;

                if (isSinglePage)
                {
                    renderImageRatio = (double)aPageSize.Width / (double)aPageSize.Height;
                }
                else
                {
                    renderImageRatio = (double)(aPageSize.Width * 2) / (double)aPageSize.Height;
                }

                if (aPageSize.Width < borderWidth && aPageSize.Height < borderHeight)
                {   //PDF原尺吋就比canvas還小 --> 貼齊canvas
                    double newPageW, newPageH;
                    if (renderImageRatio > borderRatio)
                    {   //寬先頂到
                        newPageW = borderWidth / 2;
                        baseScale = newPageW / (double)aPageSize.Width;
                        newPageH = Math.Round(baseScale * (double)aPageSize.Height, 2);
                    }
                    else
                    {   //高先頂到
                        newPageH = borderHeight;
                        baseScale = newPageH / (double)aPageSize.Height;
                        newPageW = Math.Round(baseScale * (double)aPageSize.Width, 2);
                    }

                    aPageSize.Width = (int)newPageW;
                    aPageSize.Height = (int)newPageH;
                }
                else
                {   //PDF有一邊比canvas還大
                    double newPageW, newPageH;
                    if (renderImageRatio > borderRatio)
                    {   //寬先頂到
                        newPageW = borderWidth / 2;
                        baseScale = newPageW / (double)aPageSize.Width;
                        newPageH = Math.Round(baseScale * (double)aPageSize.Height, 2);
                    }
                    else
                    {   //高先頂到
                        newPageH = borderHeight;
                        baseScale = newPageH / (double)aPageSize.Height;
                        newPageW = Math.Round(baseScale * (double)aPageSize.Width, 2);
                    }

                    aPageSize.Width = (int)newPageW;
                    aPageSize.Height = (int)newPageH;
                }
            }
            else
            {
                //PDF原尺吋
                aPageSize.Width = (int)((aPageDim.w / 72.0 * dpi) * zoom * baseScale);
                aPageSize.Height = (int)((aPageDim.h / 72.0 * dpi) * zoom * baseScale);
            }

            //Region rgn1 = new Region(new System.Drawing.Rectangle(-m_Offset.X, -m_Offset.Y, aPageSize.Width, aPageSize.Height));
            //rgn1.Complement(new System.Drawing.Rectangle(0, 0, (int)borderWidth, (int)borderHeight));
            //rgn1.Complement(new System.Drawing.Rectangle(0, 0, aPageSize.Width, aPageSize.Height));
            aWholePage.left = -m_Offset.X;
            aWholePage.top = -m_Offset.Y;
            aWholePage.right = aWholePage.left + aPageSize.Width;
            aWholePage.bottom = aWholePage.top + aPageSize.Height;

            //計算要ren的範圍
            //TODO: 改成部分ren，目前是全ren
            aDrawRect.left = 0;
            aDrawRect.top = 0;
            if (zoomStep == 0)
            {
                if (aPageSize.Width < borderWidth)
                {
                    aDrawRect.right = aPageSize.Width;
                }
                else
                {
                    aDrawRect.right = (int)borderWidth;
                }
                if (aPageSize.Height < borderHeight)
                {
                    aDrawRect.bottom = aPageSize.Height;
                }
                else
                {
                    aDrawRect.bottom = (int)borderHeight;
                }
            }
            else
            {
                aDrawRect.right = aPageSize.Width;
                aDrawRect.bottom = aPageSize.Height;
            }

            //aDrawRect.right = aPageSize.Width;
            //aDrawRect.bottom = aPageSize.Height;
            Marshal.StructureToPtr(aWholePage, p1, false);
            Marshal.StructureToPtr(aDrawRect, p2, false);
            commonRenderParam.WholePageRect = p1;
            commonRenderParam.DrawRect = p2;
            commonRenderParam.RenderTarget = PXCV_Lib36.PXCV_RenderMode.pxvrm_Viewing;
            commonRenderParam.Flags = 0;
            //System.Drawing.Rectangle rc = new System.Drawing.Rectangle(0, 0, aControlSize.Width, aControlSize.Height);
            //System.Drawing.Rectangle rc = new System.Drawing.Rectangle(0, 0, aPageSize.Width, aPageSize.Height);
            //rc.Intersect(new System.Drawing.Rectangle(-m_Offset.X, -m_Offset.Y, aPageSize.Width, aPageSize.Height));
            //e.DrawRectangle(System.Windows.Media.Brushes.White, null, new Rect(new System.Windows.Size(rc.Width, rc.Height)));
            //aGraphics.FillRectangle(System.Drawing.Brushes.White, rc);
            //aGraphics.FillRegion(System.Drawing.Brushes.Gray, rgn1);
            //rgn1.Dispose();



            return commonRenderParam;
        }

        //產生 PDF 元產所需的參數
        private PXCV_Lib36.PXV_CommonRenderParameters prepareCommonRenderParameter(PDFDoc pdfDoc, int dpi, int pageNumber, float zoom, int offsetX, int offsetY, bool isSinglePage)
        {
            IntPtr p1 = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(PXCV_Helper.RECT)));
            IntPtr p2 = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(PXCV_Helper.RECT)));
            System.Drawing.Point m_Offset = new System.Drawing.Point(offsetX, offsetY);
            System.Drawing.Size aPageSize = System.Drawing.Size.Empty;
            PXCV_Helper.RECT aWholePage = new PXCV_Helper.RECT();
            PXCV_Helper.RECT aDrawRect = new PXCV_Helper.RECT();
            PXCV_Lib36.PXV_CommonRenderParameters commonRenderParam = new PXCV_Lib36.PXV_CommonRenderParameters();
            PageDimension aPageDim;
            pdfDoc.GetPageDimensions(pageNumber, out aPageDim.w, out aPageDim.h);

            Border border = GetBorderInReader(); //可視範圍
            double borderHeight = (border.ActualHeight / (double)96) * dpi;
            double borderWidth = (border.ActualWidth / (double)96) * dpi;

            if (zoomStep == 0)
            {
                //PDF原尺吋
                aPageSize.Width = (int)((aPageDim.w / 72.0 * dpi) * zoom);
                aPageSize.Height = (int)((aPageDim.h / 72.0 * dpi) * zoom);

                double borderRatio = borderWidth / borderHeight;
                double renderImageRatio = 0;

                if (isSinglePage)
                {
                    renderImageRatio = (double)aPageSize.Width / (double)aPageSize.Height;
                }
                else
                {
                    renderImageRatio = (double)(aPageSize.Width * 2) / (double)aPageSize.Height;
                }

                if (aPageSize.Width < borderWidth && aPageSize.Height < borderHeight)
                {   //PDF原尺吋就比canvas還小 --> 貼齊canvas
                    double newPageW, newPageH;
                    if (renderImageRatio > borderRatio)
                    {   //寬先頂到
                        newPageW = borderWidth / 2;
                        baseScale = newPageW / (double)aPageSize.Width;
                        newPageH = Math.Round(baseScale * (double)aPageSize.Height, 2);
                    }
                    else
                    {   //高先頂到
                        newPageH = borderHeight;
                        baseScale = newPageH / (double)aPageSize.Height;
                        newPageW = Math.Round(baseScale * (double)aPageSize.Width, 2);
                    }

                    aPageSize.Width = (int)newPageW;
                    aPageSize.Height = (int)newPageH;
                }
                else
                {   //PDF有一邊比canvas還大
                    double newPageW, newPageH;
                    if (renderImageRatio > borderRatio)
                    {   //寬先頂到
                        newPageW = borderWidth / 2;
                        baseScale = newPageW / (double)aPageSize.Width;
                        newPageH = Math.Round(baseScale * (double)aPageSize.Height, 2);
                    }
                    else
                    {   //高先頂到
                        newPageH = borderHeight;
                        baseScale = newPageH / (double)aPageSize.Height;
                        newPageW = Math.Round(baseScale * (double)aPageSize.Width, 2);
                    }

                    aPageSize.Width = (int)newPageW;
                    aPageSize.Height = (int)newPageH;
                }
            }
            else
            {
                //PDF原尺吋
                aPageSize.Width = (int)((aPageDim.w / 72.0 * dpi) * zoom * baseScale);
                aPageSize.Height = (int)((aPageDim.h / 72.0 * dpi) * zoom * baseScale);
            }

            //Region rgn1 = new Region(new System.Drawing.Rectangle(-m_Offset.X, -m_Offset.Y, aPageSize.Width, aPageSize.Height));
            //rgn1.Complement(new System.Drawing.Rectangle(0, 0, (int)borderWidth, (int)borderHeight));
            //rgn1.Complement(new System.Drawing.Rectangle(0, 0, aPageSize.Width, aPageSize.Height));
            aWholePage.left = -m_Offset.X;
            aWholePage.top = -m_Offset.Y;
            aWholePage.right = aWholePage.left + aPageSize.Width;
            aWholePage.bottom = aWholePage.top + aPageSize.Height;

            //計算要ren的範圍
            //TODO: 改成部分ren，目前是全ren
            aDrawRect.left = 0;
            aDrawRect.top = 0;
            if (zoomStep == 0)
            {
                if (aPageSize.Width < borderWidth)
                {
                    aDrawRect.right = aPageSize.Width;
                }
                else
                {
                    aDrawRect.right = (int)borderWidth;
                }
                if (aPageSize.Height < borderHeight)
                {
                    aDrawRect.bottom = aPageSize.Height;
                }
                else
                {
                    aDrawRect.bottom = (int)borderHeight;
                }
            }
            else
            {
                aDrawRect.right = aPageSize.Width;
                aDrawRect.bottom = aPageSize.Height;
            }

            //aDrawRect.right = aPageSize.Width;
            //aDrawRect.bottom = aPageSize.Height;
            Marshal.StructureToPtr(aWholePage, p1, false);
            Marshal.StructureToPtr(aDrawRect, p2, false);
            commonRenderParam.WholePageRect = p1;
            commonRenderParam.DrawRect = p2;
            commonRenderParam.RenderTarget = PXCV_Lib36.PXCV_RenderMode.pxvrm_Viewing;
            commonRenderParam.Flags = 0;
            //System.Drawing.Rectangle rc = new System.Drawing.Rectangle(0, 0, aControlSize.Width, aControlSize.Height);
            //System.Drawing.Rectangle rc = new System.Drawing.Rectangle(0, 0, aPageSize.Width, aPageSize.Height);
            //rc.Intersect(new System.Drawing.Rectangle(-m_Offset.X, -m_Offset.Y, aPageSize.Width, aPageSize.Height));
            //e.DrawRectangle(System.Windows.Media.Brushes.White, null, new Rect(new System.Windows.Size(rc.Width, rc.Height)));
            //aGraphics.FillRectangle(System.Drawing.Brushes.White, rc);
            //aGraphics.FillRegion(System.Drawing.Brushes.Gray, rgn1);
            //rgn1.Dispose();


            return commonRenderParam;
        }

        //Bitmap to BitmapSource
        private BitmapSource CreateBitmapSourceFromBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
            {
                //throw new ArgumentNullException("bitmap");
                LogTool.Debug(new ArgumentNullException("bitmap"));
            }

            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                bitmap.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }

        private MemoryStream renPdfToStream(string pageFile, byte[] key, int pg, int dpi, float scal)
        {
            Mutex mLoad = new Mutex(requestInitialOwnership, "LoadMutex", out loadMutexWasCreated);
            if (!(requestInitialOwnership & loadMutexWasCreated))
            {
                mLoad.WaitOne();
            }
            MemoryStream outputStream = new MemoryStream();
            MemoryStream bs = caTool.fileAESDecode(pageFile, key, false);

            Byte[] pdfBinaryArray = new Byte[bs.Length];
            int iLength = pdfBinaryArray.Length;

            IntPtr pdfBufferPtr = Marshal.AllocHGlobal(iLength);
            try
            {
                Marshal.Copy(bs.GetBuffer(), 0, pdfBufferPtr, iLength);
            }
            catch
            {
                Marshal.Copy(bs.GetBuffer(), 0, pdfBufferPtr, iLength - 1);
            }
            bs.Close();

            IntPtr oLengthPtr = Marshal.AllocHGlobal(4);
            IntPtr oBufferPtr = Marshal.AllocHGlobal(4);
            IntPtr pdfRet = new IntPtr();
            IntPtr oBuffer = new IntPtr();
            int oLength = 1;
            try
            {
                //Partial
                IntPtr pWidth = Marshal.AllocHGlobal(4);
                IntPtr pHeight = Marshal.AllocHGlobal(4);
                NativeMethods.pdfPageSize(dpi, scal, pdfBufferPtr, iLength, pWidth, pHeight, pg);
                int oWidth = Marshal.ReadInt32(pWidth);
                int oHeight = Marshal.ReadInt32(pHeight);

                Marshal.FreeHGlobal(pWidth);
                Marshal.FreeHGlobal(pHeight);

                pdfRet = NativeMethods.pdfLoadFromMemoryPartial(dpi, scal, pdfBufferPtr, iLength, oBufferPtr, oLengthPtr, 0, 0, oWidth, oHeight,
                pg);

                //pdfRet = NativeMethods.pdfLoadFromMemory(dpi, scal, pdfBufferPtr, iLength, oBufferPtr, oLengthPtr, pg);
                oBuffer = (IntPtr)Marshal.ReadInt32(oBufferPtr);
                oLength = Marshal.ReadInt32(oLengthPtr);
                Byte[] oAry = new Byte[oLength];
                Marshal.Copy(oBuffer, oAry, 0, oLength); // 'Copy memory block
                outputStream.Write(oAry, 0, oAry.Length);
            }
            catch
            {
                Marshal.FreeHGlobal(pdfBufferPtr);
                Marshal.FreeHGlobal(oBufferPtr);
                Marshal.FreeHGlobal(oLengthPtr);
            }
            NativeMethods.CoTaskMemFree(oBuffer); // 'Free memory(coupled with "CoTaskMemAlloc")
            Marshal.FreeHGlobal(pdfBufferPtr);
            Marshal.FreeHGlobal(oBufferPtr);
            Marshal.FreeHGlobal(oLengthPtr);

            outputStream.Position = 0;

            mLoad.ReleaseMutex();

            return outputStream;
        }

        private System.Windows.Controls.Image getPHEJSingleBigPageToReplace(CACodecTools caTool, byte[] curKey, string pagePath, float scal)
        {
            System.Windows.Controls.Image bigImage = new System.Windows.Controls.Image();
            //同時處理單頁以及雙頁資料
            //單頁
            bigImage.Source = getPHEJSingleBitmapImage(caTool, curKey, pagePath, scal);
            bigImage.Stretch = Stretch.Uniform;
            bigImage.Margin = new Thickness(offsetOfImage);
            bigImage.Name = "imageInReader";
            bigImage.RenderTransform = tfgForImage;
            bigImage.MouseLeftButtonDown += ImageInReader_MouseLeftButtonDown;
            //GC.Collect();
            return bigImage;
        }

        private BitmapImage getPHEJSingleBitmapImage(CACodecTools caTool, byte[] curKey, string pagePath, float scal)
        {
            BitmapImage bitmapImage = new BitmapImage();
            Bitmap image = renPdfToBitmap(pagePath, curKey, 0, PDFdpi, scal, 0, true);
            using (MemoryStream memory = new MemoryStream())
            {
                image.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                //memory.Position = 0;
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.CacheOption = BitmapCacheOption.None;
                bitmapImage.StreamSource.Close();
                bitmapImage.StreamSource = null;
                bitmapImage.Freeze();


                memory.Dispose();
                memory.Close();
                image.Dispose();
                image = null;
            }
            return bitmapImage;
        }

        public bool IsWidthPage(string pagePath)
        {
            bool rtn = false;

            try
            {
                int pageIndex = 0;
                string[] arr = pagePath.Split(new char[] { '_', '.' });
                pageIndex = int.Parse(arr[arr.Length - 2]);
                //string imgName = bookId + "_" + pageIndex + ".jpg";
                string path = Path.Combine(bookPath, "HYWEB", "thumbs");
                string[] files = Directory.GetFiles(path);

                foreach (string file in files)
                {
                    if (file.Contains("_" + pageIndex + ".jpg") == true)
                    {
                        System.Drawing.Image image = System.Drawing.Image.FromFile(file);

                        if (image.Width > image.Height)
                            rtn = true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }

            return rtn;
        }




        // wayne marked 20141201
        private void getPHEJSingleBitmapImageAsync(CACodecTools caTool, byte[] curKey, string pagePath, float scal, int curPageIndex, Border border)
        {
            try
            {
                BitmapImage bitmapImage = new BitmapImage();

                // wayne marked 20141201,配合1139行
                if (IsSmallDisplaySize == true)
                {

                    int PSH = (int)System.Windows.SystemParameters.PrimaryScreenHeight;

                    if (IsWidthPage(pagePath) == true)
                    {

                        switch (PSH)
                        {
                            case 768:
                                PDFdpi = 250;
                                break;
                            case 960:
                                PDFdpi = 250;
                                break;
                            case 1024:
                                PDFdpi = 192;
                                break;
                            case 1440:
                                PDFdpi = 320;
                                break;
                            default:
                                PDFdpi = 250;
                                break;
                        }
                        //MessageBox.Show(1.ToString());
                    }
                    else
                    {
                        switch (PSH)
                        {
                            case 600:
                                PDFdpi = 250;
                                break;
                            case 800:
                                PDFdpi = 250;
                                break;
                            case 960:
                                PDFdpi = 250;
                                break;
                            default:
                                PDFdpi = 96;
                                break;
                        }
                        //MessageBox.Show(2.ToString());
                    }
                }
                else
                {
                    //PDFdpi = 96;
                    int PSW = (int)System.Windows.SystemParameters.PrimaryScreenWidth;
                    int PSH = (int)System.Windows.SystemParameters.PrimaryScreenHeight;

                    int devicePixelWidth = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width;
                    int devicePixelHeight = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height;
                    //MessageBox.Show(devicePixelWidth.ToString());
                    //MessageBox.Show(devicePixelHeight.ToString());

                    switch (PSH)
                    {
                        case 600:
                            PDFdpi = 144;
                            break;
                        case 768:
                            if (devicePixelWidth >= 2160)
                                PDFdpi = 144;
                            break;
                        case 800:
                            if (devicePixelWidth >= 2160)
                                PDFdpi = 144;
                            break;
                        case 840:
                            if (devicePixelWidth >= 2160)
                                PDFdpi = 144;
                            break;
                        case 864:
                            if (devicePixelWidth >= 2160)
                                PDFdpi = 144;
                            break;
                        case 896:
                            if (devicePixelWidth >= 2160)
                                PDFdpi = 144;
                            break;
                        case 928:
                            if (devicePixelWidth >= 2160)
                                PDFdpi = 144;
                            break;
                        case 960:
                            if (devicePixelWidth >= 2160)
                                PDFdpi = 144;
                            break;
                        case 1050:
                            if (devicePixelWidth >= 2160)
                                PDFdpi = 144;
                            break;
                        case 1440:
                            if (devicePixelWidth >= 2160)
                                PDFdpi = 144;
                            break;
                        default:
                            PDFdpi = 96;
                            break;
                    }
                    //MessageBox.Show(3.ToString());
                }

                //float width = (float)System.Windows.SystemParameters.PrimaryScreenWidth;
                //float height = (float)System.Windows.SystemParameters.PrimaryScreenHeight;

                //if (width >= 2160 && height >= 1440)
                //{
                //    PDFdpi = 555;
                //}
                //MessageBox.Show(IsSmallDisplaySize.ToString());
                //MessageBox.Show(width.ToString());
                //MessageBox.Show(height.ToString());
                //MessageBox.Show(PDFdpi.ToString());
                Bitmap image = renPdfToBitmap(pagePath, curKey, 0, PDFdpi, scal, 0, border, true);
                using (MemoryStream memory = new MemoryStream())
                {
                    if (image == null)
                    {
                        image = renPdfToBitmap(pagePath, curKey, 0, PDFdpi, scal, 0, border, true);
                    }
                    image.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                    //memory.Position = 0;
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.CacheOption = BitmapCacheOption.None;
                    bitmapImage.StreamSource.Close();
                    bitmapImage.StreamSource = null;
                    bitmapImage.Freeze();


                    memory.Dispose();
                    memory.Close();
                    image.Dispose();
                    image = null;
                }

                EventHandler<imageSourceRenderedResultEventArgs> imageRenderResult = imageSourceRendered;

                if (imageRenderResult != null)
                {
                    imageRenderResult(this, new imageSourceRenderedResultEventArgs(bitmapImage, curPageIndex, scal));
                    Debug.WriteLine("scal:" + scal.ToString() + "@ getPHEJSingleBitmapImageAsync");
                }
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }
        }

        private System.Windows.Controls.Image getPHEJSingleBigPageToReplace(CACodecTools caTool, byte[] curKey, string pagePath)
        {
            return getPHEJSingleBigPageToReplace(caTool, curKey, pagePath, PDFScale);
        }

        private bool requestInitialOwnership = true;
        private bool loadMutexWasCreated = false;

        private BitmapImage getPHEJDoubleBitmapImage(CACodecTools caTool, byte[] curKey, string leftPagePath, string rightPagePath, float scal)
        {
            BitmapImage bitmapImage = new BitmapImage();
            try
            {
                //雙頁
                Bitmap image1 = renPdfToBitmap(leftPagePath, curKey, 0, PDFdpi, scal, 0, false);
                Bitmap image2 = renPdfToBitmap(rightPagePath, curKey, 0, PDFdpi, scal, 1, false);

                int mergeWidth = Convert.ToInt32(image1.Width + image2.Width);
                int mergeHeight = Convert.ToInt32(Math.Max(image1.Height, image2.Height));


                Bitmap bitmap = new Bitmap(mergeWidth, mergeHeight);
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    g.DrawImage(image1, 0, 0, image1.Width, image1.Height);
                    g.DrawImage(image2, image1.Width, 0, image2.Width, image2.Height);
                    g.Dispose();
                }

                using (MemoryStream memory = new MemoryStream())
                {
                    bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                    //memory.Position = 0;
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.CacheOption = BitmapCacheOption.None;
                    bitmapImage.StreamSource.Close();
                    bitmapImage.StreamSource = null;
                    bitmapImage.Freeze();


                    memory.Dispose();
                    memory.Close();
                    bitmap.Dispose();
                    bitmap = null;
                }

                image1 = null;
                image2 = null;

                GC.Collect();
            }
            catch
            {
                //處理圖片過程出錯
            }
            return bitmapImage;
        }

        private void getPHEJDoubleBitmapImageAsync(CACodecTools caTool, byte[] curKey, string leftPagePath, string rightPagePath, float scal, int curPageIndex, Border border)
        {
            BitmapImage bitmapImage = new BitmapImage();
            Bitmap image1 = null;
            Bitmap image2 = null;
            Bitmap bitmap = null;
            try
            {
                //雙頁
                image1 = renPdfToBitmap(leftPagePath, curKey, 0, PDFdpi, scal, 0, border, false);
                image2 = renPdfToBitmap(rightPagePath, curKey, 0, PDFdpi, scal, 1, border, false);

                int mergeWidth = Convert.ToInt32(image1.Width + image2.Width);
                int mergeHeight = Convert.ToInt32(Math.Max(image1.Height, image2.Height));


                bitmap = new Bitmap(mergeWidth, mergeHeight);
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    g.DrawImage(image1, 0, 0, image1.Width, image1.Height);
                    g.DrawImage(image2, image1.Width, 0, image2.Width, image2.Height);
                    g.Dispose();
                }

                using (MemoryStream memory = new MemoryStream())
                {
                    bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                    //memory.Position = 0;
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.CacheOption = BitmapCacheOption.None;
                    bitmapImage.StreamSource.Close();
                    bitmapImage.StreamSource = null;
                    bitmapImage.Freeze();


                    memory.Dispose();
                    memory.Close();
                    bitmap.Dispose();
                    bitmap = null;
                }

                image1 = null;
                image2 = null;

                GC.Collect();
            }
            catch
            {
                //處理圖片過程出錯
                image1 = null;
                image2 = null;
                bitmap = null;
            }

            EventHandler<imageSourceRenderedResultEventArgs> imageRenderResult = imageSourceRendered;

            if (imageRenderResult != null)
            {
                imageRenderResult(this, new imageSourceRenderedResultEventArgs(bitmapImage, curPageIndex, scal));
                Debug.WriteLine("scal:" + scal.ToString() + " @ getPHEJDoubleBitmapImageAsync");
            }
        }


        #endregion

        #region HEJ Reading Page

        private BitmapImage getHEJSingleBitmapImage(CACodecTools caTool, byte[] curKey, string lastPagePath, float pdfScale)
        {
            BitmapImage bigBitmapImage = new BitmapImage();
            try
            {
                using (MemoryStream bMapLast = caTool.fileAESDecode(lastPagePath, curKey, false))
                {
                    //同時處理單頁以及雙頁資料
                    //單頁
                    bigBitmapImage.BeginInit();
                    bigBitmapImage.StreamSource = bMapLast;
                    bigBitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bigBitmapImage.EndInit();
                    bigBitmapImage.CacheOption = BitmapCacheOption.None;
                    bigBitmapImage.StreamSource.Close();
                    bigBitmapImage.StreamSource = null;
                    bigBitmapImage.Freeze();

                    bMapLast.Dispose();
                    bMapLast.Close();
                }
            }
            catch (Exception e)
            {
                //TODO: 萬一檔案解析失敗, 判定為壞檔, 重新下載

                //throw e;
                LogTool.Debug(e);
            }
            return bigBitmapImage;
        }

        private System.Windows.Controls.Image getSingleBigPageToReplace(CACodecTools caTool, byte[] curKey, string lastPagePath)
        {
            System.Windows.Controls.Image bigImage = new System.Windows.Controls.Image();
            try
            {
                bigImage.Source = getHEJSingleBitmapImage(caTool, curKey, lastPagePath, PDFScale);
                bigImage.Stretch = Stretch.Uniform;
                bigImage.Margin = new Thickness(offsetOfImage);
                bigImage.Name = "imageInReader";
                bigImage.RenderTransform = tfgForImage;
                bigImage.MouseLeftButtonDown += ImageInReader_MouseLeftButtonDown;
            }
            catch
            {
                //處理圖片過程出錯
            }
            return bigImage;
        }

        private BitmapImage getHEJDoubleBitmapImage(CACodecTools caTool, byte[] curKey, string leftPagePath, string rightPagePath, float pdfScale)
        {
            BitmapImage bitmapImage = new BitmapImage();
            try
            {
                using (MemoryStream bMapLeft = caTool.fileAESDecode(leftPagePath, curKey, false))
                {
                    using (MemoryStream bMapRight = caTool.fileAESDecode(rightPagePath, curKey, false))
                    {
                        //雙頁
                        System.Drawing.Bitmap image1 = new Bitmap(bMapLeft);
                        System.Drawing.Bitmap image2 = new Bitmap(bMapRight);

                        int mergeWidth = Convert.ToInt32(image1.Width + image2.Width);
                        int mergeHeight = Convert.ToInt32(Math.Max(image1.Height, image2.Height));

                        Bitmap bitmap = new Bitmap(mergeWidth, mergeHeight);
                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                            g.DrawImage(image1, 0, 0, image1.Width, image1.Height);
                            g.DrawImage(image2, image1.Width, 0, image2.Width, image2.Height);
                            g.Dispose();
                        }

                        using (MemoryStream memory = new MemoryStream())
                        {
                            bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                            //memory.Position = 0;
                            bitmapImage.BeginInit();
                            bitmapImage.StreamSource = memory;
                            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            bitmapImage.EndInit();
                            bitmapImage.CacheOption = BitmapCacheOption.None;
                            bitmapImage.StreamSource.Close();
                            bitmapImage.StreamSource = null;
                            bitmapImage.Freeze();
                            memory.Dispose();
                            memory.Close();
                            bitmap.Dispose();
                            bitmap = null;
                        }

                        bMapLeft.Dispose();
                        bMapLeft.Close();
                        bMapRight.Dispose();
                        bMapRight.Close();
                        image1 = null;
                        image2 = null;
                    }
                }
            }
            catch (Exception e)
            {
                //TODO: 萬一檔案解析失敗, 判定為壞檔, 重新下載
                //throw e;
                LogTool.Debug(e);
            }
            return bitmapImage;
        }

        private System.Windows.Controls.Image getDoubleBigPageToReplace(CACodecTools caTool, byte[] curKey, string leftPagePath, string rightPagePath)
        {
            System.Windows.Controls.Image mergedImage = new System.Windows.Controls.Image();
            try
            {
                mergedImage.Source = getHEJDoubleBitmapImage(caTool, curKey, leftPagePath, rightPagePath, PDFScale);
                mergedImage.Stretch = Stretch.Uniform;
                mergedImage.Margin = new Thickness(offsetOfImage);
                mergedImage.Name = "imageInReader";
                mergedImage.RenderTransform = tfgForImage;
                mergedImage.MouseLeftButtonDown += ImageInReader_MouseLeftButtonDown;
            }
            catch
            {
                //處理圖片過程出錯
            }
            return mergedImage;
        }

        #endregion

        //雙頁頁碼轉換單頁頁碼
        private int getSingleCurPageIndex(int doubleCurPageIndex)
        {
            if (doubleCurPageIndex == 0)
            {
                doubleCurPageIndex = 0;
            }
            else if (doubleCurPageIndex == (_FlowDocumentDouble.Blocks.Count - 1))
            {
                doubleCurPageIndex = (_FlowDocument.Blocks.Count - 1);
            }
            else
            {
                doubleCurPageIndex = doubleCurPageIndex * 2;
            }

            return doubleCurPageIndex;
        }

        //單頁頁碼轉換雙頁頁碼
        private int getDoubleCurPageIndex(int singleCurPageIndex)
        {
            if (singleCurPageIndex == 0)
            {
                singleCurPageIndex = 0;
            }
            else if (singleCurPageIndex == (_FlowDocument.Blocks.Count - 1))
            {
                singleCurPageIndex = (_FlowDocumentDouble.Blocks.Count - 1);
            }
            else
            {
                if (singleCurPageIndex % 2 == 1)
                {
                    singleCurPageIndex = (singleCurPageIndex + 1) / 2;
                }
                else
                {
                    singleCurPageIndex = singleCurPageIndex / 2;
                }
            }
            return singleCurPageIndex;
        }

        //準備基本資料: ImageStatus, ReadPagePair, ThumbnailImageAndPage, FlowDocument
        private bool prepareReadingPageDataSource()
        {
            if (hejMetadata != null)
            {
                this._FlowDocumentDouble = new FlowDocument();
                this._FlowDocument = new FlowDocument();


                //初始化單頁所需的小圖資料
                singleThumbnailImageAndPageList = new List<ThumbnailImageAndPage>();
                singleImgStatus = new List<ImageStatus>();
                singleReadPagePair = new Dictionary<int, ReadPagePair>();

                for (int i = 0; i < hejMetadata.SImgList.Count; i++)
                {
                    try
                    {
                        string pagePath = bookPath + "\\" + hejMetadata.SImgList[i].path;
                        if (hejMetadata.SImgList[i].path.Contains("tryPageEnd")) //試閱書的最後一頁
                            pagePath = hejMetadata.SImgList[i].path;

                        setFlowDocumentData(hejMetadata.LImgList[i].pageNum, pagePath, "", singleThumbnailImageAndPageList,
                                         singleImgStatus, _FlowDocument);


                        string largePagePath = bookPath + "\\" + hejMetadata.LImgList[i].path;
                        if (hejMetadata.LImgList[i].path.Contains("tryPageEnd")) //試閱書的最後一頁
                            largePagePath = hejMetadata.LImgList[i].path;

                        ReadPagePair rpp = new ReadPagePair(i, -1, largePagePath, "", hejMetadata.LImgList[i].pageId, "", PDFdpi);
                        if (!singleReadPagePair.ContainsKey(i))
                        {
                            singleReadPagePair.Add(i, rpp);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception: {0}, From getHEJThumbnailAndPage, Single", ex);
                    }
                }

                //初始化雙頁所需的小圖資料
                doubleThumbnailImageAndPageList = new List<ThumbnailImageAndPage>();
                doubleImgStatus = new List<ImageStatus>();
                doubleReadPagePair = new Dictionary<int, ReadPagePair>();

                string coverPath = "";
                string backCoverPath = "";

                for (int i = 0; i < hejMetadata.manifestItemList.Count; i++)
                {
                    //先找出封面和封底
                    if (hejMetadata.manifestItemList[i].id.Equals("cover")
                        || (hejMetadata.manifestItemList[i].id.Equals("backcover")))
                    {
                        try
                        {
                            if (hejMetadata.manifestItemList[i].href.StartsWith("thumbs/"))
                            {
                                hejMetadata.manifestItemList[i].href = hejMetadata.manifestItemList[i].href.Replace("thumbs/", "");
                            }

                            string coverPagePath = bookPath + "\\HYWEB\\thumbs\\" + hejMetadata.manifestItemList[i].href;

                            if (hejMetadata.manifestItemList[i].id.Equals("cover"))
                            {
                                //string pageId = "";
                                //for (int j = 0; j < hejMetadata.LImgList.Count; j++)
                                //{
                                //    //取pageId
                                //    if (hejMetadata.manifestItemList[i].href.StartsWith(hejMetadata.LImgList[j].pageId))
                                //    {
                                //        pageId = hejMetadata.LImgList[j].pageNum;
                                //        break;
                                //    }
                                //}
                                coverPath = coverPagePath;
                            }
                            else if (hejMetadata.manifestItemList[i].id.Equals("backcover"))
                            {
                                backCoverPath = coverPagePath;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Exception: {0}, From getHEJThumbnailAndPage, Double, Cover and BackCover", ex);
                        }
                    }
                    continue;
                }

                for (int i = 0; i < hejMetadata.SImgList.Count; i++)
                {
                    try
                    {
                        if ((bookPath + "\\" + hejMetadata.SImgList[i].path).Equals(coverPath))
                        {

                            setFlowDocumentData(hejMetadata.LImgList[i].pageNum, coverPath, "", doubleThumbnailImageAndPageList,
                                    doubleImgStatus, _FlowDocumentDouble);

                            string largePagePath = bookPath + "\\" + hejMetadata.LImgList[i].path;
                            if (hejMetadata.LImgList[i].path.Contains("tryPageEnd")) //試閱書的最後一頁
                                largePagePath = hejMetadata.LImgList[i].path;

                            ReadPagePair rpp = new ReadPagePair(0, -1, largePagePath, "", hejMetadata.LImgList[i].pageId, "", PDFdpi);
                            if (!doubleReadPagePair.ContainsKey(0))
                            {
                                doubleReadPagePair.Add(0, rpp);
                            }
                            continue;
                        }
                        else if ((bookPath + "\\" + hejMetadata.SImgList[i].path).Equals(backCoverPath))
                        {
                            setFlowDocumentData(hejMetadata.LImgList[i].pageNum, backCoverPath, "", doubleThumbnailImageAndPageList,
                                    doubleImgStatus, _FlowDocumentDouble);

                            int lastPageIndex = (int)((i + 1) / 2);

                            string largePagePath = bookPath + "\\" + hejMetadata.LImgList[i].path;
                            if (hejMetadata.LImgList[i].path.Contains("tryPageEnd")) //試閱書的最後一頁
                                largePagePath = hejMetadata.LImgList[i].path;

                            //ReadPagePair rpp = new ReadPagePair(lastPageIndex, -1, largePagePath, "", hejMetadata.LImgList[i].pageId, "", PDFdpi);
                            ReadPagePair rpp = new ReadPagePair(i, -1, largePagePath, "", hejMetadata.LImgList[i].pageId, "", PDFdpi);
                            if (!doubleReadPagePair.ContainsKey(lastPageIndex))
                            {
                                doubleReadPagePair.Add(lastPageIndex, rpp);
                            }
                            continue;
                        }

                        if (i % 2 == 1)
                        {
                            if ((i + 1) == hejMetadata.SImgList.Count)
                            {
                                string lastPagePath = bookPath + "\\" + hejMetadata.SImgList[i].path;
                                if (hejMetadata.SImgList[i].path.Contains("tryPageEnd")) //試閱書的最後一頁
                                    lastPagePath = hejMetadata.SImgList[i].path;

                                setFlowDocumentData(hejMetadata.LImgList[i].pageNum, lastPagePath, "", doubleThumbnailImageAndPageList,
                                    doubleImgStatus, _FlowDocumentDouble);

                                int lastPageIndex = (int)((i + 1) / 2);

                                string largePagePath = bookPath + "\\" + hejMetadata.LImgList[i].path;
                                if (hejMetadata.LImgList[i].path.Contains("tryPageEnd")) //試閱書的最後一頁
                                    largePagePath = hejMetadata.LImgList[i].path;

                                ReadPagePair rpp = new ReadPagePair(i, -1, largePagePath, "", hejMetadata.LImgList[i].pageId, "", PDFdpi);
                                if (!doubleReadPagePair.ContainsKey(lastPageIndex))
                                {
                                    doubleReadPagePair.Add(lastPageIndex, rpp);
                                }
                                break;
                            }
                            else
                            {
                                string leftPagePath = bookPath + "\\" + hejMetadata.SImgList[i].path;
                                string rightPagePath = bookPath + "\\" + hejMetadata.SImgList[i + 1].path;
                                if (hejMetadata.SImgList[i].path.Contains("tryPageEnd")) //試閱書的最後一頁
                                    leftPagePath = hejMetadata.SImgList[i].path;

                                if (hejMetadata.SImgList[i + 1].path.Contains("tryPageEnd")) //試閱書的最後一頁                                                                   
                                    rightPagePath = hejMetadata.SImgList[i + 1].path;

                                string pageIndex = hejMetadata.LImgList[i].pageNum + "-" + hejMetadata.LImgList[i + 1].pageNum;

                                int leftPageIndex = i;
                                int rightPageIndex = i + 1;
                                int doublePageIndex = (int)(rightPageIndex / 2);

                                if (hejMetadata.direction.Equals("right"))
                                {
                                    leftPagePath = bookPath + "\\" + hejMetadata.SImgList[i + 1].path;
                                    rightPagePath = bookPath + "\\" + hejMetadata.SImgList[i].path;
                                    if (hejMetadata.SImgList[i + 1].path.Contains("tryPageEnd")) //試閱書的最後一頁                                    
                                        leftPagePath = hejMetadata.SImgList[i + 1].path;

                                    if (hejMetadata.SImgList[i].path.Contains("tryPageEnd")) //試閱書的最後一頁                                    
                                        rightPagePath = hejMetadata.SImgList[i].path;


                                    pageIndex = hejMetadata.LImgList[i + 1].pageNum + "-" + hejMetadata.LImgList[i].pageNum;

                                    leftPageIndex = i + 1;
                                    rightPageIndex = i;
                                    doublePageIndex = (int)(leftPageIndex / 2);
                                }

                                setFlowDocumentData(pageIndex, leftPagePath, rightPagePath, doubleThumbnailImageAndPageList,
                                    doubleImgStatus, _FlowDocumentDouble);

                                string largeLeftPath = bookPath + "\\" + hejMetadata.LImgList[leftPageIndex].path;
                                if (hejMetadata.LImgList[leftPageIndex].path.Contains("tryPageEnd")) //試閱書的最後一頁
                                    largeLeftPath = hejMetadata.LImgList[leftPageIndex].path;

                                string largeRightPath = bookPath + "\\" + hejMetadata.LImgList[rightPageIndex].path;
                                if (hejMetadata.LImgList[rightPageIndex].path.Contains("tryPageEnd")) //試閱書的最後一頁
                                    largeRightPath = hejMetadata.LImgList[rightPageIndex].path;


                                ReadPagePair rpp = new ReadPagePair(leftPageIndex, rightPageIndex,
                                    largeLeftPath,
                                    largeRightPath,
                                    hejMetadata.LImgList[leftPageIndex].pageId, hejMetadata.LImgList[rightPageIndex].pageId, PDFdpi);

                                if (!doubleReadPagePair.ContainsKey(doublePageIndex))
                                {
                                    doubleReadPagePair.Add(doublePageIndex, rpp);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception: {0}, From getHEJThumbnailAndPage, Double", ex);
                    }
                }

                if (PaperLess_Emeeting.Properties.Settings.Default.IsFlatUIReader == true)
                {
                    thumbNailListBox.ItemsSource = singleThumbnailImageAndPageList;
                }
                else
                {
                    Task.Factory.StartNew(() =>
                    {
                        this.Dispatcher.BeginInvoke((Action)(() =>
                        {

                            Canvas.SetZIndex(thumnailCanvas, -10);
                            thumnailCanvas.Visibility = Visibility.Visible;
                            thumbNailListBox.ItemsSource = singleThumbnailImageAndPageList;
                            thumnailCanvas.Visibility = Visibility.Collapsed;
                            Canvas.SetZIndex(thumnailCanvas, 200);
                        }));
                    });
                }


                _FlowDocumentDouble.PagePadding = new Thickness(0);
                _FlowDocument.PagePadding = new Thickness(0);

                FR.FontSize = 12;
                FR.Zoom = FR.MaxZoom = FR.MinZoom = 500;

                //int pdfMode = configMng.savePdfPageMode;
                int pdfMode = lastPageMode;
                if (pdfMode.Equals(1))
                {
                    FR.Document = this._FlowDocument;
                }
                else if (pdfMode.Equals(2))
                {
                    FR.Document = this._FlowDocumentDouble;
                }

                GC.Collect();
            }
            return true;
        }

        //準備ImageStatus, ReadPagePair, ThumbnailImageAndPage, FlowDocument的圖以及資料
        private void setFlowDocumentData(string PageIndexShowed, string leftPagePath, string rightPagePath,
            List<ThumbnailImageAndPage> thumbnailImageAndPage, List<ImageStatus> imgStatus, FlowDocument flowDocumentImported)
        {
            ThumbnailImageAndPage tip = new ThumbnailImageAndPage(PageIndexShowed, rightPagePath, leftPagePath, false);
            thumbnailImageAndPage.Add(tip);

            imgStatus.Add(ImageStatus.SMALLIMAGE);

            System.Windows.Controls.Image leftThumbNailImage = getThumbnailImageToReplace(leftPagePath, new Thickness(offsetOfImage));
            System.Windows.Controls.Image rightThumbNailImage = null;

            if (!rightPagePath.Equals(""))
            {
                rightThumbNailImage = getThumbnailImageToReplace(rightPagePath, new Thickness(offsetOfImage));

                //寬版書, 或其中有一頁寬版
                //只需要在寬版書雙頁時才需計算margin
                if (rightThumbNailImage.Source.Width > rightThumbNailImage.Source.Height)
                {

                    double borderWidth = (SystemParameters.PrimaryScreenWidth - 16) / 2;

                    double ratio = borderWidth / rightThumbNailImage.Source.Width;

                    double height = rightThumbNailImage.Source.Height * ratio;

                    double borderHeight = SystemParameters.PrimaryScreenHeight - 110;

                    double rightMargin = (Math.Abs(height - borderHeight) / 2) / ratio / 2;

                    rightThumbNailImage.Margin = new Thickness(0, rightMargin, 0, rightMargin);



                }


                if (leftThumbNailImage.Source.Width > leftThumbNailImage.Source.Height)
                {
                    double borderWidth = (SystemParameters.PrimaryScreenWidth - 16) / 2;

                    double ratio = borderWidth / leftThumbNailImage.Source.Width;

                    double height = leftThumbNailImage.Source.Height * ratio;

                    double borderHeight = SystemParameters.PrimaryScreenHeight - 110;

                    double leftMargin = (Math.Abs(height - borderHeight) / 2) / ratio / 2;

                    leftThumbNailImage.Margin = new Thickness(0, leftMargin, 0, leftMargin);
                }
            }
            //wayne add 20141003 fix 1024*768 problem
            else if (System.Windows.SystemParameters.PrimaryScreenWidth <= 1280 && System.Windows.SystemParameters.PrimaryScreenHeight <= 768)
            {

                //imgStatus.RemoveAt(imgStatus.Count - 1);
                //imgStatus.Add(ImageStatus.LARGEIMAGE);
                //rightThumbNailImage = getThumbnailImageToReplace(leftPagePath, new Thickness(offsetOfImage));

                ////寬版書, 或其中有一頁寬版
                ////只需要在寬版書雙頁時才需計算margin
                //if (rightThumbNailImage.Source.Width > rightThumbNailImage.Source.Height)
                // {
                //wayne add 20141201

                //PDFdpi = 250;

                //    double borderWidth = (SystemParameters.PrimaryScreenWidth - 16) / 2;

                //    double ratio = borderWidth / rightThumbNailImage.Source.Width;

                //    double height = rightThumbNailImage.Source.Height * ratio;

                //    double borderHeight = SystemParameters.PrimaryScreenHeight - 110;

                //    double rightMargin = (Math.Abs(height - borderHeight) / 2) / ratio / 2;

                //    rightThumbNailImage.Margin = new Thickness(200, 200, 200, 200);
                //}

                //if (leftThumbNailImage.Source.Width > leftThumbNailImage.Source.Height)
                //{
                //double borderWidth2 = (SystemParameters.PrimaryScreenWidth - 16) / 2;

                //double ratio2 = borderWidth2 / leftThumbNailImage.Source.Width;

                //double height2 = leftThumbNailImage.Source.Height * ratio2;

                //double borderHeight2 = SystemParameters.PrimaryScreenHeight - 110;

                //double leftMargin = (Math.Abs(height2 - borderHeight2) / 2) / ratio2 / 2;

                //leftThumbNailImage.Margin = new Thickness(0, leftMargin, 0, leftMargin);
                //}
            }

            //if (System.Windows.SystemParameters.PrimaryScreenWidth <= 1280 && System.Windows.SystemParameters.PrimaryScreenHeight <= 768)
            if (System.Windows.SystemParameters.PrimaryScreenWidth <= 1280 && System.Windows.SystemParameters.PrimaryScreenHeight <= 1024)
            {

                try
                {

                    bool IsWidthPage = false;
                    if (rightThumbNailImage != null && rightThumbNailImage.Source.Width > rightThumbNailImage.Source.Height)
                        IsWidthPage = true;
                    if (leftThumbNailImage != null && leftThumbNailImage.Source.Width > leftThumbNailImage.Source.Height)
                        IsWidthPage = true;

                    if (IsWidthPage == true)
                    {

                        int PSH = (int)System.Windows.SystemParameters.PrimaryScreenHeight;

                        switch (PSH)
                        {
                            case 800:
                                PDFdpi = 250;
                                break;
                            case 960:
                                PDFdpi = 250;
                                break;
                            case 1024:
                                PDFdpi = 192;
                                break;
                            case 1440:
                                PDFdpi = 320;
                                break;
                            default:
                                PDFdpi = 250;
                                break;
                        }

                    }

                }
                catch (Exception ex)
                {
                    LogTool.Debug(ex);
                }
            }

            StackPanel sp = setStackPanelWithThumbnailImage(leftThumbNailImage, rightThumbNailImage);

            BlockUIContainer bc = new BlockUIContainer(sp);
            flowDocumentImported.Blocks.Add(bc);
            bc = null;
            leftThumbNailImage = null;
            rightThumbNailImage = null;
            sp = null;
            tip = null;
        }

        //將雙頁的圖放到StackPanel中
        private StackPanel setStackPanelWithThumbnailImage(System.Windows.Controls.Image leftThumbNailImage, System.Windows.Controls.Image rightThumbNailImage)
        {
            StackPanel sp = new StackPanel();

            sp.Children.Add(leftThumbNailImage);

            if (rightThumbNailImage != null)
            {
                sp.Children.Add(rightThumbNailImage);

                if (hejMetadata.direction.Equals("right"))
                {
                    sp.FlowDirection = FlowDirection.LeftToRight;
                }
            }

            sp.Orientation = Orientation.Horizontal;
            sp.HorizontalAlignment = HorizontalAlignment.Center;
            sp.VerticalAlignment = VerticalAlignment.Center;
            sp.RenderTransform = tfgForImage;
            sp.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            sp.MouseLeftButtonDown += ImageInReader_MouseLeftButtonDown;
            return sp;
        }

        private bool useOriginalCanvasOnLockStatus = false;     //控制在鎖定頁面時Preload頁的Canvas大小

        //將Ren好的大圖放到Canvas中
        //Wayne mark important
        double newImageWidth = 0;
        double newImageHeight = 0;
        private void SendImageSourceToZoomCanvas(BitmapImage newImage)
        {
            newImageWidth = newImage.Width;
            newImageHeight = newImage.Height;

            // wayne mark
            Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");
            zoomCanvas.RenderTransform = tfgForHyperLink;

            if (bookType.Equals(BookType.HEJ))
            {
                double currentImageShowHeight = 0;
                double currentImageShowWidth = 0;

                if (newImage.Width / 2 < newImage.Height)
                {
                    Border bd = GetBorderInReader();
                    currentImageShowHeight = bd.ActualHeight;
                    currentImageShowWidth = 0;

                    currentImageShowWidth = newImage.PixelWidth * bd.ActualHeight / newImage.PixelHeight;
                }
                else if (newImage.Width / 2 > newImage.Height)
                {
                    //雙頁寬版書
                    Border bd = GetBorderInReader();

                    currentImageShowHeight = 0;
                    currentImageShowWidth = bd.ActualWidth;

                    currentImageShowHeight = newImage.PixelHeight * bd.ActualWidth / newImage.PixelWidth;
                }

                zoomCanvas.Height = currentImageShowHeight;
                zoomCanvas.Width = currentImageShowWidth;
            }
            else if (bookType.Equals(BookType.PHEJ))
            {
                //第一次鎖定的狀態下, 不用換算倍率
                if (zoomStep == 0)
                {
                    zoomCanvas.Height = newImage.PixelHeight / zoomStepScale[zoomStep] * 96 / DpiY;
                    zoomCanvas.Width = newImage.PixelWidth / zoomStepScale[zoomStep] * 96 / DpiX;



                }
                else
                {
                    if (useOriginalCanvasOnLockStatus)
                    {
                        zoomCanvas.Height = newImage.PixelHeight * 96 / DpiY;
                        zoomCanvas.Width = newImage.PixelWidth * 96 / DpiX;
                    }
                    else
                    {
                        zoomCanvas.Height = newImage.PixelHeight / zoomStepScale[zoomStep] * 96 / DpiY;
                        zoomCanvas.Width = newImage.PixelWidth / zoomStepScale[zoomStep] * 96 / DpiX;
                    }

                }

                //wayne 20141201修正低檢析度問題
                //if (System.Windows.SystemParameters.PrimaryScreenWidth <= 1280 && System.Windows.SystemParameters.PrimaryScreenHeight <= 1024)
                if (System.Windows.SystemParameters.PrimaryScreenWidth <= 1280 && System.Windows.SystemParameters.PrimaryScreenHeight <= 1024)
                {

                    //寬版書
                    if (newImage.Width > newImage.Height)
                    {
                        IsSmallDisplaySize = true;
                        zoomCanvas.SnapsToDevicePixels = true;
                        //zoomCanvas.Height = System.Windows.SystemParameters.PrimaryScreenHeight - 30;
                        zoomCanvas.Height = System.Windows.SystemParameters.PrimaryScreenHeight;
                        zoomCanvas.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
                    }

                }

            }




            //To-do: 將Canvas換成Image, 使圖片更清楚
            //System.Windows.Controls.Image img = FindVisualChildByName<System.Windows.Controls.Image>(FR, "zoomImage");
            //img.Width = zoomCanvas.Width;
            //img.Height = zoomCanvas.Height;
            //img.Source = newImage;
            //img.Stretch = Stretch.Uniform;

            //img.RenderTransform = tfgForHyperLink;

            //wayne add 20141003 fix 1024*768 problem
            //if (zoomCanvas.Height <= 361)
            //    zoomCanvas.Height = zoomCanvas.Height * 2;

            //if (zoomCanvas.Width <= 512)
            //    zoomCanvas.Width = zoomCanvas.Width * 2;

            //Canvas zoomCanvasFront = FindVisualChildByName<Canvas>(FR, "zoomCanvasFront");

            //wayne add 20141003 fix 1024*768 problem
            if (System.Windows.SystemParameters.PrimaryScreenHeight <= 768 && System.Windows.SystemParameters.PrimaryScreenWidth <= 1024)
            {
                //zoomCanvasBackground.Visibility = Visibility.Collapsed;
            }
            else
            {

            }

            ImageBrush ib = new ImageBrush();
            ib.ImageSource = newImage;

            // Wayne Edit 20150415 
            //ib.AlignmentX = AlignmentX.Left;
            //ib.AlignmentY = AlignmentY.Top;

            // Wayne Edit 20150415 
            ib.AlignmentX = AlignmentX.Center;
            ib.AlignmentY = AlignmentY.Center;
            ib.Stretch = Stretch.Uniform;



            //wayne 20141201修正低檢析度問題
            //if (System.Windows.SystemParameters.PrimaryScreenWidth <= 1280 && System.Windows.SystemParameters.PrimaryScreenHeight <= 768)
            if (System.Windows.SystemParameters.PrimaryScreenWidth <= 1280 && System.Windows.SystemParameters.PrimaryScreenHeight <= 1024)
            {

                //寬版書
                if (newImage.Width > newImage.Height)
                {
                    //ib.Stretch = Stretch.None;
                }
            }
            ib.Freeze();



            // wayne marked 
            // 1024 * 768 
            zoomCanvas.Background = ib;



        }

        private System.Windows.Controls.Image getThumbnailImageToReplace(string pagePath, Thickness margin)
        {
            System.Windows.Controls.Image thumbNailImageSingle = new System.Windows.Controls.Image();
            BitmapImage bi = new BitmapImage(new Uri(pagePath));
            thumbNailImageSingle.Source = bi;
            thumbNailImageSingle.Stretch = Stretch.Uniform;
            thumbNailImageSingle.Margin = margin;

            bi = null;
            return thumbNailImageSingle;
        }

        private byte[] getByteArrayFromImage(BitmapImage imageC)
        {
            byte[] data;
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            if (imageC.UriSource != null)
            {
                encoder.Frames.Add(BitmapFrame.Create(imageC.UriSource));
            }
            else
            {
                encoder.Frames.Add(BitmapFrame.Create(imageC));
            }

            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                data = ms.ToArray();
                ms.Close();
                ms.Dispose();
                encoder = null;
                imageC = null;
                GC.Collect();
                return data;
            }
        }


        #endregion

        #region Ren圖核心部分


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
                    RadioButton NoteRB = FindVisualChildByName<RadioButton>(FR, "NoteButton");
                    if (NoteRB != null)
                    {
                        doUpperRadioButtonClicked(MediaCanvasOpenedBy.NoteButton, NoteRB);
                        Canvas MediaTableCanvas = GetMediaTableCanvasInReader();
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

            //try
            //{
            //    noteButton_Click();
            //    //var item = thumbNailListBox.SelectedItem as ThumbnailImageAndPage;
            //    //if (item != null)
            //    //{

            //    //    TextBox tb = FindVisualChildByName<TextBox>(FR, "notePanel");
            //    //    tb.Text = bookNoteDictionary[int.Parse(item.pageIndex)].text;
            //    //}
            //}
            //catch(Exception ex)
            //{
            //    LogTool.Debug(ex);
            //}

            //取得點選的縮圖對應的單頁index
            int tempIndex = 0;
            if (NoteButtonInLBIsClicked || BookMarkInLBIsClicked)
            {
                thumbNailListBox.Focus();
                object tempItem = thumbNailListBox.SelectedItem;
                tempIndex = singleThumbnailImageAndPageList.IndexOf((ThumbnailImageAndPage)thumbNailListBox.SelectedItem);
            }
            else
            {
                tempIndex = thumbNailListBox.SelectedIndex;
            }

            //跳至該頁
            if (viewStatusIndex.Equals(PageMode.SinglePage))
            {

                bringBlockIntoView(tempIndex);
            }
            else if (viewStatusIndex.Equals(PageMode.DoublePage))
            {
                int index = tempIndex;
                if (index % 2 == 1)
                {
                    index = index + 1;
                }

                int leftCurPageIndex = index - 1;
                int rightCurPageIndex = index;
                if (hejMetadata.direction.Equals("right"))
                {
                    leftCurPageIndex = index;
                    rightCurPageIndex = index - 1;
                }

                bringBlockIntoView(index / 2);
            }

            //如果非第一次進入程式
            if (isFirstTimeLoaded)
            {
                // Wayne 下面的Code不要動它
                // 通通改成鎖定狀態就好
                isLockButtonLocked = true;

                //非鎖定, 還原一倍大小
                if (!isLockButtonLocked)
                {
                    zoomStep = 0;
                    PDFScale = (float)zoomStepScale[0];
                    resetTransform();
                    LockButton.Visibility = Visibility.Collapsed;
                }
                else //鎖定, 維持原狀
                {
                    if (tempIndex.Equals(0) || tempIndex.Equals(thumbNailListBox.Items.Count - 1))
                    {
                        //第一及最後一頁X軸要變為中心點
                        setTransformBetweenSingleAndDoublePage();
                    }
                    LockButton.Visibility = Visibility.Visible;
                }
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
                    if (hejMetadata.direction.Equals("right"))
                    {
                        ScrollViewer sv = FindVisualChildByName<ScrollViewer>(thumbNailListBox, "SVInLV");
                        sv.ScrollToRightEnd();
                        if ((tempIndex + 1) * listBoxItem.ActualWidth > this.ActualWidth / 2)
                        {
                            double scrollOffset = sv.ScrollableWidth - (tempIndex + 1) * listBoxItem.ActualWidth + this.ActualWidth / 2;
                            sv.ScrollToHorizontalOffset(scrollOffset);
                        }
                    }
                    else
                    {
                        if ((tempIndex + 1) * listBoxItem.ActualWidth > this.ActualWidth / 2)
                        {
                            ScrollViewer sv = FindVisualChildByName<ScrollViewer>(thumbNailListBox, "SVInLV");
                            double scrollOffset = (tempIndex + 1) * listBoxItem.ActualWidth - this.ActualWidth / 2;
                            sv.ScrollToHorizontalOffset(scrollOffset);
                        }
                    }
                }

                //恢復可點選狀態
                thumbNailListBox.SelectedIndex = -1;

                resetFocusBackToReader();
            }

            ShowFilterCount();
        }


        private bool isAreaButtonAndPenMemoRequestSent = false;
        private bool isPDFRendering = false;
        private int checkImageStatusRetryTimes = 0;
        private int checkImageStatusMaxRetryTimes = 5;

        //檢查圖片的狀態, 小圖的話Ren成大圖, 大圖的話, Preload前後兩頁
        private void checkImageStatus(object sender, EventArgs e)
        {
            //if (System.Windows.SystemParameters.PrimaryScreenWidth <= 1280)
            //{
            //    //wayne add 20141128
            //    return;
            //}
            bool singlePagesMode = false;
            bool doublePagesMode = false;
            if (viewStatusIndex.Equals(PageMode.SinglePage))
            {
                singlePagesMode = true;
            }
            else if (viewStatusIndex.Equals(PageMode.DoublePage))
            {
                doublePagesMode = true;
            }
            else
            {
                return;
            }

            try
            {
                DateTime eventOccurTime = DateTime.Now;
                double millisecondsAfterLastChangingPage = eventOccurTime.Subtract(lastTimeOfChangingPage).TotalMilliseconds;

                //為避免快速翻頁, 小於0.3秒的不執行
                if (millisecondsAfterLastChangingPage >= 300)
                {

                    if (checkImageStatusTimer.Interval != checkInterval)
                    {
                        checkImageStatusTimer.Interval = checkInterval;
                    }

                    //檢查是否要放大
                    if (!zoomeThread.Count.Equals(0) && !isPDFRendering)
                    {
                        for (int i = zoomeThread.Count - 1; i >= 0; i--)
                        {
                            if (PDFScale.Equals(((float)Convert.ToDouble(zoomeThread[i].Name))))
                            {
                                try
                                {
                                    if (singlePagesMode)
                                    {
                                        //單頁模式
                                        singleImgStatus[curPageIndex] = ImageStatus.LARGEIMAGE;

                                    }
                                    else if (doublePagesMode)
                                    {
                                        doubleImgStatus[curPageIndex] = ImageStatus.LARGEIMAGE;
                                    }
                                    zoomeThread[i].Start();
                                    this.imageSourceRendered += ReadWindow_imageSourceRendered;
                                    isPDFRendering = true;
                                    return;
                                    //break;
                                }
                                catch
                                {
                                    //該Thread執行中, 抓下一個Thread測試
                                    continue;
                                }
                            }
                        }
                    }

                    byte[] curKey = defaultKey;
                    if (doublePagesMode)
                    {
                        try
                        {
                            if (doubleImgStatus[curPageIndex] == ImageStatus.LARGEIMAGE && isAreaButtonAndPenMemoRequestSent)
                            {
                                //if (checkImageStatusRetryTimes > checkImageStatusMaxRetryTimes)
                                //{
                                //    if (checkImageStatusTimer.IsEnabled)
                                //    {
                                //        checkImageStatusTimer.IsEnabled = false;
                                //        checkImageStatusTimer.Stop();
                                //    }
                                //}

                                //checkImageStatusRetryTimes++;
                                return;
                            }

                            int doubleIndex = curPageIndex;

                            ReadPagePair item = doubleReadPagePair[curPageIndex];

                            if (item.rightPageIndex == -1)
                            {
                                //封面或封底
                                if (File.Exists(item.leftImagePath))
                                {
                                    resetDoublePage();
                                    isAreaButtonAndPenMemoRequestSent = true;
                                }
                            }
                            else
                            {
                                //雙頁
                                if (File.Exists(item.leftImagePath) && File.Exists(item.rightImagePath))
                                {
                                    resetDoublePage();
                                    isAreaButtonAndPenMemoRequestSent = true;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            //ren圖時發生錯誤
                            Debug.WriteLine("exception@doublePagesMode:" + ex.ToString());
                        }
                    }
                    else if (singlePagesMode)
                    {
                        try
                        {
                            if (singleImgStatus[curPageIndex] == ImageStatus.LARGEIMAGE && isAreaButtonAndPenMemoRequestSent)
                            {
                                //if (checkImageStatusRetryTimes > checkImageStatusMaxRetryTimes)
                                //{
                                //    if (checkImageStatusTimer.IsEnabled)
                                //    {
                                //        checkImageStatusTimer.IsEnabled = false;
                                //        checkImageStatusTimer.Stop();
                                //    }
                                //}

                                checkImageStatusRetryTimes++;
                                return;
                            }

                            ReadPagePair item = singleReadPagePair[curPageIndex];

                            if (File.Exists(item.leftImagePath))
                            {
                                resetSinglePage();
                                isAreaButtonAndPenMemoRequestSent = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            //ren圖時發生錯誤
                            Debug.WriteLine("exception@doublePagesMode:" + ex.ToString());
                        }
                    }
                    curKey = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("exception@checkImageStatus:" + ex.ToString());
            }
        }


        //20141128大圖小圖非常重要。
        //處理單頁資料
        private void resetSinglePage()
        {
            int myIndex = curPageIndex;

            //先處理本頁及前後需轉成大圖的
            //產生優先處理的index (本頁->下頁->前頁)
            List<int> processSequence = new List<int>();
            if (singleImgStatus[myIndex] != ImageStatus.LARGEIMAGE && singleImgStatus[myIndex] != ImageStatus.GENERATING)
            {
                processSequence.Add(myIndex);
            }

            //判斷是否需要Preload
            if (needPreload)
            {

                if (myIndex + 1 < singleReadPagePair.Count)
                {
                    if (singleImgStatus[myIndex + 1] != ImageStatus.LARGEIMAGE && singleImgStatus[myIndex + 1] != ImageStatus.GENERATING)
                    {
                        processSequence.Add(myIndex + 1);
                    }
                }
                if (myIndex - 1 > 0)
                {
                    if (singleImgStatus[myIndex - 1] != ImageStatus.LARGEIMAGE && singleImgStatus[myIndex - 1] != ImageStatus.GENERATING)
                    {
                        processSequence.Add(myIndex - 1);
                    }
                }
            }

            //if (processSequence.Count.Equals(0))
            //{
            //    //皆為大圖不做事
            //    return;
            //}

            for (int i = 0; i < processSequence.Count; i++)
            {
                if (myIndex != curPageIndex)
                {   //如果本method還沒處理完，flipview已經換頁，就不需再處理了
                    return;
                }
                ReadPagePair item = singleReadPagePair[processSequence[i]];
                //Global.downloadScheduler.jumpToBookPage(UniID, item.leftPageId);

                if (item.leftImageSource != null)
                {
                    //之前已經有ren過
                    continue;
                }

                if (item.leftImagePath != "")
                {
                    //int retryTimes = 0;
                    //while (true)
                    //{
                    if (myIndex != curPageIndex)
                    {
                        //如果本method還沒處理完已經換頁，就不需再處理了
                        return;
                    }
                    try
                    {
                        Debug.WriteLine("為大圖載入{0}", item.leftImagePath);
                        if (item.leftImagePath.Contains("tryPageEnd") || item.rightImagePath.Contains("tryPageEnd"))
                        {
                            //當中有一頁為試讀頁的最後一頁
                            Debug.WriteLine("@resetSinglePage, check tryPageEnd");
                            singleImgStatus[processSequence[i]] = ImageStatus.LARGEIMAGE;
                            return;
                        }
                        else
                        {
                            if (bookType.Equals(BookType.PHEJ))
                            {
                                item.createLargePHEJBitmapImage(caTool, defaultKey, GetBorderInReader(), true);
                            }
                            else if (bookType.Equals(BookType.HEJ))
                            {
                                item.createLargeHEJBitmapImage(caTool, defaultKey);
                            }

                            Debug.WriteLine("@resetSinglePage, !item.isRendering");
                            singleImgStatus[processSequence[i]] = ImageStatus.GENERATING;

                            if (File.Exists(item.leftImagePath))
                            {
                                if (!item.isRendering)
                                {
                                    if (bookType.Equals(BookType.PHEJ))
                                    {
                                        item.createLargePHEJBitmapImage(caTool, defaultKey, GetBorderInReader(), true);
                                    }
                                    else if (bookType.Equals(BookType.HEJ))
                                    {
                                        item.createLargeHEJBitmapImage(caTool, defaultKey);
                                    }

                                    Debug.WriteLine("@resetSinglePage, !item.isRendering");
                                    singleImgStatus[processSequence[i]] = ImageStatus.GENERATING;
                                    continue;
                                }
                            }
                            else
                            {
                                //此檔案不在
                                //List<string> filesNeedToBeDownloadedImmediately = new List<string>();
                                //if (!imagePathExists) filesNeedToBeDownloadedImmediately.Add(imagePath.Substring(imagePath.LastIndexOf("\\") + 1));

                                //foreach (string fileNeedToBeDownloadNow in filesNeedToBeDownloadedImmediately)
                                //{

                                //    Debug.WriteLine(" **** download " + fileNeedToBeDownloadNow + " immediately!!!");
                                //}
                                //bookManager.downloadManager.jumpToBookFiles(bookId, account, filesNeedToBeDownloadedImmediately);
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //未知錯誤, 不往下進行, 不重ren
                        Debug.WriteLine("@resetSinglePage" + ex.Message);
                        return;
                    }
                }
            }

            //放小圖或由大圖變小圖
            int totalPortraitPageCount = singleReadPagePair.Count;
            for (int i = 0; i < totalPortraitPageCount; i++)
            {
                if (myIndex != curPageIndex)
                {   //如果本method還沒處理完已經換頁，就不需再處理了
                    return;
                }

                //判斷是否需要Preload
                if (needPreload)
                {
                    if (Math.Abs(myIndex - i) <= 1) //本頁及前後頁在前面已經處理
                    {
                        continue;
                    }
                }
                else
                {
                    if (myIndex == i) //本頁已經處理
                    {
                        continue;
                    }
                }
                ReadPagePair item = singleReadPagePair[i];


                if (singleImgStatus[i] == ImageStatus.GENERATING || singleImgStatus[i] == ImageStatus.LARGEIMAGE)  //本頁未載入過，或現在是大圖
                {
                    if (item.leftImageSource != null)
                    {
                        item.leftImageSource = null;
                        item.decodedPDFPages = new byte[2][];
                        singleImgStatus[i] = ImageStatus.SMALLIMAGE;
                        continue;
                    }
                }
            }


            if (myIndex != curPageIndex)
            {   //如果本method還沒處理完已經換頁，就不需再處理了
                return;
            }

            // Wayne marked 20141003
            ReadPagePair curItem = singleReadPagePair[curPageIndex];
            Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");
            //while (true)
            //{
            if (myIndex != curPageIndex)
            {   //如果本method還沒處理完已經換頁，就不需再處理了
                return;
            }


            if (curItem.leftImageSource != null && !curItem.isRendering)
            {
                try
                {
                    this.baseScale = curItem.baseScale;
                    //翻頁時已經將source放入
                    if (zoomCanvas.Background == null)
                    {
                        SendImageSourceToZoomCanvas((BitmapImage)curItem.leftImageSource);
                        singleImgStatus[curPageIndex] = ImageStatus.LARGEIMAGE;
                        Debug.WriteLine("SendImageSourceToZoomCanvas@resetSinglePage");
                    }
                }
                catch (Exception ex)
                {
                    curItem.leftImageSource = null;
                    Debug.WriteLine(ex.Message.ToString());
                    return;
                }
                if (zoomCanvas.Background != null)
                {
                    //做出感應框和螢光筆
                    if (canAreaButtonBeSeen)
                    {
                        CheckAndProduceAreaButton(curItem.leftPageIndex, -1, defaultKey, zoomCanvas);
                    }
                    loadCurrentStrokes(hejMetadata.LImgList[curItem.leftPageIndex].pageId);
                    loadCurrentStrokes(singleReadPagePair[curPageIndex].leftPageIndex);
                    GC.Collect();
                }
                //break;
            }
            else
            {
                //wayne add
                try
                {
                    //take off
                    //if (isSyncOwner == false && isSyncing == false)
                    loadCurrentStrokes(singleReadPagePair[curPageIndex].leftPageIndex);
                }
                catch (Exception ex)
                {
                    LogTool.Debug(ex);
                }
            }
            //}
        }

        //處理雙頁資料
        private void resetDoublePage()
        {
            int myIndex = curPageIndex;

            //先處理本頁及前後需轉成大圖的
            //產生優先處理的index (本頁->下頁->前頁)
            List<int> processSequence = new List<int>();
            if (doubleImgStatus[myIndex] != ImageStatus.LARGEIMAGE && doubleImgStatus[myIndex] != ImageStatus.GENERATING)
            {
                processSequence.Add(myIndex);
            }

            //判斷是否需要Preload
            if (needPreload)
            {
                if (myIndex + 1 < doubleReadPagePair.Count)
                {
                    if (doubleImgStatus[myIndex + 1] != ImageStatus.LARGEIMAGE && doubleImgStatus[myIndex + 1] != ImageStatus.GENERATING)
                    {
                        processSequence.Add(myIndex + 1);
                    }
                }
                if (myIndex - 1 > 0)
                {
                    if (doubleImgStatus[myIndex - 1] != ImageStatus.LARGEIMAGE && doubleImgStatus[myIndex - 1] != ImageStatus.GENERATING)
                    {
                        processSequence.Add(myIndex - 1);
                    }
                }
            }

            //if (processSequence.Count.Equals(0))
            //{
            //    //皆為大圖不做事
            //    return;
            //}

            for (int i = 0; i < processSequence.Count; i++)
            {
                if (myIndex != curPageIndex)
                {   //如果本method還沒處理完，flipview已經換頁，就不需再處理了
                    return;
                }
                ReadPagePair item = doubleReadPagePair[processSequence[i]];
                //Global.downloadScheduler.jumpToBookPage(UniID, item.leftPageId);

                if (item.leftImageSource != null)
                {
                    continue;
                }

                if (item.leftImagePath != "")
                {
                    //while (true)
                    //{
                    if (myIndex != curPageIndex)
                    {
                        //如果本method還沒處理完已經換頁，就不需再處理了
                        return;
                    }
                    try
                    {
                        if (item.leftImagePath.Contains("tryPageEnd") || item.rightImagePath.Contains("tryPageEnd"))
                        {
                            //當中有一頁為試讀頁的最後一頁
                            Debug.WriteLine("@resetDoublePage, check tryPageEnd @ second time");
                            doubleImgStatus[processSequence[i]] = ImageStatus.LARGEIMAGE;
                            return;
                        }
                        else
                        {
                            Debug.WriteLine("為大圖載入{0}", item.leftImagePath);
                            if (File.Exists(item.leftImagePath))
                            {
                                if (!item.isRendering)
                                {
                                    if (bookType.Equals(BookType.PHEJ))
                                    {
                                        item.createLargePHEJBitmapImage(caTool, defaultKey, GetBorderInReader(), false);
                                    }
                                    else if (bookType.Equals(BookType.HEJ))
                                    {
                                        item.createLargeHEJBitmapImage(caTool, defaultKey);
                                    }
                                    Debug.WriteLine("@resetDoublePage, !item.isRendering");
                                    doubleImgStatus[processSequence[i]] = ImageStatus.GENERATING;
                                    continue;
                                }
                            }
                            else
                            {
                                //封面或封底沒有檔案
                                //List<string> filesNeedToBeDownloadedImmediately = new List<string>();
                                //filesNeedToBeDownloadedImmediately.Add(imagePath.Substring(imagePath.LastIndexOf("\\") + 1));
                                //Debug.WriteLine(" **** download " + imagePath.Substring(imagePath.LastIndexOf("\\") + 1) + " immediately!!!");
                                //bookManager.downloadManager.jumpToBookFiles(bookId, account, filesNeedToBeDownloadedImmediately);


                                //其他有檔案尚未下載好
                                //List<string> filesNeedToBeDownloadedImmediately = new List<string>();
                                //if (!leftImageExists) filesNeedToBeDownloadedImmediately.Add(leftImagePath.Substring(leftImagePath.LastIndexOf("\\") + 1));
                                //if (!rightImageExists) filesNeedToBeDownloadedImmediately.Add(rightImagePath.Substring(rightImagePath.LastIndexOf("\\") + 1));
                                //foreach (string fileNeedToBeDownloadNow in filesNeedToBeDownloadedImmediately)
                                //{

                                //    Debug.WriteLine(" **** download " + fileNeedToBeDownloadNow + " immediately!!!");
                                //}
                                //bookManager.downloadManager.jumpToBookFiles(bookId, account, filesNeedToBeDownloadedImmediately);
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //未知錯誤, 不往下進行, 不重ren
                        Debug.WriteLine("@resetDoublePage" + ex.Message);
                        return;
                    }
                }
            }

            //if (doubleImgStatus[myIndex] != ImageStatus.LARGEIMAGE)
            //{
            //    //如果到這裡還沒有變大圖, 代表檔案未準備好, 留給下次處理
            //    return;
            //}

            //放小圖或由大圖變小圖
            int totalPortraitPageCount = doubleReadPagePair.Count;
            for (int i = 0; i < totalPortraitPageCount; i++)
            {
                if (myIndex != curPageIndex)
                {   //如果本method還沒處理完已經換頁，就不需再處理了
                    return;
                }

                //判斷是否需要Preload
                if (needPreload)
                {
                    if (Math.Abs(myIndex - i) <= 1) //本頁及前後頁在前面已經處理
                    {
                        continue;
                    }
                }
                else
                {
                    if (myIndex == i) //本頁在前面已經處理
                    {
                        continue;
                    }
                }

                ReadPagePair item = doubleReadPagePair[i];

                if (doubleImgStatus[i] == ImageStatus.GENERATING || doubleImgStatus[i] == ImageStatus.LARGEIMAGE)  //本頁未載入過，或現在是大圖
                {
                    item.leftImageSource = null;
                    item.decodedPDFPages = new byte[2][];
                    doubleImgStatus[i] = ImageStatus.SMALLIMAGE;
                    continue;
                }
            }

            //送Thread前再檢查一次
            if (myIndex != curPageIndex)
            {
                //如果本method還沒處理完已經換頁，就不需再處理了
                return;
            }

            ReadPagePair curItem = doubleReadPagePair[curPageIndex];

            Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");
            //while (true)
            //{
            if (myIndex != curPageIndex)
            {   //如果本method還沒處理完已經換頁，就不需再處理了
                return;
            }

            if (curItem.leftImageSource != null && !curItem.isRendering)
            {
                try
                {
                    this.baseScale = curItem.baseScale;
                    //翻頁時已經將source放入
                    if (zoomCanvas.Background == null)
                    {
                        SendImageSourceToZoomCanvas((BitmapImage)curItem.leftImageSource);
                        doubleImgStatus[curPageIndex] = ImageStatus.LARGEIMAGE;
                        Debug.WriteLine("SendImageSourceToZoomCanvas@resetDoublePage");
                    }
                }
                catch (Exception ex)
                {
                    curItem.leftImageSource = null;
                    Debug.WriteLine(ex.Message.ToString());
                    return;
                }
                if (zoomCanvas.Background != null)
                {
                    //做出感應框和螢光筆
                    if (curItem.rightPageIndex == -1)
                    {
                        //封面或封底或單頁
                        if (canAreaButtonBeSeen)
                        {
                            //Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");
                            CheckAndProduceAreaButton(curItem.leftPageIndex, -1, defaultKey, zoomCanvas);
                        }
                        loadCurrentStrokes(hejMetadata.LImgList[curItem.leftPageIndex].pageId);
                        loadCurrentStrokes(singleReadPagePair[curPageIndex].leftPageIndex);
                    }
                    else
                    {
                        //雙頁
                        if (canAreaButtonBeSeen)
                        {
                            CheckAndProduceAreaButton(curItem.leftPageIndex, curItem.rightPageIndex, defaultKey, zoomCanvas);
                        }
                        loadDoublePagesStrokes(hejMetadata.LImgList[curItem.leftPageIndex].pageId, hejMetadata.LImgList[curItem.rightPageIndex].pageId);
                        loadDoublePagesStrokes(curItem.leftPageIndex, curItem.rightPageIndex);
                    }
                    GC.Collect();
                }
                //break;
            }
            //}
        }

        private bool ifAskedJumpPage = false;
        private bool isFirstTimeChangingPage = false;

        internal delegate void LoadingPageHandler(int pageIndex);
        internal static LoadingPageHandler LoadingEvent;

        private void checkOtherDevicePage()
        {
            if (!ifAskedJumpPage)
            {
                if (lastViewPage.Count > 0)
                {
                    LastPageData thislastPage = null;
                    if (lastViewPage.ContainsKey(CName))
                    {
                        thislastPage = lastViewPage[CName];
                    }

                    foreach (KeyValuePair<string, LastPageData> lastPage in lastViewPage)
                    {
                        if (lastPage.Key != CName)
                        {
                            LastPageData blp = lastViewPage[lastPage.Key];
                            bool showLastPage = false;

                            if (thislastPage != null && thislastPage.updatetime < blp.updatetime)
                            {
                                showLastPage = true;
                            }
                            else if (thislastPage == null)
                            {
                                showLastPage = true;
                            }

                            if (showLastPage)
                            {
                                string messageText = String.Format("您最近一次於 {0} 閱讀到第 {1} 頁。是否要跳到該頁？", lastPage.Key, (lastPage.Value.index + 1));


                                if (blp.index == curPageIndex)
                                {
                                    ifAskedJumpPage = true;
                                    return;
                                }

                                MessageBoxResult msgResult = MessageBox.Show(messageText, "", MessageBoxButton.YesNo);
                                if (msgResult.Equals(MessageBoxResult.Yes))
                                {
                                    int targetPageIndex = -1;
                                    if (blp.index > 0)
                                    {
                                        targetPageIndex = blp.index;

                                        LoadingEvent = testLoading;

                                        IAsyncResult result = null;

                                        AsyncCallback initCompleted = delegate(IAsyncResult ar)
                                        {
                                            LoadingEvent.EndInvoke(result);

                                            //Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Invoker)delegate
                                            this.Dispatcher.BeginInvoke((Invoker)delegate
                                            {
                                                Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");
                                                zoomCanvas.Background = null;
                                                int pdfMode = lastPageMode;
                                                if (pdfMode.Equals(1))
                                                {
                                                    //單頁
                                                    bringBlockIntoView(targetPageIndex);
                                                }
                                                else if (pdfMode.Equals(2))
                                                {
                                                    //雙頁
                                                    int doubleLastViewPage = getDoubleCurPageIndex(targetPageIndex);
                                                    bringBlockIntoView(doubleLastViewPage);
                                                }
                                            });
                                        };

                                        result = LoadingEvent.BeginInvoke(targetPageIndex, initCompleted, null);
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
                ifAskedJumpPage = true;
            }
        }

        void testLoading(int Pageindex)
        { }

        private void showLastReadPageAndStartPreload()
        {
            //第一頁, 直接取大圖
            if (viewStatusIndex.Equals(PageMode.SinglePage))
            {
                resetSinglePage();
            }
            else if (viewStatusIndex.Equals(PageMode.DoublePage))
            {
                resetDoublePage();
            }

            //顯示完第一頁後才preload
            if (!isWindowsXP)
            {
                needPreload = true;
            }

            //第一頁
            if (!checkImageStatusTimer.IsEnabled)
            {
                checkImageStatusTimer.IsEnabled = true;
                checkImageStatusTimer.Start();
            }
        }

        private IInputElement pageViewerPager;

        //取得PageViewer
        void FR_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Debug.WriteLine("FR_PreviewLostKeyboardFocus newFocus: {0}, oldFocus:{1}", e.NewFocus.ToString(), e.OldFocus.ToString());

            if (e.OldFocus is FlowDocumentReader)
            {
                pageViewerPager = e.NewFocus;

                FR.PreviewLostKeyboardFocus -= FR_PreviewLostKeyboardFocus;
                e.Handled = true;
            }
        }

        #endregion

        #region 螢光筆處理

        private double originalCanvasWidth = 1;
        private double originalCanvasHeight = 1;
        private double fullScreenCanvasWidth = 1;
        private double fullScreenCanvasHeight = 1;
        private double baseStrokesCanvasWidth = 0;
        private double baseStrokesCanvasHeight = 0;

        private string StatusFileName = "originalPenmemoStatus.xml";

        private void saveOriginalStrokeStatus(double originalCanvasWidth, double originalCanvasHeight)
        {
            try
            {
                if (!File.Exists(bookPath + "\\hyweb\\strokes\\" + StatusFileName))
                {
                    FileStream fs = new FileStream(bookPath + "\\hyweb\\strokes\\" + StatusFileName, FileMode.Create);

                    XmlWriter w = XmlWriter.Create(fs);

                    w.WriteStartDocument();
                    w.WriteStartElement("status");
                    w.WriteElementString("originalCanvasWidth", originalCanvasWidth.ToString());
                    w.WriteElementString("originalCanvasHeight", originalCanvasHeight.ToString());
                    w.WriteEndElement();

                    w.WriteEndDocument();
                    w.Flush();
                    fs.Close();
                }
            }
            catch
            {
            }
        }

        private void loadOriginalStrokeStatus()
        {
            try
            {
                if (File.Exists(bookPath + "\\hyweb\\strokes\\" + StatusFileName))
                {
                    FileStream fs = new FileStream(bookPath + "\\hyweb\\strokes\\" + StatusFileName, FileMode.Open);

                    XmlReader r = XmlReader.Create(fs);

                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(r);

                    foreach (XmlNode nodes in xmlDoc.ChildNodes)
                    {
                        if (nodes.Name.Equals("status"))
                        {
                            foreach (XmlNode node in nodes.ChildNodes)
                            {
                                if (node.Name.Equals("originalCanvasWidth"))
                                {
                                    baseStrokesCanvasWidth = Convert.ToDouble(node.InnerText);
                                }
                                else if (node.Name.Equals("originalCanvasHeight"))
                                {
                                    baseStrokesCanvasHeight = Convert.ToDouble(node.InnerText);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void loadDoublePagesStrokes(string LeftImgID, string RightImgID)
        {
            Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");
            if (zoomCanvas.Width.Equals(Double.NaN) || zoomCanvas.Height.Equals(Double.NaN))
            {
                //大圖尚未初始化, 不做事
                return;
            }

            double offsetX = zoomCanvas.Width / 2;

            StrokeCollection strokeCollection = new StrokeCollection();
            StrokeCollection leftStrokes = new StrokeCollection();
            StrokeCollection rightStrokes = new StrokeCollection();
            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");

            if (!isFullScreenButtonClick)
            {
                originalCanvasWidth = zoomCanvas.Width;
                originalCanvasHeight = zoomCanvas.Height;
            }
            else
            {
                fullScreenCanvasWidth = zoomCanvas.Width;
                fullScreenCanvasHeight = zoomCanvas.Height;
            }

            penMemoCanvas.Width = zoomCanvas.Width;
            penMemoCanvas.Height = zoomCanvas.Height;
            penMemoCanvas.RenderTransform = tfgForHyperLink;

            if (File.Exists(bookPath + "/hyweb/strokes/" + LeftImgID + ".isf"))
            {
                FileStream fs = new FileStream(bookPath + "/hyweb/strokes/" + LeftImgID + ".isf",
                                      FileMode.Open);
                if (fs.Length > 0)
                {
                    leftStrokes = new StrokeCollection(fs);
                }
                fs.Close();
            }
            if (File.Exists(bookPath + "/hyweb/strokes/" + RightImgID + ".isf"))
            {
                FileStream fs = new FileStream(bookPath + "/hyweb/strokes/" + RightImgID + ".isf", FileMode.Open);

                rightStrokes = new StrokeCollection(fs);
                fs.Close();
            }

            if (leftStrokes.Count > 0)
            {
                System.Windows.Media.Matrix moveMatrix = new System.Windows.Media.Matrix(1, 0, 0, 1, 0, 0);
                if (!baseStrokesCanvasHeight.Equals(0) && !baseStrokesCanvasWidth.Equals(0))
                {
                    if (originalCanvasHeight != baseStrokesCanvasHeight || (originalCanvasWidth / 2) != baseStrokesCanvasWidth)
                    {
                        double ratioX = (originalCanvasWidth / 2) / baseStrokesCanvasWidth;
                        double ratioY = originalCanvasHeight / baseStrokesCanvasHeight;

                        moveMatrix.Scale(ratioX, ratioY);
                    }
                }

                if (isFullScreenButtonClick)
                {
                    double ratioX = (fullScreenCanvasWidth / 2) / (originalCanvasWidth / 2);
                    double ratioY = fullScreenCanvasHeight / originalCanvasHeight;
                    moveMatrix.Scale(ratioX, ratioY);
                }
                leftStrokes.Transform(moveMatrix, false);
                strokeCollection.Add(leftStrokes);
            }

            if (rightStrokes.Count > 0)
            {
                System.Windows.Media.Matrix moveMatrix = new System.Windows.Media.Matrix(1, 0, 0, 1, offsetX, 0);
                if (!baseStrokesCanvasHeight.Equals(0) && !baseStrokesCanvasWidth.Equals(0))
                {
                    if (originalCanvasHeight != baseStrokesCanvasHeight || (originalCanvasWidth / 2) != baseStrokesCanvasWidth)
                    {
                        double ratioX = (originalCanvasWidth / 2) / baseStrokesCanvasWidth;
                        double ratioY = originalCanvasHeight / baseStrokesCanvasHeight;
                        moveMatrix.OffsetX /= ratioX;
                        moveMatrix.Scale(ratioX, ratioY);
                    }
                }

                if (isFullScreenButtonClick)
                {
                    double ratioX = (fullScreenCanvasWidth / 2) / (originalCanvasWidth / 2);
                    double ratioY = fullScreenCanvasHeight / originalCanvasHeight;
                    moveMatrix.OffsetX /= ratioX;
                    moveMatrix.Scale(ratioX, ratioY);
                }
                rightStrokes.Transform(moveMatrix, false);
                strokeCollection.Add(rightStrokes);
            }

            penMemoCanvas.Strokes = strokeCollection;
        }

        private void loadCurrentStrokes(String imageID)
        {
            Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");
            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");

            if (zoomCanvas.Width.Equals(Double.NaN) || zoomCanvas.Height.Equals(Double.NaN))
            {
                //大圖尚未初始化, 不做事
                return;
            }

            if (!isFullScreenButtonClick)
            {
                originalCanvasWidth = zoomCanvas.Width;
                originalCanvasHeight = zoomCanvas.Height;
            }
            else
            {

                fullScreenCanvasWidth = zoomCanvas.Width;
                fullScreenCanvasHeight = zoomCanvas.Height;
            }

            penMemoCanvas.Width = zoomCanvas.Width;
            penMemoCanvas.Height = zoomCanvas.Height;
            penMemoCanvas.RenderTransform = tfgForHyperLink;

            //讀取local檔案
            StrokeCollection strokeCollection = new StrokeCollection();
            if (File.Exists(bookPath + "\\hyweb\\strokes\\" + imageID + ".isf"))
            {
                FileStream fs = new FileStream(bookPath + "\\hyweb\\strokes\\" + imageID + ".isf",
                                      FileMode.Open);
                if (fs.Length > 0)
                {
                    strokeCollection = new StrokeCollection(fs);
                }
                fs.Close();
            }

            if (strokeCollection.Count > 0)
            {
                System.Windows.Media.Matrix moveMatrix = new System.Windows.Media.Matrix(1, 0, 0, 1, 0, 0);
                if (!baseStrokesCanvasHeight.Equals(0) && !baseStrokesCanvasWidth.Equals(0))
                {
                    if (originalCanvasHeight != baseStrokesCanvasHeight || originalCanvasWidth != baseStrokesCanvasWidth)
                    {
                        double ratioX = originalCanvasWidth / baseStrokesCanvasWidth;
                        double ratioY = originalCanvasHeight / baseStrokesCanvasHeight;

                        moveMatrix.Scale(ratioX, ratioY);
                    }
                }

                if (isFullScreenButtonClick)
                {
                    double ratioX = fullScreenCanvasWidth / originalCanvasWidth;
                    double ratioY = fullScreenCanvasHeight / originalCanvasHeight;

                    moveMatrix.Scale(ratioX, ratioY);
                }
                strokeCollection.Transform(moveMatrix, false);

                penMemoCanvas.Strokes = strokeCollection;
            }

        }

        private void loadDoublePagesStrokes(int leftIndex, int rightIndex)
        {
            Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");

            if (zoomCanvas.Width.Equals(Double.NaN) || zoomCanvas.Height.Equals(Double.NaN))
            {
                //大圖尚未初始化, 不做事
                return;
            }

            double offsetX = zoomCanvas.Width / 2;

            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");


            penMemoCanvas.Width = zoomCanvas.Width;
            penMemoCanvas.Height = zoomCanvas.Height;
            penMemoCanvas.RenderTransform = tfgForHyperLink;

            //由資料庫取回註記
            bookStrokesDictionary = bookManager.getStrokesDics(userBookSno);

            //從DB取資料
            //isFirstLoad = true;
            if (bookStrokesDictionary.ContainsKey(leftIndex))
            {
                List<StrokesData> curPageStrokes = bookStrokesDictionary[leftIndex];
                int strokesCount = curPageStrokes.Count;
                for (int i = 0; i < strokesCount; i++)
                {
                    if (curPageStrokes[i].status == "0")
                    {
                        paintStrokeOnInkCanvas(curPageStrokes[i], zoomCanvas.Width / 2, zoomCanvas.Height, 0, 0);
                    }
                }
            }
            //isFirstLoad = false;

            //isFirstLoad = true;
            //從DB取資料
            if (bookStrokesDictionary.ContainsKey(rightIndex))
            {
                List<StrokesData> curPageStrokes = bookStrokesDictionary[rightIndex];
                int strokesCount = curPageStrokes.Count;
                for (int i = 0; i < strokesCount; i++)
                {
                    if (curPageStrokes[i].status == "0")
                    {
                        paintStrokeOnInkCanvas(curPageStrokes[i], zoomCanvas.Width / 2, zoomCanvas.Height, offsetX, 0);
                    }
                }
            }
            //isFirstLoad = false;
        }

        private void paintStrokeOnInkCanvas(StrokesData strokeJson, double currentInkcanvasWidth, double currentInkcanvasHeight, double offsetX, double offsetY)
        {

            try
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
                    //sp.X = (p.X * widthScaleRatio) == Double.NaN ? 1 : (p.X * widthScaleRatio);
                    //sp.Y = (p.Y * heightScaleRatio) == Double.NaN ? 1 : (p.Y * heightScaleRatio);
                    sp.X = p.X * widthScaleRatio;
                    sp.Y = p.Y * heightScaleRatio;

                    pointsList.Add(sp);
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
                targetStroke.DrawingAttributes.Width = strokeWidth * 3;
                targetStroke.DrawingAttributes.Height = strokeWidth * 3;
                System.Windows.Media.ColorConverter ccr = new System.Windows.Media.ColorConverter();
                System.Windows.Media.Color clr = ConvertHexStringToColour(strokeColor);
                targetStroke.DrawingAttributes.Color = clr;

                System.Windows.Media.Matrix moveMatrix = new System.Windows.Media.Matrix(1, 0, 0, 1, offsetX, 0);
                if (targetStroke != null)
                {
                    //把解好的螢光筆畫到inkcanvas上
                    InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
                    targetStroke.Transform(moveMatrix, false);
                    penMemoCanvas.Strokes.Add(targetStroke.Clone());
                    targetStroke = null;
                }
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }

        }

        //由於改成將螢光筆資料存在DB中, 故這個function改為將原來檔案中的資料存到DB, 並將原檔案殺掉
        private void convertCurrentStrokesToDB(string imageID)
        {
            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");

            //如果檔案存在的話將原來檔案中的資料存到DB, 並將原檔案殺掉
            if (File.Exists(bookPath + "\\hyweb\\strokes\\" + imageID + ".isf"))
            {
                if (penMemoCanvas.Strokes.Count > 0)
                {

                    DateTime dt = new DateTime(1970, 1, 1);

                    //server上的儲存時間的格式是second, Ticks單一刻度表示千萬分之一秒

                    long currentTime = DateTime.Now.ToUniversalTime().Subtract(dt).Ticks / 10000000;

                    StrokeCollection strokeCollection = penMemoCanvas.Strokes;

                    System.Windows.Media.Matrix moveMatrix = new System.Windows.Media.Matrix(1, 0, 0, 1, 0, 0);
                    if (!baseStrokesCanvasHeight.Equals(0) && !baseStrokesCanvasWidth.Equals(0))
                    {
                        if (originalCanvasHeight != baseStrokesCanvasHeight || originalCanvasWidth != baseStrokesCanvasWidth)
                        {
                            //DPI不同時的處理
                            double ratioX = baseStrokesCanvasWidth / originalCanvasWidth;
                            double ratioY = baseStrokesCanvasHeight / originalCanvasHeight;

                            moveMatrix.Scale(ratioX, ratioY);
                        }
                    }

                    if (isFullScreenButtonClick)
                    {
                        //全螢幕->換回原本倍率
                        double ratioX = originalCanvasWidth / fullScreenCanvasWidth;
                        double ratioY = originalCanvasHeight / fullScreenCanvasHeight;

                        moveMatrix.Scale(ratioX, ratioY);
                    }
                    strokeCollection.Transform(moveMatrix, false);

                    List<string> batchCmds = new List<string>();

                    int strokesCount = penMemoCanvas.Strokes.Count;
                    for (int i = 0; i < strokesCount; i++)
                    {

                        int pointCount = penMemoCanvas.Strokes[i].StylusPoints.Count;
                        DrawingAttributes d = penMemoCanvas.Strokes[i].DrawingAttributes;

                        string colorString = d.Color.ToString();
                        colorString = colorString.Remove(1, 2);

                        string pointsMsg = "";
                        for (int j = 0; j < pointCount; j++)
                        {
                            StylusPoint stylusPoint = penMemoCanvas.Strokes[i].StylusPoints[j];
                            pointsMsg += "{" + stylusPoint.X.ToString() + ", " + stylusPoint.Y.ToString() + "};";
                        }

                        pointsMsg = pointsMsg.Substring(0, pointsMsg.LastIndexOf(';'));

                        StrokesData sd = new StrokesData();
                        sd.objectId = "";
                        sd.alpha = (float)(d.IsHighlighter ? 0.5 : 1);
                        sd.bookid = "";
                        sd.canvasheight = (float)penMemoCanvas.Height;
                        sd.canvaswidth = (float)penMemoCanvas.Width;
                        sd.color = colorString;
                        sd.createtime = currentTime;
                        sd.index = curPageIndex;
                        sd.points = pointsMsg;
                        sd.status = "0";
                        sd.synctime = 0;
                        sd.updatetime = currentTime;
                        sd.userid = "";
                        sd.vendor = "";
                        sd.width = (float)d.Height;

                        //存DB, 重複的話就不存了
                        string cmd = bookManager.insertStrokeCmdString(userBookSno, sd);
                        if (!batchCmds.Contains(cmd))
                            batchCmds.Add(cmd);

                        //bookManager.saveStrokesData(userBookSno, false, sd);

                        //bookManager.saveStrokesData(userBookSno, curPageIndex, false, "", currentTime, currentTime, 0
                        //    , null, penMemoCanvas.Height, penMemoCanvas.Width, (d.IsHighlighter ? 0.5 : 1), pointsMsg, colorString, d.Height);
                    }

                    if (batchCmds.Count > 0)
                        bookManager.saveBatchData(batchCmds);

                }
                try
                {
                    System.IO.File.Delete(bookPath + "\\hyweb\\strokes\\" + imageID + ".isf");
                }
                catch (System.IO.IOException e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }
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
                bool isSamePoint = compareStrokeInDB(thisStroke, curPageStrokes[j], penMemoCanvas.Width, penMemoCanvas.Height);

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


                InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
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
                sd.canvasheight = (float)penMemoCanvas.Height;
                sd.canvaswidth = (float)penMemoCanvas.Width;
                sd.color = colorString;
                sd.createtime = currentTime + i;
                sd.index = curPageIndex;
                sd.points = pointsMsg;
                sd.status = "0";
                sd.synctime = 0;
                sd.updatetime = currentTime + i;
                sd.userid = account;
                sd.vendor = vendorId;
                sd.width = (float)d.Height;

                bookManager.saveStrokesData(userBookSno, false, sd);
                //bookManager.saveStrokesData(userBookSno, curPageIndex, false, "", currentTime, currentTime, 0
                //    , "0", penMemoCanvas.Width, penMemoCanvas.Height, (d.IsHighlighter ? 0.5 : 1), pointsMsg, colorString, d.Height);
            }
        }

        public void strokeChaneEventHandler(DrawingAttributes d)
        {
            //Console.WriteLine(type+":"+value);
            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
            penMemoCanvas.DefaultDrawingAttributes = d;
        }

        public void strokeUndoEventHandler()
        {
            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");

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
            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
            while (tempStrokes.Count > 0)
            {
                penMemoCanvas.Strokes.Add(tempStrokes[tempStrokes.Count - 1]);
                tempStrokes.RemoveAt(tempStrokes.Count - 1);
            }
        }

        public void strokeEraseEventHandler()
        {
            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
            penMemoCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
        }

        public void strokeLineEventHandler()
        {
            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
            penMemoCanvas.EditingMode = InkCanvasEditingMode.None;
            penMemoCanvas.MouseLeftButtonDown += inkCanvas1_MouseDown;
            //penMemoCanvas.MouseDown += inkCanvas1_MouseDown;
            penMemoCanvas.MouseUp += inkCanvas1_MouseUp;
            penMemoCanvas.MouseMove += inkCanvas1_MouseMove;
            isStrokeLine = true;
        }

        public void strokeCurveEventHandler()
        {
            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
            penMemoCanvas.MouseDown -= inkCanvas1_MouseDown;
            penMemoCanvas.MouseUp -= inkCanvas1_MouseUp;
            penMemoCanvas.MouseMove -= inkCanvas1_MouseMove;
            penMemoCanvas.EditingMode = InkCanvasEditingMode.Ink;
            isStrokeLine = false;
        }

        private void inkCanvas1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
            if (penMemoCanvas.EditingMode == InkCanvasEditingMode.None)
            {
                stylusPC = new StylusPointCollection();

                System.Windows.Point p = e.GetPosition(penMemoCanvas);

                stylusPC.Add(new StylusPoint(p.X, p.Y));

            }
        }

        private void inkCanvas1_MouseMove(object sender, MouseEventArgs e)
        {
            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
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
            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
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

            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
            StackPanel mediaListPanel = GetMediaListPanelInReader();
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

            //


        }

        public void alterPenmemoAnimation(StrokeToolPanelHorizontal_Reader toolPanel, double f, double t)
        {

            DoubleAnimation a = new DoubleAnimation();
            a.From = f;
            a.To = t;
            a.Duration = new Duration(TimeSpan.FromSeconds(0.3));

            toolPanel.BeginAnimation(StrokeToolPanelHorizontal_Reader.WidthProperty, a);
        }

        public void showPenToolPanelEventHandler(bool isCanvasShowed)
        {
            Canvas popupControlCanvas = FindVisualChildByName<Canvas>(FR, "PopupControlCanvas");

            if (isCanvasShowed)
            {
                //PopupControlCanvas open
                Canvas.SetZIndex(popupControlCanvas, 901);
                if (popupControlCanvas.Visibility.Equals(Visibility.Collapsed))
                {
                    popupControlCanvas.Visibility = Visibility.Visible;
                }
            }
            else
            {
                //PopupControlCanvas close
                Canvas.SetZIndex(popupControlCanvas, 899);
                if (popupControlCanvas.Visibility.Equals(Visibility.Visible))
                {
                    popupControlCanvas.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void PopupControlCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Canvas popupControlCanvas = FindVisualChildByName<Canvas>(FR, "PopupControlCanvas");
            Canvas.SetZIndex(popupControlCanvas, 899);
            if (popupControlCanvas.Visibility.Equals(Visibility.Visible))
            {
                popupControlCanvas.Visibility = Visibility.Collapsed;
            }

            Grid penMemoToolBar = FindVisualChildByName<Grid>(FR, "PenMemoToolBar");
            StrokeToolPanelHorizontal_Reader strokeToolPanelHorizontal = (StrokeToolPanelHorizontal_Reader)penMemoToolBar.Children[penMemoToolBar.Children.Count - 1];
            strokeToolPanelHorizontal.closePopup();
        }

        void penMemoCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // throw new NotImplementedException();
            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
            StrokeCollection strokeCol = penMemoCanvas.GetSelectedStrokes();
            if (strokeCol.Count > 0)
            {
                penMemoCanvas.Strokes.Remove(strokeCol);
            }

        }

        public void strokeDelAllEventHandler()
        {
            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");

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

        #endregion

        #region 感應框

        private void CheckAndProduceAreaButton(int leftCurPageIndex, int rightCurPageIndex, byte[] curKey, UIElement ImageCanvas)
        {
            if (pageInfoManager == null)
            {
                canAreaButtonBeSeen = false;
                return;
            }

            Border bd = GetBorderInReader();

            Canvas zoomCanvas = (Canvas)ImageCanvas;

            double currentCanvasShowHeight = zoomCanvas.Height;
            double currentCanvasShowWidth = zoomCanvas.Width;

            double currentImageShowHeight = 0;
            double currentImageShowWidth = 0;

            //if (zoomCanvas.Background != null)
            //{
            try
            {
                currentImageShowHeight = ((ImageBrush)(zoomCanvas.Background)).ImageSource.Height;
                currentImageShowWidth = ((ImageBrush)(zoomCanvas.Background)).ImageSource.Width;
            }
            catch
            {
                //圖片ren不出來, 有錯誤則return
                return;
            }
            //}
            setCanvasSizeAndProduceAreaButton(leftCurPageIndex, rightCurPageIndex, curKey, currentCanvasShowWidth, currentCanvasShowHeight, currentImageShowWidth, currentImageShowHeight);
        }

        private void setCanvasSizeAndProduceAreaButton(int leftCurPageIndex, int rightCurPageIndex, byte[] curKey,
            double currentCanvasShowWidth, double currentCanvasShowHeight, double currentImageShowWidth, double currentImageShowHeight)
        {

            //double currentImageShowHeight = currentCanvasShowHeight;
            //double currentImageShowWidth = currentCanvasShowWidth;

            double currentRatio = currentCanvasShowHeight / currentImageShowHeight;

            if (!rightCurPageIndex.Equals(-1))
            {
                //雙頁
                //currentImageShowHeight = currentCanvasShowHeight;
                currentImageShowWidth = currentImageShowWidth / 2;

                currentRatio = currentCanvasShowHeight / currentImageShowHeight;

                double offsetX = currentImageShowWidth * currentRatio;
                double offsetY = 0;
                ProduceAreaButton(rightCurPageIndex, curKey, currentCanvasShowWidth, currentCanvasShowHeight, currentRatio, offsetX, offsetY, currentImageShowHeight, currentImageShowWidth);
                //currentPageWidthForStrokes = currentImageShowWidth;
            }

            //單頁
            ProduceAreaButton(leftCurPageIndex, curKey, currentCanvasShowWidth, currentCanvasShowHeight, currentRatio, 0, 0, currentImageShowHeight, currentImageShowWidth);
            //currentPageWidthForStrokes = currentCanvasShowWidth;
        }

        private void ProduceAreaButton(int pageIndex, byte[] curKey, double currentCanvasShowWidth, double currentCanvasShowHeight, double currentRatio, double offsetX, double offsetY, double currentImageShowHeight, double currentImageShowWidth)
        {
            if (pageInfoManager.HyperLinkAreaDictionary.ContainsKey(hejMetadata.LImgList[pageIndex].pageId))
            {
                Canvas stageCanvas = GetStageCanvasInReader();
                pageInfo = pageInfoManager.getHyperLinkAreasByPageId(hejMetadata.LImgList[pageIndex].pageId, curKey);
                //stageCanvas.Background = System.Windows.Media.Brushes.Transparent;
                //stageCanvas.MouseLeftButtonDown += ImageInReader_MouseLeftButtonDown;
                stageCanvas.RenderTransform = tfgForHyperLink;

                stageCanvas.Height = currentCanvasShowHeight;
                stageCanvas.Width = currentCanvasShowWidth;

                double currentImageOriginalHeight = currentImageShowHeight;
                double currentImageOriginalWidth = currentImageShowWidth;

                List<HyperLinkArea> HyperLinkAreas = pageInfo.hyperLinkAreas;
                if (pageInfo.refHeight != 0 && pageInfo.refWidth != 0)
                {
                    currentImageOriginalHeight = pageInfo.refHeight;
                    currentImageOriginalWidth = pageInfo.refWidth;
                    currentRatio = currentCanvasShowHeight / currentImageOriginalHeight;
                    if (!offsetX.Equals(0))
                    {
                        offsetX = currentImageOriginalWidth * currentRatio;
                    }
                }

                for (int i = 0; i < HyperLinkAreas.Count; i++)
                {
                    if (!(HyperLinkAreas[i].itemRef.Count.Equals(0) && HyperLinkAreas[i].items.Count.Equals(0)))
                        createHyperLinkButton(HyperLinkAreas[i], hejMetadata.LImgList[pageIndex].pageId, stageCanvas, pageInfo, currentRatio, offsetX, offsetY);
                }
            }
        }

        private void createHyperLinkButton(HyperLinkArea hyperLinkAreas, string pageID, Canvas canvas, PageInfoMetadata pageInfo, double currentRatio, double offsetX, double offsetY)
        {
            string areaID = hyperLinkAreas.areaId;

            if (areaID.StartsWith("FullText"))
            {
                //全文
                RadioButton rb = FindVisualChildByName<RadioButton>(FR, "FullTextButton");
                rb.Visibility = Visibility.Visible;
                rb.Uid = areaID;
                rb.Tag = pageInfo;
            }
            else
            {
                float startX = hyperLinkAreas.startX;
                float startY = hyperLinkAreas.startY;
                float endX = hyperLinkAreas.endX;
                float endY = hyperLinkAreas.endY;

                Button areaButton = new Button();
                areaButton.Style = (Style)FindResource("AreaButtonStyle");


                //HyperLinkArea area = pageInfoManager.getHyperLinkArea(pageID, areaID);
                //switch (a.items[0].mediaType)
                //{
                //    case "image/jpeg":
                //        button2.Visibility = Visibility.Collapsed;
                //        break;
                //    case "video/mp4":
                //        button2.Style = (Style)Application.Current.Resources["videohyperlinkButtonStyle"];
                //        break;
                //    case "audio/mpeg":
                //        button2.Style = (Style)Application.Current.Resources["audiohyperlinkButtonStyle"];
                //        break;
                //    case "application/x-url":
                //        button2.Style = (Style)Application.Current.Resources["linkhyperlinkButtonStyle"];
                //        break;
                //    case "application/hsd":
                //        button2.Style = (Style)Application.Current.Resources["slideshowhyperlinkButtonStyle"];
                //        break;
                //    case "text/html":
                //        button2.Style = (Style)Application.Current.Resources["fullTexthyperlinkButtonStyle"];
                //        break;
                //    default:
                //        //something else
                //        break;
                //}

                canvas.Children.Add(areaButton);

                //areaButton.Opacity = 0.3;

                double aW = Math.Ceiling((endX - startX) * currentRatio);
                double aH = Math.Ceiling((endY - startY) * currentRatio);
                double aX = Math.Floor(startX * currentRatio);
                double aY = Math.Floor((startY) * currentRatio);

                areaButton.Width = aW;
                areaButton.Height = aH;
                areaButton.Uid = areaID;
                areaButton.Tag = pageID;
                areaButton.Click += button1_Click;

                Canvas.SetTop(areaButton, aY + offsetY);
                Canvas.SetLeft(areaButton, aX + offsetX);

                //有感應圖示
                if (hyperLinkAreas.shape.Equals("icon"))
                {
                    string imagePath = bookPath + "\\HYWEB\\" + hyperLinkAreas.imagePath.Replace("/", "\\");
                    areaButton.Background = new ImageBrush(new BitmapImage(new Uri(imagePath)));

                }
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            string uid = ((Button)sender).Uid;
            string tag = (string)((Button)sender).Tag;

            HyperLinkArea areaButton = pageInfoManager.getHyperLinkArea(tag, uid);
            int targetPageIndex = 0;
            if (areaButton != null)
            {
                if (areaButton.items.Count > 0)
                {
                    targetPageIndex = getPageIndexByItemId(areaButton.items[0].id);
                }
            }
            else
            {
                //全文
                areaButton = pageInfoManager.getHyperLinkAreaForFullText(tag, uid);
                targetPageIndex = getPageIndexByItemId(areaButton.items[0].id);
            }

            string sourcePath = bookPath + "\\HYWEB\\" + areaButton.items[0].href.Replace("/", "\\");

            doMedia(areaButton.items[0].mediaType, sourcePath, targetPageIndex);

        }

        private void doMedia(string mediaType, string sourcePath, int targetPageIndex)
        {
            //if (!File.Exists(sourcePath) && !mediaType.Equals("application/x-url"))
            //{
            //    //媒體尚未下載完
            //    //MessageBox.Show("檔案未下載完畢，請稍後再試", "未下載完畢", MessageBoxButton.OK);
            //    MessageBox.Show(langMng.getLangString("fileNotDownloadedPls"), langMng.getLangString("yetDownloadComplete"), MessageBoxButton.OK);
            //    return;
            //}

            //switch (mediaType)
            //{
            //    case "image/jpeg":
            //        if (targetPageIndex != -1)
            //        {
            //            bringBlockIntoView(targetPageIndex);
            //        }
            //        break;
            //    case "image/png":
            //        Window window = new Window();
            //        BitmapImage img = getHEJSingleBitmapImage(caTool, defaultKey, sourcePath, PDFScale);
            //        window.Width = img.PixelWidth;
            //        window.Height = img.PixelHeight;
            //        window.Background = new ImageBrush(img);
            //        window.Show();
            //        break;
            //    case "application/pdf":
            //        if (targetPageIndex != -1)
            //        {
            //            bringBlockIntoView(targetPageIndex);
            //        }
            //        break;
            //    case "video/mp4":
            //        string videoFilePath = sourcePath;
            //        MoviePlayer m = new MoviePlayer(videoFilePath, true);
            //        m.ShowDialog();

            //        break;
            //    case "application/hsd":
            //        //string slideShowFilePath = sourcePath;
            //        slideShow ss = new slideShow(this.configMng);
            //        ss.hsdFile = sourcePath;
            //        ss.ShowDialog();

            //        break;
            //    case "application/x-url":
            //        sourcePath = sourcePath.Replace(bookPath + "\\HYWEB\\", "");
            //        sourcePath = sourcePath.Replace("\\", "/");
            //        Process.Start(new ProcessStartInfo(sourcePath));
            //        break;
            //    case "audio/mpeg":
            //        //需要改為
            //        string audioFilePath = sourcePath;
            //        AudioPlayer s = new AudioPlayer(audioFilePath, ObservableMediaList, false);
            //        s.Show();
            //        s.Topmost = true;

            //        //s.ShowDialog();  


            //        break;
            //    case "text/html":
            //        //string textFilePath = sourcePath;  
            //        //Stream htmlStream = caTool.fileAESDecode(textFilePath, false);
            //        //string htmlString = "";
            //        //using (var reader = new StreamReader(htmlStream, Encoding.Default))
            //        //{
            //        //    htmlString = reader.ReadToEnd();
            //        //}
            //        //fullTextView fv = new fullTextView();
            //        //fv.htmlString = htmlString;
            //        //fv.ShowDialog();
            //        showFullText(sourcePath);
            //        break;
            //    case "text/plain":
            //        //string textPlainFilePath = sourcePath;
            //        //Stream htmlPlainStream = caTool.fileAESDecode(textPlainFilePath, false);
            //        //string htmlPlainString = "";
            //        //using (var reader = new StreamReader(htmlPlainStream, Encoding.Default))
            //        //{
            //        //    htmlPlainString = reader.ReadToEnd();
            //        //}
            //        //fullTextView fvPlain = new fullTextView();
            //        //fvPlain.htmlString = htmlPlainString;
            //        //fvPlain.ShowDialog();
            //        showFullText(sourcePath);
            //        break;
            //    default:
            //        //something else
            //        break;
            //}
        }

        private void showFullText(string sourcePath)
        {
            //Stream htmlStream = caTool.fileAESDecode(sourcePath, false);
            //byte[] bytes;

            //using (MemoryStream ms = new MemoryStream())
            //{
            //    htmlStream.CopyTo(ms);
            //    bytes = ms.ToArray();
            //}
            //Encoding big5 = Encoding.GetEncoding(950);
            ////將byte[]轉為string再轉回byte[]看位元數是否有變
            //Encoding encode = (bytes.Length == big5.GetByteCount(big5.GetString(bytes))) ? Encoding.Default : Encoding.UTF8;
            //htmlStream.Position = 0;
            //StreamReader reader = new StreamReader(htmlStream, encode);
            //fullTextView fv = new fullTextView(configMng);
            //fv.htmlString = reader.ReadToEnd();
            //reader.Close();
            //fv.ShowDialog();
        }


        private int getPageIndexByItemId(string id)
        {
            if (viewStatusIndex.Equals(PageMode.SinglePage))
            {
                for (int i = 0; i < hejMetadata.SImgList.Count; i++)
                {
                    if (hejMetadata.SImgList[i].pageId == id)
                    {
                        return i;
                    }
                }
            }
            else if (viewStatusIndex.Equals(PageMode.DoublePage))
            {
                for (int i = 0; i < hejMetadata.SImgList.Count; i++)
                {
                    if (hejMetadata.SImgList[i].pageId == id)
                    {
                        int doubleCurPage = 0;
                        doubleCurPage = getDoubleCurPageIndex(i);
                        return doubleCurPage;
                    }
                }
            }
            return -1;
        }

        #endregion

        #region 多媒體清單

        private StackPanel getMediaListFromXML()
        {
            Canvas MediaTableCanvas = GetMediaTableCanvasInReader();
            //StackPanel mediaListPanel = GetMediaListPanelInReader();
            StackPanel sp = new StackPanel();

            TabControl tc = new TabControl();
            for (int i = 0; i < ObservableMediaList.Count; i++)
            {
                TabItem ti = new TabItem();
                ti.Header = ObservableMediaList[i].categoryName;
                ti.HeaderTemplate = (DataTemplate)FindResource("MediaListBoxHeaderTemplateStyle");
                //for (int j = 0; j < ObservableMediaList[i].mediaList.Count; j++)
                //{
                //    ObservableMediaList[i].mediaList[j].pageId = hejMetadata.spineList[ObservableMediaList[i].mediaList[j].pageId];
                //}
                if (!ObservableMediaList[i].mediaList.Count.Equals(0))
                {
                    ListBox lb = new ListBox();
                    lb.ItemsSource = ObservableMediaList[i].mediaList;
                    lb.Style = (Style)FindResource("MediaListBoxStyle");
                    lb.SelectionChanged += lv_SelectionChanged;
                    ti.Content = lb;

                    tc.Items.Add(ti);
                    lb = null;
                }
            }
            sp.Children.Add(tc);
            tc = null;
            return sp;
        }

        void lv_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!((ListBox)sender).SelectedIndex.Equals(-1))
            {
                Canvas MediaTableCanvas = GetMediaTableCanvasInReader();
                string mediaSourcePath = ((Media)(e.AddedItems[0])).mediaSourcePath;
                string mediaType = ((Media)(e.AddedItems[0])).mediaType;

                doMedia(mediaType, mediaSourcePath, -1);

                ((ListBox)sender).SelectedIndex = -1;
                e.Handled = true;
            }
        }

        private void ButtonInMediaList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Canvas MediaTableCanvas = GetMediaTableCanvasInReader();
                int targetPageIndex = Convert.ToInt32(((RadioButton)sender).Uid) - 1;

                if (viewStatusIndex.Equals(PageMode.DoublePage))
                {
                    targetPageIndex = getDoubleCurPageIndex(targetPageIndex);
                }

                if (targetPageIndex > -1)
                {
                    bringBlockIntoView(targetPageIndex);
                }
            }
            catch
            {
                //非整數
                return;
            }


        }

        #endregion

        #region RadioButton in ThumbNailListBox

        private bool BookMarkInLBIsClicked = false;

        private void BookMarkButtonInListBox_Checked(object sender, RoutedEventArgs e)
        {

            if (CheckIsNowClick(BookMarkButtonInListBoxSP) == true)
                return;
            ShowBookMark();

            return;
            //切換資料結構
            if (BookMarkInLBIsClicked)
            {
                BookMarkButtonInListBox.IsChecked = false;
                BookMarkInLBIsClicked = false;

                AllImageButtonInListBox.IsChecked = true;
                Task.Factory.StartNew(() =>
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
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

                //if (PaperLess_Emeeting.Properties.Settings.Default.IsFlatUIReader == false)
                //{
                List<ThumbnailImageAndPage> bookMarkedPage = new List<ThumbnailImageAndPage>();
                foreach (KeyValuePair<int, BookMarkData> bookMarkPair in bookMarkDictionary)
                {
                    if (bookMarkPair.Value.status == "0")
                    {
                        bookMarkedPage.Add(singleThumbnailImageAndPageList[bookMarkPair.Key]);
                    }
                }
                thumbNailListBox.ItemsSource = bookMarkedPage;
                BookMarkInLBIsClicked = true;
                BookMarkButtonInListBox.IsChecked = true;
                //}
                //else
                //{
                //    int i = 0;
                //    //var listK = bookMarkDictionary.Select(x=>x.Key).ToList();
                //    var listV = bookMarkDictionary.Select(x=>x.Value.status).ToList();
                //    foreach (ThumbnailImageAndPage item in thumbNailListBox.Items)
                //    {
                //        int PageIndex = int.Parse(item.pageIndex);
                //        ListBoxItem listBoxItem = (ListBoxItem)(thumbNailListBox.ItemContainerGenerator.ContainerFromIndex(i));
                //        if (listV[i].Equals("0"))
                //        {
                //            listBoxItem.Visibility = Visibility.Visible;
                //        }
                //        else
                //        {
                //            listBoxItem.Visibility = Visibility.Collapsed;
                //        }
                //        i++;
                //    }
                //}


            }

        }

        private bool NoteButtonInLBIsClicked = false;


        //Wayne mark 20150204
        //下方有作筆記的縮圖
        private void NoteButtonInListBox_Checked(object sender, RoutedEventArgs e)
        {

            if (CheckIsNowClick(NoteButtonInListBoxSP) == true)
                return;

            ShowNote();

            return;
            //切換資料結構
            if (NoteButtonInLBIsClicked)
            {
                NoteButtonInListBox.IsChecked = false;
                NoteButtonInLBIsClicked = false;
                AllImageButtonInListBox.IsChecked = true;
                Task.Factory.StartNew(() =>
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
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


                thumbNailListBox.ItemsSource = notePages;
                NoteButtonInLBIsClicked = true;
                NoteButtonInListBox.IsChecked = true;
            }
        }

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
                    this.Dispatcher.BeginInvoke(new Action(() =>
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
            return;


            BookMarkInLBIsClicked = false;
            NoteButtonInLBIsClicked = false;
            AllImageButtonInListBox.IsChecked = true;
            Task.Factory.StartNew(() =>
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    thumbNailListBox.ItemsSource = singleThumbnailImageAndPageList;
                }));
            });
            //thumbNailListBox.ItemsSource = singleThumbnailImageAndPageList;
        }

        private bool CheckIsNowClick(StackPanel SP)
        {
            System.Windows.Media.Brush backgroundColor = SP.Background;

            if (backgroundColor is SolidColorBrush)
            {
                string colorValue = ((SolidColorBrush)backgroundColor).Color.ToString();
                if (colorValue.Equals("#FFF66F00"))
                    return true;
            }

            return false;
        }
        #endregion

        #region Upper RadioButton

        private bool isFullScreenButtonClick = false;

        private void FullScreenButton_Checked(object sender, RoutedEventArgs e)
        {
            RoutedCommand fullScreenzoomInSettings = new RoutedCommand();
            fullScreenzoomInSettings.InputGestures.Add(new KeyGesture(Key.Escape));

            Grid toolBarInReader = FindVisualChildByName<Grid>(FR, "ToolBarInReader");
            RadioButton FullScreenButton = FindVisualChildByName<RadioButton>(FR, "FullScreenButton");

            //resetViewStatus();


            LockButton.IsChecked = false;
            isLockButtonLocked = false;
            resetTransform();
            LockButton.Visibility = Visibility.Collapsed;

            this.Visibility = Visibility.Collapsed;
            this.WindowState = WindowState.Maximized;

            if (!isFullScreenButtonClick)
            {
                CommandBindings.Add(new CommandBinding(fullScreenzoomInSettings, FullScreenButton_Checked));
                this.WindowStyle = WindowStyle.None;
                this.Visibility = Visibility.Visible;

                toolBarInReader.Visibility = Visibility.Collapsed;

                Canvas toolBarSensor = FindVisualChildByName<Canvas>(FR, "ToolBarSensor");
                toolBarSensor.Visibility = Visibility.Visible;
                toolBarSensor.IsMouseDirectlyOverChanged += toolBarSensor_IsMouseDirectlyOverChanged;

                FullScreenButton.IsChecked = true;
                isFullScreenButtonClick = true;
            }
            else
            {
                CommandBindings.Remove(CommandBindings[CommandBindings.Count - 1]);

                setWindowToFitScreen();
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                this.Visibility = Visibility.Visible;

                toolBarInReader.Visibility = Visibility.Visible;

                Canvas toolBarSensor = FindVisualChildByName<Canvas>(FR, "ToolBarSensor");
                toolBarSensor.Visibility = Visibility.Collapsed;
                toolBarSensor.IsMouseDirectlyOverChanged -= toolBarSensor_IsMouseDirectlyOverChanged;
                toolBarInReader.IsMouseDirectlyOverChanged -= toolBarSensor_IsMouseDirectlyOverChanged;

                FullScreenButton.IsChecked = false;
                isFullScreenButtonClick = false;
            }
            resetViewStatus();
            for (int i = 0; i < doubleImgStatus.Count; i++)
            {
                //目前先將所有圖變為小圖
                if (doubleImgStatus[i] == ImageStatus.GENERATING || doubleImgStatus[i] == ImageStatus.LARGEIMAGE)  //本頁未載入過，或現在是大圖
                {
                    ReadPagePair item = doubleReadPagePair[i];
                    if (item.leftImageSource != null)
                    {
                        item.leftImageSource = null;
                        item.decodedPDFPages = new byte[2][];
                        doubleImgStatus[i] = ImageStatus.SMALLIMAGE;
                        continue;
                    }
                }
            }

            for (int i = 0; i < singleImgStatus.Count; i++)
            {
                //目前先將所有圖變為小圖
                if (singleImgStatus[i] == ImageStatus.GENERATING || singleImgStatus[i] == ImageStatus.LARGEIMAGE)  //本頁未載入過，或現在是大圖
                {
                    ReadPagePair item = singleReadPagePair[i];
                    if (item.leftImageSource != null)
                    {
                        item.leftImageSource = null;
                        item.decodedPDFPages = new byte[2][];
                        singleImgStatus[i] = ImageStatus.SMALLIMAGE;
                        continue;
                    }
                }
            }

            //fullScreenToggle = true;

        }

        void toolBarSensor_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                if (sender is Canvas)
                {
                    Grid toolBarInReader = FindVisualChildByName<Grid>(FR, "ToolBarInReader");
                    toolBarInReader.Visibility = Visibility.Visible;
                    toolBarInReader.IsMouseDirectlyOverChanged += toolBarSensor_IsMouseDirectlyOverChanged;

                    Canvas toolBarSensor = FindVisualChildByName<Canvas>(FR, "ToolBarSensor");
                    toolBarSensor.Visibility = Visibility.Collapsed;
                    toolBarSensor.IsMouseDirectlyOverChanged -= toolBarSensor_IsMouseDirectlyOverChanged;
                }
            }
            else
            {
                if (sender is Grid)
                {
                    Grid toolBarInReader = FindVisualChildByName<Grid>(FR, "ToolBarInReader");
                    toolBarInReader.Visibility = Visibility.Collapsed;
                    toolBarInReader.IsMouseDirectlyOverChanged -= toolBarSensor_IsMouseDirectlyOverChanged;

                    Canvas toolBarSensor = FindVisualChildByName<Canvas>(FR, "ToolBarSensor");
                    toolBarSensor.Visibility = Visibility.Visible;
                    toolBarSensor.IsMouseDirectlyOverChanged += toolBarSensor_IsMouseDirectlyOverChanged;
                }
            }
        }

        private void PageViewButton_Checked(object sender, RoutedEventArgs e)
        {
            checkViewStatus(PageMode.SinglePage);
        }

        private void TwoPageViewButton_Checked(object sender, RoutedEventArgs e)
        {
            checkViewStatus(PageMode.DoublePage);
        }

        private PageMode viewStatusIndex = PageMode.DoublePage;

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

        private void checkViewStatus(PageMode curViewStatusIndex)
        {
            if (viewStatusIndex.Equals(curViewStatusIndex))
            {
                return;
            }

            resetTransform();

            viewStatusIndex = curViewStatusIndex;

            resetViewStatus();

            LockButton.IsChecked = false;
            isLockButtonLocked = false;
            LockButton.Visibility = Visibility.Collapsed;

            bool isTryRead = false;

            //試閱不提供螢光筆跟註記
            if (trialPages > 0)
            {
                isTryRead = true;
            }

            RadioButton NoteRb = FindVisualChildByName<RadioButton>(FR, "NoteButton");
            RadioButton ShareRb = FindVisualChildByName<RadioButton>(FR, "ShareButton");
            RadioButton PenMemoRb = FindVisualChildByName<RadioButton>(FR, "PenMemoButton");
            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
            Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");
            BrushConverter bc = new BrushConverter();

            int transCurPage = 0;
            switch (viewStatusIndex)
            {
                case PageMode.SinglePage:
                    transCurPage = getSingleCurPageIndex(curPageIndex);

                    int formerPage = transCurPage;
                    if (transCurPage != 0 && transCurPage != (_FlowDocument.Blocks.Count - 1))
                    {
                        //除首頁以及最後一頁, 切換單頁換成前面那一頁
                        transCurPage--;
                    }

                    //尋找切換雙頁前的單頁是哪頁
                    int singlePageSum = 0;
                    for (int i = 0; i < singleImgStatus.Count; i++)
                    {
                        if (singleImgStatus[i] == ImageStatus.LARGEIMAGE)
                        {
                            singlePageSum += i;
                        }
                    }
                    int formerSinglePage = singlePageSum / 3;

                    if (formerSinglePage == transCurPage || formerSinglePage == formerPage)
                    {
                        //同頁切換
                        transCurPage = formerSinglePage;
                    }

                    FR.Document = _FlowDocument;

                    //第一頁切換不會有翻頁event, 故要重新送
                    if (transCurPage.Equals(0))
                    {
                        if (singleReadPagePair[curPageIndex].leftImageSource != null)
                        {
                            useOriginalCanvasOnLockStatus = true;
                            try
                            {
                                SendImageSourceToZoomCanvas((BitmapImage)singleReadPagePair[curPageIndex].leftImageSource);

                                //做出感應框和螢光筆
                                if (canAreaButtonBeSeen)
                                {
                                    CheckAndProduceAreaButton(singleReadPagePair[curPageIndex].leftPageIndex, -1, defaultKey, zoomCanvas);
                                }
                                loadCurrentStrokes(hejMetadata.LImgList[singleReadPagePair[curPageIndex].leftPageIndex].pageId);
                                loadCurrentStrokes(singleReadPagePair[curPageIndex].leftPageIndex);
                                Debug.WriteLine("SendImageSourceToZoomCanvas@checkViewStatus");
                            }
                            catch (Exception ex)
                            {
                                //還沒有指派到ImageSource中, 當作沒有ren好
                                singleReadPagePair[curPageIndex].leftImageSource = null;
                                Debug.WriteLine(ex.Message.ToString());

                            }
                        }
                    }
                    else
                    {
                        zoomCanvas.Background = (System.Windows.Media.Brush)bc.ConvertFrom("#FF212020");
                    }
                    bringBlockIntoView(transCurPage);

                    curPageIndex = transCurPage;

                    if (NoteRb.Visibility.Equals(Visibility.Collapsed) && !isTryRead)
                    {
                        NoteRb.Visibility = Visibility.Visible;
                    }

                    if (ShareRb.Visibility.Equals(Visibility.Collapsed))
                    {
                        if (isSharedButtonShowed)
                        {
                            ShareRb.Visibility = Visibility.Visible;
                        }
                    }

                    if (canPrint)
                    {
                        RadioButton PrintRb = FindVisualChildByName<RadioButton>(FR, "PrintButton");
                        if (PrintRb.Visibility.Equals(Visibility.Collapsed))
                        {
                            PrintRb.Visibility = Visibility.Visible;
                        }
                    }
                    break;

                case PageMode.DoublePage:
                    transCurPage = getDoubleCurPageIndex(curPageIndex);

                    FR.Document = _FlowDocumentDouble;

                    //第一頁切換不會有翻頁event, 故要重新送
                    if (transCurPage.Equals(0))
                    {
                        if (doubleReadPagePair[curPageIndex].leftImageSource != null)
                        {
                            useOriginalCanvasOnLockStatus = true;
                            try
                            {
                                SendImageSourceToZoomCanvas((BitmapImage)doubleReadPagePair[curPageIndex].leftImageSource);
                                Debug.WriteLine("SendImageSourceToZoomCanvas@TextBlock_TargetUpdated_1");
                                //做出感應框和螢光筆
                                if (doubleReadPagePair[curPageIndex].rightPageIndex == -1)
                                {
                                    //封面或封底或單頁
                                    if (canAreaButtonBeSeen)
                                    {
                                        //Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");
                                        CheckAndProduceAreaButton(doubleReadPagePair[curPageIndex].leftPageIndex, -1, defaultKey, zoomCanvas);
                                    }
                                    loadCurrentStrokes(hejMetadata.LImgList[doubleReadPagePair[curPageIndex].leftPageIndex].pageId);
                                    loadCurrentStrokes(singleReadPagePair[curPageIndex].leftPageIndex);
                                }
                                else
                                {
                                    //雙頁
                                    if (canAreaButtonBeSeen)
                                    {
                                        CheckAndProduceAreaButton(doubleReadPagePair[curPageIndex].leftPageIndex, doubleReadPagePair[curPageIndex].rightPageIndex, defaultKey, zoomCanvas);
                                    }
                                    loadDoublePagesStrokes(hejMetadata.LImgList[doubleReadPagePair[curPageIndex].leftPageIndex].pageId, hejMetadata.LImgList[doubleReadPagePair[curPageIndex].rightPageIndex].pageId);
                                    loadDoublePagesStrokes(doubleReadPagePair[curPageIndex].leftPageIndex, doubleReadPagePair[curPageIndex].rightPageIndex);
                                }
                            }
                            catch (Exception ex)
                            {
                                //還沒有指派到ImageSource中, 當作沒有ren好
                                doubleReadPagePair[curPageIndex].leftImageSource = null;
                                Debug.WriteLine(ex.Message.ToString());

                            }
                        }
                    }
                    else
                    {
                        zoomCanvas.Background = (System.Windows.Media.Brush)bc.ConvertFrom("#FF212020");
                    }
                    bringBlockIntoView(transCurPage);
                    curPageIndex = transCurPage;


                    if (NoteRb.Visibility.Equals(Visibility.Visible) && !isTryRead)
                    {
                        NoteRb.Visibility = Visibility.Collapsed;
                    }
                    if (ShareRb.Visibility.Equals(Visibility.Visible))
                    {
                        ShareRb.Visibility = Visibility.Collapsed;
                    }

                    if (canPrint)
                    {
                        RadioButton PrintRb = FindVisualChildByName<RadioButton>(FR, "PrintButton");
                        if (PrintRb.Visibility.Equals(Visibility.Visible))
                        {
                            PrintRb.Visibility = Visibility.Collapsed;
                        }
                    }
                    break;
                case PageMode.None:
                    break;
            }

            if (isFullScreenButtonClick)
            {
                Grid toolBarInReader = FindVisualChildByName<Grid>(FR, "ToolBarInReader");
                toolBarInReader.Visibility = Visibility.Collapsed;
                toolBarInReader.IsMouseDirectlyOverChanged -= toolBarSensor_IsMouseDirectlyOverChanged;

                Canvas toolBarSensor = FindVisualChildByName<Canvas>(FR, "ToolBarSensor");
                toolBarSensor.Visibility = Visibility.Visible;
                toolBarSensor.IsMouseDirectlyOverChanged += toolBarSensor_IsMouseDirectlyOverChanged;
                resetViewStatus();
            }
        }

        private void resetViewStatus()
        {
            Canvas stageCanvas = GetStageCanvasInReader();
            isAreaButtonAndPenMemoRequestSent = false;

            if (stageCanvas.Children.Count > 0)
            {
                stageCanvas.Children.Clear();
                //stageCanvas.MouseLeftButtonDown -= ImageInReader_MouseLeftButtonDown;
                //stageCanvas.Background = null;
                RadioButton fTRB = FindVisualChildByName<RadioButton>(FR, "FullTextButton");
                fTRB.Visibility = Visibility.Collapsed;
            }


            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
            if (penMemoCanvas.Strokes.Count > 0)
            {
                penMemoCanvas.Strokes.Clear();
            }


            Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");
            zoomCanvas.Background = null;
        }

        private void Touch_MouseLeftButtonUp(object sender, MouseEventArgs e)
        {
            Canvas canvas = (Canvas)sender;

            TextBlock curPageInReader = FindVisualChildByName<TextBlock>(FR, "CurPageInReader");
            int pgNow = int.Parse(curPageInReader.Text);


            TextBlock TotalPageInReader = FindVisualChildByName<TextBlock>(FR, "TotalPageInReader");
            int pgCount = int.Parse(TotalPageInReader.Text);

            string tagData = canvas.Tag as string;
            switch (tagData)
            {
                case "MoveDown":
                case "MoveRight":
                    if (pgNow > 1)
                        bringBlockIntoView(pgNow - 1);
                    break;

                case "MoveUp":
                case "MoveLeft":
                    if (pgNow < pgCount)
                        bringBlockIntoView(pgNow + 1);
                    break;

            }
        }


        private void FullTextButton_Checked(object sender, RoutedEventArgs e)
        {
            PageInfoMetadata pageInfo = (PageInfoMetadata)((RadioButton)sender).Tag;

            HyperLinkArea areaButton = pageInfo.hyperLinkAreas[0];
            string sourcePath = bookPath + "\\HYWEB\\" + areaButton.items[0].href.Replace("/", "\\");

            doMedia(areaButton.items[0].mediaType, sourcePath, -1);
        }

        private void LockButton_Checked(object sender, RoutedEventArgs e)
        {
            //RadioButton rb = FindVisualChildByName<RadioButton>(FR, "LockButton");
            if (isLockButtonLocked.Equals(false))
            {
                LockButton.IsChecked = true;
                isLockButtonLocked = true;
            }
            else
            {
                LockButton.IsChecked = false;
                isLockButtonLocked = false;
                resetTransform();
                LockButton.Visibility = Visibility.Collapsed;
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

        private MediaCanvasOpenedBy openedby = MediaCanvasOpenedBy.None;
        private int clickedPage = 0;

        private void MediaListButton_Checked(object sender, RoutedEventArgs e)
        {
            doUpperRadioButtonClicked(MediaCanvasOpenedBy.MediaButton, sender);
        }

        private void SearchButton_Checked(object sender, RoutedEventArgs e)
        {
            doUpperRadioButtonClicked(MediaCanvasOpenedBy.SearchButton, sender);
        }

        private void TocButton_Checked(object sender, RoutedEventArgs e)
        {
            doUpperRadioButtonClicked(MediaCanvasOpenedBy.CategoryButton, sender);
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
                Border mediaListBorder = FindVisualChildByName<Border>(FR, "mediaListBorder");
                if (mediaListBorder != null)
                {
                    Canvas.SetTop(mediaListBorder, Double.NaN);
                    Canvas.SetRight(mediaListBorder, Double.NaN);
                    Canvas.SetBottom(mediaListBorder, 64);
                    Canvas.SetLeft(mediaListBorder, PenSP.Width + 64);
                }

                //thumnailCanvas.Visibility = Visibility.Collapsed;
                //ViewThumbSP.Background = ColorTool.HexColorToBrush("#000000");

            }

            doUpperRadioButtonClicked(MediaCanvasOpenedBy.NoteButton, sender);

            Canvas MediaTableCanvas = GetMediaTableCanvasInReader();

            if (MediaTableCanvas != null && MediaTableCanvas.Visibility == Visibility.Visible)
            {
                MemoSP.Background = ColorTool.HexColorToBrush("#F66F00");

                if (CheckIsNowClick(ViewThumbSP) == true)
                {
                    MyAnimation(thumnailCanvas, 500, "Height", 150, 0, () => { thumnailCanvas.Visibility = Visibility.Collapsed; });
                    ViewThumbSP.Background = ColorTool.HexColorToBrush("#000000");
                }
            }


        }

        public UIElement cloneElement(UIElement orig)
        {
            if (orig == null)
                return (null);
            string s = XamlWriter.Save(orig);
            StringReader stringReader = new StringReader(s);
            XmlReader xmlReader = XmlTextReader.Create(stringReader, new XmlReaderSettings());
            return (UIElement)XamlReader.Load(xmlReader);
        }



        private void SettingsButton_Checked(object sender, RoutedEventArgs e)
        {
            doUpperRadioButtonClicked(MediaCanvasOpenedBy.SettingButton, sender);
        }

        private Dictionary<MediaCanvasOpenedBy, StackPanel> RelativePanel
            = new Dictionary<MediaCanvasOpenedBy, StackPanel>();

        private void ContentButton_Checked(object sender, RoutedEventArgs e)
        {
            if (thumbNailListBoxOpenedFullScreen)
            {
                thumnailCanvas.Visibility = Visibility.Hidden;
            }

            Canvas MediaTableCanvas = GetMediaTableCanvasInReader();

            //同一個頁同按鈕, 只開關canvas
            if (MediaTableCanvas.Visibility.Equals(Visibility.Visible))
            {
                MediaTableCanvas.Visibility = Visibility.Collapsed;
            }

            int targetPageIndex = hejMetadata.tocPageIndex - 1;
            if (viewStatusIndex.Equals(PageMode.DoublePage))
            {
                targetPageIndex = getDoubleCurPageIndex(targetPageIndex);
            }
            if (targetPageIndex != -1)
            {
                bringBlockIntoView(targetPageIndex);
            }
        }

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
                RadioButton rb = FindVisualChildByName<RadioButton>(FR, childNameInReader);
                rb.IsChecked = false;
            }
            else if (childNameInReader.Equals("NoteButton"))
            {
                TextBox tb = FindVisualChildByName<TextBox>(FR, "notePanel");

                int targetPageIndex = curPageIndex;

                bool isSuccess = setNotesInMem(tb.Text, targetPageIndex);

                //bool isUpdate = bookNoteDictionary[targetPageIndex].notes == "" ? false : true;



                //bookNoteDictionary[targetPageIndex].notes = tb.Text;
                //DateTime dt = new DateTime(1970, 1, 1);

                ////server上的儲存時間的格式是second, Ticks單一刻度表示千萬分之一秒

                //long currentTime = DateTime.Now.ToUniversalTime().Subtract(dt).Ticks / 10000000;
                //bookManager.saveNoteData(userBookSno, targetPageIndex, tb.Text,isUpdate, currentTime, currentTime);

                //bookManager.saveNoteData(userBookSno, targetPageIndex.ToString(), tb.Text);
                RadioButton NoteRB = FindVisualChildByName<RadioButton>(FR, "NoteButton");
                if (tb.Text.Equals(""))
                {
                    NoteRB.IsChecked = false;
                    TriggerBookMark_NoteButtonOrElse(NoteRB);
                }
                else
                {
                    NoteRB.IsChecked = true;
                    TriggerBookMark_NoteButtonOrElse(NoteRB);
                }

                //ShowNote();
                ShowAddition();
            }
            resetFocusBackToReader();
        }

        private void resetFocusBackToReader()
        {
            if (pageViewerPager != null)
            {
                if (pageViewerPager.Focusable && pageViewerPager.IsEnabled)
                {
                    if (!pageViewerPager.IsKeyboardFocused)
                    {
                        //Debug.WriteLine("pageViewerPager pageViewerPager.Focusable: {0}, pageViewerPager.IsEnabled:{1}, pageViewerPager.IsKeyboardFocused: {2}, pageViewerPager.IsKeyboardFocusWithin:{3}",
                        //    pageViewerPager.Focusable.ToString(), pageViewerPager.IsEnabled.ToString(),
                        //    pageViewerPager.IsKeyboardFocused.ToString(), pageViewerPager.IsKeyboardFocusWithin.ToString()
                        //    );
                        Keyboard.Focus(pageViewerPager);
                    }

                }
            }
        }

        private void PenMemoButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton PenMemoButton = (RadioButton)sender;
            if (viewStatusIndex.Equals(PageMode.DoublePage))
            {
                MessageBox.Show(langMng.getLangString("doublePageStrokeModeAlert"), langMng.getLangString("strokeMode"), MessageBoxButton.OK);
                //MessageBox.Show("欲使用螢光筆功能，請切換到單頁模式。", "螢光筆模式", MessageBoxButton.OK);
                PenMemoButton.IsChecked = false;
                return;
            }

            openedby = MediaCanvasOpenedBy.PenMemo;
            Grid toolBarInReader = FindVisualChildByName<Grid>(FR, "ToolBarInReader");
            Grid penMemoToolBar = FindVisualChildByName<Grid>(FR, "PenMemoToolBar");
            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
            StrokeToolPanelHorizontal_Reader strokeToolPanelHorizontal = new StrokeToolPanelHorizontal_Reader();
            strokeToolPanelHorizontal.langMng = this.langMng;
            Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");

            Canvas stageCanvas = GetStageCanvasInReader();

            if (penMemoToolBar.Visibility.Equals(Visibility.Collapsed))
            {
                toolBarInReader.Visibility = Visibility.Collapsed;
                penMemoToolBar.Visibility = Visibility.Visible;
                PenMemoButton.IsChecked = false;


                strokeToolPanelHorizontal.determineDrawAtt(penMemoCanvas.DefaultDrawingAttributes, isStrokeLine);

                //打開
                Canvas.SetZIndex(penMemoCanvas, 900);
                Canvas.SetZIndex(stageCanvas, 2);
                Canvas.SetZIndex(zoomCanvas, 850);

                penMemoCanvas.Background = System.Windows.Media.Brushes.Transparent;
                penMemoCanvas.EditingMode = InkCanvasEditingMode.Ink;

                penMemoCanvas.Visibility = Visibility.Visible;

                strokeToolPanelHorizontal.HorizontalAlignment = HorizontalAlignment.Right;
                penMemoToolBar.Children.Add(strokeToolPanelHorizontal);

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
                Canvas HiddenControlCanvas = FindVisualChildByName<Canvas>(FR, "HiddenControlCanvas");
                if (HiddenControlCanvas.Visibility.Equals(Visibility.Collapsed))
                {
                    HiddenControlCanvas.Visibility = Visibility.Visible;
                }

                Keyboard.ClearFocus();

                //把其他的按鈕都disable
                //disableAllOtherButtons(true);

                ButtonsStatusWhenOpenPenMemo(0.5, false);
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
                Canvas.SetZIndex(zoomCanvas, 1);
                Canvas.SetZIndex(penMemoCanvas, 2);
                Canvas.SetZIndex(stageCanvas, 3);
                ((RadioButton)sender).IsChecked = false;
                penMemoCanvas.EditingMode = InkCanvasEditingMode.None;

                alterPenmemoAnimation(strokeToolPanelHorizontal, strokeToolPanelHorizontal.Width, 0);

                //存現在的營光筆
                convertCurrentStrokesToDB(hejMetadata.LImgList[curPageIndex].pageId);

                penMemoToolBar.Children.Remove(penMemoToolBar.Children[penMemoToolBar.Children.Count - 1]);
                Canvas popupControlCanvas = FindVisualChildByName<Canvas>(FR, "PopupControlCanvas");
                if (popupControlCanvas.Visibility.Equals(Visibility.Visible))
                {
                    popupControlCanvas.Visibility = Visibility.Collapsed;
                }
                Canvas HiddenControlCanvas = FindVisualChildByName<Canvas>(FR, "HiddenControlCanvas");
                if (HiddenControlCanvas.Visibility.Equals(Visibility.Visible))
                {
                    HiddenControlCanvas.Visibility = Visibility.Collapsed;
                }

                penMemoToolBar.Visibility = Visibility.Collapsed;
                toolBarInReader.Visibility = Visibility.Visible;
                ButtonsStatusWhenOpenPenMemo(1, true);
                resetFocusBackToReader();
            }
        }

        private void ButtonsStatusWhenOpenPenMemo(double opacity, bool isEnabled)
        {
            RadioButton leftPageButtonRb = FindVisualChildByName<RadioButton>(FR, "leftPageButton");
            RadioButton rightPageButtonRb = FindVisualChildByName<RadioButton>(FR, "rightPageButton");

            leftPageButtonRb.Opacity = opacity;
            rightPageButtonRb.Opacity = opacity;

            LockButton.IsEnabled = isEnabled;
            ShowListBoxButton.IsEnabled = IsEnabled;
        }

        private void BackToBookShelfButton_Click(object sender, RoutedEventArgs e)
        {
            if (isSyncing == true && isSyncOwner == false)
                return;
            this.Close();
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
                    this.Dispatcher.BeginInvoke(new Action(() =>
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


        private void ShowAll()
        {
            try
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
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }
        }

        private void ShowAddition(bool ShowFilter = true)
        {
            //MoveBoxPage();
            if (ShowFilter == true)
                ShowFilterCount();
            ShowImageCenter();
        }

        private void ShowImageCenter()
        {

            if (Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action(ShowImageCenter));
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


        #endregion

        #region 縮圖列及縮圖總覽

        private int thumbNailListBoxStatus = 0;
        // 0->關閉, 1->縮圖列, 2->縮圖總覽

        private bool thumbNailListBoxOpenedFullScreen = false;

        private double thumbnailListBoxHeight = 150;

        private void ChangeThumbNailListBoxRelativeStatus()
        {
            Canvas MediaTableCanvas = GetMediaTableCanvasInReader();
            if (MediaTableCanvas.Visibility.Equals(Visibility.Visible))
            {
                MediaTableCanvas.Visibility = Visibility.Collapsed;
            }
            RadioButton ShowAllImageButton = FindVisualChildByName<RadioButton>(FR, "ShowAllImageButton");
            ScrollViewer sv = FindVisualChildByName<ScrollViewer>(thumbNailListBox, "SVInLV");
            WrapPanel wrapPanel = FindVisualChildByName<WrapPanel>(FR, "wrapPanel");

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

            //thumbNailListBox.ItemsSource = singleThumbnailImageAndPageList;

            switch (thumbNailListBoxStatus)
            {
                case 0:
                    thumbNailListBoxOpenedFullScreen = false;
                    thumnailCanvas.Visibility = Visibility.Hidden;
                    ShowListBoxButton.Visibility = Visibility.Visible;

                    ShowAllImageButton.IsChecked = false;

                    AllImageButtonInListBox.IsChecked = true;
                    //thumbNailListBox.ItemsSource = singleThumbnailImageAndPageList;

                    if (!downloadProgBar.Visibility.Equals(Visibility.Collapsed))
                    {
                        downloadProgBar.Margin = new Thickness(0, 0, 0, 0);
                    }

                    LockButton.Margin = new Thickness(0, 0, 15, 15);

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

                    ShowAllImageButton.IsChecked = false;

                    ShowListBoxButton.Visibility = Visibility.Hidden;

                    if (!downloadProgBar.Visibility.Equals(Visibility.Collapsed))
                    {
                        downloadProgBar.Margin = new Thickness(0, 0, 0, thumbnailListBoxHeight);
                    }

                    LockButton.Margin = new Thickness(0, 0, 15, 15 + thumbnailListBoxHeight);

                    break;
                case 2:
                    thumbNailListBoxOpenedFullScreen = true;

                    Binding heightBinding = new Binding();
                    heightBinding.Source = FR;
                    heightBinding.Path = new PropertyPath("ActualHeight");
                    thumnailCanvas.SetBinding(Canvas.HeightProperty, heightBinding);

                    Binding widthBinding = new Binding();
                    widthBinding.Source = FR;
                    widthBinding.Path = new PropertyPath("ActualWidth");

                    Binding convertBinding = new Binding();
                    convertBinding.Source = FR;
                    convertBinding.Path = new PropertyPath("ActualHeight");
                    convertBinding.Converter = new thumbNailListBoxWidthHeightConverter();
                    convertBinding.ConverterParameter = 30;

                    thumbNailListBox.SetBinding(ListBox.HeightProperty, convertBinding);
                    thumbNailListBox.SetBinding(ListBox.WidthProperty, widthBinding);

                    sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                    sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;

                    thumbNailCanvasStackPanel.Orientation = Orientation.Vertical;
                    RadioButtonStackPanel.Orientation = Orientation.Horizontal;
                    thumbNailCanvasGrid.HorizontalAlignment = HorizontalAlignment.Right;

                    thumnailCanvas.Visibility = Visibility.Visible;

                    ShowAllImageButton.IsChecked = true;

                    ShowListBoxButton.Visibility = Visibility.Hidden;

                    HideListBoxButton.ToolTip = langMng.getLangString("closeThumbnail"); //"關閉縮圖總覽";

                    if (!downloadProgBar.Visibility.Equals(Visibility.Collapsed))
                    {
                        downloadProgBar.Margin = new Thickness(0, 0, 0, 0);
                    }

                    LockButton.Margin = new Thickness(0, 0, 15, 15);

                    break;
            }
        }

        private void ShowAllImageButton_Checked(object sender, RoutedEventArgs e)
        {
            thumbNailListBoxStatus = 2;
            ChangeThumbNailListBoxRelativeStatus();

            //Canvas MediaTableCanvas = GetMediaTableCanvasInReader();
            //if (MediaTableCanvas.Visibility.Equals(Visibility.Visible))
            //{
            //    MediaTableCanvas.Visibility = Visibility.Collapsed;
            //}

            //thumbNailListBoxOpenedFullScreen = true;
            //if (thumnailCanvas.Visibility.Equals(Visibility.Hidden))
            //{
            //    Binding binding = new Binding();
            //    binding.Source = FR;
            //    binding.Path = new PropertyPath("ActualHeight");
            //    thumnailCanvas.SetBinding(Canvas.HeightProperty, binding);
            //    thumbNailListBox.SetBinding(ListBox.HeightProperty, binding);


            //    ScrollViewer sv = FindVisualChildByName<ScrollViewer>(thumbNailListBox, "SVInLV");
            //    sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            //    sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            //    HideListBoxButton.ToolTip = "隱藏縮圖總覽";

            //    thumnailCanvas.Visibility = Visibility.Visible;
            //}
            //else if (thumnailCanvas.Visibility.Equals(Visibility.Visible))
            //{
            //    thumnailCanvas.Visibility = Visibility.Hidden;
            //    ((RadioButton)sender).IsChecked = false;
            //}

            //if (ShowListBoxButton.Visibility.Equals(Visibility.Hidden))
            //{
            //    ShowListBoxButton.Visibility = Visibility.Visible;
            //}
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
                //((System.Windows.Controls.Image)(btnViewThumb.Content)).Source= new BitmapImage(new Uri("images/tool-viewThumb-active@2x.png", UriKind.Relative));
                ShowAll();
                thumbNailListBoxStatus = 1;

                //Wayne Mark 圖層的抽取，取消便利貼視窗
                ChangeThumbNailListBoxRelativeStatus();
                MoveBoxPage();
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
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(300);
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    MouseTool.ShowArrow();
                }));
            });
        }
        private void MoveBoxPage()
        {
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

            if (this.Dispatcher.CheckAccess() == false)
            {
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action(ShowFilterCount));
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

            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
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
            System.Windows.Media.Brush backgroundColor = btnEraserGD.Background;

            if (backgroundColor is SolidColorBrush)
            {
                string colorValue = ((SolidColorBrush)backgroundColor).Color.ToString();
                if (colorValue.Equals("#FFF66F00") == true)
                {
                    btnEraserGD.Background = System.Windows.Media.Brushes.Transparent;
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

                Border mediaListBorder = FindVisualChildByName<Border>(FR, "mediaListBorder");

                if (mediaListBorder != null)
                {
                    Canvas.SetTop(mediaListBorder, Double.NaN);
                    Canvas.SetRight(mediaListBorder, Double.NaN);
                    //dda.FillBehavior = FillBehavior.Stop;
                    mediaListBorder.BeginAnimation(Canvas.LeftProperty, dda);
                }

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
                //ChangeThumbNailListBoxRelativeStatus();
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
            image.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);

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





        private void ShowListBoxButton_Click(object sender, RoutedEventArgs e)
        {
            thumbNailListBoxStatus = 1;
            ChangeThumbNailListBoxRelativeStatus();
            //thumbNailListBoxOpenedFullScreen = false;
            //if (thumnailCanvas.Visibility.Equals(Visibility.Hidden))
            //{
            //    thumbNailListBox.Height = thumbnailListBoxHeight;
            //    thumnailCanvas.Height = thumbnailListBoxHeight;

            //    ScrollViewer sv = FindVisualChildByName<ScrollViewer>(thumbNailListBox, "SVInLV");
            //    sv.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            //    sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            //    HideListBoxButton.ToolTip = "隱藏縮圖列";

            //    thumnailCanvas.Visibility = Visibility.Visible;
            //    if (!downloadProgBar.Visibility.Equals(Visibility.Collapsed))
            //    {
            //        downloadProgBar.Margin = new Thickness(0, 0, 0, thumbnailListBoxHeight);
            //    }

            //    LockButton.Margin = new Thickness(0, 0, 15, 15 + thumbnailListBoxHeight);

            //    HideListBoxButton.Visibility = Visibility.Visible;
            //}

            //if (ShowListBoxButton.Visibility.Equals(Visibility.Visible))
            //{
            //    ShowListBoxButton.Visibility = Visibility.Hidden;
            //}
        }

        private void HideListBoxButton_Checked(object sender, RoutedEventArgs e)
        {
            thumbNailListBoxStatus = 0;
            ChangeThumbNailListBoxRelativeStatus();
            //if (thumnailCanvas.Visibility.Equals(Visibility.Visible))
            //{
            //    thumnailCanvas.Visibility = Visibility.Hidden;
            //}

            //if (ShowListBoxButton.Visibility.Equals(Visibility.Hidden))
            //{
            //    ShowListBoxButton.Visibility = Visibility.Visible;
            //}

            //AllImageButtonInListBox.IsChecked = true;
            //thumbNailListBox.ItemsSource = singleThumbnailImageAndPageList;

            //if (!downloadProgBar.Visibility.Equals(Visibility.Collapsed))
            //{
            //    downloadProgBar.Margin = new Thickness(0, 0, 0, 0);
            //}

            //LockButton.Margin = new Thickness(0, 0, 15, 15);
        }

        #endregion

        #region 偏好設定

        private bool canAreaButtonBeSeen = true;

        private StackPanel openSettings()
        {
            //StackPanel mediaListPanel = GetMediaListPanelInReader();
            List<TextBlock> settings = new List<TextBlock>() 
			{
				new TextBlock(){Text=langMng.getLangString("showMultimediaSensor"), FontSize=14 },     //顯示多媒體感應框
				new TextBlock(){Text=langMng.getLangString("showPageButton"), FontSize=14 }            //顯示翻頁按鈕
			};
            List<bool> isSettingsChecked = new List<bool>() { true, true };

            //double panelWidth = mediaListPanel.Width;
            //double panelHeight = mediaListPanel.Height;

            StackPanel sp = new StackPanel();
            sp.Margin = new Thickness(20, 10, 20, 10);
            sp.Orientation = Orientation.Vertical;
            for (int i = 0; i < settings.Count; i++)
            {
                Grid tempGrid = new Grid();
                tempGrid.HorizontalAlignment = HorizontalAlignment.Left;
                tempGrid.Margin = new Thickness(0, 0, 0, 10);
                CheckBox settingsButton = new CheckBox() { Content = settings[i], IsChecked = isSettingsChecked[i] };
                if (i == 0)
                {
                    settingsButton.Click += AreaButtonSettingsButton_Click;
                }
                else if (i == 1)
                {
                    settingsButton.Click += LeftRightPageButtonSettingsButton_Click;
                }
                tempGrid.Children.Add(settingsButton);
                sp.Children.Add(tempGrid);
            }
            sp.Orientation = Orientation.Vertical;
            return sp;
        }

        private void LeftRightPageButtonSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            CheckBox tb = (CheckBox)sender;

            RadioButton leftPageButton = FindVisualChildByName<RadioButton>(FR, "leftPageButton");
            RadioButton rightPageButton = FindVisualChildByName<RadioButton>(FR, "rightPageButton");

            if (tb.IsChecked.Equals(true))
            {
                leftPageButton.Visibility = Visibility.Visible;
                rightPageButton.Visibility = Visibility.Visible;

            }
            else if (tb.IsChecked.Equals(false))
            {
                leftPageButton.Visibility = Visibility.Collapsed;
                rightPageButton.Visibility = Visibility.Collapsed;
            }

            resetFocusBackToReader();
        }

        void AreaButtonSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            //List<string> toggleButtonText = new List<string>() { "感應框開啟", "感應框關閉" };
            CheckBox tb = (CheckBox)sender;
            if (tb.IsChecked.Equals(true))
            {
                //tb.Content = toggleButtonText[0];
                canAreaButtonBeSeen = true;

                byte[] curKey = defaultKey;

                Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");
                if (viewStatusIndex.Equals(PageMode.SinglePage))
                {
                    CheckAndProduceAreaButton(curPageIndex, -1, curKey, zoomCanvas);
                }
                else if (viewStatusIndex.Equals(PageMode.DoublePage))
                {
                    int doubleIndex = curPageIndex;

                    if (doubleIndex.Equals(0) || doubleIndex.Equals(singleThumbnailImageAndPageList.Count - 1))
                    {
                        CheckAndProduceAreaButton(curPageIndex, -1, curKey, zoomCanvas);
                    }
                    else
                    {
                        doubleIndex = getSingleCurPageIndex(doubleIndex);
                        //推算雙頁是哪兩頁的組合
                        int leftCurPageIndex = doubleIndex - 1;
                        int rightCurPageIndex = doubleIndex;

                        if (hejMetadata.direction.Equals("right"))
                        {
                            leftCurPageIndex = doubleIndex;
                            rightCurPageIndex = doubleIndex - 1;
                        }

                        CheckAndProduceAreaButton(leftCurPageIndex, rightCurPageIndex, curKey, zoomCanvas);
                    }
                }
                curKey = null;
            }
            else if (tb.IsChecked.Equals(false))
            {
                //tb.Content = toggleButtonText[1];
                canAreaButtonBeSeen = false;
                Canvas stageCanvas = GetStageCanvasInReader();
                if (stageCanvas.Children.Count > 0)
                {
                    stageCanvas.Children.Clear();
                    //stageCanvas.MouseLeftButtonDown -= ImageInReader_MouseLeftButtonDown;
                    //stageCanvas.Background = null;
                }
            }

            resetFocusBackToReader();
        }

        #endregion

        #region 註記

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

        #endregion

        #region 全文檢索

        void searchTB_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                StackPanel mediaListPanel = GetMediaListPanelInReader();
                TextBox tb = FindVisualChildByName<TextBox>(FR, "searchBar");
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

        void searchButton_Click(object sender, RoutedEventArgs e)
        {
            StackPanel mediaListPanel = GetMediaListPanelInReader();
            TextBox tb = FindVisualChildByName<TextBox>(FR, "searchBar");
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

        void lb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = (ListBox)sender;
            if (lb.SelectedIndex != -1)
            {
                //按下跳頁, 等待後面data binding
                int index = ((SearchRecord)(e.AddedItems[0])).targetPage;
                int targetPageIndex = index - 1;

                if (viewStatusIndex.Equals(PageMode.DoublePage))
                {
                    targetPageIndex = getDoubleCurPageIndex(targetPageIndex);
                }

                if (!targetPageIndex.Equals(-1))
                {
                    bringBlockIntoView(targetPageIndex);
                }
                lb.SelectedIndex = -1;
            }
        }

        #endregion

        #region 目錄

        private StackPanel getTocNcx()
        {
            StackPanel mediaListPanel = GetMediaListPanelInReader();

            StackPanel sp = new StackPanel();
            //byte[] curKey = defaultKey;
            //string ncxFile = bookPath + "\\HYWEB\\toc.ncx";
            //XmlDocument XmlDocNcx = new XmlDocument();
            //using (MemoryStream tocStream = caTool.fileAESDecode(ncxFile, curKey, false))
            //{
            //    StreamReader xr = new StreamReader(tocStream);
            //    string xmlString = xr.ReadToEnd();
            //    xmlString = xmlString.Replace("xmlns=\"http://www.hyweb.com.tw/schemas/info\" version=\"1.0\"", "");
            //    XmlDocNcx.LoadXml(xmlString);
            //    xr.Close();
            //    xr = null;
            //    tocStream.Close();
            //    xmlString = null;
            //}
            TreeView rootTree = new TreeView();

            double totalHeight = mediaListPanel.Height;
            double totalWidth = sp.Width = mediaListPanel.Width;

            rootTree.Height = totalHeight;
            //rootTree.Width = totalWidth;
            foreach (XmlNode ncxNode in XmlDocNcx.ChildNodes)
            {
                if (ncxNode.Name == "ncx")
                {
                    foreach (XmlNode navMapNode in ncxNode.ChildNodes)
                    {
                        if (navMapNode.Name == "navMap")
                        {
                            foreach (XmlNode navMapChildNode in navMapNode.ChildNodes)
                            {
                                TreeViewItem layer1 = new TreeViewItem();
                                AddTreeNode(navMapChildNode, layer1);
                                layer1.IsExpanded = true;
                                rootTree.Items.Add(layer1);
                            }
                        }
                    }
                }
            }
            rootTree.SelectedItemChanged += rootTree_SelectedItemChanged;
            rootTree.Style = (Style)FindResource("ContentTreeViewStyle");
            rootTree.BorderBrush = System.Windows.Media.Brushes.White;
            sp.Children.Clear();
            sp.Children.Add(rootTree);
            //sp.UpdateLayout();
            return sp;
        }

        void rootTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            string targetSource = (string)((TreeViewItem)e.NewValue).Tag;

            int targetPageIndex = 0;
            for (int i = 0; i < hejMetadata.LImgList.Count; i++)
            {
                if (hejMetadata.LImgList[i].path.Replace("HYWEB\\", "").Equals(targetSource))
                {
                    if (viewStatusIndex.Equals(PageMode.SinglePage))
                    {
                        targetPageIndex = i;
                    }
                    else if (viewStatusIndex.Equals(PageMode.DoublePage))
                    {
                        targetPageIndex = getDoubleCurPageIndex(i);
                    }
                    break;
                }
            }

            if (targetPageIndex != -1)
            {
                bringBlockIntoView(targetPageIndex);
            }
        }

        private void TreeViewItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }

        private void AddTreeNode(XmlNode firstNode, TreeViewItem layer1)
        {
            foreach (XmlNode secondNode in firstNode.ChildNodes)
            {
                if (secondNode.Name == "navLabel")
                {
                    //下面只有一個text節點, 直接用innerText取值
                    layer1.Header = secondNode.InnerText;
                }
                else if (secondNode.Name == "content")
                {
                    layer1.Tag = secondNode.Attributes.GetNamedItem("src").Value;
                }
                else if (secondNode.HasChildNodes)
                {
                    TreeViewItem layer2 = new TreeViewItem();
                    AddTreeNode(secondNode, layer2);
                    layer2.IsExpanded = true;
                    layer1.Items.Add(layer2);
                }
            }
        }

        #endregion

        #region Zoom In and Out

        public event EventHandler<imageSourceRenderedResultEventArgs> imageSourceRendered;

        private void RepeatButton_Click_1(object sender, RoutedEventArgs e)
        {
            //小
            if (zoomStep == 0)
                return;

            zoomStep--;
            ZoomImage(zoomStepScale[zoomStep], zoomStepScale[0], false);
        }

        private void RepeatButton_Click_2(object sender, RoutedEventArgs e)
        {
            //大
            if (zoomStep == zoomStepScale.Length - 1)
                return;

            zoomStep++;
            ZoomImage(zoomStepScale[zoomStep], zoomStepScale[zoomStepScale.Length - 1], true);
        }

        private void SliderInReader_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (e.NewValue > e.OldValue)
                {
                    //往大的拉
                    if (zoomStep == zoomStepScale.Length - 1)
                        return;

                    zoomStep++;
                    ZoomImage(zoomStepScale[zoomStep], zoomStepScale[zoomStepScale.Length - 1], true, false);
                }
                else
                {
                    //往小的拉
                    if (zoomStep == 0)
                        return;

                    zoomStep--;
                    ZoomImage(zoomStepScale[zoomStep], zoomStepScale[0], false, false);
                }
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }

            //TextBox tb = FindVisualChildByName<TextBox>(FR, "notePanel");
            //tb.IsEnabled = true;


        }

        private void ZoomImage(double imageScale, double scaleMaxOrMin, bool Maximum)
        {
            ZoomImage(imageScale, scaleMaxOrMin, Maximum, false);
        }

        private List<Thread> zoomeThread = new List<Thread>();

        private void RepaintPDF(double imageScale)
        {
            //比照快翻頁的方法, 太過快速的翻頁省略
            lastTimeOfChangingPage = DateTime.Now;
            PDFScale = (float)imageScale;
            if (viewStatusIndex.Equals(PageMode.SinglePage))
            {
                //單頁模式
                ReadPagePair singlePagePair = singleReadPagePair[curPageIndex];

                string imagePath = singlePagePair.leftImagePath;

                bool imagePathExists = File.Exists(imagePath);
                if (imagePathExists)
                {
                    //將 ren 好PDF event (改用Thread的方式ren)
                    Border bd = GetBorderInReader();
                    //因為ren PDF的參數需要用到現在的border, 故多帶一個參數否則不同thread不能ren
                    Thread thread = new Thread(() =>
                    {
                        try
                        {
                            getPHEJSingleBitmapImageAsync(caTool, defaultKey, imagePath, PDFScale, singlePagePair.leftPageIndex, bd);
                        }
                        catch (Exception ex)
                        {
                            LogTool.Debug(ex);
                        }
                    });
                    thread.Name = PDFScale.ToString();

                    zoomeThread.Add(thread);
                }

                //string imagePath = bookPath + "\\" + hejMetadata.LImgList[curPageIndex].path;
                //if (hejMetadata.LImgList[curPageIndex].path.Contains("tryPageEnd")) //試閱書的最後一頁
                //    imagePath = hejMetadata.LImgList[curPageIndex].path;

                //bool imagePathExists = File.Exists(imagePath);
                //if (imagePathExists)
                //{
                //    //將 ren 好PDF event (改用Thread的方式ren)
                //    Border bd = GetBorderInReader();
                //    //因為ren PDF的參數需要用到現在的border, 故多帶一個參數否則不同thread不能ren
                //    Thread thread = new Thread(() => getPHEJSingleBitmapImageAsync(caTool, defaultKey, imagePath, PDFScale, curPageIndex, bd));
                //    thread.Name = PDFScale.ToString();

                //    //先加到List中, 抓最後面的那個數據以防亂點
                //    zoomeThread.Add(thread);
                //    //thread.Start();

                //    //BitmapImage imgSource = getPHEJSingleBitmapImage(caTool, defaultKey, imagePath, PDFScale);
                //    //SendImageSourceToZoomCanvas(curPageIndex, imgSource);
                //    //imgSource = null;
                //}
            }
            else if (viewStatusIndex.Equals(PageMode.DoublePage))
            {
                Border bd = GetBorderInReader();
                ReadPagePair item = doubleReadPagePair[curPageIndex];
                if (item.rightPageIndex == -1)
                {
                    //封面或封底
                    bool imagePathExists = File.Exists(item.leftImagePath);
                    if (imagePathExists)
                    {
                        //將 ren 好PDF event (改用Thread的方式ren)
                        //this.imageSourceRendered += ReadWindow_imageSourceRendered;
                        //因為ren PDF的參數需要用到現在的border, 故多帶一個參數否則不同thread不能ren
                        Thread thread = new Thread(() => getPHEJSingleBitmapImageAsync(caTool, defaultKey, item.leftImagePath, PDFScale, curPageIndex, bd));
                        thread.Name = PDFScale.ToString();

                        //先加到List中, 抓最後面的那個數據以防亂點
                        zoomeThread.Add(thread);
                    }
                }
                else
                {
                    //雙頁

                    string leftImagePath = item.leftImagePath;
                    string rightImagePath = item.rightImagePath;
                    if (File.Exists(leftImagePath) && File.Exists(rightImagePath))
                    {
                        //將 ren 好PDF event (改用Thread的方式ren)
                        //this.imageSourceRendered += ReadWindow_imageSourceRendered;
                        //因為ren PDF的參數需要用到現在的border, 故多帶一個參數否則不同thread不能ren
                        Thread thread = new Thread(() => getPHEJDoubleBitmapImageAsync(caTool, defaultKey, leftImagePath, rightImagePath, PDFScale, curPageIndex, bd));
                        thread.Name = PDFScale.ToString();

                        //先加到List中, 抓最後面的那個數據以防亂點
                        zoomeThread.Add(thread);
                    }
                }

                //Border bd = GetBorderInReader();
                //int doubleIndex = curPageIndex;
                //doubleIndex = getSingleCurPageIndex(doubleIndex);
                //if (trialPages != 0 && (singleThumbnailImageAndPageList.Count % 2) == 1
                //    && (singleThumbnailImageAndPageList.Count - hejMetadata.pagesBeforeFirstPage) == doubleIndex)
                //{
                //    //特別針對試閱書中的基數頁書
                //    int leftCurPageIndex = doubleIndex - 1;
                //    int rightCurPageIndex = doubleIndex;
                //    if (hejMetadata.direction.Equals("right"))
                //    {
                //        leftCurPageIndex = doubleIndex;
                //        rightCurPageIndex = doubleIndex - 1;
                //    }
                //    string leftImagePath = bookPath + "\\" + hejMetadata.LImgList[leftCurPageIndex].path;
                //    string rightImagePath = bookPath + "\\" + hejMetadata.LImgList[rightCurPageIndex].path;
                //    if (hejMetadata.LImgList[leftCurPageIndex].path.Contains("tryPageEnd")) //試閱書的最後一頁                       
                //        leftImagePath = hejMetadata.LImgList[leftCurPageIndex].path;

                //    if (hejMetadata.LImgList[rightCurPageIndex].path.Contains("tryPageEnd")) //試閱書的最後一頁
                //        rightImagePath = hejMetadata.LImgList[rightCurPageIndex].path;


                //    if (File.Exists(leftImagePath) && File.Exists(rightImagePath))
                //    {
                //        //將 ren 好PDF event (改用Thread的方式ren)
                //        //this.imageSourceRendered += ReadWindow_imageSourceRendered;
                //        //因為ren PDF的參數需要用到現在的border, 故多帶一個參數否則不同thread不能ren
                //        Thread thread = new Thread(() => getPHEJDoubleBitmapImageAsync(caTool, defaultKey, leftImagePath, rightImagePath, PDFScale, curPageIndex, bd));
                //        thread.Name = PDFScale.ToString();
                //        //先加到List中, 抓最後面的那個數據以防亂點
                //        zoomeThread.Add(thread);

                //        //BitmapImage mergeImgSource = getPHEJDoubleBitmapImage(caTool, defaultKey, leftImagePath, rightImagePath, PDFScale);
                //        //SendImageSourceToZoomCanvas(curPageIndex, mergeImgSource);
                //        //mergeImgSource = null;
                //    }
                //}
                //else if (doubleIndex.Equals(0) || doubleIndex.Equals(singleThumbnailImageAndPageList.Count - 1))
                //{
                //    string imagePath = bookPath + "\\" + hejMetadata.LImgList[doubleIndex].path;
                //    if (hejMetadata.LImgList[doubleIndex].path.Contains("tryPageEnd")) //試閱書的最後一頁
                //        imagePath = hejMetadata.LImgList[doubleIndex].path;

                //    bool imagePathExists = File.Exists(imagePath);
                //    if (imagePathExists)
                //    {
                //        //將 ren 好PDF event (改用Thread的方式ren)
                //        //this.imageSourceRendered += ReadWindow_imageSourceRendered;
                //        //因為ren PDF的參數需要用到現在的border, 故多帶一個參數否則不同thread不能ren
                //        Thread thread = new Thread(() => getPHEJSingleBitmapImageAsync(caTool, defaultKey, imagePath, PDFScale, curPageIndex, bd));
                //        thread.Name = PDFScale.ToString();

                //        //先加到List中, 抓最後面的那個數據以防亂點
                //        zoomeThread.Add(thread);
                //    }
                //}
                //else
                //{
                //    //推算雙頁是哪兩頁的組合
                //    int leftCurPageIndex = doubleIndex - 1;
                //    int rightCurPageIndex = doubleIndex;
                //    if (hejMetadata.direction.Equals("right"))
                //    {
                //        leftCurPageIndex = doubleIndex;
                //        rightCurPageIndex = doubleIndex - 1;
                //    }
                //    string leftImagePath = bookPath + "\\" + hejMetadata.LImgList[leftCurPageIndex].path;
                //    string rightImagePath = bookPath + "\\" + hejMetadata.LImgList[rightCurPageIndex].path;
                //    if (hejMetadata.LImgList[leftCurPageIndex].path.Contains("tryPageEnd")) //試閱書的最後一頁                        
                //        leftImagePath = hejMetadata.LImgList[leftCurPageIndex].path;
                //    if (hejMetadata.LImgList[rightCurPageIndex].path.Contains("tryPageEnd")) //試閱書的最後一頁                         
                //        rightImagePath = hejMetadata.LImgList[rightCurPageIndex].path;

                //    if (File.Exists(leftImagePath) && File.Exists(rightImagePath))
                //    {
                //        //將 ren 好PDF event (改用Thread的方式ren)
                //        //this.imageSourceRendered += ReadWindow_imageSourceRendered;
                //        //因為ren PDF的參數需要用到現在的border, 故多帶一個參數否則不同thread不能ren
                //        Thread thread = new Thread(() => getPHEJDoubleBitmapImageAsync(caTool, defaultKey, leftImagePath, rightImagePath, PDFScale, curPageIndex, bd));
                //        thread.Name = PDFScale.ToString();

                //        //先加到List中, 抓最後面的那個數據以防亂點
                //        zoomeThread.Add(thread);
                //    }
                //}
            }
            else
            {  //其他多頁模式，暫不支援
                return;
            }

            //if (!checkImageStatusTimer.IsEnabled)
            //{
            //    checkImageStatusTimer.IsEnabled = true;
            //    checkImageStatusTimer.Start();
            //}
            //}
        }

        //ren 好PDFsource後此事件會偵聽到
        //Wayne Mark
        //Show pdf to image 
        void ReadWindow_imageSourceRendered(object sender, imageSourceRenderedResultEventArgs e)
        {
            this.imageSourceRendered -= ReadWindow_imageSourceRendered;

            isPDFRendering = false;
            //確定是同一頁, 且為不同倍率才換掉圖片
            if (curPageIndex.Equals(e.renderPageIndex))
            {
                if (PDFScale.Equals(e.sourceScale))
                {
                    //if (viewStatusIndex.Equals(PageMode.SinglePage))
                    //{  
                    //    //單頁模式
                    //    if (singleImgStatus[myIndex] != ImageStatus.LARGEIMAGE && singleImgStatus[myIndex] != ImageStatus.GENERATING)
                    //    { }
                    //}
                    //else if (viewStatusIndex.Equals(PageMode.DoublePage))
                    //{
                    //    Border bd = GetBorderInReader();
                    //    ReadPagePair item = doubleReadPagePair[curPageIndex];
                    //    if (item.rightPageIndex == -1)
                    //    {
                    //        //封面或封底
                    //    }
                    //    else
                    //    {
                    //        //雙頁
                    //    }
                    //}


                    BitmapImage imgSource = e.imgSource;
                    setImgSource(imgSource, e.sourceScale);
                }
                else
                {
                    for (int i = zoomeThread.Count - 1; i >= 0; i--)
                    {
                        if (PDFScale.Equals(((float)Convert.ToDouble(zoomeThread[i].Name))))
                        {
                            try
                            {
                                zoomeThread[i].Start();
                                this.imageSourceRendered += ReadWindow_imageSourceRendered;
                                isPDFRendering = true;
                                break;
                            }
                            catch
                            {
                                //該Thread執行中, 抓下一個Thread測試
                                continue;
                            }
                        }
                    }
                }
            }
            else
            {
                isPDFRendering = false;
                zoomeThread.Clear();
            }
            e.imgSource = null;
        }

        //由於Canvas和ren好的Source 在不同的Thread, 必須用Dispatcher才有辦法換 User Thread上的東西
        private void setImgSource(BitmapImage imgSource, float pdfScale)
        {
            setImgSourceCallback setImgCallBack = new setImgSourceCallback(setImgSourceDelegate);
            this.Dispatcher.Invoke(setImgCallBack, imgSource, pdfScale);
        }

        private delegate void setImgSourceCallback(BitmapImage imgSource, float pdfScale);
        private void setImgSourceDelegate(BitmapImage imgSource, float pdfScale)
        {
            useOriginalCanvasOnLockStatus = false;
            SendImageSourceToZoomCanvas(imgSource);
            Debug.WriteLine("SendImageSourceToZoomCanvas@setImgSourceDelegate");
            zoomeThread.Clear();
            isPDFRendering = false;
            imgSource = null;
        }

        private void ZoomImage(double imageScale, double scaleMaxOrMin, bool Maximum, bool isSlide)
        {
            //return;
            //如果是PDF，重ren pdf並貼上canvas
            if (bookType.Equals(BookType.PHEJ))
            {
                RepaintPDF(imageScale);
            }

            imageZoom(imageScale, scaleMaxOrMin, Maximum, isSlide);
            // Can lock note panel
            hyperlinkZoom(imageScale, scaleMaxOrMin, Maximum, isSlide);

            if (this.needToSendBroadCast)
            {
                StackPanel stackPanel = (StackPanel)this.GetImageInReader();
                TranslateTransform translateTransform1 = (TranslateTransform)this.tfgForImage.Children[1];
                ScaleTransform scaleTransform = (ScaleTransform)this.tfgForImage.Children[0];
                double num1 = Math.Abs(stackPanel.ActualWidth * scaleTransform.ScaleX - this.ActualWidth * this.ratio) / 2.0;
                double num2 = Math.Abs(stackPanel.ActualHeight * scaleTransform.ScaleY - this.ActualHeight * this.ratio) / 2.0 / num1;
                double num3 = 0.5;
                double num4 = 0.5;
                TranslateTransform translateTransform2 = (TranslateTransform)this.tfgForHyperLink.Children[1];
                translateTransform2.X = num3;
                translateTransform2.Y = 0.0;
                TranslateTransform translateTransform3 = (TranslateTransform)this.tfgForImage.Children[1];
                translateTransform3.X = num3;
                translateTransform3.Y = 0.0;

                //放大後將圖片設定為中心
                TranslateTransform ttHyperLink = (TranslateTransform)tfgForHyperLink.Children[1];
                ttHyperLink.X = 0;
                ttHyperLink.Y = 0;

                TranslateTransform tt = (TranslateTransform)tfgForImage.Children[1];
                tt.X = 0;
                tt.Y = 0;

                imageOrigin = new System.Windows.Point(tt.X, tt.Y);
                hyperlinkOrigin = new System.Windows.Point(ttHyperLink.X, ttHyperLink.Y);

                this.sendBroadCast("{\"x\":" + num3.ToString() + ",\"y\":" + num4.ToString() + ",\"scale\": " + imageScale.ToString() + ",\"cmd\":\"R.ZC\"}");
            }
            else
                this.needToSendBroadCast = true;

            //if (!checkImageStatusTimer.IsEnabled)
            //{
            //    checkImageStatusRetryTimes = 0;
            //    checkImageStatusTimer.IsEnabled = true;
            //    checkImageStatusTimer.Start();
            //}
        }

        private void hyperlinkZoom(double imageScale, double scaleMaxOrMin, bool Maximum, bool isSlide)
        {
            StackPanel img = (StackPanel)GetImageInReader();
            Border bd = GetBorderInReader();


            //Children[0]  是 ScaleTransform
            //Children[1]  是 TranslateTransform
            TranslateTransform ttHyperlink = (TranslateTransform)tfgForHyperLink.Children[1];
            ScaleTransform hyperlinkTransform = (ScaleTransform)tfgForHyperLink.Children[0];

            double originalScaleX = hyperlinkTransform.ScaleX;
            double originalScaleY = hyperlinkTransform.ScaleY;

            hyperlinkTransform.ScaleX = imageScale;
            hyperlinkTransform.ScaleY = imageScale;


            if (Maximum)
            {
                hyperlinkTransform.ScaleX = Math.Min(hyperlinkTransform.ScaleX, scaleMaxOrMin);
                hyperlinkTransform.ScaleY = Math.Min(hyperlinkTransform.ScaleY, scaleMaxOrMin);

                //hyperlinkTransform.ScaleX = Math.Min(imageScale, scaleMaxOrMin);
                //hyperlinkTransform.ScaleY = Math.Min(imageScale, scaleMaxOrMin);
            }
            else
            {
                hyperlinkTransform.ScaleX = Math.Max(hyperlinkTransform.ScaleX, scaleMaxOrMin);
                hyperlinkTransform.ScaleY = Math.Max(hyperlinkTransform.ScaleY, scaleMaxOrMin);

                //hyperlinkTransform.ScaleX = Math.Max(imageScale, scaleMaxOrMin);
                //hyperlinkTransform.ScaleY = Math.Max(imageScale, scaleMaxOrMin);
            }

            ttHyperlink.X = ttHyperlink.X - ttHyperlink.X * (originalScaleX - hyperlinkTransform.ScaleX);
            ttHyperlink.Y = ttHyperlink.Y - ttHyperlink.Y * (originalScaleY - hyperlinkTransform.ScaleY);


            ttHyperlink.X = Math.Min(ttHyperlink.X, 0);
            ttHyperlink.X = Math.Max(ttHyperlink.X, 0);

            ttHyperlink.Y = Math.Min(ttHyperlink.Y, 0);
            ttHyperlink.Y = Math.Max(ttHyperlink.Y, 0);

            //double currentImageShowHeight = 0;
            //double currentImageShowWidth = 0;
            //if (bookType.Equals(BookType.PHEJ))
            //{
            //    currentImageShowHeight = (int)((curPageSizeHeight / 72.0 * DpiX) * hyperlinkTransform.ScaleX * baseScale);
            //    currentImageShowWidth = (int)((curPageSizeWidth / 72.0 * DpiY) * hyperlinkTransform.ScaleY * baseScale);
            //    int doubleIndex = curPageIndex;
            //    doubleIndex = getSingleCurPageIndex(doubleIndex);
            //    if (viewStatus[doubleThumbnailImageAndPageList] && !doubleIndex.Equals(0) && !doubleIndex.Equals(singleThumbnailImageAndPageList.Count - 1))
            //    {
            //        currentImageShowWidth *= 2;
            //    }
            //}
            //else if (bookType.Equals(BookType.HEJ))
            //{
            //    currentImageShowHeight = bd.ActualHeight * hyperlinkTransform.ScaleX;
            //    currentImageShowWidth = img.ActualWidth * currentImageShowHeight / img.ActualHeight;

            //}

            //double tempWidth = currentImageShowWidth * (1 - originalScaleX / hyperlinkTransform.ScaleX) / 2;
            //double tempHeight = currentImageShowHeight * (1 - originalScaleY / hyperlinkTransform.ScaleY) / 2;

            //double ratioOfBounds = this.RestoreBounds.Height / this.ActualWidth;
            //double ratioOfImage = currentImageShowHeight / currentImageShowWidth;

            //System.Windows.Point totalMove = new System.Windows.Point(
            //    (ttHyperlink.X) * (1 - originalScaleX / hyperlinkTransform.ScaleX)
            //    , (ttHyperlink.Y) * (1 - originalScaleX / hyperlinkTransform.ScaleY));

            //ttHyperlink.X = - tempWidth;
            //ttHyperlink.Y = - tempHeight;


            //if (ratioOfBounds < ratioOfImage)
            //{
            //    if (currentImageShowWidth > this.ActualWidth * ratio)
            //    {
            //        ttHyperlink.X -= totalMove.X;
            //    }
            //}
            //else
            //{
            //    if (currentImageShowHeight > this.ActualHeight * ratio)
            //    {
            //        ttHyperlink.Y -= totalMove.Y;
            //    }
            //}

            // inkCanvasForDoublePage.RenderTransform = tfgForHyperLink;


            //TextBox tb = FindVisualChildByName<TextBox>(FR, "notePanel");

            //tb.Text = "123213123123;";
        }


        #endregion

        #region GetItems

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

        private Canvas GetStageCanvasInReader()
        {
            Canvas canvas = FindVisualChildByName<Canvas>(FR, "stageCanvas");
            return canvas;
        }

        private RadioButton GetMediaListButtonInReader()
        {
            RadioButton btn = FindVisualChildByName<RadioButton>(FR, "MediaListButton");
            return btn;
        }

        private Canvas GetMediaTableCanvasInReader()
        {
            Canvas canvas = FindVisualChildByName<Canvas>(FR, "MediaTableCanvas");
            return canvas;
        }

        private StackPanel GetMediaListPanelInReader()
        {
            StackPanel canvas = FindVisualChildByName<StackPanel>(FR, "mediaListPanel");
            return canvas;
        }

        private Border GetBorderInReader()
        {
            Border border = FindVisualChildByName<Border>(FR, "PART_ContentHost");
            return border;
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

        #endregion

        #region Drag and Drop

        private double ratio = 0;
        private bool isSameScale = false;

        //滑鼠左鍵按下去event
        void ImageInReader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Border border = GetBorderInReader();
            start = e.GetPosition(border);

            if (e.ClickCount.Equals(2))
            {
                //點兩下
                //if (tt.X == 0 && tt.Y == 0 && imgTransform.ScaleX == 1 && imgTransform.ScaleY == 1)
                //{
                //    ZoomImage(1.3, 5, true);
                //    //to do 針對滑鼠的位置放大
                //    //var tmp = e.GetPosition(img);
                //}
                //else if (ttHyperLink.X == 0 && ttHyperLink.Y == 0 && hyperLinkTransform.ScaleX == 1 && hyperLinkTransform.ScaleY == 1)
                //{
                //    ZoomImage(1.3, 5, true);
                //    //to do 針對滑鼠的位置放大
                //    //var tmp = e.GetPosition(img);
                //}
                //else
                //{
                resetTransform();
                //}
                return;
            }
            else
            {
                if (sender is StackPanel)
                {
                    ((StackPanel)sender).MouseMove += ReadWindow_MouseMove;
                    ((StackPanel)sender).PreviewMouseLeftButtonUp += ReadWindow_PreviewMouseLeftButtonUp;
                }
                else if (sender is Canvas)
                {
                    ((Canvas)sender).MouseMove += ReadWindow_MouseMove;
                    ((Canvas)sender).PreviewMouseLeftButtonUp += ReadWindow_PreviewMouseLeftButtonUp;
                }
            }

            //e.Handled = true;
        }

        private void setTransformBetweenSingleAndDoublePage()
        {
            TranslateTransform tt = (TranslateTransform)tfgForImage.Children[1];
            tt.X = 0;

            TranslateTransform ttHyperLink = (TranslateTransform)tfgForHyperLink.Children[1];
            ttHyperLink.X = 0;
        }


        // wayne，這裡會影響換頁時重新放大縮小
        private void resetTransform()
        {


            TranslateTransform tt = (TranslateTransform)tfgForImage.Children[1];
            ScaleTransform imgTransform = (ScaleTransform)tfgForImage.Children[0];
            tt.X = 0;
            tt.Y = 0;
            imgTransform.ScaleX = 1;
            imgTransform.ScaleY = 1;

            TranslateTransform ttHyperLink = (TranslateTransform)tfgForHyperLink.Children[1];
            ScaleTransform hyperLinkTransform = (ScaleTransform)tfgForHyperLink.Children[0];
            ttHyperLink.X = 0;
            ttHyperLink.Y = 0;
            hyperLinkTransform.ScaleX = 1;
            hyperLinkTransform.ScaleY = 1;

            Slider sliderInReader = FindVisualChildByName<Slider>(FR, "SliderInReader");
            sliderInReader.ValueChanged -= SliderInReader_ValueChanged;
            sliderInReader.Value = imgTransform.ScaleY;
            sliderInReader.ValueChanged += SliderInReader_ValueChanged;

            LockButton.Visibility = Visibility.Visible;

            if (zoomStep != 0)
            {
                zoomStep = 0;
                ZoomImage(zoomStepScale[zoomStep], zoomStepScale[0], false, false);
                this.sendBroadCast("{\"x\":0.500000,\"y\":0.500000,\"scale\":1.000000,\"cmd\":\"R.ZC\"}");
                Debug.WriteLine("ZoomImage@resetTransform");
            }

        }

        void ReadWindow_MouseMove(object sender, MouseEventArgs e)
        {
            //System.Windows.Controls.Image img = GetImageInReader();
            StackPanel img = (StackPanel)GetImageInReader();
            TranslateTransform tt = (TranslateTransform)tfgForImage.Children[1];
            ScaleTransform imageTransform = (ScaleTransform)tfgForImage.Children[0];

            TranslateTransform ttHyperLink = (TranslateTransform)tfgForHyperLink.Children[1];
            ScaleTransform hyperLinkTransform = (ScaleTransform)tfgForHyperLink.Children[0];

            Border border = GetBorderInReader();

            Vector v = start - e.GetPosition(border);

            double ratioOfBounds = this.ActualHeight / this.ActualWidth;
            double ratioOfImage = img.ActualHeight / img.ActualWidth;

            if (e.LeftButton == MouseButtonState.Released)
            {
                //萬一已放開
                return;
            }

            moveImage(v);
        }

        private void moveImage(Vector v)
        {
            StackPanel img = (StackPanel)GetImageInReader();
            TranslateTransform tt = (TranslateTransform)tfgForImage.Children[1];
            ScaleTransform imageTransform = (ScaleTransform)tfgForImage.Children[0];

            TranslateTransform ttHyperLink = (TranslateTransform)tfgForHyperLink.Children[1];
            ScaleTransform hyperLinkTransform = (ScaleTransform)tfgForHyperLink.Children[0];

            if (hejMetadata.direction.Equals("right"))
            {
                v.X = -v.X;
            }


            double ratioOfBounds = this.ActualHeight / this.ActualWidth;
            double ratioOfImage = img.ActualHeight / img.ActualWidth;

            if (imageTransform.ScaleX != 1 && imageTransform.ScaleY != 1)
            {
                tt.X = imageOrigin.X - v.X;
                tt.Y = imageOrigin.Y - v.Y;

                if (ratioOfBounds < ratioOfImage)
                {
                    //高度相等
                    if (img.ActualWidth * imageTransform.ScaleX < this.ActualWidth * ratio)
                    {
                        //放大後的圖還小於視窗大小, 則X軸不用動
                        tt.X = 0;

                        tt.Y = Math.Min(tt.Y, (((Math.Abs(img.ActualHeight * imageTransform.ScaleY) - this.ActualHeight * ratio) / 2)));
                        tt.Y = Math.Max(tt.Y, -(((Math.Abs(img.ActualHeight * imageTransform.ScaleY) - this.ActualHeight * ratio) / 2)));
                    }
                    else
                    {
                        //放大後的圖大於視窗大小, 則X軸邊界為大圖減視窗/2
                        tt.X = Math.Min(tt.X, (((Math.Abs(img.ActualWidth * imageTransform.ScaleX) - this.ActualWidth * ratio)) / 2));
                        tt.X = Math.Max(tt.X, -(((Math.Abs(img.ActualWidth * imageTransform.ScaleX) - this.ActualWidth * ratio)) / 2));

                        tt.Y = Math.Min(tt.Y, (((Math.Abs(img.ActualHeight * imageTransform.ScaleY) - this.ActualHeight * ratio) / 2)));
                        tt.Y = Math.Max(tt.Y, -(((Math.Abs(img.ActualHeight * imageTransform.ScaleY) - this.ActualHeight * ratio) / 2)));
                    }
                }
                else
                {
                    //寬度相等
                    if (img.ActualHeight * imageTransform.ScaleY < this.ActualHeight * ratio)
                    {
                        //放大後的圖還小於視窗大小, 則X軸不用動
                        tt.Y = 0;

                        tt.X = Math.Min(tt.X, (((Math.Abs(img.ActualWidth * imageTransform.ScaleX) - this.ActualWidth * ratio) / 2)));
                        tt.X = Math.Max(tt.X, -(((Math.Abs(img.ActualWidth * imageTransform.ScaleX) - this.ActualWidth * ratio) / 2)));
                    }
                    else
                    {
                        //放大後的圖大於視窗大小, 則X軸邊界為大圖減視窗/2
                        tt.Y = Math.Min(tt.Y, (((Math.Abs(img.ActualHeight * imageTransform.ScaleY) - this.ActualHeight * ratio)) / 2));
                        tt.Y = Math.Max(tt.Y, -(((Math.Abs(img.ActualHeight * imageTransform.ScaleY) - this.ActualHeight * ratio)) / 2));

                        tt.X = Math.Min(tt.X, (((Math.Abs(img.ActualWidth * imageTransform.ScaleX) - this.ActualWidth * ratio) / 2)));
                        tt.X = Math.Max(tt.X, -(((Math.Abs(img.ActualWidth * imageTransform.ScaleX) - this.ActualWidth * ratio) / 2)));
                    }
                }
            }
            else
            {
                //原大小, 先不要移動
                //tt.X = moveImage.X - v.X;
                //tt.Y = 0;
            }

            //感應框以及圖片的倍率
            Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");

            double imageAndHyperLinkRatio = zoomCanvas.Height / img.ActualHeight;

            ////讓感應框以等倍率移動
            ttHyperLink.X = tt.X * imageAndHyperLinkRatio;
            ttHyperLink.Y = tt.Y * imageAndHyperLinkRatio;

            if (hejMetadata.direction.Equals("right"))
            {
                ttHyperLink.X = (-tt.X) * imageAndHyperLinkRatio;
            }
        }

        #endregion

        #region 推文

        private StackPanel toShareBook()
        {
            StackPanel sp = new StackPanel();
            List<ShareButton> sharePlatForm = new List<ShareButton>() 
			{
				new ShareButton("Assets/ReadWindow/icon_f.png","Facebook",SharedPlatform.Facebook, true),
				new ShareButton("Assets/ReadWindow/icon_p.png","Plurk",SharedPlatform.Plurk, false),
				new ShareButton("Assets/ReadWindow/icon_m.png","Mail",SharedPlatform.Mail, false),
				new ShareButton("Assets/ReadWindow/icon_g.png","Google+",SharedPlatform.Google, false),
				new ShareButton("Assets/ReadWindow/icon_t.png","Twitter",SharedPlatform.Twitter, false)
			};

            ListBox lb = new ListBox();
            lb.Style = (Style)FindResource("ShareListBoxStyle");
            lb.ItemsSource = sharePlatForm;

            sp.Children.Add(lb);
            return sp;
        }

        private int allowedSharedTimes = 10;
        void sharePlatformButton_Click(object sender, RoutedEventArgs e)
        {
            SharedPlatform whichPlatform = (SharedPlatform)(((RadioButton)sender).Tag);
            //Todo: 判斷是否有連線?

            StartSharing(whichPlatform);
        }

        private void StartSharing(SharedPlatform whichPlatform)
        {
            string webResponseString = getTweetData(whichPlatform);
            if (!webResponseString.Equals(""))
            {
                if (checkIfSharedTooMuch())
                {
                    BookThumbnail bt = (BookThumbnail)selectedBook;
                    //可推文
                    if (whichPlatform.Equals(SharedPlatform.Facebook))
                    {
                        string strURL = "http://www.facebook.com/sharer/sharer.php?u=" + System.Uri.EscapeDataString(webResponseString);
                        Process.Start(strURL);
                    }
                    else if (whichPlatform.Equals(SharedPlatform.Plurk))
                    {
                        //string strURL = "";
                        //if (bookType.Equals(BookType.HEJ))
                        //{
                        //    strURL = "http://www.plurk.com/?qualifier=shares&status=" + System.Uri.EscapeDataString(webResponseString) + " (" + System.Uri.EscapeDataString(RM.GetString("String306") + "【" + Label_title.Text + "】" + RM.GetString("String303") + "【" + RM.GetString("String307") + ": " + sharePage + "】" + RM.GetString("String304") + " ") + ")";

                        //}
                        //else
                        //{
                        //    strURL = "http://www.plurk.com/?qualifier=shares&status=" + System.Uri.EscapeDataString(webResponseString) + " (" + System.Uri.EscapeDataString(RM.GetString("String306") + "【" + Label_title.Text + "】" + RM.GetString("String303") + " P." + sharePage + RM.GetString("String304") + " ") + ")";

                        //}
                        //Process.Start(strURL);
                    }
                    else if (whichPlatform.Equals(SharedPlatform.Mail))
                    {
                        string strSub = "";
                        string strBody = "";

                        //strSub = "推薦【" + bt.title + "】這本電子書 P." + (curPageIndex + 1).ToString() + " 給您";
                        //strBody = "我正在閱讀【" + bt.title + "】這本電子書 P." + (curPageIndex + 1).ToString() + "推薦給您,歡迎您也一起來閱讀。";

                        strSub = langMng.getLangString("recommend") + "【" + bt.title + "】" + langMng.getLangString("thisEBook") + " P." + (curPageIndex + 1).ToString() + " " + langMng.getLangString("forYou");
                        strBody = langMng.getLangString("imReading") + "【" + bt.title + "】" + langMng.getLangString("thisEBook") + " P." + (curPageIndex + 1).ToString() + langMng.getLangString("recommend") + langMng.getLangString("forYou") + langMng.getLangString("welcomeToReader");

                        if (bookType.Equals(BookType.EPUB))
                        {
                            //strSub = RM.GetString("String302") + "【" + Label_title.Text + "】" + RM.GetString("String303") + "【" + RM.GetString("String307") + ": " + sharePage + "】" + RM.GetString("String304");
                            //strBody = RM.GetString("String305") + "【" + Label_title.Text + "】" + RM.GetString("String303") + "【" + RM.GetString("String307") + ": " + sharePage + "】" + RM.GetString("String306");

                        }
                        else if (bookType.Equals(BookType.HEJ) || bookType.Equals(BookType.PHEJ))
                        {
                            //strSub = RM.GetString("String302") + "【" + Label_title.Text + "】" + RM.GetString("String303") + " P." + sharePage + " " + RM.GetString("String304");
                            //strBody = RM.GetString("String305") + "【" + Label_title.Text + "】" + RM.GetString("String303") + " P." + sharePage + " " + RM.GetString("String306");
                        }

                        strBody = strBody + "%0A";
                        strBody = strBody + System.Uri.EscapeDataString(webResponseString);
                        strBody = strBody + "%0A" + " ";

                        //要用誰寄?
                        string emailAddress = "";
                        Process.Start("mailto://" + emailAddress + "?subject=" + strSub + "&body="
                          + strBody);
                    }
                    else if (whichPlatform.Equals(SharedPlatform.Google))
                    {
                    }
                    else if (whichPlatform.Equals(SharedPlatform.Twitter))
                    {
                    }

                    bt = null;
                }
                else
                {
                    //超過規定的次數
                    //MsgBox("本書推文已達allowedSharedTimes次限制，無法推文")
                }
            }
            else
            {
                //沒有取得值
            }
        }

        private string getTweetData(SharedPlatform platform)
        {
            //ebookType: 1. epub 2. hej 3. phej 
            string postURL = "http://openebook.hyread.com.tw/tweetservice/rest/BookInfo/add";

            XmlDocument shareInfoDoc = new XmlDocument();
            XMLTool xmlTools = new XMLTool();
            shareInfoDoc.LoadXml("<body></body>");

            BookThumbnail bt = (BookThumbnail)selectedBook;

            try
            {
                ////hej, phej
                if (bookType.Equals(BookType.HEJ) || bookType.Equals(BookType.PHEJ))
                {
                    xmlTools.appendChildToXML("unit", bt.vendorId, shareInfoDoc);
                    xmlTools.appendChildToXML("type", platform.GetHashCode().ToString(), shareInfoDoc);
                    xmlTools.appendChildToXML("bookid", bt.bookID, shareInfoDoc);
                    xmlTools.appendCDATAChildToXML("title", bt.title, shareInfoDoc);
                    xmlTools.appendCDATAChildToXML("author", bt.author, shareInfoDoc);
                    xmlTools.appendCDATAChildToXML("publisher", bt.publisher, shareInfoDoc);
                    xmlTools.appendChildToXML("publishdate", bt.publishDate.Replace("/", "-"), shareInfoDoc);
                    xmlTools.appendChildToXML("pages", bt.totalPages.ToString(), shareInfoDoc);
                    xmlTools.appendChildToXML("size", "123456", shareInfoDoc);
                    xmlTools.appendChildToXML("direction", hejMetadata.direction, shareInfoDoc);
                    xmlTools.appendChildToXML("comment", "", shareInfoDoc);
                    xmlTools.appendChildToXML("page", (curPageIndex + 1).ToString(), shareInfoDoc);
                    xmlTools.appendChildToXML("userid", bt.userId, shareInfoDoc);
                    xmlTools.appendChildToXML("username", bt.userId, shareInfoDoc);
                    xmlTools.appendChildToXML("email", "", shareInfoDoc);
                    xmlTools.appendChildToXML("comment", "", shareInfoDoc);
                }
            }
            catch
            {
            }

            try
            {
                //取圖片
                string coverPath = bookPath + "\\" + hejMetadata.SImgList[0].path;
                byte[] coverFileArray = getByteArrayFromImage(new BitmapImage(new Uri(coverPath)));
                xmlTools.appendCDATAChildToXML("coverpic", Convert.ToBase64String(coverFileArray), shareInfoDoc);

                //byte[] penMemoStream = null;
                Bitmap image1 = null;
                if (File.Exists(bookPath + "/hyweb/strokes/" + hejMetadata.LImgList[curPageIndex].pageId + ".isf"))
                {
                    InkCanvas penCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");

                    InkCanvas penMemoCanvas = new InkCanvas();
                    penMemoCanvas.Background = System.Windows.Media.Brushes.Transparent;

                    FileStream fs = new FileStream(bookPath + "/hyweb/strokes/" + hejMetadata.LImgList[curPageIndex].pageId + ".isf",
                                            FileMode.Open);
                    penMemoCanvas.Strokes = new StrokeCollection(fs);
                    fs.Close();

                    // Get the size of canvas
                    System.Windows.Size size = new System.Windows.Size(penCanvas.Width, penCanvas.Height);
                    // Measure and arrange the surface
                    // VERY IMPORTANT
                    penMemoCanvas.Measure(size);
                    penMemoCanvas.Arrange(new Rect(size));

                    // Create a render bitmap and push the surface to it
                    RenderTargetBitmap renderBitmap =
                      new RenderTargetBitmap(
                        (int)size.Width,
                        (int)size.Height,
                         DpiX,
                         DpiY,
                        //(96 / DpiX),
                        //(96 / DpiY),
                        PixelFormats.Pbgra32);
                    renderBitmap.Render(penMemoCanvas);

                    // Create a file stream for saving image
                    using (MemoryStream memStream = new MemoryStream())
                    {
                        // Use png encoder for our data
                        PngBitmapEncoder encoder = new PngBitmapEncoder();
                        // push the rendered bitmap to it
                        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                        // save the data to the stream
                        encoder.Save(memStream);
                        image1 = new Bitmap(memStream);
                    }
                }

                string pagePath = bookPath + "\\" + hejMetadata.LImgList[curPageIndex].path;
                if (hejMetadata.LImgList[curPageIndex].path.Contains("tryPageEnd")) //試閱書的最後一頁
                    pagePath = hejMetadata.LImgList[curPageIndex].path;

                BitmapImage imgSource = null;
                if (bookType.Equals(BookType.HEJ))
                {
                    imgSource = getHEJSingleBitmapImage(caTool, defaultKey, pagePath, 1f);
                }
                else if (bookType.Equals(BookType.PHEJ))
                {
                    imgSource = getPHEJSingleBitmapImage(caTool, defaultKey, pagePath, 1f);
                }


                byte[] pageFileArray = getByteArrayFromImage(imgSource);

                imgSource = null;

                Bitmap bitmap = null;
                try
                {
                    //雙頁
                    Bitmap image2 = new Bitmap(new MemoryStream(pageFileArray));
                    //Bitmap image1 = null;
                    //if (penMemoStream != null)
                    //{
                    //    image1 = new Bitmap(new MemoryStream(penMemoStream));

                    //}

                    int width = Convert.ToInt32(image2.Width);
                    int height = Convert.ToInt32(image2.Height);

                    bitmap = new Bitmap(width, height);
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        g.DrawImage(image2, 0, 0, width, height);
                        if (image1 != null)
                        {
                            g.DrawImage(image1, 0, 0, width, height);
                        }
                        g.Dispose();
                    }

                    image1 = null;
                    image2 = null;

                    GC.Collect();
                }
                catch
                {
                    //處理圖片過程出錯
                }

                //resize
                Bitmap resizeBitmap = null;
                try
                {
                    int oriWidth = Convert.ToInt32(bitmap.Width);
                    int oriHeight = Convert.ToInt32(bitmap.Height);

                    double ratio = 1024 / oriWidth;

                    int width = 1024;
                    int height = (int)(oriHeight * ratio);
                    resizeBitmap = new Bitmap(width, height);
                    using (Graphics g = Graphics.FromImage(resizeBitmap))
                    {

                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        g.DrawImage(bitmap, 0, 0, width, height);
                        g.Dispose();
                    }
                    bitmap = null;
                }
                catch
                {
                    //resize錯誤
                }


                //resizeBitmap.Save("c:\\Temp\\test.bmp");
                byte[] imageFileArray = (byte[])TypeDescriptor.GetConverter(resizeBitmap).ConvertTo(resizeBitmap, typeof(byte[]));

                xmlTools.appendCDATAChildToXML("pagepic", Convert.ToBase64String(imageFileArray), shareInfoDoc);


                imageFileArray = null;
            }
            catch
            {
            }

            //HttpRequest request = new HttpRequest(Global.proxyMode, Global.proxyHttpPort);

            HttpRequest _request = new HttpRequest(configMng.saveProxyMode, configMng.saveProxyHttpPort);
            string result = _request.postXMLAndLoadString(postURL, shareInfoDoc);

            bt = null;

            return result;
        }

        private bool checkIfSharedTooMuch()
        {
            int curTimes = bookManager.getPostTimes(userBookSno);
            if (!curTimes.Equals(-1))
            {
                if (curTimes < allowedSharedTimes)
                {
                    curTimes++;
                    bookManager.savePostTimes(userBookSno, curTimes);
                    return true;
                }
                else
                {
                    //超過規定次數

                    //MessageBox.Show("每本書最多只能分享" + allowedSharedTimes + "頁", "注意!");
                    MessageBox.Show(langMng.getLangString("overShare") + allowedSharedTimes + langMng.getLangString("page"), langMng.getLangString("warning"));
                    return false;
                }
            }
            else
            {
                //存取出錯
                return false;
            }
        }

        #endregion

        #region 列印

        private void PrintButton_Checked(object sender, RoutedEventArgs e)
        {
            byte[] curKey = defaultKey;
            //只能單頁列印
            System.Windows.Controls.Image printImage = null;
            if (viewStatusIndex.Equals(PageMode.SinglePage))
            {
                int pageIndex = curPageIndex;

                string imagePath = bookPath + "\\" + hejMetadata.LImgList[pageIndex].path;
                if (hejMetadata.LImgList[pageIndex].path.Contains("tryPageEnd")) //試閱書的最後一頁
                    imagePath = hejMetadata.LImgList[pageIndex].path;

                if (File.Exists(imagePath))
                {
                    if (bookType.Equals(BookType.HEJ))
                    {
                        printImage = getSingleBigPageToReplace(caTool, curKey, imagePath);
                    }
                    else if (bookType.Equals(BookType.PHEJ))
                    {
                        printImage = getPHEJSingleBigPageToReplace(caTool, curKey, imagePath);
                    }
                    curKey = null;
                }
                else
                {
                    //此檔案不在
                }
            }
            //else if (viewStatus[doubleThumbnailImageAndPageList])
            //{
            //    int doubleIndex = curPageIndex;

            //    if (doubleIndex.Equals(0) || doubleIndex.Equals(doubleThumbnailImageAndPageList.Count - 1))
            //    {
            //        //封面或封底
            //        string imagePath = bookPath + "\\" + hejMetadata.LImgList[doubleIndex].path;
            //        if (File.Exists(imagePath))
            //        {
            //            if (bookType.Equals(BookType.HEJ))
            //            {
            //                printImage = getSingleBigPageToReplace(caTool, curKey, imagePath);
            //            }
            //            else if (bookType.Equals(BookType.PHEJ))
            //            {
            //                printImage = getPHEJSingleBigPageToReplace(caTool, curKey, imagePath);
            //            }
            //            curKey = null;
            //        }
            //        else
            //        {
            //            //此檔案不在
            //        }
            //    }
            //    else
            //    {
            //        doubleIndex = getSingleCurPageIndex(doubleIndex);

            //        //推算雙頁是哪兩頁的組合
            //        int leftCurPageIndex = doubleIndex - 1;
            //        int rightCurPageIndex = doubleIndex;

            //        if (hejMetadata.direction.Equals("right"))
            //        {
            //            leftCurPageIndex = doubleIndex;
            //            rightCurPageIndex = doubleIndex - 1;
            //        }
            //        string leftImagePath = bookPath + "\\" + hejMetadata.LImgList[leftCurPageIndex].path;
            //        string rightImagePath = bookPath + "\\" + hejMetadata.LImgList[rightCurPageIndex].path;

            //        if (File.Exists(leftImagePath) && File.Exists(rightImagePath))
            //        {
            //            if (bookType.Equals(BookType.HEJ))
            //            {
            //                printImage = getDoubleBigPageToReplace(caTool, curKey, leftImagePath, rightImagePath);
            //            }
            //            else if (bookType.Equals(BookType.PHEJ))
            //            {
            //                printImage = getPHEJDoubleBigPageToReplace(caTool, curKey, leftImagePath, rightImagePath);
            //            }
            //            curKey = null;
            //        }
            //        else
            //        {
            //            //其中有檔案尚未下載好
            //        }
            //    }
            //}
            if (printImage != null)
            {
                FixedDocument fd = new FixedDocument();
                PrintDialog pd = new PrintDialog();
                fd.DocumentPaginator.PageSize = new System.Windows.Size(pd.PrintableAreaWidth, pd.PrintableAreaHeight);


                FixedPage page1 = new FixedPage();

                if (viewStatusIndex.Equals(PageMode.SinglePage))
                {
                    page1.Width = pd.PrintableAreaWidth;
                    page1.Height = pd.PrintableAreaHeight;

                    printImage.Width = pd.PrintableAreaWidth;
                    printImage.Height = pd.PrintableAreaHeight;
                }
                else if (viewStatusIndex.Equals(PageMode.DoublePage))
                {
                    int doubleIndex = curPageIndex;

                    if (doubleIndex.Equals(0) || doubleIndex.Equals(doubleThumbnailImageAndPageList.Count - 1))
                    {
                        //封面或封底
                        page1.Width = pd.PrintableAreaWidth;
                        page1.Height = pd.PrintableAreaHeight;

                        printImage.Width = pd.PrintableAreaWidth;
                        printImage.Height = pd.PrintableAreaHeight;
                    }
                    else
                    {
                        page1.Width = pd.PrintableAreaHeight;
                        page1.Height = pd.PrintableAreaWidth;

                        printImage.Width = pd.PrintableAreaHeight;
                        printImage.Height = pd.PrintableAreaWidth;
                    }
                }

                page1.Children.Add(printImage);
                PageContent page1Content = new PageContent();
                ((IAddChild)page1Content).AddChild(page1);

                fd.Pages.Add(page1Content);
                DV.Document = fd;
                DV.Visibility = Visibility.Visible;
            }
            else
            {
                //沒有圖
            }

            printImage = null;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (DV.Visibility.Equals(Visibility.Visible))
            {
                DV.Visibility = Visibility.Collapsed;
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            //PrintDialog pd = new PrintDialog();

            //if (viewStatusIndex.Equals(PageMode.DoublePage))
            //{
            //    int doubleIndex = curPageIndex;
            //    if (doubleIndex.Equals(0) || doubleIndex.Equals(doubleThumbnailImageAndPageList.Count - 1))
            //    {
            //        //封面或封底       
            //        pd.PrintTicket.PageOrientation = PageOrientation.Portrait;
            //    }
            //    else
            //    {
            //        pd.PrintTicket.PageOrientation = PageOrientation.Landscape;
            //    }
            //}

            //pd.PrintDocument(DV.Document.DocumentPaginator, "");
        }


        #endregion

        #region 滑鼠控制
        private DateTime stopMovingMouseTime;

        private void FR_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;

            if (isSyncing == true && isSyncOwner == false)
                return;

            if (e.Delta.Equals(120))
            {
                //前滾
                if (zoomStep == zoomStepScale.Length - 1)
                    return;

                zoomStep++;
                ZoomImage(zoomStepScale[zoomStep], zoomStepScale[zoomStepScale.Length - 1], true, false);
            }
            else if (e.Delta.Equals(-120))
            {
                //後滾
                if (zoomStep == 0)
                    return;

                zoomStep--;
                ZoomImage(zoomStepScale[zoomStep], zoomStepScale[0], false, false);
            }

            stopMovingMouseTime = DateTime.Now;
        }

        #endregion

        #region 鍵盤控制

        private int keyboardMoveParam = 5;


        //Wayne 快速鍵
        private void FR_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            //20150615 增加
            //為了notePanel可以輸入字元拿掉。
            var mediaTable = GetMediaTableCanvasInReader();
            if (mediaTable.Visibility == Visibility.Visible)
                return;

            if (e.Key == Key.OemPlus)
            {
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    //前滾
                    if (zoomStep == zoomStepScale.Length - 1)
                    {
                        e.Handled = true;
                        return;
                    }

                    zoomStep++;
                    ZoomImage(zoomStepScale[zoomStep], zoomStepScale[zoomStepScale.Length - 1], true, false);
                }
            }

            TranslateTransform tt = (TranslateTransform)tfgForImage.Children[1];
            ScaleTransform imageTransform = (ScaleTransform)tfgForImage.Children[0];
            TranslateTransform ttHyperLink = (TranslateTransform)tfgForHyperLink.Children[1];
            ScaleTransform hyperLinkTransform = (ScaleTransform)tfgForHyperLink.Children[0];

            if (hyperLinkTransform.ScaleX > 1 && hyperLinkTransform.ScaleY > 0)
            {
                switch (e.Key)
                {
                    case Key.Left:
                        moveImage(new Vector(keyboardMoveParam * (-1), 0));
                        break;
                    case Key.Right:
                        moveImage(new Vector(keyboardMoveParam, 0));
                        break;
                    case Key.Up:
                        moveImage(new Vector(0, keyboardMoveParam * (-1)));
                        break;
                    case Key.Down:
                        moveImage(new Vector(0, keyboardMoveParam));
                        break;
                    case Key.OemMinus:
                        if (Keyboard.Modifiers == ModifierKeys.Control)
                        {
                            if (zoomStep == 0)
                                break;

                            zoomStep--;
                            ZoomImage(zoomStepScale[zoomStep], zoomStepScale[0], false, false);
                        }
                        break;
                    case Key.D0:
                        if (Keyboard.Modifiers == ModifierKeys.Control)
                        {
                            resetTransform();
                        }
                        break;
                    default:
                        break;
                }

                if (isSameScale)
                {
                    imageOrigin = new System.Windows.Point(tt.X, tt.Y);
                    hyperlinkOrigin = new System.Windows.Point(ttHyperLink.X, ttHyperLink.Y);
                }
                else
                {
                    tt.X = tt.Y = 0;
                    ttHyperLink.X = ttHyperLink.Y = 0;
                    isSameScale = true;
                }

                /*
                //20150615 註解掉
                //為了notePanel可以輸入字元拿掉。
                */
                e.Handled = true;
            }

        }

        #endregion

        #region Paperless Meeting

        private string _managerId = "ReadWindow";
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
            parseJSonFromMessage(msg);
        }


        private void parseJSonFromMessage(string message)
        {
            try
            {
                Dictionary<string, Object> msgJson = JsonConvert.DeserializeObject<Dictionary<string, Object>>(message);
                long reciveTime = (long)(SocketClient.GetCurrentTimeInUnixMillis() - (ulong)((long)msgJson["sendTime"]));

                try
                {
                    Debug.WriteLine(managerId == null ? "" : managerId + ":" + msgJson["msg"].ToString());
                }
                catch { }

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
                                bringBlockIntoView(1);
                                InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
                                string tmp = msgStrings["spline"].ToString();
                                setMsgToAction(msgStrings);
                                penMemoCanvas.Strokes.Clear();
                                drawStrokeFromJson(tmp);
                            }));
                        });


                    }
                }
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }

        }

        private string splineString = "";
        private bool closeBook = false;

        //由於和 msg 在不同的Thread, 必須用Dispatcher才有辦法換 User Thread上的東西
        private void setMsgToAction(Dictionary<string, Object> msgStrings)
        {
            setMsgToActionCallback setImgCallBack = new setMsgToActionCallback(setMsgToActionDelegate);
            this.Dispatcher.Invoke(setImgCallBack, msgStrings);
        }

        private delegate void setMsgToActionCallback(Dictionary<string, Object> msgStrings);
        private void setMsgToActionDelegate(Dictionary<string, Object> msgStrings)
        {
            try
            {
                if (!msgStrings.ContainsKey("cmd"))
                {
                    InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
                    //初始化Reader狀態
                    //{"msg":"{\"bookId\":null,\"pageIndex\":null,\"annotation\":null,\"bookmark\":null,\"spline\":null}","sender":"1401","sendTime":1369031567890,"cmd":"R.init"}
                    //{"bookId":null,"pageIndex":null,"annotation":null,"bookmark":null,"spline":null}
                    foreach (KeyValuePair<string, Object> initStatus in msgStrings)
                    {
                        if (initStatus.Value != null)
                        {
                            switch (initStatus.Key)
                            {
                                case "bookId":
                                    closeBook = true;
                                    break;
                                case "pageIndex":
                                    if (msgStrings["pageIndex"] != null)
                                    {
                                        string pageIndex = msgStrings["pageIndex"].ToString();
                                        bringBlockIntoView(Convert.ToInt32(pageIndex));
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
                            }
                        }
                    }

                }
                else
                {
                    string msgString = msgStrings["cmd"].ToString();
                    string pageIndex = "";
                    RadioButton NoteRB = FindVisualChildByName<RadioButton>(FR, "NoteButton");
                    RadioButton BookMarkRb = FindVisualChildByName<RadioButton>(FR, "BookMarkButton");
                    InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
                    switch (msgString)
                    {
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
                                loadCurrentStrokes(hejMetadata.LImgList[curPageIndex].pageId);
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
                        //翻頁 Turn Page
                        //{"msg":"{\"pageIndex\":0,\"cmd\":\"R.TP\"}","sender":"hyweb001","sendTime":1369031579,"cmd":"broadcast"}
                        case "R.TP":
                            try
                            {
                                pageIndex = msgStrings["pageIndex"].ToString();
                                bringBlockIntoView(Convert.ToInt32(pageIndex));
                            }
                            catch
                            {

                            }
                            break;
                        //隱藏工具列 Hidden Action, 1=true, 0=false
                        //{"msg":"{\"hide\":1,\"cmd\":\"R.HA\"}","sender":"hyweb001","sendTime":1369031579,"cmd":"broadcast"}
                        //case "R.HA":
                        //    string hide = msgStrings["hide"].ToString();
                        //    bool isHiding = false;
                        //    if (hide.Equals("1"))
                        //        isHiding = true;
                        //    if (isHiding.Equals(isFullScreenButtonClick))
                        //    {
                        //        return;
                        //    }
                        //    //isFullScreenButtonClick = isHiding;
                        //    hideOrShowToolBar();
                        //    break;
                        //設定書籤 Set Bookmark, 1=true, 0=false
                        //{"msg":"{\"bookmark\":1,\"pageIndex\":0,\"cmd\":\"R.SB\"}","sender":"hyweb001","sendTime":1369031579,"cmd":"broadcast"}
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
                            //doUpperRadioButtonClicked(MediaCanvasOpenedBy.NoteButton, NoteRB);
                            break;
                        //設定註記文字 Set Annotation
                        //{"msg":"{\"annotation\":\"註記文字\",\"pageIndex\":0,\"cmd\":\"R.SA\"}","sender":"hyweb001","sendTime":1369031585,"cmd":"broadcast"}
                        case "R.SA":
                            pageIndex = msgStrings["pageIndex"].ToString();
                            string annotation = msgStrings["annotation"].ToString();
                            TextBox tb = FindVisualChildByName<TextBox>(FR, "notePanel");
                            annotation = annotation.Replace("\\n", "\n").Replace("\\t", "\t");
                            tb.Text = annotation;
                            int targetPageIndex = Convert.ToInt32(pageIndex);
                            NoteData nd = new NoteData() { bookid = this.bookId, text = annotation, index = targetPageIndex, status = "0" };


                            bookNoteDictionary[targetPageIndex] = nd;
                            if (tb.Text.Equals(""))
                            {
                                NoteRB.IsChecked = false;
                                TriggerBookMark_NoteButtonOrElse(NoteRB);
                            }
                            else
                            {
                                NoteRB.IsChecked = true;
                                TriggerBookMark_NoteButtonOrElse(NoteRB);
                            }
                            break;
                        //關閉對話框 Dismiss Popover Action
                        //{"msg":"{\"cmd\":\"R.DPA\"}","sender":"hyweb001","sendTime":1369031588,"cmd":"broadcast"}
                        case "R.DPA":
                            Canvas MediaTableCanvas = GetMediaTableCanvasInReader();
                            if (MediaTableCanvas.Visibility.Equals(Visibility.Visible))
                            {
                                doUpperRadioButtonClicked(MediaCanvasOpenedBy.NoteButton, NoteRB);
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
                            bringBlockIntoView(Convert.ToInt32(pageIndex));
                            try
                            {
                                drawStrokeFromJson(msgStrings["spline"].ToString());
                            }
                            catch
                            {
                                //spline為null
                                break;
                            }
                            break;


                        //放大或縮小 Zoom Change with Offset(x,y) at Scale  [x, y is point of size]
                        //{"msg":"{\"x\":0.703125,\"y\":0.816406,\"scale\":2.518765,\"cmd\":\"R.ZC\"}","sender":"garypan2","sendTime":1370335077,"cmd":"broadcast"}
                        //放大後的位移 Zoom Change with Offset(x,y) at Scale(-1)
                        //{"msg":"{\"x\":0.571289,\"y\":0.669271,\"scale\":-1.000000,\"cmd\":\"R.ZC\"}","sender":"garypan2","sendTime":1370335102,"cmd":"broadcast"}
                        case "R.ZC":
                            string scale = msgStrings["scale"].ToString();
                            string moveX = msgStrings["x"].ToString();
                            string moveY = msgStrings["y"].ToString();

                            double dbMoveX = Convert.ToDouble(moveX);
                            double dbMoveY = Convert.ToDouble(moveY);

                            double dbScale = -1;
                            if (!scale.Equals("-1"))
                            {
                                //放大
                                dbScale = Convert.ToDouble(scale);

                                //為配合iOS要除以0.75
                                //dbScale = dbScale / 0.75;

                                bool isZoomed = false;
                                int index = -1;
                                for (int i = 0; i < zoomStepScale.Length; i++)
                                {
                                    if (Math.Abs(zoomStepScale[i] - dbScale) < 0.25)
                                    {
                                        dbScale = zoomStepScale[i];
                                        index = i;
                                        isZoomed = true;
                                        break;
                                    }
                                }
                                if (!isZoomed)
                                {
                                    //超過zoomStepScale的大小
                                    dbScale = Math.Min(3, dbScale);
                                    dbScale = Math.Max(1, dbScale);
                                    if (dbScale == 3)
                                    {
                                        index = zoomStepScale.Length - 1;
                                    }
                                    else if (dbScale == 1)
                                    {
                                        index = 0;
                                    }
                                }
                                bool isLarger = true;
                                if (index == zoomStep)
                                {
                                }
                                else
                                {
                                    if (index < zoomStep)
                                    {
                                        //變小
                                        isLarger = false;
                                    }

                                    zoomStep = index;

                                    if (isLarger)
                                    {
                                        ZoomImage(dbScale, zoomStepScale[zoomStepScale.Length - 1], true, false);
                                    }
                                    else
                                    {
                                        ZoomImage(dbScale, zoomStepScale[0], false, false);
                                        resetTransform();
                                    }
                                }
                            }
                            if (!PDFScale.Equals(1))
                            {
                                StackPanel img = (StackPanel)GetImageInReader();
                                TranslateTransform tt = (TranslateTransform)tfgForImage.Children[1];
                                TranslateTransform ttHyperLink = (TranslateTransform)tfgForHyperLink.Children[1];

                                //double offsetXFromleft = dbMoveX;
                                //double offsetYFromleft = dbMoveY;

                                //tt.X=offsetXFromleft- 

                                //tt.X += dbMoveX * 10;
                                //tt.Y += dbMoveY * 10;

                                if (dbScale.Equals(-1))
                                {
                                    dbScale = (double)PDFScale;
                                }

                                //切換中心點與左上角點的不同
                                tt.X = (0.5 - dbMoveX) * img.ActualWidth * dbScale;
                                tt.Y = (0.5 - dbMoveY) * img.ActualHeight * dbScale;


                                if (img.ActualWidth * dbScale < this.ActualWidth * ratio)
                                {
                                    //放大後的圖還小於視窗大小, 則X軸不用動
                                    tt.X = 0;

                                    tt.Y = Math.Min(tt.Y, (((Math.Abs(img.ActualHeight * dbScale) - this.ActualHeight * ratio) / 2)));
                                    tt.Y = Math.Max(tt.Y, -(((Math.Abs(img.ActualHeight * dbScale) - this.ActualHeight * ratio) / 2)));
                                }
                                else
                                {
                                    //放大後的圖大於視窗大小, 則X軸邊界為大圖減視窗/2
                                    tt.X = Math.Min(tt.X, (((Math.Abs(img.ActualWidth * dbScale) - this.ActualWidth * ratio)) / 2));
                                    tt.X = Math.Max(tt.X, -(((Math.Abs(img.ActualWidth * dbScale) - this.ActualWidth * ratio)) / 2));

                                    tt.Y = Math.Min(tt.Y, (((Math.Abs(img.ActualHeight * dbScale) - this.ActualHeight * ratio) / 2)));
                                    tt.Y = Math.Max(tt.Y, -(((Math.Abs(img.ActualHeight * dbScale) - this.ActualHeight * ratio) / 2)));
                                }

                                Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");

                                double imageAndHyperLinkRatio = zoomCanvas.Height / img.ActualHeight;

                                ////讓感應框以等倍率移動
                                ttHyperLink.X = tt.X * imageAndHyperLinkRatio;
                                ttHyperLink.Y = tt.Y * imageAndHyperLinkRatio;

                                if (hejMetadata.direction.Equals("right"))
                                {
                                    ttHyperLink.X = (-tt.X) * imageAndHyperLinkRatio;
                                }
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
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


        //控制按鈕在同步時是否可用
        private void buttonStatusWhenSyncing(Visibility toolBarVisibility, Visibility syncCanvasVisibility)
        {
            ReadWindow.FindVisualChildByName<Canvas>((DependencyObject)this.FR, "toolbarSyncCanvas").Visibility = toolBarVisibility;
            ReadWindow.FindVisualChildByName<Canvas>((DependencyObject)this.FR, "syncCanvas").Visibility = syncCanvasVisibility;

            ComboBox cbBooks = ReadWindow.FindVisualChildByName<ComboBox>((DependencyObject)this.FR, "cbBooks");
            RadioButton visualChildByName1 = ReadWindow.FindVisualChildByName<RadioButton>((DependencyObject)this.FR, "SearchButton");
            RadioButton visualChildByName2 = ReadWindow.FindVisualChildByName<RadioButton>((DependencyObject)this.FR, "PenMemoButton");
            RadioButton visualChildByName3 = ReadWindow.FindVisualChildByName<RadioButton>((DependencyObject)this.FR, "BookMarkButton");
            RadioButton visualChildByName4 = ReadWindow.FindVisualChildByName<RadioButton>((DependencyObject)this.FR, "NoteButton");
            RadioButton visualChildByName5 = ReadWindow.FindVisualChildByName<RadioButton>((DependencyObject)this.FR, "ShareButton");
            RadioButton visualChildByName6 = ReadWindow.FindVisualChildByName<RadioButton>((DependencyObject)this.FR, "BackToBookShelfButton");
            RadioButton visualChildByName7 = ReadWindow.FindVisualChildByName<RadioButton>((DependencyObject)this.FR, "leftPageButton");
            RadioButton visualChildByName8 = ReadWindow.FindVisualChildByName<RadioButton>((DependencyObject)this.FR, "rightPageButton");

            RadioButton statusBMK = ReadWindow.FindVisualChildByName<RadioButton>((DependencyObject)this.FR, "statusBMK");
            RadioButton statusMemo = ReadWindow.FindVisualChildByName<RadioButton>((DependencyObject)this.FR, "statusMemo");

            if (toolBarVisibility.Equals((object)Visibility.Visible) && syncCanvasVisibility.Equals((object)Visibility.Visible))
            {
                cbBooks.Opacity = 0.5;
                visualChildByName1.Opacity = 0.5;
                visualChildByName2.Opacity = 0.5;
                visualChildByName3.Opacity = 0.5;
                visualChildByName4.Opacity = 0.5;
                visualChildByName5.Opacity = 0.5;
                visualChildByName6.Opacity = 0.5;
                this.LockButton.Opacity = 0.0;
                visualChildByName7.Opacity = 0.5;
                visualChildByName8.Opacity = 0.5;
                this.ShowListBoxButton.Visibility = Visibility.Collapsed;

                if (PaperLess_Emeeting.Properties.Settings.Default.IsFlatUIReader == true)
                {
                    if (CheckIsNowClick(MemoSP) == true)
                    {
                        noteButton_Click();
                    }
                    NewUITop.Visibility = Visibility.Collapsed;
                    NewUI.Visibility = Visibility.Collapsed;


                    if (statusBMK != null)
                    {
                        statusBMK.Width = 0;
                        statusBMK.Height = 0;
                    }

                    if (statusMemo != null)
                    {
                        statusMemo.Width = 0;
                        statusMemo.Height = 0;
                    }

                }
            }
            else
            {
                cbBooks.Opacity = 1.0;
                visualChildByName1.Opacity = 1.0;
                visualChildByName2.Opacity = 1.0;
                visualChildByName3.Opacity = 1.0;
                visualChildByName4.Opacity = 1.0;
                visualChildByName5.Opacity = 1.0;
                visualChildByName6.Opacity = 1.0;
                this.LockButton.Opacity = 1.0;
                visualChildByName7.Opacity = 1.0;
                visualChildByName8.Opacity = 1.0;
                this.ShowListBoxButton.Visibility = Visibility.Visible;

                if (PaperLess_Emeeting.Properties.Settings.Default.IsFlatUIReader == true)
                {
                    NewUITop.Visibility = Visibility.Visible;
                    NewUI.Visibility = Visibility.Visible;

                    if (statusBMK != null)
                    {
                        statusBMK.Width = 56;
                        statusBMK.Height = 56;
                    }

                    if (statusMemo != null)
                    {
                        statusMemo.Width = 56;
                        statusMemo.Height = 56;
                    }
                }
            }
            System.Windows.Controls.Image visualChildByName9 = ReadWindow.FindVisualChildByName<System.Windows.Controls.Image>((DependencyObject)this.FR, "screenBroadcasting");
            System.Windows.Controls.Image visualChildByName10 = ReadWindow.FindVisualChildByName<System.Windows.Controls.Image>((DependencyObject)this.FR, "screenReceiving");
            System.Windows.Controls.Image visualChildByName11 = ReadWindow.FindVisualChildByName<System.Windows.Controls.Image>((DependencyObject)this.FR, "StatusOnairOff");
            if (this.isSyncOwner)
            {
                visualChildByName11.Visibility = Visibility.Collapsed;
                visualChildByName9.Visibility = Visibility.Visible;
                visualChildByName10.Visibility = Visibility.Collapsed;
                //StatusOnairOff.Visibility = Visibility.Collapsed;
                //screenBroadcasting.Visibility = Visibility.Visible;
                //screenReceiving.Visibility = Visibility.Collapsed;
            }
            else if (this.isSyncing)
            {
                visualChildByName11.Visibility = Visibility.Collapsed;
                visualChildByName9.Visibility = Visibility.Collapsed;
                visualChildByName10.Visibility = Visibility.Visible;
                //StatusOnairOff.Visibility = Visibility.Collapsed;
                //screenBroadcasting.Visibility = Visibility.Collapsed;
                //screenReceiving.Visibility = Visibility.Visible;
            }
            else
            {
                visualChildByName11.Visibility = Visibility.Visible;
                visualChildByName9.Visibility = Visibility.Collapsed;
                visualChildByName10.Visibility = Visibility.Collapsed;
                //StatusOnairOff.Visibility = Visibility.Visible;
                //screenBroadcasting.Visibility = Visibility.Collapsed;
                //screenReceiving.Visibility = Visibility.Collapsed;
            }
        }


        // 這裡負責同步可以使用的按鈕
        private void switchNoteBookMarkShareButtonStatusWhenSyncing()
        {
            ComboBox cbBooks = ReadWindow.FindVisualChildByName<ComboBox>((DependencyObject)this.FR, "cbBooks");
            RadioButton visualChildByName1 = ReadWindow.FindVisualChildByName<RadioButton>((DependencyObject)this.FR, "BookMarkButton");
            RadioButton visualChildByName2 = ReadWindow.FindVisualChildByName<RadioButton>((DependencyObject)this.FR, "ShareButton");
            RadioButton visualChildByName3 = ReadWindow.FindVisualChildByName<RadioButton>((DependencyObject)this.FR, "NoteButton");
            if (this.isSyncing)
            {
                if (isSyncOwner == true)
                    cbBooks.Visibility = Visibility.Visible;
                else
                    cbBooks.Visibility = Visibility.Collapsed;
                visualChildByName1.Visibility = Visibility.Collapsed;
                visualChildByName2.Visibility = Visibility.Collapsed;
                visualChildByName3.Visibility = Visibility.Collapsed;
                this.BookMarkButtonInListBox.Visibility = Visibility.Collapsed;
                this.NoteButtonInListBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                cbBooks.Visibility = Visibility.Visible;
                visualChildByName1.Visibility = Visibility.Visible;
                visualChildByName2.Visibility = Visibility.Visible;
                visualChildByName3.Visibility = Visibility.Visible;
                this.BookMarkButtonInListBox.Visibility = Visibility.Visible;
                this.NoteButtonInListBox.Visibility = Visibility.Visible;
            }
        }

        private void syncButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ToggleButton visualChildByName1 = ReadWindow.FindVisualChildByName<ToggleButton>((DependencyObject)this.FR, "syncButton");
                Singleton_Socket.ReaderEvent = this;
                this.socket = Singleton_Socket.GetInstance(meetingId, account, userName, visualChildByName1.IsChecked == true ? true : false);
                if (this.socket == null)
                {
                    visualChildByName1.IsChecked = false;
                    MessageBox.Show("無法連接廣播同步系統", "連線失敗", MessageBoxButton.OK);
                    return;
                }
                InkCanvas visualChildByName2 = ReadWindow.FindVisualChildByName<InkCanvas>((DependencyObject)this.FR, "penMemoCanvas");
                bool? isChecked = visualChildByName1.IsChecked;
                if ((!isChecked.GetValueOrDefault() ? 0 : (isChecked.HasValue ? 1 : 0)) != 0)
                {
                    //改為每畫一筆存一次
                    //this.saveCurrentStrokes(this.hejMetadata.LImgList[this.curPageIndex].pageId);
                    visualChildByName2.Strokes.Clear();
                    this.isSyncing = true;
                    Singleton_Socket.ReaderEvent = this;
                    this.socket = Singleton_Socket.GetInstance(meetingId, account, userName, visualChildByName1.IsChecked == true ? true : false);
                    this.socket.syncSwitch(true);
                    this.clearDataWhenSync();

                    loadCurrentStrokes(singleReadPagePair[curPageIndex].leftPageIndex);
                    //this.loadCurrentStrokes(this.hejMetadata.LImgList[this.curPageIndex].pageId);
                    this.resetTransform();
                    this.buttonStatusWhenSyncing(Visibility.Visible, Visibility.Visible);
                }
                else
                {
                    //改為每畫一筆存一次
                    //this.saveCurrentStrokes(this.hejMetadata.LImgList[this.curPageIndex].pageId);
                    visualChildByName2.Strokes.Clear();
                    this.isSyncing = false;
                    this.isSyncOwner = false;
                    Singleton_Socket.ReaderEvent = this;
                    this.socket = Singleton_Socket.GetInstance(meetingId, account, userName, visualChildByName1.IsChecked == true ? true : false);
                    this.socket.syncSwitch(false);
                    this.clearDataWhenSync();

                    alterAccountWhenSyncing(false);
                    getBookPath();

                    this.initUserDataFromDB();
                    loadCurrentStrokes(singleReadPagePair[curPageIndex].leftPageIndex);

                    //this.loadCurrentStrokes(this.hejMetadata.LImgList[this.curPageIndex].pageId);
                    this.resetTransform();
                    this.buttonStatusWhenSyncing(Visibility.Collapsed, Visibility.Collapsed);
                }

                RadioButton BookMarkRb = FindVisualChildByName<RadioButton>(FR, "BookMarkButton");
                if (bookMarkDictionary.ContainsKey(curPageIndex))
                {
                    BookMarkRb.IsChecked = bookMarkDictionary[curPageIndex].status == "0" ? true : false;
                    TriggerBookMark_NoteButtonOrElse(BookMarkRb);
                }
                else
                {
                    BookMarkRb.IsChecked = false;
                    TriggerBookMark_NoteButtonOrElse(BookMarkRb);
                }

                RadioButton NoteRB = FindVisualChildByName<RadioButton>(FR, "NoteButton");
                TextBox tb = FindVisualChildByName<TextBox>(FR, "notePanel");
                if (tb != null)
                {
                    tb.Text = bookNoteDictionary[curPageIndex].text;
                }
                if (bookNoteDictionary.ContainsKey(curPageIndex))
                {
                    if (bookNoteDictionary[curPageIndex].text.Equals(""))
                    {
                        NoteRB.IsChecked = false;
                        TriggerBookMark_NoteButtonOrElse(NoteRB);
                    }
                    else
                    {
                        NoteRB.IsChecked = true;
                        TriggerBookMark_NoteButtonOrElse(NoteRB);
                    }
                }
                else
                {
                    NoteRB.IsChecked = false;
                    TriggerBookMark_NoteButtonOrElse(NoteRB);
                }


                this.switchNoteBookMarkShareButtonStatusWhenSyncing();
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }
        }

        private void clearDataWhenSync()
        {
            bookMarkDictionary = new Dictionary<int, BookMarkData>();
            bookNoteDictionary = new Dictionary<int, NoteData>();
            bookStrokesDictionary = new Dictionary<int, List<StrokesData>>();

            //bookMarkDictionary = new Dictionary<int, bool>();
            //bookNoteDictionary = new Dictionary<int, string>();

            //for (int i = 0; i < hejMetadata.LImgList.Count; i++)
            //{
            //    bookMarkDictionary.Add(i, false);
            //    bookNoteDictionary.Add(i, "");
            //}
        }

        private void deleteAllLocalPenmemoData()
        {
            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
            penMemoCanvas.Strokes.Clear();

            //刪除同步時的螢光筆
            //新版本
            alterAccountWhenSyncing(true);

            string query = "delete from bookStrokesDetail "
           + "Where userbook_sno=" + userBookSno;

            bookManager.sqlCommandNonQuery(query);

            //舊版本
            //string penMemoFilePath = "/hyweb/strokes/Sync/";

            //if (Directory.Exists(bookPath + penMemoFilePath))
            //{
            //    Directory.Delete(bookPath + penMemoFilePath, true);
            //}
        }

        //螢光筆同步處理
        private void drawStrokeFromJson(string msgString)
        {

            Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");
            try
            {
                List<PemMemoInfos> resultStrArray = JsonConvert.DeserializeObject<List<PemMemoInfos>>(msgString);
                for (int i = 0; i < resultStrArray.Count; i++)
                {
                    paintStrokeOnInkCanvas(resultStrArray[i], zoomCanvas.Width, zoomCanvas.Height);
                }
            }
            catch
            {
                //同步格式錯誤
            }


            //Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");
            //InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
            //try
            //{
            //    List<PemMemoInfos> resultStrArray = JsonConvert.DeserializeObject<List<PemMemoInfos>>(msgString);
            //    for (int i = 0; i < resultStrArray.Count; i++)
            //    {
            //        double paperWidth = 0;
            //        double paperHeight = 0;
            //        if (Double.IsNaN(zoomCanvas.Width) || Double.IsNaN(zoomCanvas.Width))
            //        {
            //            paperWidth = Double.IsNaN(zoomCanvas.Width)
            //               ? (Double.IsNaN(penMemoCanvas.Width) ? zoomCanvas.ActualWidth : penMemoCanvas.Width)
            //               : zoomCanvas.Width;


            //            paperHeight = Double.IsNaN(zoomCanvas.Height) ? zoomCanvas.ActualHeight : zoomCanvas.Height;
            //        }
            //        else
            //        {
            //            paperWidth = newImageWidth;
            //            paperHeight = newImageHeight;
            //        }

            //        //if (newImageWidth == 0 && newImageHeight == 0)
            //        //{
            //        //    paperWidth = Double.IsNaN(zoomCanvas.Width)
            //        //     ? (Double.IsNaN(penMemoCanvas.Width) ? zoomCanvas.ActualWidth : penMemoCanvas.Width)
            //        //     : zoomCanvas.Width;


            //        //    paperHeight = Double.IsNaN(zoomCanvas.Height) ? zoomCanvas.ActualHeight : zoomCanvas.Height;
            //        //}
            //        //else
            //        //{
            //        //    paperWidth = newImageWidth;
            //        //    paperHeight = newImageHeight;
            //        //}
            //        paintStrokeOnInkCanvas(resultStrArray[i], paperWidth, paperHeight);

            //    }
            //}
            //catch (Exception ex)
            //{
            //    //同步格式錯誤
            //    LogTool.Debug(ex);
            //}
        }

        private Dictionary<string, Object> convertSrtokeJosonToDic(string msg)
        {
            char[] charStr = { '[', ']' };
            string resultStr = msg.TrimEnd(charStr);
            resultStr = resultStr.TrimStart(charStr);

            string[] resultStArray = resultStr.Split('{');

            Dictionary<string, Object> result = JsonConvert.DeserializeObject<Dictionary<string, Object>>(resultStr);
            return result;
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
                        //sp.X = p.X * (widthScaleRatio == Double.NaN ? 1 : widthScaleRatio);
                        //sp.Y = p.Y * (heightScaleRatio == Double.NaN ? 1 : heightScaleRatio);
                        sp.X = p.X * (widthScaleRatio);
                        sp.Y = p.Y * (heightScaleRatio);
                        pointsList.Add(sp);
                    }
                    catch (Exception ex)
                    {
                        LogTool.Debug(ex);
                    }

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

                if (isSyncing)
                {
                    if (targetStroke != null)
                    {
                        //把解好的螢光筆畫到inkcanvas上
                        InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
                        //penMemoCanvas.DefaultDrawingAttributes = targetStroke.DrawingAttributes;
                        //penMemoCanvas.UpdateLayout();
                        penMemoCanvas.Strokes.Add(targetStroke.Clone());
                        targetStroke = null;
                    }
                }
            }
            catch (Exception ex2)
            {
                LogTool.Debug(ex2);
            }

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


        private Dictionary<string, Object> convertStrokeCommandToJson(string jsonStr)
        {
            Dictionary<string, Object> msgJson = JsonConvert.DeserializeObject<Dictionary<string, Object>>(jsonStr);

            return msgJson;
        }

        void penMemoCanvas_StrokeErased(object sender, RoutedEventArgs e)
        {
            preparePenMemoAndSend(false);
        }

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

        private void preparePenMemoAndSend(bool Division3 = true)
        {
            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");

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

                    //wayne add
                    //pm.strokeWidth = ((int)d.Height) == 0 ? 1 : (int)d.Height;
                    //pm.strokeWidth = ((int)d.Height) == 0 ? 1 : (((int)(d.Height / 3)) == 0 ? 1 : (int)(d.Height / 3));
                    //double actualHeight = 1;
                    //if (d.Height>=3 )
                    //{
                    //    actualHeight = d.Height / 3;
                    //    if( ((int)actualHeight) <1)
                    //        actualHeight=1;
                    //}
                    //pm.strokeWidth = (int)actualHeight;
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
                    //splineDic.Add("canvasHeight", penMemoCanvas.Height);
                    //splineDic.Add("canvasWidth", penMemoCanvas.Width);
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


                //if (CanSentLine == false)
                //{
                //    Task.Factory.StartNew(() =>
                //        {
                //            Thread.Sleep(3000);
                sendBroadCast(result);
                //        });
                //}
            }
        }


        //螢光筆同步處理

        private void sendBroadCast(string msg)
        {
            if (isSyncOwner && isSyncing)
            {
                Singleton_Socket.ReaderEvent = this;
                this.socket = Singleton_Socket.GetInstance(meetingId, account, userName, true);
                if (!String.IsNullOrEmpty(msg) && socket != null)
                {
                    Console.WriteLine("ReadWindow Sent: " + msg);
                    LogTool.Debug(new Exception(msg));
                    Singleton_Socket.ReaderEvent = this;
                    this.socket = Singleton_Socket.GetInstance(meetingId, account, userName, true);
                    if (socket != null && socket.GetIsConnected() == true)
                    {

                        //Task.Factory.StartNew(() =>
                        //{

                        //    if (msg.Contains(@"""cmd"":""R.SS""}"))
                        //    {
                        //        Thread.Sleep(200);
                        //    }
                        //    socket.broadcast(msg);
                        //});
                        //Thread.Sleep(1);
                        socket.broadcast(msg);
                    }
                }
            }
        }

        public void enableSyncButton(SocketClient _socket)
        {
            try
            {
                Singleton_Socket.ReaderEvent = this;
                this.socket = Singleton_Socket.GetInstance(meetingId, account, userName, true);
                if (_socket != null)
                {
                    this.socket = _socket;
                    this.socket.AddEventManager((IEventManager)this);
                }
                ReadWindow.FindVisualChildByName<System.Windows.Controls.Image>((DependencyObject)this.FR, "diableImg").Visibility = Visibility.Collapsed;
                ReadWindow.FindVisualChildByName<ToggleButton>((DependencyObject)this.FR, "syncButton").Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }

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

        private Byte[] getCipherKey()
        {
            Byte[] key = new byte[1];

            return key;
        }

        private bool loadBookXMLFiles()
        {
            HEJMetadataReader hejReader = new HEJMetadataReader(bookPath);
            List<String> requiredFiles = new List<String>();
            requiredFiles.Add("book.xml");
            requiredFiles.Add("thumbs_ok");
            requiredFiles.Add("infos_ok");
            hejMetadata = hejReader.getBookMetadata(bookPath + "\\HYWEB\\content.opf", trialPages, "", "");
            //pageInfoManager = new PageInfoManager(bookPath, hejMetadata);
            return true;
        }

        private bool isReadWindowLoaded = false;
        public bool cloud = false;
        public bool today = false;
        void ReadWindow_Loaded(object sender, RoutedEventArgs e)
        {

            if(HideCollectFile)
            {
                var img = FindVisualChildByName<System.Windows.Controls.Image>(FR, "imgJoin");
                img.Visibility = Visibility.Collapsed;
            }

            InkCanvas _penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
            DrawingAttributes da = new DrawingAttributes();
            SolidColorBrush sb = (System.Windows.Media.SolidColorBrush)ColorTool.HexColorToBrush(PaperLess_Emeeting.Properties.Settings.Default.ReaderPenColor);
            da.Color = sb.Color;
            _penMemoCanvas.DefaultDrawingAttributes =  da;

            lastPageMode = 1;
            checkViewStatus(PageMode.SinglePage);

            RadioButton PageViewButton = FindVisualChildByName<RadioButton>(FR, "PageViewButton");
            PageViewButton.IsChecked = true;
            //if (configMng.savePdfPageMode.Equals(1))
            //{
            //    checkViewStatus(ViewStatus.SinglePage);

            //    RadioButton PageViewButton = FindVisualChildByName<RadioButton>(FR, "PageViewButton");                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     
            //    PageViewButton.IsChecked = true;
            //}
            //GC.Collect();
            //checkImageStatusTimer = new DispatcherTimer();
            //checkImageStatusTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
            //checkImageStatusTimer.IsEnabled = true;
            //checkImageStatusTimer.Tick += new EventHandler(checkImageStatus);
            //checkImageStatusTimer.Start();

            this.Loaded -= ReadWindow_Loaded;

            string query = "update userbook_metadata set readtimes = readtimes+1 Where Sno= " + userBookSno;
            bookManager.sqlCommandNonQuery(query);

            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
            //penMemoCanvas.DefaultDrawingAttributes = configMng.loadStrokeSetting();
            //isStrokeLine = configMng.isStrokeLine;

            tempStrokes = new List<Stroke>();


            this.Closing += ReadWindow_Closing;
            decodedPDFPages[0] = null;
            decodedPDFPages[1] = null;


            if (trialPages > 0)
            {
                //試閱
                downloadProgBar.Visibility = Visibility.Collapsed;
            }
            else
            {
                downloadProgBar.Maximum = hejMetadata.allFileList.Count;
                downloadProgBar.Minimum = 0;

                TextBlock watermarkTextBlock = FindVisualChildByName<TextBlock>(FR, "watermarkTextBlock");
                watermarkTextBlock.Text = watermark;
                Singleton_Socket.ReaderEvent = this;
                this.socket = Singleton_Socket.GetInstance(meetingId, account, userName, isSyncing);
                if (socket != null)
                {
                    socket.AddEventManager(this);
                }
                else
                {
                    ToggleButton syncButton = FindVisualChildByName<ToggleButton>(FR, "syncButton");
                    syncButton.Visibility = Visibility.Collapsed;
                    System.Windows.Controls.Image diableImg = FindVisualChildByName<System.Windows.Controls.Image>(FR, "diableImg");
                    diableImg.Visibility = Visibility.Visible;
                }

                tempStrokes = new List<Stroke>();

                //檢查檔案下載狀態
                //if (!checkThumbnailBorderAndMediaListStatus())
                //{
                //    //isWindowsXP = true;
                //    fsw = new FileSystemWatcher(bookPath + "\\HYWEB\\");
                //    fsw.EnableRaisingEvents = true;
                //    fsw.IncludeSubdirectories = true;

                //    fsw.Changed += new FileSystemEventHandler(fsw_Changed);
                //}
                checkThumbnailBorderAndMediaListStatus();
                downloadProgBar.Visibility = Visibility.Collapsed;
            }

            ////檢查檔案下載狀態
            //if (!checkThumbnailBorderAndMediaListStatus())
            //{
            //    //isWindowsXP = true;
            //    fsw = new FileSystemWatcher(bookPath + "\\HYWEB\\");
            //    fsw.EnableRaisingEvents = true;
            //    fsw.IncludeSubdirectories = true;

            //    fsw.Changed += new FileSystemEventHandler(fsw_Changed);
            //}

            loadOriginalStrokeStatus();





            GC.Collect();
            Debug.WriteLine("@ReadWindow_Loaded");
            isReadWindowLoaded = true;

            //ChangeFlatUI(PaperLess_Emeeting.Properties.Settings.Default.IsFlatUIReader);
            if (PaperLess_Emeeting.Properties.Settings.Default.IsFlatUIReader == true)
            {



                Canvas ToolBarSensor = FindVisualChildByName<Canvas>(FR, "ToolBarSensor");
                Grid PenMemoToolBar = FindVisualChildByName<Grid>(FR, "PenMemoToolBar");
                Grid ToolBarInReader = FindVisualChildByName<Grid>(FR, "ToolBarInReader");
                PenMemoToolBar.Height = 0;
                PenMemoToolBar.Width = 0;

                ToolBarInReader.Height = 0;
                ToolBarInReader.Width = 0;

                ToolBarSensor.Height = 0;
                ToolBarSensor.Width = 0;
                AttachKey();
                AttachEvent();
            }
            else
            {
                System.Windows.Controls.Image statusBMK = FindVisualChildTool.ByName<System.Windows.Controls.Image>(FR, "statusBMK");
                System.Windows.Controls.Image statusMemo = FindVisualChildTool.ByName<System.Windows.Controls.Image>(FR, "statusMemo");
                System.Windows.Controls.Image StatusOnairOff = FindVisualChildTool.ByName<System.Windows.Controls.Image>(FR, "StatusOnairOff");


                if (statusBMK != null)
                {
                    statusBMK.Width = 0;
                    statusBMK.Height = 0;
                    statusBMK.Source = null;
                }

                if (statusBMK != null)
                {
                    statusMemo.Width = 0;
                    statusMemo.Height = 0;
                    statusBMK.Source = null;
                }

                if (statusBMK != null)
                {
                    StatusOnairOff.Width = 0;
                    StatusOnairOff.Height = 0;
                    statusBMK.Source = null;
                }
            }


            // Wayne Add
            // 用意是中途加入同步
            //InitPen();

            this.ContentRendered += (sender3, e3) =>
            {
                InitPen();

                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(6000);
                    CanSentLine = true;
                });
            };

            var imgJoin = FindVisualChildByName<System.Windows.Controls.Image>(FR, "imgJoin");

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

                    if (FolderID.Length == 0)
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

            imgJoin.MouseLeftButtonDown += (sender2, e2) =>
            {
                if (CanNotCollect)
                    return;

                if (HasJoin2Folder)
                {
                    imgJoin.Source = new BitmapImage(new Uri("image/ebTool-inCloud-on2@2x.png", UriKind.Relative));
                }
                else
                {
                    imgJoin.Source = new BitmapImage(new Uri("image/ebTool-toCloud-on2@2x.png", UriKind.Relative));
                }

                if (HasJoin2Folder)
                {
                    DelFile win = new DelFile(this, FolderID, bookId);
                    var success = win.ShowDialog();
                    if (success == true)
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

            };
            ToggleButton syncButton2 = FindVisualChildByName<ToggleButton>(FR, "syncButton");
            if (cloud && !today && syncButton2!=null)
            {
               
                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += (sender3, e3) =>
                {
                    syncButton2.Visibility = Visibility.Collapsed;
                    syncButton2.Width = 0;
                    syncButton2.Height = 0;
                    timer.Stop();
                };
                timer.Start();
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
        bool CanSentLine = false;
        //private void InitPen()
        //{
        //    Task.Factory.StartNew(() =>
        //    {
        //        Thread.Sleep(2000);
        //        this.Dispatcher.BeginInvoke(new Action(() =>
        //        {
        //            if (this.socketMessage.Equals("") == false)
        //            {
        //                this.parseJSonFromMessage(this.socketMessage);
        //                this.needToSendBroadCast = false;
        //                this.socketMessage = "";
        //            }
        //        }));
        //    });
        //}


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
                        this.needToSendBroadCast = false;
                        this.socketMessage = "";
                    }

                    InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");

                    if (penMemoCanvas.Strokes.Count <= 0 && tmpMsg.Equals("") == false)
                    {
                        this.parseJSonFromMessage(this.socketMessage);
                    }


                }));
            });
        }

        private void AttachEvent()
        {
            //ToolBarInReader.Visibility = Visibility.Collapsed;
            System.Windows.Controls.Image StatusOnairOff = ReadWindow.FindVisualChildByName<System.Windows.Controls.Image>((DependencyObject)this.FR, "StatusOnairOff");
            System.Windows.Controls.Image screenBroadcasting = ReadWindow.FindVisualChildByName<System.Windows.Controls.Image>((DependencyObject)this.FR, "screenBroadcasting");
            System.Windows.Controls.Image screenReceiving = ReadWindow.FindVisualChildByName<System.Windows.Controls.Image>((DependencyObject)this.FR, "screenReceiving");
            ToggleButton syncButton = FindVisualChildByName<ToggleButton>(FR, "syncButton");

            if (StatusOnairOff != null && screenBroadcasting != null && screenBroadcasting != null)
            {
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

                            if (syncButton != null)
                            {
                                syncButton.IsChecked = true;
                                syncButton.RaiseEvent(new RoutedEventArgs(ToggleButton.ClickEvent));
                            }

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
                            if (syncButton != null)
                            {
                                syncButton.IsChecked = false;
                                syncButton.RaiseEvent(new RoutedEventArgs(ToggleButton.ClickEvent));
                            }
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
                            if (syncButton != null)
                            {
                                syncButton.IsChecked = false;
                                syncButton.RaiseEvent(new RoutedEventArgs(ToggleButton.ClickEvent));
                            }

                            //typeof(ToggleButton).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(syncButton, new object[] { false });
                        }));
                    });
                };

            }
        }

        private void ReadWindow_Closing(object sender, CancelEventArgs e)
        {

            TextBlock totalPageInReader = FindVisualChildByName<TextBlock>(FR, "TotalPageInReader");
            int totalPage = totalPageInReader.Text.Equals("") ? 0 : int.Parse(totalPageInReader.Text);
            string myBookPath = this.bookPath;
            float width = (float)System.Windows.SystemParameters.PrimaryScreenWidth;
            float height = (float)System.Windows.SystemParameters.PrimaryScreenHeight;
            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
            float penMemoCanvasWidth = (float)penMemoCanvas.Width;
            float penMemoCanvasHeight = (float)penMemoCanvas.Height;



            ////Thread th = new Thread(() =>
            //Task.Factory.StartNew(() =>
            //    {
            //        //Singleton_PDFFactory.AddBookInPDFWork(this.bookId);
            //        Stopwatch sw = new Stopwatch();
            //        sw.Start();
            //        //                    string cmd = string.Format(@"SELECT page,status,alpha,canvasHeight,canvasWidth,color,points,width
            //        //                                                FROM bookStrokesDetail as a inner join bookinfo as b on a.userbook_sno=b.sno 
            //        //                                                where bookid='{0}' and account='{1}' "
            //        //                                             , this.bookId
            //        //                                             , this.account);

            //        //                    QueryResult rs = bookManager.sqlCommandQuery(cmd);

            //        //                    if (rs.fetchRow())
            //        //                    {
            //        //                        width = rs.getInt("canvasWidth");
            //        //                        height = rs.getInt("canvasHeight");

            //        //                    }
            //        //                    SavePDF(myBookPath, totalPage, width, height);
            //        //alterAccountWhenSyncing(this.isSyncOwner);
            //        string UserAccount = this.account;
            //        Singleton_PDFFactory.SavePDF(myBookPath, totalPage, penMemoCanvasWidth, penMemoCanvasHeight, UserAccount, this.bookId, this.dbPath);
            //        sw.Stop();
            //        Console.WriteLine(sw.ElapsedMilliseconds);
            //        //Singleton_PDFFactory.RemoveBookInPDFWork(this.bookId);
            //    });
            ////th.IsBackground = true;
            ////th.Start();

            noteButton_Click();
            //string AssemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

            if (PaperLess_Emeeting.Properties.Settings.Default.AssemblyName.Contains("TPI4F") == true)
            {
                string thumbsPath_Msize = Path.Combine(bookPath, "hyweb", "mthumbs");
                string thumbsPath_Lsize = Path.Combine(bookPath, "hyweb", "mthumbs\\Larger");
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
                Singleton_PDFFactory.SavePDF(false, myBookPath, totalPage, penMemoCanvasWidth, penMemoCanvasHeight, this.account, this.bookId, this.dbPath, thumbsPath_Msize, thumbsPath_Lsize);
            }
            //PDFFactory f2 = new PDFFactory(myBookPath, totalPage, penMemoCanvasWidth, penMemoCanvasHeight, this.account, this.bookId, this.dbPath);
            //f2.Show();
            InitSyncCenter();

            //一定要放到最後，因為最後會初始化所有參數。
            RecordPage();
        }

        //private static void GetPdfThumbnail(string sourcePdfFilePath, string destinationPngFilePath)
        //{
        //    // Use GhostscriptSharp to convert the pdf to a png
        //    GhostscriptWrapper.GenerateOutput(sourcePdfFilePath, destinationPngFilePath,
        //        new GhostscriptSettings
        //        {
        //            Device = GhostscriptDevices.bmp32b,
        //            Page = new GhostscriptPages
        //            {
        //                // Only make a thumbnail of the first page
        //                Start = 1,
        //                End = 1,
        //                AllPages = false
        //            },
        //            Resolution = new System.Drawing.Size
        //            {
        //                // Render at 72x72 dpi
        //                Height = 96,
        //                Width = 96
        //            },
        //            Size = new GhostscriptPageSize
        //            {
        //                // The dimentions of the incoming PDF must be
        //                // specified. The example PDF is US Letter sized.
        //                Native = GhostscriptPageSizes.letter
        //            }
        //        }
        //    );
        //}

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
            FileStream fs = null;
            string fileName = System.IO.Path.Combine(bookPath, "PDFFactory/PDF.pdf");
            string PDFFactoryDirectoryName = Path.GetDirectoryName(fileName);
            string FinalFilePath = System.IO.Path.Combine(bookPath, "PDF.pdf");
            try
            {

                Directory.CreateDirectory(PDFFactoryDirectoryName);
                File.Create(fileName).Dispose();
                fs = new FileStream(fileName, FileMode.Create);
                PdfWriter writer = PdfWriter.GetInstance(myDoc, fs);

                string[] files = Directory.GetFiles(PDFFactoryDirectoryName, "*.bmp");

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

                string pdfPath = Path.Combine(bookPath, "hyweb");
                string thumbsPath = "";
                string thumbsPath_Msize = Path.Combine(bookPath, "hyweb", "mthumbs");
                string thumbsPath_Lsize = Path.Combine(bookPath, "hyweb", "mthumbs\\Larger");
                if (Directory.Exists(thumbsPath_Lsize) == true)
                {
                    thumbsPath = thumbsPath_Lsize;
                }
                else
                {
                    thumbsPath = thumbsPath_Msize;
                }
                string[] pdfFiles = Directory.GetFiles(pdfPath, "*.pdf");
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
                        string pdf = Path.Combine(pdfPath, pdfPrefix + "_" + count + ".pdf");
                        string thumb = Path.Combine(thumbsPath, pdfPrefix + "_" + count + ".jpg");
                        string imgPath = Path.Combine(PDFFactoryDirectoryName, count + ".bmp");

                        Directory.CreateDirectory(Path.GetDirectoryName(thumb));
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



                            try
                            {
                                rs = bookManager.sqlCommandQuery(cmd);
                            }
                            catch (Exception ex)
                            {
                                LogTool.Debug(ex);
                            }
                            if (rs != null)
                            {
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

                    try
                    {
                        if (myDoc.IsOpen())
                            myDoc.Close();
                    }
                    catch (Exception ex)
                    {
                        LogTool.Debug(ex);
                    }

                    try
                    {
                        if (fs != null)
                            fs.Dispose();
                    }
                    catch (Exception ex)
                    {
                        LogTool.Debug(ex);
                    }

                    if (File.Exists(fileName) == true)
                    {
                        File.Copy(fileName, FinalFilePath, true);
                    }

                }
                catch (Exception ex)
                {
                    LogTool.Debug(ex);
                }
            }


        }


        public enum Definition
        {
            One = 1, Two = 2, Three = 3, Four = 4, Five = 5, Six = 6, Seven = 7, Eight = 8, Nine = 9, Ten = 10
        }

        ///<summary>
        ///將PDF文檔轉換為圖片的方法
        ///</summary>
        ///<param name ="pdfInputPath"> PDF文件路徑</ param>
        ///<param name ="imageOutputPath">圖片輸出路徑</ param>
        ///<param name ="imageName">生成圖片的名字</ param>
        ///<param name ="startPageNum">從PDF文檔的第幾頁開始轉換</ param>
        ///<param name ="endPageNum">從PDF文檔的第幾頁開始停止轉換</ param>
        ///<param name ="imageFormat">設置所需圖片格式</ param>
        ///<param name ="definition">設置圖片的清晰度，數字越大越清晰</ param>
        public static void ConvertPDF2Image(string pdfInputPath, string imageOutputPath, int startPageNum, int endPageNum, ImageFormat imageFormat, Definition definition)
        {
            PDFFile pdfFile = PDFFile.Open(pdfInputPath);

            //if (!Directory.Exists(imageOutputPath))
            //{
            //    Directory.CreateDirectory(imageOutputPath);
            //}

            // validate pageNum
            if (startPageNum <= 0)
            {
                startPageNum = 1;
            }

            if (endPageNum > pdfFile.PageCount)
            {
                endPageNum = pdfFile.PageCount;
            }

            if (startPageNum > endPageNum)
            {
                int tempPageNum = startPageNum;
                startPageNum = endPageNum;
                endPageNum = startPageNum;
            }

            // start to convert each page
            for (int i = startPageNum; i <= endPageNum; i++)
            {
                Bitmap pageImage = pdfFile.GetPageImage(i - 1, 92);
                pageImage.Save(imageOutputPath, ImageFormat.Jpeg);
                pageImage.Dispose();
            }

            pdfFile.Dispose();
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
                    Singleton_Socket.ReaderEvent = this;
                    this.socket = Singleton_Socket.GetInstance(meetingId, account, userName, isSyncing);
                    if (socket != null)
                    {
                        socket.RemoveEventManager(this);
                    }
                }
                catch (Exception ex)
                {
                    LogTool.Debug(ex);
                }



                Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");
                zoomCanvas.Background = null;
                try
                {
                    this.Closing -= ReadWindow_Closing;
                    this.imageSourceRendered -= ReadWindow_imageSourceRendered;
                }
                catch (Exception ex)
                {
                    LogTool.Debug(ex);
                }

                if (checkImageStatusTimer != null)
                {
                    checkImageStatusTimer.Tick -= new EventHandler(checkImageStatus);
                    checkImageStatusTimer.Stop();
                    checkImageStatusTimer.IsEnabled = false;
                    checkImageStatusTimer = null;
                }

                if (trialPages == 0)
                {
                    //存營光筆資料到DB
                    //InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
                    //configMng.saveStrokeSetting(penMemoCanvas.DefaultDrawingAttributes, isStrokeLine);

                    if (fsw != null)
                    {
                        fsw.EnableRaisingEvents = false;
                        fsw.IncludeSubdirectories = false;
                        fsw.Changed -= new FileSystemEventHandler(fsw_Changed);
                        fsw = null;
                    }

                    //savePageMode();


                    saveLastReadingPage();

                    //切換SyncOwner時把之前的螢光筆清除(下一版換回來)

                    // wayne add
                    // 爛東西，被下面那句害慘了，不能放在上面，放到這裡就沒問題。
                    // 會自動切換成主控，並且刪除主控的筆畫
                    deleteAllLocalPenmemoData();
                }
                clearReadPagePairData(singleReadPagePair);
                clearReadPagePairData(doubleReadPagePair);

                BindingOperations.ClearAllBindings(this);
                BindingOperations.ClearAllBindings(thumbNailListBox);

                List<ThumbnailImageAndPage> dataList = (List<ThumbnailImageAndPage>)thumbNailListBox.ItemsSource;
                for (int i = 0; i < dataList.Count; i++)
                {
                    dataList[i].leftImagePath = "";
                }
                if (thumbNailListBox.SelectedIndex > 0)
                {
                    dataList.RemoveAt(thumbNailListBox.SelectedIndex); // for remove specific
                }
                dataList.Clear(); //For removing All

                BindingOperations.ClearAllBindings(_FlowDocument);
                BindingOperations.ClearAllBindings(_FlowDocumentDouble);
                BindingOperations.ClearAllBindings(FR);

                _FlowDocument.Blocks.Clear();
                _FlowDocumentDouble.Blocks.Clear();

                thumbNailListBox.ItemsSource = null;

                FR.Document = null;
                _FlowDocument = null;
                _FlowDocumentDouble = null;
                singleThumbnailImageAndPageList.Clear();
                doubleThumbnailImageAndPageList.Clear();
                singleThumbnailImageAndPageList = null;
                doubleThumbnailImageAndPageList = null;
                tfgForImage = null;
                caTool = null;

                singleImgStatus = null;
                doubleImgStatus = null;

                selectedBook = null;
                bookPath = null;
                hejMetadata = null;

                bookMarkDictionary = null;
                tfgForHyperLink = null;
                pageInfoManager = null;
                pageInfo = null;
                RelativePanel = null;
                configMng = null;
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }
        }

        private void initUserDataFromDB()
        {

            //alterAccountWhenSyncing(this.isSyncOwner);
            //getBookPath();
            //判斷是否可以列印(新版本從service抓)           
            getBookRightsAsync(bookId);

            //由資料庫取回上次瀏覽頁
            string CName = System.Environment.MachineName;

            lastViewPage = bookManager.getLastViewPageObj(userBookSno);

            if (lastViewPage.ContainsKey(CName))
            {
                if (lastViewPage[CName].index > 0)
                {
                    int pdfMode = lastPageMode;

                    //wayne
                    if (this.isSyncing == true && CanSentLine == false)
                    {
                        if (this.isSyncOwner == true)
                        {
                            bringBlockIntoView(0);
                            loadCurrentStrokes(0);

                        }
                        //txtPage.Text = string.Format("{0} / {1}", "1", totalPage.ToString());
                    }
                    else
                    {
                        if (pdfMode.Equals(1))
                        {

                            //單頁
                            bringBlockIntoView(lastViewPage[CName].index);
                        }
                        else if (pdfMode.Equals(2))
                        {
                            //雙頁
                            int doubleLastViewPage = getDoubleCurPageIndex(lastViewPage[CName].index);
                            bringBlockIntoView(doubleLastViewPage);
                        }
                    }

                }
                else
                {
                    Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");
                    zoomCanvas.Background = null;
                }
            }
            else
            {
                Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");
                zoomCanvas.Background = null;
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


        // Wayne 初始化按鈕
        private void iniUpperButtons()
        {
            ComboBox cbBooks = ReadWindow.FindVisualChildByName<ComboBox>((DependencyObject)this.FR, "cbBooks");
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

            //判斷有沒有索引檔, 有的話才顯示按鈕
            //if (File.Exists(bookPath + "\\HYWEB\\index.zip"))
            //{
            //    RadioButton rb = FindVisualChildByName<RadioButton>(FR, "SearchButton");
            //    rb.Visibility = Visibility.Visible;
            //}

            //判斷有沒有目錄檔, 有的話才顯示按鈕
            if (File.Exists(bookPath + "\\HYWEB\\toc.ncx"))
            {
                byte[] curKey = defaultKey;
                string ncxFile = bookPath + "\\HYWEB\\toc.ncx";
                XmlDocNcx = new XmlDocument();

                using (MemoryStream tocStream = new MemoryStream())
                {
                    FileStream sourceStream = new FileStream(ncxFile, FileMode.Open);
                    sourceStream.CopyTo(tocStream);

                    RadioButton rb = FindVisualChildByName<RadioButton>(FR, "TocButton");
                    try
                    {
                        XmlDocNcx.Load(tocStream);
                        tocStream.Close();
                        rb.Visibility = Visibility.Visible;
                    }
                    catch
                    {
                        //讀取目錄檔發生無法預期的錯誤, 先不顯示目錄檔按鈕
                        tocStream.Close();
                        rb.Visibility = Visibility.Collapsed;
                    }
                    //StreamReader xr = new StreamReader(tocStream);
                    //string xmlString = xr.ReadToEnd();
                    //xmlString = xmlString.Replace("xmlns=\"http://www.daisy.org/z3986/2005/ncx/\" version=\"2005-1\"", "");
                    //XmlDocNcx.LoadXml(xmlString);
                    //xr.Close();
                    //xr = null;
                    //tocStream.Close();
                    //xmlString = null;
                }
            }
            //判斷有沒有多媒體檔
            //if (!pageInfoManager.HyperLinkAreaDictionary.Count.Equals(0))
            //{
            //    RadioButton rb = FindVisualChildByName<RadioButton>(FR, "MediaListButton");
            //    rb.Visibility = Visibility.Visible;
            //}

            //判斷是否有目錄頁
            if (hejMetadata.tocPageIndex.Equals(0))
            {
                RadioButton rb = FindVisualChildByName<RadioButton>(FR, "ContentButton");
                rb.Visibility = Visibility.Collapsed;
            }
        }

        private void cbBooks_SelectionChanged(object sender, EventArgs e)
        {
            if (isSyncing == true && isSyncOwner == false)
                return;

            ComboBox cb = (ComboBox)sender;
            //if (FromReaderWindow_ChangeBook_Event!=null)
            //    FromReaderWindow_ChangeBook_Event(cb.SelectedValue.ToString());

            BookVM bookVM = ((BookVM)cb.SelectedValue);
            if (bookVM == null)
                return;

            //RecordPage();

            //Task.Factory.StartNew(() =>
            //{
            if (Home_OpenBookFromReader_Event != null)
            {
                Home_OpenBookFromReader_Event(this.meetingId, bookVM, this.cbBooksData, this.watermark);
            }
            //});

            //this.Close();



            //Mouse.OverrideCursor = Cursors.AppStarting;
            //ReadWindow rw = new ReadWindow(this.cbBooksData,bookVM.BookPath, bookVM.FileID, account
            //                                      , userName, email, meetingId
            //                                      , watermark, dbPath, isSyncing
            //                                      , isSyncOwner, webServiceURL, socketMessage, socket);

            //rw.WindowStyle = WindowStyle.None;
            //rw.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            //rw.WindowState = WindowState.Maximized;
            //Mouse.OverrideCursor = Cursors.Arrow;
            //rw.Show();

            //if (isSyncing == true && isSyncOwner == true)
            //{
            //    string OB = "{\"bookId\":\"" + bookVM.FileID + "\",\"cmd\":\"R.OB\"}";
            //    sendBroadCast(OB);
            //}

            //this.Close();
        }


        public Dictionary<string, BookVM> cbBooksData = new Dictionary<string, BookVM>();

        public event Home_OpenBookFromReader_Function Home_OpenBookFromReader_Event;

        public bool HasJoin2Folder = false;
        public string FolderID;
        public bool CanNotCollect;
        public bool HideCollectFile = false;
        public ReadWindow(Dictionary<string, BookVM> cbBooksData
                            , Home_OpenBookFromReader_Function callback
                            , string _bookPath, string _bookId, string _account, string _userName, string _email, string _meetingId, string _watermark, string _dbPath, bool _isSync, bool _isSyncOwner, string _webServiceURL, byte[] defaultKey, string _socketMessage = "", SocketClient _socket = null)
        {

            QueryResult rs;
            try
            {


                // Wayne add
                this.cbBooksData = cbBooksData;
                this.Home_OpenBookFromReader_Event = callback;
                this.defaultKey = defaultKey;
                //this.FromReaderWindow_ChangeBook_Event = callback;
                this.socket = _socket;
                this.bookPath = _bookPath;
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
                this.bookType = BookType.PHEJ;


                bookManager = new BookManager(dbPath);

                rs = null;

                string query = "Select objectId from bookMarkDetail";
                //rs = dbConn.executeQuery(query);
                rs = bookManager.sqlCommandQuery(query);
                if (rs == null)
                {
                    //資料庫尚未更新
                    updateDataBase();
                }

                //Wayne add
                //InitSyncCenter();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception@updateDataBase: " + ex.Message);

            }
            rs = null;

            langMng = new MultiLanquageManager("zh-TW");

            this.Initialized += _InitializedEventHandler;
            lastTimeOfChangingPage = DateTime.Now;
            InitializeComponent();

            setWindowToFitScreen();
            ChangeFlatUI(PaperLess_Emeeting.Properties.Settings.Default.IsFlatUIReader);

            this.Loaded += ReadWindow_Loaded;

            ClearSyncOwnerPenLine();


          
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



        // wayne add
        public ReadWindow(Dictionary<string, BookVM> cbBooksData
                            , Home_OpenBookFromReader_Function callback
                            , string _bookPath, string _bookId, string _account, string _userName, string _email, string _meetingId, string _watermark, string _dbPath, bool _isSync, bool _isSyncOwner, string _webServiceURL, string _socketMessage = "", SocketClient _socket = null)
        {
            QueryResult rs;
            try
            {


                // Wayne add
                this.cbBooksData = cbBooksData;
                this.Home_OpenBookFromReader_Event = callback;
                //this.FromReaderWindow_ChangeBook_Event = callback;
                this.socket = _socket;
                this.bookPath = _bookPath;
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
                this.bookType = BookType.PHEJ;


                bookManager = new BookManager(dbPath);

                rs = null;

                string query = "Select objectId from bookMarkDetail";
                //rs = dbConn.executeQuery(query);
                rs = bookManager.sqlCommandQuery(query);
                if (rs == null)
                {
                    //資料庫尚未更新
                    updateDataBase();
                }

                //Wayne add
                //InitSyncCenter();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception@updateDataBase: " + ex.Message);

            }
            rs = null;

            langMng = new MultiLanquageManager("zh-TW");

            this.Initialized += _InitializedEventHandler;
            lastTimeOfChangingPage = DateTime.Now;
            InitializeComponent();

            setWindowToFitScreen();

            ChangeFlatUI(PaperLess_Emeeting.Properties.Settings.Default.IsFlatUIReader);

            this.Loaded += ReadWindow_Loaded;

            ClearSyncOwnerPenLine();

           
        }

        private void Grid_MouseEnterTransparent(object sender, MouseEventArgs e)
        {
            btnThin.Background = System.Windows.Media.Brushes.Transparent;
            btnMedium.Background = System.Windows.Media.Brushes.Transparent;
            btnLarge.Background = System.Windows.Media.Brushes.Transparent;

            Grid gd = (Grid)sender;
            gd.Background = ColorTool.HexColorToBrush("#F66F00");
        }

        private void Grid_MouseLeaveTransparent(object sender, MouseEventArgs e)
        {
            Grid gd = (Grid)sender;
            gd.Background = System.Windows.Media.Brushes.Transparent;
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            List<DependencyObject> list = new List<DependencyObject>();
            FindVisualChildTool.ByType<Grid>(PenColorSP, ref list);

            int i = 0;
            foreach (Grid btn in list)
            {
                i++;
                if (System.Windows.Media.Brush.Equals(btn.Background, System.Windows.Media.Brushes.Black) == false)
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
            gd.Background = System.Windows.Media.Brushes.Black;
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

        private void AttachKey()
        {
            this.PreviewKeyDown += (sender, e) =>
            {
                //e.Handled = true;
                if (isSyncing == true && isSyncOwner == false)
                    return;
                switch (e.Key)
                {
                    case Key.Left:
                        //MovePage(MovePageType.上一頁);
                        break;
                    case Key.Right:
                        //MovePage(MovePageType.下一頁);
                        break;
                    case Key.Up:
                        //MovePage(MovePageType.上一頁);
                        break;
                    case Key.Down:
                        //MovePage(MovePageType.下一頁);
                        break;

                    case Key.PageDown:
                        //MovePage(MovePageType.下一頁);
                        break;
                    case Key.PageUp:
                        //MovePage(MovePageType.上一頁);
                        break;

                    case Key.Home:
                        //MovePage(MovePageType.第一頁);
                        break;
                    case Key.End:
                        //MovePage(MovePageType.最後一頁);
                        break;
                    case Key.Escape:
                        OpenClosePaint();
                        break;
                    default:
                        break;
                }
            };
        }

        private void OpenClosePaint()
        {

            openedby = MediaCanvasOpenedBy.PenMemo;
            Grid toolBarInReader = FindVisualChildByName<Grid>(FR, "ToolBarInReader");
            Grid penMemoToolBar = FindVisualChildByName<Grid>(FR, "PenMemoToolBar");
            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
            StrokeToolPanelHorizontal_Reader strokeToolPanelHorizontal = new StrokeToolPanelHorizontal_Reader();
            strokeToolPanelHorizontal.langMng = this.langMng;
            Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");

            Canvas stageCanvas = GetStageCanvasInReader();

            if (Canvas.GetZIndex(penMemoCanvas) < 900)
            {
                //toolBarInReader.Visibility = Visibility.Collapsed;
                //penMemoToolBar.Visibility = Visibility.Visible;
                //PenMemoButton.IsChecked = false;


                //strokeToolPanelHorizontal.determineDrawAtt(penMemoCanvas.DefaultDrawingAttributes, isStrokeLine);

                MouseTool.ShowPen();
                //打開
                Canvas.SetZIndex(penMemoCanvas, 900);
                Canvas.SetZIndex(stageCanvas, 2);
                Canvas.SetZIndex(zoomCanvas, 850);
                NewUITop.Visibility = Visibility.Collapsed;
                NewUI.Visibility = Visibility.Collapsed;
                NewUI.Visibility = Visibility.Collapsed;
                thumnailCanvas.Visibility = Visibility.Collapsed;
                ShowListBoxButton.Visibility = Visibility.Collapsed;
                penMemoCanvas.Background = System.Windows.Media.Brushes.Transparent;
                penMemoCanvas.EditingMode = InkCanvasEditingMode.Ink;

                penMemoCanvas.Visibility = Visibility.Visible;

                strokeToolPanelHorizontal.HorizontalAlignment = HorizontalAlignment.Right;
                penMemoToolBar.Children.Add(strokeToolPanelHorizontal);

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
                Canvas HiddenControlCanvas = FindVisualChildByName<Canvas>(FR, "HiddenControlCanvas");
                if (HiddenControlCanvas.Visibility.Equals(Visibility.Collapsed))
                {
                    HiddenControlCanvas.Visibility = Visibility.Visible;
                }

                Keyboard.ClearFocus();

                //把其他的按鈕都disable
                //disableAllOtherButtons(true);

                ButtonsStatusWhenOpenPenMemo(0.5, false);
                if (isStrokeLine)
                {
                    strokeLineEventHandler();
                }
                else
                {
                    strokeCurveEventHandler();
                }

                ChangeMainPenColor();

                System.Windows.Media.Brush backgroundColor = btnEraserGD.Background;
                if (backgroundColor is SolidColorBrush)
                {
                    string colorValue = ((SolidColorBrush)backgroundColor).Color.ToString();
                    if (colorValue.Equals("#FFF66F00") == true)
                    {

                        //true 表示要對此 Bitmap 進行色彩修正，否則為 false。
                        //Bitmap bmp = new Bitmap(AppDomain.CurrentDomain.BaseDirectory + "\\Cursor\\markers-eraser@2x.png", false);
                        //BitmapImage bmp=  new BitmapImage(new Uri("images/markers-eraser@2x.png",UriKind.Relative)); 
                        //penMemoCanvas.Cursor = CursorTool.ConvertToCursor(bmp, new System.Drawing.Point(58, 58));
                        //System.Windows.Controls.Image img = new System.Windows.Controls.Image();
                        //img.Source = new BitmapImage(new Uri("images/markers-eraser@2x.png", UriKind.Relative));
                        //penMemoCanvas.Cursor = CursorTool.CreatCursor(img, new System.Drawing.Point(58, 58));

                        Mouse.OverrideCursor = CursorHelper.CreateCursor(new MyCursor());

                        penMemoCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                    }

                }


            }
            else
            {
                MouseTool.ShowArrow();
                //關閉
                Canvas.SetZIndex(zoomCanvas, 1);
                Canvas.SetZIndex(penMemoCanvas, 2);
                Canvas.SetZIndex(stageCanvas, 3);
                NewUITop.Visibility = Visibility.Visible;
                NewUI.Visibility = Visibility.Visible;
                //((RadioButton)sender).IsChecked = false;
                penMemoCanvas.EditingMode = InkCanvasEditingMode.None;

                alterPenmemoAnimation(strokeToolPanelHorizontal, strokeToolPanelHorizontal.Width, 0);

                //存現在的營光筆
                convertCurrentStrokesToDB(hejMetadata.LImgList[curPageIndex].pageId);

                penMemoToolBar.Children.Remove(penMemoToolBar.Children[penMemoToolBar.Children.Count - 1]);
                Canvas popupControlCanvas = FindVisualChildByName<Canvas>(FR, "PopupControlCanvas");
                if (popupControlCanvas.Visibility.Equals(Visibility.Visible))
                {
                    popupControlCanvas.Visibility = Visibility.Collapsed;
                }
                Canvas HiddenControlCanvas = FindVisualChildByName<Canvas>(FR, "HiddenControlCanvas");
                if (HiddenControlCanvas.Visibility.Equals(Visibility.Visible))
                {
                    HiddenControlCanvas.Visibility = Visibility.Collapsed;
                }

                penMemoToolBar.Visibility = Visibility.Collapsed;
                toolBarInReader.Visibility = Visibility.Visible;
                ButtonsStatusWhenOpenPenMemo(1, true);
                resetFocusBackToReader();

            }

            penMemoCanvas.Focus();
        }

        private Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            // BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

            try
            {
                using (MemoryStream outStream = new MemoryStream())
                {
                    BitmapEncoder enc = new BmpBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                    enc.Save(outStream);
                    System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                    return new Bitmap(bitmap);
                }
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }
            return null;
        }

        private void ChangeFlatUI(bool IsFlatUI)
        {
            if (IsFlatUI == true)
            {


                this.MouseRightButtonDown += (sender, e) =>
                {

                    OpenClosePaint();

                };
                Canvas MediaTableCanvas = GetMediaTableCanvasInReader();
                if (MediaTableCanvas != null)
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
                //Task.Factory.StartNew(() =>
                //    {
                //        Thread.Sleep(10);
                //        this.Dispatcher.BeginInvoke(new Action(() =>
                //            {
                //                System.Windows.Controls.Image statusBMK = FindVisualChildTool.ByName<System.Windows.Controls.Image>(FR, "statusBMK");
                //                System.Windows.Controls.Image statusMemo = FindVisualChildTool.ByName<System.Windows.Controls.Image>(FR, "statusMemo");
                //                System.Windows.Controls.Image StatusOnairOff = FindVisualChildTool.ByName<System.Windows.Controls.Image>(FR, "StatusOnairOff");

                //                if (statusBMK != null)
                //                {
                //                    statusBMK.Width = 0;
                //                    statusBMK.Height = 0;
                //                }

                //                if (statusBMK != null)
                //                {
                //                    statusMemo.Width = 0;
                //                    statusMemo.Height = 0;
                //                }

                //                if (statusBMK != null)
                //                {
                //                    StatusOnairOff.Width = 0;
                //                    StatusOnairOff.Height = 0;
                //                }
                //            }));
                //    });


                thumnailCanvas.Background = ColorTool.HexColorToBrush("#212020");
            }
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

        public ReadWindow(string _bookPath, string _bookId, string _account, string _userName, string _email, string _meetingId, string _watermark, string _dbPath, bool _isSync, bool _isSyncOwner, string _webServiceURL, string _socketMessage = "", SocketClient _socket = null)
        {
            QueryResult rs;
            try
            {


                this.socket = _socket;
                this.bookPath = _bookPath;
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
                this.bookType = BookType.PHEJ;


                bookManager = new BookManager(dbPath);

                rs = null;

                string query = "Select objectId from bookMarkDetail";
                //rs = dbConn.executeQuery(query);
                rs = bookManager.sqlCommandQuery(query);
                if (rs == null)
                {
                    //資料庫尚未更新
                    updateDataBase();
                }

                //Wayne add
                //InitSyncCenter();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception@updateDataBase: " + ex.Message);

            }
            rs = null;

            langMng = new MultiLanquageManager("zh-TW");

            this.Initialized += _InitializedEventHandler;
            lastTimeOfChangingPage = DateTime.Now;
            InitializeComponent();

            setWindowToFitScreen();
            ChangeFlatUI(PaperLess_Emeeting.Properties.Settings.Default.IsFlatUIReader);

            this.Loaded += ReadWindow_Loaded;

            ClearSyncOwnerPenLine();
        }

        private void updateDataBase()
        {
            List<string> alterAllTable = new List<string>();
            DateTime dt = new DateTime(1970, 1, 1);
            long currentTime = DateTime.Now.ToUniversalTime().Subtract(dt).Ticks / 10000000;

            try
            {
                alterAllTable.Add(@"CREATE TABLE `userbook_metadata` (
	                                        `sno` Long NOT NULL IDENTITY, 
	                                        `bookId` VarChar(255) WITH COMP NOT NULL, 
	                                        `account` VarChar(255) WITH COMP NOT NULL, 
	                                        `vendorId` VarChar(255) WITH COMP NOT NULL, 
	                                        `colibId` VarChar(255) WITH COMP NOT NULL, 
	                                        `book_type` VarChar(255) WITH COMP, 
	                                        `globalNo` VarChar(255) WITH COMP, 
	                                        `book_language` VarChar(30) WITH COMP, 
	                                        `orientation` VarChar(20) WITH COMP, 
	                                        `text_direction` VarChar(20) WITH COMP, 
	                                        `owner` VarChar(255) WITH COMP NOT NULL, 
	                                        `hyread_type` Byte, 
	                                        `total_pages` Short, 
	                                        `volume` VarChar(255) WITH COMP, 
	                                        `cover` VarChar(128) WITH COMP, 
	                                        `coverMD5` VarChar(255) WITH COMP, 
	                                        `filesize` Long, 
	                                        `epub_filesize` Long, 
	                                        `hej_filesize` Long, 
	                                        `phej_filesize` Long, 
	                                        `page_direction` VarChar(255) WITH COMP, 
	                                        `lastview_page` Long DEFAULT 0, 
	                                        `canPrint` Bit NOT NULL DEFAULT 0, 
	                                        `canMark` Bit NOT NULL DEFAULT 0, 
	                                        `postTimes` Long DEFAULT 0, 
	                                        `expireDate` VarChar(255), 
	                                        `readTimes` Long DEFAULT 0, 
	                                        `epubLastNode` Long DEFAULT 0, 
	                                        `epubLastPageRate` Single DEFAULT 0, 
	                                        `updateTime` DateTime, 
	                                        `rightsXML` LongText WITH COMP,
	                                        CONSTRAINT `PrimaryKey` PRIMARY KEY (`sno`, `bookId`, `account`, `vendorId`, `colibId`, `owner`)
                                        )
                                        GO
                                        CREATE INDEX `bookId`
	                                        ON `userbook_metadata` (
	                                        `bookId`
                                        )
                                        GO
                                        CREATE INDEX `colibId`
	                                        ON `userbook_metadata` (
	                                        `colibId`
                                        )
                                        GO
                                        CREATE INDEX `vendorId`
	                                        ON `userbook_metadata` (
	                                        `vendorId`
                                        )
                    GO");
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }

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

        //將 PDF ren 成 Bitmap (改用Thread的方式ren)
        private Bitmap renPdfToBitmap(string pageFile, byte[] key, int pg, int dpi, float scal, int decodedPageIndex, Border border, bool isSinglePage)
        {
            //Mutex mLoad = new Mutex(requestInitialOwnership, "LoadMutex", out loadMutexWasCreated);
            //if (!(requestInitialOwnership & loadMutexWasCreated))
            //{
            //    mLoad.WaitOne();
            //}

            System.Drawing.Color bgColor = System.Drawing.Color.White; //背景白色
            Bitmap bmp = null;
            if (decodedPDFPages[decodedPageIndex] == null) //如果此頁已經解密過，就直接拿來ren，不用再重新解密一次
            {
                try
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        //FileStream sourceStream = new FileStream(pageFile, FileMode.Open);
                        //wayne add 20140825;
                        FileStream sourceStream = new FileStream(pageFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        sourceStream.CopyTo(memoryStream);
                        decodedPDFPages[decodedPageIndex] = memoryStream.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    LogTool.Debug(ex);
                    return bmp;
                }
            }
            else
            {
                try
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        //FileStream sourceStream = new FileStream(pageFile, FileMode.Open);
                        //wayne add 20140825;
                        FileStream sourceStream = new FileStream(pageFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        sourceStream.CopyTo(memoryStream);
                        decodedPDFPages[decodedPageIndex] = memoryStream.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    LogTool.Debug(ex);
                    return bmp;
                }
            }

            try
            {   //TODO: 改成把PDF實體拉出來變global的
                PDFDoc pdfDoc = new PDFDoc();
                pdfDoc.Init("PVD20-M4IRG-QYZK9-MNJ2U-DFTK1-MAJ4L", "PDFX3$Henry$300604_Allnuts#");
                try
                {
                    pdfDoc.OpenFromMemory(decodedPDFPages[decodedPageIndex], (uint)decodedPDFPages[decodedPageIndex].Length, 0);
                }
                catch (Exception e)
                {
                    LogTool.Debug(e);
                    //throw e;
                }
                PXCV_Lib36.PXV_CommonRenderParameters commonRenderParam = prepareCommonRenderParameter(pdfDoc, dpi, pg, scal, 0, 0, border, isSinglePage);
                pdfDoc.DrawPageToDIBSection(IntPtr.Zero, pg, bgColor, commonRenderParam, out bmp);
                pdfDoc.ReleasePageCachedData(pg, (int)PXCV_Lib36.PXCV_ReleaseCachedDataFlags.pxvrcd_ReleaseDocumentImages);
                pdfDoc.Delete();
            }
            catch (Exception e)
            {
                LogTool.Debug(e);
                //throw e;
            }
            //bmp.Save("c:\\Temp\\test.bmp");
            return bmp;
        }


        private void imageZoom(double imageScale, double scaleMaxOrMin, bool Maximum, bool isSlide)
        {
            StackPanel img = (StackPanel)GetImageInReader();

            TranslateTransform tt = (TranslateTransform)tfgForImage.Children[1];
            ScaleTransform imageTransform = (ScaleTransform)tfgForImage.Children[0];

            double originalScaleX = imageTransform.ScaleX;
            double originalScaleY = imageTransform.ScaleY;

            imageTransform.ScaleX = imageScale;
            imageTransform.ScaleY = imageScale;

            if (Maximum)
            {
                imageTransform.ScaleX = Math.Min(imageTransform.ScaleX, scaleMaxOrMin);
                imageTransform.ScaleY = Math.Min(imageTransform.ScaleY, scaleMaxOrMin);
            }
            else
            {
                imageTransform.ScaleX = Math.Max(imageTransform.ScaleX, scaleMaxOrMin);
                imageTransform.ScaleY = Math.Max(imageTransform.ScaleY, scaleMaxOrMin);
            }


            //double tempWidth = img.ActualWidth * (imageTransform.ScaleX - originalScaleX) / 2;
            //double tempHeight = img.ActualHeight * (imageTransform.ScaleY - originalScaleY) / 2;

            ////ZoomCenterDeltaX = tempWidth;
            ////ZoomCenterDeltaY = tempHeight;

            double ratioOfBounds = this.RestoreBounds.Height / this.ActualWidth;
            double ratioOfImage = img.ActualHeight / img.ActualWidth;

            //System.Windows.Point totalMove = new System.Windows.Point(
            //    (tt.X) * (imageTransform.ScaleX - originalScaleX)
            //    , (tt.Y) * (imageTransform.ScaleX - originalScaleX));

            //tt.X = - tempWidth;
            //tt.Y = - tempHeight;


            tt.X = tt.X - tt.X * (originalScaleX - imageTransform.ScaleX);
            tt.Y = tt.Y - tt.Y * (originalScaleY - imageTransform.ScaleY);

            ////imageCenter = new System.Windows.Point(tt.X, tt.Y);
            tt.X = Math.Min(tt.X, 0);
            tt.X = Math.Max(tt.X, 0);

            tt.Y = Math.Min(tt.Y, 0);
            tt.Y = Math.Max(tt.Y, 0);

            if (ratioOfBounds < ratioOfImage)
            {
                ratio = img.ActualHeight / this.ActualHeight;

                //if (img.ActualWidth * imageTransform.ScaleX > this.ActualWidth * ratio)
                //{
                //    tt.X -= totalMove.X;
                //}
            }
            else
            {
                ratio = img.ActualWidth / this.RestoreBounds.Width;

                //if (img.ActualHeight * imageTransform.ScaleY > this.ActualHeight * ratio)
                //{
                //    tt.Y -= totalMove.Y;
                //}
            }

            if (!isSlide)
            {
                Slider sliderInReader = FindVisualChildByName<Slider>(FR, "SliderInReader");
                sliderInReader.ValueChanged -= SliderInReader_ValueChanged;
                sliderInReader.Value = imageScale;
                sliderInReader.ValueChanged += SliderInReader_ValueChanged;
            }

            isSameScale = false;

            //RadioButton rb = FindVisualChildByName<RadioButton>(FR, "LockButton");
            //if (imageTransform.ScaleX != 1 || imageTransform.ScaleY != 1)
            //{
            //    LockButton.Visibility = Visibility.Visible;
            //}
            //else
            //{
            //    LockButton.Visibility = Visibility.Collapsed;
            //}

        }


        //上一頁，下一頁
        //換頁event
        private void bringBlockIntoView(int pageIndex)
        {

            //capture
            //Task.Factory.StartNew(() =>
            //{
            if (IsFirstCapture == false)
            {
                try
                {
                    //Thread.Sleep(1000);
                    TextBlock curPageInReader = FindVisualChildByName<TextBlock>(FR, "CurPageInReader");
                    TakeAPicture(int.Parse(curPageInReader.Text));

                }
                catch (Exception ex)
                {
                    LogTool.Debug(ex);
                }
            }
            //});

            //試閱
            if (trialPages != 0)
            {
                if (pageIndex > (trialPages - 1))
                {
                    return;
                }
            }

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

            if (isReadWindowLoaded)
            {
                sendBroadCast("{\"pageIndex\":" + pageIndex + ",\"cmd\":\"R.TP\"}");
            }
            //needToSendBroadCast = false;

            //Task.Factory.StartNew(() =>
            //{
            //    try
            //    {
            //        Thread.Sleep(300);
            //        MoveBoxPage();
            //    }
            //    catch (Exception ex)
            //    {
            //        LogTool.Debug(ex);
            //    }
            //});
        }

        private void BookMarkButton_Checked(object sender, RoutedEventArgs e)
        {
            //RadioButton BookMarkRB = (RadioButton)sender;
            RadioButton BookMarkRB = FindVisualChildByName<RadioButton>(FR, "BookMarkButton");
            if (viewStatusIndex.Equals(PageMode.SinglePage))
            {
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

                BookMarkRB.IsChecked = !isBookMarkClicked;
                TriggerBookMark_NoteButtonOrElse(BookMarkRB);
                sendBroadCast("{\"bookmark\":" + index + ",\"pageIndex\":" + curPageIndex.ToString() + ",\"cmd\":\"R.SB\"}");
            }
            else if (viewStatusIndex.Equals(PageMode.DoublePage))
            {
                ReadPagePair rpp = doubleReadPagePair[curPageIndex];

                //推算雙頁是哪兩頁的組合

                if (rpp.rightPageIndex == -1 || rpp.leftPageIndex == -1)
                {
                    bool isBookMarkClicked = false;
                    int targetPageIndex = Math.Max(rpp.rightPageIndex, rpp.leftPageIndex);

                    //封面或封底
                    if (bookMarkDictionary.ContainsKey(targetPageIndex))
                    {
                        //原來DB中有資料
                        if (bookMarkDictionary[targetPageIndex].status == "0")
                        {
                            isBookMarkClicked = true;
                        }
                    }

                    setBookMark(targetPageIndex, !isBookMarkClicked);

                    BookMarkRB.IsChecked = !isBookMarkClicked;
                    TriggerBookMark_NoteButtonOrElse(BookMarkRB);
                }
                else
                {
                    bool hasLeft = false;
                    bool hasRight = false;
                    if (bookMarkDictionary.ContainsKey(rpp.leftPageIndex))
                    {
                        if (bookMarkDictionary[rpp.leftPageIndex].status == "0")
                        {
                            hasLeft = true;
                        }
                    }

                    if (bookMarkDictionary.ContainsKey(rpp.rightPageIndex))
                    {
                        if (bookMarkDictionary[rpp.rightPageIndex].status == "0")
                        {
                            hasRight = true;
                        }
                    }

                    if (hasLeft || hasRight)
                    {
                        //兩頁中其中有一頁為有書籤, 兩頁只能取消
                        if (hasLeft)
                        {
                            setBookMark(rpp.leftPageIndex, false);
                        }

                        if (hasRight)
                        {
                            setBookMark(rpp.rightPageIndex, false);
                        }

                        BookMarkRB.IsChecked = false;
                        TriggerBookMark_NoteButtonOrElse(BookMarkRB);
                    }
                    else
                    {
                        //兩頁都無書籤, 加在右頁

                        setBookMark(rpp.rightPageIndex, true);
                        BookMarkRB.IsChecked = true;
                        TriggerBookMark_NoteButtonOrElse(BookMarkRB);
                    }
                }
            }

            if (CheckIsNowClick(BookMarkButtonInListBoxSP) == true)
            {
                ShowBookMark();
                ShowBookMark();
            }
        }


        private void doUpperRadioButtonClicked(MediaCanvasOpenedBy whichButton, object sender)
        {
            Canvas MediaTableCanvas = GetMediaTableCanvasInReader();
            StackPanel mediaListPanel = GetMediaListPanelInReader();

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
                            TextBox tb = FindVisualChildByName<TextBox>(FR, "notePanel");
                            if (tb != null)
                            {
                                int targetPageIndex = curPageIndex;


                                bool isSuccessd = setNotesInMem(tb.Text, targetPageIndex);

                                RadioButton NoteRB = FindVisualChildByName<RadioButton>(FR, "NoteButton");
                                if (tb.Text.Equals(""))
                                {
                                    NoteRB.IsChecked = false;
                                    TriggerBookMark_NoteButtonOrElse(NoteRB);
                                }
                                else
                                {
                                    NoteRB.IsChecked = true;
                                    TriggerBookMark_NoteButtonOrElse(NoteRB);
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
                        TextBox tb = FindVisualChildByName<TextBox>(FR, "notePanel");
                        if (tb != null)
                        {
                            int targetPageIndex = curPageIndex;

                            bool isSuccessd = setNotesInMem(tb.Text, targetPageIndex);

                            tb.Text = bookNoteDictionary.ContainsKey(targetPageIndex) ? bookNoteDictionary[targetPageIndex].text : "";
                            RadioButton NoteRB = FindVisualChildByName<RadioButton>(FR, "NoteButton");
                            if (tb.Text.Equals(""))
                            {
                                NoteRB.IsChecked = false;
                                TriggerBookMark_NoteButtonOrElse(NoteRB);
                            }
                            else
                            {
                                NoteRB.IsChecked = true;
                                TriggerBookMark_NoteButtonOrElse(NoteRB);
                                //Wayne取消
                                //sendBroadCast("{\"annotation\":\"" + tb.Text + "\",\"pageIndex\":" + targetPageIndex.ToString() + ",\"cmd\":\"R.SA\"}");
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


                resetFocusBackToReader();
                return;
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
                RadioButton rb = FindVisualChildByName<RadioButton>(FR, childNameInReader);
                rb.IsChecked = false;
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
                    case MediaCanvasOpenedBy.SearchButton:
                        sp = getSearchPanelSet(panelWidth, "");
                        //showPenToolPanelEventHandler(150, 400);
                        break;
                    case MediaCanvasOpenedBy.MediaButton:
                        sp = getMediaListFromXML();
                        //showPenToolPanelEventHandler(150, 400);
                        break;
                    case MediaCanvasOpenedBy.CategoryButton:
                        sp = getTocNcx();
                        //showPenToolPanelEventHandler(150, 400);
                        break;
                    case MediaCanvasOpenedBy.NoteButton:
                        sp = getNotesAndMakeNote();
                        //sendBroadCast("{\"cmd\":\"R.AA\"}");
                        //showPenToolPanelEventHandler(150, 400);
                        break;
                    case MediaCanvasOpenedBy.ShareButton:
                        sp = toShareBook();
                        //showPenToolPanelEventHandler(150, 400);
                        break;
                    case MediaCanvasOpenedBy.SettingButton:
                        sp = openSettings();
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
            //Mouse.Capture(mediaListPanel);


            //showPenToolPanelEventHandler(150, 400);
            resetFocusBackToReader();
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

        void noteButton_Click(object sender, RoutedEventArgs e)
        {
            //wayne add
            noteButton_Click();

        }

        private void TriggerBookMark_NoteButtonOrElse(RadioButton rb)
        {
            System.Windows.Controls.Image statusBMK = FindVisualChildTool.ByName<System.Windows.Controls.Image>(FR, "statusBMK");
            System.Windows.Controls.Image statusMemo = FindVisualChildTool.ByName<System.Windows.Controls.Image>(FR, "statusMemo");
            System.Windows.Media.Brush Orange = ColorTool.HexColorToBrush("#F66F00");
            System.Windows.Media.Brush Black = ColorTool.HexColorToBrush("#000000");
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

        private void noteButton_Click()
        {
            try
            {
                StackPanel mediaListPanel = GetMediaListPanelInReader();
                //mediaListPanel.RenderTransform = tfgForHyperLink;

                TextBox tb = FindVisualChildByName<TextBox>(FR, "notePanel");
                int targetPageIndex = curPageIndex;
                bool isSuccessd;
                if (tb != null)
                    isSuccessd = setNotesInMem(tb.Text, targetPageIndex);


                bookManager.saveNoteData(userBookSno, targetPageIndex.ToString(), tb.Text);
                RadioButton NoteRB = FindVisualChildByName<RadioButton>(FR, "NoteButton");

                if (tb != null && tb.Text.Equals(""))
                {
                    NoteRB.IsChecked = false;
                    TriggerBookMark_NoteButtonOrElse(NoteRB);
                }
                else
                {
                    NoteRB.IsChecked = true;
                    TriggerBookMark_NoteButtonOrElse(NoteRB);
                }

                Canvas MediaTableCanvas = GetMediaTableCanvasInReader();
                MediaTableCanvas.Visibility = Visibility.Collapsed;
                if (tb != null)
                {
                    sendBroadCast("{\"annotation\":\"" + tb.Text + "\",\"pageIndex\":" + targetPageIndex.ToString() + ",\"cmd\":\"R.SA\"}");
                    sendBroadCast("{\"cmd\":\"R.DPA\"}");
                }
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }
        }


        //放開滑鼠左鍵的event
        private void ReadWindow_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            StackPanel img = (StackPanel)GetImageInReader();
            TranslateTransform tt = (TranslateTransform)tfgForImage.Children[1];
            ScaleTransform imageTransform = (ScaleTransform)tfgForImage.Children[0];

            TranslateTransform ttHyperLink = (TranslateTransform)tfgForHyperLink.Children[1];
            ScaleTransform hyperLinkTransform = (ScaleTransform)tfgForHyperLink.Children[0];

            double ratioOfBounds = this.ActualHeight / this.ActualWidth;
            double ratioOfImage = img.ActualHeight / img.ActualWidth;

            if (imageTransform.ScaleX == 1 && imageTransform.ScaleY == 1)
            {
                if (sender is StackPanel)
                {
                    if (ratioOfBounds < ratioOfImage)
                    {
                        tt.X = 0;
                    }
                    else
                    {
                        tt.Y = 0;
                    }
                }
                else if (sender is Canvas)
                {
                    if (ratioOfBounds < ratioOfImage)
                    {
                        ttHyperLink.X = 0;
                        tt.X = 0;
                    }
                    else
                    {
                        ttHyperLink.Y = 0;
                        tt.Y = 0;
                    }
                }
                //this.sendBroadCast("{\"x\":0.500000,\"y\":0.500000,\"scale\":1.000000,\"cmd\":\"R.ZC\"}");
            }
            else
            {
                Border borderInReader = this.GetBorderInReader();
                Canvas visualChildByName = ReadWindow.FindVisualChildByName<Canvas>((DependencyObject)this.FR, "zoomCanvas");
                double num3 = Math.Abs(img.ActualWidth * imageTransform.ScaleX - this.ActualWidth * this.ratio);
                double num4 = Math.Abs(img.ActualHeight * imageTransform.ScaleY - this.ActualHeight * this.ratio);
                double num5 = visualChildByName.Height / img.ActualHeight;
                double num6 = tt.X.Equals(0.0) ? 0.5 : 0.5 - tt.X * num5 * 2.0 / (visualChildByName.Width * imageTransform.ScaleX);
                double num7 = (Math.Abs(tt.Y - num4 / 2.0) * num5 + borderInReader.ActualHeight / 2.0) / (visualChildByName.Height * imageTransform.ScaleY);
                double num8 = num4 / num3;
                this.sendBroadCast("{\"x\":" + num6.ToString() + ",\"y\":" + num7.ToString() + ",\"scale\":" + this.PDFScale.ToString() + ",\"cmd\":\"R.ZC\"}");
            }
            if (sender is StackPanel)
            {
                //紀錄這次放開後Image的中心點
                //if (isSameScale)
                //{
                imageOrigin = new System.Windows.Point(tt.X, tt.Y);
                //}
                //else
                //{
                //    tt.X = tt.Y = 0;
                //    isSameScale = true;
                //}

                ((StackPanel)sender).MouseMove -= ReadWindow_MouseMove;
                ((StackPanel)sender).PreviewMouseLeftButtonUp -= ReadWindow_PreviewMouseLeftButtonUp;
            }
            else if (sender is Canvas)
            {
                //紀錄這次放開後Image的中心點
                //if (isSameScale)
                //{
                imageOrigin = new System.Windows.Point(tt.X, tt.Y);
                hyperlinkOrigin = new System.Windows.Point(ttHyperLink.X, ttHyperLink.Y);
                //}
                //else
                //{
                //    tt.X = tt.Y = 0;
                //    ttHyperLink.X = ttHyperLink.Y = 0;
                //    isSameScale = true;
                //}
                ((Canvas)sender).MouseMove -= ReadWindow_MouseMove;
                ((Canvas)sender).PreviewMouseLeftButtonUp -= ReadWindow_PreviewMouseLeftButtonUp;
            }


            if (PaperLess_Emeeting.Properties.Settings.Default.AssemblyName.Contains("TPI4F") == false)
            {
                Canvas canvas = (Canvas)sender;
                //TextBlock curPageInReader = FindVisualChildByName<TextBlock>(FR, "CurPageInReader");
                //int pgNow = int.Parse(curPageInReader.Text);


                //TextBlock TotalPageInReader = FindVisualChildByName<TextBlock>(FR, "TotalPageInReader");
                //int pgCount = int.Parse(TotalPageInReader.Text);


                //RadioButton left = FindVisualChildByName<RadioButton>(FR, "leftPageButton");
                //RadioButton right = FindVisualChildByName<RadioButton>(FR, "rightPageButton");
                string tagData = canvas.Tag as string;
                switch (tagData)
                {
                    case "MoveUp":
                    case "MoveLeft":
                        //if (pgNow > 1)
                        //{
                        if (zoomStep == 0)
                        {
                            NavigationCommands.NextPage.Execute(null, null);
                        }
                        //}
                        break;
                    case "MoveDown":
                    case "MoveRight":
                        //if (pgNow < pgCount)
                        //{
                        if (zoomStep == 0)
                        {
                            NavigationCommands.PreviousPage.Execute(null, null);
                        }

                        //}
                        break;

                }
            }
            e.Handled = true;
        }


        public void TakeAPicture(int PageIndex)
        {
            try
            {
                //寫入圖檔，可以改成直接拍畫面的Bitmap後不存檔直接寫入
                //Border border = GetBorderInReader();
                //StackPanel image = (StackPanel)GetImageInReader();
                //double ratio = border.ActualHeight / image.ActualHeight;
                //double startX = (border.ActualWidth - image.ActualWidth * ratio) / 2;
                //double startY = (int)((SystemParameters.PrimaryScreenHeight - border.ActualHeight) / 2);

                //Bitmap b = new Bitmap((int)(image.ActualWidth * ratio), (int)border.ActualHeight);

                //using (Graphics g = Graphics.FromImage(b))
                //{

                //    g.CompositingQuality = CompositingQuality.HighQuality;
                //    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                //    g.CompositingMode = CompositingMode.SourceCopy;
                //    g.SmoothingMode = SmoothingMode.HighQuality;
                //    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                //    g.CopyFromScreen((int)startX, (int)startY + 20, 0, 0, b.Size, CopyPixelOperation.SourceCopy);
                //    g.Dispose();
                //}
                //string filePath = System.IO.Path.Combine(bookPath, "PDFFactory");

                //b.Save(System.IO.Path.Combine(filePath, PageIndex.ToString() + ".bmp"));
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }
        }

        private void firstTimeLoading()
        {
            //第一次進入

            #region Paperless Meeting

            if (this.isSyncing == false && this.isSyncOwner == false)
            {
                if (this.trialPages == 0)
                    this.initUserDataFromDB();
                else
                    ReadWindow.FindVisualChildByName<Canvas>((DependencyObject)this.FR, "zoomCanvas").Background = (System.Windows.Media.Brush)null;
            }
            this.initUserDataFromDB();
            #endregion

            //初始化上方按鈕
            iniUpperButtons();

            //上方的總頁數及目前頁數顯示
            TextBlock totalPageInReader = FindVisualChildByName<TextBlock>(FR, "TotalPageInReader");
            totalPageInReader.Text = singleThumbnailImageAndPageList.Count.ToString();

            TextBlock curPageInReader = FindVisualChildByName<TextBlock>(FR, "CurPageInReader");
            curPageInReader.Text = (curPageIndex + 1).ToString();


            //依左右翻書修正下方縮圖列, 左右翻頁
            WrapPanel wrapPanel = FindVisualChildByName<WrapPanel>(thumbNailListBox, "wrapPanel");
            if (hejMetadata.direction.Equals("right"))
            {
                wrapPanel.FlowDirection = FlowDirection.RightToLeft;

                RadioButton leftPageButton = FindVisualChildByName<RadioButton>(FR, "leftPageButton");
                //leftPageButton.PreviewMouseDown += (sender, e) =>
                //{
                //    //MoveBoxPage();
                //    //TakeAPicture(int.Parse(curPageInReader.Text));
                //};
                leftPageButton.CommandBindings.Clear();
                leftPageButton.Command = NavigationCommands.NextPage;
                var binding = new Binding();
                binding.Source = FR;
                binding.Path = new PropertyPath("CanGoToNextPage");
                BindingOperations.SetBinding(leftPageButton, RadioButton.IsEnabledProperty, binding);

                RadioButton rightPageButton = FindVisualChildByName<RadioButton>(FR, "rightPageButton");
                //rightPageButton.PreviewMouseDown += (sender, e) =>
                //{
                //    //MoveBoxPage();
                //    //TakeAPicture(int.Parse(curPageInReader.Text));
                //};
                rightPageButton.CommandBindings.Clear();
                rightPageButton.Command = NavigationCommands.PreviousPage;
                var rightbinding = new Binding();
                rightbinding.Source = FR;
                rightbinding.Path = new PropertyPath("CanGoToPreviousPage");
                BindingOperations.SetBinding(rightPageButton, RadioButton.IsEnabledProperty, rightbinding);

                KeyBinding leftKeySettings = new KeyBinding();
                KeyBinding rightKeySettings = new KeyBinding();

                InputBindings.Clear();

                leftKeySettings.Command = NavigationCommands.NextPage;
                leftKeySettings.Key = Key.Left;
                InputBindings.Add(leftKeySettings);

                rightKeySettings.Command = NavigationCommands.PreviousPage;
                rightKeySettings.Key = Key.Right;
                InputBindings.Add(rightKeySettings);
            }
            else
            {
                try
                {
                    wrapPanel.FlowDirection = FlowDirection.LeftToRight;
                }
                catch (Exception ex)
                {
                    //LogTool.Debug(ex);
                }
            }

            isFirstTimeLoaded = true;

            #region Paperless Meeting

            ToggleButton syncButton = FindVisualChildByName<ToggleButton>(FR, "syncButton");

            Singleton_Socket.ReaderEvent = this;
            this.socket = Singleton_Socket.GetInstance(meetingId, account, userName, isSyncing);
            if (isSyncing && socket != null)
            {
                syncButton.IsChecked = true;
                isSyncing = true;
                //socket.syncSwitch(true);
                clearDataWhenSync();
                resetTransform();

                //syncButton.Content = "Sync OFF";
                //LoadFirst_BookNote_BookMark();


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
            LoadFirst_BookNote_BookMark();
            this.needToSendBroadCast = false;


            #endregion

            //取得PageViewer, 控制鍵盤上下左右
            FR.PreviewLostKeyboardFocus += FR_PreviewLostKeyboardFocus;
            Keyboard.Focus(FR);

            //初始化Timer
            checkImageStatusTimer = new DispatcherTimer();
            checkImageStatusTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            checkImageStatusTimer.Tick += new EventHandler(checkImageStatus);

            this.needToSendBroadCast = false;
            Debug.WriteLine("@isFirstTimeLoaded");



        }

        private void LoadFirst_BookNote_BookMark()
        {
            RadioButton BookMarkRb = FindVisualChildByName<RadioButton>(FR, "BookMarkButton");
            if (bookMarkDictionary.ContainsKey(curPageIndex))
            {
                BookMarkRb.IsChecked = bookMarkDictionary[curPageIndex].status == "0" ? true : false;
                TriggerBookMark_NoteButtonOrElse(BookMarkRb);
            }
            else
            {
                BookMarkRb.IsChecked = false;
                TriggerBookMark_NoteButtonOrElse(BookMarkRb);
            }

            RadioButton NoteRB = FindVisualChildByName<RadioButton>(FR, "NoteButton");
            TextBox tb = FindVisualChildByName<TextBox>(FR, "notePanel");
            if (tb != null)
            {
                tb.Text = bookNoteDictionary[curPageIndex].text;
            }
            if (bookNoteDictionary.ContainsKey(curPageIndex))
            {
                if (bookNoteDictionary[curPageIndex].text.Equals(""))
                {
                    NoteRB.IsChecked = false;
                    TriggerBookMark_NoteButtonOrElse(NoteRB);
                }
                else
                {
                    NoteRB.IsChecked = true;
                    TriggerBookMark_NoteButtonOrElse(NoteRB);
                }
            }
            else
            {
                NoteRB.IsChecked = false;
                TriggerBookMark_NoteButtonOrElse(NoteRB);
            }
        }
        private void LastNext_Click(object sender, RoutedEventArgs e)
        {
            //Task.Factory.StartNew(() =>
            //    {
            //        try
            //        {
            //            MoveBoxPage();
            //        }
            //        catch (Exception ex)
            //        {
            //            LogTool.Debug(ex);
            //        }
            //    });
        }


        private StackPanel getNotesAndMakeNote()
        {
            StackPanel mediaListPanel = GetMediaListPanelInReader();
            double panelWidth = mediaListPanel.Width;
            double panelHeight = mediaListPanel.Height;
            // mediaListPanel.Height = defaultMediaListHeight;
            Border mediaListBorder = FindVisualChildByName<Border>(FR, "mediaListBorder");
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
                Name = "notePanelButton",
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

        void noteTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = FindVisualChildByName<TextBox>(FR, "notePanel");
            int targetPageIndex = curPageIndex;
            sendBroadCast("{\"annotation\":\"" + tb.Text + "\",\"pageIndex\":" + targetPageIndex.ToString() + ",\"cmd\":\"R.SA\"}");
        }


        private StackPanel getSearchPanelSet(double panelWidth, string txtInSearchBar)
        {
            StackPanel sp = new StackPanel();
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

        private void ShareButton_Checked(object sender, RoutedEventArgs e)
        {
            MouseTool.ShowLoading();
            SentMailSP.Background = ColorTool.HexColorToBrush("#F66F00");
            //SentMail();
            SendEmail();

            RadioButton rb = (RadioButton)sender;
            rb.IsChecked = false;
            //SentMailSP.Background = ColorTool.HexColorToBrush("#000000");
        }
        private void SendEmail()
        {
            //doUpperRadioButtonClicked(MediaCanvasOpenedBy.ShareButton, sender);
            //string url = "http://140.111.34.15/WebService/MeetingService.asmx/AnnotationUpload";  //正式機
            string url = webServiceURL;
            //string url = "http://web.emeeting.hyweb.com.tw/WebService/MeetingService.asmx/AnnotationUpload";  //post url，之後應該會要改成用正式機的

            string curAppPath = this.bookPath;
            string bookPath = curAppPath + "\\imgMail";
            Directory.CreateDirectory(bookPath);
            string imgFullPath = bookPath + "\\" + Guid.NewGuid() + ".jpg";  //完整路徑

            //寫入圖檔，可以改成直接拍畫面的Bitmap後不存檔直接寫入
            Border border = GetBorderInReader();
            StackPanel image = (StackPanel)GetImageInReader();
            double ratio = border.ActualHeight / image.ActualHeight;
            double startX = (border.ActualWidth - image.ActualWidth * ratio) / 2;
            double startY = (int)((SystemParameters.PrimaryScreenHeight - border.ActualHeight) / 2);

            // Set image source.
            //string width = image.ActualWidth.ToString();
            //string height = image.ActualHeight.ToString();
            //string RectString = string.Format("0,0,{0},{1}", width, height);
            string RectString = string.Format("0,0,{0},{1}", border.ActualWidth, border.ActualHeight);
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

                SentMailSP.Background = ColorTool.HexColorToBrush("#000000");
                MouseTool.ShowArrow();
            }

        }

        private void SentMail()
        {
            try
            {
                //doUpperRadioButtonClicked(MediaCanvasOpenedBy.ShareButton, sender);
                //string url = "http://140.111.34.15/WebService/MeetingService.asmx/AnnotationUpload";  //正式機
                string url = webServiceURL;
                //string url = "http://web.emeeting.hyweb.com.tw/WebService/MeetingService.asmx/AnnotationUpload";  //post url，之後應該會要改成用正式機的
                string imgFileName = hejMetadata.LImgList[curPageIndex].path;  //圖檔檔名
                string imgFullPath = this.bookPath + "\\" + imgFileName;  //完整路徑
                string meetingId = this.meetingId;  //會議ID
                string bookId = this.bookId;  //書檔ID
                string email = this.email;  //要寄到哪個EMAIL
                string note = "";

                if (bookNoteDictionary.ContainsKey(curPageIndex))
                {
                    note = bookNoteDictionary[curPageIndex].text;  //該頁的註記內容
                }

                //Multipart 變數
                string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");
                byte[] boundaryBytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
                string formDataTemplate = "\r\n--" + boundary + "\r\nContent-Disposition: form-data; name=\"{0}\";\r\n\r\n{1}";
                string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n Content-Type: application/octet-stream\r\n\r\n";

                //XML Request Body
                StringBuilder sb = new StringBuilder();
                sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>")
                    .Append("<UserInfo><MeetingID>").Append(meetingId).Append("</MeetingID><AttachID>")
                    .Append(bookId).Append("</AttachID><Email>")
                    .Append(email).Append("</Email><Text>").Append(note).Append("</Text></UserInfo>");

                Stream memStream = new System.IO.MemoryStream();

                string xmlDoc = string.Format(formDataTemplate, "xmlDoc", sb.ToString());
                byte[] formItemBytes = System.Text.Encoding.UTF8.GetBytes(xmlDoc);
                memStream.Write(formItemBytes, 0, formItemBytes.Length);

                memStream.Write(boundaryBytes, 0, boundaryBytes.Length);

                string header = string.Format(headerTemplate, "annotationImage", imgFileName);
                byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
                memStream.Write(headerbytes, 0, headerbytes.Length);

                //寫入圖檔，可以改成直接拍畫面的Bitmap後不存檔直接寫入
                Border border = GetBorderInReader();
                StackPanel image = (StackPanel)GetImageInReader();
                double ratio = border.ActualHeight / image.ActualHeight;
                double startX = (border.ActualWidth - image.ActualWidth * ratio) / 2;
                double startY = (int)((SystemParameters.PrimaryScreenHeight - border.ActualHeight) / 2);

                Bitmap b = new Bitmap((int)(image.ActualWidth * ratio), (int)border.ActualHeight);

                using (Graphics g = Graphics.FromImage(b))
                {
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    if (PaperLess_Emeeting.Properties.Settings.Default.IsFlatUIReader == true)
                        g.CopyFromScreen((int)startX, (int)startY, 0, 0, b.Size, CopyPixelOperation.SourceCopy);
                    else
                        g.CopyFromScreen((int)startX, (int)startY + 30, 0, 0, b.Size, CopyPixelOperation.SourceCopy);
                    g.Dispose();
                }

                string curAppPath = this.bookPath;
                string bookPath = curAppPath + "\\imgMail";
                Directory.CreateDirectory(bookPath);
                string FinalImgFullPath = bookPath + "\\" + Guid.NewGuid() + ".bmp";  //完整路徑

                //b.Save(bookPath + "\\temp.bmp");
                b.Save(FinalImgFullPath);

                //using (MemoryStream memoryStream = new MemoryStream())
                //{
                //    FileStream sourceStream = new FileStream(imgFullPath, FileMode.Open);
                //    sourceStream.CopyTo(memoryStream);
                //    decodedPDFPages[decodedPageIndex] = memoryStream.ToArray();
                //}
                //FileStream fileStream = new FileStream(bookPath + "\\temp.bmp", FileMode.Open, FileAccess.Read);
                // Wayne Add 20140826

                //FileStream fileStream = new FileStream(bookPath + "\\temp.bmp", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                FileStream fileStream = new FileStream(FinalImgFullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                byte[] buffer = new byte[1024];
                int bytesRead = 0;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    memStream.Write(buffer, 0, bytesRead);
                }
                try
                {
                    if (decodedPDFPages != null)
                        memStream.Write(decodedPDFPages[0], 0, decodedPDFPages[0].Length);
                }
                catch (Exception ex)
                {
                    LogTool.Debug(ex);
                }

                try
                {
                    memStream.Write(boundaryBytes, 0, boundaryBytes.Length);
                    //fileStream.Close();
                }
                catch (Exception ex)
                {
                    LogTool.Debug(ex);
                }


                // POST Request
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "multipart/form-data; boundary=" + boundary;
                request.ContentLength = memStream.Length;

                Stream dataStream = request.GetRequestStream();
                memStream.Position = 0;
                byte[] tempBuffer = new byte[memStream.Length];
                memStream.Read(tempBuffer, 0, tempBuffer.Length);
                memStream.Close();
                dataStream.Write(tempBuffer, 0, tempBuffer.Length);
                dataStream.Close();

                //Get Response
                WebResponse response = request.GetResponse();
                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                //textBox1.Text = responseFromServer;
                reader.Close();
                dataStream.Close();
                response.Close();
                request = null;
                response = null;

                //RadioButton rb = (RadioButton)sender;
                //rb.IsChecked = false;

                //MessageBox.Show("資料已送出");
                //Thread.Sleep(500);

                AutoClosingMessageBox.Show("資料已送出");
            }
            catch (Exception ex)
            {
                //MessageBox.Show("傳送失敗");
                //Thread.Sleep(500);

                AutoClosingMessageBox.Show("傳送失敗");
            }


        }

        private ListBox hyftdSearch(string keyWord)
        {
            string hyftdDir = bookPath + "\\HYWEB\\fulltext";

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
                    string filename = categoryNameArray[i].Replace(hyftdDir + "\\", "");

                    filename = filename.Replace(filename.Substring(filename.LastIndexOf('.')), "");

                    for (int k = 0; k < hejMetadata.SImgList.Count; k++)
                    {
                        if (hejMetadata.SImgList[k].path.Contains(filename))
                        {
                            SearchRecord sr = new SearchRecord(hejMetadata.SImgList[k].pageNum, htmlCode, k + 1);
                            sr.imagePath = bookPath + "\\" + hejMetadata.SImgList[k].path;
                            srList.Add(sr);
                        }
                    }
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

        //wayne add
        bool isFirstLoad = false;
        private void loadCurrentStrokes(int curIndex)
        {
            #region Paperless Meeting

            if (this.isSyncing && !this.isSyncOwner)
                return;

            #endregion

            Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");
            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");

            if (zoomCanvas.Width.Equals(Double.NaN) || zoomCanvas.Height.Equals(Double.NaN))
            {
                //大圖尚未初始化, 不做事
                return;
            }

            penMemoCanvas.Width = zoomCanvas.Width;
            penMemoCanvas.Height = zoomCanvas.Height;
            penMemoCanvas.RenderTransform = tfgForHyperLink;

            //由資料庫取回註記
            bookStrokesDictionary = bookManager.getStrokesDics(userBookSno);

            //從DB取資料
            isFirstLoad = true;
            if (bookStrokesDictionary.ContainsKey(curIndex))
            {
                List<StrokesData> curPageStrokes = bookStrokesDictionary[curIndex];
                int strokesCount = curPageStrokes.Count;
                for (int i = 0; i < strokesCount; i++)
                {
                    if (curPageStrokes[i].status == "0")
                    {
                        paintStrokeOnInkCanvas(curPageStrokes[i], zoomCanvas.Width, zoomCanvas.Height, 0, 0);
                    }
                }
                Task.Factory.StartNew(() =>
                {
                    if (CanSentLine == false)
                        Thread.Sleep(2000);
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        preparePenMemoAndSend();
                    }));
                });
            }
            isFirstLoad = false;
        }


        private void TextBlock_TargetUpdated_1(object sender, DataTransferEventArgs e)
        {
            if (doubleReadPagePair.Count == 0 || singleReadPagePair.Count == 0) //清除資料引發的事件
            {
                return;
            }



            decodedPDFPages[0] = null; decodedPDFPages[1] = null;  //清空已解密的PDF byte array
            Canvas stageCanvas = GetStageCanvasInReader();

            InkCanvas penMemoCanvas = FindVisualChildByName<InkCanvas>(FR, "penMemoCanvas");
            openedby = MediaCanvasOpenedBy.None;
            isAreaButtonAndPenMemoRequestSent = false;

            //清除上頁感應框及全文
            if (stageCanvas.Children.Count > 0)
            {
                stageCanvas.Children.Clear();
                RadioButton fTRB = FindVisualChildByName<RadioButton>(FR, "FullTextButton");
                fTRB.Visibility = Visibility.Collapsed;
            }

            //清除上頁螢光筆
            if (penMemoCanvas.Strokes.Count > 0)
            {
                convertCurrentStrokesToDB(hejMetadata.LImgList[curPageIndex].pageId);
                penMemoCanvas.Strokes.Clear();
            }

            if (this.splineString != "")
            {
                try
                {
                    this.drawStrokeFromJson(this.splineString);
                    this.splineString = "";
                }
                catch
                {
                }
            }
            if (this.closeBook)
            {
                try
                {
                }
                catch
                {
                }
            }

            TextBlock tb = (TextBlock)sender;
            if (tb == null)
            {
                //萬一為null
                return;
            }

            //系統初始化第一次loading
            if (isFirstTimeLoaded == false)
            {
                firstTimeLoading();

                //監聽螢光筆事件, 改由這裡直接存DB
                penMemoCanvas.StrokeCollected += penMemoCanvasStrokeCollected;
                penMemoCanvas.StrokeErasing += penMemoCanvas_StrokeErasing;
                penMemoCanvas.StrokeErased += penMemoCanvas_StrokeErased;

                if (lastViewPage == null)
                {
                    //試閱中
                    showLastReadPageAndStartPreload();
                    ifAskedJumpPage = true;
                    return;
                }


                if (lastViewPage.ContainsKey(CName))
                {
                    //if (lastViewPage[CName].index != 0)
                    if (lastViewPage[CName].index != 0 && isSyncing == false)
                    {
                        //非第一頁, 直接跳頁, 先不顯示小圖
                        Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");
                        BrushConverter bc = new BrushConverter();
                        zoomCanvas.Background = (System.Windows.Media.Brush)bc.ConvertFrom("#FF212020");
                        isFirstTimeChangingPage = true;
                        return;
                    }
                    else
                    {
                        showLastReadPageAndStartPreload();
                    }
                }
                else
                {
                    showLastReadPageAndStartPreload();
                }
                return;
            }
            else
            {
                curPageIndex = Convert.ToInt32(tb.Text) - 1;
                if (isFirstTimeChangingPage)
                {
                    showLastReadPageAndStartPreload();
                    isFirstTimeChangingPage = false;
                }

                Canvas zoomCanvas = FindVisualChildByName<Canvas>(FR, "zoomCanvas");
                zoomCanvas.Background = null;

                zoomeThread.Clear();
                isPDFRendering = false;

                //capture
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(1500);
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {

                            TakeAPicture(curPageIndex + 1);
                            IsFirstCapture = false;
                        }
                        catch (Exception ex)
                        {
                            LogTool.Debug(ex);
                        }
                    }));
                });

            }

            //翻書時間, 為避免快翻所記錄
            lastTimeOfChangingPage = DateTime.Now;

            //Preload下有預先load好的頁面, 直接放到Canvas中
            if (viewStatusIndex.Equals(PageMode.SinglePage))
            {
                if (singleReadPagePair[curPageIndex].leftImageSource != null)
                {
                    useOriginalCanvasOnLockStatus = true;
                    try
                    {
                        SendImageSourceToZoomCanvas((BitmapImage)singleReadPagePair[curPageIndex].leftImageSource);
                        singleImgStatus[curPageIndex] = ImageStatus.LARGEIMAGE;
                        Debug.WriteLine("SendImageSourceToZoomCanvas@TextBlock_TargetUpdated_1");
                    }
                    catch (Exception ex)
                    {
                        //還沒有指派到ImageSource中, 當作沒有ren好
                        singleReadPagePair[curPageIndex].leftImageSource = null;
                        Debug.WriteLine(ex.Message.ToString());

                    }
                }
            }
            else if (viewStatusIndex.Equals(PageMode.DoublePage))
            {
                if (doubleReadPagePair[curPageIndex].leftImageSource != null)
                {
                    useOriginalCanvasOnLockStatus = true;
                    try
                    {
                        SendImageSourceToZoomCanvas((BitmapImage)doubleReadPagePair[curPageIndex].leftImageSource);
                        doubleImgStatus[curPageIndex] = ImageStatus.LARGEIMAGE;
                        Debug.WriteLine("SendImageSourceToZoomCanvas@TextBlock_TargetUpdated_1");
                    }
                    catch (Exception ex)
                    {
                        //還沒有指派到ImageSource中, 當作沒有ren好
                        doubleReadPagePair[curPageIndex].leftImageSource = null;
                        Debug.WriteLine(ex.Message.ToString());

                    }
                }
            }

            // wayne 20140825 改為放大鎖定
            //如為放大鎖定狀態, 則換頁後重ren
            if (1 == 1 || isLockButtonLocked && bookType.Equals(BookType.PHEJ))
            //if (isLockButtonLocked && bookType.Equals(BookType.PHEJ))
            {
                Debug.WriteLine("RepaintPDF@TextBlock_TargetUpdated_1");
                int i = 0;

                //RepaintPDF(zoomStepScale[zoomStep]);

                //wayne add  20140825
                try
                {
                    RepaintPDF(zoomStepScale[zoomStep]);
                }
                catch (Exception ex)
                {
                    if (i++ <= 3)
                    {
                        RepaintPDF(zoomStepScale[zoomStep]);
                    }
                }
            }

            //上方書籤以及註記狀態
            if (curPageIndex < hejMetadata.SImgList.Count)
            {
                if (curPageIndex < 0)
                {
                    return;
                }

                if (!isSyncing)
                {
                    //由資料庫取回書籤資料
                    bookMarkDictionary = bookManager.getBookMarkDics(userBookSno);

                    //由資料庫取回註記
                    bookNoteDictionary = bookManager.getBookNoteDics(userBookSno);
                }

                RadioButton BookMarkRb = FindVisualChildByName<RadioButton>(FR, "BookMarkButton");
                RadioButton NoteRb = FindVisualChildByName<RadioButton>(FR, "NoteButton");

                if (viewStatusIndex.Equals(PageMode.SinglePage))//單頁
                {
                    if (curPageIndex > singleThumbnailImageAndPageList.Count - 1)
                    {
                        return;
                    }
                    thumbNailListBox.SelectedItem = singleThumbnailImageAndPageList[curPageIndex];

                    TextBlock curPageInReader = FindVisualChildByName<TextBlock>(FR, "CurPageInReader");
                    curPageInReader.Text = (curPageIndex + 1).ToString();

                    if (bookMarkDictionary.ContainsKey(curPageIndex))
                    {
                        if (bookMarkDictionary[curPageIndex].status == "0")
                        {
                            BookMarkRb.IsChecked = true;
                            TriggerBookMark_NoteButtonOrElse(BookMarkRb);
                        }
                        else
                        {
                            BookMarkRb.IsChecked = false;
                            TriggerBookMark_NoteButtonOrElse(BookMarkRb);
                        }
                    }
                    else
                    {
                        BookMarkRb.IsChecked = false;
                        TriggerBookMark_NoteButtonOrElse(BookMarkRb);
                    }

                    if (bookNoteDictionary.ContainsKey(curPageIndex))
                    {
                        if (bookNoteDictionary[curPageIndex].status == "0")
                        {
                            NoteRb.IsChecked = true;
                            TriggerBookMark_NoteButtonOrElse(NoteRb);
                        }
                        else
                        {
                            NoteRb.IsChecked = false;
                            TriggerBookMark_NoteButtonOrElse(NoteRb);
                        }
                    }
                    else
                    {
                        NoteRb.IsChecked = false;
                        TriggerBookMark_NoteButtonOrElse(NoteRb);
                    }
                }
                else if (viewStatusIndex.Equals(PageMode.DoublePage))
                {

                    ReadPagePair rpp = doubleReadPagePair[curPageIndex];

                    //下方縮圖列focus在頁數大的那頁
                    int doubleIndex = Math.Max(rpp.rightPageIndex, rpp.leftPageIndex);

                    if (doubleIndex < thumbNailListBox.Items.Count)
                    {
                        thumbNailListBox.SelectedItem = thumbNailListBox.Items[doubleIndex];
                    }

                    //推算雙頁是哪兩頁的組合
                    if (rpp.rightPageIndex == -1 || rpp.leftPageIndex == -1)
                    {
                        bool isBookMarkClicked = false;
                        int targetPageIndex = Math.Max(rpp.rightPageIndex, rpp.leftPageIndex);

                        //封面或封底
                        if (bookMarkDictionary.ContainsKey(targetPageIndex))
                        {
                            //原來DB中有資料
                            if (bookMarkDictionary[targetPageIndex].status == "0")
                            {
                                isBookMarkClicked = true;
                            }
                        }

                        TextBlock curPageInReader = FindVisualChildByName<TextBlock>(FR, "CurPageInReader");
                        curPageInReader.Text = (targetPageIndex + 1).ToString();

                        BookMarkRb.IsChecked = isBookMarkClicked;
                        TriggerBookMark_NoteButtonOrElse(BookMarkRb);
                    }
                    else
                    {
                        TextBlock curPageInReader = FindVisualChildByName<TextBlock>(FR, "CurPageInReader");
                        curPageInReader.Text = ((rpp.leftPageIndex + 1) + "-" + (rpp.rightPageIndex + 1)).ToString();

                        bool hasLeft = false;
                        bool hasRight = false;
                        if (bookMarkDictionary.ContainsKey(rpp.leftPageIndex))
                        {
                            if (bookMarkDictionary[rpp.leftPageIndex].status == "0")
                            {
                                hasLeft = true;
                            }
                        }

                        if (bookMarkDictionary.ContainsKey(rpp.rightPageIndex))
                        {
                            if (bookMarkDictionary[rpp.rightPageIndex].status == "0")
                            {
                                hasRight = true;
                            }
                        }

                        if (hasLeft || hasRight)
                        {
                            //兩頁中其中有一頁為有書籤, 顯示有
                            BookMarkRb.IsChecked = true;
                            TriggerBookMark_NoteButtonOrElse(BookMarkRb);
                        }
                        else
                        {
                            //兩頁都無書籤, 顯示無
                            BookMarkRb.IsChecked = false;
                            TriggerBookMark_NoteButtonOrElse(BookMarkRb);
                        }
                    }
                }
            }
            checkOtherDevicePage();


        }

        #endregion

        private void btnJoin_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
