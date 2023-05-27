using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Dapper;
using FYP.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace FYP.Controllers;
public class AccountController : Controller
{
    private const string LOGIN_SQ =
        @"SELECT * FROM users WHERE userid = '{0}' AND user_pw = HASHBYTES('SHA1', '{1}')";
    private const string LOGIN_EMP =
        @"SELECT * FROM employee WHERE employee_id = '{0}' AND employee_pw = HASHBYTES('SHA1', '{1}')";

    private const string LASTLOGIN_SQ =
        @"UPDATE Users SET LastLogin = GETDATE() WHERE UserID = '{0}'";

    private const string ForgetPW_SQ =
        @"SELECT Password FROM Users WHERE UserID = '{0}' AND Email = '{1}'";

    private const string RECN = "Home";
    private const string REVW = "Index";
    private const string LV = "Login";

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
    public IActionResult Policy()
    {
        return View();
    }
    public IActionResult TermsConditions()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(UserLogin user)
    {
        if (!AuthenticateUser(user.UserID, user.Password, out ClaimsPrincipal principal))
        {
            ViewData["Message"] = "Incorrect User ID or Password";
            ViewData["MsgType"] = "warning";
            return View(LV);
        }
        else
        {
            // Sign in the user
            HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = user.RememberMe
                });

            DBUtl.ExecSQL(LASTLOGIN_SQ, user.UserID, user.Password); //update the last login timestamp of the user

            user.RedirectToUsers = true; // Set the RedirectToUsers property to true

            return RedirectToAction(REVW, RECN); // Redirect to the users to home
        }
    }

    [Authorize]
    public IActionResult Logout(string returnUrl = null!)
    {
        HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Login");
    }


    [Authorize]
    public IActionResult Users()
    {
        // Retrieve user data and pass it to the view
        DataTable usersData = DBUtl.GetTable(""); // Query to retrieve user data
        return View(usersData);
    }
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Users");
        }
        else
        {
            return View();
        }
    }

    [HttpPost]
    public IActionResult Register(NewUser newUser)
    {
        if (ModelState.IsValid)
        {
            // Save the user registration data to the database
            using (var connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                string insertQuery = "INSERT INTO Users (UserID, UserPw, FullName, School, Email, PhoneNo) " +
                                     "VALUES (@UserId, HASHBYTES('SHA1', @UserPw), @FullName, @School, @Email, @PhoneNo)";

                connection.Execute(insertQuery, newUser);
            }

            // Redirect the user to the login page after successful registration
            return RedirectToAction("Login");
        }

        // If there are validation errors, return the registration view with the model
        return View(newUser);
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
    




