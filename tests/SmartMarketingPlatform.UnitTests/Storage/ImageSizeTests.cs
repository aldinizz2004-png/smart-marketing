using SmartMarketingPlatform.Common.Storage;

namespace SmartMarketingPlatform.UnitTests.Storage;

public class ImageSizeTests
{
    internal static byte[] Png(int width, int height)
    {
        var bytes = new byte[33];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, (byte)'I', (byte)'H', (byte)'D', (byte)'R' }.CopyTo(bytes, 0);
        BitConverter.GetBytes(width).Reverse().ToArray().CopyTo(bytes, 16);
        BitConverter.GetBytes(height).Reverse().ToArray().CopyTo(bytes, 20);
        return bytes;
    }

    [Fact]
    public void Reads_png_size()
    {
        Assert.Equal((1080, 1350), ImageSize.Read(Png(1080, 1350)));
    }

    [Fact]
    public void Reads_jpeg_size_after_other_segments()
    {
        byte[] jpeg =
        [
            0xFF, 0xD8,
            0xFF, 0xE0, 0x00, 0x10, .. new byte[14],                    // APP0 (JFIF), 16 bytes
            0xFF, 0xC0, 0x00, 0x11, 0x08, 0x02, 0x58, 0x03, 0x20,       // SOF0: height 600, width 800
            .. new byte[12]
        ];

        Assert.Equal((800, 600), ImageSize.Read(jpeg));
    }

    [Fact]
    public void Reads_extended_webp_size()
    {
        var webp = new byte[30];
        "RIFF"u8.CopyTo(webp);
        "WEBPVP8X"u8.CopyTo(webp.AsSpan(8));
        webp[24] = 0xFF; webp[25] = 0x03;    // width - 1 = 1023
        webp[27] = 0xFF; webp[28] = 0x02;    // height - 1 = 767

        Assert.Equal((1024, 768), ImageSize.Read(webp));
    }

    [Fact]
    public void Unknown_bytes_have_no_size()
    {
        Assert.Null(ImageSize.Read("%PDF-1.7"u8));
    }
}
