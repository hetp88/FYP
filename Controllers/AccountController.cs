
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
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ZXing;

namespace FYP.Controllers;
public class AccountController : Controller
{
    private const string LOGIN_SQ =
        @"SELECT *
FROM users
INNER JOIN employee ON users.userid = employee.userid
WHERE users.userid = '{0}' AND users.user_pw = HASHBYTES('SHA1', '{1}')
  AND employee.employee_pw = HASHBYTES('SHA1', '{1}')";

    private const string LOGIN_EMP =
        @"SELECT * FROM employee WHERE employee_id = '{0}' AND employee_pw = HASHBYTES('SHA1', '{1}')";

    private const string LASTLOGIN_SQ =
        @"UPDATE Users SET LastLogin = GETDATE() WHERE UserID = '{0}'";

    private const string ForgetPW_SQ =
        @"SELECT Password FROM Users WHERE UserID = '{0}' AND Email = '{1}'";

    //private const string UROLE = "roles_id";

    //private const string RECN = "Home";
    //private const string REVW = "Index";

    //private const string RECN1 = "Account";
    //private const string REVW1 = "Login";

    //private const string LV = "Login";

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

    [HttpPost]
    public IActionResult Login(UserLogin user)
    {
        if (!AuthenticateUser(user.UserID, user.Password, out ClaimsPrincipal principal))
        {
            ViewData["Message"] = "Incorrect User ID or Password";
            ViewData["MsgType"] = "warning";
            return View("Login");
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

            return RedirectToAction("Index", "Home"); // Redirect to the users to home
        }
    }

    public IActionResult Logout(string returnUrl = null!)
    {
        HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Login", "Account");
    }
    [AllowAnonymous]
    public IActionResult Forbidden()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult Register()
    {
        return View("Register");

    }

    [AllowAnonymous]
    [HttpPost]
    public IActionResult Register(NewUser newUser)
    {
        if (!ModelState.IsValid)
        {
            //Validation Check
            ViewData["MsgType"] = "danger";
            return View("Register");
        }
        else
        {
            // Save the user registration data to the database
            using (var connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                int UserID = int.Parse(ExtractNumbersFrom(newUser.Email));

                int totalDigits = UserID.ToString().Length;

                string insertQuery = "";
                string X = "UserID, newUser.UserPw2, newUser.UserName, newUser.School, newUser.Email, newUser.PhoneNo";

if (totalDigits == 4)
                {
                    insertQuery += @"INSERT INTO users(userid, user_pw, username, roles_id, school, email, phone_no, last_login)" +
                                     "VALUES ('{0}', HASHBYTES('SHA1', '{1}'), '{2}', '1', '{3}', '{4}', '{5}', 'null')";
                }
                else if (totalDigits == 8)
                {
                    insertQuery += @"INSERT INTO users(userid, user_pw, username, roles_id, school, email, phone_no, last_login)" +
                                    "VALUES ('{0}', HASHBYTES('SHA1', '{1}'), '{2}', '2', '{3}', '{4}', '{5}', 'null')";
                }

                if (connection.Execute(insertQuery, X) == 1)
                {
                    ViewData["Message"] = "User registered successfully";
                    ViewData["MsgType"] = "success";
                }
                else
                {
                    ViewData["Message"] = "User register failed";
                    ViewData["MsgType"] = "warning";
                }
            }

            // Redirect the user to the login page after successful registration
            return View("Login");
        }

    }

    private string ExtractNumbersFrom(string email)
    {
        string[] NumArray = email.Split("@");
        string numbers = NumArray[0];
        return numbers;
    }

    [Authorize(Roles = "support engineer, administrator")]
    public IActionResult Users()
    {
        // Retrieve user data and pass it to the view
        DataTable usersData = DBUtl.GetTable("SELECT * FROM users " +
                                                "INNER JOIN role ON role.role_id = users.role_id " +
                                                "WHERE role_type = 'support engineer' OR role_type = 'administrator'"); // Query to retrieve user data
        return View(usersData);
    }

    public IActionResult Policy()
    {
        return View();
    }
    public IActionResult TermsConditions()
    {
        return View();
    }
    public IActionResult ForgetPw()
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
                     new Claim(ClaimTypes.Role, ds.Rows[0]["roles_id"].ToString()!)
                     }, "Basic"
                  )
               );
            return true;
        }
        return false;
    }
}