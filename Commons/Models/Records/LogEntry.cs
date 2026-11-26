using System;
using System.Text;
using SportDJ.Commons.Enums;

namespace SportDJ.Commons.Models.Records;

/// <summary>
/// A message of the log, with the exception as text, if there was one.
/// </summary>
public record LogEntry(DateTime Time, LogLevel Level, string Source, string Message, string Exception)
{
    #region Public Properties

    public bool HasException => !string.IsNullOrEmpty(Exception);

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// One line for the message, the exception follows on further lines.
    /// </summary>
    public string Format()
    {
        var result = new StringBuilder();

        result.Append(Time.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        result.Append(' ');
        result.Append(GetLevelText());
        result.Append(' ');
        result.Append(Source);
        result.Append(": ");
        result.Append(Message);

        if (HasException)
        {
            result.AppendLine();
            result.Append(Exception);
        }

        return result.ToString();
    }

    #endregion Public Methods

    #region Private Methods

    private string GetLevelText()
    {
        return Level switch
        {
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            _ => "ERR",
        };
    }

    #endregion Private Methods
}
