namespace SmartMarketingPlatform.Common.Publishing;

// Returns the same realistic comments for every post, so the whole comments flow can be tried
// without real accounts. Each comment id contains the post id, so syncing twice adds nothing.
public sealed class FakeCommentGateway(TimeProvider clock) : ICommentGateway
{
    private static readonly (string Author, string Body)[] Samples =
    [
        ("Lina Haddad", "بجنن! أطيب لاتيه شربته بنابلس 😍"),
        ("Omar Saleh", "بكم الحجم الكبير؟"),
        ("Rami Khalil", "طلبت امبارح ووصل الطلب بارد ومتأخر ساعة"),
        ("Sara Nasser", "بدي اطلب 3 للمكتب، في توصيل؟"),
        ("promo_bot_77", "Get free followers now https://spam.example")
    ];

    public Task<IReadOnlyList<IncomingComment>> GetCommentsAsync(CommentTarget target, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        IReadOnlyList<IncomingComment> comments = Samples.Select((s, i) => new IncomingComment(
            $"fake_{target.ExternalPostId}_{i + 1}",
            null,
            $"user_{i + 1}",
            s.Author,
            s.Author.ToLowerInvariant().Replace(' ', '_'),
            s.Body,
            now.AddMinutes(-(Samples.Length - i) * 7))).ToList();
        return Task.FromResult(comments);
    }

    public Task ReplyAsync(CommentTarget target, string parentCommentId, string body, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
