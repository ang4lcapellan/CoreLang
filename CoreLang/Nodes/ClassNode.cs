using System.Collections.Generic;

namespace CoreLang.Nodes
{
    public class ClassNode : AstNode
    {
        public string Name { get; }
        public List<AstNode> Members { get; } = new();

        public ClassNode(string name, IEnumerable<AstNode> members)
        {
            Name = name;
            Members.AddRange(members);
        }
    }
}
