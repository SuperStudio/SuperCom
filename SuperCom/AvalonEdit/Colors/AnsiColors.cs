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
    public static class AnsiColors
    {
        public static AnsiColor Clear { get; } = new Clear();
        public static AnsiColor Default { get; } = new Default();
        public static AnsiColor Reverse { get; } = new Reverse();
        public static AnsiColor Underline { get; } = new Underline();
        public static AnsiColor Red { get; } = new Red();
        public static AnsiColor DarkRed { get; } = new DarkRed();
        public static AnsiColor Black { get; } = new Black();
        public static AnsiColor DarkGray { get; } = new DarkGray();
        public static AnsiColor LightGray { get; } = new LightGray();
        public static AnsiColor Green { get; } = new Green();
        public static AnsiColor DarkGreen { get; } = new DarkGreen();
        public static AnsiColor Yellow { get; } = new Yellow();
        public static AnsiColor DarkYellow { get; } = new DarkYellow();
        public static AnsiColor Blue { get; } = new Blue();
        public static AnsiColor DarkBlue { get; } = new DarkBlue();
        public static AnsiColor Purple { get; } = new Purple();
        public static AnsiColor DarkPurple { get; } = new DarkPurple();
        public static AnsiColor Cyan { get; } = new Cyan();
        public static AnsiColor DarkCyan { get; } = new DarkCyan();
        public static AnsiColor White { get; } = new White();
        public static AnsiColor Orange { get; } = new Orange();
        public static AnsiColor Pink { get; } = new Pink();
        public static AnsiColor Brown { get; } = new Brown();
        public static AnsiColor Magenta { get; } = new Magenta();

        /// <summary>
        /// A List of all of the AnsiColor objects that are supported.
        /// </summary>
        public static List<AnsiColor> ToList()
        {
            return new()
            {
                new Clear(),
                new Green(),
                new DarkGreen(),
                new White(),
                new LightGray(),
                new DarkGray(),
                new Black(),
                new Red(),
                new DarkRed(),
                new Blue(),
                new DarkBlue(),
                new Yellow(),
                new DarkYellow(),
                new Cyan(),
                new DarkCyan(),
                new Purple(),
                new DarkPurple(),
                new Pink(),
                new Brown(),
                new Magenta(),
                new Orange()
            };
        }
    }

}