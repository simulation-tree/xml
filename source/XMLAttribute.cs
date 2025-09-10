using System;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace XML
{
    /// <summary>
    /// An attribute of an XML element, consisting of a name and a value.
    /// </summary>
    [SkipLocalsInit]
    public readonly struct XMLAttribute : IDisposable
    {
        //todo: maybe these could be both in 1 Text value, and then a int field to describe the length of the name?
        //this would reduce allocation count. but then they individually couldnt be borrowed.
        private readonly Text name;
        private readonly Text value;

        /// <summary>
        /// The name of the attribute.
        /// </summary>
        public readonly Text.Borrowed Name => name.Borrow();

        /// <summary>
        /// The value contained in the attribute.
        /// </summary>
        public readonly Text.Borrowed Value => value.Borrow();

        /// <summary>
        /// Checks if this attribute has been disposed.
        /// </summary>
        public readonly bool IsDisposed => name.IsDisposed;

        /// <summary>
        /// Creates a new XML attribute with the given <paramref name="name"/> containing the <paramref name="value"/>.
        /// </summary>
        public XMLAttribute(ReadOnlySpan<char> name, ReadOnlySpan<char> value)
        {
            this.name = new(name);
            this.value = new(value);
        }

        /// <summary>
        /// Creates a new XML attribute with the given <paramref name="name"/> and <paramref name="value"/>.
        /// </summary>
        public XMLAttribute(string name, string value)
        {
            this.name = new(name);
            this.value = new(value);
        }

        /// <summary>
        /// Creates a new XML attribute by reading from the given <paramref name="reader"/>.
        /// </summary>
        public XMLAttribute(ref XMLReader reader)
        {
            Token nameToken = reader.ReadToken();
            if (nameToken.type != Token.Type.Text)
            {
                throw new();
            }

            Span<char> buffer = stackalloc char[256];
            int length = reader.GetText(nameToken, buffer);
            name = new(buffer.Slice(0, length));

            Token valueToken = reader.ReadToken();
            if (valueToken.type != Token.Type.Text)
            {
                throw new();
            }

            length = reader.GetText(valueToken, buffer);
            value = new(buffer.Slice(0, length));
        }

        /// <inheritdoc/>
        public readonly void Dispose()
        {
            name.Dispose();
            value.Dispose();
        }

        /// <inheritdoc/>
        public unsafe readonly override string ToString()
        {
            Text tempList = new(0);
            ToString(tempList);
            string result = tempList.ToString();
            tempList.Dispose();
            return result;
        }

        /// <summary>
        /// Appends the string representation of this attribute to the given <paramref name="destination"/>.
        /// </summary>
        public readonly void ToString(Text destination)
        {
            destination.Append(name.AsSpan());
            destination.Append('=');
            destination.Append('"');
            destination.Append(value.AsSpan());
            destination.Append('"');
        }
    }
}