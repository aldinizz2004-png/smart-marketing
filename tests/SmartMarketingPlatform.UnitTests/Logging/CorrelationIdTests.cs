using SmartMarketingPlatform.Common.Logging;

namespace SmartMarketingPlatform.UnitTests.Logging;

public class CorrelationIdTests
{
    [Theory]
    [InlineData("abc-123")]
    [InlineData("order.42_retry")]
    [InlineData("0f8fad5bd9cb469fa16570867728950e")]
    public void A_plain_id_from_the_caller_is_kept(string incoming)
    {
        Assert.Equal(incoming, CorrelationId.Resolve(incoming, () => "new"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("has spaces")]
    [InlineData("line\nbreak")]
    [InlineData("<script>")]
    [InlineData("this-id-is-much-too-long-to-be-a-correlation-id-0123456789-0123456789")]
    public void Anything_else_gets_a_new_id(string? incoming)
    {
        Assert.Equal("new", CorrelationId.Resolve(incoming, () => "new"));
    }
}
