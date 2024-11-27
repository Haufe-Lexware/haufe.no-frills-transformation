using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Logging
{
    public class StdOutLogger : ILogger
    {
        public StdOutLogger(LogLevel logLevel)
        {
            DateFormat = "yyyy-MM-dd HH:mm:ss";
            LogLevel = logLevel;
        }

        public string DateFormat { get; set; }
        public LogLevel LogLevel { get; set; }

        public void Info(string info)
        {
            if (LogLevel != LogLevel.Info)
                return;
            MakeLog("INFO: " + info);
        }
        
        public void Warning(string warn)
        {
            if (LogLevel != LogLevel.Info
                && LogLevel != LogLevel.Warning)
                return;
            MakeLog("WARNING: " + warn);
        }

        public void Error(string error)
        {
            // Always log errors
            MakeLog("ERROR: " + error);
        }

        protected virtual void MakeLog(string log)
        {
            Log(string.Format("[{0}] {1}", DateTime.Now.ToString(DateFormat), log));
        }

        protected virtual void Log(string log)
        {
            Console.WriteLine(log);
        }

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // No implementation necessary here.
        }
        #endregion
    }
}
