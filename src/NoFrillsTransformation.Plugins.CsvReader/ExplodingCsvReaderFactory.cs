using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Csv
{
    [Export(typeof(NoFrillsTransformation.Interfaces.ISourceReaderFactory))]
    public class ExplodingCsvReaderFactory : ISourceReaderFactory
    {
        public bool CanReadSource(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return false;
            string temp = source.ToLowerInvariant();
            if (!temp.StartsWith("csv.explode://"))
                return false;
            if (!temp.EndsWith(".csv") && !temp.EndsWith(".txt"))
                return false;
            return true;
        }

        public ISourceReader CreateReader(IContext context, string source, string? config)
        {
            ExplodingCsvReader? reader = null;
            try
            {
                context.Logger.Info("ExplodingCsvReaderFactory: Attempting to create a ExplodingCsvReader.");
                
                reader = new ExplodingCsvReader(context, source, config);
            }
            catch (Exception)
            {
                if (null != reader)
                    reader.Dispose();
                reader = null;
                throw;
            }
            context.Logger.Info("ExplodingCsvReaderFactory: Successfully created a ExplodingCsvReader."); 
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
