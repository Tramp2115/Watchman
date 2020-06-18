﻿using Devscord.DiscordFramework.Framework.Commands.Parsing.Models;
using Devscord.DiscordFramework.Framework.Commands.Responses;
using Devscord.DiscordFramework.Middlewares.Contexts;
using Devscord.DiscordFramework.Services.Factories;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Watchman.Common.Models;
using Watchman.DomainModel.Users;

namespace Watchman.Discord.Areas.Protection.Services
{
    public class AntiSpamService
    {
        private const int LONGER_TIME = 120;
        private const int SHORTER_TIME = 10;
        private const int WARNS_SHORT_EXPIRATION = 600;
        private const int WARNS_LONG_EXPIRATION = 3600 * 6;
        private const int MUTES_EXPIRATION = 3600 * 12;
        private const int FIRST_MUTE_LENGTH = 600;
        private const int SECOND_MUTE_LENGTH = 3600 * 24 * 7;

        private readonly MuteService _muteService;
        private readonly MessagesServiceFactory _messagesServiceFactory;

        public AntiSpamService(MuteService muteService, MessagesServiceFactory messagesServiceFactory)
        {
            this._muteService = muteService;
            this._messagesServiceFactory = messagesServiceFactory;
        }

        private readonly List<(ulong AuthorId, DateTime SentAt)> _lastMessages = new List<(ulong, DateTime)>();
        private readonly List<(ulong AuthorId, DateTime WarnedAt)> _warns = new List<(ulong, DateTime)>();
        private readonly List<(ulong AuthorId, DateTime MutedAt)> _mutes = new List<(ulong, DateTime)>();

        public void AddUserMessage(Contexts contexts, DiscordRequest discordRequest) => this._lastMessages.Add((contexts.User.Id, discordRequest.SentAt));

        public int CountUserMessagesShorterTime(ulong userId)
        {
            return this._lastMessages
                .Where(x => x.SentAt >= DateTime.UtcNow.AddSeconds(-SHORTER_TIME))
                .Count(x => x.AuthorId == userId);
        }

        public int CountUserMessagesLongerTime(ulong userId)
        {
            this.ClearOldMessages();
            return this._lastMessages
                .Count(x => x.AuthorId == userId);
        }

        public int CountUserWarnsInShortTime(ulong userId)
        {
            return this._warns
                .Where(x => x.AuthorId == userId)
                .Count(x => x.WarnedAt > DateTime.UtcNow.AddSeconds(-WARNS_SHORT_EXPIRATION));
        }

        public int CountUserWarnsInLongTime(ulong userId)
        {
            this.ClearOldWarns();
            return this._warns.Count(x => x.AuthorId == userId);
        }

        public int CountUserMutesInLongTime(ulong userId)
        {
            this.ClearOldMutes();
            return this._mutes.Count(x => x.AuthorId == userId);
        }

        public void SetPunishment(Contexts contexts, ProtectionPunishment punishment)
        {
            if (punishment.Option == ProtectionPunishmentOption.Nothing)
            {
                return;
            }

            Log.Information("Spam recognized! User: {user} on channel: {channel} server: {server}",
                            contexts.User.Name, contexts.Channel.Name, contexts.Server.Name);

            var messagesService = this._messagesServiceFactory.Create(contexts);
            switch (punishment.Option)
            {
                case ProtectionPunishmentOption.Warn:
                    this._warns.Add((contexts.User.Id, DateTime.UtcNow));
                    messagesService.SendResponse(x => x.SpamAlertRecognized(contexts), contexts);
                    break;

                case ProtectionPunishmentOption.Mute:
                    this._mutes.Add((contexts.User.Id, DateTime.UtcNow));
                    this.MuteUserForSpam(contexts, FIRST_MUTE_LENGTH).Wait();
                    messagesService.SendResponse(x => x.SpamAlertUserIsMuted(contexts), contexts);
                    break;

                case ProtectionPunishmentOption.LongMute:
                    this.MuteUserForSpam(contexts, SECOND_MUTE_LENGTH).Wait();
                    messagesService.SendResponse(x => x.SpamAlertUserIsMutedForLong(contexts), contexts);
                    break;
            }
        }

        private void ClearOldMessages() => this._lastMessages.RemoveAll(x => x.SentAt < DateTime.UtcNow.AddSeconds(-LONGER_TIME));

        private void ClearOldWarns() => this._warns.RemoveAll(x => x.WarnedAt < DateTime.UtcNow.AddSeconds(-WARNS_LONG_EXPIRATION));

        private void ClearOldMutes() => this._mutes.RemoveAll(x => x.MutedAt < DateTime.UtcNow.AddSeconds(-MUTES_EXPIRATION));

        private async Task MuteUserForSpam(Contexts contexts, int lengthInSeconds)
        {
            var timeRange = new TimeRange(DateTime.UtcNow, DateTime.UtcNow.AddSeconds(lengthInSeconds));
            var muteEvent = new MuteEvent(contexts.User.Id, timeRange, "Spam detected (by bot)", contexts.Server.Id);
            await this._muteService.MuteUserOrOverwrite(contexts, muteEvent, contexts.User);
            this._muteService.UnmuteInFuture(contexts, muteEvent, contexts.User);
        }
    }
}
