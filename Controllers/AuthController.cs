using Meeting_Of_Minutes.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

            ViewBag.CompanyDropDown = FillCompanyDropDown();
            ViewBag.DepartmentDropDown = FillDepartmentDropDown();
            return View(new UserModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(UserModel model)
        {
            ValidateDepartment(model.UserRole, model.DepartmentID);

            if (!ModelState.IsValid)
            {
                ViewBag.CompanyDropDown = FillCompanyDropDown();
                ViewBag.DepartmentDropDown = FillDepartmentDropDown(model.CompanyName);
                return View(model);
            }

            try
            {
                string sqlConnString = _configuration.GetConnectionString("DefaultConnection")
                    ?? Meeting_Of_Minutes.DbConnectionHelper.ConnectionString;

                bool isValidUser = false;
                int userId = 0;
                string userName = string.Empty;
                string email = string.Empty;
                string contactNo = string.Empty;
                string city = string.Empty;
                string userRole = string.Empty;
                string companyName = string.Empty;
                int? staffId = null;
                int? departmentId = null;
                string departmentName = string.Empty;

                using (SqlConnection sqlConnection = new SqlConnection(sqlConnString))
                using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.CommandText = "PR_MST_User_SelectForLogin";
                    sqlCommand.Parameters.AddWithValue("@Email", model.Email ?? string.Empty);
                    sqlCommand.Parameters.AddWithValue("@Password", model.Password ?? string.Empty);
                    sqlCommand.Parameters.AddWithValue("@UserRole", model.UserRole ?? string.Empty);
                    sqlCommand.Parameters.AddWithValue("@CompanyName", model.CompanyName ?? string.Empty);
                    sqlCommand.Parameters.AddWithValue("@DepartmentID", model.DepartmentID.HasValue ? model.DepartmentID.Value : DBNull.Value);

                    sqlConnection.Open();
                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            isValidUser = true;
                            userId = Convert.ToInt32(reader["UserID"]);
                            userName = reader["UserName"].ToString() ?? string.Empty;
                            email = reader["Email"].ToString() ?? string.Empty;
                            contactNo = reader["ContactNo"].ToString() ?? string.Empty;
                            city = reader["City"].ToString() ?? string.Empty;
                            userRole = reader["UserRole"].ToString() ?? string.Empty;
                            companyName = reader["CompanyName"].ToString() ?? string.Empty;
                            staffId = reader["StaffID"] == DBNull.Value ? null : Convert.ToInt32(reader["StaffID"]);
                            departmentId = reader["DepartmentID"] == DBNull.Value ? null : Convert.ToInt32(reader["DepartmentID"]);
                            departmentName = reader["DepartmentName"].ToString() ?? string.Empty;
                        }
                    }
                }

                if (isValidUser)
                {
                    TempData.Remove("SuccessMessage");
                    TempData.Remove("SucessMessage");

                    HttpContext.Session.SetInt32("UserID", userId);
                    HttpContext.Session.SetString("UserName", userName);
                    HttpContext.Session.SetString("Email", email);
                    HttpContext.Session.SetString("ContactNo", contactNo);
                    HttpContext.Session.SetString("City", city);
                    HttpContext.Session.SetString("UserRole", userRole);
                    HttpContext.Session.SetString("CompanyName", companyName);

                    if (staffId.HasValue)
                    {
                        HttpContext.Session.SetInt32("StaffID", staffId.Value);
                    }
                    else
                    {
                        HttpContext.Session.Remove("StaffID");
                    }

                    if (departmentId.HasValue)
                    {
                        HttpContext.Session.SetInt32("DepartmentID", departmentId.Value);
                    }
                    else
                    {
                        HttpContext.Session.Remove("DepartmentID");
                    }

                    HttpContext.Session.SetString("DepartmentName", departmentName);
                    return RedirectToAction("DashBoard", "DashBoard");
                }

                ModelState.AddModelError(string.Empty, "Invalid email, password, or role.");
                ViewBag.CompanyDropDown = FillCompanyDropDown();
                ViewBag.DepartmentDropDown = FillDepartmentDropDown(model.CompanyName);
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Login failed: " + ex.Message);
                ViewBag.CompanyDropDown = FillCompanyDropDown();
                ViewBag.DepartmentDropDown = FillDepartmentDropDown(model.CompanyName);
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

            ViewBag.CompanyDropDown = FillCompanyDropDown();
            ViewBag.DepartmentDropDown = FillDepartmentDropDown();
            return View(new RegisterModel { UserRole = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterModel model)
        {
            model.UserRole = "Admin";
            model.DepartmentID = null;
            ValidateDepartment(model.UserRole, model.DepartmentID);

            if (!ModelState.IsValid)
            {
                ViewBag.CompanyDropDown = FillCompanyDropDown();
                ViewBag.DepartmentDropDown = FillDepartmentDropDown(model.CompanyName);
                return View(model);
            }

            try
            {
                string sqlConnString = _configuration.GetConnectionString("DefaultConnection")
                    ?? Meeting_Of_Minutes.DbConnectionHelper.ConnectionString;

                using (SqlConnection sqlConnection = new SqlConnection(sqlConnString))
                {
                    sqlConnection.Open();

                    using (SqlCommand checkCommand = sqlConnection.CreateCommand())
                    {
                        checkCommand.CommandType = CommandType.StoredProcedure;
                        checkCommand.CommandText = "PR_MST_User_SelectByEmail";
                        checkCommand.Parameters.AddWithValue("@Email", model.Email ?? string.Empty);

                        using (SqlDataReader reader = checkCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                ModelState.AddModelError("Email", "Email already exists.");
                                ViewBag.CompanyDropDown = FillCompanyDropDown();
                                ViewBag.DepartmentDropDown = FillDepartmentDropDown(model.CompanyName);
                                return View(model);
                            }
                        }
                    }

                    using (SqlCommand nameCheckCommand = sqlConnection.CreateCommand())
                    {
                        nameCheckCommand.CommandType = CommandType.Text;
                        nameCheckCommand.CommandText = "SELECT COUNT(*) FROM MST_User WHERE UserName = @UserName";
                        nameCheckCommand.Parameters.AddWithValue("@UserName", model.UserName ?? string.Empty);

                        int userCount = Convert.ToInt32(nameCheckCommand.ExecuteScalar());
                        if (userCount > 0)
                        {
                            ModelState.AddModelError("UserName", "Username already exists.");
                            ViewBag.CompanyDropDown = FillCompanyDropDown();
                            ViewBag.DepartmentDropDown = FillDepartmentDropDown(model.CompanyName);
                            return View(model);
                        }
                    }

                    using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                    {
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        sqlCommand.CommandText = "PR_MST_User_Insert";
                        sqlCommand.Parameters.AddWithValue("@UserName", model.UserName ?? string.Empty);
                        sqlCommand.Parameters.AddWithValue("@Email", model.Email ?? string.Empty);
                        sqlCommand.Parameters.AddWithValue("@Password", model.Password ?? string.Empty);
                        sqlCommand.Parameters.AddWithValue("@ContactNo", model.ContactNo ?? string.Empty);
                        sqlCommand.Parameters.AddWithValue("@City", model.City ?? string.Empty);
                        sqlCommand.Parameters.AddWithValue("@UserRole", model.UserRole ?? "Admin");
                        sqlCommand.Parameters.AddWithValue("@CompanyName", model.CompanyName ?? string.Empty);
                        sqlCommand.Parameters.AddWithValue("@StaffID", DBNull.Value);
                        sqlCommand.Parameters.AddWithValue("@DepartmentID", model.DepartmentID.HasValue ? model.DepartmentID.Value : DBNull.Value);
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
                ViewBag.CompanyDropDown = FillCompanyDropDown();
                ViewBag.DepartmentDropDown = FillDepartmentDropDown(model.CompanyName);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public JsonResult GetDepartmentsByCompany(string companyName)
        {
            List<object> departments = FillDepartmentDropDown(companyName)
                .Select(d => new
                {
                    value = d.Value,
                    text = d.Text
                })
                .Cast<object>()
                .ToList();

            return Json(departments);
        }

        [HttpGet]
        public JsonResult GetUserContextByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { found = false });
            }

            string sqlConnString = _configuration.GetConnectionString("DefaultConnection")
                ?? Meeting_Of_Minutes.DbConnectionHelper.ConnectionString;

            using (SqlConnection sqlConnection = new SqlConnection(sqlConnString))
            using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
            {
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.CommandText = "PR_MST_User_SelectByEmail";
                sqlCommand.Parameters.AddWithValue("@Email", email);
                sqlConnection.Open();

                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return Json(new
                        {
                            found = true,
                            userRole = reader["UserRole"].ToString(),
                            companyName = reader["CompanyName"].ToString(),
                            departmentId = reader["DepartmentID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["DepartmentID"])
                        });
                    }
                }
            }

            return Json(new { found = false });
        }

        public void ValidateDepartment(string? userRole, int? departmentId)
        {
            if (string.Equals(userRole, "User", StringComparison.OrdinalIgnoreCase) && (!departmentId.HasValue || departmentId.Value == 0))
            {
                ModelState.AddModelError("DepartmentID", "Please select department.");
            }
        }

        public List<SelectListItem> FillCompanyDropDown()
        {
            List<SelectListItem> list = new List<SelectListItem>();
            string sqlConnString = _configuration.GetConnectionString("DefaultConnection")
                ?? Meeting_Of_Minutes.DbConnectionHelper.ConnectionString;

            using (SqlConnection sqlConnection = new SqlConnection(sqlConnString))
            using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
            {
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.CommandText = "PR_MST_Company_DDL";
                sqlConnection.Open();

                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new SelectListItem
                        {
                            Value = reader["CompanyName"].ToString(),
                            Text = reader["CompanyName"].ToString()
                        });
                    }
                }
            }

            return list;
        }

        public List<SelectListItem> FillDepartmentDropDown(string? companyName = null)
        {
            List<SelectListItem> list = new List<SelectListItem>();
            string sqlConnString = _configuration.GetConnectionString("DefaultConnection")
                ?? Meeting_Of_Minutes.DbConnectionHelper.ConnectionString;

            using (SqlConnection sqlConnection = new SqlConnection(sqlConnString))
            using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
            {
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.CommandText = "PR_MOM_DEPARTMENT_DDL";
                sqlCommand.Parameters.AddWithValue("@CompanyName", string.IsNullOrWhiteSpace(companyName) ? DBNull.Value : companyName);
                sqlConnection.Open();

                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new SelectListItem
                        {
                            Value = reader["DepartmentID"].ToString(),
                            Text = reader["DepartmentName"].ToString()
                        });
                    }
                }
            }

            return list;
        }
    }
}
