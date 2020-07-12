using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



    //public enum ProjectName
    //{
    //    新北市政府電子化會議系統 = 1,
    //    檔案管理局電子化會議系統 = 2,
    //    會議室無紙化管理系統 = 3
    //}

    //public enum AssemblyName
    //{
    //    PaperLess_Emeeting_NTPC = 1,
    //    PaperLess_Emeeting_Archives = 2,
    //    PaperLess_Emeeting_Edu = 3
    //}


    public enum UserDevice
    {
        PC=1
    }

    public enum DisplayUserNameMode
    {
        None = 1,
        UserName=2,
        UserID_UserName=3
    }

    public enum AgendaFilter
    {
        顯示全部附件 = 1,
        顯示父議題和子議題附件 = 2,
        顯示當前議題附件 = 3
    }

    public enum MeetingUserType
    {
        議事管理人員=1, 
        與會人員=2, 
        代理人=3, 
        其它=4
    }

    public enum LawDataStatus
    {
         無效 = 0,
         有效 = 1
    }

    public enum LawFileType
    {
        從未下載 = 1,
        暫停中 = 2,
        正在下載中 = 3,
        已下載完成 = 4,
        排入下載中 = 5,
        解壓縮中 = 6,
        解壓縮失敗 = 7,
        更新檔未下載 = 8,
        更新檔暫停中 = 9,
        更新檔正在下載中 = 10,
        更新檔已下載完成 = 11,
        更新檔排入下載中 = 12,
        更新檔解壓縮中 = 13,
        更新檔解壓縮失敗 = 14

        
    }


    public enum MeetingFileType
    {
        從未下載 = 1,
        暫停中 = 2,
        正在下載中 = 3,
        已下載完成 = 4,
        排入下載中 = 5,
        已下載過但是未完成的檔案=6,
        已經下載過一次且可以更新版本的檔案_目前下載未完成 = 7,
        已經下載過一次且可以更新版本的檔案_目前下載已完成 = 8,
        解壓縮中 = 9,
        解壓縮失敗 = 10
    }

    public enum DownloaderType
    {
        沒有任何檔案下載中 =0,
        //開始 = 1,
        正在下載中 = 2,
        暫停 = 3,
        停止 = 4,
        檔案下載完成 = 5,
        下載出錯 = 6

    }


    public enum MeetingFileCate
    {
        電子書 = 0,
        Html5投影片 = 1,
        影片檔 = 2

    }

    public enum SignListCT_Order
    {
        序號 = 0,
        機關單位 = 1,
        是否簽到 = 2

    }

    // userMeeting.isDownload + userMeeting.isBrowserd
    public enum MeetingRoomButtonType
    {
        NN = 1,
        NY = 2,
        NO = 3,

        YN = 4,
        YY = 5,
        YO = 6,

        ON = 7,
        OY = 8,
        OO = 9
    }

    public enum PDFStatus
    {
        尚未匯出 = 0,
        匯出成功 = 1,
        匯出中 = 2,
        等待中 = 3
    }



    //電子書
    public enum MovePageType
    {
        上一頁 = 1,
        下一頁 = 2,
        第一頁 = 3,
        最後一頁 = 4
    }

    //電子書
    public enum PenColorType
    {
        紅Thin = 101,
        紅Medium = 201,
        紅Bold = 301,

        紅透Thin = 102,
        紅透Medium = 202,
        紅透Bold = 302,

        黃Thin = 103,
        黃Medium = 203,
        黃Bold = 303,

        黃透Thin = 104,
        黃透Medium = 204,
        黃透Bold = 304,

        綠Thin = 105,
        綠Medium = 205,
        綠Bold = 305,

        綠透Thin = 106,
        綠透Medium = 206,
        綠透Bold = 306,

        藍Thin = 107,
        藍Medium = 207,
        藍Bold = 307,

        藍透Thin = 108,
        藍透Medium = 208,
        藍透Bold = 308,

        紫Thin = 109,
        紫Medium = 209,
        紫Bold = 309,

        紫透Thin = 110,
        紫透Medium = 210,
        紫透Bold = 310

    }
   