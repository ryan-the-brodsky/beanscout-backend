﻿using System;
using Duende.IdentityServer.Models;
using System.ComponentModel.DataAnnotations;

namespace BeanScout.DataTransferObjects
{
    public class ResetPasswordDTO
    {
        [Required(ErrorMessage = "Password is required")]
        public string? Password { get; set; }

        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }

        public string? Email { get; set; }
        public string? Token { get; set; }
    }
}

