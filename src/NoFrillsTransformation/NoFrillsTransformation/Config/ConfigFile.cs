using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace NoFrillsTransformation.Config
{
    [XmlRoot("Transformation")]
    public class ConfigFileXml
    {
        [XmlArray("Includes")]
        [XmlArrayItem("Include")]
        public IncludeXml[] Includes { get; set; }

        public LoggerXml Logger { get; set; }
        public SourceTargetXml Source { get; set; }
        public SourceTransformXml SourceTransform { get; set; }
        public SourceTargetXml Target { get; set; }

        public OutputFieldsXml OutputFields { get; set; }

        public string FilterMode { get; set; }
        [XmlArray("SourceFilters")]
        [XmlArrayItem("SourceFilter")]
        public SourceFilterXml[] SourceFilters { get; set; }
        [XmlArray("LookupMaps")]
        [XmlArrayItem("LookupMap")]
        public LookupMapXml[] LookupMaps { get; set; }
        [XmlArray("Mappings")]
        [XmlArrayItem("Mapping")]
        public MappingXml[] Mappings { get; set; }
        [XmlArray("OperatorConfigs")]
        [XmlArrayItem("OperatorConfig")]
        public OperatorConfigXml[] OperatorConfigs { get; set; }
        [XmlArray("CustomOperators")]
        [XmlArrayItem("CustomOperator")]
        public CustomOperatorXml[] CustomOperators { get; set; }
    }

    public class IncludeXml
    {
        [XmlText]
        public string FileName { get; set; }
    }

    public class OutputFieldsXml
    {
        [XmlText]
        public bool Value { get; set; }
        [XmlAttribute("noSizes")]
        public bool NoSizes { get; set; }
    }

    public class LoggerXml
    {
        [XmlText]
        public string Config { get; set; }
        [XmlAttribute("type")]
        public string LogType { get; set; }
        [XmlAttribute("level")]
        public string LogLevel { get; set; }
    }

    public class SourceTargetXml
    {
        [XmlText]
        public string Uri { get; set; }
        [XmlAttribute("config")]
        public string Config { get; set; }
    }

    public class SourceTransformXml
    {
        public SourceTargetXml Transform { get; set; }
        [XmlArray("Parameters")]
        [XmlArrayItem("Parameter")]
        public TransformParameterXml[] Parameters { get; set; }
        [XmlArray("Settings")]
        [XmlArrayItem("Setting")]
        public TransformSettingXml[] Settings { get; set; }
    }

    public class TransformParameterXml
    {
        [XmlText]
        public string FunctionString { get; set; }
        [XmlAttribute("name")]
        public string Name { get; set; }
    }

    public class TransformSettingXml
    {
        [XmlText]
        public string Setting { get; set; }
        [XmlAttribute("name")]
        public string Name { get; set; }
    }

    public class SourceFilterXml
    {
        [XmlText]
        public string Expression { get; set; }
    }

    public class LookupMapXml
    {
        [XmlAttribute("keyField")]
        public string Key { get; set; }
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlAttribute("noFailOnMiss")]
        public bool NoFailOnMiss { get; set; }
        public SourceTargetXml Source { get; set; }
    }

    public class MappingXml
    {
        [XmlArray("Fields")]
        [XmlArrayItem("Field")]
        public FieldXml[] Fields { get; set; }
    }

    public class FieldXml
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlText]
        public string Expression { get; set; }
        [XmlAttribute("maxSize")]
        public int MaxSize { get; set; }
        [XmlAttribute("config")]
        public string Config { get; set; }
    }

    public class OperatorConfigXml
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlText]
        public string Config { get; set; }
    }

    public class CustomOperatorXml
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlAttribute("paramCount")]
        public int ParamCount { get; set; }
        [XmlAttribute("returnType")]
        public string ReturnType { get; set; }

        [XmlArray("Parameters")]
        [XmlArrayItem("Parameter")]
        public ParameterXml[] Parameters { get; set; }

        public string Function { get; set; }

        public SwitchCaseXml Switch { get; set; }
    }

    public class ParameterXml
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlAttribute("type")]
        public string Type { get; set; }
    }

    public class SwitchCaseXml
    {
        [XmlElement("Case")]
        public CaseXml[] Cases { get; set; }

        public string Otherwise;
    }

    public class CaseXml
    {
        [XmlAttribute("condition")]
        public string Condition { get; set; }
        [XmlText]
        public string Function { get; set; }
    }
}
