using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Interfaces
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    public interface ILoggerFactory
    {
        string LogType { get; }
        ILogger CreateLogger(string configuration, LogLevel logLevel);
    }
}
