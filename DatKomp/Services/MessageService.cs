using DatKomp.Models;
using Npgsql;

namespace DatKomp.Services;

public class MessageService
{
    private readonly string _connectionString;

    public MessageService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
    }

    public async Task CreateMessageAsync(ContactMessage message)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"INSERT INTO contact_message (user_id, email, content, created_at_utc, is_read)
                             VALUES (@user_id, @email, @content, @created_at_utc, @is_read)";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@user_id", message.UserId);
        cmd.Parameters.AddWithValue("@email", (object?)message.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@content", message.Content);
        cmd.Parameters.AddWithValue("@created_at_utc", message.CreatedAtUtc);
        cmd.Parameters.AddWithValue("@is_read", message.IsRead);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<ContactMessage>> GetAllMessagesAsync()
    {
        var list = new List<ContactMessage>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"SELECT id, user_id, email, content, created_at_utc, is_read
                             FROM contact_message
                             ORDER BY created_at_utc DESC";

        await using var cmd = new NpgsqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new ContactMessage
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                Email = reader.IsDBNull(reader.GetOrdinal("email"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("email")),
                Content = reader.GetString(reader.GetOrdinal("content")),
                CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("created_at_utc")),
                IsRead = reader.GetBoolean(reader.GetOrdinal("is_read"))
            });
        }

        return list;
    }
}
