using Meeting_Of_Minutes.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using Microsoft.Data.SqlClient;
using ClosedXML.Excel;

namespace Meeting_Of_Minutes.Controllers
{
    public class MeetingsController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public MeetingsController(IWebHostEnvironment env)
        {
            _env = env;
        }

        #region AddEdit
        public IActionResult MeetingsAddEdit(int? id)
        {
            ViewBag.DepartmentDropDown = FillDepartmentDropDown();
            ViewBag.MeetingTypeDropDown = FillMeetingTypeDropDown();
            ViewBag.MeetingVenueDropDown = FillMeetingVenueDropdown();

            MeetingsModel model = new MeetingsModel();

            if (id.HasValue)
            {
                SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_Meetings_SelectByPK";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MeetingID", id.Value);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    model.MeetingID = Convert.ToInt32(reader["MeetingID"]);
                    model.MeetingDate = reader["MeetingDate"] as DateTime?;
                    model.MeetingVenueID = reader["MeetingVenueID"] as int?;
                    model.MeetingTypeID = reader["MeetingTypeID"] as int?;
                    model.DepartmentID = reader["DepartmentID"] as int?;
                    model.MeetingDescription = reader["MeetingDescription"].ToString();
                    model.DocumentPath = reader["DocumentPath"].ToString();
                    model.IsCancelled = reader["IsCancelled"] == DBNull.Value ? false : Convert.ToBoolean(reader["IsCancelled"]);
                    model.CancellationDateTime = reader["CancellationDateTime"] as DateTime?;
                    model.CancellationReason = reader["CancellationReason"].ToString();
                }

