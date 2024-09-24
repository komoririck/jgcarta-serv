using hololive_oficial_cardgame_server;
using Microsoft.AspNetCore.Mvc;

namespace hololive_oficial_cardgame_server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CreateAccountController : ControllerBase
    {

        private readonly ILogger<CreateAccountController> _logger;

        public CreateAccountController(ILogger<CreateAccountController> logger)
        {
            _logger = logger;
        }


        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CreateAccount createAccount)
        {
            try
            {
                createAccount = new DBConnection().CreateAccount();
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to save CreateAccount data");
                return StatusCode(500, "An error occurred while processing your request");
            }

            return CreatedAtAction(nameof(Post), new { createAccount.PlayerID, createAccount.Password }, createAccount); // Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(User.ToString()))
        }
    }
}