using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaperLess_Emeeting.App_Code.DownloadItem;
using PaperLess_Emeeting.App_Code.MessageBox;
using PaperLess_Emeeting.App_Code.Socket;
using PaperlessSync.Broadcast.Service;
using PaperlessSync.Broadcast.Socket;
using System;
using System.Collections.Generic;
using System.Data;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;

namespace PaperLess_Emeeting
{
    /// <summary>
    /// BroadcastCT.xaml 的互動邏輯
    /// </summary>
    public partial class PDFFactoryCT : UserControl
    {
        public string MeetingID { get; set; }
        public string UserID { get; set; }
        public bool CanDetectServerState = true;
        public MeetingData md;
        public bool HasRecordFile = false;
        int All_FileCount;
        public PDFFactoryCT()
        {
           
            MouseTool.ShowLoading();
            InitializeComponent();
            this.Loaded += PDFFactoryCT_Loaded;
            this.Unloaded += PDFFactoryCT_Unloaded;
            
        }

        private void PDFFactoryCT_Unloaded(object sender, RoutedEventArgs e)
        {
           
            
            //if (tokenSource != null)
            //    tokenSource.Cancel();
            CanDetectServerState = false;
        }

        private void PDFFactoryCT_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                InitSelectDB();
                // 只要是 CT 主要畫面，優先權設定為Send，因為設定Normal，按鈕的出現會感覺卡卡的。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        //InitUI();
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

        private void InitSelectDB()
        {
            DataTable dt = MSCE.GetDataTable("select MeetingID,UserID from NowLogin");
            if (dt.Rows.Count > 0)
            {
                this.MeetingID = dt.Rows[0]["MeetingID"].ToString().Trim();
                this.UserID = dt.Rows[0]["UserID"].ToString().Trim();
            }
            //DB查詢登入
            dt = MSCE.GetDataTable("select MeetingJson from MeetingData where MeetingID=@1 and UserID =@2"
                                    , MeetingID
                                    , UserID);

            if (dt.Rows.Count > 0)
            {
                md = JsonConvert.DeserializeObject<MeetingData>(dt.Rows[0]["MeetingJson"].ToString());

                //Task.Factory.StartNew(() =>
                //{
                    //this.Dispatcher.BeginInvoke(new Action(() =>
                    //{
                    List<MeetingDataDownloadFileFile> FileList = new List<MeetingDataDownloadFileFile>();


                    try
                    {
                        // <File ID="cAS66-P" Url="http://com-meeting.ntpc.hyweb.com.tw/Public/MeetingAttachFile/2/2-b167-P.phej" FileName="ae717047" version="1"/>

                        // 如果meetingData.MeetingsFile.FileList沒有子節點，就會轉型失敗
                        //XmlNode[] FileListXml = (XmlNode[])md.MeetingsFile.FileList;
                        //foreach (XmlNode item in FileListXml)
                        foreach (MeetingDataMeetingsFileFile item in md.MeetingsFile.FileList)
                        {
                            MeetingDataDownloadFileFile recordFile = new MeetingDataDownloadFileFile();
                            recordFile.AgendaID = "record";
                            //recordFile.FileName = item.Attributes["FileName"].Value;
                            //recordFile.ID = item.Attributes["ID"].Value;
                            //recordFile.Url = item.Attributes["Url"].Value;
                            //recordFile.version = byte.Parse(item.Attributes["version"].Value);
                            recordFile.FileName = item.FileName;
                            recordFile.ID = item.ID;
                            recordFile.Url = item.Url;
                            recordFile.version = item.version;
                            FileList.Add(recordFile);
                            HasRecordFile = true;
                        }
                        //if (HasRecordFile == true)
                        //{
                        //    this.Dispatcher.BeginInvoke(new Action(() =>
                        //    {
                        //        //btnRecord.Visibility = Visibility.Visible;
                        //    }));
                        //}


                    }
                    catch (Exception ex)
                    {
                        // 這裡不要寫Log好了
                        //LogTool.Debug(ex);
                    }
                    //foreach (MeetingDataMeetingsFileFile item in meetingData.MeetingsFile.FileList)
                    //{
                    //    MeetingDataDownloadFileFile recordFile = new MeetingDataDownloadFileFile();
                    //    recordFile.AgendaID = "record";
                    //    recordFile.FileName = item.FileName;
                    //    recordFile.ID = item.ID;
                    //    recordFile.Url = item.Url;
                    //    recordFile.version = item.version;
                    //    FileList.Add(recordFile);
                    //}

                    FileList.AddRange(md.DownloadFile.DownloadFileList.ToList());
                    All_FileCount = FileList.Count;
                    //Task.Factory.StartNew(() =>
                    //{
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            // (7)加入檔案
                            int i = 0;
                            //foreach (MeetingDataDownloadFileFile item in meetingData.DownloadFile.DownloadFileList)
                            foreach (MeetingDataDownloadFileFile item in FileList)
                            {
                                i++;
                                File_DownloadItemViewModel fileItem = FileItemTool.Gen(item, UserID, MeetingID);
                                PDFFactoryRowSP.Children.Add(new PDFFactoryRow(i, fileItem));
                                //bool IsLastRow = (i == FileList.Count);
                                //int mutiThreadIndex = i;

                                //if (item.AgendaID.Equals("") == true || item.AgendaID.Equals("c") == true || item.AgendaID.Equals("i") == true)
                                //{
                                //    HasSubjectFile = true;
                                //    imgSubject.Visibility = Visibility.Visible;
                                //}
                                //FileRowSP.Children.Add(new FileRow(UserID, UserName, UserPWD, meetingData.ID, UserEmail
                                //                                   , mutiThreadIndex, IsLastRow, item
                                //                                   , MeetingDataCT_RaiseAllDownload_Callback
                                //                                   , MeetingDataCT_HangTheDownloadEvent_Callback
                                //                                   , MeetingDataCT_IsAllFileRowFinished_AddInitUIFinished_Callback
                                //                                   , MeetingDataCT_GetBookVMs_ByMeetingFileCate_Callback
                                //                                   , MeetingDataCT_GetWatermark_Callback
                                //                                   , meetingRoomButtonType));

                            }
                        }));
                    //}));

