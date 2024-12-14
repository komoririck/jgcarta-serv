using hololive_oficial_cardgame_server.EffectControllers;
using hololive_oficial_cardgame_server.SerializableObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace hololive_oficial_cardgame_server.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class PlayerInfoController : ControllerBase
    {
        private readonly ILogger<PlayerInfoController> _logger;

        public PlayerInfoController(ILogger<PlayerInfoController> logger)
        {
            _logger = logger;
        }

        [HttpPut("UpdateName")]
        [Consumes("application/json")]
        public IActionResult UpdateName([FromBody] PlayerRequest playerInfo)
        {
            try
            {
                bool success = new DBConnection().UpdatePlayerName(playerInfo);
                if (!success)
                {
                    throw new Exception("Failed to update name.");
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to process data {Exception}", e);
                return StatusCode(500, "An error occurred while processing your request");
            }
            return Ok("Name updated successfully");
        }

        [HttpPut("UpdateProfilePicture")]
        [Consumes("application/json")]
        public IActionResult UpdateProfilePicture([FromBody] PlayerRequest playerInfo)
        {
            try
            {
                bool success = new DBConnection().UpdatePlayerProfilePicture(playerInfo);
                if (!success)
                {
                    throw new Exception("Failed to update profile picture.");
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to process data {Exception}", e);
                return StatusCode(500, "An error occurred while processing your request");
            }
            return Ok("Profile picture updated successfully");
        }

        [HttpPost("GetFullProfile")]
        [Consumes("application/json")]
        public IActionResult GetPlayerFullProfile([FromBody] PlayerRequest getPlayerInfo)
        {
            if (string.IsNullOrEmpty(getPlayerInfo.playerID) || string.IsNullOrEmpty(getPlayerInfo.password))
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "An error occurred while processing your request",
                    Errors = new List<string> { "Invalid player ID or password is missing." }
                });
            }

            try
            {
                PlayerInfo playerInfo = new DBConnection().GetPlayerInfo(getPlayerInfo.playerID, getPlayerInfo.password);
                playerInfo.PlayerItemBox = new DBConnection().GetPlayerItemBox(getPlayerInfo.playerID);
                playerInfo.PlayerTitles = new DBConnection().GetPlayerTitles(getPlayerInfo.playerID);
                playerInfo.Badges = new DBConnection().GetPlayerBadgesV2(getPlayerInfo.playerID);
                playerInfo.PlayerMissionList = new DBConnection().GetPlayerMission(getPlayerInfo.playerID);
                playerInfo.PlayerMessageBox = new DBConnection().GetPlayerMessageBox(getPlayerInfo.playerID);

                return Ok(playerInfo);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to retrieve player full profile data");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An error occurred while processing your request",
                    Errors = new List<string> { e.Message }
                });
            }
        }

        [HttpPost("GetItemBox")]
        [Consumes("application/json")]
        public async Task<IActionResult> GetPlayerItemBox([FromBody] PlayerRequest getPlayerInfo)
        {
            return RetrievePlayerData(getPlayerInfo, () =>
                new DBConnection().GetPlayerItemBox(getPlayerInfo.playerID)
            );
        }

        [HttpPost("GetBadge")]
        [Consumes("application/json")]
        public async Task<IActionResult> GetPlayerBadge([FromBody] PlayerRequest getPlayerInfo)
        {
            return RetrievePlayerData(getPlayerInfo, () =>
                new DBConnection().GetPlayerBadges(getPlayerInfo.playerID)
            );
        }

        [HttpPost("GetTitle")]
        [Consumes("application/json")]
        public async Task<IActionResult> GetPlayerTitle([FromBody] PlayerRequest getPlayerInfo)
        {
            return RetrievePlayerData(getPlayerInfo, () =>
                new DBConnection().GetPlayerTitles(getPlayerInfo.playerID)
            );
        }

        [HttpPost("GetMission")]
        [Consumes("application/json")]
        public async Task<IActionResult> GetPlayerMission([FromBody] PlayerRequest getPlayerInfo)
        {
            return RetrievePlayerData(getPlayerInfo, () =>
                new DBConnection().GetPlayerMission(getPlayerInfo.playerID)
            );
        }

        [HttpPost("GetMessageBox")]
        [Consumes("application/json")]
        public async Task<IActionResult> GetPlayerMessageBox([FromBody] PlayerRequest getPlayerInfo)
        {
            return RetrievePlayerData(getPlayerInfo, () =>
                new DBConnection().GetPlayerMessageBox(getPlayerInfo.playerID)
            );
        }

        [HttpPost("GetProfile")]
        [Consumes("application/json")]
        public IActionResult GetPlayerProfile([FromBody] PlayerRequest getPlayerInfo)
        {
            return RetrievePlayerData(getPlayerInfo, () =>
                new DBConnection().GetPlayerInfo(getPlayerInfo.playerID, getPlayerInfo.password)
            );
        }

        private IActionResult RetrievePlayerData(PlayerRequest getPlayerInfo, Func<object> retrieveDataFunc)
        {
            if (string.IsNullOrEmpty(getPlayerInfo.playerID))
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "An error occurred while processing your request",
                    Errors = new List<string> { "Invalid player ID." }
                });
            }

            try
            {
                var playerData = retrieveDataFunc.Invoke();
                return Ok(playerData);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to retrieve player data");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An error occurred while processing your request",
                    Errors = new List<string> { e.Message }
                });
            }
        }
    }
}
