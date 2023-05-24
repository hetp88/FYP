using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;
using System;
using FYP.Models;

namespace FYP.Controllers;
public class AccountController : Controller
{
    private const string LOGIN_SQ =
        @"";
    private const string LASTLOGIN_SQ =
        @"";
    private const string ForgetPW_SQ =
        @"";
    public IActionResult Login()
    {
        return View();
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



