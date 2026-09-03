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
    public class White : AnsiColor
    {
        public override string AnsiCode => "\x1B[1;37m";

        public override string MudColorCode => "{W";

        public override string Name => "White";

    }
}