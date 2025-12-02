using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Logging
{
    class FileLogger : StdOutLogger
    {
        public FileLogger(string config, LogLevel logLevel) : base(logLevel)
        {
            _fileWriter = new StreamWriter(config, true);
        }

        private StreamWriter? _fileWriter;

        protected override void Log(string log)
        {
            if (null == _fileWriter)
                return;
            _fileWriter.WriteLine(log);
        }

        #region IDisposable
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                // free managed resources
                if (null != _fileWriter)
                {
                    _fileWriter.Dispose();
                    _fileWriter = null;
                }
            }
        }
        #endregion
    }
}
