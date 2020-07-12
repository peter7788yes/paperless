using PaperLess_Emeeting.App_Code;
using PaperLess_Emeeting.App_Code.ClickOnce;
using PaperLess_Emeeting.App_Code.DownloadItem;
using PaperLess_Emeeting.App_Code.WS;
using PaperLess_ViewModel;
using System;
using System.Data;
using System.IO;
using System.Windows.Media;

/// <summary>
/// FileTool 的摘要描述
/// </summary>
public class FileItemTool
{
    public static File_DownloadItemViewModel Gen(MeetingDataDownloadFileFile meetingDataDownloadFileFile, string UserID, string MeetingID)
    {
        // 更新檔不支援續傳
        // 檢查是否再下載當中
        // 下面兩行在多執行緒環境可能換造成死結
        // 把把他們都丟到main thread去跑就可以解決死結了
        //FileDownloader fileDownloader = Singleton_FileDownloader.GetInstance(MeetingID);
        //File_DownloadItemViewModel fileItem = fileDownloader.GetInList(meetingDataDownloadFileFile.ID);
        //if (fileItem != null)
        //    return fileItem;

        File_DownloadItemViewModel fileItem = new File_DownloadItemViewModel();

        try
        {
            #region DB
            string db_FileRowID = "";
            string db_Url = "";
            string db_StorageFileName = "";
            long db_DownloadBytes = 0;
            long db_TotalBytes = 0;
            // FileVersion為現在目前檔案的版本
            int db_FileVersion = 1;
            // 有完成過的檔案版本
            int db_FinishedFileVersion = 0;
            string db_EncryptionKey = "";
            string SQL = "";
            int success = 0;
            DataTable dt = MSCE.GetDataTable("SELECT ID,Url,StorageFileName,DownloadBytes,TotalBytes,FileVersion,FinishedFileVersion,EncryptionKey FROM FileRow where ID=@1 and UserID=@2 and MeetingID=@3"
                                   , meetingDataDownloadFileFile.ID
                                   , UserID
                                   , MeetingID);
            if (dt.Rows.Count > 0)
            {
                db_FileRowID = dt.Rows[0]["ID"].ToString();
                db_Url = dt.Rows[0]["Url"].ToString();
                db_StorageFileName = dt.Rows[0]["StorageFileName"].ToString();
                db_DownloadBytes = long.Parse(dt.Rows[0]["DownloadBytes"].ToString().Equals("") ? "0" : dt.Rows[0]["DownloadBytes"].ToString());
                db_TotalBytes = long.Parse(dt.Rows[0]["TotalBytes"].ToString().Equals("") ? "0" : dt.Rows[0]["TotalBytes"].ToString());
                db_FileVersion = int.Parse(dt.Rows[0]["FileVersion"].ToString().Equals("") || dt.Rows[0]["FileVersion"].ToString().Equals("0") ? "1" : dt.Rows[0]["FileVersion"].ToString());
                db_FinishedFileVersion = int.Parse(dt.Rows[0]["FinishedFileVersion"].ToString().Equals("") ? "0" : dt.Rows[0]["FileVersion"].ToString());
                db_EncryptionKey = dt.Rows[0]["EncryptionKey"].ToString();
            }
            else
            {
                SQL = @"INSERT INTO FileRow(ID,DownloadBytes,TotalBytes,UserID,MeetingID,DisplayFileName,FileVersion,EncryptionKey) 
                                                    VALUES(@1,0,0,@2,@3,@4,@5,@6)";
                success = MSCE.ExecuteNonQuery(SQL
                                                   , meetingDataDownloadFileFile.ID
                                                   , UserID
                                                   , MeetingID
                                                   , meetingDataDownloadFileFile.FileName
                                                   , meetingDataDownloadFileFile.version.Equals("") == true ? "1" : meetingDataDownloadFileFile.version
                                                   , meetingDataDownloadFileFile.EncryptionKey == null ? "" : meetingDataDownloadFileFile.EncryptionKey);
                if (success < 1)
                    LogTool.Debug(new Exception(@"DB失敗: " + SQL));
            }
            #endregion

            //File_DownloadItemViewModel fileItem = new File_DownloadItemViewModel();
            fileItem.MeetingID = MeetingID;
            fileItem.ID = meetingDataDownloadFileFile.ID;
            fileItem.UserID = UserID;
            fileItem.FileName = meetingDataDownloadFileFile.FileName;
            fileItem.Url = meetingDataDownloadFileFile.Url;
            fileItem.AgendaID = meetingDataDownloadFileFile.AgendaID;
            fileItem.EncryptionKey = meetingDataDownloadFileFile.EncryptionKey == null ? "" : meetingDataDownloadFileFile.EncryptionKey;
            //string AppPath = System.AppDomain.CurrentDomain.BaseDirectory;
            //string File_StorageFileFolder = PaperLess_Emeeting.Properties.Settings.Default.File_StorageFileFolder;
            //fileItem.StorageFileFolder = System.IO.Path.Combine(ClickOnceTool.GetFilePath() , File_StorageFileFolder);
            fileItem.StorageFileFolder = Path.Combine(ClickOnceTool.GetFilePath(), PaperLess_Emeeting.Properties.Settings.Default.File_StorageFileFolder);
            #region 取得 Http URL 的檔名
            string fileName = DateTime.Now.ToFileTime().ToString();
            try
            {
                Uri uri = new Uri(fileItem.Url);
                string tempFileName = System.IO.Path.GetFileName(uri.LocalPath);
                if (tempFileName.Equals(@"/") == false)
                    fileName = tempFileName;
            }
            catch (Exception ex)
            {
                LogTool.Debug(ex);
            }
            #endregion

            fileItem.StorageFileName = string.Format("{0}_{1}_{2}_{3}", UserID, MeetingID, meetingDataDownloadFileFile.ID, fileName);
            fileItem.UnZipFileFolder = Path.Combine(ClickOnceTool.GetFilePath(), PaperLess_Emeeting.Properties.Settings.Default.File_UnZipFileFolder);
            LogTool.Debug(ClickOnceTool.GetFilePath());
            fileItem.DownloadBytes = db_DownloadBytes;
            fileItem.TotalBytes = db_TotalBytes;
            int tempFileItemFileVersion =0;
            int.TryParse(meetingDataDownloadFileFile.version,out tempFileItemFileVersion);
            if (tempFileItemFileVersion < 1)
                tempFileItemFileVersion = 1;
            fileItem.FileVersion = tempFileItemFileVersion;
            if (db_FinishedFileVersion >0 && (fileItem.FileVersion > db_FinishedFileVersion ))
            {
                //可更新
                fileItem.CanUpdate = true;
            }

            // 先檢查檔案存不存在
            if (File.Exists(fileItem.StorageFilePath) == true)
            {
                // 未下載完成的，從來沒有下載過
                if (db_DownloadBytes == 0)
                {
                    // 刪除未下載完成但是檔案存在的，但是DB紀錄為沒有下載過的，或是被刪除的
                    // 例外狀況不會擲回
                    if (File.Exists(fileItem.StorageFilePath) == true)
                        File.Delete(fileItem.StorageFilePath);

                    fileItem.DownloadBytes = 0;
                    fileItem.TotalBytes = 0;
                    fileItem.FileType = MeetingFileType.從未下載;

                }
                else if (db_DownloadBytes < db_TotalBytes) //未下載完成的，有下載過的
                {
                    if (fileItem.CanUpdate == true)
                    {
                        // 未下載完成的，需要更新
                        // 可以更新，就算未下載完成
                        // DownloadBytes和 TotalBytes 也把他設定成0
                        fileItem.DownloadBytes = 0;
                        fileItem.TotalBytes = 0;
                        fileItem.FileType = MeetingFileType.已經下載過一次且可以更新版本的檔案_目前下載未完成;
                    }
                    else   // 未下載完成的，不用更新
                    {
                        // 下載到一半的
                        fileItem.DownloadBytes = db_DownloadBytes;
                        fileItem.TotalBytes = db_TotalBytes;
                        fileItem.FileType = MeetingFileType.已下載過但是未完成的檔案;
                    }
                }
                else
                {
                    // 已經下載完成的，需要更新
                    if (fileItem.CanUpdate == true)
                    {
                        fileItem.DownloadBytes = 0;
                        fileItem.TotalBytes = 0;
                        fileItem.FileType = MeetingFileType.已經下載過一次且可以更新版本的檔案_目前下載已完成;
                    }
                    else // 已經下載完成的，不用更新
                    {
                        fileItem.FileType = MeetingFileType.已下載完成;
                    }

                    //結束;
                }
            }
            else
            {
                fileItem.DownloadBytes = 0;
                fileItem.TotalBytes = 0;
                fileItem.FileType = MeetingFileType.從未下載;
            }

            // 把DB的檔案資訊更新
            SQL = @"update FileRow set DownloadBytes=@1,TotalBytes=@2,UserID=@3,MeetingID=@4,FileVersion=@5 where ID=@6";
            success = MSCE.ExecuteNonQuery(SQL
                                           , fileItem.DownloadBytes.ToString()
                                           , fileItem.TotalBytes.ToString()
                                           , UserID
                                           , MeetingID
                                           , fileItem.FileVersion.ToString()
                                           , fileItem.ID);
            if (success < 1)
                LogTool.Debug(new Exception(@"DB失敗: " + SQL));
        }
        catch (Exception ex)
        {
            LogTool.Debug(ex);
        }
        return fileItem;
    }
}
