//using System;
//using System.Text;
//using CodeTools;

//namespace SupportTools.CodeCreators;

//public sealed class SshCodeBlock : CodeBlockBase, ICodeItem
//{
//    private readonly string _closeDelimiter;
//    private readonly string _openDelimiter;

//    // ReSharper disable once ConvertToPrimaryConstructor
//    public SshCodeBlock(string blockHeader, string openDelimiter, string closeDelimiter, params object?[] codeList) :
//        base(codeList)
//    {
//        _openDelimiter = openDelimiter;
//        _closeDelimiter = closeDelimiter;
//        BlockHeader = blockHeader;
//    }

//    private string BlockHeader { get; }

//    public override string Output(int indentLevel)
//    {
//        var indent = new string(' ', indentLevel * Stats.IndentSize);
//        var sb = new StringBuilder();
//        sb.AppendLine(indent + BlockHeader);
//        if (!string.IsNullOrWhiteSpace(_openDelimiter))
//            sb.AppendLine(indent + _openDelimiter);
//        sb.Append(base.Output(indentLevel));
//        sb.AppendLine(indent + _closeDelimiter);

//        return sb.ToString();
//    }

//    public override string OutputCreator(int indentLevel, int additionalIndentLevel)
//    {
//        var indent = indentLevel == 0
//            ? string.Empty
//            : new string(' ', (indentLevel + additionalIndentLevel) * Stats.IndentSize);
//        var sb = new StringBuilder();
//        if (indentLevel > 0) sb.Append("," + Environment.NewLine + indent);
//        sb.Append($"new CodeBlock({BlockHeader.Quotas()}");
//        sb.Append(base.OutputCreator(indentLevel, additionalIndentLevel));
//        sb.Append(")");

//        return sb.ToString();
//    }
//}

