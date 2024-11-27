using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;
using NoFrillsTransformation.Plugins.Sap.Config;

namespace NoFrillsTransformation.Plugins.Sap
{
    [Export(typeof(NoFrillsTransformation.Interfaces.ISourceTransformerFactory))]
    public class SapTransformerFactory : ISourceTransformerFactory
    {
        public bool CanPerformTransformation(string transform)
        {
            if (string.IsNullOrWhiteSpace(transform))
                return false;
            string temp = transform.ToLowerInvariant();
            if (!temp.StartsWith("sap://"))
                return false;
            //if (!temp.EndsWith(".csv") && !temp.EndsWith(".txt"))
            //    return false;
            return true;
        }

        public ISourceTransformer CreateTransformer(IContext context, string source, string config, IParameter[] parameters, ISetting[] settings)
        {
            string rfcName = ParseRfcName(source);
            var sapConfig = SapConfig.ParseConfig(context, config);

            return new SapTransformer(context, sapConfig, rfcName, parameters, settings);
        }

        private static string ParseRfcName(string source)
        {
            string t = source.Substring(6); // strip sap://
            if (!t.StartsWith("rfc:", StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("For the SapTransformer, only RFC destinations (sap://RFC:<RFC Name>) are currently supported. Got: " + source);
            t = t.Substring(4); // strip RFC:

            return t;
        }
    }
}
