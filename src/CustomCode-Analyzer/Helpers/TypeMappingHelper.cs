using System;
using System.Collections.Generic;

namespace CustomCode_Analyzer
{
    /// <summary>
    /// Provides a centralized mapping between OutSystems <c>OSDataType</c> enum names
    /// (e.g., "Text", "Integer") and the corresponding .NET types or language aliases
    /// (e.g., "String" / "string", "Int32" / "int").
    /// </summary>
    public static class TypeMappingHelper
    {
        /// <summary>
        /// A local copy of the OSDataType enum
        /// </summary>
        internal enum OSDataType
        {
            InferredFromDotNetType,
            Text,
            Integer,
            LongInteger,
            Decimal,
            Boolean,
            DateTime,
            Date,
            Time,
            PhoneNumber,
            Email,
            BinaryData,
            Currency
        }

        /// <summary>
        /// Maps <c>OSDataType</c> enum <b>names</b> (e.g., "Text", "Integer")
        /// to a tuple containing:
        /// <list type="bullet">
        ///   <item><description><b>DotNetType</b>: the canonical .NET type name (e.g., "String", "Int32").</description></item>
        ///   <item><description><b>AliasType</b>: the C# language alias (e.g., "string", "int").</description></item>
        /// </list>
        /// This dictionary is used by <see cref="TryGetDotNetTypeName"/> and <see cref="TryGetAliasTypeName"/>.
        /// </summary>
        internal static readonly IReadOnlyDictionary<
            string,
            (string DotNetType, string AliasType)
        > OSDataTypeMap = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            // Key = OSDataType enum name, Value = (dotNetType, csharpAlias)
            ["Text"] = ("String", "string"),
            ["PhoneNumber"] = ("String", "string"),
            ["Email"] = ("String", "string"),
            ["Integer"] = ("Int32", "int"),
            ["LongInteger"] = ("Int64", "long"),
            ["Decimal"] = ("Decimal", "decimal"),
            ["Currency"] = ("Decimal", "decimal"),
            ["Boolean"] = ("Boolean", "bool"),
            ["DateTime"] = ("DateTime", "DateTime"),
            ["Date"] = ("DateTime", "DateTime"),
            ["Time"] = ("DateTime", "DateTime"),
            ["BinaryData"] = ("Byte[]", "byte[]"),
        };

        /// <summary>
        /// Attempts to retrieve the <b>canonical .NET type name</b> for a given
        /// OutSystems <c>OSDataType</c> name. For example:
        /// <list type="bullet">
        ///   <item><description><c>"Text"</c> → <c>"String"</c></description></item>
        ///   <item><description><c>"Integer"</c> → <c>"Int32"</c></description></item>
        /// </list>
        /// </summary>
        internal static bool TryGetDotNetTypeName(string dataTypeName, out string dotNetTypeName)
        {
            if (OSDataTypeMap.TryGetValue(dataTypeName, out var tuple))
            {
                dotNetTypeName = tuple.DotNetType;
                return true;
            }
            dotNetTypeName = null;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve the <b>C# alias type name</b> for a given
        /// OutSystems <c>OSDataType</c> name. For example:
        /// <list type="bullet">
        ///   <item><description><c>"Text"</c> → <c>"string"</c></description></item>
        ///   <item><description><c>"Integer"</c> → <c>"int"</c></description></item>
        /// </list>
        /// </summary>
        internal static bool TryGetAliasTypeName(string dataTypeName, out string aliasType)
        {
            if (OSDataTypeMap.TryGetValue(dataTypeName, out var tuple))
            {
                aliasType = tuple.AliasType;
                return true;
            }
            aliasType = null;
            return false;
        }
    }
}
