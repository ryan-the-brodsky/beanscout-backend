using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using BeanScout.Models;
using AutoMapper;
using BeanScout.DataTransferObjects;

namespace BeanScout.Controllers
{
	[Route("api/accounts")]
	[ApiController]
	public class AccountController : ControllerBase
	{
		private readonly UserManager<IdentityUser> _userManager;
		private readonly IMapper _mapper;
		public AccountController(UserManager<IdentityUser> userManager, IMapper mapper)
		{
			_userManager = userManager;
			_mapper = mapper;
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

				return BadRequest(new RegistrationResponseDto { Errors = errors });
			}

			return StatusCode(201);
		}
	}
}

