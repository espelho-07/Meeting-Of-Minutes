using Meeting_Of_Minutes.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc.Rendering;
using ClosedXML.Excel;

namespace Meeting_Of_Minutes.Controllers
{
    public class MeetingMemberController : Controller
    {
        #region Actions
        public IActionResult MeetingMemberAddEdit(int? id)
        {
            ViewBag.MeetingDropDown = FillMeetingDropDown();
            ViewBag.StaffDropDown = FillStaffDropDown();

            MeetingMemberModel model = new MeetingMemberModel();

            if (id.HasValue)
            {
                SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_MeetingMember_SelectByPK";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MeetingMemberID", id.Value);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    model.MeetingMemberID = Convert.ToInt32(reader["MeetingMemberID"]);
                    model.MeetingID = Convert.ToInt32(reader["MeetingID"]);
                    model.StaffID = Convert.ToInt32(reader["StaffID"]);
                    model.IsPresent = Convert.ToBoolean(reader["IsPresent"]);
                    model.Remarks = reader["Remarks"].ToString();
                }

                reader.Close();
                con.Close();
            }

            return View(model);
        }


        public IActionResult MeetingMemberList()
        {
            List<MeetingMemberModel> list = new List<MeetingMemberModel>();

            SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MeetingMember_SelectAll";
            cmd.CommandType = CommandType.StoredProcedure;

            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                MeetingMemberModel model = new MeetingMemberModel();
                model.MeetingMemberID = Convert.ToInt32(reader["MeetingMemberID"]);
                model.MeetingID = Convert.ToInt32(reader["MeetingID"]);
                model.StaffID = Convert.ToInt32(reader["StaffID"]);
                model.MeetingDescription = reader["MeetingDescription"].ToString();
                model.StaffName = reader["StaffName"].ToString();
                model.IsPresent = Convert.ToBoolean(reader["IsPresent"]);
                model.Remarks = reader["Remarks"].ToString();
                list.Add(model);
            }

            reader.Close();
            con.Close();

            return View(list);
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
                cmd.CommandText = "PR_MeetingMember_SelectAll";

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                dt.Load(dr);
                dr.Close();
                con.Close();

                using (XLWorkbook workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("MeetingMembers");

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

                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MeetingMemberList.xlsx");
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error exporting data: " + ex.Message;
                return RedirectToAction("MeetingMemberList");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public IActionResult Save(MeetingMemberModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.MeetingDropDown = FillMeetingDropDown();
                ViewBag.StaffDropDown = FillStaffDropDown();
                return View("MeetingMemberAddEdit", model);
            }

            SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
            con.Open();

            if (model.MeetingMemberID == 0)
            {
                SqlCommand checkCmd = new SqlCommand();
                checkCmd.Connection = con;
                checkCmd.CommandText = "SELECT COUNT(*) FROM MOM_MeetingMember WHERE MeetingID = @MeetingID AND StaffID = @StaffID";
                checkCmd.CommandType = CommandType.Text;
                checkCmd.Parameters.AddWithValue("@MeetingID", model.MeetingID);
                checkCmd.Parameters.AddWithValue("@StaffID", model.StaffID);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    con.Close();
                    ModelState.AddModelError("StaffID", "Same staff already added in this meeting.");
                    ViewBag.MeetingDropDown = FillMeetingDropDown();
                    ViewBag.StaffDropDown = FillStaffDropDown();
                    return View("MeetingMemberAddEdit", model);
                }
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandType = CommandType.StoredProcedure;

            if (model.MeetingMemberID == 0)
            {
                cmd.CommandText = "PR_MeetingMember_Insert";
                cmd.Parameters.AddWithValue("@MeetingID", model.MeetingID);
                cmd.Parameters.AddWithValue("@StaffID", model.StaffID);
                cmd.Parameters.AddWithValue("@IsPresent", model.IsPresent);
                cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? string.Empty);
                cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
            }
            else
            {
                cmd.CommandText = "PR_MeetingMember_UpdateByPK";
                cmd.Parameters.AddWithValue("@MeetingMemberID", model.MeetingMemberID);
                cmd.Parameters.AddWithValue("@IsPresent", model.IsPresent);
                cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? string.Empty);
            }
            TempData["SuccessMessage"] = model.MeetingMemberID == 0 ? "Meeting member added successfully." : "Meeting member updated successfully.";
            cmd.ExecuteNonQuery();
            con.Close();

            return RedirectToAction("MeetingMemberList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int MeetingMemberID)
        {
            try
            {
                SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_MeetingMember_DeleteByPK";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MeetingMemberID", MeetingMemberID);

                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
                TempData["SuccessMessage"] = "Meeting member deleted successfully.";
            }
            catch
            {
                TempData["DeleteError"] = "Delete failed.";
            }

            return RedirectToAction("MeetingMemberList");
        }


        public List<SelectListItem> FillMeetingDropDown()
        {
            List<SelectListItem> list = new List<SelectListItem>();
            SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_Meetings_SelectAll";
            cmd.CommandType = CommandType.StoredProcedure;
            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string value = reader["MeetingID"].ToString();
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
                        Value = value,
                        Text = reader["MeetingDescription"].ToString()
                    });
                }
            }
            reader.Close();
            con.Close();
            return list;
        }

        public List<SelectListItem> FillStaffDropDown()
        {
            List<SelectListItem> list = new List<SelectListItem>();
            SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_Staff_SelectAll";
            cmd.CommandType = CommandType.StoredProcedure;
            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string value = reader["StaffID"].ToString();
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
                        Value = value,
                        Text = reader["StaffName"].ToString()
                    });
                }
            }
            reader.Close();
            con.Close();
            return list;
        }
        #endregion
    }
}






