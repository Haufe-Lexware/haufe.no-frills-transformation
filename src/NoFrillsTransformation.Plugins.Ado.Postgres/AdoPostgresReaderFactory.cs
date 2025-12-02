using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Ado.Postgres
{
    [Export(typeof(NoFrillsTransformation.Interfaces.ISourceReaderFactory))]
    public class AdoPostgresReaderFactory : ISourceReaderFactory
    {
        private const string PREFIX = "postgres://";

        public bool CanReadSource(string source)
        {
            source = source.ToLowerInvariant();
            return source.StartsWith(PREFIX);
        }

        public ISourceReader CreateReader(IContext context, string source, string? config)
        {
            string sqlQuery = source.Substring(PREFIX.Length); // strip PREFIX
            return new AdoPostgresReader(context, config, sqlQuery);
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
