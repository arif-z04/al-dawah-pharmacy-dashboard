using System.Data;
using AlDawahPharma.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace AlDawahPharma.Infrastructure.Data;

public class OracleConnectionFactory : IOracleConnectionFactory
{
    private readonly string _connectionString;

    public OracleConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("OracleDb")
            ?? Environment.GetEnvironmentVariable("ORACLE_CONNECTION_STRING")
            ?? "User Id=C##PHARMACY_APP;Password=PharmacyApp2026#;Data Source=localhost:1521/FREE;";
    }

    public IDbConnection CreateConnection()
    {
        return new OracleConnection(_connectionString);
    }

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
