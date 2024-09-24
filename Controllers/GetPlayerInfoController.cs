using Microsoft.AspNetCore.Mvc;

namespace hololive_oficial_cardgame_server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GetPlayerInfoController : ControllerBase
    {

        private readonly ILogger<GetPlayerInfoController> _logger;

        public GetPlayerInfoController(ILogger<GetPlayerInfoController> logger)
        {
            _logger = logger;
        }


        [HttpPost]
        public async Task<IActionResult> Post([FromBody] GetPlayerInfo getPlayerInfo)
        {
            if (getPlayerInfo.PlayerID.Equals(0) || getPlayerInfo.PlayerID.Equals(null) || string.IsNullOrEmpty(getPlayerInfo.Password) )
            {
                return StatusCode(501, "An error occurred while processing your request, invalid player ");
            }
            try
            {

                switch (getPlayerInfo.RequestData.type)
                {
                    case "PlayerFullProfile":
                        PlayerInfo getPlayerFulInfoReturn = new PlayerInfo();
                        getPlayerFulInfoReturn = new DBConnection().GetPlayerInfo(getPlayerInfo.PlayerID, getPlayerInfo.Password);

                        getPlayerFulInfoReturn.PlayerItemBox = new DBConnection().GetPlayerItemBox(getPlayerInfo.PlayerID);
                        getPlayerFulInfoReturn.PlayerTitles = new DBConnection().GetPlayerTitles(getPlayerInfo.PlayerID);
                        getPlayerFulInfoReturn.Badges = new DBConnection().GetPlayerBadgesV2(getPlayerInfo.PlayerID);
                        getPlayerFulInfoReturn.PlayerMissionList = new DBConnection().GetPlayerMission(getPlayerInfo.PlayerID);
                        getPlayerFulInfoReturn.PlayerMessageBox = new DBConnection().GetPlayerMessageBox(getPlayerInfo.PlayerID);

                        return CreatedAtAction(nameof(Post), new { }, getPlayerFulInfoReturn);
                        break;

                    case "PlayerItemBox":
                        List<PlayerItemBox> getPlayerItemBoxInfoReturn = new List<PlayerItemBox>();
                        getPlayerItemBoxInfoReturn = new DBConnection().GetPlayerItemBox(getPlayerInfo.PlayerID);
                        return CreatedAtAction(nameof(Post), new { }, getPlayerItemBoxInfoReturn);
                        break;
                    case "PlayerBadge":
                        List<PlayerBadge> getPlayerBadgeInfoReturn = new List<PlayerBadge>();
                        getPlayerBadgeInfoReturn = new DBConnection().GetPlayerBadges(getPlayerInfo.PlayerID);
                        return CreatedAtAction(nameof(Post), new { }, getPlayerBadgeInfoReturn);
                        break;
                    case "PlayerTitle":
                        List<PlayerTitle> getPlayerTitleInfoReturn = new List<PlayerTitle>();
                        getPlayerTitleInfoReturn = new DBConnection().GetPlayerTitles(getPlayerInfo.PlayerID);
                        return CreatedAtAction(nameof(Post), new { }, getPlayerTitleInfoReturn);
                        break;
                    case "PlayerMission":
                        List<PlayerMission> getPlayerMissionInfoReturn = new List<PlayerMission>();
                        getPlayerMissionInfoReturn = new DBConnection().GetPlayerMission(getPlayerInfo.PlayerID);
                        return CreatedAtAction(nameof(Post), new { }, getPlayerMissionInfoReturn);
                        break;
                    case "PlayerMessageBox":
                        List<PlayerMessageBox> getPlayerMessageBoxInfoReturn = new List<PlayerMessageBox>();
                        getPlayerMessageBoxInfoReturn = new DBConnection().GetPlayerMessageBox(getPlayerInfo.PlayerID);
                        return CreatedAtAction(nameof(Post), new { }, getPlayerMessageBoxInfoReturn);
                        break;
                    case "PlayerProfile":
                        PlayerInfo getPlayerInfoReturn = new PlayerInfo();
                        getPlayerInfoReturn = new DBConnection().GetPlayerInfo(getPlayerInfo.PlayerID, getPlayerInfo.Password);
                        return CreatedAtAction(nameof(Post), new { }, getPlayerInfoReturn);
                        break;
                }

            }
            catch (Exception e)
            {
                _logger.LogError("Failed to save player Information data");
                return StatusCode(500, "An error occurred while processing your request");
            }

            return null;
            // Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(User.ToString()))
        }
    }
}
