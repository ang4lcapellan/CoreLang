using System;
using System.Collections.Generic;
using System.Text;

namespace CoreLang.Nodes
{
    public sealed class ProgramNode : AstNode
    {
        public List<AstNode> Items { get; }

        public ProgramNode(List<AstNode> items)
        {
            Items = items;
        }
    }
}
