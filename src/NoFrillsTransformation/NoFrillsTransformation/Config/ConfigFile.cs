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
        public SourceTargetXml Source { get; set; }
        public SourceTargetXml Target { get; set; }
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
    }

    public class SourceTargetXml
    {
        [XmlText]
        public string Uri { get; set; }
        [XmlAttribute("config")]
        public string Config { get; set; }
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
    }

    public class OperatorConfigXml
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlText]
        public string Config { get; set; }
    }
}
