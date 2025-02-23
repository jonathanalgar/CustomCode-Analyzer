using System.Collections.Generic;

namespace CustomCode_Analyzer.Helpers
{
    public static class AttributeNames
    {
        /// <summary>
        /// Valid names for the OSInterface attribute.
        /// </summary>
        internal static readonly HashSet<string> OSInterfaceAttributeNames =
        [
            "OSInterfaceAttribute",
            "OSInterface",
        ];

        /// <summary>
        /// Valid names for the OSStructure attribute.
        /// </summary>
        internal static readonly HashSet<string> OSStructureAttributeNames =
        [
            "OSStructureAttribute",
            "OSStructure",
        ];

        /// <summary>
        /// Valid names for the OSStructureField attribute.
        /// </summary>
        internal static readonly HashSet<string> OSStructureFieldAttributeNames =
        [
            "OSStructureFieldAttribute",
            "OSStructureField",
        ];

        /// <summary>
        /// Valid names for the OSIgnore attribute.
        /// </summary>
        internal static readonly HashSet<string> OSIgnoreAttributeNames =
        [
            "OSIgnoreAttribute",
            "OSIgnore",
        ];
    }
}
