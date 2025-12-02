using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine
{
    class TransformerSetting : ISetting
    {
        public TransformerSetting(string name, string setting)
        {
            Name = name;
            Setting = setting;
        }
        public string Name { get; set; }
        public string Setting { get; set; }
    }
}
