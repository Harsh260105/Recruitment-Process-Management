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
  useCandidateEducation,
  useAddCandidateEducation,
  useUpdateCandidateEducation,
  useDeleteCandidateEducation,
  type Schemas,
} from "@/hooks/candidate";

type CandidateEducation = Schemas["CandidateEducationDto"];

type EducationFormState = {
  institutionName: string;
  degree: string;
  fieldOfStudy: string;
  startYear: string;
  endYear: string;
  gpa: string;
  gpaScale: string;
  educationType: string;
};

const emptyForm: EducationFormState = {
  institutionName: "",
  degree: "",
  fieldOfStudy: "",
  startYear: "",
  endYear: "",
  gpa: "",
  gpaScale: "",
  educationType: "",
};

const toNumber = (value: string) => {
  if (!value.trim()) return undefined;
  const parsed = Number(value);
  return Number.isNaN(parsed) ? undefined : parsed;
};

export const CandidateEducationPage = () => {
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [editingEducation, setEditingEducation] =
    useState<CandidateEducation | null>(null);
  const [formState, setFormState] = useState<EducationFormState>(emptyForm);
  const [formError, setFormError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const { data: profile } = useCandidateProfile();
  const educationQuery = useCandidateEducation();
  const addEducation = useAddCandidateEducation();
  const updateEducation = useUpdateCandidateEducation();
  const deleteEducation = useDeleteCandidateEducation();

  useEffect(() => {
    if (editingEducation) {
      setFormState({
        institutionName: editingEducation.institutionName ?? "",
        degree: editingEducation.degree ?? "",
        fieldOfStudy: editingEducation.fieldOfStudy ?? "",
        startYear:
          editingEducation.startYear !== undefined &&
          editingEducation.startYear !== null
            ? String(editingEducation.startYear)
            : "",
        endYear:
          editingEducation.endYear !== undefined &&
          editingEducation.endYear !== null
            ? String(editingEducation.endYear)
            : "",
        gpa:
          editingEducation.gpa !== undefined && editingEducation.gpa !== null
            ? String(editingEducation.gpa)
            : "",
        gpaScale: editingEducation.gpaScale ?? "",
        educationType: editingEducation.educationType ?? "",
      });
    } else {
      setFormState(emptyForm);
    }
  }, [editingEducation]);

  const closeDialog = () => {
    setIsDialogOpen(false);
    setEditingEducation(null);
    setFormError(null);
  };

  const openForCreate = () => {
    setEditingEducation(null);
    setFormState(emptyForm);
    setIsDialogOpen(true);
  };

  const buildPayload = (): Schemas["UpdateCandidateEducationDto"] => ({
    institutionName: formState.institutionName || undefined,
    degree: formState.degree || undefined,
    fieldOfStudy: formState.fieldOfStudy || undefined,
    startYear: toNumber(formState.startYear),
    endYear: toNumber(formState.endYear),
    gpa: toNumber(formState.gpa),
    gpaScale: formState.gpaScale || undefined,
    educationType: formState.educationType || undefined,
  });

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setFormError(null);
    setSuccessMessage(null);

    if (
      !formState.institutionName ||
      !formState.degree ||
      !formState.startYear ||
      !formState.gpa
    ) {
      setFormError("Institution, degree, start year, and GPA are required");
      return;
    }

    try {
      const parsedStartYear = Number(formState.startYear);
      const parsedGpa = Number(formState.gpa);

      if (Number.isNaN(parsedStartYear) || Number.isNaN(parsedGpa)) {
        setFormError("Please provide numeric values for start year and GPA");
        return;
      }

      if (editingEducation?.id) {
        const response = await updateEducation.mutateAsync({
          educationId: editingEducation.id,
          data: buildPayload(),
        });
        setSuccessMessage(response.message || "Education updated successfully");
      } else {
        const base = buildPayload();
        const response = await addEducation.mutateAsync({
          institutionName: formState.institutionName,
          degree: formState.degree,
          fieldOfStudy: base.fieldOfStudy,
          startYear: parsedStartYear,
          endYear: base.endYear,
          gpaScale: base.gpaScale,
          gpa: parsedGpa,
          educationType: base.educationType,
        } as Schemas["CreateCandidateEducationDto"]);
        setSuccessMessage(response.message || "Education added successfully");
      }
      closeDialog();
    } catch (error) {
      const message = getErrorMessage(error) || "Failed to save education";
      setFormError(message);
    }
  };

  const handleDelete = (education: CandidateEducation) => {
    if (!education.id) return;
    const confirmed = window.confirm(
      `Remove ${education.degree ?? "this entry"}?`
    );
    if (!confirmed) return;
    deleteEducation.mutate(education.id);
  };

  const educationItems: CandidateEducation[] = useMemo(() => {
    const items = educationQuery.data ?? [];
    return [...items].sort((a, b) => {
      const aYear = a.endYear ?? a.startYear ?? 0;
      const bYear = b.endYear ?? b.startYear ?? 0;
      return bYear - aYear;
    });
  }, [educationQuery.data]);

  const isSaving = addEducation.isPending || updateEducation.isPending;

  return (
    <div className="space-y-4">
      {successMessage && (
        <div className="bg-green-50 text-green-800 p-3 rounded-md text-sm">
          {successMessage}
        </div>
      )}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold">Education</h1>
          <p className="text-muted-foreground">
            {!profile
              ? "Create your profile first to manage your education history."
              : "Highlight your academic background and credentials."}
          </p>
        </div>
        <Button onClick={openForCreate} disabled={!profile}>
          Add education
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Education history</CardTitle>
          <CardDescription>
            Provide the degrees and certifications that strengthen your profile.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {!profile ? (
            <div className="rounded-lg border border-amber-200 bg-amber-50 p-6 text-center">
              <p className="text-sm text-amber-700">
                Please create your profile before adding education history.
              </p>
            </div>
          ) : educationQuery.isLoading ? (
            <LoadingSpinner />
          ) : !educationItems.length ? (
            <p className="text-sm text-muted-foreground">
              No education entries yet. Add at least your latest degree.
            </p>
          ) : (
            <div className="space-y-3">
              {educationItems.map((education) => (
                <div key={education.id} className="rounded-lg border p-4">
                  <div className="flex flex-wrap items-center justify-between gap-3">
                    <div>
                      <p className="text-base font-semibold">
                        {education.degree ?? "Degree"} •{" "}
                        {education.institutionName ?? "Institution"}
                      </p>
                      <p className="text-xs text-muted-foreground">
                        {education.fieldOfStudy ?? "Field of study"}
                      </p>
                    </div>
                    <div className="flex items-center gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => {
                          setEditingEducation(education);
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
                        onClick={() => handleDelete(education)}
                        disabled={!profile}
                      >
                        Remove
                      </Button>
                    </div>
                  </div>
                  <div className="mt-3 flex flex-wrap gap-6 text-sm text-muted-foreground">
                    <span>
                      {education.startYear ?? "—"} -{" "}
                      {education.endYear ?? "Present"}
                    </span>
                    {education.gpa !== undefined && education.gpa !== null && (
                      <span>
                        GPA: {education.gpa}
                        {education.gpaScale ? ` / ${education.gpaScale}` : ""}
                      </span>
                    )}
                    {education.educationType && (
                      <span>{education.educationType}</span>
                    )}
                  </div>
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
              {editingEducation ? "Update education" : "Add education"}
            </DialogTitle>
            <DialogDescription>
              Enter the details exactly as you want recruiters to see them.
            </DialogDescription>
          </DialogHeader>
          <form className="space-y-4" onSubmit={handleSubmit}>
            <div className="space-y-2">
              <Label htmlFor="institution">Institution</Label>
              <Input
                id="institution"
                value={formState.institutionName}
                onChange={(event) =>
                  setFormState((prev) => ({
                    ...prev,
                    institutionName: event.target.value,
                  }))
                }
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="degree">Degree</Label>
              <Input
                id="degree"
                value={formState.degree}
                onChange={(event) =>
                  setFormState((prev) => ({
                    ...prev,
                    degree: event.target.value,
                  }))
                }
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="fieldOfStudy">Field of study</Label>
              <Input
                id="fieldOfStudy"
                value={formState.fieldOfStudy}
                onChange={(event) =>
                  setFormState((prev) => ({
                    ...prev,
                    fieldOfStudy: event.target.value,
                  }))
                }
              />
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="startYear">Start year</Label>
                <Input
                  id="startYear"
                  value={formState.startYear}
                  inputMode="numeric"
                  onChange={(event) =>
                    setFormState((prev) => ({
                      ...prev,
                      startYear: event.target.value,
                    }))
                  }
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="endYear">End year</Label>
                <Input
                  id="endYear"
                  value={formState.endYear}
                  inputMode="numeric"
                  onChange={(event) =>
                    setFormState((prev) => ({
                      ...prev,
                      endYear: event.target.value,
                    }))
                  }
                />
              </div>
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="gpa">GPA</Label>
                <Input
                  id="gpa"
                  value={formState.gpa}
                  inputMode="decimal"
                  onChange={(event) =>
                    setFormState((prev) => ({
                      ...prev,
                      gpa: event.target.value,
                    }))
                  }
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="gpaScale">GPA scale</Label>
                <Input
                  id="gpaScale"
                  value={formState.gpaScale}
                  onChange={(event) =>
                    setFormState((prev) => ({
                      ...prev,
                      gpaScale: event.target.value,
                    }))
                  }
                  placeholder="e.g. 10, 4.0"
                />
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="educationType">Education type</Label>
              <Input
                id="educationType"
                value={formState.educationType}
                onChange={(event) =>
                  setFormState((prev) => ({
                    ...prev,
                    educationType: event.target.value,
                  }))
                }
                placeholder="Full-time, Part-time, Certification"
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
