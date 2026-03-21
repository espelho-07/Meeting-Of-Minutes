using demo.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;

namespace demo.Controllers
{
    public class DepartmentController : Controller
    {

        private readonly IWebHostEnvironment _env;
        private readonly string connectionString =
            "Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True;";

        public DepartmentController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet]
        public IActionResult DepartmentList()
        {
            List<DepartmentModel> list = getAllDepartment(null);
            return View(list);
        }

        [HttpPost]
        public IActionResult DepartmentList(IFormCollection formdata)
        {
            string searchtext = formdata["searchtext"].ToString();

            if (string.IsNullOrWhiteSpace(searchtext))
                searchtext = null;

            ViewBag.searchtext = searchtext;

            List<DepartmentModel> list = getAllDepartment(searchtext);
            return View(list);
        }

        public IActionResult DepartmentDetailed(int? deptid)
        {
            DepartmentModel deptdetail = new DepartmentModel();

            SqlConnection con = new SqlConnection("Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True;");

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "PR_MOM_DEPARTMENT_SELECTBYPK";

            cmd.Parameters.AddWithValue("@DEPTID", deptid);

            con.Open();

            SqlDataReader reader = cmd.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    deptdetail.DepartmentName = reader["DepartmentName"].ToString();
                    deptdetail.DepartmentID = Convert.ToInt32(reader["DepartmentId"]);
                    deptdetail.DepartmentLogo = reader["DepartmentLogo"].ToString();
                    deptdetail.Created = Convert.ToDateTime(reader["Created"]);
                    deptdetail.Modified = Convert.ToDateTime(reader["Modified"]);
                }
            }
            con.Close();
            return View(deptdetail);
        }

        public IActionResult DepartmentAddEdit(int? deptId)
        {
            DepartmentModel department = new DepartmentModel();
            if (deptId.HasValue && deptId > 0)
            {
                SqlConnection con = new SqlConnection("Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True;");

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "PR_MOM_DEPARTMENT_SELECTBYPK";

                cmd.Parameters.AddWithValue("@DEPTID", deptId);

                con.Open();

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        department.DepartmentName = reader["DepartmentName"].ToString();
                        department.DepartmentID = Convert.ToInt32(reader["DepartmentId"]);
                        department.DepartmentLogo = reader["DepartmentLogo"].ToString();
                    }
                }
                con.Close();
                return View(department);
            }
            return View();
        }

        [HttpPost]
        public IActionResult Save(DepartmentModel model)
        {
            string filePath = "";

            if (model.LogoFile != null)
            {
                string folder = Path.Combine(_env.WebRootPath, "uploads");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid() + Path.GetExtension(model.LogoFile.FileName);

                string fullPath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    model.LogoFile.CopyTo(stream);
                }

                filePath = "/uploads/" + fileName;
            }

            model.Created = DateTime.Now;
            model.Modified = DateTime.Now;

            SqlConnection con = new SqlConnection("Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True;");
            con.Open();


            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@DEPARTNAME", model.DepartmentName);
            cmd.Parameters.AddWithValue("@DEPARTMENTLOGO", filePath);

            if (model.DepartmentID > 0)
            {
                cmd.CommandText = "PR_MOM_DEPARTMENT_UPDATE_BY_PK";
                cmd.Parameters.AddWithValue("@DEPARTMENTID", model.DepartmentID);
            }
            else
            {
                cmd.CommandText = "PR_MOM_DEPARTMENT_INSERT";
            }

            cmd.ExecuteNonQuery();
            con.Close();
            return RedirectToAction("DepartmentList");
        }


        public IActionResult DeleteDepartment(int deptid)
        {
            SqlConnection con = new SqlConnection("Server=IndStar\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True;");

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MOM_DEPARTMENT_DELETE_BY_PK";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@DepartmentId", deptid);

            con.Open();
            try
            {
                int rows = cmd.ExecuteNonQuery();
                if (rows > 0)
                {
                    TempData["SuccessMsg"] = "Department deleted";
                }


                con.Close();

            }
            catch
            {
                TempData["ErrMsg"] = "Cannot delete this department because staff or meetings are linked to it.";
            }
            return RedirectToAction("DepartmentList");
        }
        public List<DepartmentModel> getAllDepartment(string searchtext)
        {
            List<DepartmentModel> list = new List<DepartmentModel>();

            SqlConnection con = new SqlConnection("Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True");

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MOM_DEPARTMENT_LIST_JOIN";
            cmd.CommandType = CommandType.StoredProcedure;

            if (searchtext != null)
            {
                cmd.Parameters.AddWithValue("@searchtext", searchtext);
            }
            else
            {
                cmd.Parameters.AddWithValue("@searchtext", DBNull.Value);
            }

            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                DepartmentModel d = new DepartmentModel();
                d.DepartmentID = Convert.ToInt32(reader["DepartmentId"]);
                d.DepartmentName = reader["DepartmentName"].ToString();
                d.StaffCount = Convert.ToInt32(reader["StaffCount"]);
                d.MeetingCount = Convert.ToInt32(reader["MeetingCount"]);
                d.Created = Convert.ToDateTime(reader["Created"]);
                d.Modified = Convert.ToDateTime(reader["Modified"]);
                d.DepartmentLogo = reader["DepartmentLogo"].ToString();
                list.Add(d);
            }

            reader.Close();
            con.Close();

            return list;
        }
    }
}