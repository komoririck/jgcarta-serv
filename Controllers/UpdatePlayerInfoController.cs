using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;

namespace hololive_oficial_cardgame_server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UpdatePlayerInfoController : ControllerBase
    {

        private readonly ILogger<UpdatePlayerInfoController> _logger;

        public UpdatePlayerInfoController(ILogger<UpdatePlayerInfoController> logger)
        {
            _logger = logger;
        }


        [HttpPost]
        public async Task<IActionResult> Post([FromBody] PlayerInfo PlayerInfo)
        {

            DBConnection.ReturnMessage ReturnInfo = new DBConnection.ReturnMessage("Error");
            try
            {
                switch (PlayerInfo.RequestData.type)
                {
                    case "UpdateName":
                        ReturnInfo = new DBConnection().UpdatePlayerName(PlayerInfo);
                        break;
                    case "UpdateProfilePicture":
                        ReturnInfo = new DBConnection().UpdatePlayerProfilePicture(PlayerInfo);
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to save player Information data");
                return StatusCode(500, "An error occurred while processing your request");
            }
            return CreatedAtAction(nameof(Post), new { }, ReturnInfo);
        }
    }
}
