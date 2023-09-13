using SuperCom.AvalonEdit.Colors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SuperCom.AvalonEdit.Colorizers
{
    public class ColorMap
    {
        /// <summary>
        /// The AnsiColor from the common library.
        /// </summary>
        public AnsiColor AnsiColor { get; set; }

        private SolidColorBrush _solidColorBrush;

        /// <summary>
        /// The WPF SolidColorBrush the AnsiColor maps to.
        /// </summary>
        public SolidColorBrush Brush {
            get => _solidColorBrush;
            set {
                _solidColorBrush = value;

                // So, anything created from the WPF Brushes as most of ours are already frozen.  If
                // new colors are used in the future we can lock them here so perform well.  That said,
                // if we allow these colors to be changed we will need to unlocked them first.
                if (!_solidColorBrush.IsFrozen && _solidColorBrush.CanFreeze) {
                    _solidColorBrush.Freeze();
                }
            }
        }

    }
}
