using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Logging
{
    [Export(typeof(ILoggerFactory))]
    public class StdOutLoggerFactory : ILoggerFactory
    {
        public string LogType { get { return "std"; } }

        public ILogger CreateLogger(string config, LogLevel logLevel)
        {
            return new StdOutLogger(logLevel);
        }
    }
}
