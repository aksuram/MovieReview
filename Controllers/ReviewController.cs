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
    }
}
