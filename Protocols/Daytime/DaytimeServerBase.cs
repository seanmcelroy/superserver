using System.Buffers;
using System.Globalization;
using System.Text;

namespace SuperServer.Protocols.Daytime;

public static class DaytimeServerBase
{
    // Maximum size for daytime response (ISO 8601 + CRLF is ~35 bytes, allow extra for other formats)
    private const int MaxBufferSize = 128;

    public static CultureInfo ParseCulture(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return CultureInfo.InvariantCulture;

        try
        {
            return CultureInfo.GetCultureInfo(culture);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    }

    /// <summary>
    /// Formats the current UTC time into a pooled buffer.
    /// Caller must return the buffer using ArrayPool&lt;byte&gt;.Shared.Return().
    /// </summary>
    /// <returns>A tuple of the rented buffer and the number of bytes written.</returns>
    public static (byte[] Buffer, int Length) FormatDaytimePooled(CultureInfo culture, string formatSpecifier)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(MaxBufferSize);
        try
        {
            string formatted;
            try
            {
                formatted = DateTime.UtcNow.ToString(formatSpecifier, culture) + "\r\n";
            }
            catch (FormatException)
            {
                formatted = DateTime.UtcNow.ToString("o", culture) + "\r\n";
            }

            var length = Encoding.ASCII.GetBytes(formatted, buffer);
            return (buffer, length);
        }
        catch
        {
            // Return buffer on failure before rethrowing
            ArrayPool<byte>.Shared.Return(buffer);
            throw;
        }
    }
}
