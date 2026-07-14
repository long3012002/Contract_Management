import { useLocation } from "react-router-dom";
import {
  LayoutDashboard,
  Landmark,
  FolderKanban,
  Package,
  Building2,
  FileText,
  Users,
  Settings,
  Bell,
  Briefcase,
  ClipboardList
} from "lucide-react";

export function useSidebarNavigation() {
  const location = useLocation();

  const navigationItems = [
    {
      name: "Dashboard",
      path: "/dashboard",
      icon: LayoutDashboard,
    },
    {
      name: "Nguồn vốn",
      path: "/source-projects",
      icon: Landmark,
    },
    {
      name: "Dự án",
      path: "/projects",
      icon: FolderKanban,
    },
    {
      name: "Gói thầu",
      path: "/bid-packages",
      icon: Package,
    },
    {
      name: "Nhà thầu",
      path: "/contractors",
      icon: Building2,
    },
    {
      name: "Hợp đồng",
      path: "/contracts",
      icon: FileText,
    },
    {
      name: "Công việc",
      path: "/tasks",
      icon: Briefcase,
    },
    {
      name: "Đội ngũ",
      path: "/team",
      icon: Users,
    },
    {
      name: "Phân quyền",
      path: "/permissions",
      icon: Users,
    },
    {
      name: "Lịch sử hoạt động",
      path: "/audit-logs",
      icon: ClipboardList,
    },
    {
      name: "Cài đặt",
      path: "/settings",
      icon: Settings,
    },
  ];

  const checkIsActive = (path) => {
    return location.pathname.startsWith(path);
  };

  return {
    navigationItems,
    checkIsActive,
  };
}
