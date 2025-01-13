using System.Collections.Generic;

namespace CustomCode_Analyzer
{
    public static class AttributeNames
    {
        /// <summary>
        /// Valid names for the OSInterface attribute.
        /// </summary>
        public static readonly HashSet<string> OSInterfaceAttributeNames = new()
        {
            "OSInterfaceAttribute",
            "OSInterface",
        };

        /// <summary>
        /// Valid names for the OSStructure attribute.
        /// </summary>
        public static readonly HashSet<string> OSStructureAttributeNames = new()
        {
            "OSStructureAttribute",
            "OSStructure",
        };

        /// <summary>
        /// Valid names for the OSStructureField attribute.
        /// </summary>
        public static readonly HashSet<string> OSStructureFieldAttributeNames = new()
        {
            "OSStructureFieldAttribute",
            "OSStructureField",
        };

        /// <summary>
        /// Valid names for the OSIgnore attribute.
        /// </summary>
        public static readonly HashSet<string> OSIgnoreAttributeNames = new()
        {
            "OSIgnoreAttribute",
            "OSIgnore",
        };
    }
}
