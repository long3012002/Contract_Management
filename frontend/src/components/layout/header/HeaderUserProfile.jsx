import React from "react";

export default function HeaderUserProfile() {
  return (
    <div className="flex items-center gap-3 cursor-pointer p-1 pr-3 hover:bg-zinc-50 rounded-full transition-colors border border-transparent hover:border-border">
      <div className="w-8 h-8 rounded-full overflow-hidden flex items-center justify-center bg-primary/20 text-primary font-semibold text-sm">
        AD
      </div>
      <div className="flex flex-col">
        <span className="text-sm font-medium leading-none text-foreground">Admin User</span>
        <span className="text-xs text-muted-foreground mt-1">Quản lý</span>
      </div>
    </div>
  );
}
