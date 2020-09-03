using System;
using LiteDB;

namespace donniebot.classes
{
    public class ModerationAction
    {
        public string Reason { get; }
        public ulong UserId { get; }
        public ulong ModeratorId { get; }
        public ulong GuildId { get; }
        public bool Active { get; set; }
        public ActionType Type { get; }
        public DateTime Timestamp { get; } = DateTime.Now;
        public TimeSpan ActivePeriod { get; }
        [BsonId]
        public int Id { get; set; }

        public ModerationAction(string reason, ulong uId, ulong mId, ulong gId, ActionType type, TimeSpan period)
        {
            Reason = reason;
            UserId = uId;
            ModeratorId = mId;
            GuildId = gId;
            Active = true;
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