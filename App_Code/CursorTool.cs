using Microsoft.Win32.SafeHandles;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public class CursorTool
{

    struct IconInfo
    {
        public bool fIcon;
        public int xHotspot;
        public int yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    [DllImport("user32.dll")]
    static extern IntPtr CreateIconIndirect(ref IconInfo piconinfo);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetIconInfo(IntPtr hIcon, ref IconInfo piconinfo);

    public static Cursor CreatCursor(Bitmap b, int x, int y)
    {
        /// get icon from input bitmap
        IconInfo ico = new IconInfo();
        GetIconInfo(b.GetHicon(), ref ico);

        /// set the hotspot
        ico.xHotspot = x;
        ico.yHotspot = y;
        ico.fIcon = false;

        /// create a cursor from iconinfo
        IntPtr cursor = CreateIconIndirect(ref ico);
        return CursorInteropHelper.Create(new SafeFileHandle(cursor, true));
    }

    public static Cursor ConvertToCursor(FrameworkElement visual, System.Windows.Point hotSpot)
    {
        int width = (int)visual.Width;
        int height = (int)visual.Height;

        // Render to a bitmap
        var bitmapSource = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmapSource.Render(visual);

        // Convert to System.Drawing.Bitmap
        var pixels = new int[width * height];
        bitmapSource.CopyPixels(pixels, width, 0);
        var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(pixels[y * width + x]));

        // Save to .ico format
        var stream = new MemoryStream();
        System.Drawing.Icon.FromHandle(bitmap.GetHicon()).Save(stream);

        // Convert saved file into .cur format
        stream.Seek(2, SeekOrigin.Begin);
        stream.WriteByte(2);
        stream.Seek(10, SeekOrigin.Begin);
        stream.WriteByte((byte)(int)(hotSpot.X * width));
        stream.WriteByte((byte)(int)(hotSpot.Y * height));
        stream.Seek(0, SeekOrigin.Begin);

        // Construct Cursor
        return new Cursor(stream);
    }
    public static Cursor CreatCursor(UIElement u,System.Windows.Point p)
    {
        Cursor c;

        /// move to the orignal point of parent
        u.Measure(new System.Windows.Size(double.PositiveInfinity,double.PositiveInfinity));
        u.Arrange(new Rect(0, 0,
                           u.DesiredSize.Width,
                           u.DesiredSize.Height));

        /// render the source to a bitmap image
        RenderTargetBitmap r =
            new RenderTargetBitmap(
                (int)u.DesiredSize.Width,
                (int)u.DesiredSize.Height,
                96, 96, PixelFormats.Pbgra32);
        r.Render(u);

        /// reset back to the orignal position
        u.Measure(new System.Windows.Size(0, 0));

        using (MemoryStream m = new MemoryStream())
        {
            /// use an encoder to transform to Bitmap
            PngBitmapEncoder e = new PngBitmapEncoder();
            e.Frames.Add(BitmapFrame.Create(r));
            e.Save(m);
            System.Drawing.Bitmap b =
                new System.Drawing.Bitmap(m);

            /// create cursor from Bitmap
            c = CreatCursor(b,
                (int)p.X, (int)p.Y);
        }

        return c;
    }
}
