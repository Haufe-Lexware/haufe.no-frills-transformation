using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Acumatica
{
    [Export(typeof(NoFrillsTransformation.Interfaces.ISourceReaderFactory))]
    public class AcumaticaReaderFactory : ISourceReaderFactory
    {
        public bool SupportsQuery => false;


        public bool CanReadSource(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return false;
            string temp = source.ToLowerInvariant();
            if (!temp.StartsWith("acumatica.xml://"))
                return false;
            if (!temp.EndsWith(".xml"))
                return false;
            return true;
        }

        public ISourceReader CreateReader(IContext context, string source, string? config)
        {
            context.Logger.Info("AcumaticaReaderFactory: Creating an AcumaticaReader.");
            return new AcumaticaReader(context, source, config);
        }
    }
}
