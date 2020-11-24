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
    }
}
