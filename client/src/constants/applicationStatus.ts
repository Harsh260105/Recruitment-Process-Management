import type { BadgeProps } from "@/components/ui/badge";

export type ApplicationStatusOption = {
  value: string;
  label: string;
};

export const APPLICATION_STATUS_OPTIONS: ApplicationStatusOption[] = [
  { value: "1", label: "Applied" },
  { value: "2", label: "Test invited" },
  { value: "3", label: "Test completed" },
  { value: "4", label: "Under review" },
  { value: "5", label: "Shortlisted" },
  { value: "6", label: "Interview" },
  { value: "7", label: "Selected" },
  { value: "8", label: "Hired" },
  { value: "9", label: "Rejected" },
  { value: "10", label: "Withdrawn" },
  { value: "11", label: "On hold" },
];

const STATUS_META_MAP: Record<
  number,
  { label: string; variant: BadgeProps["variant"] }
> = {
  1: { label: "Applied", variant: "pending" }, // Orange - just applied
  2: { label: "Test invited", variant: "info" }, // Cyan - invited to test
  3: { label: "Test completed", variant: "warning" }, // Yellow - test done, waiting
  4: { label: "Under review", variant: "secondary" }, // Gray - being reviewed
  5: { label: "Shortlisted", variant: "success" }, // Green - positive progress
  6: { label: "Interview", variant: "default" }, // Blue - interview stage
  7: { label: "Selected", variant: "success" }, // Green - selected
  8: { label: "Hired", variant: "success" }, // Green - final success
  9: { label: "Rejected", variant: "destructive" }, // Red - rejection
  10: { label: "Withdrawn", variant: "outline" }, // Border only - withdrawn
  11: { label: "On hold", variant: "secondary" }, // Gray - paused
};

export const getStatusMeta = (status?: number) => {
  return (
    STATUS_META_MAP[status ?? 0] ?? { label: "Unknown", variant: "outline" }
  );
};
