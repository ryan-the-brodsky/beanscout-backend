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

        [JsonIgnore]
        public override bool EmailConfirmed { get; set; }

        [JsonIgnore]
        public override bool TwoFactorEnabled { get; set; }

        [JsonIgnore]
        public override string PhoneNumber { get; set; }

        [JsonIgnore]
        public override bool PhoneNumberConfirmed { get; set; }

        [JsonIgnore]
        public override string SecurityStamp { get; set; }

        [JsonIgnore]
        public override bool LockoutEnabled { get; set; }

        [JsonIgnore]
        public override DateTimeOffset? LockoutEnd { get; set; }

        [JsonIgnore]
        public override int AccessFailedCount { get; set; }

        [JsonIgnore]
        public override string ConcurrencyStamp { get; set; }

    }
}

