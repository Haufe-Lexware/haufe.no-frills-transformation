using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine
{
    class LoggerFactory
    {
        public LoggerFactory(CompositionContainer container)
        {
            container.ComposeParts(this);
        }

#pragma warning disable 0649
        // Inject Loggers via MEF
        [ImportMany(typeof(NoFrillsTransformation.Interfaces.ILoggerFactory))]
        private ILoggerFactory[] _injectedLoggers;
#pragma warning restore 0649

        //public ILogger CreateLogger()

        public ILogger CreateLogger(string logType, LogLevel logLevel, string config)
        {
            ILogger logger = null;

            foreach (var lf in _injectedLoggers)
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
