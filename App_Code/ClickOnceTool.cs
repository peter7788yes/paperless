using PaperLess_Emeeting.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;

namespace PaperLess_Emeeting.App_Code.ClickOnce
{
    public class ClickOnceTool
    {

        public static string GetDataPath()
        {
            string DataPath = "";

            if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)//是否為ClickOnce應用程式
            {
                DataPath = System.Deployment.Application.ApplicationDeployment.CurrentDeployment.DataDirectory;
            }
            else
            {
                DataPath = AppDomain.CurrentDomain.BaseDirectory;
            }

            return DataPath;
        }


        public static string GetFilePath()
        {
            string FilePath = "";

            if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)//是否為ClickOnce應用程式
            {
                // 改成跟DataPath同一目錄
                //FilePath = System.Deployment.Application.ApplicationDeployment.CurrentDeployment.DataDirectory;
                //FilePath = GetAPP_ProgramFilesx86();
                FilePath = GetMyDocument();
                //FilePath = GetUserAppDataPath();
            }
            else
            {
                FilePath = AppDomain.CurrentDomain.BaseDirectory;
            }

            return FilePath;
        }
       


        private static string GetMyDocument()
        { 
            //string MyDocument = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + @"\" + Settings.Default["FilePath"].ToString(); 
            string MyDocument = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + @"\Hyweb\" + Assembly.GetExecutingAssembly().GetName().Name;  //Application.Current.MainWindow.GetType().Assembly.GetName(); 
            if (Directory.Exists(MyDocument) == false)
            {
                Directory.CreateDirectory(MyDocument);
            }
            return MyDocument;
        }

        public static string GetUserAppDataPath()
        {
            string path = string.Empty;
            Assembly assm;
            Type at;
            object[] r;

            // Get the .EXE assembly
            assm = Assembly.GetEntryAssembly();
            // Get a 'Type' of the AssemblyCompanyAttribute
            at = typeof(AssemblyCompanyAttribute);
            // Get a collection of custom attributes from the .EXE assembly
            r = assm.GetCustomAttributes(at, false);
            // Get the Company Attribute
            AssemblyCompanyAttribute ct =((AssemblyCompanyAttribute)(r[0]));
            // Build the User App Data Path
            path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            path += @"\Hyweb\" + Assembly.GetExecutingAssembly().GetName().Name;  
            //path += @"\" + ct.Company;
            //path += @"\" + assm.GetName().Version.ToString();

            return path;
        }

        private static string GetAPP_ProgramFilesx86()
        {
            string App_Path = ProgramFilesx86() + @"\Hyweb\" + Assembly.GetExecutingAssembly().GetName().Name; // Application.Current.MainWindow.GetType().Assembly.GetName(); 
            if (Directory.Exists(App_Path) == false)
            {
                Directory.CreateDirectory(App_Path);
            }

            return App_Path;
        }


        private static string ProgramFilesx86()
        {
            if (8 == IntPtr.Size
                || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }

            return Environment.GetEnvironmentVariable("ProgramFiles");
        }
    }
}
