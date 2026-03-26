using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QIN_Production_Web.Data
{
    public class SqlManager
    {
        private static readonly string server = "QINSQL064";
        private static readonly string database = "qinFSK\\table1";
        private static readonly string fertigungdatabase = "Fertigung";
        private static readonly string username = "db.user";
        private static readonly string password = "232323";

        public static readonly string connectionString;
        public static readonly string FertigungConnectionString;

        static SqlManager()
        {
            connectionString = $"Data Source={server};Initial Catalog={database};User ID={username};Password={password}; TrustServerCertificate=True;";
            FertigungConnectionString = $"Data Source={server};Initial Catalog={fertigungdatabase};User ID={username};Password={password}; TrustServerCertificate=True;";
        }
    }
}
