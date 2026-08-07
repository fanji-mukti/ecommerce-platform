namespace ECommerce.Tests.Common.Builders;

/// <summary>
/// Portable order read-model data used in test builders.
/// Service integration tests map this to the service's OrderReadModel entity by matching
/// property names (mirrors ProductBuilder's pattern).
/// </summary>
public record OrderReadModelData(
    Guid Id,
    string UserId,
    string Status,
    decimal TotalAmount,
    DateTimeOffset CreatedAt);

/// <summary>
/// Fluent test data builder for order read-model data.
/// </summary>
public class OrderBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _userId = "11111111-1111-1111-1111-111111111111";
    private string _status = "Pending";
    private decimal _totalAmount = 19.98m;
    private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;

    public OrderBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public OrderBuilder WithUserId(string userId)
    {
        _userId = userId;
        return this;
    }

    public OrderBuilder WithStatus(string status)
    {
        _status = status;
        return this;
    }

    public OrderBuilder WithTotalAmount(decimal totalAmount)
    {
        _totalAmount = totalAmount;
        return this;
    }

    public OrderBuilder WithCreatedAt(DateTimeOffset createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public OrderReadModelData Build() => new(
        Id: _id,
        UserId: _userId,
        Status: _status,
        TotalAmount: _totalAmount,
        CreatedAt: _createdAt);
}
