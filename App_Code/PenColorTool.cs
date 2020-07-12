using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;


public class PenColorTool
{
    //new BitmapImage(new Uri(ButtonTool.GetButtonImage(item.ID, IsActived), UriKind.Relative));
    public static BitmapImage GetButtonImage(PenColorType PTC)
    {
        string imgSource = "";
        switch (PTC)
        {
            case PenColorType.紅Thin:
                imgSource = "images/markersNow-red-thin@2x.png";
                break;
            case PenColorType.紅Medium:
                imgSource = "images/markersNow-red-medium@2x.png";
                break;
            case PenColorType.紅Bold:
                imgSource = "images/markersNow-red-bold@2x.png";
                break;
            case PenColorType.紅透Thin:
                imgSource = "images/markersNow-red50-thin@2x.png";
                break;
            case PenColorType.紅透Medium:
                imgSource = "images/markersNow-red50-medium@2x.png";
                break;
            case PenColorType.紅透Bold:
                imgSource = "images/markersNow-red50-bold@2x.png";
                break;

            case PenColorType.黃Thin:
                imgSource = "images/markersNow-yellow-thin@2x.png";
                break;
            case PenColorType.黃Medium:
                imgSource = "images/markersNow-yellow-medium@2x.png";
                break;
            case PenColorType.黃Bold:
                imgSource = "images/markersNow-yellow-bold@2x.png";
                break;
            case PenColorType.黃透Thin:
                imgSource = "images/markersNow-yellow50-thin@2x.png";
                break;
            case PenColorType.黃透Medium:
                imgSource = "images/markersNow-yellow50-medium@2x.png";
                break;
            case PenColorType.黃透Bold:
                imgSource = "images/markersNow-yellow50-bold@2x.png";
                break;

            case PenColorType.綠Thin:
                imgSource = "images/markersNow-green-thin@2x.png";
                break;
            case PenColorType.綠Medium:
                imgSource = "images/markersNow-green-medium@2x.png";
                break;
            case PenColorType.綠Bold:
                imgSource = "images/markersNow-green-bold@2x.png";
                break;
            case PenColorType.綠透Thin:
                imgSource = "images/markersNow-green50-thin@2x.png";
                break;
            case PenColorType.綠透Medium:
                imgSource = "images/markersNow-green50-medium@2x.png";
                break;
            case PenColorType.綠透Bold:
                imgSource = "images/markersNow-green50-bold@2x.png";
                break;

            case PenColorType.藍Thin:
                imgSource = "images/markersNow-blue-thin@2x.png";
                break;
            case PenColorType.藍Medium:
                imgSource = "images/markersNow-blue-medium@2x.png";
                break;
            case PenColorType.藍Bold:
                imgSource = "images/markersNow-blue-bold@2x.png";
                break;
            case PenColorType.藍透Thin:
                imgSource = "images/markersNow-blue50-thin@2x.png";
                break;
            case PenColorType.藍透Medium:
                imgSource = "images/markersNow-blue50-medium@2x.png";
                break;
            case PenColorType.藍透Bold:
                imgSource = "images/markersNow-blue50-bold@2x.png";
                break;

            case PenColorType.紫Thin:
                imgSource = "images/markersNow-purple-thin@2x.png";
                break;
            case PenColorType.紫Medium:
                imgSource = "images/markersNow-purple-medium@2x.png";
                break;
            case PenColorType.紫Bold:
                imgSource = "images/markersNow-purple-bold@2x.png";
                break;
            case PenColorType.紫透Thin:
                imgSource = "images/markersNow-purple50-thin@2x.png";
                break;
            case PenColorType.紫透Medium:
                imgSource = "images/markersNow-purple50-medium@2x.png";
                break;
            case PenColorType.紫透Bold:
                imgSource = "images/markersNow-purple50-bold@2x.png";
                break;
                
        }

        return new BitmapImage(new Uri(imgSource, UriKind.Relative)); 
    }

    
}

