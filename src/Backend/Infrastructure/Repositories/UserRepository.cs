using System.Data;
using AlDawahPharma.Application.DTOs;
using AlDawahPharma.Application.Exceptions;
using AlDawahPharma.Application.Interfaces;
using AlDawahPharma.Domain.Entities;
using Oracle.ManagedDataAccess.Client;

namespace AlDawahPharma.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly IPasswordHasher _passwordHasher;

    public UserRepository(IOracleConnectionFactory connectionFactory, IPasswordHasher passwordHasher)
    {
        _connectionFactory = connectionFactory;
        _passwordHasher = passwordHasher;
    }

    public async Task<User?> GetByIdAsync(long userId)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand(
            "SELECT UserID, FullName, Username, Password, Role, Phone, Email, CreatedAt FROM Users WHERE UserID = :p_id", conn);
        cmd.Parameters.Add(new OracleParameter("p_id", userId));

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapUser(reader);
        }
        return null;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand(
            "SELECT UserID, FullName, Username, Password, Role, Phone, Email, CreatedAt FROM Users WHERE LOWER(Username) = LOWER(:p_uname)", conn);
        cmd.Parameters.Add(new OracleParameter("p_uname", username.Trim()));

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapUser(reader);
        }
        return null;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        var list = new List<User>();
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand(
            "SELECT UserID, FullName, Username, Password, Role, Phone, Email, CreatedAt FROM Users ORDER BY UserID ASC", conn);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(MapUser(reader));
        }
        return list;
    }

    public async Task<User> CreateAsync(CreateUserRequest request)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();

        // Check duplicate username
        using (var checkCmd = new OracleCommand("SELECT COUNT(*) FROM Users WHERE LOWER(Username) = LOWER(:p_uname)", conn))
        {
            checkCmd.Parameters.Add(new OracleParameter("p_uname", request.Username.Trim()));
            var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
            if (count > 0)
                throw new BusinessRuleException($"Username '{request.Username}' is already taken.");
        }

        // Generate manual ID respecting DB strategy
        long nextId;
        using (var idCmd = new OracleCommand("SELECT NVL(MAX(UserID), 0) + 1 FROM Users", conn))
        {
            nextId = Convert.ToInt64(await idCmd.ExecuteScalarAsync());
        }

        var hashedPassword = _passwordHasher.HashPassword(request.Password);

        using (var cmd = new OracleCommand(
            "INSERT INTO Users (UserID, FullName, Username, Password, Role, Phone, Email, CreatedAt) " +
            "VALUES (:p_id, :p_fname, :p_uname, :p_pwd, :p_role, :p_phone, :p_email, SYSDATE)", conn))
        {
            cmd.Parameters.Add(new OracleParameter("p_id", nextId));
            cmd.Parameters.Add(new OracleParameter("p_fname", request.FullName.Trim()));
            cmd.Parameters.Add(new OracleParameter("p_uname", request.Username.Trim()));
            cmd.Parameters.Add(new OracleParameter("p_pwd", hashedPassword));
            cmd.Parameters.Add(new OracleParameter("p_role", request.Role));
            cmd.Parameters.Add(new OracleParameter("p_phone", (object?)request.Phone ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("p_email", (object?)request.Email ?? DBNull.Value));

            await cmd.ExecuteNonQueryAsync();
        }

        return (await GetByIdAsync(nextId))!;
    }

    public async Task<bool> UpdateAsync(long userId, UpdateUserRequest request)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand(
            "UPDATE Users SET FullName = :p_fname, Role = :p_role, Phone = :p_phone, Email = :p_email WHERE UserID = :p_id", conn);
        cmd.Parameters.Add(new OracleParameter("p_fname", request.FullName.Trim()));
        cmd.Parameters.Add(new OracleParameter("p_role", request.Role));
        cmd.Parameters.Add(new OracleParameter("p_phone", (object?)request.Phone ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("p_email", (object?)request.Email ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("p_id", userId));

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<bool> ChangePasswordAsync(long userId, string hashedPassword)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand("UPDATE Users SET Password = :p_pwd WHERE UserID = :p_id", conn);
        cmd.Parameters.Add(new OracleParameter("p_pwd", hashedPassword));
        cmd.Parameters.Add(new OracleParameter("p_id", userId));

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(long userId)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand("DELETE FROM Users WHERE UserID = :p_id", conn);
        cmd.Parameters.Add(new OracleParameter("p_id", userId));

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    private static User MapUser(IDataRecord record)
    {
        return new User
        {
            UserID = Convert.ToInt64(record["UserID"]),
            FullName = record["FullName"].ToString()!,
            Username = record["Username"].ToString()!,
            Password = record["Password"].ToString()!,
            Role = record["Role"].ToString()!,
            Phone = record["Phone"] == DBNull.Value ? null : record["Phone"].ToString(),
            Email = record["Email"] == DBNull.Value ? null : record["Email"].ToString(),
            CreatedAt = Convert.ToDateTime(record["CreatedAt"])
        };
    }
}
