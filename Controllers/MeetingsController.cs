using Meeting_Of_Minutes.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Meeting_Of_Minutes.Controllers
{
    public class MeetingsController : Controller
    {
        #region Actions
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


        public IActionResult MeetingsList()
        {
            List<MeetingsModel> meetingsList = new List<MeetingsModel>();

            SqlConnection con = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_Meetings_SelectAll";
            cmd.CommandType = CommandType.StoredProcedure;

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

            return View(meetingsList);
        }


        public IActionResult MeetingsDetails(int id)
        {
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
            }

            reader.Close();
            con.Close();

            return View(model);
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
                cmd.Parameters.AddWithValue("@DocumentPath", model.DocumentPath ?? string.Empty);
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
                cmd.Parameters.AddWithValue("@DocumentPath", model.DocumentPath ?? string.Empty);
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
        #endregion
    }
}








