using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ash.HTMLParser
{
    public partial class Parser
    {
        private readonly List<Tag> _tags;

        private static readonly char[] typeNameEndChars = new char[] { ' ', '>', '/', '\n' };
        private static readonly char[] attributeEnd = new char[] { ' ', '=', '>', '\n', '\t' };

        public Parser(string document)
        {
            if (string.IsNullOrWhiteSpace(document))
                throw new ArgumentException("Argument 'document' is null or whitespace");

            _tags = new List<Tag>();
            var bodyStart = document.IndexOf("<body") + 1;
            if (bodyStart == 0)
                throw new ArgumentException("Document does not contain tag <body>");

            var (body, _, _) = Parse(document, bodyStart, null);
            _tags.Add(body);
        }

    #nullable enable

        public IReadOnlyList<ITag> Tags => _tags;

        public ITag? TagById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Argument 'id' is null or whitespace");

            return _tags.FirstOrDefault(tag => tag.Id == id);
        }

        public List<ITag> TagByClass(string @class)
        {
            if (string.IsNullOrWhiteSpace(@class))
                throw new ArgumentException("Argument '@class' is null or whitespace");

            return TagByClasses(new string[] { @class });
        }

        public List<ITag> TagByClasses(ICollection<string> classes)
        {
            if (classes == null)
                throw new ArgumentException("Argument 'classes' is null");

            return _tags.Where(tag => classes.All(cs => tag.InnerClasses.Contains(cs))).ToList<ITag>();
        }

        public List<string> AllHrefs(string tagType, ICollection<string>? classes = null)
        {
            if (string.IsNullOrWhiteSpace(tagType))
                throw new ArgumentException("Argument 'tagType' is null or whitespace");

            var result = _tags.Where(tag => tag.InnerType == tagType);
            if (classes != null)
                result = result.Where(tag => classes.All(cs => tag.InnerClasses.Contains(cs)));

            return result.Where(tag => tag.InnerAttributes.ContainsKey("href"))
                .Select(tag => tag.InnerAttributes["href"])
                .ToList();
        }

        #region Parse

        private (Tag?, int, List<string>?) Parse(string document, int position, Tag? parent)
        {
            var tag = new Tag
            {
                InnerParent = parent
            };

            var typeNameEnd = document.IndexOfAny(typeNameEndChars, position);
            var tagType = document[position..typeNameEnd];
            tag.InnerType = tagType;

            var (isTrash, trashEndPosition) = IgnoreTrash(document, position, tag);
            if (isTrash)
                return (null, trashEndPosition, null);

            var returnCloseTagName = new List<string>();

            position = typeNameEnd;
            var actualChar = document[position];
            while (char.IsWhiteSpace(actualChar))
            {
                position++;
                actualChar = document[position];
            }

            if (actualChar == '/' && document[position + 1] == '>')
                return (tag, position + 2, returnCloseTagName);

            if (actualChar != '>')
            {
                position = SetAttributes(document, position, tag);
                actualChar = document[position];

                if (actualChar == '/' && document[position + 1] == '>')
                    return (tag, position + 2, returnCloseTagName);
            }

            position++;

            var stringBuilder = new StringBuilder();
            actualChar = document[position];
            while (actualChar != '<' || document[position + 1] != '/')
            {
                if (actualChar == '<')
                {
                    var (childTag, newPosition, closeTagNames) = Parse(document, position + 1, tag);
                    position = newPosition;
                    actualChar = document[position];
                    if (childTag == null)
                        continue;                    
                    tag.InnerChildren.Add(childTag);
                    _tags.Add(childTag);
                    if (closeTagNames.Count > 0)
                    {
                        if (closeTagNames.Remove(tag.InnerType))
                        {
                            tag.InnerText = stringBuilder.ToString().Trim();
                            return (tag, position, returnCloseTagName);
                        }
                        returnCloseTagName = closeTagNames;
                    }
                }
                else
                {
                    stringBuilder.Append(actualChar);
                    position++;
                    actualChar = document[position];
                }
            }

            tag.InnerText = stringBuilder.ToString().Trim();
            var closeTagPosition = document.IndexOf('>', position);
            var thisClosetagName = document[(position + 2)..closeTagPosition];
            if (thisClosetagName != tag.Type)
                returnCloseTagName.Add(thisClosetagName);
            position = closeTagPosition + 1;

            return (tag, position, returnCloseTagName);
        }

        private (bool, int) IgnoreTrash(string document, int position, Tag tag)
        {
            if (tag.InnerType.StartsWith("!--"))
            {
                tag.InnerType = "!--";
                return (true, document.IndexOf("-->", position) + 3);
            }

            if (tag.InnerType == "script")
                return (true, document.IndexOf("/script>", position) + 8);

            if (tag.InnerType == "style")
                return (true, document.IndexOf("/style>", position) + 7);

            return (false, position);
        }

        private int SetAttributes(string document, int position, Tag tag)
        {
            while (document[position] != '>' && document[position] != '/')
            {
                var attributeEndPosition = document.IndexOfAny(attributeEnd, position);
                var attributeName = document[position..attributeEndPosition];
                position = attributeEndPosition;

                var actualChar = document[position];
                while (char.IsWhiteSpace(actualChar))
                {
                    position++;
                    actualChar = document[position];
                }

                if (actualChar != '=')
                {
                    tag.AddAttribute(attributeName, null);
                    if (actualChar == '>')
                        break;
                    continue;
                }
                position++;
                actualChar = document[position];

                while (char.IsWhiteSpace(actualChar))
                {
                    position++;
                    actualChar = document[position];
                }

                string attributeValue;
                var afterQuoteLength = 1;
                if (actualChar == '\'' || actualChar == '"')
                {
                    afterQuoteLength = 2;
                    attributeValue = document[(position + 1)..document.IndexOf(actualChar, position + 1)];
                }
                else
                    attributeValue = document[(position)..document.IndexOf(' ', position)];

                if (attributeName == "style")
                    SetStyles(attributeValue, tag);
                else if (attributeName == "class")
                    SetClasses(attributeValue, tag);
                else
                    tag.AddAttribute(attributeName, attributeValue);

                position += attributeValue.Length + afterQuoteLength;
                actualChar = document[position];
                while (char.IsWhiteSpace(actualChar))
                {
                    position++;
                    actualChar = document[position];
                }
            }

            return position;
        }

        private void SetStyles(string attributeValue, Tag tag)
        {
            foreach(var attribute in attributeValue.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var value = attribute.Split(':');
                if (value.Length == 2)
                    tag.AddStyle(value[0].Trim(), value[1].Trim());
            }
        }

        private void SetClasses(string attributeValue, Tag tag)
        {
            tag.InnerClasses.AddRange(attributeValue.Split(' '));
        }

        #endregion Parse

        public ITable? TableById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Argument 'id' is null or whitespace");

            var tableTag = _tags.FirstOrDefault(tag => tag.Type == "table" && tag.Id == id);
            if (tableTag == null)
                return null;

            return GetTable(tableTag);
        }

        public ITable? TableByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument 'name' is null or whitespace");

            var tableTag = _tags.FirstOrDefault(tag => tag.Type == "table" && tag.InnerGetAttribute(name) == "name");
            if (tableTag == null)
                return null;

            return GetTable(tableTag);
        }

        private ITable? GetTable(Tag tableTag)
        {
            var result = new Table();
        }
    }
}