using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Interfaces
{
    public interface ITargetWriterFactory
    {
        bool CanWriteTarget(string target);
        ITargetWriter CreateWriter(IContext context, string target, IFieldDefinition[] fieldDefs, string config);
    }
}
