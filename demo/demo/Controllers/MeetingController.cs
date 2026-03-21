using demo.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace demo.Controllers
{
    public class MeetingController : Controller
    {
        public IActionResult MeetingList()
        {
            List<MeetingsModel> list = new List<MeetingsModel>();

            SqlConnection con = new SqlConnection("Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True");

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MOM_MEETINGS_LIST_JOIN";
            cmd.CommandType = CommandType.StoredProcedure;

            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                MeetingsModel m = new MeetingsModel();
                m.MeetingID = Convert.ToInt32(reader["MeetingID"]);
                m.MeetingName = Convert.ToString(reader["MeetingName"]);
                m.MeetingDate = Convert.ToDateTime(reader["MeetingDate"]);
                m.MeetingTypeID = Convert.ToInt32(reader["MeetingTypeID"]);
                m.MeetingTypeName = reader["MeetingTypeName"].ToString();
                m.MeetingDescription = Convert.ToString(reader["MeetingDescription"]);
                m.DepartmentID = Convert.ToInt32(reader["DepartmentID"]);
                m.DepartmentName = reader["DepartmentName"].ToString();
                m.MeetingVenueID = Convert.ToInt32(reader["MeetingVenueID"]);
                m.MeetingVenueName = reader["MeetingVenueName"].ToString();
                list.Add(m);
            }

            reader.Close();
            con.Close();

            return View(list);
        }

        public IActionResult MeetingAddEdit(int? meetingid)
        {
            ViewBag.MeetingTypeList = FillMeetingTypeDropDown();
            ViewBag.MeetingVenueList = FillMeetingVenueDropDown();
            ViewBag.DepartmentList = FillDepartmentDropDown();

            MeetingsModel meeting = new MeetingsModel();
            if (meetingid.HasValue && meetingid > 0)
            {
                SqlConnection con = new SqlConnection("Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True;");

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "PR_MOM_MEETINGS_SELECTBYPK";

                cmd.Parameters.AddWithValue("@MEETINGID", meetingid);

                con.Open();

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        meeting.MeetingID = Convert.ToInt32(reader["MeetingID"]);
                        meeting.MeetingName = reader["MeetingName"].ToString();
                        meeting.MeetingDate = Convert.ToDateTime(reader["MeetingDate"]);
                        meeting.MeetingDescription = reader["MeetingDescription"].ToString();
                        meeting.DocumentPath = reader["DocumentPath"].ToString();
                        meeting.MeetingVenueID = Convert.ToInt32(reader["MeetingVenueId"]);
                        meeting.MeetingTypeID = Convert.ToInt32(reader["MeetingTypeID"]);
                        meeting.DepartmentID = Convert.ToInt32(reader["DepartmentID"]);
                    }
                }
                con.Close();
                return View(meeting);
            }
            return View();
        }

        public IActionResult Save(MeetingsModel model)
        {
            

            SqlConnection con = new SqlConnection("Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True;");
            con.Open();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@MEETINGNAME", model.MeetingName);
            cmd.Parameters.AddWithValue("@MEETINGDATE", model.MeetingDate);
            cmd.Parameters.AddWithValue("@MEETINGDESCRIPTION", model.MeetingDescription);
            cmd.Parameters.AddWithValue("@DOCUMENTPATH", model.DocumentPath);
            cmd.Parameters.AddWithValue("@MEETINGTYPEID", model.MeetingTypeID);
            cmd.Parameters.AddWithValue("@MEETINGVENUEID", model.MeetingVenueID);
            cmd.Parameters.AddWithValue("@DEPARTMENTID", model.DepartmentID);


            if (model.MeetingID > 0)
            {
                cmd.CommandText = "PR_MOM_MEETINGS_UPDATE_BY_PK";
                cmd.Parameters.AddWithValue("@MEETINGID", model.MeetingID);
            }
            else
            {
                cmd.CommandText = "PR_MOM_MEETINGS_INSERT";
            }


            cmd.ExecuteNonQuery();
            con.Close();
            return RedirectToAction("MeetingList");
        }

        public IActionResult DeleteMeeting(int meetingid)
        {
            SqlConnection con = new SqlConnection("Server=IndStar\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True;");

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MOM_MEETINGS_DELETE_BY_PK";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@MEETINGID", meetingid);

            con.Open();
            try
            {
                int rows = cmd.ExecuteNonQuery();
                if (rows > 0)
                {
                    TempData["SuccessMsg"] = "Meeting deleted";
                }
                con.Close();

            }
            catch
            {
                TempData["ErrMsg"] = "Cannot delete this meeting because staff or meetings are linked to it.";
            }
            return RedirectToAction("MeetingList");
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

        public List<SelectListItem> FillMeetingVenueDropDown()
        {

            List<SelectListItem> mvlist = new List<SelectListItem>();

            SqlConnection con = new SqlConnection("Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True");

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MOM_MEETINGVENUE_DDL";
            cmd.CommandType = CommandType.StoredProcedure;

            con.Open();

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                mvlist.Add(new SelectListItem(reader["MeetingVenueName"].ToString(), reader["MeetingVenueID"].ToString()));
            }

            reader.Close();
            con.Close();

            return mvlist;
        }

        public List<SelectListItem> FillMeetingTypeDropDown()
        {

            List<SelectListItem> mtlist = new List<SelectListItem>();

            SqlConnection con = new SqlConnection("Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True");

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MOM_MEETINGTYPE_DDL";
            cmd.CommandType = CommandType.StoredProcedure;

            con.Open();

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                mtlist.Add(new SelectListItem(reader["MeetingTypeName"].ToString(), reader["MeetingTypeID"].ToString()));
            }

            reader.Close();
            con.Close();

            return mtlist;
        }
    }
}