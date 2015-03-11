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
            using (var reader = readerFactory.CreateReader(configFile.Source.Uri, configFile.Source.Config))
            {

            }

            var expression = ExpressionParser.ParseExpression("OpptRecTypes(OpptMap($BELEGART, $DeveloperName), $Id)", null);
            var more = ExpressionParser.ParseExpression("\"This is a fixed text.\"", null);
            var wupf = ExpressionParser.ParseExpression("Concat(TargetRowNum(), Concat(\"-\", Lookilook(SourceRowNum(), $Whatever)))", null);
        }
    }
}
