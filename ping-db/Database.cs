using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace PingDatabase
{
    abstract class DatabaseConnection
    {
        protected readonly DbConnectionStringBuilder _connectionStringBuilder;

        protected DatabaseConnection(DbConnectionStringBuilder connectionStringBuilder)
        {
            _connectionStringBuilder = connectionStringBuilder;
        }

        public virtual bool ContainsOption(string option)
        {
            return _connectionStringBuilder.ContainsKey(option) 
                   && !string.IsNullOrEmpty(_connectionStringBuilder[option] as string);
        }

        public virtual void SetOption(string name, object value)
        {
            _connectionStringBuilder[name] = value;
        }

        public virtual string Server => (string) _connectionStringBuilder["Server"];
        public virtual string Database => (string)_connectionStringBuilder["Database"];
        public abstract string ServerType { get; }

        public async Task<object> Ping(string commandText, string payload, CancellationToken ctsToken)
        {
            using (DbConnection conn = CreateConnection())
            {
                using (var command = CreateCommand(conn, commandText, payload))
                {
                    await conn.OpenAsync(ctsToken);
                    return await command.ExecuteScalarAsync(ctsToken);
                }
            }
        }

        public async Task<DatabaseConnection>  CheckConnection(CancellationToken ctsToken)
        {
            using (DbConnection conn = CreateConnection())
            {
                await conn.OpenAsync(ctsToken);
                return this;
            }
        }

        private static DbCommand CreateCommand(DbConnection connection, string commandText, string payload)
        {
            var command = connection.CreateCommand();
            command.CommandText = commandText;
            var param = command.CreateParameter();
            param.ParameterName = "@payload";
            param.DbType = DbType.String;
            param.Value = payload;
            command.Parameters.Add(param);
            return command;
        }

        protected abstract DbConnection CreateConnection();
    }
}
