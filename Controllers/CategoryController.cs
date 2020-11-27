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
    [Route("api/categories")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private IConfiguration Configuration { get; set; }

        public CategoryController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // GET: api/categories
        //everyone
        //paging
        [HttpGet]
        public async Task<ActionResult<List<Category>>> GetCategories()
        {
            await using var Connection = new NpgsqlConnection(Configuration.GetConnectionString("DatabaseUrl"));
            var Categories = await Connection.QueryAsync<Category>("SELECT * FROM category ORDER BY id");

            if (!Categories.Any())
            {
                return NotFound();
            }

            return Categories.ToList();
        }

        // GET api/categories/{id}
        //everyone
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            await using var Connection = new NpgsqlConnection(Configuration.GetConnectionString("DatabaseUrl"));
            var Category = await Connection.QueryFirstOrDefaultAsync<Category>("SELECT * FROM category WHERE id = @id", new { id = id });

            if (Category == null)
            {
                return NotFound();
            }

            return Category;
        }

        // PUT api/categories/{id}
        //admin
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCategory(int id, Category category)
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
            var affectedRows = await Connection.ExecuteAsync("UPDATE category SET name = @name WHERE id = @id", new { name = category.Name, id = id });

            if (affectedRows == 0)
            {
                return BadRequest();
            }

            return NoContent();
        }

        // POST api/categories/
        //admin
        [HttpPost]
        public async Task<ActionResult<Category>> PostCategory(Category category)
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
            category.Id = await Connection.ExecuteScalarAsync<int>("INSERT INTO category (name) VALUES (@name) RETURNING id;", new { name = category.Name });

            return CreatedAtAction("GetCategory", new { id = category.Id }, category);
        }

        // DELETE api/categories/{id}
        //admin
        [HttpDelete("{id}")]
        public async Task<ActionResult<Category>> DeleteCategory(int id)
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

            //Check if category exists
            var Category = await Connection.QueryFirstOrDefaultAsync<Category>("SELECT * FROM category WHERE id = @id", new { id = id });
            if (Category == null)
            {
                return NotFound();
            }

            //Check if category has movies that use it
            var count = await Connection.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM movie WHERE \"categoryId\" = @id", new { id = id });
            if (count != 0)
            {
                return Conflict();
            }

            //Delete category and check if it was deleted
            var deleted = await Connection.ExecuteAsync("DELETE FROM category WHERE id = @id", new { id = id });
            if (deleted != 0)
            {
                return Category;
            }

            return Conflict();
        }

        // GET api/categories/{id}/movies
        //everyone
        //paging
        [HttpGet("{id}/movies")]
        public async Task<ActionResult<List<Movie>>> GetMovies(int id)
        {
            await using var Connection = new NpgsqlConnection(Configuration.GetConnectionString("DatabaseUrl"));
            var Movies = await Connection.QueryAsync<Movie>("SELECT * FROM movie WHERE \"categoryId\" = @id ORDER BY id", new { id = id });

            if (!Movies.Any())
            {
                return NotFound();
            }

            return Movies.ToList();
        }
    }
}
