
using Microsoft.Data.SqlClient;
using Dapper;
using FYP.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Data;
using XAct;
using System.Security.Cryptography;
using XAct.Messages;


namespace FYP.Controllers;
public class AccountController : Controller
{
    private const string LOGIN_SQ =
        @"SELECT u.userid, u.user_pw, r.roles_type FROM users u 
            INNER JOIN roles r ON r.roles_id = u.roles_id
            WHERE u.userid = '{0}' AND u.user_pw = HASHBYTES('SHA1', '{1}')";
    private const string LOGIN_EMP =
        @"SELECT e.employee_id, e.employee_pw, r.roles_type FROM employee e 
            INNER JOIN roles r ON r.roles_id = e.roles_id
            WHERE e.employee_id = '{0}' AND e.employee_pw = HASHBYTES('SHA1', '{1}')";

    private const string LASTLOGIN_SQ =
        @"UPDATE users SET last_login=@Last_login WHERE userid = @UserID";

    private const string LASTLOGIN_EMP =
        @"UPDATE employee SET last_login=@Last_login WHERE employee_id = @UserID";

    private const string ForgetPW_SQ =
        @"SELECT password FROM Users WHERE userid = @UserID AND email = @Email";



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

    [AllowAnonymous]
    public IActionResult Login()
    {
        return View();
    }
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View();

    }

    [AllowAnonymous]
    [HttpPost]
    public IActionResult Login(UserLogin account)
    {
        if (!AuthenticateUser(account.UserID.ToString() , account.Password, out ClaimsPrincipal principal))
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
                    IsPersistent = account.RememberMe
                });

            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                UserLogin u = new UserLogin
                {
                    Last_login = DateTime.Now,
                    UserID = account.UserID, 
                };


                if (account.UserID.ToString().Length == 4 || account.UserID.ToString().Length == 8)
                {
                    connection.Execute(LASTLOGIN_SQ, u); //update the last login timestamp of the user
                }
                else if (account.UserID.ToString().Length == 3 || account.UserID.ToString().Length == 5 || account.UserID.ToString().Length == 6)
                {
                    connection.Execute(LASTLOGIN_EMP, u); //update the last login timestamp of the user
                }
            }

            account.RedirectToUsers = true; // Set the RedirectToUsers property to true

            contextAccessor.HttpContext.Session.SetInt32("userID", account.UserID);

            return RedirectToAction("Index", "Home"); // Redirect to the users to home
        }
    }

    [Authorize]
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

            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
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

    public IActionResult Users(string searchUserID, string searchRole, string searchName, string searchSchool, string searchEmail, string searchPhone, string searchLastLogin)
    {
        if (User.IsInRole("helpdesk agent") || User.IsInRole("support engineer") || User.IsInRole("administrator"))
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                string query = @"SELECT u.userid, u.username, r.roles_type AS Role, u.school, u.email, u.phone_no AS phoneNo, u.last_login AS Last_login
                       FROM users u
                       INNER JOIN roles r ON r.roles_id = u.roles_id";

                StringBuilder whereClause = new StringBuilder();

                // Build the WHERE clause based on the provided search values
                if (!string.IsNullOrEmpty(searchUserID))
                {
                    whereClause.Append("u.userid = @SearchUserID AND ");
                }
                if (!string.IsNullOrEmpty(searchRole))
                {
                    whereClause.Append("r.roles_type = @SearchRole AND ");
                }
                if (!string.IsNullOrEmpty(searchName))
                {
                    whereClause.Append("u.username = @SearchName AND ");
                }
                if (!string.IsNullOrEmpty(searchSchool))
                {
                    whereClause.Append("u.school = @SearchSchool AND ");
                }
                if (!string.IsNullOrEmpty(searchEmail))
                {
                    whereClause.Append("u.email = @SearchEmail AND ");
                }
                if (!string.IsNullOrEmpty(searchPhone))
                {
                    whereClause.Append("u.phone_no = @SearchPhone AND ");
                }
                if (!string.IsNullOrEmpty(searchLastLogin))
                {
                    whereClause.Append("u.last_login = @SearchLastLogin AND ");
                }

                // Remove the trailing "AND" from the WHERE clause
                if (whereClause.Length > 0)
                {
                    whereClause.Remove(whereClause.Length - 5, 5); // Remove the last " AND "
                    query += " WHERE " + whereClause.ToString();
                }

                connection.Open();

                List<Users> users = connection.Query<Users>(query, new
                {
                    SearchUserID = searchUserID,
                    SearchRole = searchRole,
                    SearchName = searchName,
                    SearchSchool = searchSchool,
                    SearchEmail = searchEmail,
                    SearchPhone = searchPhone,
                    SearchLastLogin = searchLastLogin
                }).ToList();

                return View(users);
            }
        }
        else
        {
            // Unauthorized actions for other roles
            return View("Forbidden");
        }
    }

    [AllowAnonymous]
    public IActionResult Policy()
    {
        return View();
    }
    [AllowAnonymous]
    public IActionResult TermsConditions()
    {
        return View();
    }

    public IActionResult Profile()
    {
        if (User.IsInRole("student") || User.IsInRole("staff"))
        {
            int? currentuser = contextAccessor.HttpContext.Session.GetInt32("userID");
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                string query = $"SELECT userid, email, phone_no AS phoneNo, username, school FROM users WHERE userid='{currentuser}'";
                connection.Open();
                List<Users> u = connection.Query<Users>(query).AsList();
                //Console.WriteLine(currentuser);
                return View(u);
            }
        }
        else
        {
            // Unauthorized actions for other roles
            return View("Forbidden");
        }
    }
    public IActionResult EmpProfile()
    {
        if (User.IsInRole("helpdesk agent") || User.IsInRole("support engineer") || User.IsInRole("administrator"))
        {
            int? currentuser = contextAccessor.HttpContext.Session.GetInt32("userID");
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                string query2 = $"SELECT employee_id AS EmployeeId, email, phone_no AS Phone_no, name FROM employee WHERE employee_id='{currentuser}'";

                connection.Open();
                List<Employee> f = connection.Query<Employee>(query2).AsList();
                //Console.WriteLine(currentuser);
                return View(f);
            }
        }
        else
        {
            // Unauthorized actions for other roles
            return View("Forbidden");
        }
    }
    public IActionResult EditProfile2()
    {
        return View();
    }
    [HttpGet]
    public IActionResult UpdatePassword()
    {
        return View();
    }

    [HttpPost]
    public IActionResult EditProfile2(ChangePasswordViewModel eu)
    {
        int? currentuser = contextAccessor.HttpContext.Session.GetInt32("userID");
        using (SqlConnection connection = new SqlConnection(GetConnectionString()))
        {
            connection.Open();
            ChangePasswordViewModel eut = new ChangePasswordViewModel()
            {
                PhoneNumber= eu.PhoneNumber,
            };
            string updatephoneE = $"UPDATE employee SET phone_no = @PhoneNumber WHERE employee_id = '{currentuser}'";
            string updatephone = $"UPDATE users SET phone_no = @PhoneNumber WHERE userid = '{currentuser}'";

            if (connection.Execute(updatephoneE, eut) == 1)
            {
                ViewData["Message"] = "Updated successfully.";
                return RedirectToAction("EmpProfile");
            }
            else if (connection.Execute(updatephone, eut) == 1)
            {
                ViewData["Message"] = "Updated successfully.";
                return RedirectToAction("Profile");
            }
            else
            {
                ViewData["Message"] = "Unsuccessful update. Do try again.";
                return RedirectToAction("EditProfile2");
            }
        }
    }

    [HttpPost]
    public IActionResult UpdatePassword(NewEmployee np)
    {
        int? currentuser = contextAccessor.HttpContext.Session.GetInt32("userID");
        using (SqlConnection connection = new SqlConnection(GetConnectionString()))
        {
            connection.Open();
            NewEmployee pl = new NewEmployee()
            {
                EmpPw = np.EmpPw, // Assuming np.EmpPw is the plain text password
                Employee_id = (int)currentuser,
            };

            // Convert the plain text password to varbinary using SHA1 hashing
            byte[] passwordBytes = Encoding.UTF8.GetBytes(np.EmpPw);
            byte[] hashedPasswordBytes = SHA1.Create().ComputeHash(passwordBytes);
            string hashedPassword = "0x" + BitConverter.ToString(hashedPasswordBytes).Replace("-", "");

            string updatepwE = $"UPDATE employee SET employee_pw = {hashedPassword} WHERE employee_id = '{currentuser}'";
            string updatepwU = $"UPDATE users SET user_pw = {hashedPassword} WHERE userid = '{currentuser}'";

            if (connection.Execute(updatepwE, pl) == 1)
            {
                Console.WriteLine(np.EmpPw);
                ViewData["Message"] = "Updated successfully.";
                return RedirectToAction("EmpProfile");
            }
            else if (connection.Execute(updatepwU, pl) == 1)
            {
                Console.WriteLine(np.EmpPw);
                ViewData["Message"] = "Updated successfully.";
                return RedirectToAction("Profile");
            }
            else
            {
                ViewData["Message"] = "Unsuccessful update. Please try again.";
                return RedirectToAction("EditProfile2");
            }
        }
    }
    //========================================================================================//

    [HttpGet]
    public IActionResult ForgetPw()
    {
        return View();
    }

    [HttpPost]
    public IActionResult ForgetPw(ForgetPw npw)
    {
        using (SqlConnection connection = new SqlConnection(GetConnectionString()))
        {
            connection.Open();
            ForgetPw pwu = new ForgetPw()
            {
                Email = npw.Email,
            };

            Random random = new Random();
            int code = random.Next(100000, 999999);

            string ce = $"SELECT email FROM users WHERE email = '{pwu.Email}'";
            using (SqlCommand command = new SqlCommand(ce, connection))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string user = pwu.Email;
                        string subject = "Verification Code";
                        string message = $"Your verification code is: {code}";
                        string error;

                        if (EmailUtl.SendEmail(user, subject, message, out error))
                        {
                            // Store verification code in session
                            HttpContext.Session.SetInt32("VerificationCode", code);
                            HttpContext.Session.SetString("UserEmail", user); // Store user email in session
                            return RedirectToAction("VerifyCode", new { email = user });
                        }
                        else
                        {
                            TempData["Message"] = "Email not sent";
                            TempData["MsgType"] = "danger";
                            return RedirectToAction("ForgetPw");
                        }
                    }
                    else
                    {
                        TempData["Message"] = "Email Not Found";
                        TempData["MsgType"] = "danger";
                        return RedirectToAction("ForgetPw");
                    }
                }
            }
        }
    }

    [HttpGet]
    public IActionResult VerifyCode(string email)
    {
        ViewBag.Email = email;
        return View();
    }

    [HttpPost]
    public IActionResult VerifyCode(int code, string email)
    {
        int? storedCode = HttpContext.Session.GetInt32("VerificationCode");
        string userEmail = HttpContext.Session.GetString("UserEmail"); // Retrieve user email from session

        if (storedCode.HasValue && code == storedCode && userEmail == email)
        {
            // Code is correct, store the user email in TempData and redirect to the change password action
            HttpContext.Session.SetString("UserEmail", userEmail);
            return RedirectToAction("ChangePassword", new { email = userEmail });
        }
        else
        {
            TempData["Message"] = "Verification Incorrect";
            TempData["MsgType"] = "danger";
            return RedirectToAction("VerifyCode", new { email = email });
            // Code is incorrect, handle the error
        }
    }

    [HttpGet]
    public IActionResult ChangePassword(string email)
    {
        ViewBag.Email = email;
        return View();
    }

    [HttpPost]
    public IActionResult ChangePassword(NewEmployee vm, string email)
    {
        string userEmail = HttpContext.Session.GetString("UserEmail"); // Retrieve user email from session

        using (SqlConnection connection = new SqlConnection(GetConnectionString()))
        {
            connection.Open();
            NewEmployee pl = new NewEmployee()
            {
                EmpPw = vm.EmpPw,
                Email = (string)userEmail,
            };
            // Convert the plain text password to varbinary using SHA1 hashing
            byte[] passwordBytes = Encoding.UTF8.GetBytes(vm.EmpPw);
            byte[] hashedPasswordBytes = SHA1.Create().ComputeHash(passwordBytes);
            string hashedPassword1 = "0x" + BitConverter.ToString(hashedPasswordBytes).Replace("-", "");
            
            string query = $"UPDATE users SET user_pw = {hashedPassword1} WHERE email = '{pl.Email}';";
            string updatepwE = $"UPDATE employee SET employee_pw = {hashedPassword1} WHERE employee_id = 400001";

            //Send Email to inform user on Password update
            string template = "Dear user, " +
                              "<br>" +
                              "<br>Your Password Have been reset successfully" +
                              "<br>" +
                              "<br>" +
                              "<br>Thank you, " +
                              "<br>IT Helper";
            string title = "Password reset Successfully";
            string message = String.Format(template);
            if (connection.Execute(query, pl) == 1)
            {
                Console.WriteLine(vm.EmpPw);
                ViewData["Message"] = "Updated successfully.";
                if (EmailUtl.SendEmail(pl.Email, title, message, out string result))
                {
                    ViewData["Message"] = "Password Updaetd Successfully";
                    ViewData["MsgType"] = "success";
                }
                else
                {
                    ViewData["Message"] = result;
                    ViewData["MsgType"] = "warning";
                }
                return RedirectToAction("Login");
            }
            else if (connection.Execute(updatepwE, pl) == 1)
            {
                Console.WriteLine(vm.EmpPw);
                ViewData["Message"] = "Updated successfully.";
                return RedirectToAction("Login");
            }
            else
            {
                ViewData["Message"] = "Unsuccessful update. Please try again.";
                return RedirectToAction("ChangePassword");
            }
        }
    }

    private static bool AuthenticateUser(string uid, string pw, out ClaimsPrincipal principal)
    {
        principal = null!;

        DataTable ds = DBUtl.GetTable(LOGIN_SQ, uid, pw);
        DataTable de = DBUtl.GetTable(LOGIN_EMP, uid, pw);
        if (ds.Rows.Count == 1)
        {
            principal =
               new ClaimsPrincipal(
                  new ClaimsIdentity(
                     new Claim[] {
                         new Claim(ClaimTypes.NameIdentifier, uid),
                         new Claim(ClaimTypes.Role, ds.Rows[0]["roles_type"].ToString()!)
                     }, "Basic"
                  )
               );
            return true;
        }
        else if (de.Rows.Count == 1)
        {
            principal =
               new ClaimsPrincipal(
                  new ClaimsIdentity(
                     new Claim[] {
                         new Claim(ClaimTypes.NameIdentifier, uid),
                         new Claim(ClaimTypes.Role, de.Rows[0]["roles_type"].ToString()!)
                     }, "Basic"
                  )
               );
            return true;
        }
        return false;
    }
}