using Meeting_Of_Minutes.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Meeting_Of_Minutes.Controllers
{
    public class DepartmentController : Controller
    {
        #region Addedit

        public IActionResult DepartmentAddEdit(int? id)
        {
            DepartmentModel model = new DepartmentModel();

            if (id.HasValue)
            {
                SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_Department_SelectByPK";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@DepartmentID", id.Value);

                con.Open();
                SqlDataReader sdr = cmd.ExecuteReader();

                if (sdr.Read())
                {
                    model.DepartmentID = Convert.ToInt32(sdr["DepartmentID"]);
                    model.DepartmentName = sdr["DepartmentName"].ToString();
                }

                sdr.Close();
                con.Close();
            }

            return View(model);
        }
        #endregion

        #region GetAll
        public IActionResult DepartmentList()
        {
            List<DepartmentModel> departments = new List<DepartmentModel>();

            SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_Department_SelectAll";
            cmd.CommandType = CommandType.StoredProcedure;

            con.Open();
            SqlDataReader sdr = cmd.ExecuteReader();

            while (sdr.Read())
            {
                DepartmentModel department = new DepartmentModel();
                department.DepartmentID = Convert.ToInt32(sdr["DepartmentID"]);
                department.DepartmentName = sdr["DepartmentName"].ToString();
                department.StaffCount = Convert.ToInt32(sdr["StaffCount"]);
                department.MeetingsCount = Convert.ToInt32(sdr["MeetingsCount"]);
                departments.Add(department);
            }

            sdr.Close();
            con.Close();

            return View(departments);
        }
        #endregion

        #region View
        public IActionResult DepartmentView(int id)
        {
            DepartmentModel model = new DepartmentModel();

            SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_Department_SelectByPK";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@DepartmentID", id);

            con.Open();
            SqlDataReader sdr = cmd.ExecuteReader();

            if (sdr.Read())
            {
                model.DepartmentID = Convert.ToInt32(sdr["DepartmentID"]);
                model.DepartmentName = sdr["DepartmentName"].ToString();
                model.StaffCount = Convert.ToInt32(sdr["StaffCount"]);
                model.MeetingsCount = Convert.ToInt32(sdr["MeetingsCount"]);
            }

            sdr.Close();
            con.Close();

            return View(model);
        }

        #endregion

        #region Update Insret
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(DepartmentModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("DepartmentAddEdit", model);
            }

            SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
            con.Open();

            if (model.DepartmentID == 0)
            {
                SqlCommand checkCmd = new SqlCommand();
                checkCmd.Connection = con;
                checkCmd.CommandText = "SELECT COUNT(*) FROM MOM_Department WHERE DepartmentName = @DepartmentName";
                checkCmd.CommandType = CommandType.Text;
                checkCmd.Parameters.AddWithValue("@DepartmentName", model.DepartmentName);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    con.Close();
                    ModelState.AddModelError("DepartmentName", "Department already exists.");
                    return View("DepartmentAddEdit", model);
                }
            }
            else
            {
                SqlCommand checkCmd = new SqlCommand();
                checkCmd.Connection = con;
                checkCmd.CommandText = "SELECT COUNT(*) FROM MOM_Department WHERE DepartmentName = @DepartmentName AND DepartmentID <> @DepartmentID";
                checkCmd.CommandType = CommandType.Text;
                checkCmd.Parameters.AddWithValue("@DepartmentName", model.DepartmentName);
                checkCmd.Parameters.AddWithValue("@DepartmentID", model.DepartmentID);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    con.Close();
                    ModelState.AddModelError("DepartmentName", "Department already exists.");
                    return View("DepartmentAddEdit", model);
                }
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandType = CommandType.StoredProcedure;

            if (model.DepartmentID == 0)
            {
                cmd.CommandText = "PR_Department_Insert";
                cmd.Parameters.AddWithValue("@DepartmentName", model.DepartmentName);
                cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
            }
            else
            {
                cmd.CommandText = "PR_Department_UpdateByPK";
                cmd.Parameters.AddWithValue("@DepartmentID", model.DepartmentID);
                cmd.Parameters.AddWithValue("@DepartmentName", model.DepartmentName);
            }

            TempData["SuccessMessage"] = model.DepartmentID == 0 ? "Department added successfully." : "Department updated successfully.";
            cmd.ExecuteNonQuery();
            con.Close();

            return RedirectToAction("DepartmentList");
        }
        #endregion

        #region Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int DepartmentID)
        {
            try
            {
                SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_Department_DeleteByPK";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@DepartmentID", DepartmentID);

                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
            }
            catch
            {
                TempData["DeleteError"] = "FK violation: department linked data exists.";
            }

            return RedirectToAction("DepartmentList");
        }

        #endregion
    }
}




