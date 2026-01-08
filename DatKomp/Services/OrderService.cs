using DatKomp.Models;
using Npgsql;

namespace DatKomp.Services;

public class OrderService
{
    private readonly string _connectionString;

    public OrderService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
    }

    public async Task<List<DeliveryType>> GetActiveDeliveryTypesAsync()
    {
        var list = new List<DeliveryType>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"SELECT id, name, description, price, is_active FROM delivery_type WHERE is_active = true ORDER BY price";

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new DeliveryType
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Description = reader.IsDBNull(reader.GetOrdinal("description"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("description")),
                Price = reader.GetDecimal(reader.GetOrdinal("price")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active"))
            });
        }

        return list;
    }

    public async Task<List<DeliveryType>> GetAllDeliveryTypesAsync()
    {
        var list = new List<DeliveryType>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"SELECT id, name, description, price, is_active
                             FROM delivery_type
                             ORDER BY id";

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new DeliveryType
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Description = reader.IsDBNull(reader.GetOrdinal("description"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("description")),
                Price = reader.GetDecimal(reader.GetOrdinal("price")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active"))
            });
        }

        return list;
    }

    public async Task<List<UserOrderSummary>> GetOrdersForUserAsync(int userId)
    {
        var ordersById = new Dictionary<int, UserOrderSummary>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"SELECT 
                                     o.id,
                                     o.created_at_utc,
                                     o.items_total,
                                     o.delivery_price,
                                     o.grand_total,
                                     s.name AS status_name,
                                     oi.qty,
                                     oi.unit_price,
                                     oi.line_total,
                                     p.name AS product_name
                              FROM orders o
                              JOIN order_status s ON s.id = o.order_status_id
                              LEFT JOIN order_item oi ON oi.order_id = o.id
                              LEFT JOIN product p ON p.id = oi.product_id
                              WHERE o.user_id = @user_id
                              ORDER BY o.created_at_utc DESC, o.id";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@user_id", userId);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var orderId = reader.GetInt32(reader.GetOrdinal("id"));

            if (!ordersById.TryGetValue(orderId, out var order))
            {
                order = new UserOrderSummary
                {
                    Id = orderId,
                    CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("created_at_utc")),
                    ItemsTotal = reader.GetDecimal(reader.GetOrdinal("items_total")),
                    DeliveryPrice = reader.GetDecimal(reader.GetOrdinal("delivery_price")),
                    GrandTotal = reader.GetDecimal(reader.GetOrdinal("grand_total")),
                    StatusName = reader.GetString(reader.GetOrdinal("status_name"))
                };

                ordersById[orderId] = order;
            }

            // Some orders might (theoretically) have no items; guard against nulls from LEFT JOIN
            var productNameOrdinal = reader.GetOrdinal("product_name");
            if (!reader.IsDBNull(productNameOrdinal))
            {
                var qtyOrdinal = reader.GetOrdinal("qty");
                var unitPriceOrdinal = reader.GetOrdinal("unit_price");
                var lineTotalOrdinal = reader.GetOrdinal("line_total");

                var item = new OrderItemSummary
                {
                    ProductName = reader.GetString(productNameOrdinal),
                    Quantity = reader.GetInt32(qtyOrdinal),
                    UnitPrice = reader.GetDecimal(unitPriceOrdinal),
                    LineTotal = reader.GetDecimal(lineTotalOrdinal)
                };

                order.Items.Add(item);
            }
        }

        return ordersById.Values
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToList();
    }

    public async Task<List<AdminOrderSummary>> GetAllOrdersAsync()
    {
        var orders = new List<AdminOrderSummary>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"SELECT o.id,
                                     o.created_at_utc,
                                     o.first_name,
                                     o.last_name,
                                     o.email,
                                     o.items_total,
                                     o.delivery_price,
                                     o.grand_total,
                                     s.id AS status_id,
                                     s.name AS status_name
                              FROM orders o
                              JOIN order_status s ON s.id = o.order_status_id
                              ORDER BY o.created_at_utc DESC";

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            orders.Add(new AdminOrderSummary
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("created_at_utc")),
                CustomerName = $"{reader.GetString(reader.GetOrdinal("first_name"))} {reader.GetString(reader.GetOrdinal("last_name"))}",
                Email = reader.GetString(reader.GetOrdinal("email")),
                ItemsTotal = reader.GetDecimal(reader.GetOrdinal("items_total")),
                DeliveryPrice = reader.GetDecimal(reader.GetOrdinal("delivery_price")),
                GrandTotal = reader.GetDecimal(reader.GetOrdinal("grand_total")),
                StatusId = reader.GetInt32(reader.GetOrdinal("status_id")),
                StatusName = reader.GetString(reader.GetOrdinal("status_name"))
            });
        }

        return orders;
    }

    public async Task<OrderDetailsAdmin?> GetOrderDetailsAsync(int orderId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"SELECT o.id,
                                     o.created_at_utc,
                                     o.first_name,
                                     o.last_name,
                                     o.address_line,
                                     o.phone,
                                     o.email,
                                     o.items_total,
                                     o.delivery_price,
                                     o.grand_total,
                                     o.order_status_id,
                                     s.name AS status_name,
                                     dt.name AS delivery_name,
                                     oi.qty,
                                     oi.unit_price,
                                     oi.line_total,
                                     p.name AS product_name
                              FROM orders o
                              JOIN order_status s ON s.id = o.order_status_id
                              JOIN delivery_type dt ON dt.id = o.delivery_type_id
                              LEFT JOIN order_item oi ON oi.order_id = o.id
                              LEFT JOIN product p ON p.id = oi.product_id
                              WHERE o.id = @order_id";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@order_id", orderId);

        await using var reader = await command.ExecuteReaderAsync();

        OrderDetailsAdmin? order = null;

        while (await reader.ReadAsync())
        {
            if (order == null)
            {
                order = new OrderDetailsAdmin
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("created_at_utc")),
                    FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                    LastName = reader.GetString(reader.GetOrdinal("last_name")),
                    AddressLine = reader.GetString(reader.GetOrdinal("address_line")),
                    Phone = reader.GetString(reader.GetOrdinal("phone")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    ItemsTotal = reader.GetDecimal(reader.GetOrdinal("items_total")),
                    DeliveryPrice = reader.GetDecimal(reader.GetOrdinal("delivery_price")),
                    GrandTotal = reader.GetDecimal(reader.GetOrdinal("grand_total")),
                    StatusId = reader.GetInt32(reader.GetOrdinal("order_status_id")),
                    StatusName = reader.GetString(reader.GetOrdinal("status_name")),
                    DeliveryTypeName = reader.GetString(reader.GetOrdinal("delivery_name"))
                };
            }

            var productNameOrdinal = reader.GetOrdinal("product_name");
            if (!reader.IsDBNull(productNameOrdinal))
            {
                var item = new OrderItemSummary
                {
                    ProductName = reader.GetString(productNameOrdinal),
                    Quantity = reader.GetInt32(reader.GetOrdinal("qty")),
                    UnitPrice = reader.GetDecimal(reader.GetOrdinal("unit_price")),
                    LineTotal = reader.GetDecimal(reader.GetOrdinal("line_total"))
                };

                order!.Items.Add(item);
            }
        }

        return order;
    }

    public async Task<List<OrderStatusModel>> GetAllOrderStatusesAsync()
    {
        var list = new List<OrderStatusModel>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"SELECT id, name FROM order_status ORDER BY id";

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new OrderStatusModel
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name"))
            });
        }

        return list;
    }

    public async Task UpdateOrderStatusAsync(int orderId, int statusId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"UPDATE orders SET order_status_id = @status_id WHERE id = @order_id";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@order_id", orderId);
        command.Parameters.AddWithValue("@status_id", statusId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<int> CreateDeliveryTypeAsync(DeliveryType type)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"INSERT INTO delivery_type (name, description, price, is_active)
                             VALUES (@name, @description, @price, @is_active)
                             RETURNING id";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@name", type.Name);
        command.Parameters.AddWithValue("@description", (object?)type.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@price", type.Price);
        command.Parameters.AddWithValue("@is_active", type.IsActive);

        var id = await command.ExecuteScalarAsync();
        return Convert.ToInt32(id);
    }

    public async Task UpdateDeliveryTypeAsync(DeliveryType type)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"UPDATE delivery_type
                             SET name = @name,
                                 description = @description,
                                 price = @price,
                                 is_active = @is_active
                             WHERE id = @id";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", type.Id);
        command.Parameters.AddWithValue("@name", type.Name);
        command.Parameters.AddWithValue("@description", (object?)type.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@price", type.Price);
        command.Parameters.AddWithValue("@is_active", type.IsActive);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> DeliveryTypeHasOrdersAsync(int deliveryTypeId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"SELECT 1 FROM orders WHERE delivery_type_id = @delivery_type_id LIMIT 1";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@delivery_type_id", deliveryTypeId);

        var result = await cmd.ExecuteScalarAsync();
        return result != null && result != DBNull.Value;
    }

    public async Task<bool> DeleteDeliveryTypeAsync(int deliveryTypeId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"DELETE FROM delivery_type WHERE id = @id";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", deliveryTypeId);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<int> CreateOrderAsync(CheckoutViewModel model, List<CartItem> cartItems, int userId, int defaultOrderStatusId)
    {
        if (cartItems.Count == 0)
        {
            throw new InvalidOperationException("Cannot create order with empty cart.");
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // Get delivery type to know price
            const string deliverySql = @"SELECT price FROM delivery_type WHERE id = @id";
            decimal deliveryPrice;

            await using (var deliveryCmd = new NpgsqlCommand(deliverySql, connection, transaction))
            {
                deliveryCmd.Parameters.AddWithValue("@id", model.DeliveryTypeId);
                var result = await deliveryCmd.ExecuteScalarAsync();
                if (result == null)
                {
                    throw new InvalidOperationException("Invalid delivery type.");
                }

                deliveryPrice = (decimal)result;
            }

            var itemsTotal = cartItems.Sum(i => i.Price * i.Quantity);
            var grandTotal = itemsTotal + deliveryPrice;

            // Insert into orders
            const string orderSql = @"INSERT INTO orders
                    (created_at_utc, user_id, first_name, last_name, address_line, phone, email,
                     delivery_type_id, order_status_id, items_total, delivery_price, grand_total)
                    VALUES (@created_at_utc, @user_id, @first_name, @last_name, @address_line, @phone, @email,
                            @delivery_type_id, @order_status_id, @items_total, @delivery_price, @grand_total)
                    RETURNING id";

            int orderId;
            await using (var orderCmd = new NpgsqlCommand(orderSql, connection, transaction))
            {
                orderCmd.Parameters.AddWithValue("@created_at_utc", DateTime.UtcNow);
                orderCmd.Parameters.AddWithValue("@user_id", userId);
                orderCmd.Parameters.AddWithValue("@first_name", model.FirstName);
                orderCmd.Parameters.AddWithValue("@last_name", model.LastName);
                orderCmd.Parameters.AddWithValue("@address_line", model.AddressLine);
                orderCmd.Parameters.AddWithValue("@phone", model.Phone);
                orderCmd.Parameters.AddWithValue("@email", model.Email);
                orderCmd.Parameters.AddWithValue("@delivery_type_id", model.DeliveryTypeId);
                orderCmd.Parameters.AddWithValue("@order_status_id", defaultOrderStatusId);
                orderCmd.Parameters.AddWithValue("@items_total", itemsTotal);
                orderCmd.Parameters.AddWithValue("@delivery_price", deliveryPrice);
                orderCmd.Parameters.AddWithValue("@grand_total", grandTotal);

                orderId = Convert.ToInt32(await orderCmd.ExecuteScalarAsync());
            }

            // Insert order items and decrease product stock
            const string itemSql = @"INSERT INTO order_item (order_id, product_id, qty, unit_price, line_total)
                                     VALUES (@order_id, @product_id, @qty, @unit_price, @line_total)";

            const string stockSql = @"UPDATE product
                                       SET stock_qty = stock_qty - @qty
                                       WHERE id = @product_id";

            foreach (var item in cartItems)
            {
                // order item
                await using (var itemCmd = new NpgsqlCommand(itemSql, connection, transaction))
                {
                    itemCmd.Parameters.AddWithValue("@order_id", orderId);
                    itemCmd.Parameters.AddWithValue("@product_id", item.ProductId);
                    itemCmd.Parameters.AddWithValue("@qty", item.Quantity);
                    itemCmd.Parameters.AddWithValue("@unit_price", item.Price);
                    itemCmd.Parameters.AddWithValue("@line_total", item.Price * item.Quantity);

                    await itemCmd.ExecuteNonQueryAsync();
                }

                // stock update
                await using (var stockCmd = new NpgsqlCommand(stockSql, connection, transaction))
                {
                    stockCmd.Parameters.AddWithValue("@product_id", item.ProductId);
                    stockCmd.Parameters.AddWithValue("@qty", item.Quantity);
                    await stockCmd.ExecuteNonQueryAsync();
                }
            }

            await transaction.CommitAsync();

            return orderId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
