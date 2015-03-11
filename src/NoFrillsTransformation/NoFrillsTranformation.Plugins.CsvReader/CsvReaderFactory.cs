using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTranformation.Plugins.CsvReader
{
    [Export(typeof(NoFrillsTransformation.Interfaces.ISourceReaderFactory))]
    public class CsvReaderFactory : ISourceReaderFactory
    {
        public bool CanReadSource(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return false;
            string temp = source.ToLowerInvariant();
            if (!temp.StartsWith("file://"))
                return false;
            if (!temp.EndsWith(".csv") && !temp.EndsWith(".txt"))
                return false;
            return true;
        }

        public ISourceReader CreateReader(string source, string config)
        {
            CsvReaderPlugin reader = null;
            try
            {
                reader = new CsvReaderPlugin(source, config);
            }
            catch (Exception)
            {
                if (null != reader)
                    reader.Dispose();
                reader = null;
                throw;
            }
            return reader;
        }

        // 
        public bool SupportsQuery
        {
            get
            {
                return false;
            }
        }
    }
}
