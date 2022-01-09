namespace merlin.classes
{
    public class MuteResult
    {
        public string Message { get; set; }
        public ulong UserId { get; set; }
        public bool IsSuccess { get; set; }
        public ModerationAction Action { get; set; }

        private MuteResult(string message, ulong id, bool success)
        {
            Message = message;
            UserId = id;
            IsSuccess = success;
        }

        private MuteResult(ModerationAction action)
        {
            Action = action;
            UserId = action.UserId;
            IsSuccess = true;
        }
        
        public static MuteResult FromSuccess(string message, ulong id) => new MuteResult(message, id, true);
        public static MuteResult FromSuccess(ModerationAction action) => new MuteResult(action);
        public static MuteResult FromError(string message, ulong id) => new MuteResult(message, id, false);
    }
}