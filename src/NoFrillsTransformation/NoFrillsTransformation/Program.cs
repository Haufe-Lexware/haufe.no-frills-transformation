using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using LumenWorks.Framework.IO.Csv;
using NoFrillsTransformation.Config;
using NoFrillsTransformation.Engine;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation
{
    class Program
    {
        enum ExitCodes
        {
            Success = 0,
            Failure = 1,
        }

        static int Main(string[] args)
        {
            try
            {
                if (VerifyArguments(args))
                    (new Program()).Run(args[0]);
                Console.WriteLine("Operation finished successfully.");

                return (int)ExitCodes.Success;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Operation failed: " + e.Message);
                return (int)ExitCodes.Failure;
            }
        }

        private static bool VerifyArguments(string[] args)
        {
            if (args.Length == 0)
                throw new ArgumentException("Missing argument. Use -help for instructions.");
            if (args[0].Equals("-help"))
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  NoFrillsTransformation.exe <config file>");
                return false;
            }
            return true;
        }

        private void Run(string configFileName)
        {
            var configFile = ReadConfigFile(configFileName);

            // Set up MEF
            var catalog = new DirectoryCatalog(".");
            var container = new CompositionContainer(catalog);

            var readerFactory = new ReaderFactory(container);
            var writerFactory = new WriterFactory(container);
            var operatorFactory = new OperatorFactory(container);

            Context context = new Context();
            InitOperators(configFile, context, operatorFactory);
            InitLookupMaps(configFile, readerFactory, context);

            ReadFilters(configFile, context);
            ReadMappings(configFile, context);

            using (var reader = readerFactory.CreateReader(configFile.Source.Uri, configFile.Source.Config))
            {
                context.SourceReader = reader;
                using (var writer = writerFactory.CreateWriter(configFile.Target.Uri, context.TargetFields, configFile.Target.Config))
                {
                    context.TargetWriter = writer;

                    Process(context);
                }
            }
        }

        private void InitOperators(ConfigFileXml config, Context context, OperatorFactory operatorFactory)
        {
            //var opFactory = new OperatorFactory(container);
            foreach (var op in operatorFactory.Operators)
            {
                if (context.Operators.ContainsKey(op.Name))
                    throw new InvalidOperationException("An operator with the name '" + op.Name + "' is already defined. Operators' names must be unique.");
                context.Operators[op.Name] = op;
            }

            // Do we have operator configurations?
            if (null != config.OperatorConfigs)
            {
                foreach (var opConfig in config.OperatorConfigs)
                {
                    if (!context.Operators.ContainsKey(opConfig.Name))
                        throw new InvalidOperationException("Configuration was passed in config XML for unknown operator '" + opConfig.Name + ". Are your plugins in the same folder as the executable?");
                    context.Operators[opConfig.Name].Configure(opConfig.Config);
                }
            }
        }

        private static ConfigFileXml ReadConfigFile(string configFileName)
        {
            ConfigFileXml configFile = null;
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(ConfigFileXml));
                using (var fs = new FileStream(configFileName, FileMode.Open))
                {
                    configFile = xmlSerializer.Deserialize(fs) as ConfigFileXml;
                }
            }
            catch (Exception e)
            {
                throw new ArgumentException("Could not read XML config file: " + e.Message);
            }
            return configFile;
        }

        private void Process(Context context)
        {
            try
            {
                string[] outValues = new string[context.TargetFields.Length];

                var evaluator = new ExpressionParser();

                while (!context.SourceReader.IsEndOfStream)
                {
                    context.SourceReader.NextRecord();
                    context.SourceRecordsRead++;

                    if (!RecordMatchesFilter(evaluator, context))
                    {
                        context.SourceRecordsFiltered++;
                        continue;
                    }
                    context.SourceRecordsProcessed++;

                    for (int i = 0; i < outValues.Length; ++i)
                    {
                        outValues[i] = evaluator.Evaluate(evaluator, context.TargetFields[i].Expression, context);
                    }

                    context.TargetWriter.WriteRecord(outValues);
                    context.TargetRecordsWritten++;
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("A processing error occurred in source row " + context.SourceRecordsRead + ": " + e.Message);
            }
        }

        private bool RecordMatchesFilter(IEvaluator eval, Context context)
        {
            if (null == context.Filters)
                return true;
            foreach (var filter in context.Filters)
            {
                bool val = ExpressionParser.StringToBool(eval.Evaluate(eval, filter.Expression, context));
                if (FilterMode.And == context.FilterMode
                    && !val)
                    return false;
                if (FilterMode.Or == context.FilterMode
                    && val)
                    return true;
            }
            return (FilterMode.And == context.FilterMode);
        }

        private void InitLookupMaps(ConfigFileXml configFile, ReaderFactory readerFactory, Context context)
        {
            if (null == configFile.LookupMaps)
                return; // No lookup maps
            foreach (var lookupMap in configFile.LookupMaps)
            {
                context.LookupMaps.Add(lookupMap.Name, LookupMapFactory.CreateLookupMap(lookupMap, readerFactory));
                string nameLow = lookupMap.Name.ToLowerInvariant();
                if (context.Operators.ContainsKey(nameLow))
                    throw new InvalidOperationException("Duplicate use of operator '" + lookupMap.Name + "' for lookup maps; please rename the lookup map.");
                context.Operators[nameLow] = new NoFrillsTransformation.Engine.Operators.LookupOperator(nameLow);
            }
        }

        private void ReadFilters(ConfigFileXml configFile, Context context)
        {
            try
            {
                context.FilterMode = FilterMode.And; // Default to and
                if (!string.IsNullOrWhiteSpace(configFile.FilterMode))
                {
                    string mode = configFile.FilterMode.ToLower();
                    if (mode.Equals("or"))
                        context.FilterMode = FilterMode.Or;
                }
                if (null == configFile.SourceFilters
                    || configFile.SourceFilters.Length == 0)
                    return; // No filters
                int filterCount = configFile.SourceFilters.Length;
                context.Filters = new FilterDef[filterCount];
                for (int i = 0; i < filterCount; ++i)
                {
                    var filterXml = configFile.SourceFilters[i];
                    context.Filters[i] = new FilterDef { Expression = ExpressionParser.ParseExpression(filterXml.Expression, context) };
                    if (context.Filters[i].Expression.Operator.ReturnType != ParamType.Bool)
                        throw new InvalidOperationException("Source filter expression mismatch: Expression '" + 
                            filterXml.Expression + "' does not evaluate to a boolean value.");
                }
            }
            catch (Exception e)
            {
                throw new ArgumentException("Could not read/parse filter definitions: " + e.Message);
            }
        }

        private void ReadMappings(ConfigFileXml configFile, Context context)
        {
            if (null == configFile.Mappings)
                throw new ArgumentException("Configuration file misses Mappings section.");
            if (configFile.Mappings.Length == 0)
                throw new ArgumentException("Configuration file does not have a valid Mapping (<Mapping> tag missing).");
            if (configFile.Mappings.Length > 1)
                throw new ArgumentException("Multiple Mapping tags are not allowed currently.");
            var map = configFile.Mappings[0]; // Pick first mapping; it might be extended later on.
            if (null == map.Fields)
                throw new ArgumentException("Missing field definitions in mapping.");
            int fieldCount = map.Fields.Length;

            //context.TargetFieldNames = new string[] { };
            //context.TargetFieldSizes = new int[] { }; 
            context.TargetFields = new TargetFieldDef[fieldCount];

            for (int i = 0; i < fieldCount; ++i)
            {
                var field = map.Fields[i];
                try
                {
                    var tfd = new TargetFieldDef();
                    tfd.FieldName = field.Name;
                    tfd.FieldSize = field.MaxSize;
                    tfd.Expression = ExpressionParser.ParseExpression(field.Expression, context);

                    context.TargetFields[i] = tfd;
                }
                catch (Exception e)
                {
                    throw new ArgumentException("An error occurred while parsing field '" + field.Name + "': " + e.Message);
                }
            }
        }
    }
}
