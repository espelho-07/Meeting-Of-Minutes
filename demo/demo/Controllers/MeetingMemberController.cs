using demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;

namespace demo.Controllers
{
    public class MeetingMemberController : Controller
    {
        public IActionResult MeetingMembersList()
        {
            List<MeetingMemberModel> list = new List<MeetingMemberModel>();

            SqlConnection con = new SqlConnection("Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True");

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MOM_MEETINGMEMBER_SELECTALL";
            cmd.CommandType = CommandType.StoredProcedure;

            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                MeetingMemberModel mm = new MeetingMemberModel();
                mm.MeetingMemberId = Convert.ToInt32(reader["MeetingMemberId"]);
                mm.MeetingID = Convert.ToInt32(reader["MeetingID"]);
                mm.StaffID = Convert.ToInt32(reader["StaffID"]);
                mm.IsPresent = Convert.ToString(reader["IsPresent"]);
                mm.Remarks = Convert.ToString(reader["Remarks"]);
                mm.Created = Convert.ToDateTime(reader["Created"]);
                mm.Modified = Convert.ToDateTime(reader["Modified"]);
                list.Add(mm);
            }

            reader.Close();
            con.Close();

            return View(list);
        }

        public IActionResult MeetingMembersAddEdit(int? memberid)
        {
            ViewBag.MeetingNameList = FillMeetingNameDropDown();
            ViewBag.StaffList = FillStaffDropDown();

            MeetingMemberModel meetingmember = new MeetingMemberModel();
            if (memberid.HasValue && memberid > 0)
            {
                SqlConnection con = new SqlConnection("Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True;");

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "PR_MOM_MEETINGMEMBER_SELECTBYPK";

                cmd.Parameters.AddWithValue("@MEETINGMEMBERID", memberid);

                con.Open();

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        meetingmember.MeetingMemberId = Convert.ToInt32(reader["MeetingMemberID"]);
                        meetingmember.MeetingID = Convert.ToInt32(reader["MeetingID"]);
                        meetingmember.StaffID = Convert.ToInt32(reader["StaffID"]);
                        meetingmember.IsPresent = reader["IsPresent"].ToString();
                        meetingmember.Remarks = reader["Remarks"].ToString();
                    }
                }
                con.Close();
                return View(meetingmember);
            }
            return View();
        }

        public IActionResult Save(MeetingMemberModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("MeetingMembersAddEdit", model);
            }
            SqlConnection con = new SqlConnection("Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True;");
            con.Open();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@MEETINGID", model.MeetingID);
            cmd.Parameters.AddWithValue("@STAFFID", model.StaffID);
            //cmd.Parameters.AddWithValue("@ISPRESENT", model.IsPresent);
            cmd.Parameters.AddWithValue("@REMARKS", model.Remarks);

            if (model.MeetingMemberId > 0)
            {
                cmd.CommandText = "PR_MOM_MEETINGMEMBER_UPDATE_BY_PK";
                cmd.Parameters.AddWithValue("@MEETINGMEMBERID", model.MeetingMemberId);
            }
            else
            {
                cmd.CommandText = "PR_MOM_MEETINGMEMBER_INSERT";
            }

            cmd.ExecuteNonQuery();
            con.Close();
            return RedirectToAction("MeetingMembersList");
        }

        public IActionResult DeleteMeetingMember(int memberid)
        {
            SqlConnection con = new SqlConnection("Server=IndStar\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True;");

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MOM_MEETINGMEMBER_DELETE_BY_PK";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@MEETINGMEMBERID", memberid);

            con.Open();
            try
            {
                int rows = cmd.ExecuteNonQuery();
                if (rows > 0)
                {
                    TempData["SuccessMsg"] = "Meeting member deleted";
                }


                con.Close();

            }
            catch
            {
                TempData["ErrMsg"] = "Cannot delete this member because meetings are linked to it.";
            }
            return RedirectToAction("MeetingMembersList");
        }


        public List<SelectListItem> FillMeetingNameDropDown()
        {

            List<SelectListItem> meetingnamelist = new List<SelectListItem>();

            SqlConnection con = new SqlConnection("Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True");

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MOM_MEETINGS_DDL";
            cmd.CommandType = CommandType.StoredProcedure;

            con.Open();

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                meetingnamelist.Add(new SelectListItem(reader["MeetingName"].ToString(), reader["MeetingID"].ToString()));
            }

            reader.Close();
            con.Close();

            return meetingnamelist;
        }

        public List<SelectListItem> FillStaffDropDown()
        {

            List<SelectListItem> stafflist = new List<SelectListItem>();

            SqlConnection con = new SqlConnection("Server=INDSTAR\\SQLEXPRESS;Database=PROJECT_MOM;Trusted_Connection=True;TrustServerCertificate=True");

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MOM_STAFF_DDL";
            cmd.CommandType = CommandType.StoredProcedure;

            con.Open();

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                stafflist.Add(new SelectListItem(reader["StaffName"].ToString(), reader["StaffID"].ToString()));
            }

            reader.Close();
            con.Close();

            return stafflist;
        }
    }
}
