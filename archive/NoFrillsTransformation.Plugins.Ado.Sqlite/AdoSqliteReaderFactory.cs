using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Ado.Sqlite
{
    [Export(typeof(NoFrillsTransformation.Interfaces.ISourceReaderFactory))]
    public class AdoSqliteReaderFactory : ISourceReaderFactory
    {
        private const string PREFIX = "sqlite://";

        public bool CanReadSource(string source)
        {
            source = source.ToLowerInvariant();
            return source.StartsWith(PREFIX);
        }

        public ISourceReader CreateReader(IContext context, string source, string config)
        {
            string sqlQuery = source.Substring(PREFIX.Length); // strip PREFIX
            return new AdoSqliteReader(context, config, sqlQuery);
        }

        public bool SupportsQuery
        {
            get
            {
                return false;
            }
        }
    }
}
