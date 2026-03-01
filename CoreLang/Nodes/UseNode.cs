namespace CoreLang.Nodes
{
    public class UseNode : AstNode
    {
        public string LibraryName { get; }

        public UseNode(string libraryName)
        {
            LibraryName = libraryName;
        }
    }
}
