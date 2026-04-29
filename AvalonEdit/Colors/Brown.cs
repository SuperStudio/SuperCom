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
    public class Brown : AnsiColor
    {
        public override string AnsiCode => "\x1B[38;5;130m";

        public override string MudColorCode => "{n";

        public override string Name => "Brown";
    }
}