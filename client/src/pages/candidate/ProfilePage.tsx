import { useEffect, useMemo, useRef, useState } from "react";
import { isAxiosError } from "axios";
import {
  User,
  Mail,
  Phone,
  MapPin,
  Briefcase,
  Clock,
  Move,
  ExternalLink,
  HatGlasses,
  BookOpen,
} from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import {
  useCandidateProfile,
  useCandidateResumeUrl,
  useCreateCandidateProfile,
  useUpdateCandidateProfile,
  useUploadCandidateResume,
  useDeleteCandidateResume,
  type Schemas,
} from "@/hooks/candidate";
import { formatDateToLocal } from "@/utils/dateUtils";

type CandidateProfile = Schemas["CandidateProfileResponseDto"];

type ProfileFormState = {
  currentLocation: string;
  totalExperience: string;
  currentCTC: string;
  expectedCTC: string;
  noticePeriod: string;
  linkedInProfile: string;
  gitHubProfile: string;
  portfolioUrl: string;
  college: string;
  degree: string;
  graduationYear: string;
  source: string;
  isOpenToRelocation: boolean;
};

const createEmptyFormState = (): ProfileFormState => ({
  currentLocation: "",
  totalExperience: "",
  currentCTC: "",
  expectedCTC: "",
  noticePeriod: "",
  linkedInProfile: "",
  gitHubProfile: "",
  portfolioUrl: "",
  college: "",
  degree: "",
  graduationYear: "",
  source: "",
  isOpenToRelocation: false,
});

const mapProfileToFormState = (
  profile?: CandidateProfile
): ProfileFormState => {
  if (!profile) return createEmptyFormState();
  return {
    currentLocation: profile.currentLocation ?? "",
    totalExperience:
      profile.totalExperience !== undefined && profile.totalExperience !== null
        ? String(profile.totalExperience)
        : "",
    currentCTC:
      profile.currentCTC !== undefined && profile.currentCTC !== null
        ? String(profile.currentCTC)
        : "",
    expectedCTC:
      profile.expectedCTC !== undefined && profile.expectedCTC !== null
        ? String(profile.expectedCTC)
        : "",
    noticePeriod:
      profile.noticePeriod !== undefined && profile.noticePeriod !== null
        ? String(profile.noticePeriod)
        : "",
    linkedInProfile: profile.linkedInProfile ?? "",
    gitHubProfile: profile.gitHubProfile ?? "",
    portfolioUrl: profile.portfolioUrl ?? "",
    college: profile.college ?? "",
    degree: profile.degree ?? "",
    graduationYear:
      profile.graduationYear !== undefined && profile.graduationYear !== null
        ? String(profile.graduationYear)
        : "",
    source: profile.source ?? "",
    isOpenToRelocation: profile.isOpenToRelocation ?? false,
  };
};

const DetailRow = ({
  label,
  value,
}: {
  label: string;
  value?: string | number | null;
}) => {
  const getIcon = (label: string) => {
    switch (label.toLowerCase()) {
      case "name":
        return <User className="h-4 w-4 text-muted-foreground" />;
      case "email":
        return <Mail className="h-4 w-4 text-muted-foreground" />;
      case "phone":
        return <Phone className="h-4 w-4 text-muted-foreground" />;
      case "current location":
        return <MapPin className="h-4 w-4 text-muted-foreground" />;
      case "experience":
        return <Briefcase className="h-4 w-4 text-muted-foreground" />;
      case "notice period":
        return <Clock className="h-4 w-4 text-muted-foreground" />;
      case "relocation":
        return <Move className="h-4 w-4 text-muted-foreground" />;
      case "linkedin":
      case "github":
      case "portfolio":
        return <ExternalLink className="h-4 w-4 text-muted-foreground" />;
      case "source":
        return <HatGlasses className="h-4 w-4 text-muted-foreground" />;
      case "education":
        return <BookOpen className="h-4 w-4 text-muted-foreground" />;
      default:
        return null;
    }
  };

  return (
    <div className="flex items-start gap-3 rounded-lg border bg-muted/20 p-4 transition-colors hover:bg-muted/30">
      {getIcon(label)}
      <div className="flex-1">
        <p className="text-xs uppercase tracking-wide text-muted-foreground font-medium">
          {label}
        </p>
        <p className="text-sm font-medium text-foreground mt-1">
          {value && value !== "" ? value : "—"}
        </p>
      </div>
    </div>
  );
};

const toNumber = (value: string) => {
  if (!value.trim()) return undefined;
  const parsed = Number(value);
  return Number.isNaN(parsed) ? undefined : parsed;
};

