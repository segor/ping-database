using System.Data.Common;
using Npgsql;

namespace PingDatabase
{
    sealed class PgSqlConnectionImpl : DatabaseConnection
    {
        public PgSqlConnectionImpl(string connectionString) : base(
            new NpgsqlConnectionStringBuilder(connectionString ?? string.Empty))
        {}

        public override string ServerType => "PostgreSQL Server";

        protected override DbConnection CreateConnection()
        {
            return  new NpgsqlConnection(_connectionStringBuilder.ConnectionString);
        }
    }
}