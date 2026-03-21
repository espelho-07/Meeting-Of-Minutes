using demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;

namespace demo.Controllers
{
    public class StaffController : Controller
    {
        [HttpGet]
        public IActionResult StaffList()
        {
            List<StaffModel> list = getAllStaff(null);
            return View(list);
        }

        [HttpPost]
        public IActionResult StaffList(IFormCollection formdata)
        {
            string searchtext = formdata["searchtext"].ToString();

            if (string.IsNullOrWhiteSpace(searchtext))
                searchtext = null;

            ViewBag.searchtext = searchtext;

            List<StaffModel> list = getAllStaff(searchtext);
            return View(list);
        }
        public List<StaffModel> getAllStaff(string searchtext)
        {
            List<StaffModel> stafflist = new List<StaffModel>();

            SqlConnection con = new SqlConnection("Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True");

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MOM_DEPARTMENT_STAFF_JOIN";
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
                StaffModel s = new StaffModel();
                s.DepartmentID = Convert.ToInt32(reader["DepartmentId"]);
                s.DepartmentName = reader["DepartmentName"].ToString();
                s.StaffID = Convert.ToInt32(reader["StaffID"]);
                s.StaffName = reader["StaffName"].ToString();
                s.MobileNo = reader["MobileNo"].ToString();
                s.EmailAddress = reader["EmailAddress"].ToString();
                s.Created = Convert.ToDateTime(reader["Created"]);
                //s.Modified = Convert.ToDateTime(reader["Modified"]);
                stafflist.Add(s);
            }

            reader.Close();
            con.Close();

            return stafflist;
        }

        public IActionResult StaffAddEdit(int? staffid)
        {
            ViewBag.DepartmentList = FillDepartmentDropDown();

            StaffModel staff = new StaffModel();
            SqlConnection con = new SqlConnection("Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True;");

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "PR_MOM_STAFF_SELECTBYPK";

            if (staffid.HasValue && staffid > 0)
            {

                cmd.Parameters.AddWithValue("@STAFFID", staffid);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        staff.StaffName = reader["Staffname"].ToString();
                        staff.DepartmentID = Convert.ToInt32(reader["DepartmentId"]);
                        staff.EmailAddress = reader["EmailAddress"].ToString();
                        staff.MobileNo = reader["MobileNo"].ToString();
                        staff.Remarks = reader["Remarks"].ToString();
                        staff.StaffID = Convert.ToInt32(reader["StaffId"]);
                    }
                }
                reader.Close();

                return View(staff);
            }
            con.Close();
            return View();
        }

        public IActionResult SaveStaff(StaffModel model)
        {
            ViewBag.DepartmentList = FillDepartmentDropDown();

            if (!ModelState.IsValid)
            {
                return View("StaffAddEdit", model);
            }
            try
            {
                SqlConnection con = new SqlConnection("Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True;");

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@DEPARTMENTID", model.DepartmentID);
                cmd.Parameters.AddWithValue("@STAFFNAME", model.StaffName);
                cmd.Parameters.AddWithValue("@MOBILENO", model.MobileNo);
                cmd.Parameters.AddWithValue("@EMAILADDRESS", model.EmailAddress);
                cmd.Parameters.AddWithValue("@REMARKS", model.Remarks);

                if (model.StaffID > 0)
                {
                    cmd.CommandText = "PR_MOM_STAFF_UPDATE_BY_PK";
                    cmd.Parameters.AddWithValue("@STAFFID", model.StaffID);
                }
                else
                {
                    cmd.CommandText = "PR_MOM_STAFF_INSERT";
                }

                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
            }
            catch (Exception ex)
            {
                TempData["DDLerror"] = ex.Message;
            }
            return RedirectToAction("StaffList");
        }

        public List<SelectListItem> FillDepartmentDropDown()
        {

            List<SelectListItem> deptlist = new List<SelectListItem>();


            SqlConnection con = new SqlConnection("Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True");

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MOM_DEPARTMENT_DDL";
            cmd.CommandType = CommandType.StoredProcedure;

            con.Open();

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                deptlist.Add(new SelectListItem(reader["DepartmentName"].ToString(), reader["DepartmentID"].ToString()));
            }

            reader.Close();
            con.Close();

            return deptlist;
        }

        public IActionResult DeleteStaff(int staffid)
        {
            SqlConnection con = new SqlConnection("Server=IndStar\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True;");

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MOM_STAFF_DELETE_BY_PK";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@STAFFID", staffid);

            con.Open();
            try
            {
                int rows = cmd.ExecuteNonQuery();
                if (rows > 0)
                {
                    TempData["SuccessMsg"] = "Record deleted";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrMsg"] = "Staff is linked to other field !";
            }
            con.Close();
            return RedirectToAction("StaffList");
        }
    }
}
