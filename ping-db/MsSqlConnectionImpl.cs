using System.Data.Common;
using System.Data.SqlClient;

namespace PingDatabase
{
    sealed class MsSqlConnectionImpl : DatabaseConnection
    {
        public MsSqlConnectionImpl(string connectionString) : base(
            new SqlConnectionStringBuilder(connectionString ?? string.Empty))
        {}

        public override string ServerType => "MS SQL Server";

        protected override DbConnection CreateConnection()
        {
            return  new SqlConnection(_connectionStringBuilder.ConnectionString);
        }
    }
}