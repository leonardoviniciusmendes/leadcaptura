using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using LeadEngine.Application.DTOs;
using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Services;

internal static class VideoHeaderReader
{
    public static VideoHeader? Detect(ReadOnlySpan<byte> content)
    {
        if (content.Length >= 12 && Encoding.ASCII.GetString(content.Slice(4, 4)) == "ftyp")
        {
            return DetectMp4(content);
        }

        ReadOnlySpan<byte> webmSignature = [0x1A, 0x45, 0xDF, 0xA3];
        if (content.Length >= 4 && content.Slice(0, 4).SequenceEqual(webmSignature))
        {
            return DetectWebm(content);
        }

        return null;
    }

    private static VideoHeader? DetectMp4(ReadOnlySpan<byte> content)
    {
        var brand = content.Length >= 12 ? Encoding.ASCII.GetString(content.Slice(8, 4)) : string.Empty;
        if (string.IsNullOrWhiteSpace(brand))
        {
            return null;
        }

        uint? timescale = null;
        ulong? duration = null;
        int? width = null;
        int? height = null;
        ScanMp4Boxes(content, box =>
        {
            if (box.Type == "mvhd" && box.Payload.Length >= 24)
            {
                var version = box.Payload[0];
                if (version == 0 && box.Payload.Length >= 20)
                {
                    timescale = BinaryPrimitives.ReadUInt32BigEndian(box.Payload.AsSpan(12, 4));
                    duration = BinaryPrimitives.ReadUInt32BigEndian(box.Payload.AsSpan(16, 4));
                }
                else if (version == 1 && box.Payload.Length >= 32)
                {
                    timescale = BinaryPrimitives.ReadUInt32BigEndian(box.Payload.AsSpan(20, 4));
                    duration = BinaryPrimitives.ReadUInt64BigEndian(box.Payload.AsSpan(24, 8));
                }
            }

            if (box.Type == "tkhd")
            {
                var version = box.Payload.Length > 0 ? box.Payload[0] : 0;
                var widthOffset = version == 1 ? 88 : 76;
                var heightOffset = widthOffset + 4;
                if (box.Payload.Length >= heightOffset + 4)
                {
                    width = (int)Math.Round(BinaryPrimitives.ReadUInt32BigEndian(box.Payload.AsSpan(widthOffset, 4)) / 65536d);
                    height = (int)Math.Round(BinaryPrimitives.ReadUInt32BigEndian(box.Payload.AsSpan(heightOffset, 4)) / 65536d);
                }
            }
        });

        if (width is null or <= 0 || height is null or <= 0 || timescale is null or 0 || duration is null)
        {
            return null;
        }

        return new VideoHeader("video/mp4", width.Value, height.Value, duration.Value / (double)timescale.Value);
    }

