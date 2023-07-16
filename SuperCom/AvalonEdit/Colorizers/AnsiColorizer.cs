using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using SuperCom.AvalonEdit.Colors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static System.MemoryExtensions;

namespace SuperCom.AvalonEdit.Colorizers
{
    public class AnsiColorizer : DocumentColorizingTransformer
    {
        protected override void ColorizeLine(DocumentLine line)
        {
            // Instead of allocating a string here we're going to use a Span and work from it.
            var text = CurrentContext.Document.GetText(line).AsSpan();

            // There are no ANSI codes on this line, no need to further process.
            if (text.IndexOf('\x1B') == -1) {
                return;
            }

            foreach (var color in Colorizer.ColorMap) {
                int start = 0;
                int index;
                int ansiLength = color.AnsiColor.AnsiCode.AsSpan().Length;

                while ((index = text.IndexOf(color.AnsiColor.AnsiCode.AsSpan(), start)) >= 0) {
                    // This should look for the index of the next color code EXCEPT when it's a style code.
                    int endMarker = text.IndexOfNextColorCode(index + 1);

                    // If the end marker isn't found on this line then it goes to the end of the line
                    if (endMarker == -1) {
                        endMarker = text.Length;
                    }

                    // TODO: To get rid of the closure allocation here I believe AvalonEdit would need to be updated
                    //       to support the action accepting additional state info, like the foreground/background brush.
                    // All of the text that needs to be colored
                    base.ChangeLinePart(
                        line.Offset + index,    // startOffset
                        line.Offset + endMarker, // endOffset
                        (VisualLineElement element) => {
                            element.TextRunProperties.SetForegroundBrush(color.Brush);
                        });

                    // Search for next occurrence, again, we'll reference the span and not the length.
                    start = index + ansiLength;
                }
            }

            // Styles that should be applied after the colors are applied.  Styles may reverse the text
            // underline it, make it blink, etc.  They key thing about a style is that they work on top
            // of any color codes that might have already been applied.
            foreach (var color in Colorizer.StyleMap) {
                int start = 0;
                int index;
                int ansiLength = color.AnsiColor.AnsiCode.AsSpan().Length;

                while ((index = text.IndexOf(color.AnsiColor.AnsiCode.AsSpan(), start)) >= 0) {
                    // Find the clear color code if it exists.
                    int endMarker = text.IndexOf(AnsiColors.Clear.AnsiCode.AsSpan(), index + 1);

                    // If the end marker isn't found on this line then it goes to the end of the line
                    if (endMarker == -1) {
                        endMarker = text.Length;
                    }

                    // Flip flop the colors
                    base.ChangeLinePart(
                        line.Offset + index,    // startOffset
                        line.Offset + endMarker, // endOffset
                        (VisualLineElement element) => {
                            if (color.AnsiColor is Reverse) {
                                var foreground = element.TextRunProperties.ForegroundBrush;
                                var background = element.BackgroundBrush ?? Brushes.Black;
                                element.TextRunProperties.SetForegroundBrush(background);
                                element.TextRunProperties.SetBackgroundBrush(foreground);
                            } else if (color.AnsiColor is Underline) {
                                element.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
                            }
                        });

                    // Search for the next occurrence
                    start = index + ansiLength;
                }
            }
        }
    }
}
