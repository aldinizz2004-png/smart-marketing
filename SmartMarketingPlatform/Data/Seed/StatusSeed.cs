using System.Reflection;
using System.Text.RegularExpressions;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Seed;

// Builds the Statuses seed rows from StatusIds: class name = Category, field name = Code.
public static class StatusSeed
{
    public static Status[] All() =>
        typeof(StatusIds).GetNestedTypes()
            .SelectMany(category => category
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(field =>
                {
                    var id = (int)field.GetRawConstantValue()!;
                    return new Status
                    {
                        Id = id,
                        Category = category.Name,
                        Code = field.Name,
                        TranslationKey = $"status.{ToSnake(category.Name)}.{ToSnake(field.Name)}",
                        SortOrder = id % 100,
                    };
                }))
            .ToArray();

    // "ContentItem" -> "content_item", "InReview" -> "in_review"
    private static string ToSnake(string name) =>
        Regex.Replace(name, "(?<!^)([A-Z])", "_$1").ToLowerInvariant();
}