                reader.Close();
                con.Close();
            }

            return View(model);
        }


        [HttpGet]
        public IActionResult MeetingsList()
        {
            List<MeetingsModel> meetingsList = GetAllMeetings(null);
            return View(meetingsList);
        }

        [HttpPost]
        public IActionResult MeetingsList(IFormCollection formdata)
        {
            string searchtext = formdata["searchtext"].ToString();

            if (string.IsNullOrWhiteSpace(searchtext))
            {
                searchtext = null;
            }

            ViewBag.searchtext = searchtext;

            List<MeetingsModel> meetingsList = GetAllMeetings(searchtext);
            return View(meetingsList);
        }

        public List<MeetingsModel> GetAllMeetings(string searchtext)
        {
            List<MeetingsModel> meetingsList = new List<MeetingsModel>();

            SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_Meetings_SelectAll";
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
                MeetingsModel meeting = new MeetingsModel();
                meeting.MeetingID = Convert.ToInt32(reader["MeetingID"]);
                meeting.MeetingDate = reader["MeetingDate"] as DateTime?;
                meeting.MeetingTypeName = reader["MeetingTypeName"].ToString();
                meeting.DepartmentName = reader["DepartmentName"].ToString();
                meeting.MeetingVenueName = reader["MeetingVenueName"].ToString();
                meeting.MeetingDescription = reader["MeetingDescription"].ToString();
                meeting.DocumentPath = reader["DocumentPath"].ToString();
                meeting.IsCancelled = reader["IsCancelled"] == DBNull.Value ? false : Convert.ToBoolean(reader["IsCancelled"]);
                meeting.CancellationDateTime = reader["CancellationDateTime"] as DateTime?;
                meeting.CancellationReason = reader["CancellationReason"].ToString();
                meetingsList.Add(meeting);
            }

            reader.Close();
            con.Close();

            return meetingsList;
        }


        public IActionResult MeetingsDetails(int id)
        {
            MeetingDetailsViewModel viewModel = new MeetingDetailsViewModel();
            MeetingsModel model = new MeetingsModel();

            SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_Meetings_SelectByPK";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@MeetingID", id);

            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                model.MeetingID = Convert.ToInt32(reader["MeetingID"]);
                model.MeetingDate = reader["MeetingDate"] as DateTime?;
                model.MeetingVenueID = reader["MeetingVenueID"] as int?;
                model.MeetingTypeID = reader["MeetingTypeID"] as int?;
                model.DepartmentID = reader["DepartmentID"] as int?;
                model.MeetingDescription = reader["MeetingDescription"].ToString();
                model.DocumentPath = reader["DocumentPath"].ToString();
                model.IsCancelled = reader["IsCancelled"] == DBNull.Value ? false : Convert.ToBoolean(reader["IsCancelled"]);
                model.CancellationDateTime = reader["CancellationDateTime"] as DateTime?;
                model.CancellationReason = reader["CancellationReason"].ToString();
                model.DepartmentName = GetSelectedText(FillDepartmentDropDown(), model.DepartmentID);
                model.MeetingTypeName = GetSelectedText(FillMeetingTypeDropDown(), model.MeetingTypeID);
                model.MeetingVenueName = GetSelectedText(FillMeetingVenueDropdown(), model.MeetingVenueID);
            }

            reader.Close();
            cmd.Parameters.Clear();
            cmd.CommandText = "PR_MeetingMember_SelectAll";
            cmd.CommandType = CommandType.StoredProcedure;
            reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                if (Convert.ToInt32(reader["MeetingID"]) == id)
                {
                    MeetingMemberModel member = new MeetingMemberModel();
                    member.MeetingMemberID = Convert.ToInt32(reader["MeetingMemberID"]);
                    member.MeetingID = Convert.ToInt32(reader["MeetingID"]);
                    member.StaffID = Convert.ToInt32(reader["StaffID"]);
                    member.MeetingDescription = reader["MeetingDescription"].ToString();
                    member.StaffName = reader["StaffName"].ToString();
                    member.IsPresent = Convert.ToBoolean(reader["IsPresent"]);
                    member.Remarks = reader["Remarks"].ToString();
                    viewModel.Members.Add(member);
                }
            }

            reader.Close();
            con.Close();

            viewModel.Meeting = model;
            viewModel.NewMember.MeetingID = id;
            ViewBag.StaffDropDown = FillStaffDropDown(model.DepartmentID);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddMeetingMember(MeetingDetailsViewModel viewModel)
        {
            if (viewModel.NewMember.MeetingID == 0)
            {
                TempData["ErrorMessage"] = "Meeting not found.";
                return RedirectToAction("MeetingsList");
            }

            if (viewModel.NewMember.StaffID == 0)
            {
                TempData["ErrorMessage"] = "Please select staff.";
                return RedirectToAction("MeetingsDetails", new { id = viewModel.NewMember.MeetingID });
            }

            SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
            con.Open();

            SqlCommand checkCmd = new SqlCommand();
            checkCmd.Connection = con;
            checkCmd.CommandText = "SELECT COUNT(*) FROM MOM_MeetingMember WHERE MeetingID = @MeetingID AND StaffID = @StaffID";
            checkCmd.CommandType = CommandType.Text;
            checkCmd.Parameters.AddWithValue("@MeetingID", viewModel.NewMember.MeetingID);
            checkCmd.Parameters.AddWithValue("@StaffID", viewModel.NewMember.StaffID);

            int count = Convert.ToInt32(checkCmd.ExecuteScalar());

            if (count > 0)
            {
                con.Close();
                TempData["ErrorMessage"] = "Same staff already added in this meeting.";
                return RedirectToAction("MeetingsDetails", new { id = viewModel.NewMember.MeetingID });
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MeetingMember_Insert";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@MeetingID", viewModel.NewMember.MeetingID);
            cmd.Parameters.AddWithValue("@StaffID", viewModel.NewMember.StaffID);
            cmd.Parameters.AddWithValue("@IsPresent", false);
            cmd.Parameters.AddWithValue("@Remarks", string.Empty);
            cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
            cmd.ExecuteNonQuery();
            con.Close();

            TempData["SuccessMessage"] = "Meeting member added successfully.";
            return RedirectToAction("MeetingsDetails", new { id = viewModel.NewMember.MeetingID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateMeetingMemberAttendance(int MeetingMemberID, int MeetingID, bool IsPresent, string Remarks)
        {
            SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MeetingMember_UpdateByPK";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@MeetingMemberID", MeetingMemberID);
            cmd.Parameters.AddWithValue("@IsPresent", IsPresent);
            cmd.Parameters.AddWithValue("@Remarks", Remarks ?? string.Empty);

            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();

            TempData["SuccessMessage"] = "Attendance updated successfully.";
            return RedirectToAction("MeetingsDetails", new { id = MeetingID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteMeetingMember(int MeetingMemberID, int MeetingID)
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

            return RedirectToAction("MeetingsDetails", new { id = MeetingID });
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
                cmd.CommandText = "PR_Meetings_SelectAll";
                cmd.Parameters.AddWithValue("@searchtext", DBNull.Value);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                dt.Load(dr);
                dr.Close();
                con.Close();

                using (XLWorkbook workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Meetings");

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

                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MeetingsList.xlsx");
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error exporting data: " + ex.Message;
                return RedirectToAction("MeetingsList");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public IActionResult Save(MeetingsModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.DepartmentDropDown = FillDepartmentDropDown();
                ViewBag.MeetingTypeDropDown = FillMeetingTypeDropDown();
                ViewBag.MeetingVenueDropDown = FillMeetingVenueDropdown();
                return View("MeetingsAddEdit", model);
            }

            string filePath = model.DocumentPath ?? string.Empty;

            if (model.DocumentFile != null)
            {
                string folder = Path.Combine(_env.WebRootPath, "uploads");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string fileName = Guid.NewGuid() + Path.GetExtension(model.DocumentFile.FileName);
                string fullPath = Path.Combine(folder, fileName);

                using (FileStream stream = new FileStream(fullPath, FileMode.Create))
                {
                    model.DocumentFile.CopyTo(stream);
                }

                filePath = "/uploads/" + fileName;
            }

            SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
            con.Open();

            if (model.MeetingID == 0)
            {
                SqlCommand checkCmd = new SqlCommand();
                checkCmd.Connection = con;
                checkCmd.CommandText = "SELECT COUNT(*) FROM MOM_Meetings WHERE MeetingDate=@MeetingDate AND MeetingVenueID=@MeetingVenueID AND MeetingTypeID=@MeetingTypeID AND DepartmentID=@DepartmentID";
                checkCmd.CommandType = CommandType.Text;
                checkCmd.Parameters.AddWithValue("@MeetingDate", model.MeetingDate);
                checkCmd.Parameters.AddWithValue("@MeetingVenueID", model.MeetingVenueID);
                checkCmd.Parameters.AddWithValue("@MeetingTypeID", model.MeetingTypeID);
                checkCmd.Parameters.AddWithValue("@DepartmentID", model.DepartmentID);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    con.Close();
                    ModelState.AddModelError("MeetingDate", "Same meeting already exists.");
                    ViewBag.DepartmentDropDown = FillDepartmentDropDown();
                    ViewBag.MeetingTypeDropDown = FillMeetingTypeDropDown();
                    ViewBag.MeetingVenueDropDown = FillMeetingVenueDropdown();
                    return View("MeetingsAddEdit", model);
                }
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandType = CommandType.StoredProcedure;

            if (model.MeetingID == 0)
            {
                cmd.CommandText = "PR_Meetings_Insert";
                cmd.Parameters.AddWithValue("@MeetingDate", model.MeetingDate);
                cmd.Parameters.AddWithValue("@MeetingVenueID", model.MeetingVenueID);
                cmd.Parameters.AddWithValue("@MeetingTypeID", model.MeetingTypeID);
                cmd.Parameters.AddWithValue("@DepartmentID", model.DepartmentID);
                cmd.Parameters.AddWithValue("@MeetingDescription", model.MeetingDescription ?? string.Empty);
                cmd.Parameters.AddWithValue("@DocumentPath", filePath);
                cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
            }
            else
            {
                cmd.CommandText = "PR_Meetings_UpdateByPK";
                cmd.Parameters.AddWithValue("@MeetingID", model.MeetingID);
                cmd.Parameters.AddWithValue("@MeetingDate", model.MeetingDate);
                cmd.Parameters.AddWithValue("@MeetingVenueID", model.MeetingVenueID);
                cmd.Parameters.AddWithValue("@MeetingTypeID", model.MeetingTypeID);
                cmd.Parameters.AddWithValue("@DepartmentID", model.DepartmentID);
                cmd.Parameters.AddWithValue("@MeetingDescription", model.MeetingDescription ?? string.Empty);
                cmd.Parameters.AddWithValue("@DocumentPath", filePath);
            }
            TempData["SuccessMessage"] = model.MeetingID == 0 ? "Meeting added successfully." : "Meeting updated successfully.";
            cmd.ExecuteNonQuery();
            con.Close();

            return RedirectToAction("MeetingsList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int MeetingID)
        {
            try
            {
                SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_Meetings_DeleteByPK";
                cmd.Parameters.AddWithValue("@MeetingID", MeetingID);
                cmd.CommandType = CommandType.StoredProcedure;

                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
                TempData["SuccessMessage"] = "Meeting deleted successfully.";
            }
            catch
            {
                TempData["DeleteError"] = "FK violation: linked data exists.";
            }

            return RedirectToAction("MeetingsList");
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
                    list.Add(new SelectListItem { Text = sdr["DepartmentName"].ToString(), Value = value });
                }
            }
            sdr.Close();
            con.Close();
            return list;
        }

        public List<SelectListItem> FillMeetingTypeDropDown()
        {
            List<SelectListItem> list = new List<SelectListItem>();
            SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MOM_MEETINGTYPE_DDL";
            cmd.CommandType = CommandType.StoredProcedure;
            con.Open();
            SqlDataReader sdr = cmd.ExecuteReader();
            while (sdr.Read())
            {
                string value = sdr["MeetingTypeID"].ToString();
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
                    list.Add(new SelectListItem { Text = sdr["MeetingTypeName"].ToString(), Value = value });
                }
            }
            sdr.Close();
            con.Close();
            return list;
        }

        public List<SelectListItem> FillMeetingVenueDropdown()
        {
            List<SelectListItem> list = new List<SelectListItem>();
            SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MOM_MEETINGVENUE_DDL";
            cmd.CommandType = CommandType.StoredProcedure;
            con.Open();
            SqlDataReader sdr = cmd.ExecuteReader();
            while (sdr.Read())
            {
                string value = sdr["MeetingVenueID"].ToString();
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
                    list.Add(new SelectListItem { Text = sdr["MeetingVenueName"].ToString(), Value = value });
                }
            }
            sdr.Close();
            con.Close();
            return list;
        }

        public List<SelectListItem> FillStaffDropDown(int? departmentId = null)
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
                if (departmentId.HasValue)
                {
                    int staffDepartmentId = reader["DepartmentID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["DepartmentID"]);
                    if (staffDepartmentId != departmentId.Value)
                    {
                        continue;
                    }
                }

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

        public string GetSelectedText(List<SelectListItem> list, int? id)
        {
            if (!id.HasValue)
            {
                return string.Empty;
            }

            foreach (SelectListItem item in list)
            {
                if (item.Value == id.Value.ToString())
                {
                    return item.Text;
                }
            }

            return string.Empty;
        }
        #endregion
    }
}








