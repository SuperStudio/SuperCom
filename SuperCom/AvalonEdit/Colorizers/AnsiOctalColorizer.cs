using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using SuperCom.AvalonEdit.Colors;
using SuperCom.AvalonEdit.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace SuperCom.AvalonEdit.Colorizers
{
    public class AnsiOctalColorizer : DocumentColorizingTransformer
    {

        private static string Red = "\\033[0;31m";
        private static string End = "\\033[0m";

        private static int len = Red.Length;

        protected override void ColorizeLine(DocumentLine line)
        {
            var text = CurrentContext.Document.GetText(line);

            if (text.IndexOf("\\033[") == -1) {
                return;
            }

            int index = text.IndexOf(Red);
            int endMarker = text.IndexOf(End);

            if (endMarker == -1) {
                endMarker = text.Length;
            }



            int offset = line.Offset + index;
            if (offset >= 0)
                base.CurrentContext.Document.Replace(offset, len, "");

            base.ChangeLinePart(
                line.Offset + index + len,    // startOffset
                line.Offset + endMarker, // endOffset
                (VisualLineElement element) => {
                    element.TextRunProperties.SetForegroundBrush(Brushes.Red);
                });




            //base.ChangeLinePart(
            //     line.Offset + index + 1,    // startOffset
            //     line.Offset + index + len, // endOffset
            //    (VisualLineElement element) => {
            //        base.CurrentElements.Remove(element);
            //    });

            // Search for next occurrence, again, we'll reference the span and not the length.
            //start = index + ansiLength;



            // Styles that should be applied after the colors are applied.  Styles may reverse the text
            // underline it, make it blink, etc.  They key thing about a style is that they work on top
            // of any color codes that might have already been applied.
            //foreach (var color in Colorizer.StyleMap) {
            //    int start = 0;
            //    int index;
            //    int ansiLength = color.AnsiColor.AnsiCode.Length;

            //    while ((index = text.IndexOf(color.AnsiColor.AnsiCode, start)) >= 0) {
            //        // Find the clear color code if it exists.
            //        int endMarker = text.IndexOf(AnsiColors.Clear.AnsiCode, index + 1);

            //        // If the end marker isn't found on this line then it goes to the end of the line
            //        if (endMarker == -1) {
            //            endMarker = text.Length;
            //        }

            //        // Flip flop the colors
            //        base.ChangeLinePart(
            //            line.Offset + index,    // startOffset
            //            line.Offset + endMarker, // endOffset
            //            (VisualLineElement element) => {
            //                if (color.AnsiColor is Reverse) {
            //                    var foreground = element.TextRunProperties.ForegroundBrush;
            //                    var background = element.BackgroundBrush ?? Brushes.Black;
            //                    element.TextRunProperties.SetForegroundBrush(background);
            //                    element.TextRunProperties.SetBackgroundBrush(foreground);
            //                } else if (color.AnsiColor is Underline) {
            //                    element.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
            //                }
            //            });

            //        // Search for the next occurrence
            //        start = index + ansiLength;
            //    }
            //}
        }
    }
}
