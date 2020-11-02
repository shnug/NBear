using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace NBear.Common
{
    public interface IExpression
    {
        string Sql { get; set; }
        Dictionary<string, KeyValuePair<DbType, object>> Parameters { get; }
    }

    [Serializable]
    public class NameDuplicatedException : Exception
    {
        public NameDuplicatedException() { }
        public NameDuplicatedException(string name) : base(name) { }
    }
}
