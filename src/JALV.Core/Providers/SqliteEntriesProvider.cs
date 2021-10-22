using System.Data;
using System.Data.SQLite;

namespace JALV.Core.Providers
{
    public class SqliteEntriesProvider : AbstractEntriesProviderBase
    {
        protected override IDbConnection CreateConnection(string dataSource)
        {
            var sb = new SQLiteConnectionStringBuilder { DataSource = dataSource, FailIfMissing = true };
            var connectionString = sb.ConnectionString;
            return new SQLiteConnection(connectionString);
        }
    }
}