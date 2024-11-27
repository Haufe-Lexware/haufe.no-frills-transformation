using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Salesforce
{
    [Export(typeof(NoFrillsTransformation.Interfaces.ITargetWriterFactory))]
    public class SfdcWriterFactory : SfdcBaseFactory, ITargetWriterFactory
    {
        private const string PROTOCOL = "sfdc://";

        public bool CanWriteTarget(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
                return false;
            string temp = target.ToLowerInvariant();
            if (!temp.StartsWith(PROTOCOL))
                return false;
            //if (!temp.EndsWith(".csv") && !temp.EndsWith(".txt"))
            //    return false;
            return true;
        }

        public ITargetWriter CreateWriter(IContext context, string target, IFieldDefinition[] fieldDefs, string config)
        {
            context.Logger.Info("SfdcWriterFactory: Creating a SfdcWriter.");
            var sfdcTarget = ParseTarget(PROTOCOL, target);
            var sfdcConfig = ParseConfig(context, config);
            return new SfdcWriter(context, sfdcTarget, fieldDefs, sfdcConfig);
        }
    }
}
