using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using JWT;
using JWT.Algorithms;
using JWT.Builder;
using JWT.Exceptions;
using JWT.Serializers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MovieReview.Models;
using Npgsql;

namespace MovieReview.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private IConfiguration Configuration { get; set; }

        public UserController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // GET: api/users
        //everyone
        //paging
        [HttpGet]
        public async Task<ActionResult<List<User>>> GetUsers()
        {
            await using var Connection = new NpgsqlConnection(Configuration.GetConnectionString("DatabaseUrl"));
            var Users = await Connection.QueryAsync<User>("SELECT * FROM public.user ORDER BY id");

            if (!Users.Any())
            {
                return NotFound();
            }

            return Users.ToList();
        }

        // GET api/users/{id}
        //everyone
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            await using var Connection = new NpgsqlConnection(Configuration.GetConnectionString("DatabaseUrl"));
            var User = await Connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM public.user WHERE id = @id", new { id = id });

            if (User == null)
            {
                return NotFound();
            }

            return User;
        }

        // PUT api/users/{id}
        //user (admin everything)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            //NEEDS AN ADMIN MODE
            //Get token from the header
            Microsoft.Extensions.Primitives.StringValues header;
            if (!Request.Headers.TryGetValue("Authorization", out header))
            {
                return Forbid();
            }
            header.ToString();
            string[] strings = header.ToString().Split(' ');

            //Check if format is as expected
            if (strings.Length < 2)
            {
                return Forbid();
            }

            if (strings[0].ToLower() != "bearer")
            {
                return Forbid();
            }

            string token = strings[1];
            IDictionary<string, object> Claims = null;
            //Verify token
            try
            {
                IJsonSerializer serializer = new JsonNetSerializer();
                var provider = new UtcDateTimeProvider();
                IJwtValidator validator = new JwtValidator(serializer, provider);
                IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
                IJwtAlgorithm algorithm = new HMACSHA256Algorithm();
                IJwtDecoder decoder = new JwtDecoder(serializer, validator, urlEncoder, algorithm);

                Claims = decoder.DecodeToObject<IDictionary<string, object>>(token, Configuration["Jwt:SecretKey"], true);
            }
            catch (TokenExpiredException)
            {
                return Unauthorized();
            }
            catch (SignatureVerificationException)
            {
                return Unauthorized();
            }

            await using var Connection = new NpgsqlConnection(Configuration.GetConnectionString("DatabaseUrl"));

            //Check if user exists
            var User = await Connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM \"user\" WHERE id = @id", new { id = id });
            if (User == null)
            {
                return NotFound();
            }

            //Check roles
            bool pass = false;

            if ((string)Claims["role"] == "a")
            {
                pass = true;
            }
            else if (User.Id == (long)Claims["id"])
            {
                pass = true;
            }

            if (!pass)
            {
                return Unauthorized();
            }

            //Update user details
            int affectedRows = 0;
            try
            {
                affectedRows = await Connection.ExecuteAsync("UPDATE \"user\" SET username = @user, password = @pw, email = @email WHERE id = @id",
                    new
                    {
                        user = user.Username,
                        pw = user.Password,
                        email = user.Email,
                        id = id
                    }
                );
            }
            catch (Exception)
            {
                return Conflict();
            }

            if (affectedRows == 0)
            {
                return BadRequest();
            }

            return NoContent();
        }

        // POST api/users
        //everyone
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            //MAKE ADMIN USER ROLE CREATION AVAILABLE
            /*//Get token from the header
            Microsoft.Extensions.Primitives.StringValues header;
            if (!Request.Headers.TryGetValue("Authorization", out header))
            {
                return Forbid();
            }
            header.ToString();
            string[] strings = header.ToString().Split(' ');

            //Check if format is as expected
            if (strings.Length < 2)
            {
                return Forbid();
            }

            if (strings[0].ToLower() != "bearer")
            {
                return Forbid();
            }

            string token = strings[1];
            IDictionary<string, object> Claims = null;
            //Verify token
            try
            {
                IJsonSerializer serializer = new JsonNetSerializer();
                var provider = new UtcDateTimeProvider();
                IJwtValidator validator = new JwtValidator(serializer, provider);
                IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
                IJwtAlgorithm algorithm = new HMACSHA256Algorithm();
                IJwtDecoder decoder = new JwtDecoder(serializer, validator, urlEncoder, algorithm);

                Claims = decoder.DecodeToObject<IDictionary<string, object>>(token, Configuration["Jwt:SecretKey"], true);
            }
            catch (TokenExpiredException)
            {
                return Unauthorized();
            }
            catch (SignatureVerificationException)
            {
                return Unauthorized();
            }

            //Check roles
            if ((string)Claims["role"] != "a")
            {
                return Unauthorized();
            }*/

            await using var Connection = new NpgsqlConnection(Configuration.GetConnectionString("DatabaseUrl"));
            int userId;
            try
            {
                userId = await Connection.ExecuteScalarAsync<int>("INSERT INTO \"user\" (username, password, email, role) " +
                "VALUES(@username, @password, @email, @role) RETURNING id;",
                new { username = user.Username, password = user.Password, email = user.Email, role = "u" });
            }
            catch (Exception)
            {
                return Conflict();
            }

            var User = await Connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM public.user WHERE id = @id", new { id = userId });

            return CreatedAtAction("GetUser", new { id = User.Id }, User);
        }

        // DELETE api/users/{id}
        //admin
        [HttpDelete("{id}")]
        public async Task<ActionResult<User>> DeleteUser(int id)
        {
            //Get token from the header
            Microsoft.Extensions.Primitives.StringValues header;
            if (!Request.Headers.TryGetValue("Authorization", out header))
            {
                return Forbid();
            }
            header.ToString();
            string[] strings = header.ToString().Split(' ');

            //Check if format is as expected
            if (strings.Length < 2)
            {
                return Forbid();
            }

            if (strings[0].ToLower() != "bearer")
            {
                return Forbid();
            }

            string token = strings[1];
            IDictionary<string, object> Claims = null;
            //Verify token
            try
            {
                IJsonSerializer serializer = new JsonNetSerializer();
                var provider = new UtcDateTimeProvider();
                IJwtValidator validator = new JwtValidator(serializer, provider);
                IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
                IJwtAlgorithm algorithm = new HMACSHA256Algorithm();
                IJwtDecoder decoder = new JwtDecoder(serializer, validator, urlEncoder, algorithm);

                Claims = decoder.DecodeToObject<IDictionary<string, object>>(token, Configuration["Jwt:SecretKey"], true);
            }
            catch (TokenExpiredException)
            {
                return Unauthorized();
            }
            catch (SignatureVerificationException)
            {
                return Unauthorized();
            }

            //Check roles
            if ((string)Claims["role"] != "a")
            {
                return Unauthorized();
            }

            await using var Connection = new NpgsqlConnection(Configuration.GetConnectionString("DatabaseUrl"));

            //Check if user exists
            var User = await Connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM public.user WHERE id = @id", new { id = id });
            if (User == null)
            {
                return NotFound();
            }

            //Check if user has reviews that use it
            var count = await Connection.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM review WHERE \"userId\" = @id", new { id = id });
            if (count != 0)
            {
                return Conflict();
            }

            //Delete user and check if it was deleted
            var deleted = await Connection.ExecuteAsync("DELETE FROM public.user WHERE id = @id", new { id = id });
            if (deleted != 0)
            {
                return User;
            }

            return Conflict();
        }

        // GET api/users/{id}/reviews
        //everyone
        //paging
        [HttpGet("{id}/reviews")]
        public async Task<ActionResult<List<Review>>> GetReviews(int id)
        {
            await using var Connection = new NpgsqlConnection(Configuration.GetConnectionString("DatabaseUrl"));
            var Reviews = await Connection.QueryAsync<Review>("SELECT * FROM review WHERE \"userId\" = @id ORDER BY id", new { id = id });

            if (!Reviews.Any())
            {
                return NotFound();
            }

            return Reviews.ToList();
        }
    }
}
