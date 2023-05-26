using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Dapper;
using FYP.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Text;

namespace FYP.Controllers;
public class AccountController : Controller
{
    private const string LOGIN_SQ =
        @"SELECT * FROM users WHERE userid = '{0}' AND user_pw = HASHBYTES('SHA1', '{1}')";
    private const string LOGIN_EMP =
        @"SELECT * FROM employee WHERE employee_id = '{0}' AND employee_pw = HASHBYTES('SHA1', '{1}')";

    private const string LASTLOGIN_SQ =
        @"UPDATE Users SET LastLogin = GETDATE() WHERE UserID = @UserID";

    private const string ForgetPW_SQ =
        @"SELECT Password FROM Users WHERE UserID = @UserID AND Email = @Email";

    private const string RECN = "Ticket";
    private const string REVW = "ViewTicket";
    private const string LV = "Login";

    private readonly IConfiguration _configuration;

    public AccountController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

   

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(UserLogin userLogin)
    {
        if (ModelState.IsValid)
        {
            string uid = userLogin.UserID;
            string pw = userLogin.Password;

            if (AuthenticateUser(uid, pw, out ClaimsPrincipal principal))
            {
                HttpContext.SignInAsync(principal); // Sign in the user

                userLogin.RedirectToUsers = true; // Set the RedirectToUsers property to true

                return RedirectToAction(REVW, RECN); // Redirect to the users to home
            }
            else
            {

                ViewData["Message"] = "Incorrect User ID or Password";
                ViewData["MsgType"] = "warning";
                return View(LV);
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
            try
            {
                using (SqlConnection connection = new SqlConnection(GetConnectionString()))
                {
                    connection.Open();

                    // Check if the user ID already exists in the database
                    SqlCommand checkUserIdCmd = new SqlCommand("SELECT COUNT(*) FROM users WHERE userid = @userid", connection);
                    checkUserIdCmd.Parameters.AddWithValue("@userid", newUser.UserId);
                    int existingUserCount = (int)checkUserIdCmd.ExecuteScalar();

                    if (existingUserCount > 0)
                    {
                        ModelState.AddModelError("UserID", "User ID already exists. Please choose a different User ID.");
                        return View(newUser);
                    }

                    // Insert the new user into the database
                    SqlCommand insertUserCmd = new SqlCommand(
                        "INSERT INTO users (userid, user_pw, username, roles_id, school, email, phone_no) " +
                        "VALUES (@userid, HASHBYTES('SHA1', @userPw), @username, 1, @school, @email, @phoneNo)",
                        connection);
                    insertUserCmd.Parameters.AddWithValue("@userid", newUser.UserId);
                    insertUserCmd.Parameters.AddWithValue("@username", newUser.FullName);
                    insertUserCmd.Parameters.AddWithValue("@school", newUser.School);
                    insertUserCmd.Parameters.AddWithValue("@email", newUser.Email);
                    insertUserCmd.Parameters.AddWithValue("@phoneNo", newUser.PhoneNo);
                    insertUserCmd.ExecuteNonQuery();

                    connection.Close();
                }

                return RedirectToAction("Login"); // Redirect to the login page after successful registration
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while registering the user.");
                // Log the exception or handle it appropriately
            }
        }

        return View(newUser); // Return the view with model errors if registration fails
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
    




