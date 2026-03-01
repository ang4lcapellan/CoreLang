using System;
using System.Collections.Generic;
using System.Text;

namespace CoreLang.Nodes
{
    public abstract class AstNode
    {
        public int Line { get; init; }
        public int Column { get; init; }
    }
}
