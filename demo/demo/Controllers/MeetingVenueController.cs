using demo.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;

namespace demo.Controllers
{
    public class MeetingVenueController : Controller
    {
        public IActionResult MeetingVenueList()
        {
            List<MeetingVenueModel> list = new List<MeetingVenueModel>();

            SqlConnection con = new SqlConnection("Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True");

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MOM_MEETINGVENUE_JOIN";
            cmd.CommandType = CommandType.StoredProcedure;

            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                MeetingVenueModel mv = new MeetingVenueModel();
                mv.MeetingVenueID = Convert.ToInt32(reader["MeetingVenueID"]);
                mv.MeetingVenueName = Convert.ToString(reader["MeetingVenueName"]);
                mv.Created = Convert.ToDateTime(reader["Created"]);
                //mv.Modified = Convert.ToDateTime(reader["Modified"]);
                mv.MeetingCount = Convert.ToInt32(reader["MeetingCount"]);
                list.Add(mv);
            }

            reader.Close();
            con.Close();

            return View(list);
        }

        public IActionResult MeetingVenueAddEdit(int? meetingvenueid)
        {
            MeetingVenueModel meetingvenue = new MeetingVenueModel();
            if (meetingvenueid.HasValue && meetingvenueid > 0)
            {
                SqlConnection con = new SqlConnection("Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True;");

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "PR_MOM_MEETINGVENUE_SELECTBYPK";

                cmd.Parameters.AddWithValue("@MEETINGVENUEID", meetingvenueid);

                con.Open();

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        meetingvenue.MeetingVenueName = reader["MeetingVenueName"].ToString();
                    }
                }
                con.Close();
                return View(meetingvenue);
            }
            return View();
        }

        public IActionResult Save(MeetingVenueModel model)
        {
            SqlConnection con = new SqlConnection("Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True;");
            con.Open();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@MEETINGVENUENAME", model.MeetingVenueName);

            if (model.MeetingVenueID > 0)
            {
                cmd.CommandText = "PR_MOM_MEETINGVENUE_UPDATE_BY_PK";
                cmd.Parameters.AddWithValue("@MEETINGVENUEID", model.MeetingVenueID);
            }
            else
            {
                cmd.CommandText = "PR_MOM_MEETINGVENUE_INSERT";
            }

            cmd.ExecuteNonQuery();
            con.Close();
            return RedirectToAction("MeetingVenueList");
        }

        public IActionResult DeleteMeetingVenue(int meetingvenueid)
        {
            SqlConnection con = new SqlConnection("Server=IndStar\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True;");

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MOM_MEETINGVANUE_DELETE_BY_PK";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@MEETINGVANUEID", meetingvenueid);

            con.Open();
            try
            {
                int rows = cmd.ExecuteNonQuery();
                if (rows > 0)
                {
                    TempData["SuccessMsg"] = "Record deleted";
                }


                con.Close();

            }
            catch (Exception ex)
            {
                TempData["ErrMsg"] = "Can't delete this venue !";
            }
            return RedirectToAction("MeetingVenueList");
        }
    }
}
