using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyCodeCamp.Data;
using MyCodeCamp.Data.Entities;
using MyCodeCamp.Filters;
using MyCodeCamp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace MyCodeCamp.Controllers
{
    public class AuthController : Controller
    {
        private CampContext _context;
        private SignInManager<CampUser> _signInMgr;
        private ILogger<AuthController> _logger;
        private UserManager<CampUser> _userMgr;
        private IPasswordHasher<CampUser> _hasher;
        private IConfigurationRoot _config;

        public AuthController(CampContext context, SignInManager<CampUser> signInMgr, ILogger<AuthController> logger,
                              UserManager<CampUser> userMgr, IPasswordHasher<CampUser> hasher, IConfigurationRoot config)
        {
            _context = context;
            _signInMgr = signInMgr;
            _logger = logger;
            _userMgr = userMgr;
            _hasher = hasher;
            _config = config;
        }

        // This Login() method allow us to create a cookie and login as necessary
        [HttpPost("api/auth/login")]
        [ValidateModel]
        public async Task<IActionResult> Login([FromBody] CredentialModel model)  
        {
            try
            {
                var result = await _signInMgr.PasswordSignInAsync(model.UserName, model.Password, false, false);
                if (result.Succeeded)
                {
                    return Ok();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"++++++++++++++++++Exception thrown while logging in++++++++++++++ {ex}");
            }

            return BadRequest("Failed to login");
        }


        /**/ 
        [ValidateModel]
        [HttpPost("api/auth/token")]
        public async Task<IActionResult> CreateToken([FromBody] CredentialModel model)
        {
            try
            {
                var user = await _userMgr.FindByNameAsync(model.UserName);
                if (user != null)
                {
                    if (_hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password) == PasswordVerificationResult.Success)
                    {
                        var userClaims = await _userMgr.GetClaimsAsync(user);

                        // create claims that will be passed to a token
                        var claims = new[]
                        {
                            new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
                            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
                            new Claim(JwtRegisteredClaimNames.Email, user.Email)
                         }.Union(userClaims);

                        // Create signing credentials
                        //var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("VERYLONGKEYVALUETHATISSECURE"));
                        //var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Tokens:Key"]));
                        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                        // create token
                        var token = new JwtSecurityToken(

                          //issuer: "http://mycodecamp.org",
                          //audience: "http://mycodecamp.org",
                          //claims: claims,
                          //expires: DateTime.UtcNow.AddMinutes(15),
                          //signingCredentials: creds
                          issuer: _config["Tokens:Issuer"],
                          audience: _config["Tokens:Audience"],
                          claims: claims,
                          expires: DateTime.UtcNow.AddMinutes(15),
                          signingCredentials: creds
                          );

                        return Ok(new
                        {
                            token = new JwtSecurityTokenHandler().WriteToken(token),
                            expiration = token.ValidTo
                        });
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"+++++++++Exception thrown while creating JWT++++++++++++++++++++++ {ex}");
            }

            return BadRequest("Failed to generate token");
        }


    }
}
