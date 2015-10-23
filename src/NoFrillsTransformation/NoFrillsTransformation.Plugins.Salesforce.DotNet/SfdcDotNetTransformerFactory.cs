using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Salesforce.DotNet
{
    [Export(typeof(NoFrillsTransformation.Interfaces.ISourceTransformerFactory))]
    public class SfdcDotNetTransformerFactory : SfdcBaseFactory, ISourceTransformerFactory
    {
        private const string PREFIX = "soql.net://";

        public bool CanPerformTransformation(string transform)
        {
            if (string.IsNullOrWhiteSpace(transform))
                return false;
            string temp = transform.ToLowerInvariant();
            if (!temp.StartsWith(PREFIX))
                return false;
            return true;
        }

        public ISourceTransformer CreateTransformer(IContext context, string source, string config, IParameter[] parameters, ISetting[] settings)
        {
            string soql = source.Substring(11);
            var soqlQuery = ParseQuery(soql);
            var sfdcConfig = ParseConfig(context, config);

            return new SfdcDotNetTransformer(context, sfdcConfig, soqlQuery, parameters, settings);
        }
    }
}
