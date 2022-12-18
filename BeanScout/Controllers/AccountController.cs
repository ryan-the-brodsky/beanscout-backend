﻿using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using BeanScout.Models;
using AutoMapper;
using BeanScout.DataTransferObjects;
using BeanScout.JwtFeatures;
using System.IdentityModel.Tokens.Jwt;
using BeanScout.Services.EmailService;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace BeanScout.Controllers
{
	[Route("api/accounts")]
	[ApiController]
	public class AccountController : ControllerBase
	{
		private readonly UserManager<IdentityUser> _userManager;
		private readonly IMapper _mapper;
		private readonly JwtHandler _jwtHandler;
		private readonly EmailSender _emailSender;
		public AccountController(UserManager<IdentityUser> userManager, IMapper mapper, JwtHandler jwtHandler, EmailSender emailSender)
		{
			_userManager = userManager;
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
            var message = new Message(user.Email, "Confirmation email link", $"Please confirm your account by clicking this link: {confirmationLink}");
            await _emailSender.SendEmailAsync(message);

			return Created("/api/account/registration", user);
		}

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] UserForAuthenticationDto userForAuthentication)
        {
            var user = await _userManager.FindByNameAsync(userForAuthentication.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, userForAuthentication.Password))
                return Unauthorized(new AuthResponseDto { ErrorMessage = "Invalid Authentication" });


            User confirmedUser = (User)user;

            var emailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
			if (!emailConfirmed)
			{
				// add logic sending back an error message about non-verified email
				Console.WriteLine("Unconfirmed email!");
			}

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

