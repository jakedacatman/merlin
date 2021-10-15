using System;
using LiteDB;

namespace donniebot.classes
{
    public class DjRole
    {
        public ulong GuildId { get; }
        public ulong RoleId { get; }
        [BsonId]
        public int Id { get; set; }

        public DjRole(ulong guildId, ulong roleId)
        {
            GuildId = guildId;
            RoleId = roleId;
        }
    }
}