using System.Buffers.Binary;

namespace LeadEngine.Application.Services;

internal static class ImageHeaderReader
{
    public static ImageHeader? Detect(byte[] content)
    {
        ReadOnlySpan<byte> pngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
        if (content.Length >= 24 && content.AsSpan(0, 8).SequenceEqual(pngSignature))
        {
            return new ImageHeader("image/png", BinaryPrimitives.ReadInt32BigEndian(content.AsSpan(16, 4)), BinaryPrimitives.ReadInt32BigEndian(content.AsSpan(20, 4)));
        }

        if (content.Length >= 10 && content[0] == 'G' && content[1] == 'I' && content[2] == 'F')
        {
            return new ImageHeader("image/gif", BinaryPrimitives.ReadUInt16LittleEndian(content.AsSpan(6, 2)), BinaryPrimitives.ReadUInt16LittleEndian(content.AsSpan(8, 2)));
        }

        if (content.Length >= 30 && content[0] == 'R' && content[1] == 'I' && content[2] == 'F' && content[3] == 'F' && content[8] == 'W' && content[9] == 'E' && content[10] == 'B' && content[11] == 'P')
        {
            return DetectWebp(content);
        }

        if (content.Length >= 4 && content[0] == 0xFF && content[1] == 0xD8)
        {
            return DetectJpeg(content);
        }

        return null;
    }

    private static ImageHeader? DetectJpeg(byte[] content)
    {
        var index = 2;
        while (index + 9 < content.Length)
        {
            if (content[index] != 0xFF)
            {
                index++;
                continue;
            }

            var marker = content[index + 1];
            if (marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF)
            {
                return new ImageHeader("image/jpeg", BinaryPrimitives.ReadUInt16BigEndian(content.AsSpan(index + 7, 2)), BinaryPrimitives.ReadUInt16BigEndian(content.AsSpan(index + 5, 2)));
            }

            if (marker == 0xD9 || marker == 0xDA)
            {
                break;
            }

            var length = BinaryPrimitives.ReadUInt16BigEndian(content.AsSpan(index + 2, 2));
            if (length < 2)
            {
                break;
            }

            index += 2 + length;
        }

        return null;
    }

    private static ImageHeader? DetectWebp(byte[] content)
    {
        var format = System.Text.Encoding.ASCII.GetString(content, 12, 4);
        if (format == "VP8 " && content.Length >= 30)
        {
            return new ImageHeader("image/webp", BinaryPrimitives.ReadUInt16LittleEndian(content.AsSpan(26, 2)) & 0x3FFF, BinaryPrimitives.ReadUInt16LittleEndian(content.AsSpan(28, 2)) & 0x3FFF);
        }

        if (format == "VP8L" && content.Length >= 25)
        {
            var b0 = content[21];
            var b1 = content[22];
            var b2 = content[23];
            var b3 = content[24];
            return new ImageHeader("image/webp", 1 + (((b1 & 0x3F) << 8) | b0), 1 + (((b3 & 0x0F) << 10) | (b2 << 2) | ((b1 & 0xC0) >> 6)));
        }

        if (format == "VP8X" && content.Length >= 30)
        {
            var width = 1 + content[24] + (content[25] << 8) + (content[26] << 16);
            var height = 1 + content[27] + (content[28] << 8) + (content[29] << 16);
            return new ImageHeader("image/webp", width, height);
        }

        return null;
    }
}

internal sealed record ImageHeader(string MimeType, int Width, int Height);
