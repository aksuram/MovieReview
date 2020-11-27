using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MovieReview.Models
{
    public class JwtToken
    {
        [Required]
        public string Token { get; set; }

        public JwtToken()
        {
        }

        public JwtToken(string token)
        {
            Token = token;
        }
    }
}
