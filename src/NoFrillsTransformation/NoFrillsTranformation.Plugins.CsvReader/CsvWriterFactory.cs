using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTranformation.Plugins.Csv
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

        public ITargetWriter CreateWriter(string target, string[] fieldNames, int[] fieldSizes, string config)
        {
            return null;
        }
    }
}
