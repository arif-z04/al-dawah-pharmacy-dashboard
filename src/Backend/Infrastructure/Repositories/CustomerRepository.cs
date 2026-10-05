using System.Data;
using AlDawahPharma.Application.DTOs;
using AlDawahPharma.Application.Exceptions;
using AlDawahPharma.Application.Interfaces;
using Oracle.ManagedDataAccess.Client;

namespace AlDawahPharma.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;

    public CustomerRepository(IOracleConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<CustomerDto>> GetAllAsync()
    {
        var list = new List<CustomerDto>();
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand(
            "SELECT CustomerID, CustomerName, Phone, Address FROM Customer ORDER BY CustomerID ASC", conn);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(MapCustomer(reader));
        }
        return list;
    }

    public async Task<CustomerDto?> GetByIdAsync(long customerId)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand(
            "SELECT CustomerID, CustomerName, Phone, Address FROM Customer WHERE CustomerID = :p_id", conn);
        cmd.Parameters.Add(new OracleParameter("p_id", customerId));

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapCustomer(reader);
        }
        return null;
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();

        long nextId;
        using (var idCmd = new OracleCommand("SELECT NVL(MAX(CustomerID), 0) + 1 FROM Customer", conn))
        {
            nextId = Convert.ToInt64(await idCmd.ExecuteScalarAsync());
        }

        using (var cmd = new OracleCommand(
            "INSERT INTO Customer (CustomerID, CustomerName, Phone, Address) " +
            "VALUES (:p_id, :p_name, :p_phone, :p_addr)", conn))
        {
            cmd.Parameters.Add(new OracleParameter("p_id", nextId));
            cmd.Parameters.Add(new OracleParameter("p_name", request.CustomerName.Trim()));
            cmd.Parameters.Add(new OracleParameter("p_phone", (object?)request.Phone ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("p_addr", (object?)request.Address ?? DBNull.Value));

            await cmd.ExecuteNonQueryAsync();
        }

        return (await GetByIdAsync(nextId))!;
    }

    public async Task<bool> UpdateAsync(long customerId, UpdateCustomerRequest request)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand(
            "UPDATE Customer SET CustomerName = :p_name, Phone = :p_phone, Address = :p_addr WHERE CustomerID = :p_id", conn);
        cmd.Parameters.Add(new OracleParameter("p_name", request.CustomerName.Trim()));
        cmd.Parameters.Add(new OracleParameter("p_phone", (object?)request.Phone ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("p_addr", (object?)request.Address ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("p_id", customerId));

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(long customerId)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand("DELETE FROM Customer WHERE CustomerID = :p_id", conn);
        cmd.Parameters.Add(new OracleParameter("p_id", customerId));

        try
        {
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
        catch (OracleException ex) when (ex.Number == 2292)
        {
            throw new BusinessRuleException("Cannot delete this customer because sales records are associated with them.");
        }
    }

    private static CustomerDto MapCustomer(IDataRecord reader)
    {
        return new CustomerDto
        {
            CustomerID = Convert.ToInt64(reader["CustomerID"]),
            CustomerName = reader["CustomerName"].ToString()!,
            Phone = reader["Phone"] == DBNull.Value ? null : reader["Phone"].ToString(),
            Address = reader["Address"] == DBNull.Value ? null : reader["Address"].ToString()
        };
    }
}
