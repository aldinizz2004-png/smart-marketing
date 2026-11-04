using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Common.Pagination;

namespace SmartMarketingPlatform.UnitTests.Pagination;

public class PagedResultTests
{
    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(95, 10, 10)]
    public void Calculates_total_pages(int totalCount, int pageSize, int expectedPages)
    {
        var result = new PagedResult<int>([], 1, pageSize, totalCount);

        Assert.Equal(expectedPages, result.TotalPages);
    }

    [Fact]
    public void Reports_next_and_previous_pages()
    {
        var middle = new PagedResult<int>([], 2, 10, 30);
        var last = new PagedResult<int>([], 3, 10, 30);

        Assert.True(middle.HasNext);
        Assert.True(middle.HasPrevious);
        Assert.False(last.HasNext);
    }

    [Fact]
    public void Map_keeps_paging_information()
    {
        var page = new PagedResult<int>([1, 2, 3], 2, 3, 9);

        var mapped = page.Map(x => $"#{x}");

        Assert.Equal(["#1", "#2", "#3"], mapped.Items);
        Assert.Equal((2, 3, 9), (mapped.Page, mapped.PageSize, mapped.TotalCount));
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, PageQuery.MaxPageSize + 1)]
    public void Rejects_out_of_range_page_values(int page, int pageSize)
    {
        var query = new PageQuery { Page = page, PageSize = pageSize };

        var isValid = Validator.TryValidateObject(query, new ValidationContext(query), [], validateAllProperties: true);

        Assert.False(isValid);
    }
}
