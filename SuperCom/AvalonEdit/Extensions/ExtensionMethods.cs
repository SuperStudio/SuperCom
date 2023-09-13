using SuperCom.AvalonEdit.Colors;
using System.Text;

namespace SuperCom.AvalonEdit.Extensions
{
    public static class ExtensionMethods
    {

        public static bool IsAnsiString(this string text)
        {
            if (text.IndexOf('\x1B') >= 0 || text.IndexOf("\\x1B") >= 0) {
                return true;
            }
            return false;
        }
        public static bool IsAnsiString(this StringBuilder builder)
        {
            return builder.ToString().IsAnsiString();
        }

        public static int IndexOf(this StringBuilder builder, char value)
        {
            int end = builder.Length;
            for (int i = 0; i < end; i++) {
                if (builder[i] == value)
                    return i;
            }
            return -1;
        }

        public static int IndexOfNextColorCode(this string span, int startIndex)
        {
            // Look up both the starting position of the value and the second value that
            // it isn't supposed to be after those positions.
            int index = span.IndexOf('\x1B', startIndex);

            // Not found at all, return -1.
            if (index == -1) {
                return -1;
            }

            int reverseIndex = span.IndexOf(AnsiColors.Reverse.AnsiCode, startIndex);
            int underlineIndex = span.IndexOf(AnsiColors.Underline.AnsiCode, startIndex);

            // Index isn't -1 and the notIndex was not found, return the index.
            if (index != reverseIndex && index != underlineIndex) {
                return index;
            }

            // index and notIndex are equal, search for the next location where index exists
            // but notIndex does not.
            int start = index;

            while ((span.IndexOf('\x1B', start)) >= 0) {
                index = span.IndexOf('\x1B', start);

                // Not found at all, return -1.
                if (index == -1) {
                    return -1;
                }

                reverseIndex = span.IndexOf(AnsiColors.Reverse.AnsiCode, start);
                underlineIndex = span.IndexOf(AnsiColors.Underline.AnsiCode, start);

                // Index isn't -1 and the notIndex was not found, return the index.
                if (index != reverseIndex && index != underlineIndex) {
                    return index;
                }

                // Increment the start position and search for the next occurrence at the top
                // of the loop.
                start = index + 1;
            }

            // No instances of the value were found where notValue wasn't found at the same
            // position.  Return -1;
            return -1;
        }
    }
}
