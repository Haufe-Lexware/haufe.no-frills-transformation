using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine
{
    class ReaderFactory
    {
        public ReaderFactory()
        {
            var catalog = new DirectoryCatalog(".");
            var container = new CompositionContainer(catalog);
            container.ComposeParts(this);
        }


#pragma warning disable 0649
        [ImportMany(typeof(NoFrillsTransformation.Interfaces.ISourceReaderFactory))]
        private ISourceReaderFactory[] _readerFactories;
#pragma warning restore 0649

        public ISourceReader CreateReader(string source, string config)
        {
            foreach (var rf in _readerFactories)
            {
                if (rf.CanReadSource(source))
                    return rf.CreateReader(source, config);
            }
            throw new InvalidOperationException("Could not find a suitable reader for source '" + source + "'.");
        }
    }
}
