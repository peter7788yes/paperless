using PaperLess_Emeeting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Windows;
using System.Windows.Data;

namespace ReadPageModule
{
    public class CulturesHelper
    {
        private static bool _isFoundInstalledCultures = false;

        private static List<CultureInfo> _supportedCultures = new List<CultureInfo>();

        public static ObjectDataProvider _objectDataProvider;

        public static CultureInfo _designTimeCulture = new CultureInfo("zh-TW");

        //public ResourceManager RM;

        public static List<CultureInfo> SupportedCultures
        {
            get
            {
                return _supportedCultures;
            }
        }

        //public string getRmString(string sourceName)
        //{            
        //    return RM.GetString(sourceName);
        //}


        public CulturesHelper()
        {
            if( !_isFoundInstalledCultures )
            {

                CultureInfo cultureInfo = new CultureInfo( "" );

                foreach( string dir in Directory.GetDirectories( System.Windows.Forms.Application.StartupPath ) )
                {
                    try
                    {

                        DirectoryInfo dirinfo = new DirectoryInfo( dir );
                        cultureInfo = CultureInfo.GetCultureInfo( dirinfo.Name );

                        if( dirinfo.GetFiles( Path.GetFileNameWithoutExtension( System.Windows.Forms.Application.ExecutablePath ) + ".resources.dll" ).Length > 0 )
                        {
                            _supportedCultures.Add( cultureInfo );
                        }
                    }
                    catch( ArgumentException )
                    {
                    }
                }

                if( DesignerProperties.GetIsInDesignMode( new DependencyObject() ) )
                {
                    PaperLess_Emeeting.Properties.Resources.Culture = _designTimeCulture;
                    //PaperLess_Emeeting.Properties.Settings.Default.DefaultCulture = _designTimeCulture;
                }
                else if (_supportedCultures.Count > 0 && PaperLess_Emeeting.Properties.Settings.Default.DefaultCulture != null)
                {
                    PaperLess_Emeeting.Properties.Resources.Culture = PaperLess_Emeeting.Properties.Settings.Default.DefaultCulture;
                }

                _isFoundInstalledCultures = true;
            }

            //RM = new ResourceManager("Wpf_ObjectDataSource.Resource", Assembly.GetExecutingAssembly());
        }

        public PaperLess_Emeeting.Properties.Resources GetResourceInstance()
        {
            return new PaperLess_Emeeting.Properties.Resources();
        }

        public PaperLess_Emeeting.Properties.Resources GetResourceInstance( string cultureName )
        {
            ChangeCulture( new CultureInfo( cultureName ) );

            return new PaperLess_Emeeting.Properties.Resources();
        }


        public static ObjectDataProvider ResourceProvider
        {
            get
            {
                if( _objectDataProvider == null )
                {
                    _objectDataProvider = ( ObjectDataProvider ) App.Current.FindResource( "Resources" );
                }
                return _objectDataProvider;
            }
        }

        public static void ChangeCulture( CultureInfo culture )
        {
            if( _supportedCultures.Contains( culture ) )
            {
                PaperLess_Emeeting.Properties.Resources.Culture = culture;
                //PaperLess_Emeeting.Properties.Settings.Default.DefaultCulture = culture;
                PaperLess_Emeeting.Properties.Settings.Default.Save();

                ResourceProvider.Refresh();
            }
        }

    }
}
