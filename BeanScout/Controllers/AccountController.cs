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
    }
}

