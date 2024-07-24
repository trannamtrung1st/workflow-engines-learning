using WELearning.DynamicCodeExecution.Models;

namespace WELearning.DynamicCodeExecution.Helpers;

public static class DiagnosticHelper
{
    public static (int Line, int Column, int StartIndex, int EndIndex) RecalculatePosition(
        int lineNumber, int column, (int Start, int End) location, UserContentInfo contentInfo)
    {
        if (lineNumber == 0 || column < 0 || location.Start < 0)
            return (-1, -1, -1, -1);
        var (_, lines, lineStart, lineEnd, indexStart, indexEnd) = contentInfo;
        int originalLine; int originalColumn;
        int startIndex; int endIndex;
        if (lineNumber > lineEnd || location.End > indexEnd)
        {
            startIndex = indexEnd - indexStart + 1;
            endIndex = startIndex;
            originalLine = lineEnd - lineStart + 1;
            originalColumn = lines[lineEnd - 1].Length;
        }
        else if (lineNumber < lineStart || location.Start < indexStart)
        {
            startIndex = 0;
            endIndex = startIndex;
            originalLine = 1;
            originalColumn = 0;
        }
        else
        {
            startIndex = location.Start - indexStart;
            endIndex = location.End == -1 ? -1 : location.End - indexStart;
            originalLine = lineNumber - lineStart + 1;
            originalColumn = column;
        }

        return (originalLine, originalColumn, startIndex, endIndex);
    }
}