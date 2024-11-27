using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using NoFrillsTransformation.Interfaces;
using NoFrillsTransformation.Plugins.Salesforce.Config;

namespace NoFrillsTransformation.Plugins.Salesforce
{
    public class SfdcBaseFactory
    {
        protected static SfdcConfig ParseConfig(IContext context, string configFile)
        {
            try
            {
                string resolvedPath = context.ResolveFileName(configFile);
                XmlSerializer serializer = new XmlSerializer(typeof(SfdcConfig));
                using (var fs = new FileStream(resolvedPath, FileMode.Open))
                {
                    var config = (SfdcConfig)serializer.Deserialize(fs);
                    config.DataLoaderDir = context.ReplaceParameters(config.DataLoaderDir);
                    config.LogFileDir = context.ResolveFileName(context.ReplaceParameters(config.LogFileDir), false);
                    config.SuccessFileName = context.ResolveFileName(context.ReplaceParameters(config.SuccessFileName), false);
                    config.ErrorFileName = context.ResolveFileName(context.ReplaceParameters(config.ErrorFileName), false);
                    config.SfdcEncryptedPassword = context.ReplaceParameters(config.SfdcEncryptedPassword);
                    config.SfdcEndPoint = context.ReplaceParameters(config.SfdcEndPoint);
                    config.SfdcUsername = context.ReplaceParameters(config.SfdcUsername);

                    if (context.Parameters.ContainsKey("sfdckeeptempfiles"))
                    {
                        config.KeepTempFiles = SfdcBool(context.Parameters["sfdckeeptempfiles"]);
                        context.Logger.Info("SFDC Settings: Override KeepTempFiles=" + config.KeepTempFiles);
                    }
                    if (context.Parameters.ContainsKey("sfdcfailonerrors"))
                    {
                        config.FailOnErrors = SfdcBool(context.Parameters["sfdcfailonerrors"]);
                        context.Logger.Info("SFDC Settings: Override FailOnErrors=" + config.KeepTempFiles);
                    }

                    return config;
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Could not read SFDC configuration file: " + configFile + ", error message: " + ex.Message);
            }
        }

        private static  bool SfdcBool(string boolString)
        {
            switch (boolString.ToLowerInvariant())
            {
                case "yes":
                case "on":
                case "1":
                case "true":
                    return true;
            }
            return false;
        }

        public static SoqlQuery ParseQuery(string soql)
        {
            string[] parts = soql.Split(' ');

            if (parts.Length == 0)
                throw new ArgumentException("Invalid SOQL query: " + soql);

            if (!parts[0].Equals("select", StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("Invalid SOQL query. Must start with 'Select': " + soql);

            var q = new SoqlQuery();

            bool hasFoundFrom = false;
            bool hasFoundEntity = false;
            var fields = new List<string>();
            for (int i = 1; i < parts.Length; ++i)
            {
                string f = parts[i].Trim();
                if (hasFoundFrom)
                {
                    q.Entity = f;
                    hasFoundEntity = true;
                    break;
                }
                else if (f.Equals("from", StringComparison.InvariantCultureIgnoreCase))
                {
                    hasFoundFrom = true;
                    continue;
                }

                f = f.Replace(",", "");
                if (string.IsNullOrWhiteSpace(f))
                    continue;

                fields.Add(f);
            }

            if (!hasFoundFrom)
                throw new ArgumentException("Invalid SOQL query, keyword 'From' was not found: " + soql);
            if (!hasFoundEntity)
                throw new ArgumentException("Invalid SOQL query; query entity was not found after 'From' keyword: " + soql);

            q.FieldNames = fields.ToArray();
            q.Soql = soql;

            return q;
        }

        // Typical target strings:
        // sfdc://Account.insert
        // sfdc://User.upsert:ImportedSapGuid__c
        protected static SfdcTarget ParseTarget(string protocol, string target)
        {
            try
            {
                // Strip sfdc://
                string t = target.Substring(protocol.Length);
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
                throw new ArgumentException("Malformed SFDC target string. Expected '" + protocol + "<Entity>.<operation>[:<externalId>]', whereas <operation> is 'insert', 'update', 'upsert', 'delete' or 'hard_delete', and the optional <externalId> can be passed for the 'upsert' operation.");
            }
        }
    }
}
