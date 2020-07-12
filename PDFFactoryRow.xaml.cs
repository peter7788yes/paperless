using Newtonsoft.Json;
using PaperLess_Emeeting.App_Code;
using PaperLess_Emeeting.App_Code.DownloadItem;
using PaperLess_Emeeting.App_Code.MessageBox;
using PaperLess_Emeeting.App_Code.Socket;
using PaperLess_Emeeting.App_Code.Tools;
using PaperlessSync.Broadcast.Socket;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PaperLess_Emeeting
{
    /// <summary>
    /// BroadcastRow.xaml 的互動邏輯
    /// </summary>
    public partial class PDFFactoryRow : UserControl
    {
        public int index { get; set; }
        public File_DownloadItemViewModel fileItem { get; set; }
        public PDFStatus pdfStatus;
        public DispatcherTimer timer = null;
        Storyboard sb;
        // 帳號,姓名,裝置,燈號
        public PDFFactoryRow(int index, File_DownloadItemViewModel fileItem)
        {
            //MouseTool.ShowLoading();
            InitializeComponent();
            this.index = index;
            sb = (Storyboard)this.TryFindResource("sb");
            this.fileItem = fileItem;
            this.Loaded += PDFFactoryRow_Loaded;
            //MouseTool.ShowArrow();
        }

        private void PDFFactoryRow_Loaded(object sender, RoutedEventArgs e)
        {
            this.txtIndex.Text = index.ToString();
            this.txtFileName.Text = fileItem.FileName;

            Task.Factory.StartNew(() =>
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    InitUI();
                    InitEvent();
                }));
               
            });
           
        }

        private void InitEvent()
        {
            btnExport.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnExport.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnExport.MouseLeftButtonDown += (sender, e) => 
            {
                Task.Factory.StartNew(()=>{
                    Singleton_PDFFactory.AddBookInPDFWork(fileItem.ID);
                    this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            btnExport.Visibility = Visibility.Collapsed;
                            btnDownload.Visibility = Visibility.Collapsed;
                            sb.Begin();
                            DataTable dt = MSCE.GetDataTable("select PDFFactoryParameterJson from FileRow where userid=@1 and id=@2"
                                               , fileItem.UserID
                                               , fileItem.ID);
                            if (dt.Rows.Count > 0)
                            {
                            
                                PDFFactoryParameter pdfFactoryParameter = JsonConvert.DeserializeObject<PDFFactoryParameter>(dt.Rows[0][0].ToString());
                                Home home = Application.Current.Windows.OfType<Home>().First();
                                
                                if(home.IsInSync ==true)
                                {
                                    pdfFactoryParameter.UserAccount += "_Sync";
                                }

                                Singleton_PDFFactory.SavePDF(pdfFactoryParameter);
                            }


                        }));
                    Singleton_PDFFactory.RemoveBookInPDFWork(fileItem.ID);
                });
            };

            btnDownload.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnDownload.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnDownload.MouseLeftButtonDown += (sender, e) => 
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                //Environment.SpecialFolder.MyDocuments
                //This cannot be found as it is not a valid path, so nothing gets selected.
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                dlg.FileName = FileNameTool.PureFileName(fileItem.FileName); // Default file name
                dlg.DefaultExt = ".pdf"; // Default file extension
                dlg.Filter = "PDF documents (.pdf)|*.pdf"; // Filter files by extension
                // Show save file dialog box
                Nullable<bool> result = dlg.ShowDialog();
                // Process save file dialog box results
                if (result == true)
                {
                    // Save document
                    string srcFilePath=System.IO.Path.Combine(fileItem.UnZipFilePath,"PDF.pdf");
                    string saveFilePath = dlg.FileName;
                    if (File.Exists(srcFilePath) == true)
                    {
                        File.Copy(srcFilePath, saveFilePath,true);
                    }
                }
                
            };


            //偵測是否轉檔中
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1100);
            timer.Tick += (sender, e) =>
            {
                if (Singleton_PDFFactory.IsPDFInWork(fileItem.ID) == true)
                {
                    //this.Dispatcher.BeginInvoke(new Action(() =>
                    //    {
                    //        //Console.Write(sb.GetIsPaused());
                    //        //sb.Begin();
                    //    }));
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        pdfStatus = PDFStatus.匯出中;
                        btnExport.Visibility = Visibility.Collapsed;
                        btnDownload.Visibility = Visibility.Collapsed;
                        txtStatus.Text = pdfStatus.ToString();
                    }));
                }
                else if (File.Exists(System.IO.Path.Combine(fileItem.UnZipFilePath, "PDF.pdf")) == true && fileItem.FileType == MeetingFileType.已下載完成)
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                       
                        sb.Stop();
                        pdfStatus = PDFStatus.匯出成功;
                        btnExport.Visibility = Visibility.Visible;
                        btnDownload.Visibility = Visibility.Visible;
                        txtStatus.Text = pdfStatus.ToString();
                    }));
                   
                }
                else
                {

                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        sb.Stop();
                        pdfStatus = PDFStatus.尚未匯出;
                        btnExport.Visibility = Visibility.Collapsed;
                        btnDownload.Visibility = Visibility.Collapsed;
                        txtStatus.Text = pdfStatus.ToString();
                    })); 
                }
            };
            timer.Start();

        }



       

        private void InitUI()
        {
            //this.txtIndex.Text = index.ToString();
            //this.txtFileName.Text = fileItem.FileName;

            sb.Begin();
            if (Singleton_PDFFactory.IsPDFInWork(fileItem.ID) == true)
            {
                //PDFStatus.匯出中;
                sb.Begin();
            }
            else if (File.Exists(System.IO.Path.Combine(fileItem.UnZipFilePath, "PDF.pdf")) == true && fileItem.FileType == MeetingFileType.已下載完成)
            {
                sb.Stop();
                pdfStatus = PDFStatus.匯出成功;
                txtStatus.Text = pdfStatus.ToString();
            }
            else
            {
                sb.Stop();
                pdfStatus = PDFStatus.尚未匯出;
                txtStatus.Text = pdfStatus.ToString();
            }
           
            switch (pdfStatus)
            {
                case PDFStatus.匯出成功:
                    btnExport.Visibility = Visibility.Visible;
                    btnDownload.Visibility = Visibility.Visible;
                    break;
            }
        }

       
    }

   
}
