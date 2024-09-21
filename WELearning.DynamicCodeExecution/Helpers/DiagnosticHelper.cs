using WELearning.DynamicCodeExecution.Models;

namespace WELearning.DynamicCodeExecution.Helpers;

public static class DiagnosticHelper
{
    public static (int Line, int LineEnd, int Column, int ColumnEnd, int StartIndex, int EndIndex) RecalculatePosition(
        int lineNumber, int column, (int Start, int End) location, UserContentInfo contentInfo)
    {
        if (lineNumber == 0 || column < 0 || location.Start < 0)
            return (-1, -1, -1, -1, -1, -1);
        var (_, lines, lineStart, lineEnd, indexStart, indexEnd) = contentInfo;
        int originalLine; int originalColumn;
        int originalLineEnd; int originalColumnEnd;
        int startIndex; int endIndex;
        if (lineNumber > lineEnd || location.End > indexEnd)
        {
            startIndex = indexEnd - indexStart + 1;
            endIndex = startIndex;
            originalLine = lineEnd - lineStart + 1;
            originalLineEnd = originalLine;
            originalColumn = lines[lineEnd - 1].Length;
            originalColumnEnd = originalColumn;
        }
        else if (lineNumber < lineStart || location.Start < indexStart)
        {
            startIndex = 0;
            endIndex = startIndex;
            originalLine = 1;
            originalLineEnd = originalLine;
            originalColumn = 0;
            originalColumnEnd = originalColumn;
        }
        else
        {
            startIndex = location.Start - indexStart;
            endIndex = location.End == -1 ? -1 : location.End - indexStart;
            originalLine = lineNumber - lineStart + 1;
            originalColumn = column;

            var tempEnd = location.End;
            var rangeLineEnd = 1;
            int lineLen;
            while (rangeLineEnd <= lines.Length && tempEnd > (lineLen = lines[rangeLineEnd - 1].Length))
            {
                rangeLineEnd++;
                tempEnd -= lineLen + 1;
            }
            originalLineEnd = rangeLineEnd - lineStart + 1;
            originalColumnEnd = tempEnd + 1;
        }

        return (originalLine, originalLineEnd, originalColumn, originalColumnEnd, startIndex, endIndex);
    }
}
