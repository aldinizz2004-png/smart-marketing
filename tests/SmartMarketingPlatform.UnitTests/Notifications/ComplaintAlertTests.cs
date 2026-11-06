using SmartMarketingPlatform.Common.Notifications;

namespace SmartMarketingPlatform.UnitTests.Notifications;

public class ComplaintAlertTests
{
    private static readonly string[] Owners = ["owner@test.local", "admin@test.local"];

    [Fact]
    public void Build_SendsToAllRecipients_WithCountInSubject()
    {
        var message = ComplaintAlert.Build(Owners, "Cafe Yafa", [Line("القهوة باردة")]);

        Assert.Equal(Owners, message.To);
        Assert.Equal("Cafe Yafa: 1 new complaint(s) need a reply", message.Subject);
        Assert.Contains("القهوة باردة", message.HtmlBody);
        Assert.Contains("dir=\"rtl\"", message.HtmlBody);
    }

    [Fact]
    public void Build_EncodesCustomerText()
    {
        var message = ComplaintAlert.Build(Owners, "Cafe <b>", [new ComplaintLine("<script>", "instagram", "bad & <img src=x>", "<i>sorry</i>")]);

        Assert.DoesNotContain("<script>", message.HtmlBody);
        Assert.DoesNotContain("<img", message.HtmlBody);
        Assert.DoesNotContain("<i>sorry", message.HtmlBody);
        Assert.Contains("bad &amp; &lt;img src=x&gt;", message.HtmlBody);
        Assert.Contains("Cafe &lt;b&gt;", message.HtmlBody);
    }

    [Fact]
    public void Build_ListsFiveComplaints_AndCountsTheRest()
    {
        var complaints = Enumerable.Range(1, 8).Select(i => Line($"complaint-{i}")).ToList();

        var message = ComplaintAlert.Build(Owners, "Cafe", complaints);

        Assert.Contains("complaint-5", message.HtmlBody);
        Assert.DoesNotContain("complaint-6", message.HtmlBody);
        Assert.Contains("و 3 غيرهم", message.HtmlBody);
        Assert.Equal("Cafe: 8 new complaint(s) need a reply", message.Subject);
    }

    [Fact]
    public void Build_UsesPlaceholder_WhenAuthorIsUnknown()
    {
        var message = ComplaintAlert.Build(Owners, "Cafe", [new ComplaintLine(null, "facebook", "late order", null)]);

        Assert.Contains("زبون", message.HtmlBody);
        Assert.DoesNotContain("الرد المقترح", message.HtmlBody);
    }

    private static ComplaintLine Line(string body) => new("Sara", "instagram", body, "نعتذر منك");
}
