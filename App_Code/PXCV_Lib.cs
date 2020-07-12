using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace PXCView36
{
    public class PXCV_Lib36
    {
        public delegate bool PXV36_CALLBACK_FUNC(int stage, int level, int param);

        public enum PXV36_CallbackStage
        {
            PXCVClb_Start = 1,
            PXCVClb_Processing,
            PXCVClb_Finish,
        }

        public enum PXCV_RenderMode
        {
            pxvrm_Viewing = 0,
            pxvrm_Printing,
            pxvrm_Exporting,

        }

        public enum PXCV_ReleaseCachedDataFlags
        {
            pxvrcd_ReleaseDocumentImages = 0x0001,
            pxvrcd_ReleaseDocumentFonts = 0x0002,
            pxvrcd_ReleaseGlobalFonts = 0x0004,
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PXV_CommonRenderParameters
        {
            public IntPtr WholePageRect;
            public IntPtr DrawRect;
            public Int32 Flags;
            public PXCV_RenderMode RenderTarget;
        };

        public enum PXV_CommonRenderParametersFlags
        {
	        pxvrpf_None						=	0x0000,
	        pxvrpf_Rotate_NoRotate			=	0x0000,
	        pxvrpf_Rotate_Rotate90CW		=	0x0001,
	        pxvrpf_Rotate_Rotate180			=	0x0002,
	        pxvrpf_Rotate_Rotate90CCW		=	0x0003,
	        pxvrpf_Rotate_RotateMask		=	0x0003,
	        pxvrpf_UseVectorRenderer		=	0x0004,
        };

        [DllImport("pxcview", EntryPoint = "PXCV_Init")]
        public static extern int PXCV_Init(out IntPtr hDoc, string Key, string DevCode);
        // Deinitialize PDF Object
        [DllImport("pxcview")]
        public static extern int PXCV_Delete(IntPtr hDoc);
        // Set callback function
        [DllImport("pxcview")]
        public static extern int PXCV_SetCallBack(IntPtr hDoc, PXV36_CALLBACK_FUNC pProc, Int32 UserData);
        // Read document
        [DllImport("pxcview")]
        public static extern int PXCV_ReadDocumentW(IntPtr hDoc, [MarshalAs(UnmanagedType.LPWStr)]string pwFileName, Int32 Flags);
        // Check the password for encrypted document
        [DllImport("pxcview")]
        public static extern int PXCV_CheckPassword(IntPtr hDoc, byte[] pPassword, Int32 PassLen);
        // Continue reading document after checking password
        [DllImport("pxcview")]
        public static extern int PXCV_FinishReadDocument(IntPtr hDoc, Int32 Flags);

        [DllImport("pxcview")]
        public static extern int PXCV_GetPagesCount(IntPtr hDoc, out int count);

        [DllImport("pxcview")]
        public static extern int PXCV_GetPageDimensions(IntPtr hDoc, Int32 page_num, out double width, out double height);

        [DllImport("pxcview")]
        public static extern int PXCV_GetPageRotation(IntPtr hDoc, Int32 page_num, ref int angle);

        [DllImport("pxcview")]
        public static extern int PXCV_DrawPageToDC(IntPtr hDoc, Int32 page_num, IntPtr hDC, ref PXV_CommonRenderParameters pParams);

        [DllImport("pxcview")]
        public static extern int PXCV_DrawPageToDIBSection(IntPtr hDoc, Int32 page_num, ref PXV_CommonRenderParameters pParams, IntPtr hBaseDC, Int32 backcolor, ref IntPtr hBitmap, IntPtr hSection, Int32 dwOffset);

        [DllImport("pxcview")]
        public static extern int PXCV_ReadDocumentFromIStream(IntPtr hDoc, IStream aIStream, Int32 dwFlags);

        [DllImport("pxcview")]
        public static extern int PXCV_ReadDocumentFromMemory(IntPtr hDoc, byte[] pMemory, UInt32 size, Int32 dwFlags);

        [DllImport("pxcview")]
        public static extern int PXCV_ReleasePageCachedData(IntPtr hDoc, Int32 page_num, Int32 dwFlags);

        [DllImport("pxcview")]
        public static extern int PXCV_ReleaseCachedData(IntPtr hDoc, Int32 dwFlags);

        //-- Error descriptions API
        [DllImport("pxcview")]
        public static extern int PXCV_Err_FormatSeverity(int errorcode, byte[] buf, Int32 maxlen);
        [DllImport("pxcview")]
        public static extern int PXCV_Err_FormatFacility(int errorcode, byte[] buf, Int32 maxlen);
        [DllImport("pxcview")]
        public static extern int PXCV_Err_FormatErrorCode(int errorcode, byte[] buf, Int32 maxlen);
    };

    public class PDFDoc : IDisposable
    {
        [DllImport("gdi32")]
        public static extern int DeleteObject(IntPtr hObject);

        private IntPtr m_Doc = IntPtr.Zero;
        private bool m_bDisposed;

        public PDFDoc()
        {
            m_bDisposed = true;
        }

        ~PDFDoc()
        {
            Dispose(false);
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool bDisposing)
        {
            if (bDisposing)
            {
            }
            Delete();
        }

        public bool IsInitialized
        {
            get
            {
                return !m_bDisposed;
            }
        }

        public void Init(string aKey, string aDevCode)
        {
            if (!m_bDisposed)
                throw new ApplicationException("PDFLib.Init");
            if (PXCV_Error.IS_DS_FAILED(PXCV_Lib36.PXCV_Init(out m_Doc, aKey, aDevCode)))
                throw new ApplicationException("PDFDoc.Init.PXCV_Init");
            m_bDisposed = false;
        }

        public void Delete()
        {
            if (!m_bDisposed)
            {
                if (PXCV_Error.IS_DS_FAILED(PXCV_Lib36.PXCV_Delete(m_Doc)))
                    throw new ApplicationException("PDFDoc.Delete.PXCV_Delete");
                m_bDisposed = true;
            }
        }

        public void Open(string aFileName)
        {
            if (m_bDisposed)
                throw new ApplicationException("PDFDoc.OpenFromIStream");

            if (PXCV_Error.IS_DS_FAILED(PXCV_Lib36.PXCV_ReadDocumentW(m_Doc, aFileName, 0)))
                throw new ApplicationException("PDFDoc.Open.PXCV_ReadDocumentW");
        }

        public void OpenFromIStream(IStream aIStream, Int32 flags)
        {
            if (m_bDisposed)
                throw new ApplicationException("PDFDoc.OpenFromIStream");
            if (PXCV_Error.IS_DS_FAILED(PXCV_Lib36.PXCV_ReadDocumentFromIStream(m_Doc, aIStream, flags)))
                throw new ApplicationException("PDFDoc.OpenFromPXCV_ReadDocumentFromIStream");
        }

        public void OpenFromMemory(byte[] pMemory, UInt32 size, Int32 flags)
        {
            if (m_bDisposed)
                throw new ApplicationException("PDFDoc.OpenFromMemory");
            if (PXCV_Error.IS_DS_FAILED(PXCV_Lib36.PXCV_ReadDocumentFromMemory(m_Doc, pMemory, size, flags)))
                throw new ApplicationException("PDFDoc.OpenFromPXCV_ReadDocumentFromMemory");
        }

        public void GetPageDimensions(int pn, out double w, out double h)
        {
            if (m_bDisposed)
                throw new ApplicationException("PDFDoc.GetPageDimensions");
            if (PXCV_Error.IS_DS_FAILED(PXCV_Lib36.PXCV_GetPageDimensions(m_Doc, pn, out w, out h)))
                throw new ApplicationException("PDFDoc.OpenFromPXCV_GetPageDimensions");
        }

        public int GetPagesCount()
        {
            int cnt = 0;

            if (m_bDisposed)
                throw new ApplicationException("PDFDoc.GetPagesCount");
            if (PXCV_Error.IS_DS_FAILED(PXCV_Lib36.PXCV_GetPagesCount(m_Doc, out cnt)))
                throw new ApplicationException("PDFDoc.OpenFromPXCV_GetPageDimensions");

            return cnt;
        }

        public void ReleasePageCachedData(int pn, int flags)
        {
            if (m_bDisposed)
                throw new ApplicationException("PDFDoc.DrawPageToDIBSection");

            if (PXCV_Error.IS_DS_FAILED(PXCV_Lib36.PXCV_ReleasePageCachedData(m_Doc, pn, flags)))
                throw new ApplicationException("PDFDoc.ReleasePageCachedData.PXCV_ReleasePageCachedData");
        }

        public void ReleaseCachedData(int flags)
        {
            if (m_bDisposed)
                throw new ApplicationException("PDFDoc.ReleaseCachedData");

            if (PXCV_Error.IS_DS_FAILED(PXCV_Lib36.PXCV_ReleaseCachedData(m_Doc, flags)))
                throw new ApplicationException("PDFDoc.ReleaseCachedData.PXCV_ReleaseCachedData");
        }

        public void DrawPageToDIBSection(IntPtr hBaseDC, int pn, Color aBkgColor, PXCV_Lib36.PXV_CommonRenderParameters aCommonRenderParams, out Bitmap aBitmap)
        {
            aBitmap = null;

            if (m_bDisposed)
                throw new ApplicationException("PDFDoc.DrawPageToDIBSection");

            IntPtr hBitmap = IntPtr.Zero;
            Int32 clr = aBkgColor.ToArgb();

            if (PXCV_Error.IS_DS_FAILED(PXCV_Lib36.PXCV_DrawPageToDIBSection(m_Doc, pn, ref aCommonRenderParams, hBaseDC, clr, ref hBitmap, IntPtr.Zero, 0)))
                throw new ApplicationException("PDFDoc.DrawPageToDIBSection.PXCV_DrawPageToDIBSection");
            aBitmap = Image.FromHbitmap(hBitmap);
            DeleteObject(hBitmap);
        }

        public void DrawPageToDC(IntPtr hDC, int pn, PXCV_Lib36.PXV_CommonRenderParameters aCommonRenderParams)
        {
            if (m_bDisposed)
                throw new ApplicationException("PDFDoc.DrawPage");

            if (PXCV_Error.IS_DS_FAILED(PXCV_Lib36.PXCV_DrawPageToDC(m_Doc, pn, hDC, ref aCommonRenderParams)))
                throw new ApplicationException("PDFDoc.DrawPageToDC.PXCV_DrawPageToDC");
        }
    }
}
