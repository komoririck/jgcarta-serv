﻿using hololive_oficial_cardgame_server.EffectControllers;
using hololive_oficial_cardgame_server.SerializableObjects;
using Microsoft.AspNetCore.Mvc;
using static hololive_oficial_cardgame_server.DBConnection;

namespace hololive_oficial_cardgame_server.Controllers
{
    [Route("[controller]")]
    public class ControlMatchRoomController : ControllerBase
    {

        private readonly ILogger<ControlMatchRoomController> _logger;

        public ControlMatchRoomController(ILogger<ControlMatchRoomController> logger)
        {
            _logger = logger;
        }

        [HttpPut("JoinRoom")]
        [Consumes("application/json")]
        public async Task<IActionResult> JoinRoom([FromBody] PlayerRequest getPlayerInfo)
        {
            return await ManegerRequest(getPlayerInfo, () => new DBConnection().JoinMatchRoomQueue(getPlayerInfo)
            );
        }
        [HttpPut("CreateRoom")]
        [Consumes("application/json")]
        public async Task<IActionResult> CreateRoom([FromBody] PlayerRequest getPlayerInfo)
        {
            return await ManegerRequest(getPlayerInfo, () => new DBConnection().CreateMatchRoomQueue(getPlayerInfo)
            );
        }
        [HttpDelete("CancelRoom")]
        [Consumes("application/json")]
        public async Task<IActionResult> CancelRoom([FromBody] PlayerRequest getPlayerInfo)
        {
            return await ManegerRequest(getPlayerInfo, () => new DBConnection().DismissMatchRoom(getPlayerInfo)
            );
        }
        [HttpPut("LeaveRoom")]
        [Consumes("application/json")]
        public async Task<IActionResult> LeaveRoom([FromBody] PlayerRequest getPlayerInfo)
        {
            return await ManegerRequest(getPlayerInfo, () => new DBConnection().LeaveMatchRoom(getPlayerInfo)
            );
        }
        [HttpPost("JoinTable")]
        [Consumes("application/json")]
        public async Task<IActionResult> JoinTable([FromBody] PlayerRequest getPlayerInfo)
        {
            return await ManegerRequest(getPlayerInfo, () => new DBConnection().JoinTable(getPlayerInfo)
            );
        }
        [HttpDelete("LeaveTable")]
        [Consumes("application/json")]
        public async Task<IActionResult> LeaveTable([FromBody] PlayerRequest getPlayerInfo)
        {
            return await ManegerRequest(getPlayerInfo, () => new DBConnection().LeaveTable(getPlayerInfo)
            );
        }
        [HttpPut("LockTable")]
        [Consumes("application/json")]
        public async Task<IActionResult> LockTable([FromBody] PlayerRequest getPlayerInfo)
        {
            return await ManegerRequest(getPlayerInfo, () => new DBConnection().LockTable(getPlayerInfo)
            );
        }
        [HttpPut("UnlockTable")]
        [Consumes("application/json")]
        public async Task<IActionResult> UnlockTable([FromBody] PlayerRequest getPlayerInfo)
        {
            return await ManegerRequest(getPlayerInfo, () => new DBConnection().UnlockTable(getPlayerInfo)
            );
        }
        [HttpPut("UpdateRoom")]
        [Consumes("application/json")]
        public async Task<IActionResult> GetPlayerProfile([FromBody] PlayerRequest getPlayerInfo)
        {
            return await ManegerRequest(getPlayerInfo, () => new DBConnection().UpdateRoom(getPlayerInfo)
            );
        }
        private async Task<IActionResult> ManegerRequest(PlayerRequest getPlayerInfo, Func<object> retrieveDataFunc)
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