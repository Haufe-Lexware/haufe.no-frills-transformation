using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Inline
{
    class InlineReader : ISourceReader, IRecord
    {
        public InlineReader(IContext context, string source, string? config)
        {
            if (null == config)
                throw new ArgumentNullException("InlineReader: Configuration is required.");
            _config = config;
            _fieldNames = new String[] { config };
            _context = context;

            _source = source.Substring("inline://".Length).Split(new char[] { ';' });
        }

        private string _config;
        private IContext _context;
        private string[] _source;
        private int _pos = -1;

        public bool IsEndOfStream
        {
            get { return (_pos >= _source.Length); }
        }

        public void NextRecord()
        {
            _pos++;
        }

        public IRecord CurrentRecord
        {
            get { return this; }
        }

        public int FieldCount
        {
            get { return 1; }
        }

        private string[] _fieldNames;
        public string[] FieldNames
        {
            get { return _fieldNames; }
        }

        public int GetFieldIndex(string fieldName)
        {
            if (!_config.Equals(fieldName, StringComparison.CurrentCultureIgnoreCase))
                throw new ArgumentException("InlineReader: Unknown field name '" + fieldName + "'.");
            return 0;
        }

        public IRecord Query(string key)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        public string this[int index]
        {
            get
            {
                if (index == 0)
                    return _source[_pos];
                throw new InvalidOperationException("InlineReader: Read past end of inline input stream (position " + _pos + ").");
            }
        }

        public string this[string fieldName]
        {
            get
            {
                return this[GetFieldIndex(fieldName)];
            }
        }
    }
}
