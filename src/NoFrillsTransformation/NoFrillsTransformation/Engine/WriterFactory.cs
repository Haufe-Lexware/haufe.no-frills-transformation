using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine
{
    class WriterFactory
    {
        public WriterFactory(CompositionContainer container)
        {
            container.ComposeParts(this);
        }


#pragma warning disable 0649
        [ImportMany(typeof(NoFrillsTransformation.Interfaces.ITargetWriterFactory))]
        private ITargetWriterFactory[] _writerFactories;
#pragma warning restore 0649

        public ITargetWriter CreateWriter(string target, TargetFieldDef[] fieldDefs, string config)
        {
            var fieldNames = GetFieldNames(fieldDefs);
            var fieldSizes = GetFieldSizes(fieldDefs);
            foreach (var wf in _writerFactories)
            {
                if (wf.CanWriteTarget(target))
                    return wf.CreateWriter(target, fieldNames, fieldSizes, config);
            }
            throw new InvalidOperationException("Could not find a suitable writer for target '" + target + "'.");
        }

        private static string[] GetFieldNames(TargetFieldDef[] fieldDefs)
        {
            return fieldDefs.Select(def => def.FieldName).ToArray();
        }

        private static int[] GetFieldSizes(TargetFieldDef[] fieldDefs)
        {
            return fieldDefs.Select(def => def.FieldSize).ToArray();
        }
    }
}
