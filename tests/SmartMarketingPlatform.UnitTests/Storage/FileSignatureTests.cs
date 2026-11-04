using System.Text;
using SmartMarketingPlatform.Common.Storage;

namespace SmartMarketingPlatform.UnitTests.Storage;

public class FileSignatureTests
{
    [Theory]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00 }, "image/jpeg", ".jpg")]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 }, "image/png", ".png")]
    [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37 }, "application/pdf", ".pdf")]
    public void Detects_known_binary_formats(byte[] header, string mimeType, string extension)
    {
        var result = FileSignature.Detect(header);

        Assert.Equal((mimeType, extension), result);
    }

    [Fact]
    public void Detects_webp()
    {
        var header = "RIFF"u8.ToArray().Concat(new byte[] { 0, 0, 0, 0 }).Concat("WEBPVP8 "u8.ToArray()).ToArray();

        Assert.Equal(("image/webp", ".webp"), FileSignature.Detect(header));
    }

    [Fact]
    public void Detects_mp4()
    {
        var header = new byte[] { 0, 0, 0, 0x20 }.Concat("ftypisom"u8.ToArray()).ToArray();

        Assert.Equal(("video/mp4", ".mp4"), FileSignature.Detect(header));
    }

    [Fact]
    public void Detects_utf8_text_including_arabic()
    {
        var header = Encoding.UTF8.GetBytes("قائمة الأسعار: لاتيه الفستق 14 شيكل");

        Assert.Equal(("text/plain", ".txt"), FileSignature.Detect(header));
    }

    [Fact]
    public void Accepts_text_whose_header_ends_in_the_middle_of_an_arabic_letter()
    {
        var bytes = Encoding.UTF8.GetBytes("لاتيه");
        var cutInsideLastLetter = bytes[..^1];

        Assert.Equal(("text/plain", ".txt"), FileSignature.Detect(cutInsideLastLetter));
    }

    [Fact]
    public void Rejects_invalid_utf8()
    {
        var header = new byte[] { 0x41, 0x42, 0xC3, 0x28, 0x43, 0x44 };

        Assert.Null(FileSignature.Detect(header));
    }

    [Fact]
    public void Rejects_unknown_binary_content()
    {
        var header = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00 };

        Assert.Null(FileSignature.Detect(header));
    }

    [Fact]
    public void Ignores_the_file_extension_and_trusts_the_bytes()
    {
        var executableRenamedToJpg = new byte[] { 0x4D, 0x5A, 0x00, 0x00 };

        Assert.Null(FileSignature.Detect(executableRenamedToJpg));
    }
}
