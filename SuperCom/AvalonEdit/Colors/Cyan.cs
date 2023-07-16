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
    public class Cyan : AnsiColor
    {
        public override string AnsiCode => "\x1B[1;36m";

        public override string MudColorCode => "{C";

        public override string Name => "Cyan";
    }
}
