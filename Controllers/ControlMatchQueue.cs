using hololive_oficial_cardgame_server;
using Microsoft.AspNetCore.Mvc;
using static hololive_oficial_cardgame_server.DBConnection;

namespace hololive_oficial_cardgame_server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ControlMatchQueue : ControllerBase
    {

        private readonly ILogger<ControlMatchQueue> _logger;

        public ControlMatchQueue(ILogger<ControlMatchQueue> logger)
        {
            _logger = logger;
        }


        [HttpPost]
        public async Task<IActionResult> Post([FromBody] GenericPlayerCommunication _GenericPlayerCommunication)
        {
            DBConnection.ReturnMessage _ReturnMessage = new DBConnection.ReturnMessage("Error");
            try
            {
                switch (_GenericPlayerCommunication.RequestData.description)
                {
                    case "Casual":
                    case "Ranked":
                    case "Room":
                        _ReturnMessage = new DBConnection().JoinMatchQueue(_GenericPlayerCommunication);
                        break;
                    case "Cancel":
                        _ReturnMessage = new DBConnection().CancelMatchQueue(_GenericPlayerCommunication);
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to save CreateAccount data");
                return StatusCode(500, "An error occurred while processing your request");
            }

            return CreatedAtAction(nameof(Post), new { }, _ReturnMessage);
        }
    }
}