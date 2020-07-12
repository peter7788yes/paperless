using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PaperLess_Emeeting.App_Code.DownloadItem
{
    public class File_DownloadItemViewModel
    {

        public string MeetingID { get; set; }
        public string ID { get; set; }
        public string FileName { get; set; }
        public string Url { get; set; }
        public string AgendaID { get; set; }
        public int FileVersion { get; set; }
        public int FinishedFileVersion { get; set; }
        public string EncryptionKey { get; set; }
        public string StorageFileFolder { get; set; }
        public string StorageFileName { get; set; }
        public string StorageFilePath { get { return StorageFileFolder + "\\" + StorageFileName; } private set { } }
        public long DownloadBytes { get; set; }
        public long TotalBytes { get; set; }
        public double LastPercentage { get; set; }
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

      
        public bool CanUpdate { get; set; }
        public MeetingFileType FileType { get; set; }
        public string UserID { get; set; }
        public string UnZipFileFolder { get; set; }
        public string UnZipFilePath { get { return UnZipFileFolder + "\\" + UserID + "\\" + MeetingID + "\\" + ID + "\\" + FileVersion; } private set { } }
     
        public MeetingFileCate FileCate 
        {
            get
            {
                string typeChar = "P";

                if( ID !=null )
                {
                    typeChar = ID.Split('-').Last();
                }

                switch (typeChar)
                {
                    case "P":
                        return MeetingFileCate.電子書;
                        break;
                    case "H":
                        return MeetingFileCate.Html5投影片;
                        break;
                    case "V":
                        return MeetingFileCate.影片檔;
                        break;
                    default:
                        return MeetingFileCate.電子書;
                        break;
                }
            }
            private set { }
        }

        public File_DownloadItemViewModel()
        {
            FileVersion = 1;
        }

    }
}
