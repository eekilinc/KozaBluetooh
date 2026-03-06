using System.Globalization;
using System.Text;

namespace BluetoothDeskApp.Services;

public static class CommandPayloadBuilder
{
    public static byte[] Build(string input, string commandMode, string lineEnding)
    {
        byte[] body = string.Equals(commandMode, "HEX", StringComparison.OrdinalIgnoreCase)
            ? ParseHex(input)
            : Encoding.ASCII.GetBytes(input);

        var ending = lineEnding.ToUpperInvariant() switch
        {
            "LF" => new byte[] { 0x0A },
            "CR" => new byte[] { 0x0D },
            "CRLF" => new byte[] { 0x0D, 0x0A },
            _ => Array.Empty<byte>()
        };

        if (ending.Length == 0)
        {
            return body;
        }

        var combined = new byte[body.Length + ending.Length];
        Buffer.BlockCopy(body, 0, combined, 0, body.Length);
        Buffer.BlockCopy(ending, 0, combined, body.Length, ending.Length);
        return combined;
    }

    public static byte[] ParseHex(string input)
    {
        var cleaned = input
            .Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace(",", string.Empty)
            .Trim();

        if (string.IsNullOrWhiteSpace(cleaned))
        {
            throw new InvalidOperationException("HEX veri boş olamaz.");
        }

        if (cleaned.Length % 2 != 0)
        {
            throw new InvalidOperationException("HEX uzunluğu çift olmalı. Örnek: AA01FF");
        }

        var bytes = new byte[cleaned.Length / 2];
        for (var i = 0; i < cleaned.Length; i += 2)
        {
            if (!byte.TryParse(cleaned.Substring(i, 2), NumberStyles.HexNumber, null, out var b))
            {
                throw new InvalidOperationException($"Geçersiz HEX byte: {cleaned.Substring(i, 2)}");
            }

            bytes[i / 2] = b;
        }

        return bytes;
    }
}
