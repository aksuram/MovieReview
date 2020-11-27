using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using JWT;
using JWT.Algorithms;
using JWT.Exceptions;
using JWT.Serializers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MovieReview.Models;
using Npgsql;

namespace MovieReview.Controllers
{
    [Route("api/reviews")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private IConfiguration Configuration { get; set; }

        public ReviewController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // GET: api/reviews
        //everyone
        //paging
        [HttpGet]
        public async Task<ActionResult<List<Review>>> GetReviews()
        {
            await using var Connection = new NpgsqlConnection(Configuration.GetConnectionString("DatabaseUrl"));
            var Reviews = await Connection.QueryAsync<Review>("SELECT * FROM review ORDER BY id");

            if (!Reviews.Any())
            {
                return NotFound();
            }

            return Reviews.ToList();
        }

        // GET api/reviews/{id}
        //everyone
        [HttpGet("{id}")]
        public async Task<ActionResult<Review>> GetReview(int id)
        {
            await using var Connection = new NpgsqlConnection(Configuration.GetConnectionString("DatabaseUrl"));
            var Review = await Connection.QueryFirstOrDefaultAsync<Review>("SELECT * FROM review WHERE id = @id", new { id = id });

            if (Review == null)
            {
                return NotFound();
            }

            return Review;
        }

        // PUT api/reviews/{id}
        //user (admin everything)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReview(int id, Review review)
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

            //Check if review exists
            var Review = await Connection.QueryFirstOrDefaultAsync<Review>("SELECT * FROM review WHERE id = @id", new { id = id });
            if (Review == null)
            {
                return NotFound();
            }

            //Check roles
            bool pass = false;

            if ((string)Claims["role"] == "a")
            {
                pass = true;
            }
            else if (Review.UserId == (long)Claims["id"])
            {
                pass = true;
            }

            if (!pass)
            {
                return Unauthorized();
            }

            //Update review details
            int affectedRows = 0;
            try
            {
                affectedRows = await Connection.ExecuteAsync("UPDATE review SET rating = @rating, description = @desc WHERE id = @id",
                    new
                    {
                        rating = review.Rating,
                        desc = review.Description,
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

        // POST api/reviews
        //user
        [HttpPost]
        public async Task<ActionResult<Review>> PostReview(Review review)
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

            await using var Connection = new NpgsqlConnection(Configuration.GetConnectionString("DatabaseUrl"));

            int reviewId = 0;
            try
            {
                reviewId = await Connection.ExecuteScalarAsync<int>("INSERT INTO review " +
                    "( rating, description, \"userId\", \"movieId\" ) " + 
                    "VALUES ( @rating, @desc, @uid, @mid ) RETURNING id;",
                        new
                        {
                            rating = review.Rating,
                            desc = review.Description,
                            uid = Claims["id"],
                            mid = review.MovieId
                        }
                );
            }
            catch (Exception)
            {
                return Conflict();
            }

            var Review = await Connection.QueryFirstOrDefaultAsync<Review>("SELECT * FROM review WHERE id = @id", new { id = reviewId });

            return CreatedAtAction("GetReview", new { id = Review.Id }, Review);
        }

        // DELETE api/reviews/{id}
        //user (admin everything)
        [HttpDelete("{id}")]
        public async Task<ActionResult<Review>> DeleteReview(int id)
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

            await using var Connection = new NpgsqlConnection(Configuration.GetConnectionString("DatabaseUrl"));

            //Check if review exists
            var Review = await Connection.QueryFirstOrDefaultAsync<Review>("SELECT * FROM review WHERE id = @id", new { id = id });
            if (Review == null)
            {
                return NotFound();
            }

            //Check roles
            bool pass = false;

            if ((string)Claims["role"] == "a")
            {
                pass = true;
            }
            else if (Review.UserId == (long)Claims["id"])
            {
                pass = true;
            }

            if (!pass)
            {
                return Unauthorized();
            }

            //Delete review and check if it was deleted
            var deleted = await Connection.ExecuteAsync("DELETE FROM review WHERE id = @id", new { id = id });
            if (deleted != 0)
            {
                return Review;
            }

            return Conflict();
        }
    }
}
