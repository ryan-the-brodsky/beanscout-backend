using System;
using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;

namespace BeanScout.Models
{
	public class User : IdentityUser
	{
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

		[JsonIgnore]
		public List<Review> Reviews { get; set; } = new();

	}
}

