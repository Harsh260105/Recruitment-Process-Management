import { useMemo, useState } from "react";
import { Users2, UserPlus, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import type { components } from "@/types/api";
import { useStaffSearch } from "@/hooks/staff/useStaffSearch";
import { Input } from "@/components/ui/input";
import { useDebounce } from "@/hooks/useDebounce";

type StaffProfile = components["schemas"]["StaffProfileResponseDto"];

export interface StaffSelection {
  staffProfileId: string;
  userId: string;
  name: string;
  email?: string | null;
  department?: string | null;
}

interface UserPickerProps {
  selected: StaffSelection[];
  onChange: (participants: StaffSelection[]) => void;
  placeholder?: string;
  disabled?: boolean;
  maxParticipants?: number;
}

const buildSelection = (profile: StaffProfile): StaffSelection | null => {
  if (!profile.userId) {
    return null;
  }

  const name = `${profile.firstName ?? ""} ${profile.lastName ?? ""}`.trim();

  return {
    staffProfileId: profile.id ?? profile.userId,
    userId: profile.userId,
    name: name || profile.email || "Unnamed",
    email: profile.email,
    department: profile.department,
  };
};

export const UserPicker = ({
  selected,
  onChange,
  placeholder = "Search staff to invite",
  disabled,
  maxParticipants = 6,
}: UserPickerProps) => {
  const [open, setOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [department, setDepartment] = useState("");
  const [location, setLocation] = useState("");

  const debouncedSearchTerm = useDebounce(searchTerm, 500);
  const debouncedDepartment = useDebounce(department, 500);
  const debouncedLocation = useDebounce(location, 500);

  const staffQuery = useStaffSearch({
    query: debouncedSearchTerm,
    department: debouncedDepartment,
    location: debouncedLocation,
    pageSize: 15,
    enabled: open && !disabled,
  });

  const options = useMemo(() => {
    return (staffQuery.data?.items ?? [])
      .map((profile) => buildSelection(profile))
      .filter((profile): profile is StaffSelection => Boolean(profile));
  }, [staffQuery.data?.items]);

  const handleToggle = (participant: StaffSelection) => {
    const alreadySelected = selected.some(
      (item) => item.userId === participant.userId
    );

    if (alreadySelected) {
      onChange(selected.filter((item) => item.userId !== participant.userId));
      return;
    }

    if (selected.length >= maxParticipants) {
      return;
    }

    onChange([...selected, participant]);
    setSearchTerm("");
  };

  const handleRemove = (userId: string) => {
    onChange(selected.filter((participant) => participant.userId !== userId));
  };

  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between text-sm">
        <span className="font-medium flex items-center gap-1">
          <Users2 className="h-4 w-4" /> Participants
        </span>
        <span className="text-xs text-muted-foreground">
          {selected.length}/{maxParticipants}
        </span>
      </div>
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button
            type="button"
            variant="outline"
            disabled={disabled}
            className="w-full justify-between"
          >
            <span className="flex items-center gap-2 text-left">
              <UserPlus className="h-4 w-4" />
              {selected.length
                ? `${selected.length} participant${
                    selected.length === 1 ? "" : "s"
                  }`
                : placeholder}
            </span>
            <span className="text-xs text-muted-foreground">
              {disabled ? "Disabled" : "Search"}
            </span>
          </Button>
        </PopoverTrigger>
        <PopoverContent className="p-0 bg-emerald-50 w-[683px]" align="start">
          <div className="p-2">
            <Input
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Search by name, email, or department"
              className="mb-2"
            />
            <div className="flex gap-2 mb-2">
              <Input
                value={department}
                onChange={(e) => setDepartment(e.target.value)}
                placeholder="Filter by department (optional)"
                className="flex-1"
              />
              <Input
                value={location}
                onChange={(e) => setLocation(e.target.value)}
                placeholder="Filter by location (optional)"
                className="flex-1"
              />
            </div>
            <div className="max-h-60 overflow-y-auto gap-1 flex flex-col">
              {staffQuery.isLoading ? (
                <div className="p-3 text-sm text-muted-foreground">
                  Finding available staff...
                </div>
              ) : options.length === 0 ? (
                <div className="p-3 text-sm text-muted-foreground">
                  No matching staff
                </div>
              ) : (
                options.map((participant) => {
                  const isSelected = selected.some(
                    (item) => item.userId === participant.userId
                  );
                  const isDisabled =
                    (!isSelected && selected.length >= maxParticipants) ||
                    disabled;

                  return (
                    <button
                      key={participant.userId}
                      onClick={() => {
                        if (!isDisabled) {
                          handleToggle(participant);
                        }
                      }}
                      disabled={isDisabled}
                      className={`w-full rounded-md border p-3 text-left hover:bg-accent hover:border-primary transition-colors text-sm ${
                        isDisabled
                          ? "opacity-50 cursor-not-allowed"
                          : "cursor-pointer"
                      }`}
                    >
                      <div className="flex items-center justify-between gap-2">
                        <div className="flex-1 min-w-0">
                          <span className="font-medium truncate">
                            {participant.name}
                          </span>
                          <div className="text-xs text-muted-foreground truncate">
                            {participant.email}
                            {participant.department
                              ? ` • ${participant.department}`
                              : ""}
                          </div>
                          
                        </div>
                        {isSelected && (
                          <span className="text-xs text-emerald-600">
                            Added
                          </span>
                        )}
                      </div>
                    </button>
                  );
                })
              )}
            </div>
          </div>
        </PopoverContent>
      </Popover>
      {!!selected.length && (
        <div className="flex flex-wrap gap-2">
          {selected.map((participant) => (
            <Badge
              key={participant.userId}
              variant="secondary"
              className="flex items-center gap-1"
            >
              <span>{participant.name}</span>
              <button
                type="button"
                className="rounded-full hover:bg-muted p-0.5"
                onClick={() => handleRemove(participant.userId)}
                aria-label={`Remove ${participant.name}`}
              >
                <X className="h-3 w-3" />
              </button>
            </Badge>
          ))}
        </div>
      )}
    </div>
  );
};
