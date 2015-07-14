using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
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
            {
                var parameters = ExtractParameters(args);
                if (null != parameters)
                    return (new Program()).Run(args[0], parameters);
            }
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
                Console.WriteLine("  NoFrillsTransformation.exe <config file> [param1=setting1 param2=setting2...]");
                return false;
            }
            return true;
        }

        private static Dictionary<string, string> ExtractParameters(string[] args)
        {
            var dict = new Dictionary<string, string>();
            if (args.Length <= 1)
                return dict;
            for (int i = 1; i < args.Length; ++i)
            {
                string p = TrimQuotes(args[i]);
                int eqIndex = p.IndexOf('=');
                if (eqIndex < 0)
                {
                    Console.WriteLine("Invalid parameter setting: '" + p + "'.");
                    Console.WriteLine("Parameters have to be in format: paramName=paramValue");
                    return null;
                }
                string paramName = TrimQuotes(p.Substring(0, eqIndex));
                if (string.IsNullOrEmpty(paramName))
                {
                    Console.WriteLine("Invalid parameter name: '" + paramName + "'.");
                    return null;
                }
                string paramValue = TrimQuotes(p.Substring(eqIndex + 1));

                if (dict.ContainsKey(paramName))
                {
                    Console.WriteLine("Duplicate parameter name: '" + paramName + "'.");
                    return null;
                }
                dict[paramName] = paramValue;
            }
            return dict;
        }

        private static string TrimQuotes(string s)
        {
            if (s.StartsWith("\"") && s.EndsWith("\""))
                return s.Substring(1, s.Length - 2);
            return s;
        }

        private int Run(string configFileName, Dictionary<string, string> parameters)
        {
            using (Context context = new Context())
            {
                try
                {
                    context.Parameters = parameters;
                    context.ConfigFileName = configFileName;
                    var configFile = ReadConfigFile(configFileName);
                    var includes = ReadIncludes(context, configFile);
                    configFile = MergeConfigFiles(configFile, includes);

                    // Set up MEF
                    var catalog = new DirectoryCatalog(".");
                    var container = new CompositionContainer(catalog);

                    var loggerFactory = new LoggerFactory(container);
                    var readerFactory = new ReaderFactory(container);
                    var writerFactory = new WriterFactory(container);
                    var operatorFactory = new OperatorFactory(container);
                    var transformerFactory = new TransformerFactory(container);

                    InitLogger(configFile, context, loggerFactory);
                    LogParameters(context);
                    ReplaceParameters(configFile, context);
                    context.Logger.Info("Read configuration file: " + configFileName);

                    InitOperators(configFile, context, operatorFactory);
                    InitLookupMaps(configFile, context, readerFactory);
                    InitCustomOperators(configFile, context);//, operatorFactory);
                    InitTransformer(configFile, context, transformerFactory);

                    LogOperators(context);

                    ReadFilters(configFile, context);
                    ReadMappings(configFile, context);
                    ReadSources(context, configFile, readerFactory);

                    if (null != configFile.OutputFields)
                    {
                        if (configFile.OutputFields.Value)
                            return OutputSourceFieldsToConsole(context, configFile.OutputFields.NoSizes);
                    }

                    context.TargetWriter = writerFactory.CreateWriter(context, configFile.Target.Uri, context.TargetFields, configFile.Target.Config);

                    Process(context);

                    // Explicitly dispose readers and writers to have control
                    // on when these resources are released. If something fails,
                    // this is done in the Dispose() method of Context.

                    foreach (var reader in context.SourceReaders)
                        reader.Dispose();
                    context.SourceReaders = null;
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

        private static void ReadSources(Context context, ConfigFileXml configFile, ReaderFactory readerFactory)
        {
            var sourceList = new List<ISourceReader>();
            var sourceFiles = new HashSet<string>();
            if (null != configFile.Source
                && null != configFile.Sources
                && configFile.Sources.Length > 0)
                throw new ArgumentException("Configuration error: You can't both define the <Source> and <Sources> tag.");

            if (null != configFile.Source)
            {
                var thisSource = configFile.Source;
                AddSources(context, readerFactory, sourceList, sourceFiles, thisSource);
            }

            if (null != configFile.Sources
                && configFile.Sources.Length > 0)
            {
                for (int i = 0; i < configFile.Sources.Length; ++i)
                {
                    //    sourceList.Add(readerFactory.CreateReader(context, configFile.Sources[i].Uri, configFile.Sources[i].Config));
                    AddSources(context, readerFactory, sourceList, sourceFiles, configFile.Sources[i]);
                }
            }

            context.SourceReaders = sourceList.ToArray();

            if (context.SourceReaders.Length <= 0)
                throw new ArgumentException("No Sources were identified.");
        }

        private static void AddSources(Context context, ReaderFactory readerFactory, List<ISourceReader> sourceList, HashSet<string> sourceFiles, SourceTargetXml thisSource)
        {
            // Wildcards in Source?
            if (thisSource.Uri.StartsWith("file://")
                && (thisSource.Uri.Contains("*") || thisSource.Uri.Contains("?")))
            {
                string sourceFile = thisSource.Uri.Substring(7); // strip file://
                context.Logger.Info("Detected wildcards in file name (" + sourceFile + ").");
                string path = Path.GetDirectoryName(sourceFile);
                string resolvedPath = context.ResolveFileName(path, false);
                string fileName = Path.GetFileName(sourceFile);
                foreach (string sourceFileName in Directory.EnumerateFiles(resolvedPath, fileName))
                {
                    if (sourceFiles.Contains(sourceFileName))
                        continue;
                    context.Logger.Info("Creating reader for: " + sourceFileName);
                    sourceList.Add(readerFactory.CreateReader(context, "file://" + sourceFileName, thisSource.Config));
                    sourceFiles.Add(sourceFileName);
                }
            }
            else
            {
                sourceList.Add(readerFactory.CreateReader(context, thisSource.Uri, thisSource.Config));
            }
        }


        private int OutputSourceFieldsToConsole(Context context, bool noSizes)
        {
            try
            {
                context.SourceReader = context.SourceReaders[0];

                int[] sizes = new int[context.SourceReader.FieldCount];
                for (int i = 0; i < sizes.Length; ++i)
                    sizes[i] = 0;
                int maxCount = 100;

                if (!noSizes)
                {
                    while (!context.SourceReader.IsEndOfStream
                        && context.SourceRecordsRead < maxCount)
                    {
                        context.SourceReader.NextRecord();
                        context.SourceRecordsRead++;

                        var rec = context.SourceReader.CurrentRecord;
                        for (int i = 0; i < sizes.Length; ++i)
                        {
                            int l = rec[i].Length;
                            if (l > sizes[i])
                                sizes[i] = l;
                        }
                    }
                    for (int i = 0; i < sizes.Length; ++i)
                    {
                        sizes[i] = ((sizes[i] + 10) / 10) * 10;
                    }
                }

                Console.WriteLine("  <Mappings>");
                Console.WriteLine("    <Mapping>");
                Console.WriteLine("      <Fields>");

                for (int i = 0; i < sizes.Length; ++i)
                {
                    if (!noSizes)
                        Console.WriteLine("        <Field name=\"{0}\" maxSize=\"{1}\">${0}</Field>", context.SourceReader.FieldNames[i], sizes[i]);
                    else
                        Console.WriteLine("        <Field name=\"{0}\">${0}</Field>", context.SourceReader.FieldNames[i]);
                }
                Console.WriteLine("      </Fields>");
                Console.WriteLine("    </Mapping>");
                Console.WriteLine("  </Mappings>");

                return (int)ExitCodes.Success;
            }
            finally
            {
                context.SourceReader = null;
            }
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

        private void LogParameters(Context context)
        {
            foreach (string key in context.Parameters.Keys)
            {
                context.Logger.Info("Parameter: " + key + "=" + context.Parameters[key]);
            }
        }

        private void ReplaceParameters(ConfigFileXml config, Context context)
        {
            if (null != config.Source)
            {
                config.Source.Uri = context.ReplaceParameters(config.Source.Uri);
                config.Source.Config = context.ReplaceParameters(config.Source.Config);
            }
            if (null != config.Sources)
            {
                foreach (var source in config.Sources)
                {
                    source.Uri = context.ReplaceParameters(source.Uri);
                    source.Config = context.ReplaceParameters(source.Config);
                }
            }
            config.Target.Uri = context.ReplaceParameters(config.Target.Uri);
            config.Target.Config = context.ReplaceParameters(config.Target.Config);
            if (null != config.SourceTransform)
            {
                config.SourceTransform.Transform.Config = context.ReplaceParameters(config.SourceTransform.Transform.Config);
                config.SourceTransform.Transform.Uri = context.ReplaceParameters(config.SourceTransform.Transform.Uri);
                if (null != config.SourceTransform.Parameters)
                {
                    foreach (var parameter in config.SourceTransform.Parameters)
                    {
                        parameter.FunctionString = context.ReplaceParameters(parameter.FunctionString);
                    }
                }
                if (null != config.SourceTransform.Settings)
                {
                    foreach (var setting in config.SourceTransform.Settings)
                    {
                        setting.Setting = context.ReplaceParameters(setting.Setting);
                    }
                }
            }

            if (null != config.LookupMaps)
            {
                foreach (var lookup in config.LookupMaps)
                {
                    lookup.Source.Uri = context.ReplaceParameters(lookup.Source.Uri);
                    lookup.Source.Config = context.ReplaceParameters(lookup.Source.Config);
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
                    string opName = opConfig.Name.ToLowerInvariant();
                    if (!context.Operators.ContainsKey(opName))
                        throw new InvalidOperationException("Configuration was passed in config XML for unknown operator '" + opConfig.Name + ". Are your plugins in the same folder as the executable?");
                    context.Operators[opName].Configure(opConfig.Config);
                }
            }

            context.Logger.Info("Initialized operators.");
        }

        private void InitCustomOperators(ConfigFileXml configFile, Context context)//, OperatorFactory operatorFactory)
        {
            try
            {
                if (null == configFile.CustomOperators)
                    return;

                foreach (var op in configFile.CustomOperators)
                {
                    var returnType = ExpressionParser.StringToType(op.ReturnType);
                    var name = op.Name.ToLowerInvariant();
                    var paramNameList = new List<string>();
                    var paramTypeList = new List<ParamType>();
                    if (null != op.Parameters)
                    {
                        foreach (var param in op.Parameters)
                        {
                            paramNameList.Add(param.Name.ToLowerInvariant());
                            paramTypeList.Add(ExpressionParser.StringToType(param.Type));
                        }
                    }
                    if (paramNameList.Count != op.ParamCount)
                        throw new ArgumentException("Custom operator paramCount attribute does not match actual parameter count: " + op.ParamCount + " vs. " + paramNameList.Count);

                    if (context.Operators.ContainsKey(name))
                        throw new ArgumentException("Duplicate definition for custom operator '" + op.Name + "' found.");
                    // Plain function?
                    if (op.Function != null)
                    {
                        if (op.Switch != null)
                            throw new ArgumentException("A CustomOperator cannot have both a Function and a Switch/Case block: " + op.Name);
                        context.Operators[name] = new Engine.Operators.CustomOperator(name)
                            {
                                ParamCount = op.ParamCount,
                                ParamNames = paramNameList.ToArray(),
                                ParamTypes = paramTypeList.ToArray(),
                                ReturnType = returnType,
                                Expression = ExpressionParser.ParseExpression(op.Function, context)
                            };
                    }
                    else if (op.Switch != null)
                    {
                        int conditionCount = op.Switch.Cases.Length;
                        var conditions = new IExpression[conditionCount];
                        var caseFunctions = new IExpression[conditionCount];
                        for (int i = 0; i < conditionCount; ++i)
                        {
                            var thisCase = op.Switch.Cases[i];
                            conditions[i] = ExpressionParser.ParseExpression(thisCase.Condition, context);
                            if (conditions[i].Operator.ReturnType != ParamType.Bool)
                                throw new ArgumentException("The return type of Case conditions must be boolean: " + thisCase.Condition + ", is " + conditions[i].Operator.ReturnType);
                            caseFunctions[i] = ExpressionParser.ParseExpression(thisCase.Function, context);
                        }
                        var otherwise = ExpressionParser.ParseExpression(op.Switch.Otherwise, context);

                        context.Operators[name] = new Engine.Operators.CustomOperator(name)
                            {
                                ParamCount = op.ParamCount,
                                ParamNames = paramNameList.ToArray(),
                                ParamTypes = paramTypeList.ToArray(),
                                ReturnType = returnType,
                                Conditions = conditions,
                                CaseFunctions = caseFunctions,
                                Otherwise = otherwise
                            };
                    }
                    else
                    {
                        throw new ArgumentException("A CustomOperator must either have a Function tag or a Switch/Case block: " + op.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException("An error occurred while parsing the custom operators: " + ex.Message);
            }
        }

        private void InitTransformer(ConfigFileXml configFile, Context context, TransformerFactory transformerFactory)
        {
            try
            {
                if (null == configFile.SourceTransform)
                    return;
                if (null == configFile.SourceTransform.Transform)
                    return;

                var parameters = new IParameter[configFile.SourceTransform.Parameters.Length];
                for (int i = 0; i < parameters.Length; ++i)
                {
                    var xmlParam = configFile.SourceTransform.Parameters[i];
                    parameters[i] = new TransformerParameter
                    {
                        Name = xmlParam.Name,
                        FunctionString = xmlParam.FunctionString,
                        Function = ExpressionParser.ParseExpression(xmlParam.FunctionString, context)
                    };
                }
                ISetting[] settings = null;
                if (null != configFile.SourceTransform.Settings)
                {
                    settings = new ISetting[configFile.SourceTransform.Settings.Length];
                    for (int i = 0; i < configFile.SourceTransform.Settings.Length; ++i)
                    {
                        var xmlSetting = configFile.SourceTransform.Settings[i];
                        settings[i] = new TransformerSetting
                        {
                            Name = xmlSetting.Name,
                            Setting = xmlSetting.Setting
                        };
                    }
                }
                else
                {
                    settings = new ISetting[0];
                }

                var transformer = transformerFactory.CreateTransformer(context,
                    configFile.SourceTransform.Transform.Uri,
                    configFile.SourceTransform.Transform.Config,
                    parameters,
                    settings);

                context.Transformer = transformer;
            }
            catch (Exception ex)
            {
                throw new ArgumentException("An error occurred while instantiating the SourceTransform plugin: " + ex.Message);
            }
        }

        private static ConfigFileXml ReadConfigFile(string configFileName)
        {
            ConfigFileXml configFile = null;
            try
            {
                bool errors = ValidateConfigFile(configFileName);

                if (errors)
                    throw new ArgumentException("XML config file could not be validated successfully.");

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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        private static bool ValidateConfigFile(string configFileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "NoFrillsTransformation.Config.Config.xsd";

            bool errors = false;
            Stream stream = null;
            try
            {
                stream = assembly.GetManifestResourceStream(resourceName);
                using (var xsdStream = XmlReader.Create(stream))
                {
                    stream = null;
                    var schemaSet = new XmlSchemaSet();
                    schemaSet.Add("", xsdStream);
                    var xmlDoc = XDocument.Load(configFileName);
                    bool first = true;
                    xmlDoc.Validate(schemaSet, (o, e) =>
                    {
                        if (first)
                            Console.WriteLine("The input file does not adher to the input file schema:");
                        first = false;
                        Console.WriteLine(e.Message);
                        errors = true;
                    });
                }
            }
            finally
            {
                if (null != stream)
                    stream.Dispose();
                stream = null;
            }
            return errors;
        }

        private static ConfigFileXml[] ReadIncludes(IContext context, ConfigFileXml configFile)
        {
            int includeCount = configFile.Includes != null ? configFile.Includes.Length : 0;

            ConfigFileXml[] includes = new ConfigFileXml[includeCount];
            for (int i = 0; i < includeCount; ++i)
            {
                string resolvedPath = context.ResolveFileName(configFile.Includes[i].FileName);
                includes[i] = ReadConfigFile(resolvedPath);
            }
            return includes;
        }

        private static ConfigFileXml MergeConfigFiles(ConfigFileXml configFile, ConfigFileXml[] includes)
        {
            var configFiles = new List<ConfigFileXml>();
            configFiles.AddRange(includes);
            configFiles.Add(configFile);

            var customOperators = new List<CustomOperatorXml>();
            var lookupMaps = new List<LookupMapXml>();
            foreach (var cf in configFiles)
            {
                if (null != cf.CustomOperators)
                {
                    foreach (var customOperator in cf.CustomOperators)
                        customOperators.Add(customOperator);
                }
                if (null != cf.LookupMaps)
                {
                    foreach (var lookupMap in cf.LookupMaps)
                        lookupMaps.Add(lookupMap);
                }
            }

            configFile.CustomOperators = customOperators.ToArray();
            configFile.LookupMaps = lookupMaps.ToArray();
            return configFile;
        }

        private void Process(Context context)
        {
            try
            {
                context.Logger.Info("Started processing.");

                string[] outValues = new string[context.TargetFields.Length];

                var evaluator = new ExpressionParser();

                if (null == context.SourceReaders)
                {
                    context.SourceReaders = new ISourceReader[] { context.SourceReader };
                }

                for (int source = 0; source < context.SourceReaders.Length; ++source)
                {
                    context.Logger.Info("Reading Source #" + (source + 1));
                    try
                    {
                        context.SourceReader = context.SourceReaders[source];

                        while (!context.SourceReader.IsEndOfStream)
                        {
                            context.SourceReader.NextRecord();
                            if (context.SourceReader.IsEndOfStream)
                                break;
                            context.SourceRecordsRead++;

                            bool hasMultipleTransformedRecord = true;
                            bool isFirstRecord = true;

                            while (hasMultipleTransformedRecord)
                            {
                                if (null != context.Transformer)
                                {
                                    if (isFirstRecord)
                                    {
                                        try
                                        {
                                            isFirstRecord = false;
                                            context.InTransform = true;
                                            context.Transformer.Transform(context, evaluator);
                                        }
                                        finally
                                        {
                                            context.InTransform = false;
                                        }
                                    }
                                    else
                                    {
                                        context.Transformer.NextRecord();
                                    }
                                }
                                else
                                {
                                    hasMultipleTransformedRecord = false;
                                }

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

                                if (null != context.Transformer)
                                {
                                    hasMultipleTransformedRecord = context.Transformer.HasMoreRecords();
                                }
                            }
                        }
                    }
                    finally
                    {
                        context.SourceReader = null;
                    }
                }

                context.Logger.Info("Finishing Write Process.");
                context.TargetWriter.FinishWrite();

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
                    tfd.Config = (null != field.Config) ? context.ReplaceParameters(field.Config) : null;
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
