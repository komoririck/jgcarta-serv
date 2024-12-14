namespace hololive_oficial_cardgame_server;

using hololive_oficial_cardgame_server.SerializableObjects;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("[controller]")]
public class AccountAuthenticationController : ControllerBase
{
}
