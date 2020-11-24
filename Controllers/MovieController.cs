using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
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
    }
}
