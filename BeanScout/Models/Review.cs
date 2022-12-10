using System.ComponentModel.DataAnnotations;

namespace BeanScout.Models
{
	public class Review
	{
		public int Id { get; set; }

		public User User { get; set; }

		[Required]
		public int Rating { get; set; }

		[Required]
		public string Roaster { get; set; }

		[Required]
		public string DrinkName { get; set; }

		public string? ReviewText { get; set; }
	}
}

