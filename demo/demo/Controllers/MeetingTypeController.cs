using demo.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;

namespace demo.Controllers
{
    public class MeetingTypeController : Controller
    {

        private readonly IConfiguration _configuration;

        public MeetingTypeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult MeetingTypeList()
        {
            List<MeetingTypeModel> list = new List<MeetingTypeModel>();

            string ?connectionString = _configuration.GetConnectionString("DefaultConnection");

            SqlConnection con = new SqlConnection(connectionString);

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MOM_MEETINGTYPE_SELECTALL";
            cmd.CommandType = CommandType.StoredProcedure;

            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                MeetingTypeModel mt = new MeetingTypeModel();
                mt.MeetingTypeID = Convert.ToInt32(reader["MeetingTypeID"]);
                mt.MeetingTypeName = Convert.ToString(reader["MeetingTypeName"]);
                mt.Remarks = Convert.ToString(reader["Remarks"]);
                mt.Created = Convert.ToDateTime(reader["Created"]);
                mt.Modified = Convert.ToDateTime(reader["Modified"]);
                list.Add(mt);
            }

            reader.Close();
            con.Close();

            return View(list);
        }

        public IActionResult MeetingTypeAddEdit(int? meetingtypeid)
        {
            MeetingTypeModel meetingtype = new MeetingTypeModel();
            if (meetingtypeid.HasValue && meetingtypeid > 0)
            {
                SqlConnection con = new SqlConnection("Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True;");

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "PR_MOM_MEETINGTYPE_SELECTBYPK";

                cmd.Parameters.AddWithValue("@MEETINGTYPEID", meetingtypeid);

                con.Open();

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        meetingtype.MeetingTypeName = reader["MeetingTypeName"].ToString();
                        meetingtype.MeetingTypeID = Convert.ToInt32(reader["MeetingTypeID"]);
                        meetingtype.Remarks = reader["Remarks"].ToString();
                    }
                }
                con.Close();
                return View(meetingtype);
            }
            return View();
        }

        public IActionResult Save(MeetingTypeModel model)
        {
            SqlConnection con = new SqlConnection("Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True;");
            con.Open();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@MEETINGTYPENAME", model.MeetingTypeName);
            cmd.Parameters.AddWithValue("@REMARKS", model.Remarks);

            if (model.MeetingTypeID > 0)
            {
                cmd.CommandText = "PR_MOM_MEETINGTYPE_UPDATE_BY_PK";
                cmd.Parameters.AddWithValue("@MeetingTypeID", model.MeetingTypeID);
            }
            else
            {
                cmd.CommandText = "PR_MOM_MEETINGTYPE_INSERT";
            }

            cmd.ExecuteNonQuery();
            con.Close();
            return RedirectToAction("MeetingTypeList");
        }

        public IActionResult DeleteMeetingType(int meetingtypeid)
        {
            SqlConnection con = new SqlConnection("Server=IndStar\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True;");

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MOM_MEETINGTYPE_DELETE_BY_PK";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@MEETINGTYPEID", meetingtypeid);

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
            catch
            {
                TempData["ErrMsg"] = "Can't delete this meeting type !";
            }
            return RedirectToAction("MeetingTypeList");
        }
    }
}
