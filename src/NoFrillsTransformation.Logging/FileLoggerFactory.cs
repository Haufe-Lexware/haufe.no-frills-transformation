using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Logging
{  
    [Export(typeof(ILoggerFactory))]
    public class FileLoggerFactory : ILoggerFactory
    {
        public string LogType { get { return "file"; } }

        public ILogger CreateLogger(string config, LogLevel logLevel)
        {
            return new FileLogger(config, logLevel);
        }
    }
}
