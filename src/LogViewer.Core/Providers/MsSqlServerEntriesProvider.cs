using System.Data;
using System.Data.SqlClient;

namespace LogViewer.Core.Providers
{
    public class MsSqlServerEntriesProvider : AbstractEntriesProviderBase
    {
        protected override IDbConnection CreateConnection(string dataSource)
        {
            return new SqlConnection(dataSource);
        }
    }
}