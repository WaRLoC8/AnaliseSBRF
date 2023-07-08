using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace AnaliseSBRF
{
    class DBUtils
    {
        public static MySqlConnection GetDBConnection()
        {
            string host = "garage";
            if (Environment.MachineName.Equals("ROBOT")) host = "localhost";
            int port = 3306;
            string database = Form1.dataBase;
            string username = "root";
            string password = "Loco8360!";

            return DBMySQLUtils.GetDBConnection(host, port, database, username, password);
        }

    }
}
