namespace poc_mercadopago.Helpers;

/// <summary>
/// Sanitiza strings externos antes de incluirlos en logs para prevenir log injection.
///
/// El ataque: un atacante envía datos con saltos de línea (\n) que, al loggearse,
/// crean entradas de log falsas que parecen legítimas.
///
/// La defensa: reemplazar todos los caracteres de control por un placeholder visible.
/// </summary>
public static class LogSanitizer
{
    // Caracteres que pueden falsificar entradas de log
    private static readonly char[] DangerousChars = ['\n', '\r', '\t', '\0'];

    /// <summary>
    /// Reemplaza caracteres de control por el placeholder [CTRL].
    /// Null-safe: devuelve "(null)" si el input es null.
    /// </summary>
    public static string Sanitize(string? input)
    {
        if (input is null) return "(null)";

        // Si no hay caracteres peligrosos, devolver el string original sin allocar nada nuevo
        if (input.IndexOfAny(DangerousChars) == -1)
            return input;

        // Reemplazar caracteres peligrosos por placeholder visible en el log
        return input
            .Replace("\n", "[LF]")
            .Replace("\r", "[CR]")
            .Replace("\t", "[TAB]")
            .Replace("\0", "[NUL]");
    }

    /// <summary>
    /// Sanitiza y trunca. Útil para campos que no deberían ser muy largos
    /// (user-agent, query strings) para evitar que el log se infle.
    /// </summary>
    public static string SanitizeAndTruncate(string? input, int maxLength = 200)
    {
        var sanitized = Sanitize(input);
        return sanitized.Length <= maxLength
            ? sanitized
            : sanitized[..maxLength] + "...[truncated]";
    }
}
