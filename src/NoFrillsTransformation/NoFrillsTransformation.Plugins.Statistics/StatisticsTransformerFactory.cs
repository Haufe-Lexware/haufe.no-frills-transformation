using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Statistics
{
    [Export(typeof(NoFrillsTransformation.Interfaces.ISourceTransformerFactory))]
    public class StatisticsTransformerFactory : ISourceTransformerFactory
    {
        private const string PREFIX = "stats://";

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
            string t = source.Substring(PREFIX.Length).ToLowerInvariant(); // strip prefix
            switch (t)
            {
                case "frequency":
                    return CreateFrequencyTransformer(context, t, config, parameters, settings);
            }
            throw new ArgumentException("Plugin.Statistics: Could not find transform for source '" + t + "'.");
        }

        private ISourceTransformer CreateFrequencyTransformer(IContext context, string t, string config, IParameter[] parameters, ISetting[] settings)
        {
            string target = FindTarget(context, settings);
            return new FrequencyTransformer(context, target, config, parameters);
        }

        private string FindTarget(IContext context, ISetting[] settings)
        {
            foreach (var setting in settings)
            {
                if (setting.Name.Equals("target", StringComparison.InvariantCultureIgnoreCase))
                {
                    var fileName = ResolveTarget(context, setting.Setting);
                    return fileName;
                }
            }
            throw new ArgumentException("Plugin.Statistics: Could not find 'target' setting defining the output file name (as a CSV file).");
        }

        private string ResolveTarget(IContext context, string target)
        {
            if (!target.ToLowerInvariant().StartsWith("file://"))
                throw new ArgumentException("Plugin.Statistics: Target setting is expected to start with 'file://'.");
            return "file://" + context.ResolveFileName(context.ReplaceParameters(target.Substring(7)), false);
        }
    }
}
