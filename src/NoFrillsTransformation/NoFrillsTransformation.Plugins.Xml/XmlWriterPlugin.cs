using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Xml
{
    internal class XmlWriterPlugin : ITargetWriter
    {
        private IContext _context;
        private string _fileName;
        private string[] _fieldNames;
        private int[] _fieldSizes;
        private string _config;
        private XmlTextWriter _xmlWriter;
        private int _recordsWritten = 0;

        public XmlWriterPlugin(IContext context, string target, string[] fieldNames, int[] fieldSizes, string config)
        {
            this._context = context;
            var tempFileName = target.StartsWith("xml") ? target.Substring(6) : target.Substring(7); // xml:// or file://
            this._fileName = context.ResolveFileName(tempFileName, false);
            this._fieldNames = fieldNames;
            this._fieldSizes = fieldSizes;
            this._config = config;

            _xmlWriter = new XmlTextWriter(_fileName, Encoding.UTF8);
            _xmlWriter.WriteStartDocument();
            _xmlWriter.WriteStartElement("Table");
            _xmlWriter.WriteStartElement("Entries");
        }

        public void WriteRecord(string[] fieldValues)
        {
            _xmlWriter.WriteStartElement("Entry");
            for (int i=0; i<_fieldNames.Length; ++i)
            {
                _xmlWriter.WriteStartElement(_fieldNames[i]);
                _xmlWriter.WriteString(fieldValues[i]);
                _xmlWriter.WriteEndElement();
            }
            _xmlWriter.WriteEndElement(); // Entry
            _recordsWritten++;
        }

        public int RecordsWritten
        {
            get 
            {
                return _recordsWritten;
            }
        }

        public void FinishWrite()
        {
            _xmlWriter.WriteEndElement(); // Entries
            _xmlWriter.WriteEndElement(); // Table
            _xmlWriter.Close();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (null != _xmlWriter)
                {
                    _xmlWriter.Close();
                    _xmlWriter = null;
                }
            }
            // free native resources if there are any.
            //if (nativeResource != IntPtr.Zero) 
            //{
            //    Marshal.FreeHGlobal(nativeResource);
            //    nativeResource = IntPtr.Zero;
            //}
        }
    }
}
