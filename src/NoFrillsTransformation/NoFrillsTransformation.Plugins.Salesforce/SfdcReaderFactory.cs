using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using NoFrillsTransformation.Interfaces;
using NoFrillsTransformation.Plugins.Salesforce.Config;

namespace NoFrillsTransformation.Plugins.Salesforce
{
    [Export(typeof(NoFrillsTransformation.Interfaces.ISourceReaderFactory))]
    public class SfdcReaderFactory : ISourceReaderFactory
    {
        public bool CanReadSource(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return false;
            string temp = source.ToLowerInvariant();
            if (!temp.StartsWith("soql://"))
                return false;
            //if (!temp.EndsWith(".csv") && !temp.EndsWith(".txt"))
            //    return false;
            return true;
        }

        public ISourceReader CreateReader(IContext context, string source, string config)
        {
            string soql = source.Substring(7);
            var soqlQuery = ParseQuery(soql);
            var sfdcConfig = ParseConfig(config);
            SfdcReader sfdcReader = null;
            try
            {
                context.Logger.Info("SfdcReaderFactory: Attempting to create a SfdcReader."); 
                
                sfdcReader = new SfdcReader(context, soqlQuery, sfdcConfig);
                sfdcReader.Initialize();
            }
            catch (Exception ex)
            {
                if (null != sfdcReader)
                {
                    sfdcReader.Dispose();
                    sfdcReader = null;
                }
                throw new InvalidOperationException("An error occurred while creating the SfdcReader: " + ex.Message);
            }

            context.Logger.Info("SfdcReaderFactory: Successfully created a SfdcReader.");

            return sfdcReader;
        }

        // 
        public bool SupportsQuery
        {
            get
            {
                return false;
            }
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
            for (int i=1; i<parts.Length; ++i)
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

        private static SfdcReaderConfig ParseConfig(string configFile)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(SfdcReaderConfig));
                using (var fs = new FileStream(configFile, FileMode.Open))
                {
                    var config = (SfdcReaderConfig)serializer.Deserialize(fs);
                    return config;
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Could not read SfdcReader configuration file: " + configFile + ", error message: " + ex.Message);
            }
        }

    }

    public class SoqlQuery
    {
        public string Soql { get; set; }
        public string Entity { get; set; }
        public string[] FieldNames { get; set; }
    }
}
