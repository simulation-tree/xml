using System;
using Unmanaged;

namespace XML
{
    /// <summary>
    /// A writer for XML data.
    /// </summary>
    public readonly struct XMLWriter : IDisposable
    {
        private readonly ByteWriter writer;

        /// <summary>
        /// Checks if the writer is disposed.
        /// </summary>
        public readonly bool IsDisposed => writer.IsDisposed;

#if NET
        /// <summary>
        /// Creates an empty writer.
        /// </summary>
        public XMLWriter()
        {
            writer = new(4);
        }
#endif

        private XMLWriter(ByteWriter writer)
        {
            this.writer = writer;
        }

        /// <inheritdoc/>
        public readonly void Dispose()
        {
            writer.Dispose();
        }

        /// <summary>
        /// Writes the start of an <see cref="XMLNode"/>
        /// </summary>
        public readonly void WriteStartObject()
        {
            writer.WriteUTF8(Token.Open);
        }

        /// <summary>
        /// Writes the end of an <see cref="XMLNode"/>
        /// </summary>
        public readonly void WriteEndObject()
        {
            writer.WriteUTF8(Token.Close);
        }

        /// <summary>
        /// Writes a <c>/</c>.
        /// </summary>
        public readonly void WriteSlash()
        {
            writer.WriteUTF8(Token.Slash);
        }

        /// <summary>
        /// Writes an attribute <paramref name="name"/> and <paramref name="value"/> pair.
        /// </summary>
        public readonly void WriteAttribute(ReadOnlySpan<char> name, ReadOnlySpan<char> value)
        {
            writer.WriteUTF8(name);
            writer.WriteUTF8('=');
            writer.WriteUTF8('"');
            writer.WriteUTF8(value);
            writer.WriteUTF8('"');
        }

        /// <summary>
        /// Writes an attribute <paramref name="name"/> and <paramref name="value"/> pair.
        /// </summary>
        public readonly void WriteAttribute(string name, string value)
        {
            writer.WriteUTF8(name);
            writer.WriteUTF8('=');
            writer.WriteUTF8('"');
            writer.WriteUTF8(value);
            writer.WriteUTF8('"');
        }

        /// <summary>
        /// Writes the given <paramref name="value"/>.
        /// </summary>
        public readonly void WriteText(ReadOnlySpan<char> value)
        {
            writer.WriteUTF8(value);
        }

        /// <summary>
        /// Writes the given <paramref name="value"/>.
        /// </summary>
        public readonly void WriteText(string value)
        {
            writer.WriteUTF8(value);
        }

        /// <summary>
        /// Creates a new empty writer.
        /// </summary>
        public static XMLWriter Create()
        {
            return new(new(4));
        }
    }
}