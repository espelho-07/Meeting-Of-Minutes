using System.Collections.Generic;

namespace Meeting_Of_Minutes.Models
{
    public class MeetingDetailsViewModel
    {
        public MeetingsModel Meeting { get; set; } = new MeetingsModel();

        public MeetingMemberModel NewMember { get; set; } = new MeetingMemberModel();

        public List<MeetingMemberModel> Members { get; set; } = new List<MeetingMemberModel>();
    }
}
