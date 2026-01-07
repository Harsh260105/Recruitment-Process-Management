import { useState, useEffect, useMemo } from "react";
import { useNavigate } from "react-router-dom";
import {
  useSearchJobOffers,
  useGetOffersRequiringAction,
  useGetExpiringOffers,
  useGetExpiredOffers,
} from "@/hooks/staff";
import {
  OFFER_STATUS_OPTIONS,
  OFFER_STATUS_ENUM_MAP,
  OFFER_STATUS_MAP,
} from "@/constants/offer.Status";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { useDebounce } from "@/hooks/useDebounce";
import { Search } from "lucide-react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { getErrorMessage } from "@/utils/error";
import { formatDateToLocal } from "@/utils/dateUtils";

const toIsoDateString = (dateStr: string) => {
  const date = new Date(dateStr);
  return isNaN(date.getTime()) ? undefined : date.toISOString();
};

const FILTER_STATUS_OPTIONS = [
  { value: "all", status: "All" },
  ...OFFER_STATUS_OPTIONS,
];

export const OffersPage = () => {
  const navigate = useNavigate();
  const [searchTerm, setSearchTerm] = useState("");
  const [status, setStatus] = useState("all");
  const [extendedByUserId, setExtendedByUserId] = useState("");
  const [offerFromDate, setOfferFromDate] = useState("");
  const [offerToDate, setOfferToDate] = useState("");
  const [expiryFromDate, setExpiryFromDate] = useState("");
  const [expiryToDate, setExpiryToDate] = useState("");
  const [minSalary, setMinSalary] = useState(100000);
  const [maxSalary, setMaxSalary] = useState(1000000);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const [requiringActionPage, setRequiringActionPage] = useState(1);
  const [expiringPage, setExpiringPage] = useState(1);
  const [expiredPage, setExpiredPage] = useState(1);

  const [daysAhead, setDaysAhead] = useState(7);

  const canResetFilters =
    status != "all" ||
    !!searchTerm ||
    !!extendedByUserId ||
    !!offerFromDate ||
    !!offerToDate ||
    !!expiryFromDate ||
    !!expiryToDate ||
    !!minSalary ||
    !!maxSalary;

  const handleResetFilter = () => {
    setStatus("all");
    setExtendedByUserId("");
    setOfferFromDate("");
    setOfferToDate("");
    setExpiryFromDate("");
    setExpiryToDate("");
    setMinSalary(100000);
    setMaxSalary(1000000);
  };

  const debouncedSearchTerm = useDebounce(searchTerm, 500);

  // Reset to page 1 when filters change
  useEffect(() => {
    setPageNumber(1);
  }, [
    debouncedSearchTerm,
    status,
    extendedByUserId,
    offerFromDate,
    offerToDate,
    expiryFromDate,
    expiryToDate,
    minSalary,
    maxSalary,
    pageSize,
  ]);

  const searchParams = useMemo(() => {
    const params: Record<string, unknown> = {};

    if (debouncedSearchTerm.trim() !== "") {
      params.searchTerm = debouncedSearchTerm;
    }

    if (status != "all") {
      params.status = OFFER_STATUS_ENUM_MAP[status];
    }

    if (extendedByUserId) {
      params.extendedByUserId = extendedByUserId;
    }

    if (offerFromDate) {
      params.offerFromDate = toIsoDateString(offerFromDate);
    }

    if (offerToDate) {
      params.offerToDate = toIsoDateString(offerToDate);
    }

    if (expiryFromDate) {
      params.expiryFromDate = toIsoDateString(expiryFromDate);
    }

    if (expiryToDate) {
      params.expiryToDate = toIsoDateString(expiryToDate);
    }

    if (minSalary) {
      params.minSalary = minSalary;
    }

    if (maxSalary) {
      params.maxSalary = maxSalary;
    }

    return params;
  }, [
    debouncedSearchTerm,
    status,
    extendedByUserId,
    offerFromDate,
    offerToDate,
    expiryFromDate,
    expiryToDate,
    minSalary,
    maxSalary,
    pageNumber,
    pageSize,
  ]);

  const useSearchJobOffersQuery = useSearchJobOffers(
    pageNumber,
    pageSize,
    searchParams
  );

  const useGetOffersRequiringActionQuery = useGetOffersRequiringAction({
    pageNumber: requiringActionPage,
    pageSize: 10,
  });

  const useGetExpiringOffersQuery = useGetExpiringOffers(
    daysAhead,
    expiringPage,
    10
  );
  const useGetExpiredOffersQuery = useGetExpiredOffers({
    pageNumber: expiredPage,
    pageSize: 10,
  });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-semibold text-foreground">Offer room</h1>
        <p className="text-muted-foreground">Draft, send, and track offers.</p>
      </div>
      <Card>
        <CardHeader>
          <CardTitle>Filters</CardTitle>
          <CardDescription>
            Use the filters below to narrow down the list of offers.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-3">
              <Label htmlFor="search-input">Search</Label>
              <div className="relative">
                <Input
                  id="search-input"
                  type="text"
                  placeholder="Search offers"
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-9"
                />
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              </div>
            </div>

            <div className="space-y-1">
              <Label htmlFor="status-filter">Status</Label>
              <Select
                value={status}
                onValueChange={(value) => setStatus(value)}
              >
                <SelectTrigger id="status-filter">
                  <SelectValue placeholder="All Statuses" />
                </SelectTrigger>
                <SelectContent>
                  {FILTER_STATUS_OPTIONS.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      {option.status}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="space-y-2">
              <Label htmlFor="extended-by-user-id">Extended By User ID</Label>
              <Input
                id="extended-by-user-id"
                type="text"
                placeholder="eg. user-123"
                value={extendedByUserId}
                onChange={(e) => setExtendedByUserId(e.target.value)}
              />
            </div>
            <div className="space-y-1">
              <Label htmlFor="max-salary">Max Salary</Label>
              <Input
                id="max-salary"
                type="number"
                inputMode="numeric"
                placeholder="Max Salary"
                min="0"
                value={maxSalary}
                onChange={(e) => setMaxSalary(Number(e.target.value))}
              ></Input>
            </div>
            <div className="space-y-1">
              <Label htmlFor="min-salary">Min Salary</Label>
              <Input
                id="min-salary"
                type="number"
                inputMode="numeric"
                placeholder="Min Salary"
                min="0"
                value={minSalary}
                onChange={(e) => setMinSalary(Number(e.target.value))}
              ></Input>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div className="space-y-1">
              <Label htmlFor="offer-from-date">Offer From Date</Label>
              <Input
                id="offer-from-date"
                value={offerFromDate}
                onChange={(e) => setOfferFromDate(e.target.value)}
                type="date"
              />
            </div>
            <div className="space-y-1">
              <Label htmlFor="offer-to-date">Offer To Date</Label>
              <Input
                id="offer-to-date"
                value={offerToDate}
                onChange={(e) => setOfferToDate(e.target.value)}
                type="date"
              />
            </div>
            <div className="space-y-1">
              <Label htmlFor="expiry-from-date">Expiry From Date</Label>
              <Input
                id="expiry-from-date"
                value={expiryFromDate}
                onChange={(e) => setExpiryFromDate(e.target.value)}
                type="date"
              />
            </div>
            <div className="space-y-1">
              <Label htmlFor="expiry-to-date">Expiry To Date</Label>
              <Input
                id="expiry-to-date"
                value={expiryToDate}
                onChange={(e) => setExpiryToDate(e.target.value)}
                type="date"
              />
            </div>
          </div>

          <div className="flex justify-end gap-4">
            <div className="w-auto">
              <Select
                value={pageSize.toString()}
                onValueChange={(value) => setPageSize(Number(value))}
                aria-label="Select page size"
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="10">10</SelectItem>
                  <SelectItem value="25">25</SelectItem>
                  <SelectItem value="50">50</SelectItem>
                  <SelectItem value="100">100</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <Button
              type="button"
              variant="outline"
              onClick={handleResetFilter}
              disabled={!canResetFilters}
            >
              Reset Filters
            </Button>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Offers</CardTitle>
          <CardDescription>Manage and track job offers</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {useSearchJobOffersQuery.isLoading && (
            <div className="flex justify-center py-6">
              <LoadingSpinner />
            </div>
          )}

          {useSearchJobOffersQuery.isError && (
            <p className="rounded-md border border-destructive/40 bg-destructive/5 p-3 text-sm text-destructive">
              {getErrorMessage(useSearchJobOffersQuery.error)}
            </p>
          )}

          {!useSearchJobOffersQuery.isLoading &&
            !useSearchJobOffersQuery.isError &&
            !useSearchJobOffersQuery.data?.items?.length && (
              <p className="text-sm text-muted-foreground">
                No offers match the current filters.
              </p>
            )}

          {useSearchJobOffersQuery.data?.items?.length && (
            <div className="rounded-lg border border-slate-100">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Offer ID</TableHead>
                    <TableHead>Candidate</TableHead>
                    <TableHead>Job</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Offer Date</TableHead>
                    <TableHead>Expiry Date</TableHead>
                    <TableHead>Salary</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {useSearchJobOffersQuery.data.items.map((offer) => (
                    <TableRow key={offer.id}>
                      <TableCell className="font-mono text-xs text-muted-foreground">
                        {offer.id}
                      </TableCell>
                      <TableCell className="font-medium">
                        {offer.candidateName ?? "Unknown"}
                      </TableCell>
                      <TableCell>{offer.jobTitle ?? "—"}</TableCell>
                      <TableCell>
                        <Badge variant="secondary">
                          {OFFER_STATUS_MAP[offer.status || 0] || "Unknown"}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        {formatDateToLocal(offer.offerDate)}
                      </TableCell>
                      <TableCell>
                        {formatDateToLocal(offer.expiryDate)}
                      </TableCell>
                      <TableCell>{offer.offeredSalary ?? "—"}</TableCell>
                      <TableCell className="text-right">
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => navigate(`/admin/offer/${offer.id}`)}
                        >
                          View
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
        {useSearchJobOffersQuery.data &&
          useSearchJobOffersQuery.data.totalCount! > 0 && (
            <CardContent className="pt-0">
              <div className="flex items-center justify-between border-t pt-4">
                <div className="text-sm text-muted-foreground">
                  Showing {useSearchJobOffersQuery.data.items?.length} of{" "}
                  {useSearchJobOffersQuery.data.totalCount} offers
                  {useSearchJobOffersQuery.data.totalPages! > 1 && (
                    <span className="ml-2">
                      (Page {useSearchJobOffersQuery.data.pageNumber} of{" "}
                      {useSearchJobOffersQuery.data.totalPages})
                    </span>
                  )}
                </div>
                {useSearchJobOffersQuery.data.totalPages! > 1 && (
                  <div className="flex items-center gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setPageNumber(pageNumber - 1)}
                      disabled={!useSearchJobOffersQuery.data?.hasPreviousPage}
                    >
                      Previous
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setPageNumber(pageNumber + 1)}
                      disabled={!useSearchJobOffersQuery.data?.hasNextPage}
                    >
                      Next
                    </Button>
                  </div>
                )}
              </div>
            </CardContent>
          )}
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Offers Requiring Action</CardTitle>
          <CardDescription>Offers that need attention</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {useGetOffersRequiringActionQuery.isLoading && (
            <div className="flex justify-center py-6">
              <LoadingSpinner />
            </div>
          )}

          {useGetOffersRequiringActionQuery.isError && (
            <p className="rounded-md border border-destructive/40 bg-destructive/5 p-3 text-sm text-destructive">
              {getErrorMessage(useGetOffersRequiringActionQuery.error)}
            </p>
          )}

          {!useGetOffersRequiringActionQuery.isLoading &&
            !useGetOffersRequiringActionQuery.isError &&
            !useGetOffersRequiringActionQuery.data?.items?.length && (
              <p className="text-sm text-muted-foreground">
                No offers requiring action.
              </p>
            )}

          {useGetOffersRequiringActionQuery.data?.items?.length && (
            <div className="rounded-lg border border-slate-100">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Offer ID</TableHead>
                    <TableHead>Candidate</TableHead>
                    <TableHead>Job</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Offer Date</TableHead>
                    <TableHead>Expiry Date</TableHead>
                    <TableHead>Salary</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {useGetOffersRequiringActionQuery.data.items.map((offer) => (
                    <TableRow key={offer.id}>
                      <TableCell className="font-mono text-xs text-muted-foreground">
                        {offer.id}
                      </TableCell>
                      <TableCell className="font-medium">
                        {offer.candidateName ?? "Unknown"}
                      </TableCell>
                      <TableCell>{offer.jobTitle ?? "—"}</TableCell>
                      <TableCell>
                        <Badge variant="secondary">
                          {OFFER_STATUS_MAP[offer.status || 0] || "Unknown"}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        {formatDateToLocal(offer.offerDate)}
                      </TableCell>
                      <TableCell>
                        {formatDateToLocal(offer.expiryDate)}
                      </TableCell>
                      <TableCell>{offer.offeredSalary ?? "—"}</TableCell>
                      <TableCell className="text-right">
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => navigate(`/admin/offer/${offer.id}`)}
                        >
                          View
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
        {useGetOffersRequiringActionQuery.data &&
          useGetOffersRequiringActionQuery.data.totalCount! > 0 && (
            <CardContent className="pt-0">
              <div className="flex items-center justify-between border-t pt-4">
                <div className="text-sm text-muted-foreground">
                  Showing {useGetOffersRequiringActionQuery.data.items?.length}{" "}
                  of {useGetOffersRequiringActionQuery.data.totalCount} offers
                  {useGetOffersRequiringActionQuery.data.totalPages! > 1 && (
                    <span className="ml-2">
                      (Page {useGetOffersRequiringActionQuery.data.pageNumber}{" "}
                      of {useGetOffersRequiringActionQuery.data.totalPages})
                    </span>
                  )}
                </div>
                {useGetOffersRequiringActionQuery.data.totalPages! > 1 && (
                  <div className="flex items-center gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() =>
                        setRequiringActionPage(requiringActionPage - 1)
                      }
                      disabled={
                        !useGetOffersRequiringActionQuery.data?.hasPreviousPage
                      }
                    >
                      Previous
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() =>
                        setRequiringActionPage(requiringActionPage + 1)
                      }
                      disabled={
                        !useGetOffersRequiringActionQuery.data?.hasNextPage
                      }
                    >
                      Next
                    </Button>
                  </div>
                )}
              </div>
            </CardContent>
          )}
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Expiring Offers</CardTitle>
          <CardDescription>
            Offers expiring in the next {daysAhead} days
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center gap-4">
            <Label htmlFor="days-ahead">Days Ahead</Label>
            <Input
              id="days-ahead"
              type="number"
              min="1"
              max="365"
              value={daysAhead}
              onChange={(e) => setDaysAhead(Number(e.target.value) || 7)}
              className="w-20"
            />
          </div>
          {useGetExpiringOffersQuery.isLoading && (
            <div className="flex justify-center py-6">
              <LoadingSpinner />
            </div>
          )}

          {useGetExpiringOffersQuery.isError && (
            <p className="rounded-md border border-destructive/40 bg-destructive/5 p-3 text-sm text-destructive">
              {getErrorMessage(useGetExpiringOffersQuery.error)}
            </p>
          )}

          {!useGetExpiringOffersQuery.isLoading &&
            !useGetExpiringOffersQuery.isError &&
            !useGetExpiringOffersQuery.data?.items?.length && (
              <p className="text-sm text-muted-foreground">
                No expiring offers.
              </p>
            )}

          {useGetExpiringOffersQuery.data?.items?.length && (
            <div className="rounded-lg border border-slate-100">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Offer ID</TableHead>
                    <TableHead>Candidate</TableHead>
                    <TableHead>Job</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Offer Date</TableHead>
                    <TableHead>Expiry Date</TableHead>
                    <TableHead>Salary</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {useGetExpiringOffersQuery.data.items.map((offer) => (
                    <TableRow key={offer.id}>
                      <TableCell className="font-mono text-xs text-muted-foreground">
                        {offer.id}
                      </TableCell>
                      <TableCell className="font-medium">
                        {offer.candidateName ?? "Unknown"}
                      </TableCell>
                      <TableCell>{offer.jobTitle ?? "—"}</TableCell>
                      <TableCell>
                        <Badge variant="secondary">
                          {OFFER_STATUS_MAP[offer.status || 0] || "Unknown"}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        {formatDateToLocal(offer.offerDate)}
                      </TableCell>
                      <TableCell>
                        {formatDateToLocal(offer.expiryDate)}
                      </TableCell>
                      <TableCell>{offer.offeredSalary ?? "—"}</TableCell>
                      <TableCell className="text-right">
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => navigate(`/admin/offer/${offer.id}`)}
                        >
                          View
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
        {useGetExpiringOffersQuery.data &&
          useGetExpiringOffersQuery.data.totalCount! > 0 && (
            <CardContent className="pt-0">
              <div className="flex items-center justify-between border-t pt-4">
                <div className="text-sm text-muted-foreground">
                  Showing {useGetExpiringOffersQuery.data.items?.length} of{" "}
                  {useGetExpiringOffersQuery.data.totalCount} offers
                  {useGetExpiringOffersQuery.data.totalPages! > 1 && (
                    <span className="ml-2">
                      (Page {useGetExpiringOffersQuery.data.pageNumber} of{" "}
                      {useGetExpiringOffersQuery.data.totalPages})
                    </span>
                  )}
                </div>
                {useGetExpiringOffersQuery.data.totalPages! > 1 && (
                  <div className="flex items-center gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setExpiringPage(expiringPage - 1)}
                      disabled={
                        !useGetExpiringOffersQuery.data?.hasPreviousPage
                      }
                    >
                      Previous
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setExpiringPage(expiringPage + 1)}
                      disabled={!useGetExpiringOffersQuery.data?.hasNextPage}
                    >
                      Next
                    </Button>
                  </div>
                )}
              </div>
            </CardContent>
          )}
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Expired Offers</CardTitle>
          <CardDescription>Offers that have expired</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {useGetExpiredOffersQuery.isLoading && (
            <div className="flex justify-center py-6">
              <LoadingSpinner />
            </div>
          )}

          {useGetExpiredOffersQuery.isError && (
            <p className="rounded-md border border-destructive/40 bg-destructive/5 p-3 text-sm text-destructive">
              {getErrorMessage(useGetExpiredOffersQuery.error)}
            </p>
          )}

          {!useGetExpiredOffersQuery.isLoading &&
            !useGetExpiredOffersQuery.isError &&
            !useGetExpiredOffersQuery.data?.items?.length && (
              <p className="text-sm text-muted-foreground">
                No expired offers.
              </p>
            )}

          {useGetExpiredOffersQuery.data?.items?.length && (
            <div className="rounded-lg border border-slate-100">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Offer ID</TableHead>
                    <TableHead>Candidate</TableHead>
                    <TableHead>Job</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Offer Date</TableHead>
                    <TableHead>Expiry Date</TableHead>
                    <TableHead>Salary</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {useGetExpiredOffersQuery.data.items.map((offer) => (
                    <TableRow key={offer.id}>
                      <TableCell className="font-mono text-xs text-muted-foreground">
                        {offer.id}
                      </TableCell>
                      <TableCell className="font-medium">
                        {offer.candidateName ?? "Unknown"}
                      </TableCell>
                      <TableCell>{offer.jobTitle ?? "—"}</TableCell>
                      <TableCell>
                        <Badge variant="secondary">
                          {OFFER_STATUS_MAP[offer.status || 0] || "Unknown"}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        {formatDateToLocal(offer.offerDate)}
                      </TableCell>
                      <TableCell>
                        {formatDateToLocal(offer.expiryDate)}
                      </TableCell>
                      <TableCell>{offer.offeredSalary ?? "—"}</TableCell>
                      <TableCell className="text-right">
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => navigate(`/admin/offer/${offer.id}`)}
                        >
                          View
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
        {useGetExpiredOffersQuery.data &&
          useGetExpiredOffersQuery.data.totalCount! > 0 && (
            <CardContent className="pt-0">
              <div className="flex items-center justify-between border-t pt-4">
                <div className="text-sm text-muted-foreground">
                  Showing {useGetExpiredOffersQuery.data.items?.length} of{" "}
                  {useGetExpiredOffersQuery.data.totalCount} offers
                  {useGetExpiredOffersQuery.data.totalPages! > 1 && (
                    <span className="ml-2">
                      (Page {useGetExpiredOffersQuery.data.pageNumber} of{" "}
                      {useGetExpiredOffersQuery.data.totalPages})
                    </span>
                  )}
                </div>
                {useGetExpiredOffersQuery.data.totalPages! > 1 && (
                  <div className="flex items-center gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setExpiredPage(expiredPage - 1)}
                      disabled={!useGetExpiredOffersQuery.data?.hasPreviousPage}
                    >
                      Previous
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setExpiredPage(expiredPage + 1)}
                      disabled={!useGetExpiredOffersQuery.data?.hasNextPage}
                    >
                      Next
                    </Button>
                  </div>
                )}
              </div>
            </CardContent>
          )}
      </Card>
    </div>
  );
};
