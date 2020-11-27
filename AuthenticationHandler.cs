using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieReview
{
    public class AuthenticationHandler : IAuthenticationHandler
    {
        private HttpContext _context;

        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
        {
            _context = context;
            return Task.CompletedTask;
        }

        public Task<AuthenticateResult> AuthenticateAsync()
            => Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(AuthenticationProperties properties)
        {
            throw new NotImplementedException();
        }

        public Task ForbidAsync(AuthenticationProperties properties)
        {
            _context.Response.StatusCode = 403;
            return Task.FromResult(AuthenticateResult.Fail("Failed to authenticate"));
        }
    }
}
