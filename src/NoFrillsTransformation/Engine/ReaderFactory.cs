using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine
{
    class ReaderFactory
    {
        public ReaderFactory(CompositionHost container)
        {
            container.SatisfyImports(this);
        }


        [ImportMany]
        private ISourceReaderFactory[]? ReaderFactories { get; set; }

        public ISourceReader CreateReader(IContext context, string source, string? config)
        {
            if (null == ReaderFactories)
                throw new InvalidOperationException("No reader factories found.");
            foreach (var rf in ReaderFactories)
            {
                if (rf.CanReadSource(source))
                    return rf.CreateReader(context, source, config);
            }
            throw new InvalidOperationException("Could not find a suitable reader for source '" + source + "'.");
        }
    }
}
