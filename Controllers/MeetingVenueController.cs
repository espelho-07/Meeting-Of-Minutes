using Meeting_Of_Minutes.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Meeting_Of_Minutes.Controllers
{
    public class MeetingVenueController : Controller
    {
        public IActionResult MeetingVenueAddEdit()
        {
            return View();
        }

        public IActionResult MeetingVenueList()
        {

            List<MeetingVenueModel> list = new List<MeetingVenueModel>();

            SqlConnection connection = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");   // Establish a connection to the database

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = connection;
            cmd.CommandText = "PR_MEETINGVENUE_SELECTALL";
            cmd.CommandType = CommandType.StoredProcedure;

            connection.Open();
            SqlDataReader reader = cmd.ExecuteReader();


            while (reader.Read())
            {
                MeetingVenueModel mv = new MeetingVenueModel();
                mv.MeetingVenueID = Convert.ToInt32(reader["MeetingVenueID"]);
                mv.MeetingVenueName = reader["MeetingVenueName"].ToString();

                list.Add(mv);
            }

            reader.Close();
            connection.Close();

            return View(list);
        }
        
        public IActionResult Save(MeetingVenueModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("MeetingVenueAddEdit", model);
            }

            return RedirectToAction("MeetingVenueList");
        }
    }
}
