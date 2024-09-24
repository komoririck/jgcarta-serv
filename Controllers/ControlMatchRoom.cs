using hololive_oficial_cardgame_server;
using Microsoft.AspNetCore.Mvc;
using static hololive_oficial_cardgame_server.DBConnection;

namespace hololive_oficial_cardgame_server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ControlMatchRoom : ControllerBase
    {

        private readonly ILogger<ControlMatchRoom> _logger;

        public ControlMatchRoom(ILogger<ControlMatchRoom> logger)
        {
            _logger = logger;
        }


        [HttpPost]
        public async Task<IActionResult> Post([FromBody] GenericPlayerCommunication _GenericPlayerCommunication)
        {
            PlayerMatchRoom _PlayerMatchRoom = new PlayerMatchRoom();
            try
            {
                switch (_GenericPlayerCommunication.RequestData.type)
                {
                    case "JoinRoom":
                        _PlayerMatchRoom = new DBConnection().JoinMatchRoomQueue(_GenericPlayerCommunication);
                        if (_PlayerMatchRoom == null)
                            return CreatedAtAction(nameof(Post), new { }, new { });
                        break;
                    case "CreateRoom":
                        _PlayerMatchRoom = new DBConnection().CreateMatchRoomQueue(_GenericPlayerCommunication);
                        break;
                    case "CancelRoom":
                        return CreatedAtAction(nameof(Post), new { }, new DBConnection().DismissMatchRoom(_GenericPlayerCommunication));
                        break;
                    case "LeaveRoom":
                        return CreatedAtAction(nameof(Post), new { }, new DBConnection().LeaveMatchRoom(_GenericPlayerCommunication));
                        break;
                    case "JoinTable":
                        return CreatedAtAction(nameof(Post), new { }, new DBConnection().JoinTable(_GenericPlayerCommunication));
                        break;
                    case "LeaveTable":
                        return CreatedAtAction(nameof(Post), new { }, new DBConnection().LeaveTable(_GenericPlayerCommunication));
                        break;
                    case "LockTable":
                        return CreatedAtAction(nameof(Post), new { }, new DBConnection().LockTable(_GenericPlayerCommunication));
                        break;
                    case "UnlockTable":
                        return CreatedAtAction(nameof(Post), new { }, new DBConnection().UnlockTable(_GenericPlayerCommunication));
                        break;
                    case "UpdateRoom":
                        return CreatedAtAction(nameof(Post), new { }, new DBConnection().UpdateRoom(_GenericPlayerCommunication));
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to save CreateAccount data");
                return StatusCode(500, "An error occurred while processing your request");
            }

            return CreatedAtAction(nameof(Post), new { }, _PlayerMatchRoom);
        }
    }
}