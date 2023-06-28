
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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System;
using System.Data;
using Microsoft.AspNetCore.Http;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
using System.Security.Principal;

namespace FYP.Controllers;
public class AccountController : Controller
{
    private const string LOGIN_SQ =
        @"SELECT * FROM users WHERE userid = '{0}' AND user_pw = HASHBYTES('SHA1', '{1}')";
    private const string LOGIN_EMP =
        @"SELECT * FROM employee WHERE employee_id = '{0}' AND employee_pw = HASHBYTES('SHA1', '{1}')";

    private const string LASTLOGIN_SQ =
        @"UPDATE users SET last_login=GETDATE() WHERE userid = @UserID";

    private const string ForgetPW_SQ =
        @"SELECT password FROM Users WHERE userid = '{0}' AND email = '{1}'";

    //private const string UROLE = "roles_id";

    //private const string RECN = "Home";
    //private const string REVW = "Index";

    //private const string RECN1 = "Account";
    //private const string REVW1 = "Login";

    //private const string LV = "Login";

    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor contextAccessor;

    public AccountController(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        contextAccessor = httpContextAccessor;
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
        if (!AuthenticateUser(user.UserID.ToString(), user.Password, out ClaimsPrincipal principal))
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

            using (var connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                UserLogin u = new UserLogin
                {
                    UserID = user.UserID,
                };

                connection.Execute(LASTLOGIN_SQ, u);
            }
                //DBUtl.ExecSQL(LASTLOGIN_SQ, user.UserID, user.Password); //update the last login timestamp of the user

            user.RedirectToUsers = true; // Set the RedirectToUsers property to true

            contextAccessor.HttpContext.Session.SetInt32("userID", user.UserID);

            return RedirectToAction("Index", "Home"); // Redirect to the users to home
        }
    }

    public IActionResult Logout(string returnUrl = null!)
    {
        HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (Url.IsLocalUrl(returnUrl))
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
            string[] NumArray = newUser.Email.Split("@");
            string numbers = NumArray[0];
            int UserID = int.Parse(numbers);
            int totalDigits = UserID.ToString().Length;

            using (var connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                NewUser staff = new NewUser
                {
                    UserID = UserID,
                    UserPw2 = newUser.UserPw2,
                    UserName = newUser.UserName,
                    Role = 2,
                    School = newUser.School,
                    Email = newUser.Email,
                    PhoneNo = newUser.PhoneNo,
                    Last_login = null,
                };

                NewUser student = new NewUser
                {
                    UserID = UserID,
                    UserPw2 = newUser.UserPw2,
                    UserName = newUser.UserName,
                    Role = 1,
                    School = newUser.School,
                    Email = newUser.Email,
                    PhoneNo = newUser.PhoneNo,
                    Last_login = null,
                };

                if (totalDigits == 8)
                {

                    string insertQuery1 = @"INSERT INTO users(userid, user_pw, username, roles_id, school, email, phone_no, last_login)
                                            VALUES (@UserID, HASHBYTES('SHA1', @UserPw2), @UserName, @Role, @School, @Email, @PhoneNo, @Last_login)";

                    if (connection.Execute(insertQuery1, student) == 1)
                    {
                        TempData["Message"] = "Account registered successfully";
                        TempData["MsgType"] = "success";
                    }
                    else
                    {
                        TempData["Message"] = "Account registered failed";
                        TempData["MsgType"] = "danger";
                    }
                }

                else if (totalDigits == 4)
                {
                    string insertQuery2 = @"INSERT INTO users(userid, user_pw, username, roles_id, school, email, phone_no, last_login)
                                            VALUES (@UserID, HASHBYTES('SHA1', @UserPw2), @UserName, @Role, @School, @Email, @PhoneNo, @Last_login)";

                    if (connection.Execute(insertQuery2, staff) == 1)
                    {
                        TempData["Message"] = "Account registered successfully";
                        TempData["MsgType"] = "success";
                    }
                    else
                    {
                        TempData["Message"] = "Account registered failed";
                        TempData["MsgType"] = "danger";
                    }
                }
            }
        }
        // Redirect the user to the login page after successful registration
        return RedirectToAction("Login", "Account");
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
    public IActionResult Profile()
    {
        int? currentuser = contextAccessor.HttpContext.Session.GetInt32("userID");
        using (SqlConnection connection = new SqlConnection(GetConnectionString()))
        {
            string query = $"SELECT userid, email, phone_no, username, school FROM users WHERE userid='{currentuser}'";

            connection.Open();
            List<Users> u = connection.Query<Users>(query).AsList();
            //Console.WriteLine(currentuser);
            return View(u);
        }
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