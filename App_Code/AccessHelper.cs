using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Data.OleDb;
using System.Collections;
/// <summary>
/// AcceHelper 的摘要說明
/// </summary>
public static class AccessHelper
{
    //數據庫連接字符串
    //public static readonly string conn = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + HttpContext.Current.Request.PhysicalApplicationPath + System.Configuration.ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
    public static readonly string conn = "";
    // 用於緩存參數的HASH表
    private static Hashtable parmCache = Hashtable.Synchronized(new Hashtable());
    /// <summary>
    ///  給定連接的數據庫用假設參數執行一個sql命令（不返回數據集）
    /// </summary>
    /// <param name="connectionString">一個有效的連接字符串</param>
    /// <param name="commandText">存儲過程名稱或者sql命令語句</param>
    /// <param name="commandParameters">執行命令所用參數的集合</param>
    /// <returns>執行命令所影響的行數</returns>
    public static int ExecuteNonQuery(string connectionString, string cmdText, params OleDbParameter[] commandParameters)
    {
        OleDbCommand cmd = new OleDbCommand();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            PrepareCommand(cmd, conn, null, cmdText, commandParameters);
            int val = cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
            return val;
        }
    }
    /// <summary>
    /// 用現有的數據庫連接執行一個sql命令（不返回數據集）
    /// </summary>
    /// <remarks>
    ///舉例:  
    ///  int result = ExecuteNonQuery(connString, "PublishOrders", new OleDbParameter("@prodid", 24));
    /// </remarks>
    /// <param name="conn">一個現有的數據庫連接</param>
    /// <param name="commandText">存儲過程名稱或者sql命令語句</param>
    /// <param name="commandParameters">執行命令所用參數的集合</param>
    /// <returns>執行命令所影響的行數</returns>
    public static int ExecuteNonQuery(OleDbConnection connection, string cmdText, params OleDbParameter[] commandParameters)
    {
        OleDbCommand cmd = new OleDbCommand();
        PrepareCommand(cmd, connection, null, cmdText, commandParameters);
        int val = cmd.ExecuteNonQuery();
        cmd.Parameters.Clear();
        return val;
    }
    /// <summary>
    ///使用現有的SQL事務執行一個sql命令（不返回數據集）
    /// </summary>
    /// <remarks>
    ///舉例:  
    ///  int result = ExecuteNonQuery(trans, "PublishOrders", new OleDbParameter("@prodid", 24));
    /// </remarks>
    /// <param name="trans">一個現有的事務</param>
    /// <param name="commandText">存儲過程名稱或者sql命令語句</param>
    /// <param name="commandParameters">執行命令所用參數的集合</param>
    /// <returns>執行命令所影響的行數</returns>
    public static int ExecuteNonQuery(OleDbTransaction trans, string cmdText, params OleDbParameter[] commandParameters)
    {
        OleDbCommand cmd = new OleDbCommand();
        PrepareCommand(cmd, trans.Connection, trans, cmdText, commandParameters);
        int val = cmd.ExecuteNonQuery();
        cmd.Parameters.Clear();
        return val;
    }
    /// <summary>
    /// 用執行的數據庫連接執行一個返回數據集的sql命令
    /// </summary>
    /// <remarks>
    /// 舉例:  
    ///  OleDbDataReader r = ExecuteReader(connString, "PublishOrders", new OleDbParameter("@prodid", 24));
    /// </remarks>
    /// <param name="connectionString">一個有效的連接字符串</param>
    /// <param name="commandText">存儲過程名稱或者sql命令語句</param>
    /// <param name="commandParameters">執行命令所用參數的集合</param>
    /// <returns>包含結果的讀取器</returns>
    public static OleDbDataReader ExecuteReader(string connectionString, string cmdText, params OleDbParameter[] commandParameters)
    {
        //創建一個SqlCommand對象
        OleDbCommand cmd = new OleDbCommand();
        //創建一個SqlConnection對象
        OleDbConnection conn = new OleDbConnection(connectionString);
        //在這裡我們用一個try/catch結構執行sql文本命令/存儲過程，因為如果這個方法產生一個異常我們要關閉連接，因為沒有讀取器存在，
        //因此commandBehaviour.CloseConnection 就不會執行
        try
        {
            //調用 PrepareCommand 方法，對 SqlCommand 對象設置參數
            PrepareCommand(cmd, conn, null, cmdText, commandParameters);
            //調用 SqlCommand  的 ExecuteReader 方法
            OleDbDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
            //清除參數
            cmd.Parameters.Clear();
            return reader;
        }
        catch
        {
            //關閉連接，拋出異常
            conn.Close();
            throw;
        }
    }
    /// <summary>
    /// 返回一個DataSet數據集
    /// </summary>
    /// <param name="connectionString">一個有效的連接字符串</param>
    /// <param name="cmdText">存儲過程名稱或者sql命令語句</param>
    /// <param name="commandParameters">執行命令所用參數的集合</param>
    /// <returns>包含結果的數據集</returns>
    public static DataSet ExecuteDataSet(string connectionString, string cmdText, params OleDbParameter[] commandParameters)
    {
        //創建一個SqlCommand對象，並對其進行初始化
        OleDbCommand cmd = new OleDbCommand();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            PrepareCommand(cmd, conn, null, cmdText, commandParameters);
            //創建SqlDataAdapter對象以及DataSet
            OleDbDataAdapter da = new OleDbDataAdapter(cmd);
            DataSet ds = new DataSet();
            try
            {
                //填充ds
                da.Fill(ds);
                // 清除cmd的參數集合 
                cmd.Parameters.Clear();
                //返回ds
                return ds;
            }
            catch
            {
                //關閉連接，拋出異常
                conn.Close();
                throw;
            }
        }
    }
    /// <summary>
    /// 用指定的數據庫連接字符串執行一個命令並返回一個數據集的第一列
    /// </summary>
    /// <remarks>
    ///例如:  
    ///  Object obj = ExecuteScalar(connString, "PublishOrders", new OleDbParameter("@prodid", 24));
    /// </remarks>
    ///<param name="connectionString">一個有效的連接字符串</param>
    /// <param name="commandText">存儲過程名稱或者sql命令語句</param>
    /// <param name="commandParameters">執行命令所用參數的集合</param>
    /// <returns>用 Convert.To{Type}把類型轉換為想要的 </returns>
    public static object ExecuteScalar(string connectionString, string cmdText, params OleDbParameter[] commandParameters)
    {
        OleDbCommand cmd = new OleDbCommand();
        using (OleDbConnection connection = new OleDbConnection(connectionString))
        {
            PrepareCommand(cmd, connection, null, cmdText, commandParameters);
            object val = cmd.ExecuteScalar();
            cmd.Parameters.Clear();
            return val;
        }
    }
    /// <summary>
    /// 用指定的數據庫連接執行一個命令並返回一個數據集的第一列
    /// </summary>
    /// <remarks>
    /// 例如:  
    ///  Object obj = ExecuteScalar(connString, "PublishOrders", new OleDbParameter("@prodid", 24));
    /// </remarks>
    /// <param name="conn">一個存在的數據庫連接</param>
    /// <param name="commandText">存儲過程名稱或者sql命令語句</param>
    /// <param name="commandParameters">執行命令所用參數的集合</param>
    /// <returns>用 Convert.To{Type}把類型轉換為想要的 </returns>
    public static object ExecuteScalar(OleDbConnection connection, string cmdText, params OleDbParameter[] commandParameters)
    {
        OleDbCommand cmd = new OleDbCommand();
        PrepareCommand(cmd, connection, null, cmdText, commandParameters);
        object val = cmd.ExecuteScalar();
        cmd.Parameters.Clear();
        return val;
    }
    /// <summary>
    /// 將參數集合添加到緩存
    /// </summary>
    /// <param name="cacheKey">添加到緩存的變量</param>
    /// <param name="cmdParms">一個將要添加到緩存的sql參數集合</param>
    public static void CacheParameters(string cacheKey, params OleDbParameter[] commandParameters)
    {
        parmCache[cacheKey] = commandParameters;
    }
    /// <summary>
    /// 找回緩存參數集合
    /// </summary>
    /// <param name="cacheKey">用於找回參數的關鍵字</param>
    /// <returns>緩存的參數集合</returns>
    public static OleDbParameter[] GetCachedParameters(string cacheKey)
    {
        OleDbParameter[] cachedParms = (OleDbParameter[])parmCache[cacheKey];
        if (cachedParms == null)
            return null;
        OleDbParameter[] clonedParms = new OleDbParameter[cachedParms.Length];
        for (int i = 0, j = cachedParms.Length; i < j; i++)
            clonedParms = (OleDbParameter[])((ICloneable)cachedParms).Clone();
        return clonedParms;
    }
    /// <summary>
    /// 準備執行一個命令
    /// </summary>
    /// <param name="cmd">sql命令</param>
    /// <param name="conn">Sql連接</param>
    /// <param name="trans">Sql事務</param>
    /// <param name="cmdText">命令文本,例如：Select * from Products</param>
    /// <param name="cmdParms">執行命令的參數</param>
    private static void PrepareCommand(OleDbCommand cmd, OleDbConnection conn, OleDbTransaction trans, string cmdText, OleDbParameter[] cmdParms)
    {
        //判斷連接的狀態。如果是關閉狀態，則打開
        if (conn.State != ConnectionState.Open)
            conn.Open();
        //cmd屬性賦值
        cmd.Connection = conn;
        cmd.CommandText = cmdText;
        //是否需要用到事務處理
        if (trans != null)
            cmd.Transaction = trans;
        cmd.CommandType = CommandType.Text;
        //添加cmd需要的存儲過程參數
        if (cmdParms != null)
        {
            foreach (OleDbParameter parm in cmdParms)
                cmd.Parameters.Add(parm);
        }
    }
}