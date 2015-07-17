using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Ado
{
    public abstract class AdoWriter : AdoBase, ITargetWriter
    {
        public AdoWriter(IContext context, string config, string table, IFieldDefinition[] fieldDefs)
        {
            _context = context;
            _config = GetConfig(context, config);
            _table = context.ReplaceParameters(table);
            _fieldDefs = fieldDefs;

            Initialize();
        }

        protected abstract void Initialize();

        private IContext _context;
        private string _config;
        private string _table;
        private IFieldDefinition[] _fieldDefs;
        private int _recordsWritten = 0;

        protected string Config
        {
            get { return _config; }
        }

        protected IContext Context
        {
            get { return _context; }
        }

        protected string Table
        {
            get { return _table; }
        }

        protected IFieldDefinition[] FieldDefs
        {
            get { return _fieldDefs; }
        }

        protected virtual string GetInsertStatement()
        {
            var sb = new StringBuilder();
            sb.Append("insert into ");
            sb.Append(Table);
            sb.Append(" (");
            bool first = true;
            foreach (var field in FieldDefs)
            {
                if (!first)
                    sb.Append(", ");
                sb.Append(field.FieldName);
                first = false;
            }
            sb.Append(") values (");
            first = true;
            foreach (var field in FieldDefs)
            {
                if (!first)
                    sb.Append(",");
                sb.Append("?");
                first = false;
            }
            sb.Append(")");
            return sb.ToString();
        }

        private bool _isFirst = true;

        public void WriteRecord(string[] fieldValues)
        {
            Insert(fieldValues);
            _recordsWritten++;
        }

        public void FinishWrite()
        {
            EndTransaction();
        }
        
        protected virtual void BeginTransaction()
        {
        }

        protected virtual void EndTransaction()
        {
        }
        
        protected abstract void Insert(string[] fieldValues);

        public int RecordsWritten
        {
            get 
            {
                return _recordsWritten;
            }
        }

        #region IDisposable
        // Dispose() calls Dispose(true)
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // No implementation needed here, we only have managed resources.
        }
        #endregion
    }
}
