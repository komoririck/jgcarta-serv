namespace hololive_oficial_cardgame_server.EffectControllers
{
    public class ErrorResponse
    {
        public string Message { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

}
