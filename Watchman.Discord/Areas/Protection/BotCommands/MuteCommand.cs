﻿using Devscord.DiscordFramework.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using Devscord.DiscordFramework.Commands.PropertyAttributes;

namespace Watchman.Discord.Areas.Protection.BotCommands
{
    public class MuteCommand : IBotCommand
    {
        [UserMention]
        public ulong User { get; set; }
        [Time]
        public TimeSpan Time { get; set; }
        [Text]
        public string Reason { get; set; }
    }
}
