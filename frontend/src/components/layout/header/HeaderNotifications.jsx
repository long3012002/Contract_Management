import React from "react";
import { Bell, Clock } from "lucide-react";
import * as DropdownMenu from "@radix-ui/react-dropdown-menu";
import { cn } from "@/lib/utils";

const MOCK_NOTIFICATIONS = [
  {
    id: 1,
    title: "Bạn được giao một công việc mới",
    description: "Công việc 'Thiết kế giao diện Admin' trong dự án Quản lý Dự án.",
    time: "10 phút trước",
    read: false,
  },
  {
    id: 2,
    title: "Cập nhật tiến độ dự án",
    description: "Dự án CRM đã đạt 80% tiến độ. Vui lòng kiểm tra các công việc còn lại.",
    time: "2 giờ trước",
    read: false,
  },
  {
    id: 3,
    title: "Lịch họp dự án",
    description: "Cuộc họp định kỳ hàng tuần sẽ diễn ra vào 14:00 chiều nay.",
    time: "1 ngày trước",
    read: true,
  },
  {
    id: 4,
    title: "Tài liệu mới được thêm",
    description: "Nguyễn Văn A đã tải lên tài liệu 'Yêu cầu chức năng v2.0'.",
    time: "2 ngày trước",
    read: true,
  }
];

export default function HeaderNotifications() {
  const unreadCount = MOCK_NOTIFICATIONS.filter((n) => !n.read).length;

  return (
    <DropdownMenu.Root>
      <DropdownMenu.Trigger asChild>
        <button
          aria-label="Thông báo"
          className="relative p-2 text-muted-foreground hover:text-foreground transition-colors hover:bg-zinc-100 rounded-full outline-none focus-visible:ring-2 focus-visible:ring-primary cursor-pointer"
        >
          <Bell className="w-5 h-5" />
          {unreadCount > 0 && (
            <span
              className="absolute -top-1 -right-1 min-w-[18px] h-[18px] px-1 flex items-center justify-center rounded-full bg-destructive text-[10px] font-bold text-white tabular-nums border-2 border-white"
              aria-label={`${unreadCount} thông báo chưa đọc`}
            >
              {unreadCount}
            </span>
          )}
        </button>
      </DropdownMenu.Trigger>

      <DropdownMenu.Portal>
        <DropdownMenu.Content
          align="end"
          sideOffset={8}
          className="z-50 w-[380px] bg-white rounded-xl border border-border shadow-lg overflow-hidden animate-in fade-in-95 duration-100"
        >
          <div className="p-4 border-b border-border flex items-center justify-between">
            <h3 className="font-semibold text-foreground text-sm text-balance">Thông báo</h3>
            <button className="text-xs font-medium text-primary hover:underline outline-none cursor-pointer">
              Đánh dấu tất cả đã đọc
            </button>
          </div>
          <div className="max-h-[400px] overflow-y-auto overscroll-contain">
            {MOCK_NOTIFICATIONS.length === 0 ? (
              <div className="p-8 text-center text-sm text-muted-foreground">
                Không có thông báo nào.
              </div>
            ) : (
              <div className="flex flex-col">
                {MOCK_NOTIFICATIONS.map((notification) => (
                  <DropdownMenu.Item
                    key={notification.id}
                    className={cn(
                      "relative flex flex-col gap-1 p-4 outline-none cursor-pointer select-none transition-colors border-l-[3px]",
                      notification.read
                        ? "border-transparent hover:bg-zinc-50 data-[highlighted]:bg-zinc-50 hover:border-primary data-[highlighted]:border-primary"
                        : "bg-primary/[0.04] border-primary/20 hover:bg-primary/[0.08] data-[highlighted]:bg-primary/[0.08] hover:border-primary data-[highlighted]:border-primary"
                    )}
                  >
                    <div className="flex items-start justify-between gap-4">
                      <span className={cn(
                        "text-sm text-balance",
                        notification.read ? "text-foreground font-medium" : "text-foreground font-semibold"
                      )}>
                        {notification.title}
                      </span>
                      {!notification.read && (
                        <span className="w-2 h-2 rounded-full bg-primary shrink-0 mt-1.5" />
                      )}
                    </div>
                    <p className={cn(
                      "text-sm text-pretty line-clamp-2",
                      notification.read ? "text-muted-foreground" : "text-foreground/80"
                    )}>
                      {notification.description}
                    </p>
                    <div className="flex items-center gap-1.5 text-muted-foreground mt-1">
                      <Clock className="w-3.5 h-3.5" />
                      <span className="text-xs tabular-nums">
                        {notification.time}
                      </span>
                    </div>
                  </DropdownMenu.Item>
                ))}
              </div>
            )}
          </div>
          <div className="p-3 text-center border-t border-border bg-zinc-50/50">
            <button className="text-sm font-medium text-muted-foreground hover:text-foreground transition-colors outline-none cursor-pointer">
              Xem tất cả thông báo
            </button>
          </div>
        </DropdownMenu.Content>
      </DropdownMenu.Portal>
    </DropdownMenu.Root>
  );
}
