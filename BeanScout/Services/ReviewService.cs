using BeanScout.Models;
using BeanScout.Data;
using Microsoft.EntityFrameworkCore;

namespace BeanScout.Services
{
	public class ReviewService
	{
		private readonly BeanScoutContext _context;

		public ReviewService(BeanScoutContext context)
		{
			_context = context;
		}
	}
}