                //});
            }
            else
            {
                AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
                MouseTool.ShowArrow();
            }
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
                foreach (PDFFactoryRow item in PDFFactoryRowSP.Children.OfType<PDFFactoryRow>())
                {
                    if (item.txtIndex.Text.Contains(keyword) == true || item.txtFileName.Text.Contains(keyword) == true
                        || item.txtStatus.Text.Contains(keyword) == true)
                    {
                        item.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        item.Visibility = Visibility.Collapsed;
                    }

                };
            }
            else
            {
                foreach (PDFFactoryRow item in PDFFactoryRowSP.Children.OfType<PDFFactoryRow>())
                {
                    item.Visibility = Visibility.Visible;
                };
            }
           
        }

        private void ClearList()
        {
            //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                //this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action(ClearList));
                this.Dispatcher.BeginInvoke(new Action(ClearList));
            }
            else
            {
                //BroadcastRowSP.Children.Clear();
            }
        }


        private void ChangeServerCtrl(bool Online)
        {
            if (Online == true)
            {
                txtStatus.Text = "連線中";
                txtStatus.Foreground = ColorTool.HexColorToBrush("#E2F540");
                txtStatus.HorizontalAlignment = HorizontalAlignment.Left;
                btnStatus.Source = new BitmapImage(new Uri("images/btn_broadcast_connected.png", UriKind.Relative));
            }
            else
            {
                txtStatus.Text = "未啟動";
                txtStatus.Foreground = ColorTool.HexColorToBrush("#707A82");
                txtStatus.HorizontalAlignment = HorizontalAlignment.Center;
                btnStatus.Source = new BitmapImage(new Uri("images/btn_broadcast_broken.png", UriKind.Relative));
            }
        }


        private void InitUI()
        {

        }


        public bool StopSyncServer(string meetingID)
        {
            bool rtn = false;
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("<?xml version=\"1.0\" encoding=\"UTF-8\"?>")
                  .AppendFormat("<Sync>")
                  .AppendFormat("<Stop ID=\"{0}\" />",MeetingID)
                  .AppendFormat("</Sync>");

                if (PostToSyncServer("/StopSyncServer", sb.ToString()).Contains("成功"))
                    rtn = true;
            }
            catch (Exception ex)
            {
                rtn = false;
                LogTool.Debug(ex);
            }

            return rtn;
        }


        public bool SyncServerAlreadyStarted(string meetingID)
        {
            bool rtn = false;
            try
            {
                 StringBuilder sb = new StringBuilder();
                 sb.AppendFormat("<?xml version=\"1.0\" encoding=\"UTF-8\"?>")
                   .AppendFormat("<MeetingList date=\"{0}\" >", DateTime.Now.ToString("yyyyMMddHHmmss"))
                   .AppendFormat("</MeetingList>");

                 XDocument xml = XDocument.Parse(PostToSyncServer("/GetMeetingList", sb.ToString()));
                 var q = from x in xml.Element("MeetingList").Elements("Meeting")
                         select new
                         {
                             ID = x.Attribute("ID").Value.Trim(),
                         };
                 foreach (var item in q)
                 {
                     if (item.ID.Equals(meetingID) == true)
                         return true;
                 }
               
            }
            catch(Exception ex)
            {
                rtn = false;
                LogTool.Debug(ex);
            }

            return rtn;
        }

        private string PostToSyncServer(string subUrl,string sentXml )
        {
            string getXml = "";
            try
            {
                string SyncServerUrl = SocketTool.GetUrl();
                string SyncServerUrl_Imp = SocketTool.GetUrl_Imp();
                if (MeetingID.ToLower().StartsWith("i") == true)
                {
                    SyncServerUrl = SyncServerUrl_Imp;
                }
                
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(SyncServerUrl + subUrl);
                string data = sentXml;
                byte[] postData = Encoding.UTF8.GetBytes(data);

                request.Method = "POST";
                request.ContentType = "text/xml; encoding='utf-8'";
                request.ContentLength = postData.Length;

                Stream dataStream = request.GetRequestStream();
                dataStream.Write(postData, 0, postData.Length);
                dataStream.Close();

                WebResponse response = request.GetResponse();
                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                getXml = reader.ReadToEnd();
                reader.Close();
                dataStream.Close();
                response.Close();
            }
            catch(Exception ex)
            {
                LogTool.Debug(ex);
            }

            return getXml;
        }

        public  bool StartSyncServer(string meetingID)
        {
            bool rtn = false;
            //string getXml = "";

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>")
                   .Append("<Sync>")
                   .AppendFormat("<Start ID=\"{0}\" MaxClient=\"{1}\" />", meetingID, 100)
                   .AppendFormat("<Init>{0}</Init>", PaperLess_Emeeting.Properties.Settings.Default.InitConfig)
                   .Append("</Sync>");
      
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(PostToSyncServer("/StartSyncServer", sb.ToString()));
                XmlNode root = doc.DocumentElement;
                string ip = root.SelectSingleNode("/Sync/Start/@IP").Value;
                int port = int.Parse(root.SelectSingleNode("/Sync/Start/@Port").Value);
                if (ip.Equals("") == false && port >= 1 && port <= 65535)
                    rtn = true;
            }
            catch(Exception ex)
            {
                rtn = false;
                LogTool.Debug(ex);
            }

            return rtn;
        }
    }

    
}
