using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieReview.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(60)]
        public string Password { internal get; set; }

        [Required]
        [StringLength(100)]
        public string Email { internal get; set; }

        public DateTime CreationDate { get; set; }

        public DateTime? LastLoginDate { get; set; }

        [Required]
        public char Role { get; set; }

        public User()
        {
        }
    }
}
