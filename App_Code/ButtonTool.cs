using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;


public class ButtonTool
{
    //new BitmapImage(new Uri(ButtonTool.GetButtonImage(item.ID, IsActived), UriKind.Relative));
    public static BitmapImage GetButtonImage(string ButtonID, bool IsActived = false)
    {
        string imgSource = "";
        switch (ButtonID)
        {
            case "BtnHome":
                imgSource = "images/tabBarIcon_home@2x.png";
                break;
            case "BtnSeries":
                imgSource="images/tabBarIcon_meetingSet@2x.png";
                break;
            case "BtnFile":
                imgSource = "images/tabbar_ic_law@2x.png";
                //imgSource = PaperLess_Emeeting.Properties.Settings.Default.LawButton_Image;
                break;
            case "BtnLaw":
                //imgSource = "images/tabBarIcon_law@2x.png";
                imgSource = PaperLess_Emeeting.Properties.Settings.Default.LawButton_Image;
                break;
            case "BtnLogout":
                imgSource = "images/tabBarIcon_logout@2x.png";
                break;
            case "BtnBroadcast":
                imgSource = "images/tabBarIcon_brocast@2x.png";
                break;
            case "BtnSignin":
                imgSource = "images/tabBarIcon_signature@2x.png";
                break;
            case "BtnMeeting":
                imgSource = "images/tabBarIcon_meetingRecord@2x.png";
                break;
            case "BtnIndividualSign":
                imgSource = "images/tabBarIcon_individualSign@2x.png";
                break;
            case "BtnQuit":
                imgSource = "images/tabBarIcon_exit@2x.png";
                break;
            case "BtnSigninList":
                imgSource = "images/tabBarIcon_signCheck@2x.png";
                break;
            case "BtnVote":
                imgSource = "images/tabBarIcon_vote@2x.png";
                break;
            case "BtnSync":
                imgSource = "images/status-onair-off@2x.png";
                break;
            case "BtnAttendance":
                imgSource = "images/tabBarIcon_signCheck@2x.png";
                break;
            case "BtnExportPDF":
                imgSource = "images/tabBarIcon_pdf@2x.png";
                break;
            case "BtnFolder":
                imgSource = "image/tabBarIcon_inCloud@2x.png";
                break;

        }

        if (IsActived == true)
        {
            if (ButtonID.Equals("BtnSync") == true)
                imgSource = "images/status-onair-audience@2x.png";
            else
                imgSource = imgSource.Replace("@2x.png", "_actived@2x.png");
        }

        return new BitmapImage(new Uri(imgSource, UriKind.Relative)); 
    }

    public static BitmapImage GetSyncButtonImage(bool IsInSync, bool IsSyncOwner)
    {
         string imgSource="";
         if (IsInSync == true) //同步中
         {
              if (IsSyncOwner == true)//同步中是主控，顯示主控圖片
              {
                  imgSource = "images/status-onair-chairman@2x.png";  
              }
              else//同步中是聽眾，顯示聽眾圖片
              {
                     imgSource="images/status-onair-audience@2x.png";  
              }
         }
         else //不是同步中，顯示OFF
         {
             imgSource = "images/status-onair-off@2x.png";  
         }              

         return new BitmapImage(new Uri(imgSource, UriKind.Relative)); 
    }

    //public static string GetButtonActivedImage(string ButtonID)
    //{
    //    string rtn = "";
    //    switch (ButtonID)
    //    {
    //        case "BtnHome":
    //            rtn = "images/tabBarIcon_home_actived@2x.png";
    //            break;
    //        case "BtnSeries":
    //            rtn = "images/tabBarIcon_meetingSet_actived@2x.png";
    //            break;
    //        case "BtnLaw":
    //            rtn = "images/tabBarIcon_law_actived@2x.png";
    //            break;
    //        case "BtnLogout":
    //            rtn = "images/tabBarIcon_exit_actived@2x.png";
    //            break;
    //        case "BtnBroadcast":
    //            rtn = "images/tabBarIcon_brocast_actived@2x.png";
    //            break;
    //        case "BtnSignin":
    //            rtn = "images/tabBarIcon_signature_actived@2x.png";
    //            break;
    //        case "BtnMeeting":
    //            rtn = "images/tabBarIcon_meetingRecord_actived@2x.png";
    //            break;
    //        case "BtnQuit":
    //            rtn = "images/tabBarIcon_exit_actived@2x.png";
    //            break;
    //        case "BtnSigninList":
    //            rtn = "images/tabBarIcon_signCheck_actived@2x.png";
    //            break;
    //        case "BtnVote":
    //            rtn = "images/tabBarIcon_vote_actived@2x.png";
    //            break;
    //    }
    //    return rtn;
    //}
    
}

