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
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public TimeSpan ActivePeriod { get; set; }
        [BsonId]
        public int Id { get; set; }

        public ModerationAction() { }

        public ModerationAction(string reason, ulong uId, ulong mId, ulong gId, ActionType type, TimeSpan period)
        {
            Reason = reason;
            UserId = uId;
            ModeratorId = mId;
            GuildId = gId;
            Type = type;
            ActivePeriod = period;
        }
    }

    public enum ActionType
    {
        Mute = 1,
        Kick = 2,
        Ban = 4
    }
}