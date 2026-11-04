using System.Globalization;
using System.Net;
using System.Text;
using SmartMarketingPlatform.Common.Ai;

namespace SmartMarketingPlatform.Common.Reports;

// Plain HTML with inline styles: email clients ignore <style> blocks and external CSS.
public static class ReportEmailRenderer
{
    public static string Render(string businessName, OwnerReportDraft draft, OwnerReportFacts facts, bool arabic)
    {
        string T(string ar, string en) => arabic ? ar : en;
        string E(string? text) => WebUtility.HtmlEncode(text ?? "");
        var dir = arabic ? "rtl" : "ltr";
        var align = arabic ? "right" : "left";

        var html = new StringBuilder();
        html.Append($"""
            <!doctype html>
            <html lang="{(arabic ? "ar" : "en")}" dir="{dir}"><head><meta charset="utf-8"><title>{E(draft.Headline)}</title></head>
            <body style="margin:0;background:#f3f6fa;font-family:Tahoma,Arial,sans-serif;color:#1d2430">
            <div style="max-width:620px;margin:0 auto;padding:20px;text-align:{align}">
              <div style="background:#1F3A5F;color:#fff;border-radius:8px;padding:18px 20px">
                <div style="font-size:13px;opacity:.8">{E(businessName)} · {E(facts.PeriodStartLocal)} - {E(facts.PeriodEndLocal)}</div>
                <div style="font-size:20px;font-weight:bold;margin-top:6px">{E(draft.Headline)}</div>
              </div>
              <p style="font-size:15px;line-height:1.7;margin:16px 4px">{E(draft.Summary)}</p>
              <table role="presentation" width="100%" style="border-collapse:collapse;margin:8px 0 14px">
                <tr>
            """);

        foreach (var (label, value, previous, isRate) in new[]
                 {
                     (T("البوستات", "Posts"), (decimal)facts.Posts, (decimal)facts.PreviousPosts, false),
                     (T("الوصول", "Reach"), facts.Reach, facts.PreviousReach, false),
                     (T("التفاعل", "Engagement"), facts.EngagementRate, facts.PreviousEngagementRate, true)
                 })
        {
            var shown = isRate
                ? (value * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%"
                : value.ToString("N0", CultureInfo.InvariantCulture);
            html.Append($"""
                  <td style="background:#fff;border:1px solid #d9dee7;border-radius:6px;padding:10px;text-align:center;width:33%">
                    <div style="font-size:12px;color:#5b6575">{E(label)}</div>
                    <div style="font-size:20px;font-weight:bold;color:#1F3A5F">{shown}</div>
                    <div style="font-size:11px;color:#5b6575">{E(Change(value, previous, T("جديد", "new")))}</div>
                  </td>
            """);
        }
        html.Append("</tr></table>");

        Section(T("شو نجح", "What worked"), draft.Wins, "#2e7d32");
        Section(T("انتبه لـ", "Needs attention"), draft.Concerns, "#b26a00");
        Section(T("شو تعمل هالأسبوع", "What to do this week"), draft.NextActions, "#1F3A5F");

        if (facts.TopPost?.Url is { } url)
            html.Append($"""<p style="margin:14px 4px"><a href="{E(url)}" style="color:#2f6fb3">{E(T("شوف أقوى بوست", "See the top post"))}</a></p>""");

        html.Append($"""
              <p style="font-size:11px;color:#8a94a5;margin:24px 4px 0">{E(T("تقرير تلقائي من منصة التسويق الذكية. الأرقام من حساباتك، والنصائح من الذكاء الاصطناعي.", "Automatic report from Smart Marketing Platform. Numbers come from your accounts; advice is written by AI."))}</p>
            </div></body></html>
            """);
        return html.ToString();

        void Section(string title, IReadOnlyList<string> items, string color)
        {
            if (items.Count == 0) return;
            html.Append($"""<div style="background:#fff;border:1px solid #d9dee7;border-{(arabic ? "right" : "left")}:4px solid {color};border-radius:6px;padding:10px 14px;margin:10px 0">""");
            html.Append($"""<div style="font-weight:bold;color:{color};margin-bottom:6px">{E(title)}</div><ul style="margin:0;padding-{(arabic ? "right" : "left")}:18px;line-height:1.7">""");
            foreach (var item in items)
                html.Append($"<li>{E(item)}</li>");
            html.Append("</ul></div>");
        }
    }

    public static string Change(decimal now, decimal before, string newLabel = "new") =>
        before == 0
            ? (now == 0 ? "-" : newLabel)
            : ((now - before) / before).ToString("+0%;-0%;0%", CultureInfo.InvariantCulture);
}
