import { useQuery } from "@tanstack/react-query";
import { staffProfileService } from "@/services/staffProfileService";

interface UseStaffSearchParams {
  query?: string;
  department?: string;
  location?: string;
  roles?: string[];
  status?: string;
  pageSize?: number;
  enabled?: boolean;
}

export const useStaffSearch = ({
  query,
  department,
  location,
  roles,
  status,
  pageSize = 15,
  enabled = true,
}: UseStaffSearchParams) => {
  return useQuery({
    queryKey: [
      "staff-search",
      query,
      department,
      location,
      roles?.join("-"),
      status,
      pageSize,
    ],
    queryFn: async () => {
      const response = await staffProfileService.searchStaff({
        query,
        department,
        location,
        roles,
        status,
        pageSize,
      });

      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Unable to search staff profiles"
        );
      }

      return response.data;
    },
    enabled,
    staleTime: 60_000, // 1 minute
  });
};
