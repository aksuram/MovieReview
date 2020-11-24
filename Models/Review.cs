using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MovieReview.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public double Rating { get; set; }

        [Required]
        [StringLength(5000)]
        public string Description { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int MovieId { get; set; }
    }
}
