using System;
using Unmanaged;

namespace XML
{
    /// <summary>
    /// An XML token.
    /// </summary>
    public readonly struct Token
    {
        /// <summary>
        /// Character for the start of an XML element.
        /// </summary>
        public const char Open = '<';

        /// <summary>
        /// Character for the end of an XML element.
        /// </summary>
        public const char Close = '>';

        /// <summary>
        /// Slash character for closing tags or self-closing elements.
        /// </summary>
        public const char Slash = '/';

        /// <summary>
        /// Character for the prologue token, used in XML declarations or processing instructions.
        /// </summary>
        public const char Prologue = '?';

        /// <summary>
        /// The start position of the token.
        /// </summary>
        public readonly int position;

        /// <summary>
        /// The length of the token.
        /// </summary>
        public readonly int length;

        /// <summary>
        /// The type of the token.
        /// </summary>
        public readonly Type type;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public Token(int position, int length, Type type)
        {
            this.position = position;
            this.length = length;
            this.type = type;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            return $"Token(type: {type} position:{position} length:{length})";
        }

        /// <summary>
        /// Retrieves the string representation of this token.
        /// </summary>
        public readonly string GetText(XMLReader reader)
        {
            using Text buffer = new(0);
            GetText(reader, buffer);
            return buffer.ToString();
        }

        /// <summary>
        /// Copies the text that this token represents from the given <paramref name="reader"/>,
        /// into the <paramref name="destination"/> span.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public readonly int GetText(XMLReader reader, Span<char> destination)
        {
            return reader.GetText(this, destination);
        }

        /// <summary>
        /// Appends the string representation of this token to the <paramref name="destination"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/> values added.</returns>
        public readonly int GetText(XMLReader reader, Text destination)
        {
            switch (type)
            {
                case Type.Open:
                    destination.Append(Open);
                    return 1;
                case Type.Close:
                    destination.Append(Close);
                    return 1;
                case Type.Slash:
                    destination.Append(Slash);
                    return 1;
                case Type.Text:
                    return reader.GetText(this, destination);
                case Type.Prologue:
                    destination.Append(Prologue);
                    return 1;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Types of XML tokens.
        /// </summary>
        public enum Type : byte
        {
            /// <summary>
            /// Uninitialized or unknown token type.
            /// </summary>
            Unknown,

            /// <summary>
            /// Opens an XML element.
            /// </summary>
            Open,

            /// <summary>
            /// Closes an XML element.
            /// </summary>
            Close,

            /// <summary>
            /// Slash token, used for self-closing elements or closing tags.
            /// </summary>
            Slash,

            /// <summary>
            /// Text content within an XML element.
            /// </summary>
            Text,

            /// <summary>
            /// Prologue token, used for XML declarations or processing instructions.
            /// </summary>
            Prologue
        }
    }
}