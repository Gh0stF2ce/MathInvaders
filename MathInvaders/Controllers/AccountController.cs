using MathInvaders.Models;
using MathInvaders.Services;
using Microsoft.AspNetCore.Mvc;
using System;

namespace MathInvaders.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserService _userService;

        public AccountController(UserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = _userService.Register(model.Name, model.Email, model.Password);
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("уже существует"))
                {
                    ViewData["ErrorMessage"] = "Пользователь с таким email уже зарегистрирован.";
                }
                else
                {
                    ViewData["ErrorMessage"] = "Произошла ошибка при регистрации: " + ex.Message;
                }
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = _userService.Login(model.Email, model.Password);
                TempData["UserName"] = user.Name;
                TempData["UserEmail"] = user.Email;
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = ex.Message;
                return View(model);
            }
        }
    }

    public class RegisterRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}