using CodeTools;

namespace SupportTools.CodeCreators;

public class SshOneLineComment : OneLineComment
{
    public SshOneLineComment(string commentText) : base(commentText, "#")
    {
    }
}