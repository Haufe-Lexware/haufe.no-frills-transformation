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
            if (VerifyArguments(args))
                return (new Program()).Run(args[0]);
            return (int)ExitCodes.Failure;
            //try
            //{
            //    if (VerifyArguments(args))
            //        (new Program()).Run(args[0]);
            //    Console.WriteLine("Operation finished successfully.");

            //    return (int)ExitCodes.Success;
            //}
            //catch (Exception e)
            //{
            //    Console.Error.WriteLine("Operation failed: " + e.Message);
            //    return (int)ExitCodes.Failure;
            //}
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

        private int Run(string configFileName)
        {
            using (Context context = new Context())
            {
                try
                {
                    var configFile = ReadConfigFile(configFileName);

                    // Set up MEF
                    var catalog = new DirectoryCatalog(".");
                    var container = new CompositionContainer(catalog);

                    var loggerFactory = new LoggerFactory(container);
                    var readerFactory = new ReaderFactory(container);
                    var writerFactory = new WriterFactory(container);
                    var operatorFactory = new OperatorFactory(container);

                    InitLogger(configFile, context, loggerFactory);
                    context.Logger.Info("Read configuration file: " + configFileName);
                    InitOperators(configFile, context, operatorFactory);
                    InitLookupMaps(configFile, context, readerFactory);

                    LogOperators(context);

                    ReadFilters(configFile, context);
                    ReadMappings(configFile, context);

                    context.SourceReader = readerFactory.CreateReader(context, configFile.Source.Uri, configFile.Source.Config);
                    if (configFile.OutputFields)
                        return OutputSourceFieldsToConsole(context);

                    context.TargetWriter = writerFactory.CreateWriter(context, configFile.Target.Uri, context.TargetFields, configFile.Target.Config);

                    Process(context);

                    // Explicitly dispose readers and writers to have control
                    // on when these resources are released. If something fails,
                    // this is done in the Dispose() method of Context.
                    context.SourceReader.Dispose();
                    context.SourceReader = null;
                    context.TargetWriter.Dispose();
                    context.TargetWriter = null;

                    context.Logger.Info("Operation finished successfully.");
                    Console.WriteLine("Operation finished successfully.");

                    return (int)ExitCodes.Success;
                }
                catch (Exception ex)
                {
                    if (null != context.Logger)
                        context.Logger.Error("Operation failed: " + ex.Message);
                    Console.Error.WriteLine("Operation failed: " + ex.Message);
                }
                return (int)ExitCodes.Failure;
            }
        }

        private int OutputSourceFieldsToConsole(Context context)
        {
            int[] sizes = new int[context.SourceReader.FieldCount];
            for (int i = 0; i < sizes.Length; ++i)
                sizes[i] = 0;
            int maxCount = 100;

            while (!context.SourceReader.IsEndOfStream
                && context.SourceRecordsRead < maxCount)
            {
                context.SourceReader.NextRecord();
                context.SourceRecordsRead++;

                var rec = context.SourceReader.CurrentRecord;
                for (int i=0; i<sizes.Length; ++i)
                {
                    int l = rec[i].Length;
                    if (l > sizes[i])
                        sizes[i] = l;
                }
            }

            for (int i=0; i<sizes.Length; ++i)
            {
                sizes[i] = ((sizes[i] + 10) / 10) * 10;
            }

            Console.WriteLine("  <Mappings>");
            Console.WriteLine("    <Mapping>");
            Console.WriteLine("      <Fields>");

            for (int i=0; i<sizes.Length; ++i)
            {
                Console.WriteLine("        <Field name=\"{0}\" maxSize=\"{1}\">${0}</Field>", context.SourceReader.FieldNames[i], sizes[i]);
            }
            Console.WriteLine("      </Fields>");
            Console.WriteLine("    </Mapping>");
            Console.WriteLine("  </Mappings>");

            return (int)ExitCodes.Success;
        }

        private void LogOperators(Context context)
        {
            var sb = new StringBuilder();
            bool first = true;
            foreach (var op in context.Operators.Keys)
            {
                if (!first)
                    sb.Append(", ");
                first = false;
                sb.Append(op);
            }
            context.Logger.Info("Available operators: " + sb.ToString());
        }

        private void InitLogger(ConfigFileXml configFile, Context context, LoggerFactory loggerFactory)
        {
            bool usedDefaultLogger = false;
            if (null == configFile.Logger)
            {
                configFile.Logger = new LoggerXml { LogType = "std", LogLevel = "info" };
                usedDefaultLogger = true;
            }

            if (null == configFile.Logger.LogLevel)
                configFile.Logger.LogLevel = "info";
            if (null == configFile.Logger.LogType)
                configFile.Logger.LogType = "std";

            LogLevel logLevel = LogLevel.Info;
            bool unknownLogLevel = false;
            switch (configFile.Logger.LogLevel.ToLowerInvariant())
            {
                case "info": logLevel = LogLevel.Info; break;
                case "warning": logLevel = LogLevel.Warning; break;
                case "error": logLevel = LogLevel.Error; break;

                default:
                    unknownLogLevel = true;
                    break;
            }

            context.Logger = loggerFactory.CreateLogger(configFile.Logger.LogType, logLevel, configFile.Logger.Config);
            if (usedDefaultLogger)
                context.Logger.Info("Using default stdout logger (type 'std').");
            if (unknownLogLevel)
                context.Logger.Warning("Unknown log level '" + configFile.Logger.LogLevel + "', assuming 'info'.");
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

            context.Logger.Info("Initialized operators.");
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
                context.Logger.Info("Started processing.");

                string[] outValues = new string[context.TargetFields.Length];

                var evaluator = new ExpressionParser();

                while (!context.SourceReader.IsEndOfStream)
                {
                    context.SourceReader.NextRecord();
                    context.SourceRecordsRead++;

                    if (context.SourceRecordsRead % 1000 == 0
                        && context.SourceRecordsRead > 0)
                    {
                        context.Logger.Info("Processed " + context.SourceRecordsRead + " records.");
                    }

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

                context.Logger.Info("Finished processing.");
                context.Logger.Info("Total source records read: " + context.SourceRecordsRead);
                context.Logger.Info("Total target records written: " + context.TargetRecordsWritten);
                context.Logger.Info("Records filtered out: " + context.SourceRecordsFiltered);
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

        private void InitLookupMaps(ConfigFileXml configFile, Context context, ReaderFactory readerFactory)
        {
            if (null == configFile.LookupMaps)
                return; // No lookup maps
            foreach (var lookupMap in configFile.LookupMaps)
            {
                context.LookupMaps.Add(lookupMap.Name, LookupMapFactory.CreateLookupMap(context, lookupMap, readerFactory));
                string nameLow = lookupMap.Name.ToLowerInvariant();
                if (context.Operators.ContainsKey(nameLow))
                    throw new InvalidOperationException("Duplicate use of operator '" + lookupMap.Name + "' for lookup maps; please rename the lookup map.");
                context.Operators[nameLow] = new NoFrillsTransformation.Engine.Operators.LookupOperator(nameLow);
            }

            context.Logger.Info("Initialized Lookup maps.");
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

            context.Logger.Info("Initialized Filters.");
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

            context.Logger.Info("Initialized field mappings, found " + context.TargetFields.Length + " output fields.");
        }
    }
}
