using hololive_oficial_cardgame_server.SerializableObjects;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.Controllers
{
    [Route("[controller]")]
    public class DeckInfoController : ControllerBase
    {
        private readonly ILogger<DeckInfoController> _logger;

        public DeckInfoController(ILogger<DeckInfoController> logger)
        {
            _logger = logger;
        }

        [HttpPost("GetDeck")]
        [Consumes("application/json")]
        public async Task<IActionResult> Put([FromBody] PlayerRequest PlayerInfo)
        {
            try
            {
                DeckData deckData = new DBConnection().GetDeckInfo(PlayerInfo);
                return Ok(deckData);
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to save player Information data");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        [HttpPut("UpdateDeck")]
        [Consumes("application/json")]
        public async Task<IActionResult> Post([FromBody] PlayerRequest PlayerInfo)
        {
            try
            {
                bool didUpdate = DBConnection.UpdateDeckInfo(PlayerInfo);
                return Ok(didUpdate);
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to save player Information data");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }
    }
}
