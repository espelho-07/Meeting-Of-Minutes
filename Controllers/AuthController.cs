using Meeting_Of_Minutes.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Meeting_Of_Minutes.Controllers
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserName") != null)
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            return View(new UserModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(UserModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                string sqlConnString = _configuration.GetConnectionString("DefaultConnection")
                    ?? "Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;";

                bool isValidUser = false;
                int userId = 0;
                string userName = string.Empty;

                using (SqlConnection sqlConnection = new SqlConnection(sqlConnString))
                using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.CommandText = "PR_MST_User_SelectForLogin";
                    sqlCommand.Parameters.AddWithValue("@UserName", model.UserName);
                    sqlCommand.Parameters.AddWithValue("@Password", model.Password);

                    sqlConnection.Open();
                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            isValidUser = true;
                            userId = Convert.ToInt32(reader["UserID"]);
                            userName = reader["UserName"].ToString() ?? string.Empty;
                        }
                    }
                }

                if (isValidUser)
                {
                    TempData.Remove("SuccessMessage");
                    TempData.Remove("SucessMessage");
                    HttpContext.Session.SetInt32("UserID", userId);
                    HttpContext.Session.SetString("UserName", userName);
                    return RedirectToAction("DashBoard", "DashBoard");
                }

                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Login failed: " + ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetString("UserName") != null)
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            return View(new RegisterModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                string sqlConnString = _configuration.GetConnectionString("DefaultConnection")
                    ?? "Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;";

                using (SqlConnection sqlConnection = new SqlConnection(sqlConnString))
                {
                    sqlConnection.Open();

                    using (SqlCommand checkCommand = sqlConnection.CreateCommand())
                    {
                        checkCommand.CommandType = CommandType.Text;
                        checkCommand.CommandText = "SELECT COUNT(*) FROM MST_User WHERE UserName = @UserName";
                        checkCommand.Parameters.AddWithValue("@UserName", model.UserName);

                        int userCount = Convert.ToInt32(checkCommand.ExecuteScalar());
                        if (userCount > 0)
                        {
                            ModelState.AddModelError("UserName", "Username already exists.");
                            return View(model);
                        }
                    }

                    using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                    {
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        sqlCommand.CommandText = "PR_MST_User_Insert";
                        sqlCommand.Parameters.AddWithValue("@UserName", model.UserName);
                        sqlCommand.Parameters.AddWithValue("@Password", model.Password);
                        sqlCommand.Parameters.AddWithValue("@IsActive", true);
                        sqlCommand.Parameters.AddWithValue("@Modified", DateTime.Now);
                        sqlCommand.ExecuteNonQuery();
                    }
                }

                TempData["SuccessMessage"] = "Signup successful. Please login.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Signup failed: " + ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
