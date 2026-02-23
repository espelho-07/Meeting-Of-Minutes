using Meeting_Of_Minutes.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Meeting_Of_Minutes.Controllers
{
    public class MeetingsTypeController : Controller
    {
        // LIST
        public IActionResult MeetingsTypeList()
        {
            List<MeetingTypeModel> meetingTypesList = new List<MeetingTypeModel>(); // Create a list to hold the Meeting Type data

            SqlConnection connection = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");   // Establish a connection to the database

            #region Connection String
            SqlCommand cmd = new SqlCommand();  // Create a SQL command to select all records from the Meetings table
            cmd.Connection = connection; // Set the connection for the command
            cmd.CommandText = "[PR_MeetingType_SelectAll]"; // Set the command text to the stored procedure name
            cmd.CommandType = CommandType.StoredProcedure;
            #endregion

            connection.Open(); // Open the database connection

            SqlDataReader reader = cmd.ExecuteReader(); // Execute the command and get a data reader

            while (reader.Read())
            {
                MeetingTypeModel meeting = new MeetingTypeModel();
                meeting.MeetingTypeID = Convert.ToInt32(reader["MeetingTypeID"]);
                meeting.MeetingTypeName = reader["MeetingTypeName"] as string;
                meeting.Created = Convert.ToDateTime(reader["Created"]);
                meeting.Modified = Convert.ToDateTime(reader["Modified"]);
            }


            return View();
        }

        public IActionResult MeetingsTypeAddEdit(int? id)
        {

            

            return View();
        }

        public IActionResult MeetingsTypeDetails(int id)
        {

            ViewBag.MeetingID = id;
            return View();
        }

        
        public IActionResult Save(MeetingTypeModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("MeetingsTypeAddEdit", model);
            }

            return RedirectToAction("MeetingsTypeList");
        }
    }
}
