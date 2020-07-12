using BookManagerModule;
using NLog;
using PaperLess_Emeeting.App_Code.ClickOnce;
using PaperLess_Emeeting.App_Code.Socket;
using PaperLess_Emeeting.App_Code.Tools;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace PaperLess_Emeeting
{

    internal delegate void Invoker();
    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>
    public partial class App : Application
    {
        public static Logger logger = LogManager.GetCurrentClassLogger();

        public static bool IsChangeWindow = false;
        public App()
        {
            this.ShutdownMode = ShutdownMode.OnLastWindowClose;
            Singleton_Socket.Init();

            //在初始化方法設置以下相關屬性.
            //對象所允許的最大並發連接數//可在配置文件中設置
            //*System.Net.ServicePointManager.DefaultConnectionLimit = 1000;
            //是否使用 Nagle 不使用 提高效率
            //*System.Net.ServicePointManager.UseNagleAlgorithm = false;
            //Nagle 演算法能藉由緩衝處理小型的資料封包，再以單一封包傳送多個小型資料封包的方式，用以降低網路的資訊流量。 這種處理方式稱為 "Nagling"。由於這種技術可以減低資料封包傳送的數目，進而降低單位封包處理的固定資源損耗，因此廣為業界所使用。
            //變更這個屬性值並不會影響現有的 ServicePoint 物件， 只有在變更設定之後所建立的新服務點才會有影響。
            //《IETF RFC 896》對 Nagle 演算法有完整的說明。
            //對象的最大空閒時間.(默認為100秒的)
            //*System.Net.ServicePointManager.MaxServicePointIdleTime = 3600 * 1000;
            

            // 取得或設定 ServicePoint 物件所允許的同時連線最大數。
            // ServicePoint 物件所允許的同時連線最大數。 預設值為 2。
            // 設定為1024;
            System.Net.ServicePointManager.DefaultConnectionLimit = PaperLess_Emeeting.Properties.Settings.Default.ServicePoint_DefaultConnectionLimit;
            System.Net.ServicePointManager.UseNagleAlgorithm = false;
            System.Net.ServicePointManager.Expect100Continue = true;
            //ServicePoint 物件的最大閒置時間，以毫秒為單位。 預設值為 100,000 毫秒 (100 秒)。 
            System.Net.ServicePointManager.MaxServicePointIdleTime = 3600 * 1000;
            //逾時值 (以毫秒為單位)。 -1 值表示無限逾時週期。 預設值為 120,000 毫秒 (兩分鐘)。 
            System.Net.ServicePointManager.DnsRefreshTimeout = 4 * 60 * 1000; // 4 minutes
            CheckDBVersion();

            StartMenuShortCutTool.DeleteDirectory("hyweb");
            StartMenuShortCutTool.DeleteDirectory("凌網科技股份有限公司");

            var p = System.Diagnostics.Process.GetCurrentProcess();
            p.PriorityClass = System.Diagnostics.ProcessPriorityClass.AboveNormal;
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            CopyLog();
            //關閉應用程式
            Application.Current.Shutdown();
            //關閉處理序
            Environment.Exit(0);
        }

        public static void CopyLog()
        {
            if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
            {
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        string AppDir = AppDomain.CurrentDomain.BaseDirectory;
                        string dir = Directory.GetDirectories(Path.Combine(AppDir, "Logs")).OrderByDescending(f => f).First();
                        DirectoryTool.FullCopyDirectories(dir, ClickOnceTool.GetFilePath());
                    }
                    catch (Exception ex)
                    {
                        LogTool.Debug(ex);
                    }

                });
            }
        }


        private void CheckDBVersion()
        {
            try
            {
                // DB最小版本為0;
                int InitDBVersion = 0;
                int LocalDBVersion = 0;
                int ToDBVersion = PaperLess_Emeeting.Properties.Settings.Default.NowDBVersion;
                string DB_FilePath = Path.Combine(ClickOnceTool.GetDataPath(), PaperLess_Emeeting.Properties.Settings.Default.PaperLessDB_Path);
                string ConfigIni_FilePath =  Path.Combine(ClickOnceTool.GetDataPath(), PaperLess_Emeeting.Properties.Settings.Default.ConfigIni_Path);

                if (File.Exists(ConfigIni_FilePath) == true)
                {
                    IniFileTool ini = new IniFileTool(ConfigIni_FilePath);
                    try
                    {
                        int.TryParse(ini.IniReadValue("DB", "Version"), out LocalDBVersion);
                    }
                    catch (Exception ex)
                    {
                        LogTool.Debug(ex);
                    }
                    bool Success = UpdateDBVersion(LocalDBVersion);
                    if (Success == true)
                    {
                       ini.IniWriteValue("DB", "Version", ToDBVersion.ToString());
                    }
                    else
                    {
                        throw new Exception("DB更新失敗!");
                    }

                }
                else
                {
                    //在嘗試更新一次，避免DB沒有更新到
                    UpdateDBVersion(LocalDBVersion);

                    using (var writer = new FileStream(ConfigIni_FilePath, FileMode.OpenOrCreate))
                    {
                        
                    }
                    IniFileTool ini = new IniFileTool(ConfigIni_FilePath);
                    ini.IniWriteValue("DB", "Version", ToDBVersion.ToString());
                
                }
            }
            catch(Exception ex)
            {
                LogTool.Debug(ex);
            }
          
        }

        private bool UpdateDBVersion(int LocalDBVersion)
        {
            TableAction(@"alter table nowlogin add  MeetingUserType nvarchar(100),MeetingBeginTime datetime,MeetingEndTime datetime");
            TableAction(@"alter table filerow add EncryptionKey nvarchar(100) ");
            TableAction(@"CREATE TABLE [LoginInfo] ([UserID] nvarchar(100), [UserPWD] nvarchar(100),UserJson nvarchar(2000))");
            TableAction(@"CREATE TABLE [UserData] ([UserID] nvarchar(100),[ListDate] nvarchar(100), [UserJson] nvarchar(2000))");
            TableAction(@"CREATE TABLE [MeetingData] ([MeetingID] nvarchar(100), [MeetingJson] ntext, [UserID] nvarchar(100))");
            TableAction(@"CREATE TABLE [SeriesData] ([SeriesJson] ntext, [UserID] nvarchar(100))");
            TableAction(@"CREATE TABLE [LawData] ([LawJson] ntext, [UserID] nvarchar(100))");
            TableAction(@"ALTER TABLE LoginInfo ALTER COLUMN UserJson ntext");
            TableAction(@"ALTER TABLE UserData ALTER COLUMN UserJson ntext");
            TableAction(@"ALTER TABLE FileRow ALTER COLUMN StorageFileName nvarchar(100);");
            TableAction(@"ALTER TABLE LawRow ALTER COLUMN StorageFileName nvarchar(100);");
            TableAction(@"ALTER TABLE FileRow Add COLUMN PDFFactoryParameterJson nvarchar(1000);");
            TableAction(@"CREATE TABLE [UserFile] ([FolderID] nvarchar(50), [UserID] nvarchar(50), [FileID] nvarchar(50), [MeetingID] nvarchar(50), [MeetingName] nvarchar(50), [MeetingLocation] nvarchar(50), [BeginTime] nvarchar(50))");
            TableAction(@"CREATE TABLE [UserFolder] ([FolderID] nvarchar(50), [UserID] nvarchar(50), [FolderName] nvarchar(50))");

            AccessTableAction("ALTER TABLE booknoteDetail alter COLUMN notes longText;");
            return true;
        }

        private void AccessTableAction(string sql)
        {
            try
            {
                BookManager bookManager = new BookManager(System.IO.Path.Combine(ClickOnceTool.GetDataPath(), PaperLess_Emeeting.Properties.Settings.Default.bookInfo_Path));
                bookManager.sqlCommandNonQuery(sql);
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }
        }

        private bool UpdateDBVersion_legacy(int LocalDBVersion)
        {
            bool Success = false;
            try
            {
                switch (LocalDBVersion)
                {
                    case 0:
                        MSCE.ExecuteNonQuery(@"alter table nowlogin add  MeetingUserType nvarchar(50),MeetingBeginTime datetime,MeetingEndTime datetime");
                        MSCE.ExecuteNonQuery(@"alter table filerow add EncryptionKey nvarchar(100) ");
                        //MSCE.ExecuteNonQuery("ALTER TABLE FileData add column FileVersion int default(1) ");
                        //MSCE.ExecuteNonQuery("ALTER TABLE SystemData add column RemeberLogin bool default(0),UserID nvarchar(50) ");
                        //version 5.
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [LoginInfo] ([UserID] nvarchar(50), [UserPWD] nvarchar(50),UserJson nvarchar(2000))");
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [UserData] ([UserID] nvarchar(50),[ListDate] nvarchar(50), [UserJson] nvarchar(2000))");
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [MeetingData] ([MeetingID] nvarchar(50), [MeetingJson] ntext, [UserID] nvarchar(50))");
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [SeriesData] ([SeriesJson] ntext, [UserID] nvarchar(50))");
                        
                        break;
                    case 1:
                        MSCE.ExecuteNonQuery(@"alter table nowlogin add  MeetingUserType nvarchar(50)
                                                ,MeetingBeginTime datetime
                                                ,MeetingEndTime datetime");
                        MSCE.ExecuteNonQuery(@"alter table filerow add EncryptionKey nvarchar(100) ");
                        //MSCE.ExecuteNonQuery("ALTER TABLE FileData add column FileVersion int default(1) ");
                        //MSCE.ExecuteNonQuery("ALTER TABLE SystemData add column RemeberLogin bool default(0),UserID nvarchar(50) ");
                        //version 5.
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [LoginInfo] ([UserID] nvarchar(50), [UserPWD] nvarchar(50),UserJson nvarchar(2000))");
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [UserData] ([UserID] nvarchar(50),[ListDate] nvarchar(50), [UserJson] nvarchar(2000))");
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [MeetingData] ([MeetingID] nvarchar(50), [MeetingJson] ntext, [UserID] nvarchar(50))");
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [SeriesData] ([SeriesJson] ntext, [UserID] nvarchar(50))");
                        break;
                    case 2:
                        MSCE.ExecuteNonQuery(@"alter table nowlogin add  MeetingUserType nvarchar(50)
                                                ,MeetingBeginTime datetime
                                                ,MeetingEndTime datetime");
                        MSCE.ExecuteNonQuery(@"alter table filerow add EncryptionKey nvarchar(100) ");
                        //MSCE.ExecuteNonQuery("ALTER TABLE SystemData add column RemeberLogin bool default(0),UserID nvarchar(50) ");

                        //version 5.
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [LoginInfo] ([UserID] nvarchar(50), [UserPWD] nvarchar(50),UserJson nvarchar(2000))");
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [UserData] ([UserID] nvarchar(50),[ListDate] nvarchar(50), [UserJson] nvarchar(2000))");
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [MeetingData] ([MeetingID] nvarchar(50), [MeetingJson] ntext, [UserID] nvarchar(50))");
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [SeriesData] ([SeriesJson] ntext, [UserID] nvarchar(50))");

                        break;
                    case 3:
                        MSCE.ExecuteNonQuery(@"alter table nowlogin add  MeetingUserType nvarchar(50)
                                                ,MeetingBeginTime datetime
                                                ,MeetingEndTime datetime");
                        MSCE.ExecuteNonQuery(@"alter table filerow add EncryptionKey nvarchar(100) ");
                        //MSCE.ExecuteNonQuery("ALTER TABLE SystemData add column RemeberLogin bool default(0),UserID nvarchar(50) ");

                        //version 5.
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [LoginInfo] ([UserID] nvarchar(50), [UserPWD] nvarchar(50),UserJson nvarchar(2000))");
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [UserData] ([UserID] nvarchar(50),[ListDate] nvarchar(50), [UserJson] nvarchar(2000))");
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [MeetingData] ([MeetingID] nvarchar(50), [MeetingJson] ntext, [UserID] nvarchar(50))");
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [SeriesData] ([SeriesJson] ntext, [UserID] nvarchar(50))");

                        break;
                    case 4:
                        MSCE.ExecuteNonQuery(@"alter table filerow add EncryptionKey nvarchar(100) ");

                        //version 5.
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [LoginInfo] ([UserID] nvarchar(50), [UserPWD] nvarchar(50),UserJson nvarchar(2000))");
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [UserData] ([UserID] nvarchar(50),[ListDate] nvarchar(50), [UserJson] nvarchar(2000))");
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [MeetingData] ([MeetingID] nvarchar(50), [MeetingJson] ntext, [UserID] nvarchar(50))");
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [SeriesData] ([SeriesJson] ntext, [UserID] nvarchar(50))");
                        
                        break;
                    case 5:
                        MSCE.ExecuteNonQuery(@"alter table filerow add EncryptionKey nvarchar(100) ");

                        //version 5.
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [LoginInfo] ([UserID] nvarchar(50), [UserPWD] nvarchar(50),UserJson nvarchar(2000))");
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [UserData] ([UserID] nvarchar(50),[ListDate] nvarchar(50), [UserJson] nvarchar(2000))");
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [MeetingData] ([MeetingID] nvarchar(50), [MeetingJson] ntext, [UserID] nvarchar(50))");
                        MSCE.ExecuteNonQuery(@"CREATE TABLE [SeriesData] ([SeriesJson] ntext, [UserID] nvarchar(50))");
                        break;
                }

                Success = true;
            }
            catch(Exception ex)
            {
                LogTool.Debug(ex);
            }

            return Success;
        }

        private void TableAction(string SQL)
        {
            try
            {
                MSCE.ExecuteNonQuery(SQL);
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }
        }
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //非UI線程拋出的未處理異常 
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            //UI線程的未處理異常
            //Application.Current.DispatcherUnhandledException
            Application.Current.DispatcherUnhandledException += Application_DispatcherUnhandledException;
        }
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Exception ex = e.Exception;
            App.logger.Debug(string.Format("\r\n{0}\r\n{1}\r\n{2}\r\n{3}\r\n", ex.Source, ex.TargetSite, ex.Message, ex.StackTrace));
            //MessageBox.Show("我們很抱歉，應用程式發生問題，該操作已經終止，請進行重試，如果問題繼續存在，請聯繫管理員.", "意外的操作", MessageBoxButton.OK, MessageBoxImage.Information);//這里通常需要給用戶一些較為友好的提示，並且後續可能的操作
            e.Handled = true;//使用這一行代碼告訴運行時，該異常被處理了，不再作為UnhandledException拋出了。
        }
        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            App.logger.Debug(string.Format("\r\n{0}\r\n{1}\r\n{2}\r\n{3}\r\n", "1", "2", "3", "4"));
            //MessageBox.Show("我們很抱歉，當前應用程序遇到一些問題，該操作已經終止，請進行重試，如果問題繼續存在，請聯繫管理員.", "意外的操作", MessageBoxButton.OK, MessageBoxImage.Information);

            //e.Handled = true;//使用這一行代碼告訴運行時，該異常被處理了，不再作為UnhandledException拋出了。
            //令人不解的是，這個事件中沒有和前面那個事件一樣的e.Handled參數，就是說，雖然這樣是可以捕捉到非UI線程的異常，而且也可以進行相應的處理，但是應用程序還是會退出，也就是說這個異常還是被當作是未處理異常繼續匯報給Runtime。
            //早期版本的策略就是說如果我們有處理那個事件，就不再往下傳播這個異常了。其實我覺得這才是合理的。我也沒有深入探究為什麼新版本要改變這個行為。
            //為了改進這一點，我們可以通過修改配置文件來實現。
            //<?xml version="1.0" encoding="utf-8" ?>
            //<configuration>
            //  <runtime>
            //    <legacyUnhandledExceptionPolicy enabled="1"/>
            //  </runtime>
            //  <startup>
            //    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.5" />
            //  </startup>
            //</configuration>
            //這裡的legacyUnhandledExceptionPolicy，如果enabled=1的話，用意是使用早期版本的異常處理策略。
        }
      

        private void Application_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
        }
    }

    
}
