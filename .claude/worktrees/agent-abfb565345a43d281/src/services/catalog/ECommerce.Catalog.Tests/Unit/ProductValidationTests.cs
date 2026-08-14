using FluentAssertions;
using Xunit;

namespace ECommerce.Catalog.Tests.Unit;

public class ProductValidationTests
{
    private readonly ProductValidationSteps _steps = new();

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(-100, 1)]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    public void ValidatePage_WhenPageBelowOne_ClampsToOne(int inputPage, int expectedPage)
    {
        _steps.Given_QueryParams(inputPage, 12);
        _steps.When_Validated();
        _steps.Then_PageIs().Should().Be(expectedPage);
    }

    [Theory]
    [InlineData(0, 12)]
    [InlineData(-1, 12)]
    [InlineData(101, 12)]
    [InlineData(200, 12)]
    [InlineData(1, 1)]
    [InlineData(12, 12)]
    [InlineData(100, 100)]
    public void ValidatePageSize_WhenOutOfBounds_ClampsToDefault(int inputPageSize, int expectedPageSize)
    {
        _steps.Given_QueryParams(1, inputPageSize);
        _steps.When_Validated();
        _steps.Then_PageSizeIs().Should().Be(expectedPageSize);
    }

    [Fact]
    public void ValidatePage_WhenPageIsZero_ClampsToOne()
    {
        _steps.Given_QueryParams(page: 0, pageSize: 12);
        _steps.When_Validated();
        _steps.Then_PageIs().Should().Be(1);
    }

    [Fact]
    public void ValidatePageSize_WhenPageSizeExceedsHundred_ClampsToTwelve()
    {
        _steps.Given_QueryParams(page: 1, pageSize: 101);
        _steps.When_Validated();
        _steps.Then_PageSizeIs().Should().Be(12);
    }

    [Fact]
    public void ValidatePageSize_WhenPageSizeIsZero_ClampsToTwelve()
    {
        _steps.Given_QueryParams(page: 1, pageSize: 0);
        _steps.When_Validated();
        _steps.Then_PageSizeIs().Should().Be(12);
    }

    [Fact]
    public void ValidatePageSize_WhenPageSizeIsNegative_ClampsToTwelve()
    {
        _steps.Given_QueryParams(page: 1, pageSize: -5);
        _steps.When_Validated();
        _steps.Then_PageSizeIs().Should().Be(12);
    }
}
