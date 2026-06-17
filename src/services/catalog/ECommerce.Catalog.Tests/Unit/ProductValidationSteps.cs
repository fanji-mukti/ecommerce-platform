namespace ECommerce.Catalog.Tests.Unit;

/// <summary>
/// Steps for product pagination validation tests.
/// Extracts the clamping logic from ProductsEndpoints for unit testing.
/// </summary>
public class ProductValidationSteps
{
    private int _page;
    private int _pageSize;
    private int _clampedPage;
    private int _clampedPageSize;

    public void Given_QueryParams(int page, int pageSize)
    {
        _page = page;
        _pageSize = pageSize;
    }

    public void When_Validated()
    {
        // Mirror the clamping logic from ProductsEndpoints
        _clampedPage = _page < 1 ? 1 : _page;
        _clampedPageSize = (_pageSize < 1 || _pageSize > 100) ? 12 : _pageSize;
    }

    public int Then_PageIs() => _clampedPage;

    public int Then_PageSizeIs() => _clampedPageSize;
}
