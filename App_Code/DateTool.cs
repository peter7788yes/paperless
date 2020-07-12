using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


public class DateTool
{
    public static bool Is366DaysYear(int year)
    {
        return DateTime.IsLeapYear(year);
    }

    public static bool Is366DaysYear(DateTime date)
    {
        return DateTime.IsLeapYear(int.Parse(date.ToString("yyyy")));
    }

    public static int HowMuchDays(int year,int month)
    {
        return DateTime.DaysInMonth(year, month);
    }

    public static int HowMuchDays(int year)
    {
        int Rtn=365;
        if (Is366DaysYear(year) == true)
            Rtn = 366;
        return Rtn;
    }
    
    public static DateTime MonthFirstDate(DateTime date)
    {
        return new DateTime(date.Year, date.Month, 1, 0, 0, 0);
    }

    public static DateTime MonthFirstDate(int year, int month)
    {
        return new DateTime(year, month, 1,0,0,0);
    }

    public static DateTime MonthLastDate(DateTime date)
    {
        return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month),23,59,59);
    }

    public static DateTime MonthLastDate(int year, int month)
    {
        return new DateTime(year, month, DateTime.DaysInMonth(year, month));
    }

    public static bool IsSameDate(DateTime date1, DateTime date2)
    {
        return date1.Date == date2.Date;
    }

    public static string DayOfWeek(DateTime date)
    {
        //DateTime.Now.DayOfWeek傳回的是DayOfWeek的列舉,列舉的原型是byte
        //DayOfWeek.Sunday =0
        //DayNames 是 string[] {"星期日","星期一"}
        //System.Globalization.DateTimeFormatInfo.GetInstance(new System.Globalization.CultureInfo("ja-JP")).DayNames[(byte)DateTime.Now.DayOfWeek]
        return System.Globalization.DateTimeFormatInfo.CurrentInfo.DayNames[(byte)date.DayOfWeek];
    }

    public static int DayOfYear(DateTime date)
    {
        return date.DayOfYear;
    }

    //接受3種格式
    //DateTool.StringToDate("2013-06-15 12:15:16")
    //DateTool.StringToDate("2013/06/15 12:15:16")
    //DateTool.StringToDate("2013.06.15 12:15:16")
    public static DateTime StringToDate(string dateString)
    {
        DateTime date = DateTime.Now;
        DateTime.TryParse(dateString, out date);

        return date;
    }


    public static bool Between(DateTime input, DateTime date1, DateTime date2)
    {
        return (input > date1 && input < date2);
    }

    public static bool CheckInTime(DateTime date1, DateTime date2)
    {
        return IsSameDate(date1, DateTime.Now) && Between(DateTime.Now, date1, date2);
    }

    public static long GetCurrentTimeInUnixMillis()
    {
      // 轉為正常毫秒計算的 Epoch
      return Decimal.ToInt64(Decimal.Divide(DateTime.Now.Ticks - new DateTime(1970, 1, 1, 0, 0, 0).Ticks, 10000));
    }

    public static string BirthTransAtom(DateTime birthday)
    {
        float birthdayF = 0.00F;
        //個位數時自動補零
        string zero = birthday.Day < 10 ? "0" : "";

        birthdayF = birthday.Month == 1 && birthday.Day < 20 ?
                    float.Parse(string.Format("13." + zero + "{0}", birthday.Day)) :
                    float.Parse(string.Format("{0}." + zero + "{1}", birthday.Month, birthday.Day));

        float[] atomBound = { 1.20F, 2.20F, 3.21F, 4.21F, 5.21F, 6.22F, 7.23F, 8.23F, 9.23F, 10.23F, 11.21F, 12.22F, 13.20F };
        string[] atoms = { "水瓶座", "雙魚座", "白羊座", "金牛座", "雙子座", "巨蟹座", "獅子座", "處女座", "天秤座", "天蠍座", "射手座", "魔羯座" };

        //透過時間範圍區間取得生日index
        int keyIndex = Array.FindIndex(atomBound, w =>
                                                        w <= birthdayF &&
                                                        w + 1 > birthdayF);
        string ret = atoms[keyIndex];
        return ret;
    }
}

