﻿using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Dapper;
using FYP.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace FYP.Controllers;
public class AccountController : Controller
{
    private const string LOGIN_SQ =
        @"SELECT * FROM users WHERE UserID = @userid AND Password = @user_pw";

    private const string LASTLOGIN_SQ =
        @"UPDATE Users SET LastLogin = GETDATE() WHERE UserID = @UserID";

    private const string ForgetPW_SQ =
        @"SELECT Password FROM Users WHERE UserID = @UserID AND Email = @Email";

    private const string LV = "Login";
    private const string RE_CN = "";
    private const string RE_Index = "";


    private readonly IConfiguration _configuration;

    public AccountController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string GetConnectionString()
    {
        return _configuration.GetConnectionString("DefaultConnection");
    }

    public IActionResult Login()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    public IActionResult Login(UserLogin userLogin)
    {
        if (ModelState.IsValid)
        {
            string uid = userLogin.UserID;
            string pw = userLogin.Password;

            if (!AuthenticateUser(uid, pw, out ClaimsPrincipal principal))
            {
                ViewData["Message"] = "Incorrect User ID or Password";
                ViewData["MsgType"] = "warning";
                return View(LV);
            }
            else
            {
                HttpContext.SignInAsync(principal); // Sign in the user

                userLogin.RedirectToUsers = true; // Set the RedirectToUsers property to true

                return RedirectToAction(RE_Index, RE_CN); // Redirect to the users to hommepage
            }
        }

        return View(userLogin);
    }

    [Authorize]
    public IActionResult Users()
    {
        // Retrieve user data and pass it to the view
        DataTable usersData = DBUtl.GetTable(""); // Query to retrieve user data
        return View(usersData);
    }


    public IActionResult Register()
    {
        return View("Register");
    }
    public IActionResult Register(NewUser usr)
    {
        return View();
    }
    public IActionResult Forbidden()
    {
        return View();
    }

    private static bool AuthenticateUser(string uid, string pw, out ClaimsPrincipal principal)
    {
        principal = null!;

        DataTable ds = DBUtl.GetTable(LOGIN_SQ, uid, pw);
        if (ds.Rows.Count == 1)
        {
            principal =
               new ClaimsPrincipal(
                  new ClaimsIdentity(
                     new Claim[] {
                     new Claim(ClaimTypes.NameIdentifier, uid),
                     }, "Basic"
                  )
               );
            return true;
        }
        return false;

        
    }
}
    




