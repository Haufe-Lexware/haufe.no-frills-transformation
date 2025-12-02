using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Ado
{
    public class AdoBase
    {
        protected static string GetConfig(IContext context, string? config)
        {
            if (string.IsNullOrEmpty(config))
                return string.Empty;
            // Replace any parameters (which may be part of a file name or so).
            config = context.ReplaceParameters(config);
            // Plain text
            if (!config.StartsWith("@"))
                return config;
            // Starts with @, assume it's a file name
            var fileName = context.ResolveFileName(config.Substring(1)); // strip @

            var configString = File.ReadAllText(fileName);

            // And also replace parameters inside file
            return context.ReplaceParameters(configString);
        }
    }
}
