using System;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace XML
{
    /// <summary>
    /// A reader of XML tokens that wraps an existing <see cref="ByteReader"/>.
    /// </summary>
    [SkipLocalsInit]
    public ref struct XMLReader
    {
        private ByteReader reader;
        private bool insideNode;

        /// <summary>
        /// The position of the reader in <see cref="byte"/>s.
        /// </summary>
        public readonly ref int Position => ref reader.Position;

        /// <summary>
        /// The length of data to read in <see cref="byte"/>s.
        /// </summary>
        public readonly int Length => reader.Length;

#if NET
        /// <inheritdoc/>
        [Obsolete("Default constructor not available", true)]
        public XMLReader()
        {
        }
#endif

        /// <summary>
        /// Creates a new XML format reader on top of the given <see cref="ByteReader"/>.
        /// </summary>
        public XMLReader(ByteReader reader)
        {
            this.reader = reader;
            insideNode = false;
        }

        /// <summary>
        /// Retrieves a span of all bytes in the reader.
        /// </summary>
        public readonly ReadOnlySpan<byte> GetBytes()
        {
            return reader.GetBytes();
        }

        /// <summary>
        /// Tries to peek the next <paramref name="token"/> at the given <paramref name="position"/>.
        /// </summary>
        public readonly bool TryPeekToken(int position, out Token token)
        {
            token = default;
            while (position < reader.Length)
            {
                int bytesRead = reader.PeekUTF8(position, out char c, out char high);
                if (c == Token.Open)
                {
                    token = new Token(position, bytesRead, Token.Type.Open);
                    return true;
                }
                else if (c == Token.Prologue)
                {
                    token = new Token(position, bytesRead, Token.Type.Prologue);
                    return true;
                }
                else if (c == Token.Close)
                {
                    token = new Token(position, bytesRead, Token.Type.Close);
                    return true;
                }
                else if (c == Token.Slash)
                {
                    token = new Token(position, bytesRead, Token.Type.Slash);
                    return true;
                }
                else if (c == '"')
                {
                    int start = position;
                    position += bytesRead;
                    while (position < reader.Length)
                    {
                        bytesRead = reader.PeekUTF8(position, out c, out _);
                        position += bytesRead;
                        if (c == '"')
                        {
                            token = new Token(start, position - start, Token.Type.Text);
                            return true;
                        }
                    }

                    throw new Exception($"Invalid XML, was reading text starting with '\"' but matching one to close was not found");
                }
                else if (c == '=')
                {
                    //skip
                    position += bytesRead;
                }
                else if (char.IsLetterOrDigit(c) || !(char.IsWhiteSpace(c) || c == (char)65279))
                {
                    int start = position;
                    position += bytesRead;
                    while (position < reader.Length)
                    {
                        bytesRead = reader.PeekUTF8(position, out c, out _);
                        if (c == Token.Open)
                        {
                            token = new Token(start, position - start, Token.Type.Text);
                            return true;
                        }
                        else if (insideNode)
                        {
                            if (char.IsWhiteSpace(c) || c == '=' || c == Token.Close || c == Token.Slash)
                            {
                                token = new Token(start, position - start, Token.Type.Text);
                                return true;
                            }
                            else if (!char.IsLetterOrDigit(c))
                            {
                                throw new Exception($"Invalid XML, unknown symbol `{c}` inside node");
                            }
                        }

                        position += bytesRead;
                    }

                    return false;
                }
                else
                {
                    //skip
                    position += bytesRead;
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to peek the next <paramref name="token"/> at the current position of the reader.
        /// </summary>
        public readonly bool TryPeekToken(out Token token)
        {
            return TryPeekToken(reader.Position, out token);
        }

        /// <summary>
        /// Reads the next <see cref="Token"/> from the reader and advances the position.
        /// </summary>
        public Token ReadToken()
        {
            TryPeekToken(out Token token);
            int end = token.position + token.length;
            reader.Position = end;
            if (token.type == Token.Type.Open)
            {
                insideNode = true;
            }
            else if (token.type == Token.Type.Close)
            {
                insideNode = false;
            }

            return token;
        }

        /// <summary>
        /// Tries to read the next <see cref="Token"/> from the reader and advances the position.
        /// </summary>
        public bool TryReadToken(out Token token)
        {
            bool read = TryPeekToken(out token);
            int end = token.position + token.length;
            reader.Position = end;
            if (token.type == Token.Type.Open)
            {
                insideNode = true;
            }
            else if (token.type == Token.Type.Close)
            {
                insideNode = false;
            }

            return read;
        }

        /// <summary>
        /// Reads and creates a new <see cref="XMLNode"/> instance.
        /// </summary>
        public readonly XMLNode ReadNode()
        {
            return reader.ReadObject<XMLNode>();
        }

        /// <summary>
        /// Copies the underlying text of the given <paramref name="token"/> into
        /// the <paramref name="destination"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/> values copied.</returns>
        public readonly int GetText(Token token, Span<char> destination)
        {
            int length = reader.PeekUTF8(token.position, token.length, destination);
            if (destination[0] == '"')
            {
                for (int i = 0; i < length - 1; i++)
                {
                    destination[i] = destination[i + 1];
                }

                return length - 2;
            }
            else return length;
        }

        /// <summary>
        /// Appends the text of the given <paramref name="token"/> to the
        /// given <paramref name="destination"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public readonly int GetText(Token token, Text destination)
        {
            Span<char> buffer = stackalloc char[token.length];
            int length = GetText(token, buffer);
            destination.Append(buffer.Slice(0, length));
            return length;
        }
    }
}