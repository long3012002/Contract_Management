import React from "react";
import HeaderSearch from "./header/HeaderSearch";
import HeaderNotifications from "./header/HeaderNotifications";
import HeaderUserProfile from "./header/HeaderUserProfile";

export function PageHeader() {
  return (
    <header className="h-16 flex items-center justify-between px-6 bg-white border-b border-border sticky top-0 z-10">
      {/* Search Bar */}
      <HeaderSearch />

      {/* Right Actions */}
      <div className="flex items-center gap-4 shrink-0">
        {/* Notifications Dropdown */}
        <HeaderNotifications />

        <div className="h-6 w-px bg-border mx-2"></div>

        {/* User Profile Info */}
        <HeaderUserProfile />
      </div>
    </header>
  );
}
