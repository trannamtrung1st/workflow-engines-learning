namespace WELearning.DynamicCodeExecution.Models;

public record struct UserContentInfo(string Content, string[] Lines, int LineStart, int LineEnd, int IndexStart, int IndexEnd)
{
    public static implicit operator (string Content, string[] Lines, int LineStart, int LineEnd, int IndexStart, int IndexEnd)(UserContentInfo value)
    {
        return (value.Content, value.Lines, value.LineStart, value.LineEnd, value.IndexStart, value.IndexEnd);
    }

    public static implicit operator UserContentInfo((string Content, string[] Lines, int LineStart, int LineEnd, int IndexStart, int IndexEnd) value)
    {
        return new UserContentInfo(value.Content, value.Lines, value.LineStart, value.LineEnd, value.IndexStart, value.IndexEnd);
    }
}