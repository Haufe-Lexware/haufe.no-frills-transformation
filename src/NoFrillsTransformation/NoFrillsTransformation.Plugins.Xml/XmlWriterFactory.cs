using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Xml
{
    [Export(typeof(NoFrillsTransformation.Interfaces.ITargetWriterFactory))]
    public class XmlWriterFactory : ITargetWriterFactory
    {
        public bool CanWriteTarget(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
                return false;
            string temp = target.ToLowerInvariant();
            if (!temp.StartsWith("xml://"))
                return false;
            if (!temp.EndsWith(".xml"))
                return false;
            return true;
        }

        public ITargetWriter CreateWriter(IContext context, string target, string[] fieldNames, int[] fieldSizes, string config)
        {
            context.Logger.Info("XmlWriterFactory: Creating a XmlWriterPlugin.");
            return new XmlWriterPlugin(context, target, fieldNames, fieldSizes, config);
        }
    }
}
