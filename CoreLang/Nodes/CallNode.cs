using System.Collections.Generic;

namespace CoreLang.Nodes
{
    public class CallNode : ExpressionNode
    {
        public string? ObjectName { get; }
        public string FunctionName { get; }
        public List<ExpressionNode> Arguments { get; } = new();

        public CallNode(string functionName, IEnumerable<ExpressionNode> arguments)
        {
            FunctionName = functionName;
            Arguments.AddRange(arguments);
        }

        public CallNode(string objectName, string functionName, IEnumerable<ExpressionNode> arguments)
        {
            ObjectName = objectName;
            FunctionName = functionName;
            Arguments.AddRange(arguments);
        }
    }
}
