using hololive_oficial_cardgame_server;
using hololive_oficial_cardgame_server.SerializableObjects;
using Microsoft.AspNetCore.Mvc;

namespace hololive_oficial_cardgame_server.Controllers
{
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {

        private readonly ILogger<AccountController> _logger;

        public AccountController(ILogger<AccountController> logger)
        {
            _logger = logger;
        }

        [HttpPost("Login")]
        [Consumes("application/json")]
        public async Task<IActionResult> Post([FromBody] PlayerRequest _PlayerRequest)
        {
            object returnObject;
            try
            {
                return Ok(new DBConnection().LoginAccount(_PlayerRequest.email, _PlayerRequest.password));
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to save CreateAccount data");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        [HttpPut("CreateAccount")]
        [Consumes("application/json")]
        public async Task<IActionResult> UpdateProfilePicture([FromBody] PlayerRequest _PlayerRequest)
        {
            object returnObject;
            try
            {
                    return Ok(new DBConnection().CreateAccount());
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to save CreateAccount data");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }
    }
}