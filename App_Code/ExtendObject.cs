﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

public static class ExtendObject
{
    public static Rect GetAbsoltutePlacement(this FrameworkElement element, bool relativeToScreen = false)
    {
        var absolutePos = element.PointToScreen(new System.Windows.Point(0, 0));
        if (relativeToScreen)
        {
            return new Rect(absolutePos.X, absolutePos.Y, element.ActualWidth, element.ActualHeight);
        }
        var posMW = Application.Current.MainWindow.PointToScreen(new System.Windows.Point(0, 0));
        absolutePos = new System.Windows.Point(absolutePos.X - posMW.X, absolutePos.Y - posMW.Y);
        return new Rect(absolutePos.X, absolutePos.Y, element.ActualWidth, element.ActualHeight);
    }
}
