using Microsoft.EntityFrameworkCore;
using BeanScout.Models;
namespace BeanScout.Data
{
	public class ReviewContext : DbContext
	{
		private readonly IConfiguration _config;
		public ReviewContext(DbContextOptions<ReviewContext> options, IConfiguration config)
			:base(options)
		{
			_config = config;
		}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseNpgsql(_config["BeanScout:ConnectionString"]);

        public DbSet<Review> Reviews => Set<Review>();
	}

}

