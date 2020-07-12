using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

public class CRUDTool
{

    public static int Insert(string tbName, Dictionary<string, object> dict)
    {
        StringBuilder sql = new StringBuilder();
        sql.AppendFormat(@"insert into {0} (", tbName);

        string columnString = "";
        string paramString = "values(";
        foreach (string column in dict.Keys)
        {
            columnString += column+",";
            paramString += "@" + column + ",";
        }
        columnString = columnString.Trim().TrimEnd(',')+")";
        paramString = columnString.Trim().TrimEnd(',')+")";

        sql.Append(columnString);
        sql.Append(paramString);
        return MSCE.ExecuteNonQuery(sql.ToString(), dict.Values.Cast<string>().ToArray());
    }

    public static int Update(string tbName, string PKField, Dictionary<string, object> dict)
    {
        StringBuilder sql = new StringBuilder();
        sql.AppendFormat(@"update {0} set", tbName);

        string columnString = "";
        string whereString = "";
        foreach (string column in dict.Keys)
        {
            if (String.Equals(column, PKField, StringComparison.OrdinalIgnoreCase) ==false)
            {
                columnString += string.Format("{0}=@{0},", column);
            }
           
        }
        whereString = string.Format(" where {0}= @{0}", PKField);

        sql.Append(columnString.Trim().TrimEnd(','));
        sql.Append(whereString);
        return MSCE.ExecuteNonQuery(sql.ToString(), dict.Values.Cast<string>().Concat(new string[] { PKField }).ToArray());
    }

    public int Delete(string tbName, string condition)
    {
        string sql = string.Format("delete from [{0}] where {1}", tbName, condition);
        return MSCE.ExecuteNonQuery(sql);
    }
}