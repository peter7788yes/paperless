using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Media;
using BookManagerModule;
using DataAccessObject;
using System.Windows.Ink;

namespace PaperLess_Emeeting
{
    public class ConfigurationManager
    {
        private bool _saveLoginInfo = false;
        private int _saveFullTextSize = 0;
        private string _saveFullTextColor ="FFFFFF";
        private bool _saveShowButton = true;
        private int _saveSlideShowTime = 3;
        private bool _lastReadFirst = true;
        private string _savefilterBookStr = "111111111";
        private string _saveEpubTextColor = "FFFFFF";
        private float _saveEpubTextSize = 1.0F;
        private int _saveEpubChineseType = 1;
        private int _saveEpubFontSize = 16;
        private int _saveShowEpubPageArrow = 1;
        private int _savePdfPageMode = 2;
        private int _saveProxyMode = 1;
        private string _saveProxyHttpPort = "";
        private string _saveLanquage = "";

        private string _strokeColor;
        private int _isStrokeLine;
        public bool isStrokeLine;
        private int _isStrokeTransparent;
        private double _strokeradiusWidth;
        private DrawingAttributes _drawingAttr;
        private BookManager bookManager;

        /*
         *  顏色strokeColor(string)
            半徑strokeRadiusWidth(float)
            直線/曲線strokeIsStraight(int)(0:直線.1:曲線)
            是否透明 strokeIsTransparent(int)(0:透明,1:不透明)
         * */
        public DrawingAttributes loadStrokeSetting()
        {
            DrawingAttributes d = new DrawingAttributes();
            string sqlCommand = "Select * from configuration ";
            QueryResult rs = bookManager.sqlCommandQuery(sqlCommand);

            if (rs.fetchRow())
            {
                this._strokeColor = rs.getString("strokeColor");
                this._strokeradiusWidth = rs.getFloat("strokeRadiusWidth");
                this._isStrokeLine = rs.getInt("strokeIsStraight");
                this._isStrokeTransparent = rs.getInt("strokeIsTransparent");

                d.Width = d.Height = this._strokeradiusWidth;
                if(this._isStrokeTransparent==0){
                    d.IsHighlighter = true;
                }
                if(this._isStrokeTransparent==1){
                    d.IsHighlighter = false;
                }
                if(this._isStrokeLine ==0){
                    isStrokeLine = true;
                }
                if(this._isStrokeLine ==1){
                    isStrokeLine = false;
                }

                d.Color = ConvertHexStringToColour(this._strokeColor);
                
            }

            return d;
        }

        public void saveStrokeSetting(DrawingAttributes d, bool isLine)
        {
           this._strokeColor =  d.Color.ToString();
           this._strokeradiusWidth = d.Width;
           if (d.IsHighlighter)
           {
               this._isStrokeTransparent=0;
           }
           else
           {
               this._isStrokeTransparent=1;
           }
           if(isLine){
               //直線
               this._isStrokeLine = 0;
           }else{
               this._isStrokeLine = 1;
           }
            string sqlCommand = "Update configuration "
                + "Set strokeColor='"+ _strokeColor +"' "
                +", strokeRadiusWidth=" +_strokeradiusWidth
                +", strokeIsStraight=" + _isStrokeLine
                +", strokeIsTransparent="+ _isStrokeTransparent;
            bookManager.sqlCommandNonQuery(sqlCommand);
        }

        private Color ConvertHexStringToColour(string hexString)
        {
            byte a = 0;
            byte r = 0;
            byte g = 0;
            byte b = 0;
            if (hexString.StartsWith("#"))
            {
                hexString = hexString.Substring(1, 8);
            }
            a = Convert.ToByte(Int32.Parse(hexString.Substring(0, 2),
                System.Globalization.NumberStyles.AllowHexSpecifier));
            r = Convert.ToByte(Int32.Parse(hexString.Substring(2, 2),
                System.Globalization.NumberStyles.AllowHexSpecifier));
            g = Convert.ToByte(Int32.Parse(hexString.Substring(4, 2),
                System.Globalization.NumberStyles.AllowHexSpecifier));
            b = Convert.ToByte(Int32.Parse(hexString.Substring(6, 2), System.Globalization.NumberStyles.AllowHexSpecifier));
            return Color.FromArgb(a, r, g, b);
        }

        public string saveLanquage
        {
            get { return _saveLanquage; }
            set
            {
                _saveLanquage = value;
                saveConfiguration();
            }
        }

        public string saveProxyHttpPort
        {
            get { return _saveProxyHttpPort; }
            set
            {
                _saveProxyHttpPort = value;
                saveConfiguration();
            }
        }
        public int saveProxyMode
        {
            get { return _saveProxyMode; }
            set
            {
                _saveProxyMode = value;
                saveConfiguration();
            }
        }