    private static void ScanMp4Boxes(ReadOnlySpan<byte> content, Action<Mp4Box> visit)
    {
        var stack = new Stack<ReadOnlyMemory<byte>>();
        stack.Push(content.ToArray());
        while (stack.Count > 0)
        {
            var span = stack.Pop().Span;
            var offset = 0;
            while (offset + 8 <= span.Length)
            {
                var size = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset, 4));
                if (size < 8 || offset + size > span.Length)
                {
                    break;
                }

                var type = Encoding.ASCII.GetString(span.Slice(offset + 4, 4));
                var payload = span.Slice(offset + 8, (int)size - 8).ToArray();
                visit(new Mp4Box(type, payload));
                if (type is "moov" or "trak" or "mdia")
                {
                    stack.Push(payload);
                }

                offset += (int)size;
            }
        }
    }

    private static VideoHeader? DetectWebm(ReadOnlySpan<byte> content)
    {
        var duration = FindFloat(content, [0x44, 0x89]);
        var width = FindUnsigned(content, [0xB0]);
        var height = FindUnsigned(content, [0xBA]);
        if (duration is null or <= 0 || width is null or <= 0 || height is null or <= 0)
        {
            return null;
        }

        return new VideoHeader("video/webm", (int)width.Value, (int)height.Value, duration.Value);
    }

    private static double? FindFloat(ReadOnlySpan<byte> content, byte[] id)
    {
        var index = IndexOf(content, id);
        if (index < 0 || index + id.Length + 1 >= content.Length)
        {
            return null;
        }

        var size = content[index + id.Length];
        var start = index + id.Length + 1;
        if (size == 4 && start + 4 <= content.Length)
        {
            var bits = BinaryPrimitives.ReadInt32BigEndian(content.Slice(start, 4));
            return BitConverter.Int32BitsToSingle(bits);
        }
        if (size == 8 && start + 8 <= content.Length)
        {
            var bits = BinaryPrimitives.ReadInt64BigEndian(content.Slice(start, 8));
            return BitConverter.Int64BitsToDouble(bits);
        }

        return null;
    }

    private static ulong? FindUnsigned(ReadOnlySpan<byte> content, byte[] id)
    {
        var index = IndexOf(content, id);
        if (index < 0 || index + id.Length + 1 >= content.Length)
        {
            return null;
        }

        var size = content[index + id.Length];
        var start = index + id.Length + 1;
        if (size is < 1 or > 8 || start + size > content.Length)
        {
            return null;
        }

        ulong value = 0;
        for (var i = 0; i < size; i++)
        {
            value = (value << 8) | content[start + i];
        }

        return value;
    }

    private static int IndexOf(ReadOnlySpan<byte> content, byte[] needle)
    {
        for (var i = 0; i <= content.Length - needle.Length; i++)
        {
            if (content.Slice(i, needle.Length).SequenceEqual(needle))
            {
                return i;
            }
        }

        return -1;
    }

    private sealed record Mp4Box(string Type, byte[] Payload);
}

internal sealed record VideoHeader(string MimeType, int Width, int Height, double DurationSeconds);

internal static class VideoPosterGenerator
{
    public static byte[] GeneratePng(int width, int height, string label)
    {
        var safeWidth = Math.Clamp(width, 320, 1280);
        var safeHeight = Math.Clamp(height, 180, 720);
        var pixels = new byte[(safeWidth * 4 + 1) * safeHeight];
        var row = safeWidth * 4 + 1;
        for (var y = 0; y < safeHeight; y++)
        {
            var offset = y * row;
            pixels[offset] = 0;
            for (var x = 0; x < safeWidth; x++)
            {
                var p = offset + 1 + x * 4;
                pixels[p] = (byte)(24 + x % 32);
                pixels[p + 1] = (byte)(35 + y % 32);
                pixels[p + 2] = 48;
                pixels[p + 3] = 255;
            }
        }

        using var output = new MemoryStream();
        output.Write([137, 80, 78, 71, 13, 10, 26, 10]);
        WriteChunk(output, "IHDR", BuildIhdr(safeWidth, safeHeight));
        using var compressed = new MemoryStream();
        using (var deflate = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
        {
            deflate.Write(pixels);
        }
        WriteChunk(output, "IDAT", compressed.ToArray());
        WriteChunk(output, "IEND", []);
        return output.ToArray();
    }

    private static byte[] BuildIhdr(int width, int height)
    {
        var data = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(0, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(4, 4), height);
        data[8] = 8;
        data[9] = 6;
        return data;
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);
        var typeBytes = Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);
        stream.Write(data);
        Span<byte> crc = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32(typeBytes, data));
        stream.Write(crc);
    }

    private static uint Crc32(byte[] type, byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in type.Concat(data))
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 1) == 1 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
            }
        }

        return ~crc;
    }
}

internal static class VideoFrameSampler
{
    public static IReadOnlyList<CreativeAssetAnalysisFrame> Sample(CreativeAsset asset, byte[] posterContent)
    {
        var duration = Math.Max(asset.DurationSeconds ?? 0, 1);
        var offsets = new[] { 0.5, duration * 0.25, duration * 0.5, duration * 0.75, Math.Max(duration - 0.5, 0.5) }
            .DistinctBy(x => Math.Round(x, 1))
            .Take(5)
            .ToArray();
        return offsets
            .Select((offset, index) => new CreativeAssetAnalysisFrame($"frame-{index + 1}", offset, "image/png", posterContent))
            .ToArray();
    }
}
