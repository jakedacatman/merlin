namespace donniebot.classes
{
    public class MuteResult
    {
        public string Message { get; set; }
        public ulong UserId { get; set; }
        public bool IsSuccess { get; set; }

        private MuteResult(string message, ulong id, bool success)
        {
            Message = message;
            UserId = id;
            IsSuccess = success;
        }
        
        public static MuteResult FromSuccess(string message, ulong id) => new MuteResult(message, id, true);
        public static MuteResult FromError(string message, ulong id) => new MuteResult(message, id, false);
    }
}