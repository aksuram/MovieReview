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

        /*
        // PUT api/categories/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCategory(int id, Category category)
        {
            //TODO: check data validity
            /*if (id != category.Id)
            {
                return BadRequest();
            }*//*
            category.Id = id;

            _context.Entry(category).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }*/

        // POST api/categories/
        [HttpPost]
        public async Task<ActionResult<Category>> PostCategory(Category category)
        {
            await using var Connection = new NpgsqlConnection(Configuration.GetConnectionString("DatabaseUrl"));
            category.Id = await Connection.ExecuteScalarAsync<int>("INSERT INTO category (name) VALUES (@name) RETURNING id;", new { name = category.Name });

            return CreatedAtAction("GetCategory", new { id = category.Id }, category);
        }

        // DELETE api/categories/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult<Category>> DeleteCategory(int id)
        {
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
