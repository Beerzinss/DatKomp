using DatKomp.Models;
using Npgsql;

namespace DatKomp.Services;

public class ProductService
{
    private readonly string _connectionString;

    public ProductService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
    }

    public async Task<List<Product>> GetAllProductsAsync()
    {
        var products = new List<Product>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"SELECT id, name, description, price, stock_qty, image_url FROM product ORDER BY id";

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var product = new Product
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Description = reader.IsDBNull(reader.GetOrdinal("description"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("description")),
                Price = reader.GetDecimal(reader.GetOrdinal("price")),
                StockQty = reader.GetInt32(reader.GetOrdinal("stock_qty")),
                ImageUrl = reader.IsDBNull(reader.GetOrdinal("image_url"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("image_url"))
            };

            products.Add(product);
        }

        return products;
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"SELECT id, name, description, price, stock_qty, image_url FROM product WHERE id = @id";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var product = new Product
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Description = reader.IsDBNull(reader.GetOrdinal("description"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("description")),
                Price = reader.GetDecimal(reader.GetOrdinal("price")),
                StockQty = reader.GetInt32(reader.GetOrdinal("stock_qty")),
                ImageUrl = reader.IsDBNull(reader.GetOrdinal("image_url"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("image_url"))
            };

            return product;
        }

        return null;
    }

    public async Task<List<ProductSpec>> GetProductSpecsByProductIdAsync(int productId)
    {
        var specs = new List<ProductSpec>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"SELECT id, product_id, key, value, unit FROM product_spec WHERE product_id = @productId ORDER BY id";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@productId", productId);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var spec = new ProductSpec
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                ProductId = reader.GetInt32(reader.GetOrdinal("product_id")),
                SpecName = reader.GetString(reader.GetOrdinal("key")),
                SpecValue = reader.GetString(reader.GetOrdinal("value")),
                Unit = reader.IsDBNull(reader.GetOrdinal("unit"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("unit"))
            };

            specs.Add(spec);
        }

        return specs;
    }
}
