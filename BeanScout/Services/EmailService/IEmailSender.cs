using System;
namespace BeanScout.Services.EmailService
{
	public interface IEmailSender
	{
		void SendEmail(Message message);
		Task SendEmailAsync(Message message);
	}
}

