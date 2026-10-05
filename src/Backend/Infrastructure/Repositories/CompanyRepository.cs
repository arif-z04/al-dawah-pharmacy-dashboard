using System.Data;
using AlDawahPharma.Application.DTOs;
using AlDawahPharma.Application.Exceptions;
using AlDawahPharma.Application.Interfaces;
using Oracle.ManagedDataAccess.Client;

namespace AlDawahPharma.Infrastructure.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;

    public CompanyRepository(IOracleConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<CompanyDto>> GetAllAsync()
    {
        var list = new List<CompanyDto>();
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand(
            "SELECT c.CompanyID, c.CompanyName, c.Address, c.Phone, c.Email, COUNT(m.MedicineID) AS MedicineCount " +
            "FROM Company c " +
            "LEFT JOIN Medicine m ON c.CompanyID = m.CompanyID " +
            "GROUP BY c.CompanyID, c.CompanyName, c.Address, c.Phone, c.Email " +
            "ORDER BY c.CompanyID ASC", conn);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(MapCompany(reader));
        }
        return list;
    }

    public async Task<CompanyDto?> GetByIdAsync(long companyId)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand(
            "SELECT c.CompanyID, c.CompanyName, c.Address, c.Phone, c.Email, COUNT(m.MedicineID) AS MedicineCount " +
            "FROM Company c " +
            "LEFT JOIN Medicine m ON c.CompanyID = m.CompanyID " +
            "WHERE c.CompanyID = :p_id " +
            "GROUP BY c.CompanyID, c.CompanyName, c.Address, c.Phone, c.Email", conn);
        cmd.Parameters.Add(new OracleParameter("p_id", companyId));

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapCompany(reader);
        }
        return null;
    }

    public async Task<CompanyDto> CreateAsync(CreateCompanyRequest request)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();

        // Check duplicate company name
        using (var checkCmd = new OracleCommand("SELECT COUNT(*) FROM Company WHERE LOWER(CompanyName) = LOWER(:p_name)", conn))
        {
            checkCmd.Parameters.Add(new OracleParameter("p_name", request.CompanyName.Trim()));
            var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
            if (count > 0)
                throw new BusinessRuleException($"Company '{request.CompanyName}' already exists.");
        }

        // Generate manual ID
        long nextId;
        using (var idCmd = new OracleCommand("SELECT NVL(MAX(CompanyID), 0) + 1 FROM Company", conn))
        {
            nextId = Convert.ToInt64(await idCmd.ExecuteScalarAsync());
        }

        using (var cmd = new OracleCommand(
            "INSERT INTO Company (CompanyID, CompanyName, Address, Phone, Email) " +
            "VALUES (:p_id, :p_name, :p_addr, :p_phone, :p_email)", conn))
        {
            cmd.Parameters.Add(new OracleParameter("p_id", nextId));
            cmd.Parameters.Add(new OracleParameter("p_name", request.CompanyName.Trim()));
            cmd.Parameters.Add(new OracleParameter("p_addr", (object?)request.Address ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("p_phone", (object?)request.Phone ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("p_email", (object?)request.Email ?? DBNull.Value));

            await cmd.ExecuteNonQueryAsync();
        }

        return (await GetByIdAsync(nextId))!;
    }

    public async Task<bool> UpdateAsync(long companyId, UpdateCompanyRequest request)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand(
            "UPDATE Company SET CompanyName = :p_name, Address = :p_addr, Phone = :p_phone, Email = :p_email WHERE CompanyID = :p_id", conn);
        cmd.Parameters.Add(new OracleParameter("p_name", request.CompanyName.Trim()));
        cmd.Parameters.Add(new OracleParameter("p_addr", (object?)request.Address ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("p_phone", (object?)request.Phone ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("p_email", (object?)request.Email ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("p_id", companyId));

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(long companyId)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand("DELETE FROM Company WHERE CompanyID = :p_id", conn);
        cmd.Parameters.Add(new OracleParameter("p_id", companyId));

        try
        {
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
        catch (OracleException ex) when (ex.Number == 2292)
        {
            throw new BusinessRuleException("Cannot delete this company because medicines are associated with it.");
        }
    }

    private static CompanyDto MapCompany(IDataRecord reader)
    {
        return new CompanyDto
        {
            CompanyID = Convert.ToInt64(reader["CompanyID"]),
            CompanyName = reader["CompanyName"].ToString()!,
            Address = reader["Address"] == DBNull.Value ? null : reader["Address"].ToString(),
            Phone = reader["Phone"] == DBNull.Value ? null : reader["Phone"].ToString(),
            Email = reader["Email"] == DBNull.Value ? null : reader["Email"].ToString(),
            MedicineCount = Convert.ToInt32(reader["MedicineCount"])
        };
    }
}
