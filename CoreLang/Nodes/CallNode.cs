using System.Collections.Generic;

namespace CoreLang.Nodes
{
    public class CallNode : ExpressionNode
    {
        public string FunctionName { get; }
        public List<ExpressionNode> Arguments { get; } = new();

        public CallNode(string functionName, IEnumerable<ExpressionNode> arguments)
        {
            FunctionName = functionName;
            Arguments.AddRange(arguments);
        }
    }
}
