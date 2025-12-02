using System;
using System.Composition;
using System.Text.RegularExpressions;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Acumatica.Operators
{
    [Export(typeof(IOperator))]
    public class AcumaticaWikiReplaceOperator : IOperator
    {
        public string Name => "acuwikireplace";

        public string Description => "Replaces images and upload files in WikiRevision with the deduplicated values from a lookup map. Requires the LookupMap to have a field named 'TargetName'.";

        public ExpressionType Type => ExpressionType.Custom;

        public int ParamCount => 2;

        public ParamType[]? ParamTypes => new ParamType[] { ParamType.String, ParamType.String };

        public ParamType ReturnType => ParamType.String;

        public void Configure(string? config)
        {
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            string lookupMapName = eval.Evaluate(eval, expression.Arguments[0], context);
            string originalValue = eval.Evaluate(eval, expression.Arguments[1], context);

            if (!context.HasLookupMap(lookupMapName))
            {
                throw new InvalidOperationException($"AcumaticaWikiReplaceOperator: Lookup map '{lookupMapName}' does not exist.");
            }

            var lookupMap = context.GetLookupMap(lookupMapName);
            // Replace image links; use a regex to find all occurrences of [image:<filename>|...]
            var newValue = Regex.Replace(originalValue, @"\[image:([a-zA-Z/\\.\-_0-9]+)\|", match =>
            {
                string originalFileName = match.Groups[1].Value;
                if (lookupMap.HasKey(originalFileName))
                {
                    string newFileName = lookupMap.GetValue(originalFileName, "TargetName");
                    return $"[image:{newFileName}|";
                }
                return match.Value; // No replacement found, return original
            });
            // Replace upload file links; use a regex to find all occurrences of [{up}<filename>|...]
            newValue = Regex.Replace(newValue, @"\[\{up\}([a-zA-Z/\\.\-_0-9]+)\|", match =>
            {
                string originalFileName = match.Groups[1].Value;
                if (lookupMap.HasKey(originalFileName))
                {
                    string newFileName = lookupMap.GetValue(originalFileName, "TargetName");
                    return $"[{{up}}{newFileName}|";
                }
                return match.Value; // No replacement found, return original
            });
            return newValue;
        }
    }
}