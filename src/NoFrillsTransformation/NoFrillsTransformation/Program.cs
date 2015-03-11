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

namespace NoFrillsTransformation
{
    class Program
    {
        static void Main(string[] args)
        {
            (new Program()).Run();
        }

        private void Run()
        {
            /*
            Console.WriteLine("Hello, world.");

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ConfigFileXml));
            ConfigFileXml configFile = null;
            using (var fs = new FileStream(@"C:\Projects\no-frills-transformation\config\sample_config.xml", FileMode.Open))
            {
                configFile = xmlSerializer.Deserialize(fs) as ConfigFileXml;
            }

            using (var tr = new StreamReader(new FileStream(configFile.Source.Uri, FileMode.Open)))
            {
                var csv = new CsvReader(tr, true, ',');
                Console.WriteLine("Number of fields:" + csv.FieldCount);
                while (!csv.EndOfStream)
                {
                    if (!csv.ReadNextRecord())
                        continue;

                    Console.WriteLine("Something: " + csv["SOBID"] + "~");
                }
            }

            var expression = ExpressionParser.ParseExpression("concat(\"MB\", Status($Status, $Meep))", null);
            */

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ConfigFileXml));
            ConfigFileXml configFile = null;
            using (var fs = new FileStream(@"C:\Projects\no-frills-transformation\config\sample_config.xml", FileMode.Open))
            {
                configFile = xmlSerializer.Deserialize(fs) as ConfigFileXml;
            }

            var readerFactory = new ReaderFactory();
            var writerFactory = new WriterFactory();

            Context context = new Context();
            InitLookupMaps(configFile, readerFactory, context);
            ReadMappings(configFile, context);

            using (var reader = readerFactory.CreateReader(configFile.Source.Uri, configFile.Source.Config))
            {
                context.SourceReader = reader;
                using (var writer = writerFactory.CreateWriter(configFile.Target.Uri, context.TargetFieldNames, context.TargetFieldSizes, configFile.Target.Config))
                {
                    context.TargetWriter = writer;

                    Process(context);
                }
            }


            //var expression = ExpressionParser.ParseExpression("OpptRecTypes(OpptMap($BELEGART, $DeveloperName), $Id)", null);
            //var more = ExpressionParser.ParseExpression("\"This is a fixed text.\"", null);
            //var wupf = ExpressionParser.ParseExpression("Concat(TargetRowNum(), Concat(\"-\", Lookilook(SourceRowNum(), $Whatever)))", null);
        }

        private void Process(Context context)
        {

        }

        private void InitLookupMaps(ConfigFileXml configFile, ReaderFactory readerFactory, Context context)
        {
            if (null == configFile.LookupMaps)
                return; // No lookup maps
            foreach (var lookupMap in configFile.LookupMaps)
            {
                context.LookupMaps.Add(lookupMap.Name, LookupMapFactory.CreateLookupMap(lookupMap, readerFactory));
            }
        }

        private void ReadMappings(ConfigFileXml configFile, Context context)
        {
            context.TargetFieldNames = new string[] { };
            context.TargetFieldSizes = new int[] { }; 
        }
    }
}
