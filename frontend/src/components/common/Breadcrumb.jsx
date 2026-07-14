import React from "react";
import { Link, useLocation } from "react-router-dom";
import { Home } from "lucide-react";
import {
  Breadcrumb as ShadcnBreadcrumb,
  BreadcrumbList,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";

const ROUTE_MAP = {
  dashboard: "Tổng quan",
  "source-projects": "Dự án nguồn",
  projects: "Dự án thực hiện",
  "bid-packages": "Gói thầu",
  contractors: "Nhà thầu",
  contracts: "Hợp đồng",
  tasks: "Công việc",
  team: "Đội ngũ",
  permissions: "Phân quyền",
  "audit-logs": "Nhật ký hệ thống",
  settings: "Cài đặt",
};

const isUUID = (str) => {
  return /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/.test(str);
};

export function Breadcrumb() {
  const location = useLocation();
  const pathnames = location.pathname.split("/").filter((x) => x);

  return (
    <ShadcnBreadcrumb className="mb-4">
      <BreadcrumbList>
        <BreadcrumbItem>
          <BreadcrumbLink asChild>
            <Link to="/dashboard" className="flex items-center gap-1.5 hover:text-primary transition-colors">
              <Home className="w-4 h-4" />
              <span>Trang chủ</span>
            </Link>
          </BreadcrumbLink>
        </BreadcrumbItem>

        {pathnames.map((value, index) => {
          const to = `/${pathnames.slice(0, index + 1).join("/")}`;
          const isLast = index === pathnames.length - 1;
          const label = ROUTE_MAP[value] || (isUUID(value) ? "Chi tiết" : decodeURIComponent(value));

          // If the first page is dashboard, don't repeat it
          if (value === "dashboard" && index === 0 && pathnames.length === 1) {
            return null;
          }

          return (
            <React.Fragment key={to}>
              <BreadcrumbSeparator />
              <BreadcrumbItem>
                {isLast ? (
                  <BreadcrumbPage className="font-semibold text-foreground select-none">
                    {label}
                  </BreadcrumbPage>
                ) : (
                  <BreadcrumbLink asChild>
                    <Link to={to} className="hover:text-primary transition-colors">
                      {label}
                    </Link>
                  </BreadcrumbLink>
                )}
              </BreadcrumbItem>
            </React.Fragment>
          );
        })}
      </BreadcrumbList>
    </ShadcnBreadcrumb>
  );
}
