namespace SSSKL.Web.Logging;

/// <summary>
/// Cleans request-controlled values before they are logged, so a client can't forge extra log lines by
/// putting line breaks in a path, header, file name or claim.
/// </summary>
public static class LogSanitizer
{
    /// <summary>Returns <paramref name="value"/> as text with carriage returns and line feeds replaced by spaces.</summary>
    public static string? Clean(object? value) =>
        value?.ToString()?.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
}
