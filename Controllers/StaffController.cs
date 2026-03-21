using Meeting_Of_Minutes.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc.Rendering;
using ClosedXML.Excel;

namespace Meeting_Of_Minutes.Controllers
{
    public class StaffController : Controller
    {
        #region Actions
        public IActionResult StaffAddEdit(int? id)
        {
            ViewBag.DepartmentDropDown = FillDepartmentDropDown();
            StaffModel model = new StaffModel();

            if (id.HasValue)
            {
                SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_Staff_SelectByPK";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StaffID", id.Value);

                con.Open();
                SqlDataReader sdr = cmd.ExecuteReader();

                if (sdr.Read())
                {
                    model.StaffID = Convert.ToInt32(sdr["StaffID"]);
                    model.DepartmentID = Convert.ToInt32(sdr["DepartmentID"]);
                    model.StaffName = sdr["StaffName"].ToString();
                    model.MobileNo = sdr["MobileNo"].ToString();
                    model.EmailAddress = sdr["EmailAddress"].ToString();
                    model.Remarks = sdr["Remarks"].ToString();
                }

                sdr.Close();
                con.Close();
            }

            return View(model);
        }


        [HttpGet]
        public IActionResult StaffList()
        {
            List<StaffModel> staffList = GetAllStaff(null);
            return View(staffList);
        }

        [HttpPost]
        public IActionResult StaffList(IFormCollection formdata)
        {
            string searchtext = formdata["searchtext"].ToString();

            if (string.IsNullOrWhiteSpace(searchtext))
            {
                searchtext = null;
            }

            ViewBag.searchtext = searchtext;

            List<StaffModel> staffList = GetAllStaff(searchtext);
            return View(staffList);
        }

        public List<StaffModel> GetAllStaff(string searchtext)
        {
            List<StaffModel> staffList = new List<StaffModel>();

            SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_Staff_SelectAll";
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
            SqlDataReader sdr = cmd.ExecuteReader();

            while (sdr.Read())
            {
                StaffModel model = new StaffModel();
                model.StaffID = Convert.ToInt32(sdr["StaffID"]);
                model.DepartmentID = Convert.ToInt32(sdr["DepartmentID"]);
                model.DepartmentName = sdr["DepartmentName"].ToString();
                model.StaffName = sdr["StaffName"].ToString();
                model.MobileNo = sdr["MobileNo"].ToString();
                model.EmailAddress = sdr["EmailAddress"].ToString();
                model.Remarks = sdr["Remarks"].ToString();
                staffList.Add(model);
            }

            sdr.Close();
            con.Close();

            return staffList;
        }

        public IActionResult ExportToExcel()
        {
            try
            {
                DataTable dt = new DataTable();

                SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "PR_Staff_SelectAll";
                cmd.Parameters.AddWithValue("@searchtext", DBNull.Value);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                dt.Load(dr);
                dr.Close();
                con.Close();

                using (XLWorkbook workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Staff");

                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = dt.Columns[i].ColumnName;
                        worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    }

                    for (int row = 0; row < dt.Rows.Count; row++)
                    {
                        for (int col = 0; col < dt.Columns.Count; col++)
                        {
                            worksheet.Cell(row + 2, col + 1).Value = dt.Rows[row][col]?.ToString();
                        }
                    }

                    worksheet.Columns().AdjustToContents();

                    using (MemoryStream stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        byte[] content = stream.ToArray();

                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StaffList.xlsx");
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error exporting data: " + ex.Message;
                return RedirectToAction("StaffList");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public IActionResult Save(StaffModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.DepartmentDropDown = FillDepartmentDropDown();
                return View("StaffAddEdit", model);
            }

            SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
            con.Open();

            if (model.StaffID == 0)
            {
                SqlCommand checkCmd = new SqlCommand();
                checkCmd.Connection = con;
                checkCmd.CommandText = "SELECT COUNT(*) FROM MOM_Staff WHERE EmailAddress = @EmailAddress";
                checkCmd.CommandType = CommandType.Text;
                checkCmd.Parameters.AddWithValue("@EmailAddress", model.EmailAddress);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    con.Close();
                    ModelState.AddModelError("EmailAddress", "Email already exists.");
                    ViewBag.DepartmentDropDown = FillDepartmentDropDown();
                    return View("StaffAddEdit", model);
                }
            }
            else
            {
                SqlCommand checkCmd = new SqlCommand();
                checkCmd.Connection = con;
                checkCmd.CommandText = "SELECT COUNT(*) FROM MOM_Staff WHERE EmailAddress = @EmailAddress AND StaffID <> @StaffID";
                checkCmd.CommandType = CommandType.Text;
                checkCmd.Parameters.AddWithValue("@EmailAddress", model.EmailAddress);
                checkCmd.Parameters.AddWithValue("@StaffID", model.StaffID);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    con.Close();
                    ModelState.AddModelError("EmailAddress", "Email already exists.");
                    ViewBag.DepartmentDropDown = FillDepartmentDropDown();
                    return View("StaffAddEdit", model);
                }
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandType = CommandType.StoredProcedure;

            if (model.StaffID == 0)
            {
                cmd.CommandText = "PR_Staff_Insert";
                cmd.Parameters.AddWithValue("@DepartmentID", model.DepartmentID);
                cmd.Parameters.AddWithValue("@StaffName", model.StaffName);
                cmd.Parameters.AddWithValue("@MobileNo", model.MobileNo);
                cmd.Parameters.AddWithValue("@EmailAddress", model.EmailAddress);
                cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? string.Empty);
                cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
            }
            else
            {
                cmd.CommandText = "PR_Staff_UpdateByPK";
                cmd.Parameters.AddWithValue("@StaffID", model.StaffID);
                cmd.Parameters.AddWithValue("@DepartmentID", model.DepartmentID);
                cmd.Parameters.AddWithValue("@StaffName", model.StaffName);
                cmd.Parameters.AddWithValue("@MobileNo", model.MobileNo);
                cmd.Parameters.AddWithValue("@EmailAddress", model.EmailAddress);
                cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? string.Empty);
            }
            TempData["SuccessMessage"] = model.StaffID == 0 ? "Staff added successfully." : "Staff updated successfully.";
            cmd.ExecuteNonQuery();
            con.Close();

            return RedirectToAction("StaffList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int StaffID)
        {
            try
            {
                SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_Staff_DeleteByPK";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StaffID", StaffID);

                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
                TempData["SuccessMessage"] = "Staff deleted successfully.";
            }
            catch
            {
                TempData["DeleteError"] = "FK violation: linked data exists.";
            }

            return RedirectToAction("StaffList");
        }


        public List<SelectListItem> FillDepartmentDropDown()
        {
            List<SelectListItem> list = new List<SelectListItem>();
            SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MOM_DEPARTMENT_DDL";
            cmd.CommandType = CommandType.StoredProcedure;
            con.Open();
            SqlDataReader sdr = cmd.ExecuteReader();
            while (sdr.Read())
            {
                string value = sdr["DepartmentID"].ToString();
                bool exists = false;
                foreach (var item in list)
                {
                    if (item.Value == value)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    list.Add(new SelectListItem
                    {
                        Text = sdr["DepartmentName"].ToString(),
                        Value = value
                    });
                }
            }
            sdr.Close();
            con.Close();
            return list;
        }
        #endregion
    }
}







