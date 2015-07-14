using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Inline
{
    [Export(typeof(NoFrillsTransformation.Interfaces.ISourceReaderFactory))]
    public class InlineReaderFactory : ISourceReaderFactory
    {
        public bool CanReadSource(string source)
        {
            return source.ToLowerInvariant().StartsWith("inline://");
        }

        public ISourceReader CreateReader(IContext context, string source, string config)
        {
            return new InlineReader(context, source, config);
        }

        public bool SupportsQuery
        {
            get { return false; }
        }
    }
}
