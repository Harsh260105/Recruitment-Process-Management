export type ApiResponse<T = undefined> = {
  success: boolean;
  message?: string | null;
  data?: T;
  errors?: string[] | null;
};

export type PagedResult<T> = {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};
