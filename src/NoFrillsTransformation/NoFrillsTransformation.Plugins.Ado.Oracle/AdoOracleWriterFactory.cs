using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Ado.Oracle
{
    [Export(typeof(NoFrillsTransformation.Interfaces.ITargetWriterFactory))]
    public class AdoOracleWriterFactory : ITargetWriterFactory
    {
        private const string PREFIX = "oracle://";
        
        public bool CanWriteTarget(string target)
        {
            target = target.ToLowerInvariant();
            return target.StartsWith(PREFIX);
        }

        public ITargetWriter CreateWriter(IContext context, string target, IFieldDefinition[] fieldDefs, string config)
        {
            string table = target.Substring(PREFIX.Length);
            return new AdoOracleWriter(context, config, table, fieldDefs);
        }
    }
}
