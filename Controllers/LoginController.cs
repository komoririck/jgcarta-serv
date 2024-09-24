using Microsoft.AspNetCore.Mvc;

namespace hololive_oficial_cardgame_server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoginController : ControllerBase
    {

        private readonly ILogger<LoginController> _logger;

        public LoginController(ILogger<LoginController> logger)
        {
            _logger = logger;
        }


        [HttpPost]
        public async Task<IActionResult> Post([FromBody] LoginData loginData)
        {
            PlayerInfo playerInfo = null;   
            try
            {
                playerInfo = new DBConnection().LoginAccount(loginData.Email, loginData.Password);
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to save player Information data");
                return StatusCode(500, "An error occurred while processing your request");
            }

            return CreatedAtAction(nameof(Post), new { }, playerInfo); // Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(User.ToString()))
        }
    }
}