        public int savePdfPageMode
        {
            get { return _savePdfPageMode; }
            set
            {
                _savePdfPageMode = value;
                saveConfiguration();
            }
        }

        public int saveShowEpubPageArrow
        {
            get { return _saveShowEpubPageArrow; }
            set
            {
                _saveShowEpubPageArrow = value;
                saveConfiguration();
            }
        }

        public int saveEpubFontSize
        {
            get { return _saveEpubFontSize; }
            set
            {
                _saveEpubFontSize = value;
                saveConfiguration();
            }
        }

        public int saveEpubChineseType
        {
            get { return _saveEpubChineseType; }
            set
            {
                _saveEpubChineseType = value;
                saveConfiguration();
            }
        }

        public string saveEpubTextColor
        {
            get { return _saveEpubTextColor; }
            set
            {
                _saveEpubTextColor = value;
                saveConfiguration();
            }
        }
        public float saveEpubTextSize
        {
            get { return _saveEpubTextSize; }
            set
            {
                _saveEpubTextSize = value;
                saveConfiguration();
            }
        }


        public string savefilterBookStr
        {
            get { return _savefilterBookStr; }
            set
            {
                _savefilterBookStr = value;
                saveConfiguration();
            }
        }
        public bool saveLoginInfo
        {
            get { return _saveLoginInfo; }
            set { 
                _saveLoginInfo = value;
                saveConfiguration();
            }
        }
        public int saveFullTextSize
        {
            get { return _saveFullTextSize; }
            set
            {
                _saveFullTextSize = value;
                saveConfiguration();
            }
        }
        public string saveFullTextColor
        {
            get { return _saveFullTextColor; }
            set
            {
                _saveFullTextColor = value;
                saveConfiguration();
            }
        }

        public bool saveShowButton
        {
            get { return _saveShowButton; }
            set
            {
                _saveShowButton = value;
                saveConfiguration();
            }
        }

        public int saveSlideShowTime
        {
            get { return _saveSlideShowTime; }
            set
            {
                _saveSlideShowTime = value;
                saveConfiguration();
            }
        }

        public bool lastReadFirst
        {
            get { return _lastReadFirst; }
            set
            {
                _lastReadFirst = value;
                saveConfiguration();
            }
        }

        public ConfigurationManager(BookManager bookManager)
        {
            this.bookManager = bookManager;
            loadConfiguration();
        }

        private  void saveConfiguration()
        {
            string sqlCommand = "Update configuration "
                + "Set fullTextColor='" + _saveFullTextColor + "' "
                + ", fullTextSize=" + _saveFullTextSize
                + ", slideShowTime=" + _saveSlideShowTime
                + ", filterBookStr='" + _savefilterBookStr + "' "
                + ", epubTextColor='" + _saveEpubTextColor + "' "
                + ", epubTextSize=" + _saveEpubTextSize + " "
                + ", epubChineseType=" + _saveEpubChineseType + " "
                + ", epubFontSize=" + _saveEpubFontSize + " "
                + ", showEpubPageArrow=" + _saveShowEpubPageArrow + " "
                + ", pdfPageMode=" + _savePdfPageMode + " "
                + ", proxyMode=" + _saveProxyMode + " "
                + ", proxyHttpPort='" + _saveProxyHttpPort + "' "
                +",  lanquage='" + _saveLanquage + "' "; 
            bookManager.sqlCommandNonQuery(sqlCommand);
        }
        
        private  void loadConfiguration()
        {
            string sqlCommand = "Select * from configuration "; 
            QueryResult rs = bookManager.sqlCommandQuery(sqlCommand);

            if (rs.fetchRow())
            {
                this._saveFullTextColor = rs.getString("fullTextColor");
                this._saveFullTextSize = rs.getInt("fullTextSize");
                this._saveSlideShowTime = rs.getInt("slideShowTime");
                this._savefilterBookStr = rs.getString("filterBookStr");
                this._saveEpubTextColor = rs.getString("epubTextColor");
                this._saveEpubTextSize = rs.getFloat("epubTextSize");
                this._saveEpubChineseType = rs.getInt("epubChineseType");
                this._saveEpubFontSize = rs.getInt("epubFontSize");
                this._saveShowEpubPageArrow = rs.getInt("showEpubPageArrow");
                this._savePdfPageMode = rs.getInt("pdfPageMode");
                this._saveProxyMode = rs.getInt("proxyMode");
                this._saveProxyHttpPort = rs.getString("proxyHttpPort");
                this._saveLanquage = rs.getString("lanquage");
            }


            //this._saveLoginInfo = rs.getInt("loginInfo")==1 ? true: false;
            //this._saveShowButton = rs.getInt("showButton") == 1 ? true : false;
        }
    }
}
