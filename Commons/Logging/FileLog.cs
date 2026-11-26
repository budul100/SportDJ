using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Records;

namespace SportDJ.Commons.Logging;

/// <summary>
/// Writes the log into one file per day, in the background, so the callers never wait for the disk.
/// The most recent entries are kept in memory as well, e.g. to show them in the app.
/// </summary>
public sealed class FileLog
    : ILog, IDisposable
{
    #region Private Fields

    private const int EntriesMax = 500;
    private const string FilePattern = "SportDJ_*.log";
    private const int RetentionDays = 14;
    private const int WaitingSecondsOnDispose = 2;

    private readonly Channel<LogEntry> channel = Channel.CreateUnbounded<LogEntry>(
        new UnboundedChannelOptions { SingleReader = true });

    private readonly List<LogEntry> entries = [];
    private readonly object entriesLock = new();
    private readonly string folder;
    private readonly Task writer;

    private bool isDisposed;

    #endregion Private Fields

    #region Public Constructors

    public FileLog()
        : this(
            folder: Path.Combine(
                path1: Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                path2: Texts.AppName,
                path3: "Logs"))
    { }

    public FileLog(string folder)
    {
        this.folder = folder;

        writer = Task.Run(WriteAsync);
    }

    #endregion Public Constructors

    #region Public Events

    public event EventHandler<LogEntry> Added;

    #endregion Public Events

    #region Public Properties

    public IReadOnlyList<LogEntry> Entries
    {
        get
        {
            lock (entriesLock)
            {
                return [.. entries];
            }
        }
    }

    #endregion Public Properties

    #region Public Methods

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;

            channel.Writer.TryComplete();

            // The rest of the queue is written, but the end of the app must not hang on a broken disk
            writer.Wait(TimeSpan.FromSeconds(WaitingSecondsOnDispose));
        }
    }

    public void Write(LogLevel level, string source, string message, Exception exception = default)
    {
        var entry = new LogEntry(
            Time: DateTime.Now,
            Level: level,
            Source: source,
            Message: message,
            Exception: exception?.ToString());

        lock (entriesLock)
        {
            entries.Add(entry);

            if (entries.Count > EntriesMax)
            {
                entries.RemoveAt(0);
            }
        }

        // Trace also works in release builds, e.g. with DebugView
        Trace.WriteLine(entry.Format());

        channel.Writer.TryWrite(entry);

        try
        {
            Added?.Invoke(
                sender: this,
                e: entry);
        }
        catch (Exception exceptionAdded)
        {
            Trace.WriteLine($"A subscriber of the log failed: {exceptionAdded}");
        }
    }

    #endregion Public Methods

    #region Private Methods

    private void DeleteOldFiles()
    {
        try
        {
            var limit = DateTime.Now.AddDays(-RetentionDays);

            var oldFiles = new DirectoryInfo(folder)
                .GetFiles(FilePattern)
                .Where(f => f.LastWriteTime < limit).ToArray();

            foreach (var oldFile in oldFiles)
            {
                oldFile.Delete();
            }
        }
        catch (Exception exception)
        {
            Trace.WriteLine($"Deleting old log files failed: {exception}");
        }
    }

    private async Task WriteAsync()
    {
        var currentDay = default(DateTime?);
        var currentWriter = default(StreamWriter);

        try
        {
            Directory.CreateDirectory(folder);

            DeleteOldFiles();

            await foreach (var entry in channel.Reader.ReadAllAsync())
            {
                try
                {
                    if (currentDay != entry.Time.Date)
                    {
                        currentWriter?.Dispose();

                        currentDay = entry.Time.Date;

                        // Other programs may read the file while it is written
                        currentWriter = new StreamWriter(
                            stream: new FileStream(
                                path: Path.Combine(folder, $"SportDJ_{entry.Time:yyyyMMdd}.log"),
                                mode: FileMode.Append,
                                access: FileAccess.Write,
                                share: FileShare.ReadWrite),
                            encoding: System.Text.Encoding.UTF8)
                        {
                            AutoFlush = true,
                        };
                    }

                    currentWriter.WriteLine(entry.Format());
                }
                catch (Exception exception)
                {
                    // The log must not fail the app, so a broken file only costs the entry
                    Trace.WriteLine($"Writing the log failed: {exception}");

                    currentWriter?.Dispose();
                    currentWriter = default;
                    currentDay = default;
                }
            }
        }
        catch (Exception exception)
        {
            Trace.WriteLine($"The log writer stopped: {exception}");
        }
        finally
        {
            currentWriter?.Dispose();
        }
    }

    #endregion Private Methods
}
