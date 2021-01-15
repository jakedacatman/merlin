using System;

namespace donniebot.classes
{
    public class DjRole
    {
        public ulong GuildId { get; }
        public ulong RoleId { get; }
        public int Id { get; set; }

        public DjRole(ulong guildId, ulong roleId)
        {
            GuildId = guildId;
            RoleId = roleId;
        }
    }
}