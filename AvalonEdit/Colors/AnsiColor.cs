/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2023 All rights reserved.
 * @license           : MIT
 */

namespace SuperCom.AvalonEdit.Colors
{

    /// <summary>
    /// ANSI color abstract class.
    /// </summary>
    public abstract class AnsiColor
    {
        /// <summary>
        /// Implicit conversion to a string.
        /// </summary>
        /// <param name="c"></param>
        public static implicit operator string(AnsiColor c) => c.ToString();

        /// <summary>
        /// Returns the AnsiCode property.
        /// </summary>
        public override string ToString()
        {
            return this.AnsiCode;
        }

        /// <summary>
        /// The ANSI sequence.
        /// </summary>
        public abstract string AnsiCode { get; }

        /// <summary>
        /// The corresponding short color code that can be used with the color.
        /// </summary>
        public abstract string MudColorCode { get; }

        /// <summary>
        /// The friendly name of the ANSI color.
        /// </summary>
        public abstract string Name { get; }

    }
}
