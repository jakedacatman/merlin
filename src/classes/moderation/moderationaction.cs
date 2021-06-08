using System;
using LiteDB;

namespace donniebot.classes
{
    public class ModerationAction
    {
        public string Reason { get; set; }
        public ulong UserId { get; set; }
        public ulong ModeratorId { get; set; }
        public ulong GuildId { get; set; }
        public ActionType Type { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public DateTime? Expiry { get; set; }
        [BsonId]
        public int Id { get; set; }

        public ModerationAction() { }

        public ModerationAction(ulong uId, ulong mId, ulong gId, ActionType type, TimeSpan? period = null, string reason = null)
        {
            Reason = reason;
            UserId = uId;
            ModeratorId = mId;
            GuildId = gId;
            Type = type;
            Expiry = period == null ? null : DateTime.UtcNow + period;
        }

        public ModerationAction(ulong uId, ulong mId, ulong gId, ActionType type, DateTime? expiry = null, string reason = null)
        {
            Reason = reason;
            UserId = uId;
            ModeratorId = mId;
            GuildId = gId;
            Type = type;

            if (type != ActionType.Kick)
                Expiry = expiry;
        }
    }

    public enum ActionType
    {
        Mute = 1,
        Kick = 2,
        Ban = 3
    }
}