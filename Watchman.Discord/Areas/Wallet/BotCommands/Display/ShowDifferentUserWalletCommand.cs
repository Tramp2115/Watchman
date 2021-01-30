﻿using Devscord.DiscordFramework.Framework.Commands;
using Devscord.DiscordFramework.Framework.Commands.PropertyAttributes;

namespace Watchman.Discord.Areas.Wallet.BotCommands.Display
{
    public class ShowDifferentUserWalletCommand : IBotCommand
    {
        [UserMention]
        public ulong User { get; set; }
    }
}
