using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Configuration;
using System.Data.SqlServerCe;
using PaperLess_Emeeting.App_Code.ClickOnce;
using System.IO;

public class MSCE
{
    //private static string Conn = "Data Source = '" + App.DB_DirPath + @"/PaperDB.sdf';Password = '" + App.DB_Password + "';";
    private static string Conn = string.Format("Data Source = '{0}';Password = '{1}';",  Path.Combine(ClickOnceTool.GetDataPath() , PaperLess_Emeeting.Properties.Settings.Default.PaperLessDB_Path), "hywebhdlw");
    private static char[] SplitChars = new char[] { ' ', ',', '(', ')', ';', '=', '+', '\'', '+' ,'\r','\n','\t'};

    //回傳第一資料列第一資料行
    public static string ExecuteScalar(string SQL, params string[] parameters)
    {
        string Rtn = "";
        try
        {
            Rtn = ExecuteScalarWithConn(Conn, SQL, parameters);
        }
        catch
        {
            throw;
        }
        return Rtn;
    }
    //執行語法
    public static string ExecuteScalarWithConn(string ConnectionString, string SQL, params string[] parameters)
    {
        string Rtn = "";
        try
        {
            List<string> paramInSQL = GetParameters(SQL);

            using (SqlCeConnection sc = new SqlCeConnection(ConnectionString))
            {
                using (SqlCeCommand cmd = new SqlCeCommand(SQL, sc))
                {
                    //CommandTimeout 重設為30秒
                    //cmd.ResetCommandTimeout();

                    //怕下列指令執行較長,將他延長設為120秒
                    //cmd.CommandTimeout = 120;

                    for (int i = 0; i <= paramInSQL.Count - 1; i++)
                    {
                        cmd.Parameters.AddWithValue(paramInSQL[i], parameters[i]);
                    }
                    sc.Open();
                    object obj = cmd.ExecuteScalar();
                    if (obj != null)
                        Rtn = obj.ToString();

                }
            }

        }
        catch
        {
            throw;
        }

        return Rtn;
    }

    //執行語法
    public static int ExecuteNonQuery(string SQL, params string[] parameters)
    {
        int Success = 0;
        try
        {
            Success = ExecuteNonQueryWithConn(Conn, SQL, parameters);
        }
        catch(Exception ex)
        {
            throw;
        }
        return Success;
    }
    //執行語法
    public static int ExecuteNonQueryWithConn(string ConnectionString, string SQL, params string[] parameters)
    {
        int Success = 0;
        try
        {
            List<string> paramInSQL = GetParameters(SQL);

            using (SqlCeConnection sc = new SqlCeConnection(ConnectionString))
            {
                using (SqlCeCommand cmd = new SqlCeCommand(SQL, sc))
                {
                    //CommandTimeout 重設為30秒
                    //cmd.ResetCommandTimeout();

                    //怕下列指令執行較長,將他延長設為120秒
                    //cmd.CommandTimeout = 120;

                    for (int i = 0; i <= paramInSQL.Count - 1; i++)
                    {
                        cmd.Parameters.AddWithValue(paramInSQL[i], parameters[i]);
                    }
                    sc.Open();
                    Success = cmd.ExecuteNonQuery();

                }
            }

        }
        catch
        {
            throw;
        }

        return Success;
    }

