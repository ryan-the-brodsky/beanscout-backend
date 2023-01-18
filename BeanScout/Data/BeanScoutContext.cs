using Microsoft.EntityFrameworkCore;
using BeanScout.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace BeanScout.Data
{
	public class BeanScoutContext : IdentityDbContext<IdentityUser>
	{
		private readonly IConfiguration _config;
		public BeanScoutContext(DbContextOptions<BeanScoutContext> options, IConfiguration config)
			:base(options)
		{
			_config = config;
		}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseNpgsql(_config["ConnectionString"]);

        public DbSet<Review> Reviews => Set<Review>();
	}

}

