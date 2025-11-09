using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Json
{
    [Export(typeof(NoFrillsTransformation.Interfaces.ISourceReaderFactory))]
    public class JsonReaderFactory : ISourceReaderFactory
    {
        public bool SupportsQuery => false;


        public bool CanReadSource(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return false;
            string temp = source.ToLowerInvariant();
            if (!temp.StartsWith("json://") && !temp.StartsWith("file://"))
                return false;
            if (!temp.EndsWith(".json"))
                return false;
            return true;
        }

        public ISourceReader CreateReader(IContext context, string source, string? config)
        {
            context.Logger.Info("JsonReaderFactory: Creating a JsonReader.");
            return new JsonReader(context, source, config);
        }
    }
}
