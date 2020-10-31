using System.Collections.Generic;

namespace Ash.HTMLParser
{
    #nullable enable

    public interface ITag
    {
        string Text { get; }

        string CleanedText { get; }

        string Type { get; }

        ITag? Parent { get; }

        IReadOnlyList<ITag> Children { get; }

        IReadOnlyList<string> Classes { get; }

        IReadOnlyDictionary<string, string> Styles { get; }

        IReadOnlyDictionary<string, string> Attributes { get; }

        string? GetAttribute(string attributename);

        string? Id { get; }

        ITag? FirstChild { get; }

        ITag? LastChild { get; }

        ITag? ClosesParent(string tagType);
    }
}