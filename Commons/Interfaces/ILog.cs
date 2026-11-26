using System;
using System.Collections.Generic;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Records;

namespace SportDJ.Commons.Interfaces;

public interface ILog
{
    #region Public Events

    /// <summary>
    /// Raised for each new entry, on the thread which wrote it.
    /// </summary>
    event EventHandler<LogEntry> Added;

    #endregion Public Events

    #region Public Properties

    /// <summary>
    /// The most recent entries, the oldest first. Older ones are only in the log files.
    /// </summary>
    IReadOnlyList<LogEntry> Entries { get; }

    #endregion Public Properties

    #region Public Methods

    void Write(LogLevel level, string source, string message, Exception exception = default);

    #endregion Public Methods
}
