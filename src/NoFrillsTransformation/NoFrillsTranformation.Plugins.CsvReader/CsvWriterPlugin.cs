using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTranformation.Plugins.Csv
{
    class CsvWriterPlugin : ITargetWriter
    {
        internal CsvWriterPlugin(string target, string[] fieldNames, int[] fieldSizes, string config)
        {
            if (null == fieldNames)
                throw new ArgumentException("Cannot create CsvWriterPlugin without field names.");

            Configure(config);
            _fileName = target.Substring(7); // Strip file://
            _fieldNames = fieldNames;
            _fieldSizes = fieldSizes;
            _writer = new StreamWriter(_fileName, false, _encoding);

            WriteHeaders();
        }

        private string _fileName;
        private string[] _fieldNames;
        private int[] _fieldSizes;
        private StreamWriter _writer;

        #region Configuration
        private char _delimiter;
        private string _encodingString;
        private Encoding _encoding;

        private void Configure(string config)
        {
            // Set defaults
            _delimiter = ',';
            _encodingString = "UTF-8";

            _encoding = Encoding.GetEncoding(_encodingString);
        }
        #endregion

        public void WriteRecord(string[] fieldValues)
        {
            var sb = new StringBuilder();
            bool first = true;
            foreach(string value in fieldValues)
            {
                if (!first)
                {
                    sb.Append(_delimiter);
                }
                first = false;

                sb.Append(EscapeValue(value));
            }
            _writer.WriteLine(sb.ToString());
            _recordsWritten++;
        }

        private int _recordsWritten = 0;
        public int RecordsWritten
        {
            get
            {
                return _recordsWritten;
            }
        }

        private void WriteHeaders()
        {
            string headers = string.Join(_delimiter.ToString(), _fieldNames);
            _writer.WriteLine(headers);
        }

        private string EscapeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            bool containsDelimiter = value.Contains(_delimiter);
            bool containsQuote = value.Contains('"');
            bool containsNewline = value.Contains('\n');
            if (containsDelimiter
                || containsQuote
                || containsNewline)
            {
                var t = value;
                if (containsQuote)
                    t = t.Replace("\"", "\"\"");
                return string.Format("\"{0}\"", t);
            }
            // Nothing to do, just return
            return value;
        }

        #region IDisposable
        // Dispose() calls Dispose(true)
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // NOTE: Leave out the finalizer altogether if this class doesn't 
        // own unmanaged resources itself, but leave the other methods
        // exactly as they are. 
        //~CsvReaderPlugin() 
        //{
        //    // Finalizer calls Dispose(false)
        //    Dispose(false);
        //}

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (null != _writer)
                {
                    _writer.Dispose();
                    _writer = null;
                }
            }
            // free native resources if there are any.
            //if (nativeResource != IntPtr.Zero) 
            //{
            //    Marshal.FreeHGlobal(nativeResource);
            //    nativeResource = IntPtr.Zero;
            //}
        }
        #endregion
    }
}
