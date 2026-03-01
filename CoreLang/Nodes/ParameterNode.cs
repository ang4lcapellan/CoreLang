using System.Collections.Generic;

namespace CoreLang.Nodes
{
    public class ParameterNode : AstNode
    {
        public string Name { get; }
        public TypeNode Type { get; }

        public ParameterNode(string name, TypeNode type)
        {
            Name = name;
            Type = type;
        }
    }
}
