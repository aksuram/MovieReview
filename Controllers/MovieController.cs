using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
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
    [Route("api/movies")]
    [ApiController]
    public class MovieController : ControllerBase
    {
        private IConfiguration Configuration { get; set; }

        public MovieController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // GET: api/movies
        //everyone
        //paging
        [HttpGet]
        public async Task<ActionResult<List<Movie>>> GetMovies()
        {
            await using var Connection = new NpgsqlConnection(Configuration.GetConnectionString("DatabaseUrl"));
            var Movies = await Connection.QueryAsync<Movie>("SELECT * FROM movie ORDER BY id");

            if (!Movies.Any())
            {
                return NotFound();
            }

            return Movies.ToList();
        }

        // GET api/movies/{id}
        //everyone
        [HttpGet("{id}")]
        public async Task<ActionResult<Movie>> GetMovie(int id)
        {
            await using var Connection = new NpgsqlConnection(Configuration.GetConnectionString("DatabaseUrl"));
            var Movie = await Connection.QueryFirstOrDefaultAsync<Movie>("SELECT * FROM movie WHERE id = @id", new { id = id });

            if (Movie == null)
            {
                return NotFound();
            }

            return Movie;
        }

        // PUT api/movies/{id}
        //admin
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMovie(int id, Movie movie)
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

            int affectedRows = 0;
            try
            {
                affectedRows = await Connection.ExecuteAsync("UPDATE movie SET title = @title, description = @desc, rating = @rating, " + 
                    "\"ageRating\" = @age, \"releaseDate\" = @release, \"categoryId\" = @cat, length = @len WHERE id = @id",
                    new
                    {
                        title = movie.Title,
                        desc = movie.Description,
                        rating = movie.Rating,
                        age = movie.AgeRating,
                        release = movie.ReleaseDate,
                        cat = movie.CategoryId,
                        len = movie.Length,
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

        // POST api/movies
        //admin
        [HttpPost]
        public async Task<ActionResult<Movie>> PostMovie(Movie movie)
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

            int movieId = 0;
            try
            {
                movieId = await Connection.ExecuteScalarAsync<int>("INSERT INTO movie ( title, description, rating, \"ageRating\", \"releaseDate\", " +
                "\"categoryId\", length ) VALUES ( @title, @desc, @rating, @age, @release, @cat, @len ) RETURNING id;",
                    new
                    {
                        title = movie.Title,
                        desc = movie.Description,
                        rating = movie.Rating,
                        age = movie.AgeRating,
                        release = movie.ReleaseDate,
                        cat = movie.CategoryId,
                        len = movie.Length
                    }
                );
            }
            catch (Exception)
            {
                return Conflict();
            }

            var Movie = await Connection.QueryFirstOrDefaultAsync<Movie>("SELECT * FROM movie WHERE id = @id", new { id = movieId });

            return CreatedAtAction("GetMovie", new { id = Movie.Id }, Movie);
        }

        // DELETE api/movies/{id}
        //admin
        [HttpDelete("{id}")]
        public async Task<ActionResult<Movie>> DeleteMovie(int id)
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

            //Check if movie exists
            var Movie = await Connection.QueryFirstOrDefaultAsync<Movie>("SELECT * FROM movie WHERE id = @id", new { id = id });
            if (Movie == null)
            {
                return NotFound();
            }

            //Check if movie has reviews that use it
            var count = await Connection.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM review WHERE \"movieId\" = @id", new { id = id });
            if (count != 0)
            {
                return Conflict();
            }

            //Delete movie and check if it was deleted
            var deleted = await Connection.ExecuteAsync("DELETE FROM movie WHERE id = @id", new { id = id });
            if (deleted != 0)
            {
                return Movie;
            }

            return Conflict();
        }

        // GET api/movies/{id}/reviews
        //everyone
        //paging
        [HttpGet("{id}/reviews")]
        public async Task<ActionResult<List<Review>>> GetReviews(int id)
        {
            await using var Connection = new NpgsqlConnection(Configuration.GetConnectionString("DatabaseUrl"));
            var Reviews = await Connection.QueryAsync<Review>("SELECT * FROM review WHERE \"movieId\" = @id ORDER BY id", new { id = id });

            if (!Reviews.Any())
            {
                return NotFound();
            }

            return Reviews.ToList();
        }
    }
}
