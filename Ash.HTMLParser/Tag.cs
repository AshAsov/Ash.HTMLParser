using System;
using System.Collections.Generic;
using System.Text;

namespace Ash.HTMLParser
{
    public partial class Parser
    {
        private class Tag : ITag
        {
            public string InnerType;

        #nullable enable

            public List<string> InnerClasses = new List<string>();

            public Dictionary<string, string> InnerStyles = new Dictionary<string, string>();

            public Dictionary<string, string> InnerAttributes = new Dictionary<string, string>();

            public List<Tag> InnerChildren = new List<Tag>();

            public string InnerText = string.Empty;

            public Tag? InnerParent;

            public string Text => InnerText;

            public string Type => InnerType;

            public ITag? Parent => InnerParent;

            public IReadOnlyList<ITag> Children => InnerChildren;

            public IReadOnlyList<string> Classes => InnerClasses;

            public IReadOnlyDictionary<string, string> Styles => InnerStyles;

            public IReadOnlyDictionary<string, string> Attributes => InnerAttributes;

            public string? Id => InnerAttributes.TryGetValue("id", out var idValue) ? idValue : null;

            public string? GetAttribute(string attributeName)
            {
                if (string.IsNullOrWhiteSpace(attributeName))
                    throw new ArgumentException("Argument 'id' is null or whitespace");

                return InnerGetAttribute(attributeName);
            }

            public string? InnerGetAttribute(string attributeName) => InnerAttributes.TryGetValue(attributeName, out var value) ? value : null;

            public ITag? FirstChild => InnerChildren.Count > 0 ? InnerChildren[0] : null;

            public ITag? LastChild => InnerChildren.Count > 0 ? InnerChildren[^1] : null;

            public ITag? ClosesParent(string tagType)
            {
                var parent = Parent;
                while (parent != null)
                {
                    if (parent.Type == tagType)
                        return parent;
                    parent = parent.Parent;
                }
                return null;
            }

        #nullable disable
            public void AddAttribute(string key, string value)
            {
                if (!InnerAttributes.TryAdd(key, value))
                    InnerAttributes[key] = value;
            }
        #nullable enable

            public void AddStyle(string key, string value)
            {
                if (!InnerStyles.TryAdd(key, value))
                    InnerStyles[key] = value;
            }

            public string CleanedText
            {
                get
                {
                    if (InnerText == string.Empty)
                        return InnerText;

                    var stringBuilder = new StringBuilder();
                    var position = 0;

                    while (true)
                    {
                        var ampPosition = InnerText.IndexOf('&', position);
                        if (ampPosition < 0)
                        {
                            stringBuilder.Append(InnerText[position..InnerText.Length]);
                            break;
                        }

                        var semicolonPosition = InnerText.IndexOfAny(new char[] { ';', ' ' }, ampPosition);
                        if (semicolonPosition < 0)
                        {
                            stringBuilder.Append(InnerText[position..InnerText.Length]);
                            break;
                        }
                        if (InnerText[semicolonPosition] == ' ')
                        {
                            stringBuilder.Append(InnerText[position..semicolonPosition]);
                            position = semicolonPosition;
                            continue;
                        }

                        stringBuilder.Append(InnerText[position..ampPosition]);

                        if (InnerText[ampPosition + 1] == '#')
                        {
                            if (int.TryParse(InnerText[(ampPosition + 2)..semicolonPosition], out var numSymbol))
                                stringBuilder.Append((char)numSymbol);
                            else
                                stringBuilder.Append(InnerText[ampPosition..(semicolonPosition + 1)]);

                            position = semicolonPosition + 1;
                            continue;
                        }

                        if (_symbols.TryGetValue(InnerText[(ampPosition + 1)..semicolonPosition], out var symbol))
                        {
                            stringBuilder.Append(symbol);
                            position = semicolonPosition + 1;
                            continue;
                        }

                        stringBuilder.Append(InnerText[ampPosition..(semicolonPosition + 1)]);
                        position = semicolonPosition + 1;
                    }

                    return stringBuilder.ToString();
                }
            }

            private static readonly Dictionary<string, char> _symbols = new Dictionary<string, char>()
            {
                { "nbsp", '\u00A0'},
                { "shy", '\u00AD'}
            };
        }
    }
}