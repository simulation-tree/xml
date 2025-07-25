namespace XML
{
    internal static class SharedFunctions
    {
        public static bool IsWhiteSpace(char character)
        {
            return char.IsWhiteSpace(character) || character == (char)65279; //BOM
        }
    }
}