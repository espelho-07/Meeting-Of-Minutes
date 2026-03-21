using Meeting_Of_Minutes.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;
using ClosedXML.Excel;

namespace Meeting_Of_Minutes.Controllers
{
    public class MeetingsTypeController : Controller
    {
        #region Actions
        [HttpGet]
        public IActionResult MeetingsTypeList()
        {
            List<MeetingTypeModel> meetingTypesList = GetAllMeetingTypes(null);
            return View(meetingTypesList);
        }

        [HttpPost]
        public IActionResult MeetingsTypeList(IFormCollection formdata)
        {
            string searchtext = formdata["searchtext"].ToString();

            if (string.IsNullOrWhiteSpace(searchtext))
            {
                searchtext = null;
            }

            ViewBag.searchtext = searchtext;

            List<MeetingTypeModel> meetingTypesList = GetAllMeetingTypes(searchtext);
            return View(meetingTypesList);
        }

        public List<MeetingTypeModel> GetAllMeetingTypes(string searchtext)
        {
            List<MeetingTypeModel> meetingTypesList = new List<MeetingTypeModel>();

            SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MeetingType_SelectAll";
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
                MeetingTypeModel meeting = new MeetingTypeModel();
                meeting.MeetingTypeID = Convert.ToInt32(reader["MeetingTypeID"]);
                meeting.MeetingTypeName = reader["MeetingTypeName"].ToString();
                meeting.Remarks = reader["Remarks"].ToString();
                meeting.Created = reader["Created"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["Created"]);
                meeting.Modified = reader["Modified"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["Modified"]);
                meetingTypesList.Add(meeting);
            }

            reader.Close();
            con.Close();

            return meetingTypesList;
        }


        public IActionResult MeetingsTypeAddEdit(int? id)
        {
            MeetingTypeModel model = new MeetingTypeModel();

            if (id.HasValue)
            {
                SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_MeetingType_SelectByPK";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MeetingTypeID", id.Value);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    model.MeetingTypeID = Convert.ToInt32(reader["MeetingTypeID"]);
                    model.MeetingTypeName = reader["MeetingTypeName"].ToString();
                    model.Remarks = reader["Remarks"].ToString();
                    model.Created = reader["Created"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["Created"]);
                    model.Modified = reader["Modified"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["Modified"]);
                }

                reader.Close();
                con.Close();
            }

            return View(model);
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
                cmd.CommandText = "PR_MeetingType_SelectAll";
                cmd.Parameters.AddWithValue("@searchtext", DBNull.Value);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                dt.Load(dr);
                dr.Close();
                con.Close();

                using (XLWorkbook workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("MeetingTypes");

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

                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MeetingTypeList.xlsx");
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error exporting data: " + ex.Message;
                return RedirectToAction("MeetingsTypeList");
            }
        }


        public IActionResult MeetingsTypeDetails(int id)
        {
            MeetingTypeModel model = new MeetingTypeModel();

            SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MeetingType_SelectByPK";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@MeetingTypeID", id);

            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                model.MeetingTypeID = Convert.ToInt32(reader["MeetingTypeID"]);
                model.MeetingTypeName = reader["MeetingTypeName"].ToString();
                model.Remarks = reader["Remarks"].ToString();
                model.Created = reader["Created"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["Created"]);
                model.Modified = reader["Modified"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["Modified"]);
            }

            reader.Close();
            con.Close();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public IActionResult Save(MeetingTypeModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("MeetingsTypeAddEdit", model);
            }

            SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
            con.Open();

            if (model.MeetingTypeID == 0)
            {
                SqlCommand checkCmd = new SqlCommand();
                checkCmd.Connection = con;
                checkCmd.CommandText = "SELECT COUNT(*) FROM MOM_MeetingType WHERE MeetingTypeName = @MeetingTypeName";
                checkCmd.CommandType = CommandType.Text;
                checkCmd.Parameters.AddWithValue("@MeetingTypeName", model.MeetingTypeName);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    con.Close();
                    ModelState.AddModelError("MeetingTypeName", "Meeting Type already exists.");
                    return View("MeetingsTypeAddEdit", model);
                }
            }
            else
            {
                SqlCommand checkCmd = new SqlCommand();
                checkCmd.Connection = con;
                checkCmd.CommandText = "SELECT COUNT(*) FROM MOM_MeetingType WHERE MeetingTypeName = @MeetingTypeName AND MeetingTypeID <> @MeetingTypeID";
                checkCmd.CommandType = CommandType.Text;
                checkCmd.Parameters.AddWithValue("@MeetingTypeName", model.MeetingTypeName);
                checkCmd.Parameters.AddWithValue("@MeetingTypeID", model.MeetingTypeID);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    con.Close();
                    ModelState.AddModelError("MeetingTypeName", "Meeting Type already exists.");
                    return View("MeetingsTypeAddEdit", model);
                }
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandType = CommandType.StoredProcedure;

            if (model.MeetingTypeID == 0)
            {
                cmd.CommandText = "PR_MeetingType_Insert";
                cmd.Parameters.AddWithValue("@MeetingTypeName", model.MeetingTypeName);
                cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? string.Empty);
                cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
            }
            else
            {
                cmd.CommandText = "PR_MeetingType_UpdateByPK";
                cmd.Parameters.AddWithValue("@MeetingTypeID", model.MeetingTypeID);
                cmd.Parameters.AddWithValue("@MeetingTypeName", model.MeetingTypeName);
                cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? string.Empty);
            }
            TempData["SuccessMessage"] = model.MeetingTypeID == 0 ? "Meeting type added successfully." : "Meeting type updated successfully.";
            cmd.ExecuteNonQuery();
            con.Close();

            return RedirectToAction("MeetingsTypeList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int MeetingTypeID)
        {
            try
            {
                SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_MeetingType_DeleteByPK";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MeetingTypeID", MeetingTypeID);

                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
                TempData["SuccessMessage"] = "Meeting type deleted successfully.";
            }
            catch
            {
                TempData["DeleteError"] = "FK violation: linked data exists.";
            }

            return RedirectToAction("MeetingsTypeList");
        }
        #endregion
    }
}








