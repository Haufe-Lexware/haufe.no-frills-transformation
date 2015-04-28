using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Csv
{
    [Export(typeof(NoFrillsTransformation.Interfaces.ITargetWriterFactory))]
    public class CsvWriterFactory : ITargetWriterFactory
    {
        public bool CanWriteTarget(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
                return false;
            string temp = target.ToLowerInvariant();
            if (!temp.StartsWith("file://"))
                return false;
            if (!temp.EndsWith(".csv") && !temp.EndsWith(".txt"))
                return false;
            return true;
        }

        public ITargetWriter CreateWriter(IContext context, string target, string[] fieldNames, int[] fieldSizes, string config)
        {
            context.Logger.Info("CsvWriterFactory: Creating a CsvWriterPlugin.");
            return new CsvWriterPlugin(target, fieldNames, fieldSizes, config);
        }
    }
}
