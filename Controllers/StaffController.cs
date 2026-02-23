using Meeting_Of_Minutes.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Meeting_Of_Minutes.Controllers
{
    public class StaffController : Controller
    {
        public IActionResult StaffAddEdit()
        {
            return View();
        }
        public IActionResult StaffList()
        {
            List<StaffModel> staffList = new List<StaffModel>();  // Create a list to hold the department data

            SqlConnection connection = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");   // Establish a connection to the database

            SqlCommand cmd = new SqlCommand();  // Create a SQL command to retrieve staff data
            cmd.Connection = connection;  // Set the connection for the command
            cmd.CommandText = "PR_Staff_SelectAll";  // Specify the stored procedure to execute
            cmd.CommandType = CommandType.StoredProcedure; // Specify that the command is a stored procedure

            connection.Open(); // Open the database connection

            SqlDataReader sdr = cmd.ExecuteReader(); // Execute the command and get a data reader to read the results

            while (sdr.Read()) // Loop through the results
            {
                StaffModel model = new StaffModel(); // Create a new instance of the StaffModel class
                model.StaffID = Convert.ToInt32(sdr["StaffID"]); // Set the StaffID property from the data reader
                model.DepartmentID = Convert.ToInt32(sdr["DepartmentID"]); // Set the DepartmentID property from the data reader
                model.StaffName = sdr["StaffName"].ToString(); // Set the StaffName property from the data reader
                model.MobileNo = sdr["MobileNo"].ToString(); // Set the MobileNo property from the data reader
                model.EmailAddress =    sdr["EmailAddress"].ToString(); // Set the EmailAddress property from the data reader
                model.Remarks = sdr["Remarks"].ToString(); // Set the Remarks property from the data reader
                staffList.Add(model); // Add the populated model to the staff list
            }

            sdr.Close(); // Close the data reader
            connection.Close(); // Close the database connection

            return View(staffList);
        }
        public IActionResult Save(StaffModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("StaffAddEdit", model);
            }

            return RedirectToAction("StaffList");
        }

    }
}
