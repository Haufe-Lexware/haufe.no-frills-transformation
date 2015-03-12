using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

using LumenWorks.Framework.IO;
using System.IO;

namespace NoFrillsTranformation.Plugins.CsvReader
{
    class CsvReaderPlugin : ISourceReader, IRecord
    {
        internal CsvReaderPlugin(string source, string config)
        {
            _fileName = source.Substring(7); // Strip file://

            TextReader textReader = null;
            try
            {
                textReader = new StreamReader(new FileStream(_fileName, FileMode.Open));
                _csvReader = new LumenWorks.Framework.IO.Csv.CsvReader(textReader, true, _delimiter);
                Configure(config);
            }
            catch (Exception)
            {
                if (null != textReader)
                    textReader.Dispose();
                throw;
            }

        }

        private string _fileName;
        private LumenWorks.Framework.IO.Csv.CsvReader _csvReader;

        #region Configuration
        protected char _delimiter = ',';

        protected void Configure(string config)
        {
            if (string.IsNullOrWhiteSpace(config))
                return;

            _csvReader.SupportsMultiline = true;
        }
        #endregion

        #region ISourceReader
        public bool IsEndOfStream
        {
            get
            {
                return _csvReader.EndOfStream;
            }
        }

        public void NextRecord()
        {
            if (IsEndOfStream)
                return;
            _csvReader.ReadNextRecord();
        }

        public IRecord CurrentRecord
        {
            get
            {
                return this;
            }
        }
        public int FieldCount
        {
            get
            {
                return _csvReader.FieldCount;
            }
        }

        private string[] _fieldNames = null;
        public string[] FieldNames
        {
            get
            {
                if (null == _fieldNames)
                {
                    _fieldNames = _csvReader.GetFieldHeaders();
                }
                return _fieldNames;
            }
        }
        public int GetFieldIndex(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("Field names for retrieving indexes must be non-empty.");
            //for (int i = 0; i < FieldNames.Length; ++i)
            //{
            //    if (fieldName.Equals(FieldNames[i], StringComparison.InvariantCultureIgnoreCase))
            //        return i;
            //}
            return _csvReader.GetFieldIndex(fieldName);
            //return -1;
        }

        public IRecord Query(string key)
        {
            throw new InvalidOperationException("The CSV Reader (CsvReaderPlugin) does not support the Query() operator.");
        }
        #endregion

        #region IRecord
        public string this[string fieldName]
        {
            get
            {
                return _csvReader[fieldName];
            }
        }

        public string this[int fieldIndex]
        {
            get
            {
                return _csvReader[fieldIndex];
            }
        }
        #endregion

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
                if (null != _csvReader)
                {
                    _csvReader.Dispose();
                    _csvReader = null;
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
