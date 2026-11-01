using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenBoardAnim.Utilities
{
    public enum LogAction
    {
        LogOnly,
        LogAndShow,
        LogAndThrow
    }
    public static class Logger
    {
        public static event EventHandler<string> MessageLogged;
        public static event EventHandler<string> WarningLogged;
        public static event EventHandler<string> ErrorLogged;
        static Logger()
        {
            // Logs deliberately do NOT live under AppDomain.CurrentDomain.BaseDirectory: an
            // installed copy runs from %ProgramFiles%\OpenDoodler, which BUILTIN\Users can only
            // read, so a File sink there silently discards every message (x64 processes get no
            // VirtualStore redirection). A user hitting a crash in the installed app then has no
            // log to attach to a report. %LocalAppData% is where the rest of the app's per-user
            // state already lives (see ThumbnailHelper/CacheService/ThemeService) and is always
            // writable for the current user.
            string logDirectory = GetLogDirectory();
            try
            {
                Directory.CreateDirectory(logDirectory);
                Log.Logger = BuildFileLogger(logDirectory);
            }
            catch (Exception ex)
            {
                // A logging setup failure must not take the app down with it - the static
                // constructor throwing would surface as a TypeInitializationException on the
                // first Logger.* call anywhere in the app. Fall back to a sink-less logger so
                // every call still succeeds and simply goes nowhere.
                System.Diagnostics.Debug.WriteLine($"Logger setup failed, logging disabled: {ex}");
                Log.Logger = new LoggerConfiguration().CreateLogger();
            }
        }

        /// <summary>
        /// Per-user log folder - %LocalAppData%\OpenBoardAnim\Logs, alongside the app's other
        /// per-user state (Thumbnails lives in the same parent folder).
        /// </summary>
        public static string GetLogDirectory()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenBoardAnim", "Logs");
        }

        private static Serilog.ILogger BuildFileLogger(string logDirectory)
        {
            return new LoggerConfiguration()
                .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Information).WriteTo.File(Path.Combine(logDirectory, "Messages.log"), rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, fileSizeLimitBytes: 3000000))
                .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Warning).WriteTo.File(Path.Combine(logDirectory, "Warnings.log"), rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, fileSizeLimitBytes: 3000000))
                .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Error).WriteTo.File(Path.Combine(logDirectory, "Exceptions.log"), rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, fileSizeLimitBytes: 3000000))
                .CreateLogger();
        }

        /// <summary>
        /// Logs the specified exception.
        /// </summary>
        /// <param name="ex">The ex.</param>
        public static bool LogError(Exception ex, LogAction action)
        {
            try
            {
                Log.Error(ex, string.Empty);
                if (action == LogAction.LogAndShow)
                    ErrorLogged?.Invoke(null, DateTime.Now + " : " + ex.Message);
            }
            catch (Exception logEx)
            {
                // If Log.Error itself fails (disk full, locked file), fall back to Debug
                // output rather than discarding the failure entirely. Also: whether the
                // caller should rethrow (LogAndThrow) must not depend on logging having
                // succeeded - a logging failure shouldn't be able to silently swallow
                // what was meant to be a critical, app-crashing exception.
                System.Diagnostics.Debug.WriteLine($"Logger.LogError failed to write to the log: {logEx}");
            }
            return action == LogAction.LogAndThrow;
        }

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        public static void LogWarning(string msg, LogAction action = LogAction.LogOnly)
        {
            try
            {
                Log.Warning(msg);
                if (action == LogAction.LogAndShow)
                    WarningLogged?.Invoke(null, DateTime.Now + " : " + msg);
            }
            catch (Exception)
            { }
        }

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        public static void LogMessage(string msg, LogAction action = LogAction.LogOnly)
        {
            try
            {
                Log.Information(msg);
                if (action == LogAction.LogAndShow)
                    MessageLogged?.Invoke(null, DateTime.Now + " : " + msg);
            }
            catch (Exception)
            { }
        }
    }
}
