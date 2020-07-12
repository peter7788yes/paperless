using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PaperLess_Emeeting.App_Code.DownloadItem
{
    public class Law_DownloadItemViewModel
    {
        public string ID { get; set; }
        public DateTime UpDate { get; set; }
        public string Name { get; set; }
        public string Link { get; set; }
        public LawDataStatus Status { get; set; }
        public string StorageFileFolder { get; set; }
        public string StorageFileName { get; set; }
        public string StorageFilePath { get { return StorageFileFolder + "\\" + StorageFileName; } private set {} }
        public long DownloadBytes { get; set; }
        public long TotalBytes { get; set; }
        public double LastPercentage { get; set; }
        public MeetingFileCate FileCate = MeetingFileCate.電子書;
        public double NowPercentage
        {
            get
            {
                if (TotalBytes == 0)
                    return 0;
                else
                    return DownloadBytes * 100 / TotalBytes;
            }
            private set { }
        }
        public LawFileType FileType { get; set; }
        public string UserID { get; set; }
        public string UnZipFileFolder { get; set; }
        public string UnZipFilePath { get { return UnZipFileFolder + "\\" + UserID + "\\" + ID; } private set { } }

    }
}
