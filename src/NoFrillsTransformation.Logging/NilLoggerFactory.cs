using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Logging
{    
    [Export(typeof(ILoggerFactory))]
    public class NilLoggerFactory : ILoggerFactory
    {
        public string LogType { get { return "nil"; } }

        public ILogger CreateLogger(string config, LogLevel logLevel)
        {
            return new NilLogger();
        }
    }
}
