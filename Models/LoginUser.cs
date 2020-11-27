using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MovieReview.Models
{
    public class LoginUser
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(60)]
        public string Password { get; set; }

        public LoginUser()
        {
        }
    }
}
