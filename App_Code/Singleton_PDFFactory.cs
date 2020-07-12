using BookManagerModule;
using DataAccessObject;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json;
using PaperLess_Emeeting.App_Code.MessageBox;
using PaperLess_Emeeting.App_Code.WS;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PaperLess_Emeeting.App_Code
{
    //取得各個會議室的下載器
    public class Singleton_PDFFactory
    {
        // 多執行緒，lock 使用
        private static readonly object thisLock = new object();

        // 將唯一實例設為 private static
        // 餓漢方式(Eager initialization)：class 載入時就產生實例，不管後面會不會用到。
        private static List<string> instance = new List<string>();

        public static Home_UnZipError_Function Home_UnZipError_Callback;

        // 設為 private，外界不能 new
        // 重要
        private Singleton_PDFFactory()
        {
        }

        // 使用靜態方法取得實例，因為載入時就 new 一個實例，所以不用考慮多執行緒的問題
        public static List<string> GetInstance()
        {
            return instance;
        }

        // 使用靜態方法取得實例，因為載入時就 new 一個實例，所以不用考慮多執行緒的問題
        public static bool IsPDFInWork(string BookID)
        {
            bool rtn;
            if (instance.Contains(BookID) == true)
            {
                rtn = true;
            }
            else
            {
                rtn = false;
            }

            return rtn;
        }

        // 使用靜態方法取得實例，因為載入時就 new 一個實例，所以不用考慮多執行緒的問題
        public static void AddBookInPDFWork(string BookID)
        {
            RemoveBookInPDFWork(BookID);
            instance.Add(BookID);
        }

        // 使用靜態方法取得實例，因為載入時就 new 一個實例，所以不用考慮多執行緒的問題
        public static void RemoveBookInPDFWork(string BookID)
        {
            instance.RemoveAll(x => x.Equals(BookID));
        }


        public static void ClearInstance()
        {
            instance.Clear();
        }

        public static void SavePDF(PDFFactoryParameter pdfFactoryParameter)
        {
            SavePDF(pdfFactoryParameter.isHtml, pdfFactoryParameter.bookPath, pdfFactoryParameter.totalPage, pdfFactoryParameter.width, pdfFactoryParameter.height, pdfFactoryParameter.UserAccount, pdfFactoryParameter.bookId, pdfFactoryParameter.dbPath, pdfFactoryParameter.thumbsPath_Msize, pdfFactoryParameter.thumbsPath_Lsize);
        }

        public static void SavePDF(bool isHtml, string bookPath, int totalPage, float width, float height, string UserAccount, string bookId, string dbPath, string thumbsPath_Msize, string thumbsPath_Lsize, int counter = 0)
        {
            //把參數存起來
            if (counter == 0)
            {
                PDFFactoryParameter pdfParameter = new PDFFactoryParameter();
                pdfParameter.isHtml = isHtml;
                pdfParameter.bookPath = bookPath;
                pdfParameter.totalPage = totalPage;
                pdfParameter.width = width;
                pdfParameter.height = height;
                pdfParameter.UserAccount = UserAccount;
                pdfParameter.bookId = bookId;
                pdfParameter.dbPath = dbPath;
                pdfParameter.thumbsPath_Msize = thumbsPath_Msize;
                pdfParameter.thumbsPath_Lsize = thumbsPath_Lsize;

                DataTable dt = MSCE.GetDataTable("select ID from FileRow where userid=@1 and id=@2"
                                                , UserAccount.Replace("_Sync", "")
                                                , bookId);

                if (dt.Rows.Count > 0)
                {
                    string ID = dt.Rows[0]["ID"].ToString();
                    string SQL = @"update  FileRow set PDFFactoryParameterJson=@1 where userid=@2 and id=@3";
                    int success = MSCE.ExecuteNonQuery(SQL
                                                , JsonConvert.SerializeObject(pdfParameter)
                                                , UserAccount.Replace("_Sync", "")
                                                , bookId);
                    if (success < 1)
                        LogTool.Debug(new Exception(@"DB失敗: " + SQL));
                }
            }


            string fileName = System.IO.Path.Combine(bookPath, @"PDFFactory\PDF.pdf");
            string PDFFactoryDirectoryName = Path.GetDirectoryName(fileName);
            string FinalFilePath = System.IO.Path.Combine(bookPath, "PDF.pdf");
            Directory.CreateDirectory(PDFFactoryDirectoryName);
            //using (StreamWriter sw = new StreamWriter(fileName))
            //{
            //}
            //File.Create(fileName).Dispose();
            //string srcPDF = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pdf.pdf");
            //File.Copy(srcPDF, fileName, true);
            //Thread th = new Thread(()=>
            Task.Factory.StartNew(() =>
            {
                try
                {
                    Singleton_PDFFactory.AddBookInPDFWork(bookId);
                    BookManager bookManager = new BookManager(dbPath);
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
                    //string fileName = System.IO.Path.Combine(bookPath, "PDFFactory/PDF.pdf");
                    //string PDFFactoryDirectoryName = Path.GetDirectoryName(fileName);
                    //string FinalFilePath = System.IO.Path.Combine(bookPath, "PDF.pdf");
                    try
                    {
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

                        string pdfPath = "";
                        if (isHtml == false)
                            pdfPath = Path.Combine(bookPath, "hyweb");
                        else
                            pdfPath = Path.Combine(bookPath, "data");
                        string thumbsPath = "";
                        //string thumbsPath_Msize = Path.Combine(bookPath, "hyweb", "mthumbs");
                        //string thumbsPath_Lsize = Path.Combine(bookPath, "hyweb", "mthumbs\\Larger");
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
                                string thumb;
                                string extension = ".jpg";
                                if (isHtml == true)
                                {
                                    extension = ".png";
                                }
                                if (isHtml == false)
                                    thumb = Path.Combine(thumbsPath, pdfPrefix + "_" + count + extension);
                                else
                                    thumb = Path.Combine(thumbsPath, "Slide" + count + extension);
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
                                    if (File.Exists(thumb) == true)
                                    {
                                        File.Copy(thumb, imgPath, true);
                                    }
                                    else
                                    {
                                        //thumb = Path.Combine(thumbsPath_Msize, pdfPrefix + "_" + count + ".jpg");
                                        thumb = Path.Combine(thumbsPath, pdfPrefix + "_" + count + extension);
                                        if (File.Exists(thumb) == true)
                                        {
                                            File.Copy(thumb, imgPath, true);
                                        }
                                    }
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
                                                           , bookId, (i - 1).ToString(), UserAccount);

                                    QueryResult rs = bookManager.sqlCommandQuery(cmd);
                                    float xWidth = 0;
                                    float xHeight = 0;
                                    if (rs != null && rs.fetchRow())
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
                                    img.SetDpi(300, 300);
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
                                                            , bookId
                                                            , (i - 1).ToString()
                                                            , UserAccount);
                                    rs = bookManager.sqlCommandQuery(cmd);

                                    if (rs != null && rs.fetchRow())
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
                                                           , bookId
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



                        }
                        catch (Exception ex)
                        {
                            LogTool.Debug(ex);
                        }
                    }

                    if (File.Exists(fileName) == true)
                    {
                        File.Copy(fileName, FinalFilePath, true);
                    }
                    //Singleton_PDFFactory.RemoveBookInPDFWork(bookId);

                    counter++;
                    if (counter <= 3)
                    {
                        Singleton_PDFFactory.SavePDF(isHtml, bookPath, totalPage, width, height, UserAccount, bookId, thumbsPath_Msize, thumbsPath_Lsize, dbPath, counter);
                    }
                    else
                    {
                        if (File.Exists(fileName) == true)
                        {
                            File.Copy(fileName, FinalFilePath, true);
                        }
                        Singleton_PDFFactory.RemoveBookInPDFWork(bookId);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    LogTool.Debug(ex);
                    if (counter > 3)
                    {
                        AutoClosingMessageBox.Show("轉檔失敗，請稍後再試");
                    }
                    //Singleton_PDFFactory.RemoveBookInPDFWork(bookId);
                }



            });

            //th.IsBackground = false;
            //th.Start();
        }
    }

    public class PDFFactoryParameter
    {
        public bool isHtml { get; set; }
        public string bookPath { get; set; }
        public int totalPage { get; set; }
        public float width { get; set; }
        public float height { get; set; }
        public string UserAccount { get; set; }
        public string bookId { get; set; }
        public string dbPath { get; set; }
        public string thumbsPath_Msize { get; set; }
        public string thumbsPath_Lsize { get; set; }
        public int counter { get; set; }
    }
}
