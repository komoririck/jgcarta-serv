using hololive_oficial_cardgame_server;
using hololive_oficial_cardgame_server.SerializableObjects;
using Microsoft.AspNetCore.Mvc;
using static hololive_oficial_cardgame_server.DBConnection;

namespace hololive_oficial_cardgame_server.Controllers
{
    [Route("[controller]")]
    public class ControlMatchQueueController : ControllerBase
    {

        private readonly ILogger<ControlMatchQueueController> _logger;

        public ControlMatchQueueController(ILogger<ControlMatchQueueController> logger)
        {
            _logger = logger;
        }

        [HttpPost("JoinQueue")]
        [Consumes("application/json")]
        public async Task<IActionResult> Post([FromBody] PlayerRequest _PlayerRequest)
        {
            try
            {
                bool success = false;
                switch (_PlayerRequest.description)
                {
                    case "Casual":
                    case "Ranked":
                    case "Room":
                        success = new DBConnection().JoinMatchQueue(_PlayerRequest);
                        break;
                }
                if (!success)
                {
                    throw new Exception("Failed put Information data");
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to save CreateAccount data");
                return StatusCode(500, "An error occurred while processing your request");
            }

            return CreatedAtAction(nameof(Post), new { }, StatusCode(200, "Success"));
        }
        [HttpPut("JoinLeave")]
        [Consumes("application/json")]
        public async Task<IActionResult> Put([FromBody] PlayerRequest _PlayerRequest)
        {
            try
            {
                bool success = false;
                switch (_PlayerRequest.description)
                {
                    case "Cancel":
                        success = new DBConnection().CancelMatchQueue(_PlayerRequest);
                        break;
                }
                if (!success)
                {
                    throw new Exception("Failed put Information data");
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to save CreateAccount data");
                return StatusCode(500, "An error occurred while processing your request");
            }

            return CreatedAtAction(nameof(Put), new { }, StatusCode(201, "Success"));
        }
    }
}