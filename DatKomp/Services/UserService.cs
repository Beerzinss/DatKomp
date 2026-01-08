using System.Security.Cryptography;
using System.Text;
using DatKomp.Models;
using Npgsql;

namespace DatKomp.Services;

public class UserService
{
    private readonly string _connectionString;

    private const int SaltSize = 16; // bytes
    private const int KeySize = 32;  // bytes
    private const int Iterations = 100_000;

    public UserService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
    }

    public async Task<AppUser?> GetByEmailAsync(string email)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"SELECT id, first_name, last_name, email, password_hash, is_admin FROM app_user WHERE email = @email";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@email", email);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new AppUser
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                LastName = reader.GetString(reader.GetOrdinal("last_name")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                IsAdmin = !reader.IsDBNull(reader.GetOrdinal("is_admin")) && reader.GetBoolean(reader.GetOrdinal("is_admin"))
            };
        }

        return null;
    }

    public async Task<AppUser?> GetByIdAsync(int id)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"SELECT id, first_name, last_name, email, password_hash, is_admin FROM app_user WHERE id = @id";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new AppUser
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                LastName = reader.GetString(reader.GetOrdinal("last_name")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                IsAdmin = !reader.IsDBNull(reader.GetOrdinal("is_admin")) && reader.GetBoolean(reader.GetOrdinal("is_admin"))
            };
        }

        return null;
    }

    public async Task<int> CreateUserAsync(string firstName, string lastName, string email, string password)
    {
        return await CreateUserAsync(firstName, lastName, email, password, isAdmin: false);
    }

    public async Task<int> CreateUserAsync(string firstName, string lastName, string email, string password, bool isAdmin)
    {
        var passwordHash = HashPassword(password);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"INSERT INTO app_user (first_name, last_name, email, password_hash, is_admin)
                     VALUES (@first_name, @last_name, @email, @password_hash, @is_admin)
                             RETURNING id";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@first_name", firstName);
        cmd.Parameters.AddWithValue("@last_name", lastName);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@password_hash", passwordHash);
        cmd.Parameters.AddWithValue("@is_admin", isAdmin);

        var id = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(id);
    }

    public async Task<List<AppUser>> GetAllUsersAsync()
    {
        var users = new List<AppUser>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"SELECT id, first_name, last_name, email, password_hash, is_admin
                             FROM app_user
                             ORDER BY id";

        await using var cmd = new NpgsqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            users.Add(new AppUser
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                LastName = reader.GetString(reader.GetOrdinal("last_name")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                IsAdmin = !reader.IsDBNull(reader.GetOrdinal("is_admin")) && reader.GetBoolean(reader.GetOrdinal("is_admin"))
            });
        }

        return users;
    }

    public async Task<int> CountAdminsAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"SELECT COUNT(*) FROM app_user WHERE is_admin = true";

        await using var cmd = new NpgsqlCommand(sql, connection);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<bool> UserHasOrdersAsync(int userId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"SELECT 1 FROM orders WHERE user_id = @user_id LIMIT 1";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@user_id", userId);

        var result = await cmd.ExecuteScalarAsync();
        return result != null && result != DBNull.Value;
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var tx = await connection.BeginTransactionAsync();

        const string deleteMessagesSql = @"DELETE FROM contact_message WHERE user_id = @user_id";
        await using (var deleteMessagesCmd = new NpgsqlCommand(deleteMessagesSql, connection, tx))
        {
            deleteMessagesCmd.Parameters.AddWithValue("@user_id", userId);
            await deleteMessagesCmd.ExecuteNonQueryAsync();
        }

        const string deleteUserSql = @"DELETE FROM app_user WHERE id = @user_id";
        int affected;
        await using (var deleteUserCmd = new NpgsqlCommand(deleteUserSql, connection, tx))
        {
            deleteUserCmd.Parameters.AddWithValue("@user_id", userId);
            affected = await deleteUserCmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
        return affected > 0;
    }

    public async Task<bool> UpdateUserAsync(AppUser user)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"UPDATE app_user
                             SET first_name = @first_name,
                                 last_name = @last_name,
                                 email = @email,
                                 is_admin = @is_admin
                             WHERE id = @id";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", user.Id);
        cmd.Parameters.AddWithValue("@first_name", user.FirstName);
        cmd.Parameters.AddWithValue("@last_name", user.LastName);
        cmd.Parameters.AddWithValue("@email", user.Email);
        cmd.Parameters.AddWithValue("@is_admin", user.IsAdmin);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<bool> UpdatePasswordAsync(int userId, string newPassword)
    {
        var passwordHash = HashPassword(newPassword);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"UPDATE app_user
                             SET password_hash = @password_hash
                             WHERE id = @id";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", userId);
        cmd.Parameters.AddWithValue("@password_hash", passwordHash);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.FromBase64String(parts[1]);

        var inputHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return CryptographicOperations.FixedTimeEquals(hash, inputHash);
    }

    private static string HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[SaltSize];
        rng.GetBytes(salt);

        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }
}
