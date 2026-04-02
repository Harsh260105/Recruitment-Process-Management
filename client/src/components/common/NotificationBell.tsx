import { useMemo } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Bell } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { cn } from "@/lib/utils";
import { notificationService } from "@/services/notificationService";

const NOTIFICATION_QUERY_KEY = ["notifications", "unread"];

export const NotificationBell = ({ className }: { className?: string }) => {
  const queryClient = useQueryClient();

  const notificationsQuery = useQuery({
    queryKey: NOTIFICATION_QUERY_KEY,
    queryFn: async () => {
      const response = await notificationService.getMyNotifications();

      if (!response.success || !response.data) {
        throw new Error(response.errors?.join(", ") || "Failed to fetch notifications");
      }

      return response.data;
    },
    staleTime: 30 * 1000,
    refetchInterval: 45 * 1000,
  });

  const markAsReadMutation = useMutation({
    mutationFn: async (notificationId: string) => {
      const response = await notificationService.markAsRead(notificationId);
      if (!response.success) {
        throw new Error(response.errors?.join(", ") || "Failed to mark notification as read");
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: NOTIFICATION_QUERY_KEY });
    },
  });

  const markAllAsReadMutation = useMutation({
    mutationFn: async () => {
      const response = await notificationService.markAllAsRead();
      if (!response.success) {
        throw new Error(response.errors?.join(", ") || "Failed to mark all notifications as read");
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: NOTIFICATION_QUERY_KEY });
    },
  });

  const notifications = notificationsQuery.data ?? [];
  const unreadCount = notifications.length;
  const unreadLabel = useMemo(() => {
    if (unreadCount > 99) return "99+";
    return unreadCount.toString();
  }, [unreadCount]);

  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          size="sm"
          className={cn("relative h-9 w-9 p-0", className)}
          aria-label="Open notifications"
        >
          <Bell className="h-4 w-4" />
          {unreadCount > 0 && (
            <span className="absolute -right-1.5 -top-1.5 min-w-5 rounded-full bg-red-600 px-1.5 text-center text-[10px] font-semibold text-white">
              {unreadLabel}
            </span>
          )}
        </Button>
      </PopoverTrigger>

      <PopoverContent align="end" className="w-[360px] border bg-white p-0 shadow-lg">
        <div className="border-b px-4 py-3">
          <div className="flex items-center justify-between">
            <p className="text-sm font-semibold">Notifications</p>
            <Button
              variant="ghost"
              size="sm"
              className="h-7 px-2 text-xs"
              disabled={unreadCount === 0 || markAllAsReadMutation.isPending}
              onClick={() => markAllAsReadMutation.mutate()}
            >
              Mark all read
            </Button>
          </div>
        </div>

        <div className="max-h-96 overflow-y-auto">
          {notificationsQuery.isLoading && (
            <p className="px-4 py-6 text-sm text-muted-foreground">Loading notifications...</p>
          )}

          {!notificationsQuery.isLoading && notifications.length === 0 && (
            <p className="px-4 py-6 text-sm text-muted-foreground">No unread notifications.</p>
          )}

          {notifications.map((notification) => (
            <div key={notification.notificationId} className="border-b bg-background px-4 py-3 last:border-b-0">
              <div className="mb-2 flex items-start justify-between gap-2">
                <p className="text-sm font-medium leading-5">{notification.title}</p>
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-7 px-2 text-xs"
                  onClick={() => markAsReadMutation.mutate(notification.notificationId)}
                  disabled={markAsReadMutation.isPending}
                >
                  Mark read
                </Button>
              </div>
              <p className="text-xs text-muted-foreground">{notification.message}</p>
              <p className="mt-2 text-[11px] text-muted-foreground">
                {new Date(notification.createdAt).toLocaleString()}
              </p>
            </div>
          ))}
        </div>
      </PopoverContent>
    </Popover>
  );
};