    //取得DataTable
    public static DataTable GetDataTable(string SQL, params string[] parameters)
    {
        DataTable dt = new DataTable();
        try
        {
            dt = GetDataTableWithConn(Conn, SQL, parameters);
        }
        catch(Exception ex)
        {
            throw;
        }
        return dt;
    }
    //取得DataTable
    public static DataTable GetDataTableWithConn(string ConnectionString, string SQL, params string[] parameters)
    {
        DataTable dt = new DataTable();
        try
        {
            List<string> paramInSQL = GetParameters(SQL);

            using (SqlCeConnection sc = new SqlCeConnection(ConnectionString))
            {
                using (SqlCeCommand cmd = new SqlCeCommand(SQL, sc))
                {
                    //CommandTimeout 重設為30秒
                    //cmd.ResetCommandTimeout();

                    //怕下列指令執行較長,將他延長設為120秒
                    //cmd.CommandTimeout = 120;

                    for (int i = 0; i <= paramInSQL.Count - 1; i++)
                    {
                        cmd.Parameters.AddWithValue(paramInSQL[i], parameters[i]);
                    }
                    string s = cmd.CommandText;
                    using (SqlCeDataAdapter da = new SqlCeDataAdapter(cmd))
                    {
                        sc.Open();
                        da.Fill(dt);
                    }
                    

                }
                
            }

        }
        catch(Exception ex)
        {
            string s = ex.Message;
            throw;
        }

        return dt;
    }


    //取得DataSet
    public static DataSet GetDataSet(string SQL, params string[] parameters)
    {
        DataSet ds = new DataSet();
        try
        {
            ds = GetDataSetWithConn(Conn, SQL, parameters);
        }
        catch (Exception ex)
        {
            throw;
        }
        return ds;
    }
    //取得DataSet
    public static DataSet GetDataSetWithConn(string ConnectionString, string SQL, params string[] parameters)
    {
        DataSet ds = new DataSet();
        try
        {
            List<string> paramInSQL = GetParameters(SQL);

            using (SqlCeConnection sc = new SqlCeConnection(ConnectionString))
            {
                using (SqlCeCommand cmd = new SqlCeCommand(SQL, sc))
                {
                    //CommandTimeout 重設為30秒
                    //cmd.ResetCommandTimeout();

                    //怕下列指令執行較長,將他延長設為120秒
                    //cmd.CommandTimeout = 120;
                    for (int i = 0; i <= paramInSQL.Count - 1; i++)
                    {
                        cmd.Parameters.AddWithValue(paramInSQL[i], parameters[i]);
                    }

                    using (SqlCeDataAdapter da = new SqlCeDataAdapter(cmd))
                    {
                        sc.Open();
                        da.Fill(ds);
                    }

                }
            }

        }
        catch
        {
            throw;
        }

        return ds;
    }

    //取得DataSet
    public static bool TransactionSQLs(List<string> SQLs, params string[] parameters)
    {
        bool finished = false;
        try
        {
            finished = TransactionSQLsWithConn(Conn, SQLs, parameters);
        }
        catch
        {
            throw;
        }
        return finished;
    }

    //多SQL交易
    public static bool TransactionSQLsWithConn(string ConnectionString, List<string> SQLs, params string[] parameters)
    {
        bool finished = false;

        try
        {
            List<string[]> wordsInSQLs = new List<string[]>();
            foreach (string sql in SQLs)
            {
                wordsInSQLs.Add(sql.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries));
            }

            List<List<string>> paramInSQLs = new List<List<string>>();
            foreach (var wordsInSQL in wordsInSQLs)
            {
                List<string> paramInSQL = new List<string>();
                foreach (var word in wordsInSQL)
                {
                    if (word.Contains("@@"))
                        continue;
                    if (word.Contains("@"))
                        paramInSQL.Add(word.Substring(word.IndexOf("@"), word.Length - word.IndexOf("@")));
                }
                paramInSQLs.Add(paramInSQL);

            }

            using (SqlCeConnection sc = new SqlCeConnection(ConnectionString))
            {
                sc.Open();
                SqlCeTransaction trans = sc.BeginTransaction();
                int parametersIndex = 0;
                string identity = "";
                try
                {
                    for (int i = 0; i <= SQLs.Count - 1; i++)
                    {
                        SqlCeCommand cmd = new SqlCeCommand(SQLs[i], sc);
                        cmd.Transaction = trans;
                        //CommandTimeout 重設為30秒
                        //cmd.ResetCommandTimeout();

                        //怕下列指令執行較長,將他延長設為120秒
                        //cmd.CommandTimeout = 120;


                        cmd.Parameters.Clear();
                        List<string> TakeOutParamInSQL = paramInSQLs[i];
                        for (int j = 0; j <= TakeOutParamInSQL.Count - 1; j++)
                        {
                            if (i != 0 && j == 0)
                                cmd.Parameters.AddWithValue(TakeOutParamInSQL[j], identity);
                            else
                                cmd.Parameters.AddWithValue(TakeOutParamInSQL[j], parameters[parametersIndex]);

                            parametersIndex++;
                        }
                        if (SQLs[i].Contains("@@"))
                        {
                            DataTable dt = new DataTable();
                            using (SqlCeDataAdapter da = new SqlCeDataAdapter(cmd))
                            {
                                da.Fill(dt);
                            }
                            if (dt.Rows.Count > 0)
                                identity = dt.Rows[0][0].ToString();
                        }
                        else
                            cmd.ExecuteNonQuery();

                    }
                    trans.Commit();
                    finished = true;
                }
                catch
                {
                    trans.Rollback();
                    finished = false;
                    throw;
                }
            }

        }
        catch
        {
            finished = false;
            throw;
        }

