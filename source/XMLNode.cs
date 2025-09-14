using Collections.Generic;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace XML
{
    /// <summary>
    /// An XML node, which can contain attributes, text content, and child nodes.
    /// </summary>
    public struct XMLNode : IDisposable, ISerializable, IEquatable<XMLNode>
    {
        private Text name;
        private List<XMLAttribute> attributes;
        private Text content;
        private List<XMLNode> children;
        private bool prologue;

        /// <summary>
        /// Name of the node.
        /// </summary>
        public readonly Text.Borrowed Name => name.Borrow();

        /// <summary>
        /// Possible text content inside the node.
        /// </summary>
        public readonly Text.Borrowed Content => content.Borrow();

        /// <summary>
        /// Checks if this node is a prologue node.
        /// </summary>
        public readonly bool IsPrologue => prologue;

        /// <summary>
        /// Indexer to access child nodes with the <paramref name="index"/>.
        /// </summary>
        public readonly ref XMLNode this[int index]
        {
            get
            {
                if (index >= children.Count)
                {
                    throw new IndexOutOfRangeException();
                }

                return ref children[index];
            }
        }

        /// <summary>
        /// Indexer to access attributes with the <paramref name="name"/>.
        /// </summary>
        public readonly ReadOnlySpan<char> this[ReadOnlySpan<char> name]
        {
            get
            {
                Span<XMLAttribute> attributesSpan = attributes.AsSpan();
                for (int i = 0; i < attributesSpan.Length; i++)
                {
                    XMLAttribute attribute = attributesSpan[i];
                    if (attribute.Name.Equals(name))
                    {
                        return attribute.Value.AsSpan();
                    }
                }

                throw new NullReferenceException($"No attribute {name.ToString()} found");
            }
            set
            {
                Span<XMLAttribute> attributesSpan = attributes.AsSpan();
                for (int i = 0; i < attributesSpan.Length; i++)
                {
                    XMLAttribute attribute = attributesSpan[i];
                    if (attribute.Name.Equals(name))
                    {
                        attribute.Value.CopyFrom(value);
                        return;
                    }
                }

                throw new NullReferenceException($"No attribute {name.ToString()} found");
            }
        }

        /// <summary>
        /// Attributes defining the node.
        /// </summary>
        public readonly ReadOnlySpan<XMLAttribute> Attributes => attributes.AsSpan();

        /// <summary>
        /// Child XML nodes.
        /// </summary>
        public readonly ReadOnlySpan<XMLNode> Children => children.AsSpan();

        /// <summary>
        /// Amount of child nodes.
        /// </summary>
        public readonly int Count => children.Count;

        /// <summary>
        /// Checks if this node has been disposed.
        /// </summary>
        public readonly bool IsDisposed => name.IsDisposed;

#if NET
        /// <summary>
        /// Creates an empty and nameless XML node.
        /// </summary>
        public XMLNode()
        {
            name = new(0);
            attributes = new(4);
            content = new(0);
            children = new(4);
        }
#endif

        /// <summary>
        /// Creates an empty XML node with the given <paramref name="name"/>.
        /// </summary>
        public XMLNode(string name)
        {
            this.name = new(name);
            attributes = new(4);
            content = new(0);
            children = new(4);
        }

        /// <summary>
        /// Creates an empty XML node with the given <paramref name="name"/>.
        /// </summary>
        public XMLNode(ReadOnlySpan<char> name)
        {
            this.name = new(name);
            attributes = new(4);
            content = new(0);
            children = new(4);
        }

        /// <summary>
        /// Creates an XML node with the given <paramref name="name"/>,
        /// containing the given <paramref name="content"/>.
        /// </summary>
        public XMLNode(string name, string content)
        {
            this.name = new(name);
            attributes = new(4);
            this.content = new(content);
            children = new(4);
        }

        /// <summary>
        /// Creates an XML node with the given <paramref name="name"/>,
        /// containing the given <paramref name="content"/>.
        /// </summary>
        public XMLNode(ReadOnlySpan<char> name, ReadOnlySpan<char> content)
        {
            this.name = new(name);
            attributes = new(4);
            this.content = new(content);
            children = new(4);
        }

        private XMLNode(Text name, List<XMLAttribute> attributes, Text content, List<XMLNode> children, bool prologue)
        {
            this.name = name;
            this.attributes = attributes;
            this.content = content;
            this.children = children;
            this.prologue = prologue;
        }

        /// <summary>
        /// Disposes this XML node and its children.
        /// </summary>
        public void Dispose()
        {
            foreach (XMLNode child in children)
            {
                child.Dispose();
            }

            foreach (XMLAttribute attribute in attributes)
            {
                attribute.Dispose();
            }

            children.Dispose();
            content.Dispose();
            attributes.Dispose();
            name.Dispose();
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfAttributeIsMissing(ReadOnlySpan<char> name)
        {
            if (!ContainsAttribute(name))
            {
                throw new NullReferenceException($"No attribute {name.ToString()} found");
            }
        }

        /// <summary>
        /// Retrieves XML formatted string representation of this node.
        /// </summary>
        public readonly override string ToString()
        {
            Text buffer = new(0);
            ToString(buffer, SerializationSettings.PrettyPrinted);
            string str = buffer.AsSpan().ToString();
            buffer.Dispose();
            return str;
        }

        /// <summary>
        /// Retrieves XML formatted string representation of this node
        /// with custom <paramref name="settings"/>.
        /// </summary>
        public readonly string ToString(SerializationSettings settings)
        {
            Text buffer = new(0);
            ToString(buffer, settings);
            string str = buffer.AsSpan().ToString();
            buffer.Dispose();
            return str;
        }

        readonly void ISerializable.Write(ByteWriter byteWriter)
        {
            Write(byteWriter, SerializationSettings.PrettyPrinted);
        }

        /// <summary>
        /// Writes the formatted text of this XML node to the given <paramref name="writer"/>
        /// with the given <paramref name="settings"/>.
        /// </summary>
        public readonly void Write(ByteWriter writer, SerializationSettings settings)
        {
            Text buffer = new(0);
            ToString(buffer, settings, 0);
            writer.WriteUTF8(buffer.AsSpan());
            buffer.Dispose();
        }

        [SkipLocalsInit]
        void ISerializable.Read(ByteReader byteReader)
        {
            attributes = new(4);
            content = new(0);
            children = new(4);

            XMLReader xmlReader = new(byteReader);
            Token token = xmlReader.ReadToken(); //<

            if (xmlReader.TryPeekToken(out Token nextToken) && nextToken.type == Token.Type.Prologue)
            {
                prologue = true;
                xmlReader.ReadToken();
            }

            //read name
            token = xmlReader.ReadToken();
            Span<char> nameBuffer = stackalloc char[256];
            Span<char> valueBuffer = stackalloc char[256];
            int length = xmlReader.GetText(token, nameBuffer);
            name = new(nameBuffer.Slice(0, length));

            //read attributes inside first node
            while (xmlReader.TryReadToken(out token))
            {
                if (token.type == Token.Type.Close)
                {
                    break; //exit first node (assume there will be a closing node)
                }
                else if (token.type == Token.Type.Prologue)
                {
                    continue;
                }
                else if (token.type == Token.Type.Slash)
                {
                    token = xmlReader.ReadToken();
                    if (token.type == Token.Type.Close)
                    {
                        return;
                    }

                    throw new Exception($"Unexpected token `{token.type}` after `{Token.Slash}` when reading end of node attributes");
                }
                else
                {
                    length = xmlReader.GetText(token, nameBuffer);
                    token = xmlReader.ReadToken();
                    int valueLength = xmlReader.GetText(token, valueBuffer);
                    XMLAttribute attribute = new(nameBuffer.Slice(0, length), valueBuffer.Slice(0, valueLength));
                    attributes.Add(attribute);
                }
            }

            if (token.type != Token.Type.Close && token.type != Token.Type.Unknown)
            {
                throw new Exception($"Unexpected token `{token.type}` when reading end of node attributes");
            }

            //read content
            while (xmlReader.TryReadToken(out token))
            {
                if (token.type == Token.Type.Text || token.type == Token.Type.Open)
                {
                    if (token.type == Token.Type.Open)
                    {
                        //check if this open node closes itself
                        if (xmlReader.TryPeekToken(out Token closeToken) && closeToken.type == Token.Type.Slash)
                        {
                            if (xmlReader.TryPeekToken(closeToken.position + closeToken.length, out closeToken) && closeToken.type == Token.Type.Text)
                            {
                                ReadOnlySpan<char> closingName = nameBuffer.Slice(0, xmlReader.GetText(closeToken, nameBuffer));
                                if (name.Equals(closingName))
                                {
                                    xmlReader.ReadToken(); //open
                                    xmlReader.ReadToken(); //slash
                                    xmlReader.ReadToken(); //close
                                    return;
                                }
                                else
                                {
                                    throw new Exception($"Encountered closing node `{closingName.ToString()}` while reading `{Name.ToString()}`");
                                }
                            }
                        }

                        byteReader.Position -= token.length;
                        XMLNode child = xmlReader.ReadNode();
                        children.Add(child);
                    }
                    else
                    {
                        using Text temp = new(token.length);
                        Span<char> tempSpan = temp.AsSpan();
                        int written = byteReader.PeekUTF8(token.position, token.length, tempSpan);
                        content.Append(tempSpan.Slice(0, written));
                        byteReader.Position = token.position + token.length;
                    }

                    if (xmlReader.TryPeekToken(out Token next) && next.type == Token.Type.Open)
                    {
                        xmlReader.TryPeekToken(next.position + next.length, out next);
                        if (next.type == Token.Type.Slash)
                        {
                            xmlReader.ReadToken(); //open
                            xmlReader.ReadToken(); //slash
                            if (xmlReader.TryReadToken(out next) && next.type == Token.Type.Text)
                            {
                                length = xmlReader.GetText(next, nameBuffer);
                                ReadOnlySpan<char> closingName = nameBuffer.Slice(0, length);
                                if (name.Equals(closingName))
                                {
                                    next = xmlReader.ReadToken(); //close
                                    if (next.type != Token.Type.Close)
                                    {
                                        throw new Exception($"Unexpected token `{next.type}` when reading closing node `{closingName.ToString()}`");
                                    }

                                    return;
                                }
                                else
                                {
                                    throw new Exception($"Unexpected closing node `{closingName.ToString()}` when reading node `{Name.ToString()}`");
                                }
                            }
                            else
                            {
                                throw new Exception($"Unexpected token `{next.type}` when reading closing node");
                            }
                        }
                    }
                }
                else
                {
                    throw new Exception($"Unexpected token `{token.type}` when reading content inside a node");
                }
            }
        }

        /// <summary>
        /// Appends the XML node to the given <paramref name="destination"/> using the default serialization settings.
        /// </summary>
        public readonly void ToString(Text destination, SerializationSettings settings = default)
        {
            ToString(destination, settings, 0);
        }

        private readonly void ToString(Text destination, SerializationSettings settings, int depth)
        {
            for (int i = 0; i < depth; i++)
            {
                settings.Indent(destination);
            }

            destination.Append(Token.Open);
            if (prologue)
            {
                destination.Append(Token.Prologue);
            }

            destination.Append(name.AsSpan());

            Span<XMLAttribute> attributesSpan = attributes.AsSpan();
            for (int i = 0; i < attributesSpan.Length; i++)
            {
                destination.Append(' ');
                XMLAttribute attribute = attributesSpan[i];
                attribute.ToString(destination);
            }

            Span<XMLNode> childrenSpan = children.AsSpan();
            if (content.Length > 0 || childrenSpan.Length > 0)
            {
                if (prologue)
                {
                    destination.Append(Token.Prologue);
                }
                else
                {
                    depth++;
                }

                destination.Append(Token.Close);
                destination.Append(content.AsSpan());
                if (childrenSpan.Length > 0)
                {
                    for (int c = 0; c < childrenSpan.Length; c++)
                    {
                        XMLNode child = childrenSpan[c];
                        if ((settings.flags & SerializationSettings.Flags.SkipEmptyNodes) == SerializationSettings.Flags.SkipEmptyNodes)
                        {
                            if (child.content.Length == 0 && child.children.Count == 0 && child.attributes.Count == 0)
                            {
                                continue;
                            }
                        }

                        if (depth == 1 && (settings.flags & SerializationSettings.Flags.RootSpacing) == SerializationSettings.Flags.RootSpacing)
                        {
                            settings.NewLine(destination);
                        }

                        settings.NewLine(destination);
                        child.ToString(destination, settings, depth);
                    }

                    if (depth == 1 && (settings.flags & SerializationSettings.Flags.RootSpacing) == SerializationSettings.Flags.RootSpacing)
                    {
                        settings.NewLine(destination);
                    }

                    settings.NewLine(destination);
                    for (int i = 0; i < depth - 1; i++)
                    {
                        settings.Indent(destination);
                    }
                }

                if (!prologue)
                {
                    destination.Append(Token.Open);
                    destination.Append(Token.Slash);
                    destination.Append(name.AsSpan());
                    destination.Append(Token.Close);
                }
            }
            else
            {
                if (attributesSpan.Length > 0)
                {
                    if ((settings.flags & SerializationSettings.Flags.SpaceBeforeClosingNode) == SerializationSettings.Flags.SpaceBeforeClosingNode)
                    {
                        destination.Append(' ');
                    }
                }

                destination.Append(Token.Slash);
                destination.Append(Token.Close);
            }
        }

        /// <summary>
        /// Adds the given <paramref name="child"/> node to the list of children.
        /// </summary>
        public readonly void AddChild(XMLNode child)
        {
            children.Add(child);
        }

        /// <summary>
        /// Tries to remove the first child node with the given <paramref name="name"/>.
        /// </summary>
        public readonly bool TryRemoveChild(ReadOnlySpan<char> name)
        {
            Span<XMLNode> childrenSpan = children.AsSpan();
            for (int i = 0; i < childrenSpan.Length; i++)
            {
                XMLNode childNode = childrenSpan[i];
                if (childNode.Name.Equals(name))
                {
                    children.RemoveAt(i, out XMLNode removed);
                    removed.Dispose();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes the child node at the given <paramref name="index"/>.
        /// </summary>
        public readonly void RemoveChildAt(int index)
        {
            children.RemoveAt(index, out XMLNode removed);
            removed.Dispose();
        }

        /// <summary>
        /// Removes the child node at the given <paramref name="index"/>.
        /// by swapping it with the last element in the list.
        /// </summary>
        public readonly void RemoveChildAtBySwapping(int index)
        {
            children.RemoveAtBySwapping(index, out XMLNode removed);
            removed.Dispose();
        }

        /// <summary>
        /// Removes the child node at the given <paramref name="index"/>.
        /// <para>
        /// The <paramref name="removed"/> node must be disposed of by the caller.
        /// </para>
        /// </summary>
        public readonly void RemoveChildAt(int index, out XMLNode removed)
        {
            children.RemoveAt(index, out removed);
        }

        /// <summary>
        /// Removes the child node at the given <paramref name="index"/>.
        /// by swapping it with the last element in the list.
        /// <para>
        /// The <paramref name="removed"/> node must be disposed of by the caller.
        /// </para>
        /// </summary>
        public readonly void RemoveChildAtBySwapping(int index, out XMLNode removed)
        {
            children.RemoveAtBySwapping(index, out removed);
        }

        /// <summary>
        /// Tries to remove the given <paramref name="node"/> from the list of children.
        /// <para>
        /// The given <paramref name="node"/> must be disposed of by the caller
        /// if this method returns <see langword="true"/>.
        /// </para>
        /// </summary>
        public readonly bool TryRemoveChild(XMLNode node)
        {
            return children.TryRemove(node);
        }

        /// <summary>
        /// Tries to remove the given <paramref name="node"/> from the list of children
        /// by swapping it with the last element in the list.
        /// <para>
        /// The given <paramref name="node"/> must be disposed of by the caller
        /// if this method returns <see langword="true"/>.
        /// </para>
        /// </summary>
        public readonly bool TryRemoveChildBySwapping(XMLNode node)
        {
            return children.TryRemoveBySwapping(node);
        }

        /// <summary>
        /// Retrieves the index of the given <paramref name="node"/> in the list of children.
        /// </summary>
        public readonly int IndexOfChild(XMLNode node)
        {
            return children.IndexOf(node);
        }

        /// <summary>
        /// Tries to retrieve the index of the given <paramref name="node"/> in the list of children.
        /// </summary>
        /// <returns><see langword="true"/> if the node is contained.</returns>
        public readonly bool TryIndexOfChild(XMLNode node, out int index)
        {
            return children.TryIndexOf(node, out index);
        }

        /// <summary>
        /// Removes and disposes all child nodes and attributes.
        /// </summary>
        public readonly void Clear()
        {
            ClearChildren();
            ClearAttributes();
        }

        /// <summary>
        /// Removes and disposes all child nodes.
        /// </summary>
        public readonly void ClearChildren()
        {
            Span<XMLNode> childrenSpan = children.AsSpan();
            for (int i = 0; i < childrenSpan.Length; i++)
            {
                childrenSpan[i].Dispose();
            }

            children.Clear();
        }

        /// <summary>
        /// Removes and disposes all attributes.
        /// </summary>
        public readonly void ClearAttributes()
        {
            Span<XMLAttribute> attributesSpan = attributes.AsSpan();
            for (int i = 0; i < attributesSpan.Length; i++)
            {
                attributesSpan[i].Dispose();
            }

            attributes.Clear();
        }

        /// <summary>
        /// Retrieves the first child node with the given <paramref name="name"/>.
        /// </summary>
        public readonly XMLNode GetFirstChild(ReadOnlySpan<char> name)
        {
            Span<XMLNode> childrenSpan = children.AsSpan();
            for (int i = 0; i < childrenSpan.Length; i++)
            {
                XMLNode childNode = childrenSpan[i];
                if (childNode.Name.Equals(name))
                {
                    return childNode;
                }
            }

            throw new NullReferenceException($"No child node {name.ToString()} found");
        }

        /// <summary>
        /// Retrieves the first child node with the given <paramref name="name"/>.
        /// </summary>
        public readonly XMLNode GetFirstChild(string name)
        {
            return GetFirstChild(name.AsSpan());
        }

        /// <summary>
        /// Tries to retrieve the first child node with the given <paramref name="name"/>.
        /// </summary>
        /// <returns><see langword="true"/> if a node was found.</returns>
        public readonly bool TryGetFirstChild(ReadOnlySpan<char> name, out XMLNode foundChildNode)
        {
            Span<XMLNode> childrenSpan = children.AsSpan();
            for (int i = 0; i < childrenSpan.Length; i++)
            {
                XMLNode childNode = childrenSpan[i];
                if (childNode.Name.Equals(name))
                {
                    foundChildNode = childNode;
                    return true;
                }
            }

            foundChildNode = default;
            return false;
        }

        /// <summary>
        /// Tries to retrieve the first child node with the given <paramref name="name"/>.
        /// </summary>
        /// <returns><see langword="true"/> if a node was found.</returns>
        public readonly bool TryGetFirstChild(string name, out XMLNode foundChildNode)
        {
            return TryGetFirstChild(name.AsSpan(), out foundChildNode);
        }

        /// <summary>
        /// Retrieves the attribute with the given <paramref name="name"/>.
        /// </summary>
        public readonly ReadOnlySpan<char> GetAttribute(string name)
        {
            return GetAttribute(name.AsSpan());
        }

        /// <summary>
        /// Retrieves the attribute with the given <paramref name="name"/>.
        /// </summary>
        public readonly ReadOnlySpan<char> GetAttribute(ReadOnlySpan<char> name)
        {
            Span<XMLAttribute> attributesSpan = attributes.AsSpan();
            for (int i = 0; i < attributesSpan.Length; i++)
            {
                XMLAttribute attribute = attributesSpan[i];
                if (attribute.Name.Equals(name))
                {
                    return attribute.Value.AsSpan();
                }
            }

            throw new NullReferenceException($"No attribute {name.ToString()} found");
        }

        /// <summary>
        /// Tries to retrieve the attribute with the given <paramref name="name"/>.
        /// </summary>
        /// <returns><see langword="true"/> if an attribute was found.</returns>
        public readonly bool TryGetAttribute(ReadOnlySpan<char> name, out ReadOnlySpan<char> value)
        {
            Span<XMLAttribute> attributesSpan = attributes.AsSpan();
            for (int i = 0; i < attributesSpan.Length; i++)
            {
                XMLAttribute attribute = attributesSpan[i];
                if (attribute.Name.Equals(name))
                {
                    value = attribute.Value.AsSpan();
                    return true;
                }
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Tries to retrieve the attribute with the given <paramref name="name"/>.
        /// </summary>
        /// <returns><see langword="true"/> if an attribute was found.</returns>
        public readonly bool TryGetAttribute(string name, out ReadOnlySpan<char> value)
        {
            return TryGetAttribute(name.AsSpan(), out value);
        }

        /// <summary>
        /// Tries to retrieve the index of the attribute with the given <paramref name="name"/>.
        /// </summary>
        /// <returns><see langword="true"/> if an attribute was found.</returns>
        public readonly bool TryIndexOfAttribute(ReadOnlySpan<char> name, out int index)
        {
            Span<XMLAttribute> attributesSpan = attributes.AsSpan();
            for (int i = 0; i < attributesSpan.Length; i++)
            {
                XMLAttribute attribute = attributesSpan[i];
                if (attribute.Name.Equals(name))
                {
                    index = i;
                    return true;
                }
            }

            index = 0;
            return false;
        }

        /// <summary>
        /// Checks if an attribute with the given <paramref name="name"/> exists.
        /// </summary>
        public readonly bool ContainsAttribute(ReadOnlySpan<char> name)
        {
            Span<XMLAttribute> attributesSpan = attributes.AsSpan();
            for (int i = 0; i < attributesSpan.Length; i++)
            {
                XMLAttribute attribute = attributesSpan[i];
                if (attribute.Name.Equals(name))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if an attribute with the given <paramref name="name"/> exists.
        /// </summary>
        public readonly bool ContainsAttribute(string name)
        {
            return ContainsAttribute(name.AsSpan());
        }

        /// <summary>
        /// Retrieves the index of the attribute with the given <paramref name="name"/>.
        /// </summary>
        public readonly int IndexOfAttribute(ReadOnlySpan<char> name)
        {
            Span<XMLAttribute> attributesSpan = attributes.AsSpan();
            for (int i = 0; i < attributesSpan.Length; i++)
            {
                XMLAttribute attribute = attributesSpan[i];
                if (attribute.Name.Equals(name))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Retrieves the index of the attribute with the given <paramref name="name"/>.
        /// </summary>
        public readonly int IndexOfAttribute(string name)
        {
            return IndexOfAttribute(name.AsSpan());
        }

        /// <summary>
        /// Sets or adds a new attribute or assigns an existing one to the given value.
        /// </summary>
        /// <returns><see langword="true"/> if it was created, otherwise it was set</returns>
        public readonly bool SetOrAddAttribute(ReadOnlySpan<char> name, ReadOnlySpan<char> value)
        {
            Span<XMLAttribute> attributesSpan = attributes.AsSpan();
            for (int i = 0; i < attributesSpan.Length; i++)
            {
                XMLAttribute attribute = attributesSpan[i];
                if (attribute.Name.Equals(name))
                {
                    attribute.Value.CopyFrom(value);
                    return false;
                }
            }

            XMLAttribute newAttribute = new(name, value);
            attributes.Add(newAttribute);
            return true;
        }

        /// <summary>
        /// Sets or adds a new attribute or assigns an existing one to the given value.
        /// </summary>
        /// <returns><see langword="true"/> if it was created, otherwise it was set</returns>
        public readonly bool SetOrAddAttribute(string name, ReadOnlySpan<char> value)
        {
            return SetOrAddAttribute(name.AsSpan(), value);
        }

        /// <summary>
        /// Tries to remove the attribute with the given <paramref name="name"/>.
        /// </summary>
        public readonly bool TryRemoveAttribute(ReadOnlySpan<char> name)
        {
            Span<XMLAttribute> attributesSpan = attributes.AsSpan();
            for (int i = 0; i < attributesSpan.Length; i++)
            {
                if (attributesSpan[i].Name.Equals(name))
                {
                    attributes.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// Tries to remove the attribute with the given <paramref name="name"/>.
        /// </summary>
        public readonly bool TryRemoveAttribute(string name)
        {
            return TryRemoveAttribute(name.AsSpan());
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is XMLNode node && Equals(node);
        }

        /// <inheritdoc/>
        public readonly bool Equals(XMLNode other)
        {
            if (IsDisposed && other.IsDisposed)
            {
                return true;
            }
            else if (IsDisposed != other.IsDisposed)
            {
                return false;
            }

            return name.Equals(other.name) && attributes.Equals(other.attributes) && content.Equals(other.content) && children.Equals(other.children);
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + name.GetHashCode();
            hash = hash * 31 + attributes.GetHashCode();
            hash = hash * 31 + content.GetHashCode();
            hash = hash * 31 + children.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Creates a new empty XML node with no name, attributes, content, or children.
        /// </summary>
        public static XMLNode Create()
        {
            Text name = new(4);
            List<XMLAttribute> attributes = new(4);
            Text content = new(0);
            List<XMLNode> children = new(4);
            return new XMLNode(name, attributes, content, children, false);
        }

        /// <inheritdoc/>
        public static bool operator ==(XMLNode left, XMLNode right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(XMLNode left, XMLNode right)
        {
            return !(left == right);
        }
    }
}