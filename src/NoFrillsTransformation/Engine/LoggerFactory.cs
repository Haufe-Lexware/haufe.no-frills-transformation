using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine
{
    class LoggerFactory
    {
        // Inject Loggers via MEF
        [ImportMany]
        private IEnumerable<ILoggerFactory>? InjectedLoggers { get; set; }

        public LoggerFactory(CompositionHost container)
        {
            container.SatisfyImports(this);
        }

        public ILogger CreateLogger(string logType, LogLevel logLevel, string config)
        {
            if (null == InjectedLoggers)
                throw new InvalidOperationException("No loggers found/MEF could not inject loggers.");
            ILogger? logger = null;

            foreach (var lf in InjectedLoggers)
            {
                if (!lf.LogType.Equals(logType, StringComparison.InvariantCultureIgnoreCase))
                    continue;
                try
                {
                    logger = lf.CreateLogger(config, logLevel);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException("While creating a logger of type '" + logType + "', an error occurred: " + ex.Message);
                }
            }

            if (null == logger)
                throw new ArgumentException("A logger factory for type '" + logType + "' was not found. Are you missing custom assemblies? Built-in log types are: std, file, and nil.");

            return logger;
        }
    }
}
