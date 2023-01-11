using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using BeanScout.Models;
using AutoMapper;
using BeanScout.DataTransferObjects;
using BeanScout.JwtFeatures;
using System.IdentityModel.Tokens.Jwt;
using BeanScout.Services.EmailService;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Web;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System.Net;

namespace BeanScout.Controllers
{
	[Route("api/accounts")]
	[ApiController]
	public class AccountController : ControllerBase
	{
		private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IMapper _mapper;
		private readonly JwtHandler _jwtHandler;
		private readonly EmailSender _emailSender;
		public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IMapper mapper, JwtHandler jwtHandler, EmailSender emailSender)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_mapper = mapper;
			_jwtHandler = jwtHandler;
			_emailSender = emailSender;
		}

		[HttpPost("Registration")]
		public async Task<IActionResult> RegisterUser([FromBody] UserForRegistrationDto userForRegistration)
		{
			if (userForRegistration == null || !ModelState.IsValid)
			{
				return BadRequest();
			}

			var user = _mapper.Map<User>(userForRegistration);

			var result = await _userManager.CreateAsync(user, userForRegistration.Password);
			if (!result.Succeeded)
			{
				var errors = result.Errors.Select(e => e.Description);

				return BadRequest(new RegistrationResponseDto { Errors = errors, IsAuthSuccessful = false });
			}

            var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action(nameof(ConfirmEmail), "Account", new { token = emailToken, email = user.Email }, Request.Scheme);
            var message = new Message(user.Email, "Confirmation email link", confirmationLink);
            await _emailSender.SendEmailAsync(message);

			return Created("/api/account/registration", user);
		}

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] UserForAuthenticationDto userForAuthentication)
        {
            var user = await _userManager.FindByNameAsync(userForAuthentication.Email);
			// No User by the name
            if (user == null)
                return Unauthorized(new AuthResponseDto { IsAuthSuccessful = false, ErrorMessage = "Invalid Authentication" });
            User confirmedUser = (User)user;
            var result = await _signInManager.PasswordSignInAsync(user, userForAuthentication.Password, true, true);
			// Wrong Password
			if(!result.Succeeded)
				return Unauthorized(new AuthResponseDto { IsAuthSuccessful = false, ErrorMessage = "Invalid Authentication" });
			//Locked Out due to bad login attemps
			if(result.IsLockedOut)
			{
				return Unauthorized(new AuthResponseDto { IsAuthSuccessful = false, ErrorMessage = "You've been locked out for too many attempts. Try again in 10 minutes." });
			}
			// Unconfirmed Email addresss
            var emailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
			if (!emailConfirmed)
			{
                var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action(nameof(ConfirmEmail), "Account", new { token = emailToken, email = user.Email }, Request.Scheme);
                var message = new Message(user.Email, "Confirmation email link", confirmationLink);
                await _emailSender.SendEmailAsync(message);
                return Unauthorized(new AuthResponseDto { IsAuthSuccessful = false, ErrorMessage = "Must confirm email address. Check your email for a new verification link." });
			}

			// At this point they're good! Send a token and a success response.
            var signingCredentials = _jwtHandler.GetSigningCredentials();
            var claims = _jwtHandler.GetClaims(user);
            var tokenOptions = _jwtHandler.GenerateTokenOptions(signingCredentials, claims);
            var token = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

            return Ok(new AuthResponseDto { IsAuthSuccessful = true, Token = token, FirstName = confirmedUser.FirstName, LastName = confirmedUser.LastName });
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
			if (user == null)
				return Unauthorized();

            var result = await _userManager.ConfirmEmailAsync(user, token);
            return Ok(result.Succeeded ? nameof(ConfirmEmail) : "Error");
        }

		[HttpPost("ForgotPassword")]
		public async Task<IActionResult> ForgotPassword([FromBody]ForgotPasswordDTO forgotPasswordDto)
		{
			if (!ModelState.IsValid)
				return BadRequest();

			var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);
			if (user == null)
				return BadRequest("Invalid Request");

			var token = await _userManager.GeneratePasswordResetTokenAsync(user);
			var param = new Dictionary<string, string?>
			{
				{"token", token },
				{"email", forgotPasswordDto.Email }
			};

			var callback = QueryHelpers.AddQueryString(forgotPasswordDto.ClientURI, param);
			var message = new Message(user.Email, "Reset password token", callback);

			await _emailSender.SendEmailAsync(message);
			return Ok();
		}

		[HttpPost("ResetPassword")]
		public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO resetPasswordDto)
		{
			if (!ModelState.IsValid)
				return BadRequest();

			var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);

			if (user == null)
				return BadRequest();

			User confirmedUser = (User)user;

			var passwordReset = await _userManager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.Password);
			if(!passwordReset.Succeeded)
			{
				var errors = passwordReset.Errors.Select(e => e.Description);
				return BadRequest(new { Errors = errors });
			}

			return Ok();
		}
		//https://github.com/xamarin/Essentials/blob/develop/Samples/Sample.Server.WebAuthenticator/Controllers/MobileAuthController.cs
		[HttpGet("google-auth")]
		public async void GoogleAuth()
		{
			try
			{
                var auth = await Request.HttpContext.AuthenticateAsync("google");

                if (!auth.Succeeded
                    || auth?.Principal == null
                    || !auth.Principal.Identities.Any(id => id.IsAuthenticated)
                    || string.IsNullOrEmpty(auth.Properties.GetTokenValue("access_token")))
                {
                    // Not authenticated, challenge
                    await Request.HttpContext.ChallengeAsync("google");

                }
                else
                {
                    var claims = auth.Principal.Identities.FirstOrDefault()?.Claims;
                    var email = string.Empty;
                    email = claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;

                    // Get parameters to send back to the callback
                    var qs = new Dictionary<string, string>
                {
                    { "access_token", auth.Properties.GetTokenValue("access_token") },
                    { "refresh_token", auth.Properties.GetTokenValue("refresh_token") ?? string.Empty },
                    { "expires", (auth.Properties.ExpiresUtc?.ToUnixTimeSeconds() ?? -1).ToString() },
                    { "email", email }
                };

                    // Build the result url
                    var url = "authtemplate" + "://google-auth-success" + string.Join(
                        "&",
                        qs.Where(kvp => !string.IsNullOrEmpty(kvp.Value) && kvp.Value != "-1")
                        .Select(kvp => $"{WebUtility.UrlEncode(kvp.Key)}={WebUtility.UrlEncode(kvp.Value)}"));

                    // Redirect to final url
                    Request.HttpContext.Response.Redirect(url);
                }
            }
			catch(Exception e)
			{
				Console.WriteLine(e);		
			}
			
		}
    }
}

