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
    public class Orange : AnsiColor
    {
        public override string AnsiCode => "\x1B[38;5;166m";

        public override string MudColorCode => "{o";

        public override string Name => "Orange";
    }
}