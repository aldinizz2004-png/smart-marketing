using System.Text;

namespace SmartMarketingPlatform.Common.Storage;

// Detects the real type of a file from its first bytes ("magic numbers").
// The file name and the Content-Type sent by the client are never trusted.
public static class FileSignature
{
    public const int HeaderLength = 4096;

    public static (string MimeType, string Extension)? Detect(ReadOnlySpan<byte> header)
    {
        if (header.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF })) return ("image/jpeg", ".jpg");
        if (header.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return ("image/png", ".png");
        if (header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8)) return ("image/webp", ".webp");
        if (header.Length >= 12 && header[4..8].SequenceEqual("ftyp"u8)) return ("video/mp4", ".mp4");
        if (header.StartsWith("%PDF-"u8)) return ("application/pdf", ".pdf");
        if (LooksLikeText(header)) return ("text/plain", ".txt");
        return null;
    }

    // Plain text has no signature: accept valid UTF-8 without NUL bytes.
    private static bool LooksLikeText(ReadOnlySpan<byte> header)
    {
        if (header.IsEmpty || header.Contains((byte)0)) return false;
        try
        {
            // The header may cut a multi-byte character at the end; ignore the last 3 bytes.
            var probe = header.Length > 3 ? header[..^3] : header;
            new UTF8Encoding(false, throwOnInvalidBytes: true).GetCharCount(probe);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }
}
