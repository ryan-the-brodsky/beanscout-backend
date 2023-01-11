using System.ComponentModel.DataAnnotations;
using Duende.IdentityServer.Models;
namespace BeanScout.DataTransferObjects
{
	public class ForgotPasswordDTO
	{
		[Required]
		[EmailAddress]
		public string? Email { get; set; }

		[Required]
		public string? ClientURI { get; set; }
	}
}