        return finished;
    }

    //很慢
    public static bool StartTransaction(string SQL, DataTable dt)
    {
        bool finished = false;
        try
        {
            finished = StartTransactionWithConn(Conn, SQL, dt);
        }
        catch
        {
            throw;
        }
        return finished;

    }
    //很慢
    public static bool StartTransactionWithConn(string ConnectionString, string SQL, DataTable dt)
    {
        bool finished = false;

        try
        {
            List<string> paramInSQL = GetParameters(SQL);

            using (SqlCeConnection sc = new SqlCeConnection(ConnectionString))
            {
                sc.Open();
                SqlCeTransaction trans = sc.BeginTransaction();

                try
                {
                    SqlCeCommand cmd = new SqlCeCommand(SQL, sc);
                    cmd.Transaction = trans;
                    //CommandTimeout 重設為30秒
                    //cmd.ResetCommandTimeout();

                    //怕下列指令執行較長,將他延長設為120秒
                    //cmd.CommandTimeout = 120;

                    foreach (DataRow dr in dt.Rows)
                    {
                        cmd.Parameters.Clear();
                        for (int i = 0; i <= paramInSQL.Count - 1; i++)
                        {
                            cmd.Parameters.AddWithValue(paramInSQL[i], dr[i]);
                        }
                        cmd.ExecuteNonQuery();
                    }

                    trans.Commit();
                    finished = true;
                }
                catch
                {
                    trans.Rollback();
                    finished = false;
                    throw;
                }


            }

        }
        catch
        {
            finished = false;
            throw;
        }

        return finished;

    }

   
   

    //懶人insert 
    //用法:insertTable("Brand","@name","@Descp","@LogicDel","sss","ppp","1");
    //EX:得到==>insert into Brand (name,Descp,LogicDel) values(@sss,@ppp,@1)
    //   然後呼叫ExecuteNonQueryWithConn(ConnectionString, SQL, values.ToArray());
    public static int insertTable(string TableName, params string[] parameters)
    {
        int Success = 0;
        try
        {
            Success = insertTableWithConn(Conn, TableName, parameters);

        }
        catch
        {
            throw;
        }
        return Success;

    }

    //懶人insert
    public static int insertTableWithConn(string ConnectionString, string TableName, params string[] parameters)
    {
        int Success = 0;

        try
        {
            int ColumnNameEndIndex = parameters.Length / 2 - 1;

            string SQL = string.Format("insert into {0} (", TableName);
            for (int i = 0; i <= ColumnNameEndIndex; i++)
            {
                SQL += string.Format("{0},", parameters[i].TrimStart('@'));
            }
            SQL = SQL.TrimEnd(',') + ") values(";

            List<string> values = new List<string>();
            for (int j = ColumnNameEndIndex + 1; j <= parameters.Length - 1; j++)
            {
                SQL += string.Format("@{0},", parameters[j]);
                values.Add(parameters[j]);
            }
            SQL = SQL.TrimEnd(',') + ")";

            ExecuteNonQueryWithConn(ConnectionString, SQL, values.ToArray());

        }
        catch
        {
            throw;
        }
        return Success;

    }

    //懶人update
    //用法:MSDB.updateTable("Brand", MSDB.GetWhere("@name","dd"), "@logicdel", "0");
    public static int updateTable(string TableName, Dictionary<string, string> whereDict, params string[] parameters)
    {
        int Success = 0;
        try
        {
            Success = updateTableWithConn(Conn, TableName, whereDict, parameters);

        }
        catch
        {
            throw;
        }
        return Success;

    }

    //懶人insert
    public static int updateTableWithConn(string ConnectionString, string TableName, Dictionary<string, string> whereDict, params string[] parameters)
    {
        int Success = 0;

        if (CheckParam(parameters) == false)
            throw new Exception("請檢查，參數有錯誤。");

        try
        {
            int numOfValues = parameters.Length / 2;
            int ColumnNameEndIndex = numOfValues - 1;

            string SQL = string.Format("update {0} set ", TableName);
            for (int i = 0; i <= ColumnNameEndIndex; i++)
            {
                SQL += string.Format("{0}=#{1},", parameters[i].TrimStart('@'), i);
            }
            SQL = string.Format("{0} where ", SQL.TrimEnd(','));

            List<string> values = new List<string>();
            for (int j = ColumnNameEndIndex + 1; j <= parameters.Length - 1; j++)
            {
                SQL = SQL.Replace(string.Format("#{0}", j - numOfValues), "@" + parameters[j]);
                values.Add(parameters[j]);
            }

            foreach (string key in whereDict.Keys)
            {
                string column = key.TrimStart('@');
                if (column.Contains("(in)") == true)
                    SQL += string.Format("{0} in ({1}) and ", column.Substring(0, column.Length - 4), whereDict[key]);
                else
                    SQL += string.Format("{0}=@{1}  and ", column, whereDict[key]);
                values.Add(whereDict[key]);
                SQL = SQL.Substring(0, SQL.Length - 4);
            }
            ExecuteNonQueryWithConn(ConnectionString, SQL, values.ToArray());
        }
        catch
        {
            throw;
        }
        return Success;
    }

    //懶人delete
    //用法: MSDB.deleteTable("Brand", MSDB.GetWhere("@name","dd"));
    public static int deleteTable(string TableName, Dictionary<string, string> whereDict, params string[] parameters)
    {
        int Success = 0;
        try
        {
            Success = deleteTableWithConn(Conn, TableName, whereDict);

        }
        catch
        {
            throw;
        }
        return Success;

    }

    //懶人delete
    public static int deleteTableWithConn(string ConnectionString, string TableName, Dictionary<string, string> whereDict)
    {
        int Success = 0;

        try
        {
            string SQL = string.Format("delete {0}  where ", TableName);

            List<string> values = new List<string>();

            foreach (string key in whereDict.Keys)
            {
                string column = key.TrimStart('@');
                if (column.Contains("(in)") == true)
                    SQL += string.Format("{0} in ({1}) and ", column.Substring(0, column.Length - 4), whereDict[key]);
                else
                    SQL += string.Format("{0}=@{1}  and ", column, whereDict[key]);
                values.Add(whereDict[key]);
                SQL = SQL.Substring(0, SQL.Length - 4);
            }

            ExecuteNonQueryWithConn(ConnectionString, SQL, values.ToArray());

        }
        catch
        {
            throw;
        }
        return Success;

    }

    //懶人select
    //用法: MSDB.deleteTable("Brand", MSDB.GetWhere("@name","dd"));
    public static DataTable selectTable(string TableName, Dictionary<string, string> selectDict = null, Dictionary<string, string> whereDict = null)
    {
        DataTable dt = new DataTable();
        try
        {
            dt = selectTableWithConn(Conn, TableName, selectDict, whereDict);

        }
        catch
        {
            throw;
        }
        return dt;

    }

    //懶人select
    public static DataTable selectTableWithConn(string ConnectionString, string TableName, Dictionary<string, string> selectDict = null, Dictionary<string, string> whereDict = null)
    {
        DataTable dt = new DataTable();

        try
        {
            string SQL = "select ";
            if (selectDict != null)
            {
                foreach (string key in selectDict.Keys)
                {
                    if (selectDict[key].Trim().Equals(""))
                        SQL += string.Format(" {0}, ", key.TrimStart('@'));
                    else
                        SQL += string.Format(" {0} as '{1}',", key.TrimStart('@'), selectDict[key]);
                }
                SQL = SQL.TrimEnd(',');
            }
            else
                SQL += "*";
            SQL += string.Format(" from {0} ", TableName);

            List<string> values = new List<string>();

            if (whereDict != null)
            {
                SQL += " where ";
                foreach (string key in whereDict.Keys)
                {
                    string column = key.TrimStart('@');
                    if (column.Contains("(in)") == true)
                        SQL += string.Format("{0} in ({1}) and ", column.Substring(0, column.Length - 4), whereDict[key]);
                    else
                        SQL += string.Format("{0}=@{1}  and ", column, whereDict[key]);
                    values.Add(whereDict[key]);
                }
                SQL = SQL.Substring(0, SQL.Length - 4);
            }
            dt = GetDataTableWithConn(ConnectionString, SQL, values.ToArray());
        }
        catch
        {
            throw;
        }
        return dt;

    }

    //懶人where
    public static Dictionary<string, string> GetWhere(params string[] parameters)
    {

        if (CheckParam(parameters) == false)
            throw new Exception("請檢查，參數有錯誤。");

        Dictionary<string, string> dict = new Dictionary<string, string>();

        for (int i = 0; i <= parameters.Length - 1; i++)
        {
            if (i % 2 == 0 && i != parameters.Length - 1)
            {
                if (parameters[i].Contains("(in)"))
                {
                    string[] arr = parameters[i + 1].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    string value = "'" + string.Join("','", arr) + "'";
                    dict.Add(parameters[i], value);
                }
                else
                    dict.Add(parameters[i], parameters[i + 1]);
            }
        }
        return dict;
    }

    //懶人select
    public static Dictionary<string, string> GetSelect(params string[] parameters)
    {
        if (CheckParam(parameters) == false)
            throw new Exception("請檢查，參數有錯誤。");

        Dictionary<string, string> dict = new Dictionary<string, string>();

        for (int i = 0; i <= parameters.Length - 1; i++)
        {
            if (i % 2 == 0 && i != parameters.Length - 1)
            {
                dict.Add(parameters[i], parameters[i + 1]);
            }
        }
        return dict;
    }

    private static List<string> GetParameters(string SQL)
    {
        List<string> paramInSQL = new List<string>();

        string[] wordsInSQL = SQL.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries);


        foreach (var word in wordsInSQL)
        {
            if (word.Contains("@@"))
                continue;
            if (word.Contains("@"))
                paramInSQL.Add(word.Substring(word.IndexOf("@"), word.Length - word.IndexOf("@")));
        }

        return new List<string>(paramInSQL.Distinct());
    }

    private static bool CheckParam(params string[] parameters)
    {
        bool Rtn = true;

        if (parameters.Length % 2 == 1)
            Rtn = false;
        for (int i = 0; i <= parameters.Length - 1; i++)
        {
            if (i % 2 == 0)
            {
                if (parameters[i].StartsWith("@") == false)
                    Rtn = false;
            }
        }

        return Rtn;
    }
}



