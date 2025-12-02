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
    public class SfdcReaderFactory : SfdcBaseFactory, ISourceReaderFactory
    {
        public bool CanReadSource(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return false;
            string temp = source.ToLowerInvariant();
            if (!temp.StartsWith("soql://"))
                return false;
            return true;
        }

        public ISourceReader CreateReader(IContext context, string source, string config)
        {
            string soql = source.Substring(7);
            var soqlQuery = ParseQuery(soql);
            var sfdcConfig = ParseConfig(context, config);
            SfdcReader sfdcReader = null;
            try
            {
                context.Logger.Info("SfdcReaderFactory: Attempting to create an SfdcReader."); 
                
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
    }
}
