using Meeting_Of_Minutes.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Meeting_Of_Minutes.Controllers
{
    public class DepartmentController : Controller
    {
        public IActionResult DepartmentAddEdit(int? id)
        {
            return View();
        }

        public IActionResult DepartmentList()
        {
            List<DepartmentModel> departments = new List<DepartmentModel>();   // Create a list to hold the department data

            SqlConnection connection = new SqlConnection("Data Source=ESPELHO\\SQLEXPRESS;Initial Catalog=MOM;Integrated Security=True; TrustServerCertificate=True;");   // Establish a connection to the database

            SqlCommand cmd = new SqlCommand();  // Create a SQL command to retrieve department data
            cmd.Connection = connection;  // Set the connection for the command
            cmd.CommandText = "PR_Department_SelectAll";  // Specify the stored procedure to execute
            cmd.CommandType = CommandType.StoredProcedure; // Specify that the command is a stored procedure

            connection.Open(); // Open the database connection

            SqlDataReader sdr = cmd.ExecuteReader(); // Execute the command and get a data reader to read the results

            while (sdr.Read())
            {
                DepartmentModel department = new DepartmentModel(); // Create a new instance of the DepartmentModel class
                department.DepartmentID = Convert.ToInt32(sdr["DepartmentID"]); // Set the DepartmentID property from the data reader
                department.DepartmentName = sdr["DepartmentName"].ToString(); // Set the DepartmentName property from the data reader
                department.Created = Convert.ToDateTime(sdr["Created"]); // Set the Created property from the data reader
                department.Modified = Convert.ToDateTime(sdr["Modified"]); // Set the Modified property from the data reader
                departments.Add(department); // Add the department to the list
            }

            sdr.Close(); // Close the data reader
            connection.Close(); // Close the database connection

            return View(departments); // Return the view with the list of departments as the model
        }

        public IActionResult DepartmentView(int id)
        {

            ViewBag.DepartmentID = id;
            ViewBag.DepartmentName = "Customer Service";
            ViewBag.Created = DateTime.Parse("2025-11-20 10:45:09");
            ViewBag.Modified = DateTime.Parse("2025-11-20 22:38:36");

            return View();
        }

        public IActionResult Save(DepartmentModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("DepartmentAddEdit", model);
            }

            return RedirectToAction("DepartmentList");
        }
    }
}
