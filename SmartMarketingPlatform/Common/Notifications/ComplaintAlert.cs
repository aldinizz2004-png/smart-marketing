using System.Net;
using System.Text;

namespace SmartMarketingPlatform.Common.Notifications;

public sealed record ComplaintLine(string? Author, string Platform, string Body, string? SuggestedReply);

// The email the owner gets when new complaints appear under the business's posts.
public static class ComplaintAlert
{
    private const int MaxListed = 5;

    public static EmailMessage Build(IReadOnlyList<string> to, string businessName, IReadOnlyList<ComplaintLine> complaints)
    {
        var html = new StringBuilder();
        html.Append("""<div dir="rtl" style="font-family:Tahoma,Arial,sans-serif;font-size:15px">""");
        html.Append($"<p><b>وصلك {complaints.Count} شكوى جديدة على بوستات {Encode(businessName)}</b></p>");
        html.Append("<p>الرد السريع والهادئ على الشكوى بيحافظ على سمعة المحل. في رد مقترح جاهز لكل وحدة بالمنصة.</p><ul>");
        foreach (var c in complaints.Take(MaxListed))
        {
            html.Append($"<li><b>{Encode(c.Author ?? "زبون")}</b> ({Encode(c.Platform)}): {Encode(c.Body)}");
            if (!string.IsNullOrWhiteSpace(c.SuggestedReply))
                html.Append($"<br/><span style=\"color:#555\">الرد المقترح: {Encode(c.SuggestedReply)}</span>");
            html.Append("</li>");
        }
        html.Append("</ul>");
        if (complaints.Count > MaxListed)
            html.Append($"<p>و {complaints.Count - MaxListed} غيرهم بالمنصة.</p>");
        html.Append("</div>");

        return new EmailMessage(to, $"{businessName}: {complaints.Count} new complaint(s) need a reply", html.ToString());
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
