namespace CoreLang.Nodes
{
    public class MemberAccessNode : ExpressionNode
    {
        public string ObjectName { get; }
        public string MemberName { get; }

        public MemberAccessNode(string objectName, string memberName)
        {
            ObjectName = objectName;
            MemberName = memberName;
        }
    }
}
