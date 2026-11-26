using System;
using System.IO;
using System.Runtime.CompilerServices;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Commons.Logging;

/// <summary>
/// Short way to write into the log from anywhere, also from static code. The source of the message is
/// taken from the file which writes it. Without a log nothing happens, so tests need no setup.
/// </summary>
public static class Log
{
    #region Private Fields

    private const string ServiceFileName = "Service";

    private static ILog current;

    #endregion Private Fields

    #region Public Properties

    public static ILog Current => current;

    #endregion Public Properties

    #region Public Methods

    public static void Debug(string message, [CallerFilePath] string file = default)
    {
        Write(LogLevel.Debug, message, default, file);
    }

    public static void Error(string message, Exception exception = default, [CallerFilePath] string file = default)
    {
        Write(LogLevel.Error, message, exception, file);
    }

    public static void Information(string message, [CallerFilePath] string file = default)
    {
        Write(LogLevel.Information, message, default, file);
    }

    public static void Initialize(ILog log)
    {
        current = log;
    }

    public static void Warning(string message, Exception exception = default, [CallerFilePath] string file = default)
    {
        Write(LogLevel.Warning, message, exception, file);
    }

    #endregion Public Methods

    #region Private Methods

    // Most services are called Service, so their folder tells more: .../PlayerService/Service.cs gives PlayerService
    private static string GetSource(string file)
    {
        // App.axaml.cs gives App, not App.axaml
        var name = Path.GetFileName(file).Split('.')[0];

        if (name == ServiceFileName)
        {
            var parts = file.Split('\\', '/');

            if (parts.Length > 1)
            {
                name = parts[^2];
            }
        }

        return name;
    }

    private static void Write(LogLevel level, string message, Exception exception, string file)
    {
        try
        {
            current?.Write(
                level: level,
                source: GetSource(file),
                message: message,
                exception: exception);
        }
        catch
        {
            // Writing the log must never be the reason for another failure
        }
    }

    #endregion Private Methods
}
