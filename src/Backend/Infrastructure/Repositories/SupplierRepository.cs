using System.Data;
using AlDawahPharma.Application.DTOs;
using AlDawahPharma.Application.Exceptions;
using AlDawahPharma.Application.Interfaces;
using Oracle.ManagedDataAccess.Client;

namespace AlDawahPharma.Infrastructure.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;

    public SupplierRepository(IOracleConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<SupplierDto>> GetAllAsync()
    {
        var list = new List<SupplierDto>();
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand(
            "SELECT SupplierID, SupplierName, ContactPerson, Phone, Email, Address, CreatedAt FROM Supplier ORDER BY SupplierID ASC", conn);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(MapSupplier(reader));
        }
        return list;
    }

    public async Task<SupplierDto?> GetByIdAsync(long supplierId)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand(
            "SELECT SupplierID, SupplierName, ContactPerson, Phone, Email, Address, CreatedAt FROM Supplier WHERE SupplierID = :p_id", conn);
        cmd.Parameters.Add(new OracleParameter("p_id", supplierId));

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapSupplier(reader);
        }
        return null;
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierRequest request)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();

        long nextId;
        using (var idCmd = new OracleCommand("SELECT NVL(MAX(SupplierID), 0) + 1 FROM Supplier", conn))
        {
            nextId = Convert.ToInt64(await idCmd.ExecuteScalarAsync());
        }

        using (var cmd = new OracleCommand(
            "INSERT INTO Supplier (SupplierID, SupplierName, ContactPerson, Phone, Email, Address, CreatedAt) " +
            "VALUES (:p_id, :p_name, :p_cp, :p_phone, :p_email, :p_addr, SYSDATE)", conn))
        {
            cmd.Parameters.Add(new OracleParameter("p_id", nextId));
            cmd.Parameters.Add(new OracleParameter("p_name", request.SupplierName.Trim()));
            cmd.Parameters.Add(new OracleParameter("p_cp", (object?)request.ContactPerson ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("p_phone", (object?)request.Phone ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("p_email", (object?)request.Email ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("p_addr", (object?)request.Address ?? DBNull.Value));

            await cmd.ExecuteNonQueryAsync();
        }

        return (await GetByIdAsync(nextId))!;
    }

    public async Task<bool> UpdateAsync(long supplierId, UpdateSupplierRequest request)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand(
            "UPDATE Supplier SET SupplierName = :p_name, ContactPerson = :p_cp, Phone = :p_phone, Email = :p_email, Address = :p_addr " +
            "WHERE SupplierID = :p_id", conn);
        cmd.Parameters.Add(new OracleParameter("p_name", request.SupplierName.Trim()));
        cmd.Parameters.Add(new OracleParameter("p_cp", (object?)request.ContactPerson ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("p_phone", (object?)request.Phone ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("p_email", (object?)request.Email ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("p_addr", (object?)request.Address ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("p_id", supplierId));

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(long supplierId)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand("DELETE FROM Supplier WHERE SupplierID = :p_id", conn);
        cmd.Parameters.Add(new OracleParameter("p_id", supplierId));

        try
        {
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
        catch (OracleException ex) when (ex.Number == 2292)
        {
            throw new BusinessRuleException("Cannot delete this supplier because medicines or purchases are linked to it.");
        }
    }

    private static SupplierDto MapSupplier(IDataRecord reader)
    {
        return new SupplierDto
        {
            SupplierID = Convert.ToInt64(reader["SupplierID"]),
            SupplierName = reader["SupplierName"].ToString()!,
            ContactPerson = reader["ContactPerson"] == DBNull.Value ? null : reader["ContactPerson"].ToString(),
            Phone = reader["Phone"] == DBNull.Value ? null : reader["Phone"].ToString(),
            Email = reader["Email"] == DBNull.Value ? null : reader["Email"].ToString(),
            Address = reader["Address"] == DBNull.Value ? null : reader["Address"].ToString(),
            CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
        };
    }
}
