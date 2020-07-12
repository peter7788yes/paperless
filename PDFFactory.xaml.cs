using PaperLess_Emeeting.App_Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PaperLess_Emeeting
{
    /// <summary>
    /// PDFFactory.xaml 的互動邏輯
    /// </summary>
    public partial class PDFFactory : Window
    {
        public PDFFactory(string myBookPath, int totalPage, float penMemoCanvasWidth, float penMemoCanvasHeight, string account, string bookId, string dbPath)
        {
            InitializeComponent();
            //Singleton_PDFFactory.SavePDF(myBookPath, totalPage, penMemoCanvasWidth, penMemoCanvasHeight, account, bookId, dbPath);
            this.Close();
        }

    }
}
