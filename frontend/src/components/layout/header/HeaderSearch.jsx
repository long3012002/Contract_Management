import React from "react";
import { Search } from "lucide-react";

export default function HeaderSearch() {
  return (
    <div className="flex items-center flex-1">
      <div className="relative w-full max-w-md">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
        <input
          type="text"
          placeholder="Tìm kiếm dự án, công việc..."
          className="w-full pl-10 pr-4 py-2 text-sm bg-zinc-50 border border-input rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-shadow"
        />
      </div>
    </div>
  );
}
