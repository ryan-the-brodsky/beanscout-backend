using BeanScout.Models;
using BeanScout.Data;
using Microsoft.EntityFrameworkCore;

namespace BeanScout.Services
{
	public class ReviewService
	{
		private readonly ReviewContext _context;

		public ReviewService(ReviewContext context)
		{
			_context = context;
		}
	}
}

