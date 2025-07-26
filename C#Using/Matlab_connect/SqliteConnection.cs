using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Data;
using System.Data.SQLite;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;

namespace Matlab_connect
{
    internal class SqliteConnection
    {
        DataTable dtData = new DataTable();
        //创建数据库
        SQLiteConnection con = new SQLiteConnection("Data Source=D:\\Project\\C#_work\\Matlab_connect\\Matlab_connect\\database.db;Version=3;");
        public void OpenList()
        {
            con.Open();
        }
        public int getListRow(string ListName)
        {
            string sql = "select count(*) from "+ ListName+";";
            SQLiteCommand countsql = new SQLiteCommand(sql, con);
            int row = Convert.ToInt32(countsql.ExecuteScalar());
            Console.WriteLine(row);
            return row;
        }
        public void InsertRouteData(string ListName,double x_route, double y_route, double z_route,int route_num)
        {
            int row = getListRow(ListName);
            string insertCommand = "Insert INTO "+ ListName+ "(id,x_route,y_route,z_route)" +
                " VALUES(" + row.ToString() + "," + x_route.ToString() + "," + y_route.ToString() + "," + z_route.ToString() + ")";
            SQLiteCommand command = new SQLiteCommand(insertCommand, con);
            SQLiteDataReader reader = command.ExecuteReader();
        }
        public void CloseList()
        {
            con.Close();
        }

    }


}
