using System.Data.Common;
using MySql.Data.MySqlClient;

namespace PingDatabase
{
    sealed class MysqlConnectionImpl : DatabaseConnection
    {
        public MysqlConnectionImpl(string connectionString) : base(
            new MySqlConnectionStringBuilder(connectionString ?? string.Empty))
        {}

        public override string ServerType => "MySQL Server";

        protected override DbConnection CreateConnection()
        {
            return  new MySqlConnection(_connectionStringBuilder.ConnectionString);
        }
    }
}