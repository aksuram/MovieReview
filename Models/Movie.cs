using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieReview.Models
{
    public class Movie
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(5000)]
        public string Description { get; set; }

        public double? Rating { get; set; }

        [StringLength(10)]
        public string? AgeRating { get; set; }

        public int? Length { get; set; }

        public DateTime? ReleaseDate { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public Movie()
        {
        }
    }
}
