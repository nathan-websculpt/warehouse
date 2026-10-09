using Microsoft.Data.SqlClient;

namespace Warehouse.Api.Data;

public sealed class SqlConnectionFactory(string connectionString)
{
    public SqlConnection CreateConnection()
    {
        return new SqlConnection(connectionString);
    }
}