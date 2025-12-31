import { useMemo, useState, useRef, useEffect } from "react";
import * as XLSX from "xlsx";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
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
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import {
  useCandidateSearch,
  useCandidateProfileById,
  useSetApplicationOverride,
  useCandidateResumeById,
} from "@/hooks/staff/candidates.hooks";
import { formatDateToLocal } from "@/utils/dateUtils";
import { getErrorMessage } from "@/utils/error";
import { useDebounce } from "@/hooks/useDebounce";
import { useStore } from "@/store";
import { authService } from "@/services/authService";
import {
  Search,
  Filter,
  Users2,
  ChevronLeft,
  ChevronRight,
  MapPin,
  Briefcase,
  DollarSign,
  Clock,
  Mail,
  Phone,
  Upload,
  Download,
  CheckCircle2,
  XCircle,
  AlertCircle,
} from "lucide-react";

const PAGE_SIZE_OPTIONS = [10, 25, 50, 100, 250, 500];

export const RecruiterCandidatesPage = () => {
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(PAGE_SIZE_OPTIONS[1]);
  const [searchQuery, setSearchQuery] = useState("");
  const [skills, setSkills] = useState("");
  const [location, setLocation] = useState("");
  const [minExperience, setMinExperience] = useState("");
  const [maxExperience, setMaxExperience] = useState("");
  const [minExpectedCTC, setMinExpectedCTC] = useState("");
  const [maxExpectedCTC, setMaxExpectedCTC] = useState("");
  const [maxNoticePeriod, setMaxNoticePeriod] = useState("");
  const [degree, setDegree] = useState("");
  const [isOpenToRelocation, setIsOpenToRelocation] = useState<string>("all");
  const [showFilters, setShowFilters] = useState(false);
  const [selectedCandidateId, setSelectedCandidateId] = useState<string | null>(
    null
  );
  const [profileDialogOpen, setProfileDialogOpen] = useState(false);
  const [overrideDialogOpen, setOverrideDialogOpen] = useState(false);
  const [overrideCandidate, setOverrideCandidate] = useState<{
    id: string;
    name: string;
    currentOverride: boolean;
    expiresAt?: string;
  } | null>(null);

  // Bulk import states
  const [showBulkImport, setShowBulkImport] = useState(false);
  const [uploadFile, setUploadFile] = useState<File | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [uploadResults, setUploadResults] = useState<{
    success: number;
    failed: number;
    errors: string[];
  } | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Export states
  const [selectedCandidates, setSelectedCandidates] = useState<string[]>([]);
  const [isExporting, setIsExporting] = useState(false);

  // Get user roles from store
  const userRoles = useStore((state) => state.auth.roles);

  const parseNumber = (value: string) => {
    if (!value.trim()) return undefined;
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : undefined;
  };

  const searchParams = useMemo(() => {
    const params: {
      Query?: string;
      Skills?: string;
      Location?: string;
      MinExperience?: number;
      MaxExperience?: number;
      MinExpectedCTC?: number;
      MaxExpectedCTC?: number;
      MaxNoticePeriod?: number;
      IsOpenToRelocation?: boolean;
      Degree?: string;
      PageNumber: number;
      PageSize: number;
    } = {
      PageNumber: pageNumber,
      PageSize: pageSize,
    };

    if (searchQuery.trim()) params.Query = searchQuery.trim();
    if (skills.trim()) params.Skills = skills.trim();
    if (location.trim()) params.Location = location.trim();

    const minExp = parseNumber(minExperience);
    if (minExp !== undefined) params.MinExperience = minExp;

    const maxExp = parseNumber(maxExperience);
    if (maxExp !== undefined) params.MaxExperience = maxExp;

    const minCTC = parseNumber(minExpectedCTC);
    if (minCTC !== undefined) params.MinExpectedCTC = minCTC;

    const maxCTC = parseNumber(maxExpectedCTC);
    if (maxCTC !== undefined) params.MaxExpectedCTC = maxCTC;

    const maxNotice = parseNumber(maxNoticePeriod);
    if (maxNotice !== undefined) params.MaxNoticePeriod = maxNotice;

    if (degree.trim()) params.Degree = degree.trim();

    if (isOpenToRelocation !== "all") {
      params.IsOpenToRelocation = isOpenToRelocation === "yes";
    }

    return params;
  }, [
    searchQuery,
    skills,
    location,
    minExperience,
    maxExperience,
    minExpectedCTC,
    maxExpectedCTC,
    maxNoticePeriod,
    degree,
    isOpenToRelocation,
    pageNumber,
    pageSize,
  ]);

  const debouncedSearchParams = useDebounce(searchParams, 400);
  const candidateSearch = useCandidateSearch(debouncedSearchParams);
  const candidateProfileQuery = useCandidateProfileById(
    selectedCandidateId || "",
    { enabled: !!selectedCandidateId && profileDialogOpen }
  );
  const candidateResumeQuery = useCandidateResumeById(
    selectedCandidateId || "",
    { enabled: !!selectedCandidateId && profileDialogOpen }
  );
  const setApplicationOverride = useSetApplicationOverride();

  const candidates = candidateSearch.data?.items ?? [];
  const totalCount = candidateSearch.data?.totalCount ?? 0;
  const totalPages = candidateSearch.data?.totalPages ?? 1;
  const hasNextPage = candidateSearch.data?.hasNextPage ?? false;
  const hasPreviousPage = candidateSearch.data?.hasPreviousPage ?? false;

  const canResetFilters =
    searchQuery.trim() !== "" ||
    skills.trim() !== "" ||
    location.trim() !== "" ||
    minExperience.trim() !== "" ||
    maxExperience.trim() !== "" ||
    minExpectedCTC.trim() !== "" ||
    maxExpectedCTC.trim() !== "" ||
    maxNoticePeriod.trim() !== "" ||
    degree.trim() !== "" ||
    isOpenToRelocation !== "all";

  // Capture total count from first successful query
  const totalCaptured = useRef(false);
  const [totalCandidatesCount, setTotalCandidatesCount] = useState(0);

  useEffect(() => {
    if (candidateSearch.isSuccess && !totalCaptured.current) {
      setTotalCandidatesCount(candidateSearch.data?.totalCount ?? 0);
      totalCaptured.current = true;
    }
  }, [candidateSearch.isSuccess]);

  const handleResetFilters = () => {
    setSearchQuery("");
    setSkills("");
    setLocation("");
    setMinExperience("");
    setMaxExperience("");
    setMinExpectedCTC("");
    setMaxExpectedCTC("");
    setMaxNoticePeriod("");
    setDegree("");
    setIsOpenToRelocation("all");
    setPageNumber(1);
  };

  const handleViewProfile = (candidateId: string) => {
    setSelectedCandidateId(candidateId);
    setProfileDialogOpen(true);
  };

  const handleCloseProfile = () => {
    setProfileDialogOpen(false);
    setSelectedCandidateId(null);
  };

  const handleOpenOverrideDialog = (candidate: any) => {
    setOverrideCandidate({
      id: candidate.id,
      name: `${candidate.firstName} ${candidate.lastName}`,
      currentOverride: candidate.canBypassApplicationLimits || false,
      expiresAt: candidate.overrideExpiresAt,
    });
    setOverrideDialogOpen(true);
  };

  const handleCloseOverrideDialog = () => {
    setOverrideDialogOpen(false);
    setOverrideCandidate(null);
  };

  const handleSetApplicationOverride = async (
    canBypass: boolean,
    expiresAt?: Date
  ) => {
    if (!overrideCandidate) return;

    try {
      await setApplicationOverride.mutateAsync({
        candidateProfileId: overrideCandidate.id,
        payload: {
          canBypassApplicationLimits: canBypass,
          overrideExpiresAt: expiresAt?.toISOString(),
        },
      });
      handleCloseOverrideDialog();
    } catch (error) {
      console.error("Failed to set application override:", error);
    }
  };

  // Check if user can bulk import (SuperAdmin, Admin, HR only)
  const canBulkImport = userRoles.some((role) =>
    ["SuperAdmin", "Admin", "HR"].includes(role)
  );

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setUploadFile(file);
      setUploadResults(null);
    }
  };

  const handleBulkUpload = async () => {
    if (!uploadFile) return;

    setIsUploading(true);
    setUploadResults(null);

    try {
      const response = await authService.bulkRegisterCandidates(uploadFile);

      if (response.success && response.data) {
        const successCount = response.data.filter((r) => r.user != null).length;
        const failedCount = response.data.length - successCount;
        const errors = response.data
          .filter((r) => r.user == null)
          .map((r) => r.message || "Unknown error");

        setUploadResults({
          success: successCount,
          failed: failedCount,
          errors,
        });

        // Refresh candidate list if any succeeded
        if (successCount > 0) {
          candidateSearch.refetch();
        }
      } else {
        setUploadResults({
          success: 0,
          failed: 0,
          errors: [response.message || "Upload failed"],
        });
      }
    } catch (error) {
      setUploadResults({
        success: 0,
        failed: 0,
        errors: [getErrorMessage(error)],
      });
    } finally {
      setIsUploading(false);
    }
  };

  const handleResetUpload = () => {
    setUploadFile(null);
    setUploadResults(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  };

  const handleExportCandidates = async () => {
    if (selectedCandidates.length === 0) return;

    setIsExporting(true);
    try {
      const selectedData = candidates.filter((candidate) =>
        selectedCandidates.includes(candidate.id!)
      );

      const exportData = selectedData.map((candidate) => ({
        "First Name": candidate.firstName,
        "Last Name": candidate.lastName,
        Email: candidate.email,
        Phone: candidate.phoneNumber || "",
        "Current Location": candidate.currentLocation || "",
        "Total Experience (Years)": candidate.totalExperience || "",
        "Expected CTC": candidate.expectedCTC || "",
        "Notice Period (Days)": candidate.noticePeriod || "",
        College: candidate.college || "",
        Degree: candidate.degree || "",
        "Graduation Year": candidate.graduationYear || "",
        "Open to Relocation": candidate.isOpenToRelocation ? "Yes" : "No",
        LinkedIn: candidate.linkedInProfile || "",
        GitHub: candidate.gitHubProfile || "",
        Portfolio: candidate.portfolioUrl || "",
        Source: candidate.source || "",
      }));

      const wb = XLSX.utils.book_new();
      const ws = XLSX.utils.json_to_sheet(exportData);

      const colWidths = [
        { wch: 15 }, // First Name
        { wch: 15 }, // Last Name
        { wch: 30 }, // Email
        { wch: 15 }, // Phone
        { wch: 20 }, // Location
        { wch: 20 }, // Experience
        { wch: 15 }, // CTC
        { wch: 20 }, // Notice Period
        { wch: 25 }, // College
        { wch: 20 }, // Degree
        { wch: 15 }, // Graduation Year
        { wch: 15 }, // Relocation
        { wch: 35 }, // LinkedIn
        { wch: 35 }, // GitHub
        { wch: 35 }, // Portfolio
        { wch: 15 }, // Source
      ];
      ws["!cols"] = colWidths;

      // Add worksheet to workbook
      XLSX.utils.book_append_sheet(wb, ws, "Candidates");

      const timestamp = new Date()
        .toLocaleDateString("en-IN")
        .replaceAll("/", "-");
      const filename = `candidates_export_${timestamp}.xlsx`;

      // Save file
      XLSX.writeFile(wb, filename);
    } catch (error) {
      console.error("Export failed:", error);
    } finally {
      setIsExporting(false);
    }
  };

  const handleSelectCandidate = (candidateId: string, checked: boolean) => {
    if (checked) {
      setSelectedCandidates((prev) => [...prev, candidateId]);
    } else {
      setSelectedCandidates((prev) => prev.filter((id) => id !== candidateId));
    }
  };

  const handleSelectAllCandidates = (checked: boolean) => {
    if (checked) {
      setSelectedCandidates(candidates.map((c) => c.id!));
    } else {
      setSelectedCandidates([]);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="space-y-1">
          <h1 className="text-3xl font-semibold text-foreground">
            Candidate Pool
          </h1>
          <p className="text-muted-foreground">
            Search and manage talent database
          </p>
        </div>
        {canBulkImport && (
          <Button onClick={() => setShowBulkImport(!showBulkImport)}>
            <Upload className="mr-2 h-4 w-4" />
            {showBulkImport ? "Hide" : "Show"} Bulk Import
          </Button>
        )}
      </div>

      {/* Stats */}
      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="pb-2">
            <div className="flex items-center justify-between">
              <p className="text-sm text-muted-foreground">Total Candidates</p>
              <Users2 className="h-4 w-4 text-muted-foreground" />
            </div>
            <CardTitle className="text-3xl">
              {candidateSearch.isLoading ? (
                <LoadingSpinner />
              ) : (
                totalCandidatesCount
              )}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-xs text-muted-foreground">In talent pool</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <div className="flex items-center justify-between">
              <p className="text-sm text-muted-foreground">Current Page</p>
              <Filter className="h-4 w-4 text-muted-foreground" />
            </div>
            <CardTitle className="text-3xl">{candidates.length}</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-xs text-muted-foreground">
              Page {pageNumber} of {totalPages}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <div className="flex items-center justify-between">
              <p className="text-sm text-muted-foreground">Active Filters</p>
              <Filter className="h-4 w-4 text-muted-foreground" />
            </div>
            <CardTitle className="text-3xl">
              {canResetFilters
                ? Object.keys(debouncedSearchParams).length - 2
                : 0}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <Button
              variant="link"
              className="h-auto p-0 text-xs"
              onClick={handleResetFilters}
              disabled={!canResetFilters}
            >
              Reset filters
            </Button>
          </CardContent>
        </Card>
      </div>

      {/* Search and Filters */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle className="text-base font-semibold">
              Search & Filter
            </CardTitle>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setShowFilters(!showFilters)}
            >
              <Filter className="mr-2 h-4 w-4" />
              {showFilters ? "Hide" : "Show"} Filters
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {/* Primary Search */}
            <div className="space-y-2">
              <Label htmlFor="search">Search</Label>
              <div className="relative">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  id="search"
                  placeholder="Name, email, location, degree, college..."
                  value={searchQuery}
                  onChange={(e) => {
                    setSearchQuery(e.target.value);
                    setPageNumber(1);
                  }}
                  className="pl-9"
                />
              </div>
            </div>

            {/* Advanced Filters */}
            {showFilters && (
              <div className="grid gap-4 md:grid-cols-3">
                <div className="space-y-2">
                  <Label htmlFor="skills">Skills (comma-separated)</Label>
                  <Input
                    id="skills"
                    placeholder="React, Python, AWS..."
                    value={skills}
                    onChange={(e) => {
                      setSkills(e.target.value);
                      setPageNumber(1);
                    }}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="location">Location</Label>
                  <Input
                    id="location"
                    placeholder="City or region..."
                    value={location}
                    onChange={(e) => {
                      setLocation(e.target.value);
                      setPageNumber(1);
                    }}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="degree">Degree</Label>
                  <Input
                    id="degree"
                    placeholder="e.g. B.Tech, MBA..."
                    value={degree}
                    onChange={(e) => {
                      setDegree(e.target.value);
                      setPageNumber(1);
                    }}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="minExperience">Min Experience (years)</Label>
                  <Input
                    id="minExperience"
                    type="number"
                    min="0"
                    placeholder="0"
                    value={minExperience}
                    onChange={(e) => {
                      setMinExperience(e.target.value);
                      setPageNumber(1);
                    }}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="maxExperience">Max Experience (years)</Label>
                  <Input
                    id="maxExperience"
                    type="number"
                    min="0"
                    placeholder="50"
                    value={maxExperience}
                    onChange={(e) => {
                      setMaxExperience(e.target.value);
                      setPageNumber(1);
                    }}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="maxNoticePeriod">
                    Max Notice Period (days)
                  </Label>
                  <Input
                    id="maxNoticePeriod"
                    type="number"
                    min="0"
                    placeholder="90"
                    value={maxNoticePeriod}
                    onChange={(e) => {
                      setMaxNoticePeriod(e.target.value);
                      setPageNumber(1);
                    }}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="minExpectedCTC">Min Expected CTC</Label>
                  <Input
                    id="minExpectedCTC"
                    type="number"
                    min="0"
                    placeholder="0"
                    value={minExpectedCTC}
                    onChange={(e) => {
                      setMinExpectedCTC(e.target.value);
                      setPageNumber(1);
                    }}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="maxExpectedCTC">Max Expected CTC</Label>
                  <Input
                    id="maxExpectedCTC"
                    type="number"
                    min="0"
                    placeholder="10000000"
                    value={maxExpectedCTC}
                    onChange={(e) => {
                      setMaxExpectedCTC(e.target.value);
                      setPageNumber(1);
                    }}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="relocation">Open to Relocation</Label>
                  <Select
                    value={isOpenToRelocation}
                    onValueChange={(value) => {
                      setIsOpenToRelocation(value);
                      setPageNumber(1);
                    }}
                  >
                    <SelectTrigger id="relocation">
                      <SelectValue placeholder="Any" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all">Any</SelectItem>
                      <SelectItem value="yes">Yes</SelectItem>
                      <SelectItem value="no">No</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Bulk Import Section */}
      {canBulkImport && showBulkImport && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base font-semibold">
              Bulk Import Candidates
            </CardTitle>
            <CardDescription>
              Upload an Excel file to register multiple candidates at once
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <Alert>
                <AlertCircle className="h-4 w-4" />
                <AlertTitle>Column Order (Important)</AlertTitle>
                <AlertDescription>
                  <div className="space-y-2 mt-2">
                    <p>
                      Columns must be in this exact order (case insensitive):
                    </p>
                    <ol className="list-decimal list-inside text-xs space-y-1 font-mono">
                      <li>Email (required - must be unique)</li>
                      <li>FirstName (required)</li>
                      <li>LastName (required)</li>
                      <li>PhoneNumber (optional)</li>
                      <li>Password (optional - auto-generated if empty)</li>
                    </ol>
                    <p className="text-xs mt-2">
                      Passwords are auto-generated if not provided. Each
                      candidate will receive a welcome email with their login
                      credentials.
                    </p>
                  </div>
                </AlertDescription>
              </Alert>

              <div className="space-y-2">
                <Label htmlFor="bulk-file">Select Excel File</Label>
                <Input
                  id="bulk-file"
                  ref={fileInputRef}
                  type="file"
                  accept=".xlsx,.xls"
                  onChange={handleFileChange}
                  disabled={isUploading}
                />
                {uploadFile && (
                  <p className="text-sm text-muted-foreground">
                    Selected: {uploadFile.name} (
                    {(uploadFile.size / 1024).toFixed(2)} KB)
                  </p>
                )}
              </div>

              {uploadResults && (
                <div className="space-y-2">
                  {uploadResults.success > 0 && (
                    <Alert className="border-green-200 bg-green-50">
                      <CheckCircle2 className="h-4 w-4 text-green-600" />
                      <AlertTitle className="text-green-800">
                        Success
                      </AlertTitle>
                      <AlertDescription className="text-green-700">
                        {uploadResults.success} candidate
                        {uploadResults.success !== 1 ? "s" : ""} registered
                        successfully
                      </AlertDescription>
                    </Alert>
                  )}
                  {uploadResults.failed > 0 && (
                    <Alert variant="destructive">
                      <XCircle className="h-4 w-4" />
                      <AlertTitle>Failed</AlertTitle>
                      <AlertDescription>
                        {uploadResults.failed} candidate
                        {uploadResults.failed !== 1 ? "s" : ""} failed to
                        register
                        {uploadResults.errors.length > 0 && (
                          <ul className="mt-2 list-disc list-inside text-xs space-y-1">
                            {uploadResults.errors
                              .slice(0, 5)
                              .map((error, idx) => (
                                <li key={idx}>{error}</li>
                              ))}
                            {uploadResults.errors.length > 5 && (
                              <li>
                                ... and {uploadResults.errors.length - 5} more
                                errors
                              </li>
                            )}
                          </ul>
                        )}
                      </AlertDescription>
                    </Alert>
                  )}
                  {uploadResults.errors.length > 0 &&
                    uploadResults.success === 0 &&
                    uploadResults.failed === 0 && (
                      <Alert variant="destructive">
                        <XCircle className="h-4 w-4" />
                        <AlertTitle>Error</AlertTitle>
                        <AlertDescription>
                          {uploadResults.errors[0]}
                        </AlertDescription>
                      </Alert>
                    )}
                </div>
              )}

              <div className="flex gap-2 justify-end pt-4">
                {!uploadResults && (
                  <>
                    <Button
                      variant="outline"
                      onClick={handleResetUpload}
                      disabled={isUploading || !uploadFile}
                    >
                      Clear
                    </Button>
                    <Button
                      onClick={handleBulkUpload}
                      disabled={isUploading || !uploadFile}
                    >
                      {isUploading ? (
                        <>
                          <LoadingSpinner />
                          Getting Accounts Ready...
                        </>
                      ) : (
                        <>
                          <Upload className="mr-2 h-4 w-4" />
                          Upload and Register
                        </>
                      )}
                    </Button>
                  </>
                )}
                {uploadResults && (
                  <Button
                    variant="outline"
                    onClick={() => {
                      setShowBulkImport(false);
                      handleResetUpload();
                    }}
                  >
                    Close
                  </Button>
                )}
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Bulk Actions Toolbar */}
      {selectedCandidates.length > 0 && (
        <Card>
          <CardContent className="py-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <span className="text-sm font-medium">
                  {selectedCandidates.length} candidate
                  {selectedCandidates.length !== 1 ? "s" : ""} selected
                </span>
              </div>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleExportCandidates}
                  disabled={isExporting}
                >
                  {isExporting ? (
                    <>
                      <LoadingSpinner />
                      Exporting...
                    </>
                  ) : (
                    <>
                      <Download className="mr-2 h-4 w-4" />
                      Export to Excel
                    </>
                  )}
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => setSelectedCandidates([])}
                >
                  Clear Selection
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Results */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="text-base font-semibold">
                Candidates
              </CardTitle>
              <CardDescription>
                {candidateSearch.isLoading
                  ? "Searching..."
                  : `${totalCount} candidate${
                      totalCount !== 1 ? "s" : ""
                    } found`}
              </CardDescription>
            </div>
            <div className="flex items-center gap-2">
              <Label htmlFor="pageSize" className="text-xs">
                Per page:
              </Label>
              <Select
                value={String(pageSize)}
                onValueChange={(value) => {
                  setPageSize(Number(value));
                  setPageNumber(1);
                }}
              >
                <SelectTrigger id="pageSize" className="w-20">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {PAGE_SIZE_OPTIONS.map((size) => (
                    <SelectItem key={size} value={String(size)}>
                      {size}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {candidateSearch.isLoading && (
            <div className="flex justify-center py-12">
              <LoadingSpinner />
            </div>
          )}

          {candidateSearch.isError && (
            <p className="rounded-md border border-destructive/40 bg-destructive/5 p-4 text-sm text-destructive">
              {getErrorMessage(candidateSearch.error)}
            </p>
          )}

          {!candidateSearch.isLoading &&
            !candidateSearch.isError &&
            candidates.length === 0 && (
              <div className="py-12 text-center">
                <Users2 className="mx-auto h-12 w-12 text-muted-foreground/50" />
                <p className="mt-4 text-sm text-muted-foreground">
                  No candidates found matching your criteria.
                </p>
                <Button
                  variant="outline"
                  className="mt-4"
                  onClick={handleResetFilters}
                  disabled={!canResetFilters}
                >
                  Reset Filters
                </Button>
              </div>
            )}

          {!candidateSearch.isLoading &&
            !candidateSearch.isError &&
            candidates.length > 0 && (
              <div className="overflow-x-auto">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead className="w-12">
                        <Checkbox
                          checked={
                            selectedCandidates.length === candidates.length &&
                            candidates.length > 0
                          }
                          onCheckedChange={(checked) =>
                            handleSelectAllCandidates(checked as boolean)
                          }
                        />
                      </TableHead>
                      <TableHead>Candidate</TableHead>
                      <TableHead>Location</TableHead>
                      <TableHead>Experience</TableHead>
                      <TableHead>Expected CTC</TableHead>
                      <TableHead>Notice Period</TableHead>
                      <TableHead>Skills</TableHead>
                      <TableHead>Relocation</TableHead>
                      <TableHead className="text-left">Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {candidates.map((candidate) => (
                      <TableRow key={candidate.id}>
                        <TableCell>
                          <Checkbox
                            checked={selectedCandidates.includes(candidate.id!)}
                            onCheckedChange={(checked) =>
                              handleSelectCandidate(
                                candidate.id!,
                                checked as boolean
                              )
                            }
                          />
                        </TableCell>
                        <TableCell>
                          <div>
                            <p className="font-medium">
                              {candidate.firstName} {candidate.lastName}
                            </p>
                            <div className="flex items-center gap-2 text-xs text-muted-foreground">
                              <Mail className="h-3 w-3" />
                              {candidate.email}
                            </div>
                            {candidate.phoneNumber && (
                              <div className="flex items-center gap-2 text-xs text-muted-foreground">
                                <Phone className="h-3 w-3" />
                                {candidate.phoneNumber}
                              </div>
                            )}
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-2">
                            <MapPin className="h-3 w-3 text-muted-foreground" />
                            <span className="text-sm">
                              {candidate.currentLocation || "—"}
                            </span>
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-2">
                            <Briefcase className="h-3 w-3 text-muted-foreground" />
                            <span className="text-sm">
                              {candidate.totalExperience
                                ? `${candidate.totalExperience} yrs`
                                : "—"}
                            </span>
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-2">
                            <DollarSign className="h-3 w-3 text-muted-foreground" />
                            <span className="text-sm">
                              {candidate.expectedCTC
                                ? `${candidate.expectedCTC.toLocaleString()}`
                                : "—"}
                            </span>
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-2">
                            <Clock className="h-3 w-3 text-muted-foreground" />
                            <span className="text-sm">
                              {candidate.noticePeriod
                                ? `${candidate.noticePeriod} days`
                                : "—"}
                            </span>
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex flex-wrap gap-1">
                            {candidate.skills?.slice(0, 3).map((skill, idx) => (
                              <Badge
                                key={idx}
                                variant="outline"
                                className="text-xs"
                              >
                                {skill}
                              </Badge>
                            ))}
                            {(candidate.skills?.length || 0) > 3 && (
                              <Badge variant="outline" className="text-xs">
                                +{(candidate.skills?.length || 0) - 3}
                              </Badge>
                            )}
                          </div>
                        </TableCell>
                        <TableCell>
                          {candidate.isOpenToRelocation ? (
                            <Badge variant="default" className="text-xs">
                              Yes
                            </Badge>
                          ) : (
                            <Badge variant="secondary" className="text-xs">
                              No
                            </Badge>
                          )}
                        </TableCell>
                        <TableCell>
                          <div className="flex gap-2">
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handleViewProfile(candidate.id!)}
                            >
                              View Profile
                            </Button>
                            {userRoles.some((role) =>
                              ["SuperAdmin", "Admin", "HR"].includes(role)
                            ) && (
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() =>
                                  handleOpenOverrideDialog(candidate)
                                }
                              >
                                Override Limits
                              </Button>
                            )}
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            )}

          {/* Pagination */}
          {!candidateSearch.isLoading &&
            !candidateSearch.isError &&
            totalCount > 0 && (
              <div className="mt-4 flex items-center justify-between">
                <p className="text-sm text-muted-foreground">
                  Showing {(pageNumber - 1) * pageSize + 1} to{" "}
                  {Math.min(pageNumber * pageSize, totalCount)} of {totalCount}{" "}
                  results
                </p>
                <div className="flex items-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
                    disabled={!hasPreviousPage}
                  >
                    <ChevronLeft className="h-4 w-4" />
                    Previous
                  </Button>
                  <span className="text-sm">
                    Page {pageNumber} of {totalPages}
                  </span>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setPageNumber((p) => p + 1)}
                    disabled={!hasNextPage}
                  >
                    Next
                    <ChevronRight className="h-4 w-4" />
                  </Button>
                </div>
              </div>
            )}
        </CardContent>
      </Card>

      {/* Profile Dialog */}
      <Dialog open={profileDialogOpen} onOpenChange={handleCloseProfile}>
        <DialogContent className="max-w-3xl max-h-[80vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Candidate Profile</DialogTitle>
            <DialogDescription>
              Detailed information about the candidate
            </DialogDescription>
          </DialogHeader>
          {candidateProfileQuery.isLoading && (
            <div className="flex justify-center py-8">
              <LoadingSpinner />
            </div>
          )}
          {candidateProfileQuery.isError && (
            <p className="text-sm text-destructive">
              {getErrorMessage(candidateProfileQuery.error)}
            </p>
          )}
          {candidateProfileQuery.data && (
            <div className="space-y-4">
              <div className="grid gap-4 md:grid-cols-2">
                <div>
                  <Label>Name</Label>
                  <p className="text-sm">
                    {candidateProfileQuery.data.firstName}{" "}
                    {candidateProfileQuery.data.lastName}
                  </p>
                </div>
                <div>
                  <Label>Email</Label>
                  <p className="text-sm">{candidateProfileQuery.data.email}</p>
                </div>
                <div>
                  <Label>Phone</Label>
                  <p className="text-sm">
                    {candidateProfileQuery.data.phoneNumber || "—"}
                  </p>
                </div>
                <div>
                  <Label>Location</Label>
                  <p className="text-sm">
                    {candidateProfileQuery.data.currentLocation || "—"}
                  </p>
                </div>
                <div>
                  <Label>Experience</Label>
                  <p className="text-sm">
                    {candidateProfileQuery.data.totalExperience
                      ? `${candidateProfileQuery.data.totalExperience} years`
                      : "—"}
                  </p>
                </div>
                <div>
                  <Label>Expected CTC</Label>
                  <p className="text-sm">
                    {candidateProfileQuery.data.expectedCTC
                      ? `₹${candidateProfileQuery.data.expectedCTC.toLocaleString()}`
                      : "—"}
                  </p>
                </div>
                <div>
                  <Label>Notice Period</Label>
                  <p className="text-sm">
                    {candidateProfileQuery.data.noticePeriod
                      ? `${candidateProfileQuery.data.noticePeriod} days`
                      : "—"}
                  </p>
                </div>
                <div>
                  <Label>Current CTC</Label>
                  <p className="text-sm">
                    {candidateProfileQuery.data.currentCTC
                      ? `₹${candidateProfileQuery.data.currentCTC.toLocaleString()}`
                      : "—"}
                  </p>
                </div>
                <div>
                  <Label>College</Label>
                  <p className="text-sm">
                    {candidateProfileQuery.data.college || "—"}
                  </p>
                </div>
                <div>
                  <Label>Graduation Year</Label>
                  <p className="text-sm">
                    {candidateProfileQuery.data.graduationYear || "—"}
                  </p>
                </div>
                <div>
                  <Label>Open to Relocation</Label>
                  <p className="text-sm">
                    {candidateProfileQuery.data.isOpenToRelocation
                      ? "Yes"
                      : "No"}
                  </p>
                </div>
              </div>

              {/* Additional Information */}
              <div className="grid gap-4 md:grid-cols-2">
                {candidateProfileQuery.data.resumeFileName && (
                  <div>
                    <Label>Resume</Label>
                    <div className="mt-2">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => {
                          if (candidateResumeQuery.data) {
                            window.open(candidateResumeQuery.data, "_blank");
                          }
                        }}
                        disabled={candidateResumeQuery.isLoading}
                      >
                        {candidateResumeQuery.isLoading ? (
                          <LoadingSpinner />
                        ) : (
                          <Download className="mr-2 h-4 w-4" />
                        )}
                        Download Resume
                      </Button>
                      {candidateResumeQuery.isError && (
                        <p className="text-xs text-destructive mt-1">
                          Failed to load resume URL
                        </p>
                      )}
                    </div>
                  </div>
                )}

                {(candidateProfileQuery.data.linkedInProfile ||
                  candidateProfileQuery.data.gitHubProfile ||
                  candidateProfileQuery.data.portfolioUrl) && (
                  <div>
                    <Label>Links</Label>
                    <div className="mt-2 space-y-1">
                      {candidateProfileQuery.data.linkedInProfile && (
                        <a
                          href={candidateProfileQuery.data.linkedInProfile}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-sm text-blue-600 hover:underline block"
                        >
                          LinkedIn Profile
                        </a>
                      )}
                      {candidateProfileQuery.data.gitHubProfile && (
                        <a
                          href={candidateProfileQuery.data.gitHubProfile}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-sm text-blue-600 hover:underline block"
                        >
                          GitHub Profile
                        </a>
                      )}
                      {candidateProfileQuery.data.portfolioUrl && (
                        <a
                          href={candidateProfileQuery.data.portfolioUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-sm text-blue-600 hover:underline block"
                        >
                          Portfolio
                        </a>
                      )}
                    </div>
                  </div>
                )}

                {candidateProfileQuery.data.source && (
                  <div>
                    <Label>Source</Label>
                    <p className="text-sm">
                      {candidateProfileQuery.data.source}
                    </p>
                  </div>
                )}

                <div>
                  <Label>Profile Created</Label>
                  <p className="text-sm">
                    {candidateProfileQuery.data.createdAt
                      ? formatDateToLocal(candidateProfileQuery.data.createdAt)
                      : "—"}
                  </p>
                </div>
              </div>

              {candidateProfileQuery.data.skills &&
                candidateProfileQuery.data.skills.length > 0 && (
                  <div>
                    <Label>Skills</Label>
                    <div className="mt-2 flex flex-wrap gap-2">
                      {candidateProfileQuery.data.skills.map((skill) => (
                        <Badge key={skill.id} variant="secondary">
                          {skill.skillName}{" "}
                          {skill.proficiencyLevel
                            ? `- Level ${skill.proficiencyLevel}`
                            : ""}
                        </Badge>
                      ))}
                    </div>
                  </div>
                )}

              {candidateProfileQuery.data.education &&
                candidateProfileQuery.data.education.length > 0 && (
                  <div>
                    <Label>Education</Label>
                    <div className="mt-2 space-y-2">
                      {candidateProfileQuery.data.education.map((edu) => (
                        <div
                          key={edu.id}
                          className="rounded-lg border bg-slate-50 p-3"
                        >
                          <p className="font-medium text-sm">
                            {edu.degree} in {edu.fieldOfStudy}
                          </p>
                          <p className="text-xs text-muted-foreground">
                            {edu.institutionName} • {edu.startYear} -{" "}
                            {edu.endYear}
                          </p>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

              {candidateProfileQuery.data.workExperience &&
                candidateProfileQuery.data.workExperience.length > 0 && (
                  <div>
                    <Label>Work Experience</Label>
                    <div className="mt-2 space-y-2">
                      {candidateProfileQuery.data.workExperience.map((exp) => (
                        <div
                          key={exp.id}
                          className="rounded-lg border bg-slate-50 p-3"
                        >
                          <p className="font-medium text-sm">{exp.jobTitle}</p>
                          <p className="text-xs text-muted-foreground">
                            {exp.companyName} •{" "}
                            {formatDateToLocal(exp.startDate)} -{" "}
                            {exp.endDate
                              ? formatDateToLocal(exp.endDate)
                              : "Present"}
                          </p>
                          {exp.jobDescription && (
                            <p className="mt-1 text-xs">{exp.jobDescription}</p>
                          )}
                        </div>
                      ))}
                    </div>
                  </div>
                )}
            </div>
          )}
        </DialogContent>
      </Dialog>

      {/* Application Override Dialog */}
      <Dialog
        open={overrideDialogOpen}
        onOpenChange={handleCloseOverrideDialog}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Application Limit Override</DialogTitle>
            <DialogDescription>
              Grant special application privileges to {overrideCandidate?.name}{" "}
              one use only.
            </DialogDescription>
          </DialogHeader>

          {overrideCandidate && (
            <div className="space-y-4 py-4">
              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <Label htmlFor="can-bypass">Allow new application</Label>
                  <Checkbox
                    id="can-bypass"
                    checked={overrideCandidate.currentOverride}
                    onCheckedChange={(checked) => {
                      setOverrideCandidate({
                        ...overrideCandidate,
                        currentOverride: checked as boolean,
                      });
                    }}
                  />
                </div>

                {overrideCandidate.currentOverride && (
                  <div>
                    <Label htmlFor="expires-at">
                      Override expires at (optional)
                    </Label>
                    <Input
                      id="expires-at"
                      type="datetime-local"
                      value={
                        overrideCandidate.expiresAt
                          ? new Date(overrideCandidate.expiresAt)
                              .toISOString()
                              .slice(0, 16)
                          : ""
                      }
                      onChange={(e) => {
                        setOverrideCandidate({
                          ...overrideCandidate,
                          expiresAt: e.target.value
                            ? new Date(e.target.value).toISOString()
                            : undefined,
                        });
                      }}
                      className="mt-1"
                    />
                    <p className="text-xs text-muted-foreground mt-1">
                      Leave empty for no time limit on override
                    </p>
                  </div>
                )}
              </div>

              {setApplicationOverride.isError && (
                <Alert variant="destructive">
                  <AlertDescription>
                    {getErrorMessage(setApplicationOverride.error)}
                  </AlertDescription>
                </Alert>
              )}
            </div>
          )}

          <DialogFooter>
            <Button
              variant="outline"
              onClick={handleCloseOverrideDialog}
              disabled={setApplicationOverride.isPending}
            >
              Cancel
            </Button>
            <Button
              onClick={() => {
                if (overrideCandidate) {
                  handleSetApplicationOverride(
                    overrideCandidate.currentOverride,
                    overrideCandidate.expiresAt
                      ? new Date(overrideCandidate.expiresAt)
                      : undefined
                  );
                }
              }}
              disabled={setApplicationOverride.isPending}
            >
              {setApplicationOverride.isPending && <LoadingSpinner />}
              Save Override
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};
