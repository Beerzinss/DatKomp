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

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        var categories = new List<Category>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"SELECT id, name FROM category ORDER BY name";

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            categories.Add(new Category
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name"))
            });
        }

        return categories;
    }

    public async Task<List<Product>> GetProductsByCategoryAsync(string categoryName)
    {
        var products = new List<Product>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"SELECT p.id, p.name, p.description, p.price, p.stock_qty, p.image_url
                             FROM product p
                             JOIN product_category pc ON pc.product_id = p.id
                             JOIN category c ON c.id = pc.category_id
                             WHERE c.name = @categoryName
                             ORDER BY p.id";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@categoryName", categoryName);

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

    public async Task<List<int>> GetCategoryIdsForProductAsync(int productId)
    {
        var ids = new List<int>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"SELECT category_id FROM product_category WHERE product_id = @product_id";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@product_id", productId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            ids.Add(reader.GetInt32(reader.GetOrdinal("category_id")));
        }

        return ids;
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

    public async Task<Dictionary<int, List<ProductSpec>>> GetSpecsForProductsAsync(IEnumerable<int> productIds)
    {
        var ids = productIds.Distinct().ToArray();
        var result = new Dictionary<int, List<ProductSpec>>();

        if (ids.Length == 0)
        {
            return result;
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"SELECT id, product_id, key, value, unit
                             FROM product_spec
                             WHERE product_id = ANY(@ids)
                             ORDER BY product_id, id";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ids", ids);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var productId = reader.GetInt32(reader.GetOrdinal("product_id"));

            if (!result.TryGetValue(productId, out var list))
            {
                list = new List<ProductSpec>();
                result[productId] = list;
            }

            list.Add(new ProductSpec
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                ProductId = productId,
                SpecName = reader.GetString(reader.GetOrdinal("key")),
                SpecValue = reader.GetString(reader.GetOrdinal("value")),
                Unit = reader.IsDBNull(reader.GetOrdinal("unit"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("unit"))
            });
        }

        return result;
    }

    public async Task<int> CreateProductAsync(Product product)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"INSERT INTO product (name, description, price, stock_qty, image_url)
                             VALUES (@name, @description, @price, @stock_qty, @image_url)
                             RETURNING id";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@name", product.Name);
        command.Parameters.AddWithValue("@description", (object?)product.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@price", product.Price);
        command.Parameters.AddWithValue("@stock_qty", product.StockQty);
        command.Parameters.AddWithValue("@image_url", (object?)product.ImageUrl ?? DBNull.Value);

        var id = await command.ExecuteScalarAsync();
        return Convert.ToInt32(id);
    }

    public async Task UpdateProductAsync(Product product)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"UPDATE product
                             SET name = @name,
                                 description = @description,
                                 price = @price,
                                 stock_qty = @stock_qty,
                                 image_url = @image_url
                             WHERE id = @id";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", product.Id);
        command.Parameters.AddWithValue("@name", product.Name);
        command.Parameters.AddWithValue("@description", (object?)product.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@price", product.Price);
        command.Parameters.AddWithValue("@stock_qty", product.StockQty);
        command.Parameters.AddWithValue("@image_url", (object?)product.ImageUrl ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteProductAsync(int id)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"DELETE FROM product WHERE id = @id";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        await command.ExecuteNonQueryAsync();
    }

    public async Task ReplaceProductCategoriesAsync(int productId, IEnumerable<int> categoryIds)
    {
        var ids = categoryIds.Distinct().ToList();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var tx = await connection.BeginTransactionAsync();

        const string deleteSql = @"DELETE FROM product_category WHERE product_id = @product_id";
        await using (var deleteCmd = new NpgsqlCommand(deleteSql, connection, tx))
        {
            deleteCmd.Parameters.AddWithValue("@product_id", productId);
            await deleteCmd.ExecuteNonQueryAsync();
        }

        const string insertSql = @"INSERT INTO product_category (product_id, category_id) VALUES (@product_id, @category_id)";

        foreach (var cid in ids)
        {
            await using var insertCmd = new NpgsqlCommand(insertSql, connection, tx);
            insertCmd.Parameters.AddWithValue("@product_id", productId);
            insertCmd.Parameters.AddWithValue("@category_id", cid);
            await insertCmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
    }

    public async Task ReplaceProductSpecsAsync(int productId, IEnumerable<ProductSpec> specs)
    {
        var validSpecs = specs
            .Where(s => !string.IsNullOrWhiteSpace(s.SpecName) && !string.IsNullOrWhiteSpace(s.SpecValue))
            .ToList();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var tx = await connection.BeginTransactionAsync();

        const string deleteSql = @"DELETE FROM product_spec WHERE product_id = @product_id";
        await using (var deleteCmd = new NpgsqlCommand(deleteSql, connection, tx))
        {
            deleteCmd.Parameters.AddWithValue("@product_id", productId);
            await deleteCmd.ExecuteNonQueryAsync();
        }

        const string insertSql = @"INSERT INTO product_spec (product_id, key, value, unit)
                             VALUES (@product_id, @key, @value, @unit)";

        foreach (var spec in validSpecs)
        {
            var key = spec.SpecName!;
            var value = spec.SpecValue!;

            await using var insertCmd = new NpgsqlCommand(insertSql, connection, tx);
            insertCmd.Parameters.AddWithValue("@product_id", productId);
            insertCmd.Parameters.AddWithValue("@key", key);
            insertCmd.Parameters.AddWithValue("@value", value);
            insertCmd.Parameters.AddWithValue("@unit", (object?)spec.Unit ?? DBNull.Value);
            await insertCmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
    }
}
