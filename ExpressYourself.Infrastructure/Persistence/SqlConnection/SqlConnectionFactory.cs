using ExpressYourself.Infrastructure.Configuration;
using ExpressYourself.Infrastructure.Persistence.Abstraction;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;


namespace ExpressYourself.Infrastructure.Persistence;

public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString(ConnectionStringNames.SqlServer) 
            ?? throw new InvalidOperationException("SQLServer connection string was not found");
    }

    public IDbConnection InitializeConnection() => new SqlConnection(_connectionString);
}
