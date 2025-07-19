using System;
using Unmanaged;

namespace XML
{
    /// <summary>
    /// Settings to customize XML serialization behaviour.
    /// </summary>
    public struct SerializationSettings
    {
        /// <summary>
        /// Default amount of spaces for indentation.
        /// </summary>
        public const int DefaultIndentation = 2;

        /// <summary>
        /// Settings for tightly packed XML serialization.
        /// </summary>
        public static readonly SerializationSettings Default = new(default, 0);

        /// <summary>
        /// Settings for pretty printed XML serialization.
        /// </summary>
        public static readonly SerializationSettings PrettyPrinted = new(Flags.CarriageReturn | Flags.LineFeed, DefaultIndentation);

        /// <summary>
        /// Options set.
        /// </summary>
        public Flags flags;

        /// <summary>
        /// Spaces to indent with.
        /// </summary>
        public int indent;

        /// <summary>
        /// Initializes a new instance with the given <paramref name="flags"/> and <paramref name="indent"/>.
        /// </summary>
        public SerializationSettings(Flags flags, int indent)
        {
            this.flags = flags;
            this.indent = indent;
        }

        /// <summary>
        /// Appends indentation to the given <paramref name="text"/>.
        /// </summary>
        public readonly void Indent(Text text)
        {
            text.Append(' ', indent);
        }

        /// <summary>
        /// Appends indentation to the given <paramref name="writer"/>.
        /// </summary>
        public readonly void Indent(ByteWriter writer)
        {
            writer.WriteUTF8(' ', indent);
        }

        /// <summary>
        /// Appends a new line to the given <paramref name="text"/>.
        /// </summary>
        public readonly void NewLine(Text text)
        {
            if ((flags & Flags.CarriageReturn) == Flags.CarriageReturn)
            {
                text.Append('\r');
            }

            if ((flags & Flags.LineFeed) == Flags.LineFeed)
            {
                text.Append('\n');
            }
        }

        /// <summary>
        /// Flags that modify the serialization behaviour.
        /// </summary>
        [Flags]
        public enum Flags : byte
        {
            /// <summary>
            /// No flags set.
            /// </summary>
            None = 0,

            /// <summary>
            /// The <c>\r</c> should be appended to the end of a line.
            /// </summary>
            CarriageReturn = 1,

            /// <summary>
            /// The <c>\n</c> should be appended to the end of a line.
            /// </summary>
            LineFeed = 2,

            /// <summary>
            /// A gap will be added between the root node and its children.
            /// </summary>
            RootSpacing = 4,

            /// <summary>
            /// Nodes that start and close but don't contain any text or attributes will be omitted.
            /// </summary>
            SkipEmptyNodes = 8
        }
    }
}