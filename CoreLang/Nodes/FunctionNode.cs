using System.Collections.Generic;

namespace CoreLang.Nodes
{
    public class FunctionNode : AstNode
    {
        public string Name { get; }
        public List<ParameterNode> Parameters { get; } = new();
        public TypeNode ReturnType { get; }
        public BlockNode Body { get; }
        public bool IsEntry { get; }

        public FunctionNode(string name, IEnumerable<ParameterNode> parameters, TypeNode returnType, BlockNode body, bool isEntry)
        {
            Name = name;
            Parameters.AddRange(parameters);
            ReturnType = returnType;
            Body = body;
            IsEntry = isEntry;
        }
    }
}
