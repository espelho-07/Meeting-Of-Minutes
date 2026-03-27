export type NavItem = {
  id: string;
  label: string;
  icon: string;
  count?: number;
};

export type QueueItem = {
  id: string;
  title: string;
  description: string;
  meta: string;
  priority: "Critical" | "Stale" | "Fresh";
};

export type MeetingRow = {
  id: string;
  title: string;
  department: string;
  type: string;
  venue: string;
  status: "Scheduled" | "Completed" | "Cancelled";
  time: string;
};

export const navSections = [
  {
    label: "Workspaces",
    items: [
      { id: "dashboard", label: "Dashboard", icon: "LayoutDashboard" },
      { id: "action-center", label: "Action Center", icon: "Sparkles", count: 6 },
      { id: "activity", label: "Activity", icon: "History" }
    ] satisfies NavItem[]
  },
  {
    label: "Master Data",
    items: [
      { id: "meeting-types", label: "Meeting Types", icon: "Tags" },
      { id: "departments", label: "Departments", icon: "Building2" },
      { id: "venues", label: "Venues", icon: "MapPin" },
      { id: "staff", label: "Staff Members", icon: "Users" }
    ] satisfies NavItem[]
  },
  {
    label: "Meetings",
    items: [
      { id: "all-meetings", label: "All Meetings", icon: "CalendarRange" },
      { id: "schedule", label: "Schedule Meeting", icon: "CalendarPlus2" },
      { id: "attendance", label: "Attendance", icon: "UserCheck" }
    ] satisfies NavItem[]
  }
];

export const queueItems: QueueItem[] = [
  {
    id: "q-1",
    title: "3 profile updates are waiting for approval",
    description: "Most of them were submitted more than 24 hours ago and are blocking user account changes.",
    meta: "Profiles · 24h+ pending",
    priority: "Critical"
  },
  {
    id: "q-2",
    title: "Transfer request from Operations to Finance",
    description: "Staff move includes company and department remap and requires document verification.",
    meta: "Transfer · Document attached",
    priority: "Stale"
  },
  {
    id: "q-3",
    title: "Meeting cancellation spike detected",
    description: "Two meetings were cancelled in the same department this week. This deserves a closer look.",
    meta: "Risk signal · Meetings",
    priority: "Fresh"
  }
];

export const meetingRows: MeetingRow[] = [
  {
    id: "M-101",
    title: "Quarterly planning sync",
    department: "Product",
    type: "Planning",
    venue: "Main Boardroom",
    status: "Scheduled",
    time: "25 Mar, 09:30 AM"
  },
  {
    id: "M-102",
    title: "Faculty leadership review",
    department: "Academic Ops",
    type: "Review",
    venue: "Conference Hall A",
    status: "Completed",
    time: "23 Mar, 02:00 PM"
  },
  {
    id: "M-103",
    title: "Vendor alignment workshop",
    department: "Operations",
    type: "External",
    venue: "Strategy Room",
    status: "Cancelled",
    time: "21 Mar, 11:00 AM"
  }
];
