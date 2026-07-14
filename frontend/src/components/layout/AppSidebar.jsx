import { Link } from "react-router-dom";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { useSidebarNavigation } from "../../hooks/useSidebarNavigation";
import { useSidebarStore } from "../../hooks/useSidebarStore";
import { cn } from "@/lib/utils";
import { Tooltip, TooltipTrigger, TooltipContent, TooltipProvider } from "@/components/ui/tooltip";
import logo from "../../assets/logo/logo_Co-opBank.png";
import logoCollapsed from "../../assets/logo/logo2.jpeg";

export function AppSidebar() {
  const { navigationItems, checkIsActive } = useSidebarNavigation();
  const isCollapsed = useSidebarStore((state) => state.isCollapsed);
  const toggleSidebar = useSidebarStore((state) => state.toggleSidebar);

  return (
    <TooltipProvider>
      <aside className={cn(
        "sticky top-0 left-0 z-20 flex flex-col shrink-0 h-dvh bg-sidebar border-r border-sidebar-border transition-all duration-300",
        isCollapsed ? "w-16" : "w-64"
      )}>
        {/* Toggle Button */}
        <button
          onClick={toggleSidebar}
          aria-label={isCollapsed ? "Expand sidebar" : "Collapse sidebar"}
          className="absolute -right-3 top-6 bg-white border border-sidebar-border text-muted-foreground hover:text-foreground hover:bg-zinc-100 dark:bg-zinc-950 dark:hover:bg-zinc-800 rounded-full p-1 shadow-sm hover:shadow transition-all cursor-pointer z-50 focus:outline-none"
        >
          {isCollapsed ? <ChevronRight className="w-3.5 h-3.5" /> : <ChevronLeft className="w-3.5 h-3.5" />}
        </button>

        {/* Logo Area */}
        <div className={cn(
          "flex items-center h-16 border-b border-sidebar-border shrink-0 transition-all duration-300",
          isCollapsed ? "justify-center px-2" : "px-6"
        )}>
          <Link to="/" className="flex items-center gap-2 overflow-hidden">
            {isCollapsed ? (
              <img src={logoCollapsed} alt="Co-opBank Logo Collapsed" className="h-8 w-8 object-contain rounded-md transition-all duration-300" />
            ) : (
              <img src={logo} alt="Co-opBank Logo" className="h-8 object-contain transition-all duration-300" />
            )}
          </Link>
        </div>


        {/* Navigation Links */}
        <div className="flex-1 overflow-y-auto py-4 px-3 space-y-1">
          {navigationItems.map((item) => {
            const isActive = checkIsActive(item.path);
            
            const linkContent = (
              <Link
                to={item.path}
                className={cn(
                  "flex items-center gap-3 px-3 py-2 rounded-md transition-colors",
                  isCollapsed && "justify-center px-0",
                  isActive
                    ? "bg-sidebar-accent text-sidebar-primary border-l-2 border-primary"
                    : "text-sidebar-foreground hover:bg-sidebar-accent/50 hover:text-sidebar-foreground/80 border-l-2 border-transparent"
                )}
              >
                <item.icon className={cn("w-5 h-5 shrink-0", isActive ? "text-primary" : "text-muted-foreground")} />
                {!isCollapsed && (
                  <span className={cn("font-medium text-sm transition-all duration-300 opacity-100 whitespace-nowrap", isActive ? "text-primary" : "")}>
                    {item.name}
                  </span>
                )}
              </Link>
            );

            if (isCollapsed) {
              return (
                <Tooltip key={item.name} delayDuration={100}>
                  <TooltipTrigger asChild>
                    {linkContent}
                  </TooltipTrigger>
                  <TooltipContent side="right" className="font-medium">
                    {item.name}
                  </TooltipContent>
                </Tooltip>
              );
            }

            return <div key={item.name}>{linkContent}</div>;
          })}
        </div>
        
        {/* Bottom Profile/Settings snippet if needed */}
        <div className="p-4 border-t border-sidebar-border shrink-0 flex justify-center">
          {isCollapsed ? (
            <span className="text-xs text-muted-foreground font-semibold">©</span>
          ) : (
            <div className="text-xs text-muted-foreground text-center whitespace-nowrap overflow-hidden">
              © 2026 Co-opBank
            </div>
          )}
        </div>
      </aside>
    </TooltipProvider>
  );
}