export const CandidateProfilePage = () => {
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [formState, setFormState] = useState<ProfileFormState>(
    createEmptyFormState()
  );
  const [formMessage, setFormMessage] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [resumeMessage, setResumeMessage] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const {
    data: profile,
    isLoading: profileLoading,
    error: profileError,
  } = useCandidateProfile();
  const resumeQuery = useCandidateResumeUrl();
  const createProfile = useCreateCandidateProfile();
  const updateProfile = useUpdateCandidateProfile();
  const uploadResume = useUploadCandidateResume();
  const deleteResume = useDeleteCandidateResume();

  useEffect(() => {
    setFormState(mapProfileToFormState(profile));
  }, [profile]);

  const handleInputChange = (
    field: keyof ProfileFormState,
    value: string | boolean
  ) => {
    setFormState((prev) => ({
      ...prev,
      [field]: value,
    }));
  };

  const buildPayload = (): Schemas["UpdateCandidateProfileDto"] => ({
    currentLocation: formState.currentLocation || undefined,
    totalExperience: toNumber(formState.totalExperience),
    currentCTC: toNumber(formState.currentCTC),
    expectedCTC: toNumber(formState.expectedCTC),
    noticePeriod: toNumber(formState.noticePeriod),
    linkedInProfile: formState.linkedInProfile || undefined,
    gitHubProfile: formState.gitHubProfile || undefined,
    portfolioUrl: formState.portfolioUrl || undefined,
    college: formState.college || undefined,
    degree: formState.degree || undefined,
    graduationYear: toNumber(formState.graduationYear),
    isOpenToRelocation: formState.isOpenToRelocation,
  });

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setFormError(null);
    setFormMessage(null);

    try {
      const payload = buildPayload();
      if (profile?.id) {
        await updateProfile.mutateAsync(payload);
        setFormMessage("Profile updated successfully");
      } else {
        await createProfile.mutateAsync({
          ...payload,
          skills: [],
          education: [],
          workExperience: [],
          source: formState.source || undefined,
        });
        setFormMessage("Profile created successfully");
      }
      setIsDialogOpen(false);
    } catch (error) {
      let message = "Failed to save profile";

      if (isAxiosError(error)) {
        const payload = error.response?.data;
        if (payload) {
          const detailedMessage =
            payload.errors?.filter(Boolean).join(", ") ?? payload.message;
          if (detailedMessage) {
            message = detailedMessage;
          }
        }
      } else if (error instanceof Error) {
        message = error.message;
      }

      setFormError(message);
    }
  };

  const handleResumeUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setResumeMessage(null);
    uploadResume.mutate(file, {
      onSuccess: () => {
        setResumeMessage("Resume uploaded successfully");
      },
      onError: (error) => {
        let message = "Failed to upload resume";

        if (isAxiosError(error)) {
          const payload = error.response?.data;
          if (payload) {
            const detailedMessage =
              payload.errors?.filter(Boolean).join(", ") ?? payload.message;
            if (detailedMessage) {
              message = detailedMessage;
            }
          }
        } else if (error instanceof Error) {
          message = error.message;
        }

        setResumeMessage(message);
      },
      onSettled: () => {
        if (fileInputRef.current) {
          fileInputRef.current.value = "";
        }
      },
    });
  };

  const handleResumeDelete = () => {
    setResumeMessage(null);
    deleteResume.mutate(undefined, {
      onSuccess: () => setResumeMessage("Resume removed"),
      onError: (error) => {
        let message = "Failed to delete resume";

        if (isAxiosError(error)) {
          const payload = error.response?.data;
          if (payload) {
            const detailedMessage =
              payload.errors?.filter(Boolean).join(", ") ?? payload.message;
            if (detailedMessage) {
              message = detailedMessage;
            }
          }
        } else if (error instanceof Error) {
          message = error.message;
        }

        setResumeMessage(message);
      },
    });
  };

  const isSaving = createProfile.isPending || updateProfile.isPending;
  const resumeUrl = resumeQuery.data ?? null;

  const profileSummary = useMemo(() => {
    if (!profile) return [];

    return [
      {
        label: "Name",
        value:
          `${profile.firstName ?? ""} ${profile.lastName ?? ""}`.trim() || "—",
      },
      { label: "Email", value: profile.email ?? "—" },
      { label: "Phone", value: profile.phoneNumber ?? "—" },
      { label: "Current location", value: profile.currentLocation ?? "—" },
      {
        label: "Experience",
        value:
          profile.totalExperience !== undefined &&
          profile.totalExperience !== null
            ? `${profile.totalExperience} yrs`
            : "—",
      },
      {
        label: "Notice period",
        value:
          profile.noticePeriod !== undefined && profile.noticePeriod !== null
            ? `${profile.noticePeriod} days`
            : "—",
      },
      {
        label: "Relocation",
        value: profile.isOpenToRelocation ? "Open" : "Not open",
      },
      { label: "Source", value: profile.source ?? "—" },
    ];
  }, [profile]);

  const renderResumeContent = () => {
    if (!profile) {
      return (
        <p className="text-sm text-muted-foreground">
          Create your profile to upload and manage your resume.
        </p>
      );
    }

    if (resumeQuery.isLoading) {
      return <LoadingSpinner />;
    }

    if (resumeUrl) {
      return (
        <div className="flex flex-col gap-2 rounded-md border bg-muted/30 p-4 text-sm">
          <span className="font-medium">
            {profile?.resumeFileName ?? "Uploaded resume"}
          </span>
          <div className="flex flex-wrap gap-3 text-xs text-muted-foreground">
            <span>
              Updated{" "}
              {profile?.updatedAt
                ? formatDateToLocal(profile.updatedAt)
                : "recently"}
            </span>
            <a
              className="text-primary underline"
              href={resumeUrl}
              target="_blank"
              rel="noreferrer"
            >
              View resume
            </a>
          </div>
        </div>
      );
    }

    return (
      <p className="text-sm text-muted-foreground">
        No resume on file. Upload a resume to speed up applications.
      </p>
    );
  };

  if (profileLoading) {
    return (
      <div className="rounded-lg border bg-white p-6">
        <LoadingSpinner />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold">Profile</h1>
          <p className="text-muted-foreground">
            Keep your information up to date to receive relevant opportunities.
          </p>
        </div>
        <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
          <DialogTrigger asChild>
            <Button
              variant="outline"
              onClick={() => {
                setFormMessage(null);
                setFormError(null);
              }}
            >
              {profile ? "Edit profile" : "Create profile"}
            </Button>
          </DialogTrigger>
          <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-3xl">
            <DialogHeader>
              <DialogTitle className="text-xl">
                {profile ? "Update your profile" : "Create your profile"}
              </DialogTitle>
              <DialogDescription>
                Share key details that help recruiters find the perfect match
                for you.
              </DialogDescription>
            </DialogHeader>
            <form className="space-y-8" onSubmit={handleSubmit}>
              {/* Professional Details Section */}
              <div className="space-y-4">
                <div className="flex items-center gap-2 border-b pb-2">
                  <div className="h-1 w-8 rounded-full bg-primary" />
                  <h3 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">
                    Professional Details
                  </h3>
                </div>
                <div className="grid gap-4 sm:grid-cols-2">
                  <div className="space-y-2">
                    <Label
                      htmlFor="currentLocation"
                      className="text-sm font-medium"
                    >
                      Current location
                    </Label>
                    <Input
                      id="currentLocation"
                      value={formState.currentLocation}
                      onChange={(event) =>
                        handleInputChange("currentLocation", event.target.value)
                      }
                      placeholder="e.g., Bangalore, India"
                      className="transition-all focus:ring-2"
                    />
                  </div>
                  <div className="space-y-2">
                    <Label
                      htmlFor="totalExperience"
                      className="text-sm font-medium"
                    >
                      Total experience (years)
                    </Label>
                    <Input
                      id="totalExperience"
                      inputMode="decimal"
                      value={formState.totalExperience}
                      onChange={(event) =>
                        handleInputChange("totalExperience", event.target.value)
                      }
                      placeholder="e.g., 5.5"
                      className="transition-all focus:ring-2"
                    />
                  </div>
                  <div className="space-y-2">
                    <Label
                      htmlFor="noticePeriod"
                      className="text-sm font-medium"
                    >
                      Notice period (days)
                    </Label>
                    <Input
                      id="noticePeriod"
                      inputMode="numeric"
                      value={formState.noticePeriod}
                      onChange={(event) =>
                        handleInputChange("noticePeriod", event.target.value)
                      }
                      placeholder="e.g., 30"
                      className="transition-all focus:ring-2"
                    />
                  </div>
                  <div className="flex items-end">
                    <label className="flex items-center gap-3 rounded-lg border border-input bg-muted/30 px-4 py-3 text-sm font-medium transition-colors hover:bg-muted/50">
                      <input
                        type="checkbox"
                        className="h-4 w-4 rounded border-gray-300"
                        checked={formState.isOpenToRelocation}
                        onChange={(event) =>
                          handleInputChange(
                            "isOpenToRelocation",
                            event.target.checked
                          )
                        }
                      />
                      Open to relocation
                    </label>
                  </div>
                </div>
              </div>

              {/* Compensation Section */}
              <div className="space-y-4">
                <div className="flex items-center gap-2 border-b pb-2">
                  <div className="h-1 w-8 rounded-full bg-primary" />
                  <h3 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">
                    Compensation
                  </h3>
                </div>
                <div className="grid gap-4 sm:grid-cols-2">
                  <div className="space-y-2">
                    <Label htmlFor="currentCTC" className="text-sm font-medium">
                      Current CTC (annual)
                    </Label>
                    <Input
                      id="currentCTC"
                      inputMode="decimal"
                      value={formState.currentCTC}
                      onChange={(event) =>
                        handleInputChange("currentCTC", event.target.value)
                      }
                      placeholder="e.g., 1200000"
                      className="transition-all focus:ring-2"
                    />
                    <p className="text-xs text-muted-foreground">
                      In your local currency
                    </p>
                  </div>
                  <div className="space-y-2">
                    <Label
                      htmlFor="expectedCTC"
                      className="text-sm font-medium"
                    >
                      Expected CTC (annual)
                    </Label>
                    <Input
                      id="expectedCTC"
                      inputMode="decimal"
                      value={formState.expectedCTC}
                      onChange={(event) =>
                        handleInputChange("expectedCTC", event.target.value)
                      }
                      placeholder="e.g., 1500000"
                      className="transition-all focus:ring-2"
                    />
                    <p className="text-xs text-muted-foreground">
                      Your target salary
                    </p>
                  </div>
                </div>
              </div>

              {/* Education Section */}
              <div className="space-y-4">
                <div className="flex items-center gap-2 border-b pb-2">
                  <div className="h-1 w-8 rounded-full bg-primary" />
                  <h3 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">
                    Education
                  </h3>
                </div>
                <div className="grid gap-4 sm:grid-cols-2">
                  <div className="space-y-2">
                    <Label htmlFor="degree" className="text-sm font-medium">
                      Degree
                    </Label>
                    <Input
                      id="degree"
                      value={formState.degree}
                      onChange={(event) =>
                        handleInputChange("degree", event.target.value)
                      }
                      placeholder="e.g., B.Tech Computer Science"
                      className="transition-all focus:ring-2"
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="college" className="text-sm font-medium">
                      College / University
                    </Label>
                    <Input
                      id="college"
                      value={formState.college}
                      onChange={(event) =>
                        handleInputChange("college", event.target.value)
                      }
                      placeholder="e.g., IIT Delhi"
                      className="transition-all focus:ring-2"
                    />
                  </div>
                  <div className="space-y-2">
                    <Label
                      htmlFor="graduationYear"
                      className="text-sm font-medium"
                    >
                      Graduation year
                    </Label>
                    <Input
                      id="graduationYear"
                      inputMode="numeric"
                      value={formState.graduationYear}
                      onChange={(event) =>
                        handleInputChange("graduationYear", event.target.value)
                      }
                      placeholder="e.g., 2020"
                      className="transition-all focus:ring-2"
                    />
                  </div>
                </div>
              </div>

              {/* Online Presence Section */}
              <div className="space-y-4">
                <div className="flex items-center gap-2 border-b pb-2">
                  <div className="h-1 w-8 rounded-full bg-primary" />
                  <h3 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">
                    Online Presence
                  </h3>
                </div>
                <div className="grid gap-4 sm:grid-cols-2">
                  <div className="space-y-2">
                    <Label
                      htmlFor="linkedInProfile"
                      className="text-sm font-medium"
                    >
                      LinkedIn profile
                    </Label>
                    <Input
                      id="linkedInProfile"
                      type="url"
                      value={formState.linkedInProfile}
                      onChange={(event) =>
                        handleInputChange("linkedInProfile", event.target.value)
                      }
                      placeholder="https://linkedin.com/in/yourprofile"
                      className="transition-all focus:ring-2"
                    />
                  </div>
                  <div className="space-y-2">
                    <Label
                      htmlFor="gitHubProfile"
                      className="text-sm font-medium"
                    >
                      GitHub profile
                    </Label>
                    <Input
                      id="gitHubProfile"
                      type="url"
                      value={formState.gitHubProfile}
                      onChange={(event) =>
                        handleInputChange("gitHubProfile", event.target.value)
                      }
                      placeholder="https://github.com/yourusername"
                      className="transition-all focus:ring-2"
                    />
                  </div>
                  <div className="space-y-2 sm:col-span-2">
                    <Label
                      htmlFor="portfolioUrl"
                      className="text-sm font-medium"
                    >
                      Portfolio website
                    </Label>
                    <Input
                      id="portfolioUrl"
                      type="url"
                      value={formState.portfolioUrl}
                      onChange={(event) =>
                        handleInputChange("portfolioUrl", event.target.value)
                      }
                      placeholder="https://yourportfolio.com"
                      className="transition-all focus:ring-2"
                    />
                  </div>
                </div>
              </div>

              {/* Additional Info Section */}
              {!profile && (
                <div className="space-y-4">
                  <div className="flex items-center gap-2 border-b pb-2">
                    <div className="h-1 w-8 rounded-full bg-primary" />
                    <h3 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">
                      Additional Information
                    </h3>
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="source" className="text-sm font-medium">
                      How did you hear about us?
                    </Label>
                    <Input
                      id="source"
                      value={formState.source}
                      onChange={(event) =>
                        handleInputChange("source", event.target.value)
                      }
                      placeholder="e.g., LinkedIn, Referral, Job board"
                      className="transition-all focus:ring-2"
                    />
                  </div>
                </div>
              )}

              {formError && (
                <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-3">
                  <p className="text-sm font-medium text-destructive">
                    {formError}
                  </p>
                </div>
              )}
              {formMessage && (
                <div className="rounded-lg border border-emerald-200 bg-emerald-50 p-3">
                  <p className="text-sm font-medium text-emerald-700">
                    {formMessage}
                  </p>
                </div>
              )}

              <DialogFooter className="gap-2 sm:gap-0">
                <Button
                  variant="outline"
                  type="button"
                  onClick={() => setIsDialogOpen(false)}
                  className="w-full sm:w-auto"
                >
                  Cancel
                </Button>
                <Button
                  type="submit"
                  disabled={isSaving}
                  className="w-full sm:w-auto"
                >
                  {isSaving ? (
                    <>
                      <LoadingSpinner />
                      <span className="ml-2">Saving...</span>
                    </>
                  ) : (
                    "Save changes"
                  )}
                </Button>
              </DialogFooter>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      {profileError && (
        <p className="text-sm text-destructive">
          We could not load your profile. Please try again later.
        </p>
      )}

      <Card>
        <CardHeader>
          <CardTitle>Profile snapshot</CardTitle>
          <CardDescription>
            Information shared with recruiters when you apply.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {profile ? (
            <div className="grid gap-4 md:grid-cols-2">
              {profileSummary.map((item) => (
                <DetailRow
                  key={item.label}
                  label={item.label}
                  value={item.value}
                />
              ))}
              <DetailRow
                label="LinkedIn"
                value={profile.linkedInProfile ?? "—"}
              />
              <DetailRow label="GitHub" value={profile.gitHubProfile ?? "—"} />
              <DetailRow
                label="Portfolio"
                value={profile.portfolioUrl ?? "—"}
              />
              <DetailRow
                label="Education"
                value={
                  profile.degree
                    ? `${profile.degree} • ${profile.college ?? ""}`.trim()
                    : "—"
                }
              />
            </div>
          ) : (
            <div className="rounded-lg border border-amber-200 bg-amber-50 p-6 text-center">
              <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-amber-100">
                <svg
                  className="h-6 w-6 text-amber-600"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z"
                  />
                </svg>
              </div>
              <h3 className="mb-2 text-lg font-semibold text-amber-900">
                Profile Incomplete
              </h3>
              <p className="mb-4 text-sm text-amber-700">
                Complete your profile to be able to apply for jobs.
              </p>
              <Button onClick={() => setIsDialogOpen(true)}>
                Create Profile
              </Button>
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <div>
            <CardTitle>Resume</CardTitle>
            <CardDescription>
              {profile
                ? "Upload a PDF or Word document (max 5MB)."
                : "Create your profile first to upload a resume."}
            </CardDescription>
          </div>
          <input
            ref={fileInputRef}
            type="file"
            className="hidden"
            accept=".pdf,.doc,.docx"
            onChange={handleResumeUpload}
          />
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => fileInputRef.current?.click()}
              disabled={uploadResume.isPending || !profile}
            >
              {resumeUrl ? "Replace resume" : "Upload resume"}
            </Button>
            {resumeUrl && (
              <Button
                variant="ghost"
                size="sm"
                onClick={handleResumeDelete}
                disabled={deleteResume.isPending || !profile}
              >
                Remove
              </Button>
            )}
          </div>
        </CardHeader>
        <CardContent className="space-y-3">
          {renderResumeContent()}

          {resumeMessage && (
            <p className="text-sm text-muted-foreground">{resumeMessage}</p>
          )}
        </CardContent>
      </Card>
    </div>
  );
};
