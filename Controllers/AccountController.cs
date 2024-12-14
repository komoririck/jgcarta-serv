using hololive_oficial_cardgame_server;
using hololive_oficial_cardgame_server.SerializableObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Asn1.Ocsp;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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
        [HttpPost("SessionLogin")]
        [Consumes("application/json")]
        public IActionResult SessionLogin([FromBody] PlayerRequest request)
        {
            if (request == null)
                return StatusCode(500, "Invalid information recieved");

            if (string.IsNullOrEmpty(request.playerID) || string.IsNullOrEmpty(request.password))
                return StatusCode(500, "Invalid information recieved");

            var playerinfo = new DBConnection().LoginSession(request.playerID, request.password);

            if (playerinfo == null)
                return Unauthorized("Invalid credentials");

            return Ok(new { playerinfo.Password });
        }
        [HttpPost("Login")]
        [Consumes("application/json")]
        public async Task<IActionResult> Post([FromBody] PlayerRequest _PlayerRequest)
        {
            try
            {
                var playerInfo = new DBConnection().LoginAccount(_PlayerRequest.email, _PlayerRequest.password);
                var token = GenerateJwtToken(playerInfo.PlayerID + "@" + playerInfo.Password);

                if (new DBConnection().UpdateSessionPassword(playerInfo.PlayerID, token)) 
                {
                    playerInfo.Password = token;
                    return Ok(playerInfo);
                }
                return StatusCode(500, "An error occurred while processing your request");
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to save CreateAccount data");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }
        [HttpPost("CreateAccount")]
        [Consumes("application/json")]
        public async Task<IActionResult> CreateAccount([FromBody] PlayerRequest _PlayerRequest)
        {
            try
            {
                var createdAccount = new DBConnection().CreateAccount();
                var playerInfo = new DBConnection().LoginAccount(createdAccount.email, createdAccount.password, createdAccount.playerID);
                var token = GenerateJwtToken(playerInfo.PlayerID + "@" + playerInfo.Password);

                if (new DBConnection().UpdateSessionPassword(playerInfo.PlayerID, token))
                {
                    playerInfo.Password = token;
                    return Ok(playerInfo);
                }
                return StatusCode(500, "An error occurred while processing your request");
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to save CreateAccount data");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }
        private string GenerateJwtToken(string userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(GetKey());

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.Name, userId)
            }),
                //Expires = DateTime.UtcNow.AddHours(1),
                //Expires = DateTime.UtcNow.AddMinutes(1),
                Issuer = "https://test-server.com",
                Audience = "https://test-api.com",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        static private string _key = "aQwertyUIOPasdfghJKLzxcvBNM123456";
        static public string GetKey()
        {
            return _key;
        }
    }
}