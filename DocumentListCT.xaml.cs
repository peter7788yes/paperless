using Newtonsoft.Json;
using PaperLess_Emeeting.App_Code.MessageBox;
using PaperLess_ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Security.Permissions;
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
using System.Xml.Linq;

namespace PaperLess_Emeeting
{
    public partial class DocumentListCT : UserControl
    {

        bool IsGrid = false;
        bool A2Z = true;
        ObservableCollection<DocumentVM> OC;
        ObservableCollection<DocumentVM> OCOri;
        List<DocumentVM> list;

        public DocumentListCT()
        {
            InitializeComponent();
            OC = new ObservableCollection<DocumentVM>();
            OC.Add(new DocumentVM() { FileIcon = "image/folder.png", FileName = "A市政會議" });
            OC.Add(new DocumentVM() { FileIcon = "image/folder.png", FileName = "B列管事項" });
            OC.Add(new DocumentVM() { FileIcon = "image/folder.png", FileName = "C列管事項" });
            OC.Add(new DocumentVM() { FileIcon = "image/folder.png", FileName = "D列管事項" });
            OC.Add(new DocumentVM() { FileIcon = "image/folder.png", FileName = "E列管事項" });
            OC.Add(new DocumentVM() { FileIcon = "image/folder.png", FileName = "F列管事項" });
            OC.OrderBy(x => x.FileName);
            OCOri = OC;
            LL.ItemsSource = OC;
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LL.ItemTemplate = IsGrid ? (DataTemplate)this.FindResource("ITP") : (DataTemplate)this.FindResource("ITP2");
            var url = IsGrid ? "gallery" : "grid";
            Image image = (Image)sender;
            image.Source = new BitmapImage(new Uri(@"image/" + url + @".png", UriKind.Relative));
            IsGrid = !IsGrid;

        }

        private void Image_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            LL.ItemsSource = A2Z ? new ObservableCollection<DocumentVM>(OC.OrderByDescending(x => x.FileName)): new ObservableCollection<DocumentVM>(OC.OrderBy(x => x.FileName));
            var url = A2Z ? "A2Z" : "Z2A";
            Image image = (Image)sender;
            image.Source = new BitmapImage(new Uri(@"image/" + url+ @".png",UriKind.Relative));
            A2Z = !A2Z;
        }

        private void txtKeyword_KeyUp(object sender, KeyEventArgs e)
        {
            string keyword = txtKeyword.Text.ToLower().Trim();
            ObservableCollection<DocumentVM> filterOC = new ObservableCollection<DocumentVM>();
            if (keyword.Equals("") == false)
            {
                foreach(var item in OC)
                {
                    var find = item.FileName.ToLower().Contains(keyword);
                    if (find)
                        filterOC.Add(item);
                }

                OC = filterOC;
            }
            else
                OC = OCOri;

            LL.ItemsSource = OC;
        }
    }
}