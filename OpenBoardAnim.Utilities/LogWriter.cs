using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
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
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Information).WriteTo.File(AppDomain.CurrentDomain.BaseDirectory + @"\Logs\Messages.log", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, fileSizeLimitBytes: 3000000))
                .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Warning).WriteTo.File(AppDomain.CurrentDomain.BaseDirectory + @"\Logs\Warnings.log", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, fileSizeLimitBytes: 3000000))
                .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Error).WriteTo.File(AppDomain.CurrentDomain.BaseDirectory + @"\Logs\Exceptions.log", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, fileSizeLimitBytes: 3000000))
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
                switch (action)
                {
                    case LogAction.LogAndShow:
                        ErrorLogged?.Invoke(null, DateTime.Now + " : " + ex.Message);
                        break;
                    case LogAction.LogAndThrow:
                        return true;
                }
            }
            catch { }
            return false;
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
