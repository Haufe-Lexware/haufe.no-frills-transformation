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
        public bool CanWriteTarget(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
                return false;
            string temp = target.ToLowerInvariant();
            if (!temp.StartsWith("sfdc://"))
                return false;
            //if (!temp.EndsWith(".csv") && !temp.EndsWith(".txt"))
            //    return false;
            return true;
        }

        public ITargetWriter CreateWriter(IContext context, string target, string[] fieldNames, int[] fieldSizes, string config)
        {
            context.Logger.Info("CsvWriterFactory: Creating a CsvWriterPlugin.");
            var sfdcTarget = ParseTarget(target);
            var sfdcConfig = ParseConfig(context, config);
            return new SfdcWriter(context, sfdcTarget, fieldNames, sfdcConfig);
        }

        // Typical target strings:
        // sfdc://Account.insert
        // sfdc://User.upsert:ImportedSapGuid__c
        private static SfdcTarget ParseTarget(string target)
        {
            try
            {
                // Strip sfdc://
                string t = target.Substring(7);
                int dotIndex = t.IndexOf('.');
                if (dotIndex < 0)
                    throw new ArgumentException();
                string entity = t.Substring(0, dotIndex);
                string operation = null;
                string externalId = null;
                int parIndex = t.IndexOf(':');
                if (parIndex > 0)
                {
                    operation = t.Substring(dotIndex + 1, parIndex - dotIndex - 1);
                    externalId = t.Substring(parIndex + 1);
                }
                else
                {
                    operation = t.Substring(dotIndex + 1);
                }
                operation = operation.ToLowerInvariant();
                if (!operation.Equals("insert", StringComparison.InvariantCultureIgnoreCase)
                    && !operation.Equals("update", StringComparison.InvariantCultureIgnoreCase)
                    && !operation.Equals("upsert", StringComparison.InvariantCultureIgnoreCase)
                    && !operation.Equals("delete", StringComparison.InvariantCultureIgnoreCase)
                    && !operation.Equals("hard_delete", StringComparison.InvariantCultureIgnoreCase))
                    throw new ArgumentException();

                return new SfdcTarget
                {
                    Entity = entity,
                    Operation = operation,
                    ExternalId = externalId
                };
            }
            catch (Exception)
            {
                throw new ArgumentException("Malformed SFDC target string. Expected 'sfdc://<Entity>.<operation>[:<externalId>]', whereas <operation> is 'insert', 'update', 'upsert', 'delete' or 'hard_delete', and the optional <externalId> can be passed for the 'upsert' operation.");
            }
        }
    }
}
