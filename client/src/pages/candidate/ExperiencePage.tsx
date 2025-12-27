import { useEffect, useMemo, useState } from "react";
import { getErrorMessage } from "@/utils/error";

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
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import {
  useCandidateProfile,
  useCandidateWorkExperience,
  useAddCandidateWorkExperience,
  useUpdateCandidateWorkExperience,
  useDeleteCandidateWorkExperience,
  type Schemas,
} from "@/hooks/candidate";

type CandidateWorkExperience = Schemas["CandidateWorkExperienceDto"];

type ExperienceFormState = {
  companyName: string;
  jobTitle: string;
  employmentType: string;
  startDate: string;
  endDate: string;
  isCurrentJob: boolean;
  location: string;
  jobDescription: string;
};

const emptyForm: ExperienceFormState = {
  companyName: "",
  jobTitle: "",
  employmentType: "",
  startDate: "",
  endDate: "",
  isCurrentJob: false,
  location: "",
  jobDescription: "",
};

const toDateInput = (value?: string | null) => {
  if (!value) return "";
  return value.split("T")[0] ?? "";
};

const toIsoString = (value: string | undefined) => {
  if (!value) return undefined;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return undefined;
  return date.toISOString();
};

export const CandidateExperiencePage = () => {
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [editingExperience, setEditingExperience] =
    useState<CandidateWorkExperience | null>(null);
  const [formState, setFormState] = useState<ExperienceFormState>(emptyForm);
  const [formError, setFormError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const { data: profile } = useCandidateProfile();
  const experienceQuery = useCandidateWorkExperience();
  const addExperience = useAddCandidateWorkExperience();
  const updateExperience = useUpdateCandidateWorkExperience();
  const deleteExperience = useDeleteCandidateWorkExperience();

  useEffect(() => {
    if (editingExperience) {
      setFormState({
        companyName: editingExperience.companyName ?? "",
        jobTitle: editingExperience.jobTitle ?? "",
        employmentType: editingExperience.employmentType ?? "",
        startDate: toDateInput(editingExperience.startDate),
        endDate: toDateInput(editingExperience.endDate),
        isCurrentJob: editingExperience.isCurrentJob ?? false,
        location: editingExperience.location ?? "",
        jobDescription: editingExperience.jobDescription ?? "",
      });
    } else {
      setFormState(emptyForm);
    }
  }, [editingExperience]);

  const closeDialog = () => {
    setIsDialogOpen(false);
    setEditingExperience(null);
    setFormError(null);
  };

  const openForCreate = () => {
    setEditingExperience(null);
    setFormState(emptyForm);
    setIsDialogOpen(true);
  };

  const buildPayload = (): Schemas["UpdateCandidateWorkExperienceDto"] => ({
    companyName: formState.companyName || undefined,
    jobTitle: formState.jobTitle || undefined,
    employmentType: formState.employmentType || undefined,
    startDate: toIsoString(formState.startDate),
    endDate: formState.isCurrentJob
      ? undefined
      : toIsoString(formState.endDate),
    isCurrentJob: formState.isCurrentJob,
    location: formState.location || undefined,
    jobDescription: formState.jobDescription || undefined,
  });

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setFormError(null);
    setSuccessMessage(null);

    if (!formState.companyName || !formState.jobTitle || !formState.startDate) {
      setFormError("Company, role, and start date are required");
      return;
    }

    const startDateIso = toIsoString(formState.startDate);
    if (!startDateIso) {
      setFormError("Please provide a valid start date");
      return;
    }

    try {
      if (editingExperience?.id) {
        const response = await updateExperience.mutateAsync({
          workExperienceId: editingExperience.id,
          data: buildPayload(),
        });
        setSuccessMessage(
          response.message || "Experience updated successfully"
        );
      } else {
        const response = await addExperience.mutateAsync({
          companyName: formState.companyName,
          jobTitle: formState.jobTitle,
          startDate: startDateIso,
          endDate: formState.isCurrentJob
            ? undefined
            : toIsoString(formState.endDate),
          employmentType: formState.employmentType || undefined,
          isCurrentJob: formState.isCurrentJob,
          location: formState.location || undefined,
          jobDescription: formState.jobDescription || undefined,
        });
        setSuccessMessage(response.message || "Experience added successfully");
      }
      closeDialog();
    } catch (error) {
      const message = getErrorMessage(error) || "Failed to save experience";
      setFormError(message);
    }
  };

  const handleDelete = (experience: CandidateWorkExperience) => {
    if (!experience.id) return;
    const confirmed = window.confirm(
      `Remove your ${experience.jobTitle ?? "role"} at ${
        experience.companyName ?? "this company"
      }?`
    );
    if (!confirmed) return;
    deleteExperience.mutate(experience.id);
  };

  const experiences: CandidateWorkExperience[] = useMemo(() => {
    const items = experienceQuery.data ?? [];
    return [...items].sort((a, b) => {
      const aDate = a.startDate ? new Date(a.startDate).getTime() : 0;
      const bDate = b.startDate ? new Date(b.startDate).getTime() : 0;
      return bDate - aDate;
    });
  }, [experienceQuery.data]);

  const isSaving = addExperience.isPending || updateExperience.isPending;

  return (
    <div className="space-y-4">
      {successMessage && (
        <div className="bg-green-50 text-green-800 p-3 rounded-md text-sm">
          {successMessage}
        </div>
      )}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold">Experience</h1>
          <p className="text-muted-foreground">
            {!profile
              ? "Create your profile first to manage your work experience."
              : "Tell us where you made an impact throughout your career."}
          </p>
        </div>
        <Button onClick={openForCreate} disabled={!profile}>
          Add experience
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Work history</CardTitle>
          <CardDescription>
            Keep this up to date so recruiters can see your most recent wins.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {!profile ? (
            <div className="rounded-lg border border-amber-200 bg-amber-50 p-6 text-center">
              <p className="text-sm text-amber-700">
                Please create your profile before adding work experience.
              </p>
            </div>
          ) : experienceQuery.isLoading ? (
            <LoadingSpinner />
          ) : !experiences.length ? (
            <p className="text-sm text-muted-foreground">
              No experience entries yet. Add your latest role to get started.
            </p>
          ) : (
            <div className="space-y-3">
              {experiences.map((experience) => (
                <div key={experience.id} className="rounded-lg border p-4">
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div>
                      <p className="text-base font-semibold">
                        {experience.jobTitle ?? "Role"}
                      </p>
                      <p className="text-sm text-muted-foreground">
                        {experience.companyName ?? "Company"}
                      </p>
                    </div>
                    <div className="flex items-center gap-2">
                      {experience.isCurrentJob && (
                        <span className="rounded-full bg-emerald-100 px-3 py-1 text-xs font-medium text-emerald-700">
                          Current role
                        </span>
                      )}
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => {
                          setEditingExperience(experience);
                          setIsDialogOpen(true);
                        }}
                        disabled={!profile}
                      >
                        Edit
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        className="text-destructive"
                        onClick={() => handleDelete(experience)}
                        disabled={!profile}
                      >
                        Remove
                      </Button>
                    </div>
                  </div>
                  <div className="mt-3 flex flex-wrap gap-6 text-sm text-muted-foreground">
                    <span>
                      {experience.startDate
                        ? toDateInput(experience.startDate)
                        : "—"}
                      {" - "}
                      {experience.isCurrentJob
                        ? "Present"
                        : experience.endDate
                        ? toDateInput(experience.endDate)
                        : "—"}
                    </span>
                    {experience.location && <span>{experience.location}</span>}
                    {experience.employmentType && (
                      <span>{experience.employmentType}</span>
                    )}
                  </div>
                  {experience.jobDescription && (
                    <p className="mt-3 whitespace-pre-wrap text-sm text-muted-foreground">
                      {experience.jobDescription}
                    </p>
                  )}
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Dialog
        open={isDialogOpen}
        onOpenChange={(open) => (open ? setIsDialogOpen(true) : closeDialog())}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {editingExperience ? "Update experience" : "Add experience"}
            </DialogTitle>
            <DialogDescription>
              Share the highlights of your role to help recruiters understand
              your impact.
            </DialogDescription>
          </DialogHeader>
          <form className="space-y-4" onSubmit={handleSubmit}>
            <div className="space-y-2">
              <Label htmlFor="companyName">Company</Label>
              <Input
                id="companyName"
                value={formState.companyName}
                onChange={(event) =>
                  setFormState((prev) => ({
                    ...prev,
                    companyName: event.target.value,
                  }))
                }
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="jobTitle">Role</Label>
              <Input
                id="jobTitle"
                value={formState.jobTitle}
                onChange={(event) =>
                  setFormState((prev) => ({
                    ...prev,
                    jobTitle: event.target.value,
                  }))
                }
                required
              />
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="employmentType">Employment type</Label>
                <Input
                  id="employmentType"
                  value={formState.employmentType}
                  onChange={(event) =>
                    setFormState((prev) => ({
                      ...prev,
                      employmentType: event.target.value,
                    }))
                  }
                  placeholder="Full-time, Contract, etc."
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="location">Location</Label>
                <Input
                  id="location"
                  value={formState.location}
                  onChange={(event) =>
                    setFormState((prev) => ({
                      ...prev,
                      location: event.target.value,
                    }))
                  }
                  placeholder="City, Country"
                />
              </div>
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="startDate">Start date</Label>
                <Input
                  id="startDate"
                  type="date"
                  value={formState.startDate}
                  onChange={(event) =>
                    setFormState((prev) => ({
                      ...prev,
                      startDate: event.target.value,
                    }))
                  }
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="endDate">End date</Label>
                <Input
                  id="endDate"
                  type="date"
                  value={formState.endDate}
                  disabled={formState.isCurrentJob}
                  onChange={(event) =>
                    setFormState((prev) => ({
                      ...prev,
                      endDate: event.target.value,
                    }))
                  }
                />
                <label className="flex items-center gap-2 text-sm font-medium">
                  <input
                    type="checkbox"
                    className="h-4 w-4"
                    checked={formState.isCurrentJob}
                    onChange={(event) =>
                      setFormState((prev) => ({
                        ...prev,
                        isCurrentJob: event.target.checked,
                        endDate: event.target.checked ? "" : prev.endDate,
                      }))
                    }
                  />
                  This is my current role
                </label>
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="jobDescription">Summary / key achievements</Label>
              <textarea
                id="jobDescription"
                className="min-h-[120px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                value={formState.jobDescription}
                onChange={(event) =>
                  setFormState((prev) => ({
                    ...prev,
                    jobDescription: event.target.value,
                  }))
                }
                placeholder="Optional: What were your responsibilities or wins?"
              />
            </div>

            {formError && (
              <p className="text-sm text-destructive">{formError}</p>
            )}

            <DialogFooter>
              <Button type="button" variant="ghost" onClick={closeDialog}>
                Cancel
              </Button>
              <Button type="submit" disabled={isSaving}>
                {isSaving ? "Saving..." : "Save"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
};
