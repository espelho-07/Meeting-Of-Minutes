using Meeting_Of_Minutes.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Meeting_Of_Minutes.Controllers
{
    public class MeetingsController : Controller
    {
        public IActionResult MeetingsAddEdit()
        {
            return View();
        }

        public IActionResult MeetingsList()
        {
            List<MeetingsModel> meetingsList = new List<MeetingsModel>();  // Create a list to hold the department data

            SqlConnection connection = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");   // Establish a connection to the database

            #region Connection String 
            SqlCommand cmd = new SqlCommand();  // Create a SQL command to select all records from the Meetings table
            cmd.Connection = connection; // Set the connection for the command
            cmd.CommandText = "PR_Meetings_SelectAll"; // Set the command text to the stored procedure name
            cmd.CommandType = CommandType.StoredProcedure;
            #endregion

            connection.Open(); // Open the database connection

            SqlDataReader reader = cmd.ExecuteReader(); // Execute the command and get a data reader

            while (reader.Read()) {
                MeetingsModel meeting = new MeetingsModel(); // Create a new instance of the MeetingsModel class
                // Map the data from the reader to the properties of the MeetingsModel instance
                meeting.MeetingID = Convert.ToInt32(reader["MeetingID"]);
                meeting.MeetingDate = reader["MeetingDate"] as DateTime?;
                //meeting.MeetingVenueID = reader["MeetingVenueID"] as int?;
                //meeting.MeetingTypeID = reader["MeetingTypeID"] as int?;
                //meeting.DepartmentID = reader["DepartmentID"] as int?;
                meeting.MeetingDescription = reader["MeetingDescription"] as string;
                meeting.DocumentPath = reader["DocumentPath"] as string;
                meeting.Created = Convert.ToDateTime(reader["Created"]);
                meeting.Modified = Convert.ToDateTime(reader["Modified"]);
                meeting.IsCancelled = Convert.ToBoolean(reader["IsCancelled"]);
                meeting.CancellationDateTime = reader["CancellationDateTime"] as DateTime?;
                meeting.CancellationReason = reader["CancellationReason"] as string;
                meetingsList.Add(meeting); // Add the MeetingsModel instance to the list
            }

            reader.Close(); // Close the data reader
            connection.Close(); // Close the database connection

            return View(meetingsList);
        }

        public IActionResult MeetingsDetails(int id)
        {
            ViewBag.MeetingID = id;
            return View();
        }

        public IActionResult Save(MeetingsModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("MeetingsAddEdit", model);
            }

            return RedirectToAction("MeetingsList");
        }
    }
}