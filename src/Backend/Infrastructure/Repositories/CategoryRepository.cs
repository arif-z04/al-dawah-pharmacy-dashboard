using System.Data;
using AlDawahPharma.Application.DTOs;
using AlDawahPharma.Application.Exceptions;
using AlDawahPharma.Application.Interfaces;
using Oracle.ManagedDataAccess.Client;

namespace AlDawahPharma.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;

    public CategoryRepository(IOracleConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        var list = new List<CategoryDto>();
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand(
            "SELECT c.CategoryID, c.CategoryName, COUNT(m.MedicineID) AS MedicineCount " +
            "FROM Category c " +
            "LEFT JOIN Medicine m ON c.CategoryID = m.CategoryID " +
            "GROUP BY c.CategoryID, c.CategoryName " +
            "ORDER BY c.CategoryID ASC", conn);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new CategoryDto
            {
                CategoryID = Convert.ToInt64(reader["CategoryID"]),
                CategoryName = reader["CategoryName"].ToString()!,
                MedicineCount = Convert.ToInt32(reader["MedicineCount"])
            });
        }
        return list;
    }

    public async Task<CategoryDto?> GetByIdAsync(long categoryId)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand(
            "SELECT c.CategoryID, c.CategoryName, COUNT(m.MedicineID) AS MedicineCount " +
            "FROM Category c " +
            "LEFT JOIN Medicine m ON c.CategoryID = m.CategoryID " +
            "WHERE c.CategoryID = :p_id " +
            "GROUP BY c.CategoryID, c.CategoryName", conn);
        cmd.Parameters.Add(new OracleParameter("p_id", categoryId));

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new CategoryDto
            {
                CategoryID = Convert.ToInt64(reader["CategoryID"]),
                CategoryName = reader["CategoryName"].ToString()!,
                MedicineCount = Convert.ToInt32(reader["MedicineCount"])
            };
        }
        return null;
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();

        // Generate next manual CategoryID
        long nextId;
        using (var idCmd = new OracleCommand("SELECT NVL(MAX(CategoryID), 0) + 1 FROM Category", conn))
        {
            nextId = Convert.ToInt64(await idCmd.ExecuteScalarAsync());
        }

        // Call Oracle Stored Procedure ADD_CATEGORY
        try
        {
            using var procCmd = new OracleCommand("ADD_CATEGORY", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            procCmd.Parameters.Add(new OracleParameter("p_categoryid", OracleDbType.Int64, nextId, ParameterDirection.Input));
            procCmd.Parameters.Add(new OracleParameter("p_categoryname", OracleDbType.Varchar2, request.CategoryName.Trim(), ParameterDirection.Input));

            await procCmd.ExecuteNonQueryAsync();
        }
        catch (OracleException ex)
        {
            if (ex.Number == 20010 || ex.Number == 20011 || ex.Number == 1)
                throw new BusinessRuleException(ex.Message.Split('\n')[0]);
            throw;
        }

        return (await GetByIdAsync(nextId))!;
    }

    public async Task<bool> UpdateAsync(long categoryId, string categoryName)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand(
            "UPDATE Category SET CategoryName = :p_name WHERE CategoryID = :p_id", conn);
        cmd.Parameters.Add(new OracleParameter("p_name", categoryName.Trim()));
        cmd.Parameters.Add(new OracleParameter("p_id", categoryId));

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(long categoryId)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand("DELETE FROM Category WHERE CategoryID = :p_id", conn);
        cmd.Parameters.Add(new OracleParameter("p_id", categoryId));

        try
        {
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
        catch (OracleException ex) when (ex.Number == 2292) // Integrity constraint violated - child record found
        {
            throw new BusinessRuleException("Cannot delete this category because medicines are assigned to it.");
        }
    }
}
