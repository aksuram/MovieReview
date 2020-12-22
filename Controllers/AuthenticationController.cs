using Dapper;
using JWT.Algorithms;
using JWT.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MovieReview.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieReview.Controllers
{
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private IConfiguration Configuration { get; set; }

        public AuthenticationController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // POST: api/login
        [Route("api/login")]
        [HttpPost]
        public async Task<ActionResult<JwtToken>> Login(LoginUser user)
        {
            await using var Connection = new NpgsqlConnection(Configuration.GetConnectionString("DatabaseUrl"));
            var User = await Connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM public.user WHERE username = @username", new { username = user.Username });

            if (User == null)
            {
                return Unauthorized();
            }

            if (User.Password != user.Password)
            {
                return Unauthorized();
            }

            var token = new JwtBuilder()
                .WithAlgorithm(new HMACSHA256Algorithm())
                .WithSecret(Configuration["Jwt:SecretKey"])
                .AddClaim("exp", DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds())
                .AddClaim("role", User.Role)
                .AddClaim("id", User.Id)
                .AddClaim("user", User.Username)
                .Encode();

            JwtToken Token = new JwtToken(token);

            return Token;
        }
    }
}
