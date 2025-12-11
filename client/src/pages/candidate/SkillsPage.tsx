import { useEffect, useMemo, useState } from "react";
import { isAxiosError } from "axios";

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
  useCandidateSkills,
  useAddCandidateSkills,
  useUpdateCandidateSkill,
  useDeleteCandidateSkill,
  useSkillCatalog,
  type Schemas,
} from "@/hooks/candidate";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import type { ApiResponse } from "@/types/http";

type CandidateSkill = Schemas["CandidateSkillDto"];

const proficiencyLabels: Record<number, string> = {
  1: "Foundation",
  2: "Intermediate",
  3: "Advanced",
  4: "Expert",
  5: "Master",
};

type SkillFormState = {
  skillId: string;
  yearsOfExperience: string;
  proficiencyLevel: string;
};

const emptyForm: SkillFormState = {
  skillId: "",
  yearsOfExperience: "",
  proficiencyLevel: "",
};

const toNumber = (value: string) => {
  if (!value.trim()) return undefined;
  const parsed = Number(value);
  return Number.isNaN(parsed) ? undefined : parsed;
};

const getErrorMessage = (error: unknown): string => {
  if (isAxiosError<ApiResponse>(error)) {
    const payload = error.response?.data;
    if (payload) {
      const detailedMessage =
        payload.errors?.filter(Boolean).join(", ") ?? payload.message;
      if (detailedMessage) {
        return detailedMessage;
      }
    }
  }

  if (error instanceof Error) {
    return error.message;
  }

  return "Unexpected error. Please try again.";
};

export const CandidateSkillsPage = () => {
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [editingSkill, setEditingSkill] = useState<CandidateSkill | null>(null);
  const [formState, setFormState] = useState<SkillFormState>(emptyForm);
  const [formError, setFormError] = useState<string | null>(null);

  const { data: profile } = useCandidateProfile();
  const skillsQuery = useCandidateSkills();
  const addSkill = useAddCandidateSkills();
  const updateSkill = useUpdateCandidateSkill();
  const deleteSkill = useDeleteCandidateSkill();
  const skillCatalogQuery = useSkillCatalog();

  useEffect(() => {
    if (editingSkill) {
      setFormState({
        skillId: editingSkill.skillId ? String(editingSkill.skillId) : "",
        yearsOfExperience:
          editingSkill.yearsOfExperience !== undefined &&
          editingSkill.yearsOfExperience !== null
            ? String(editingSkill.yearsOfExperience)
            : "",
        proficiencyLevel:
          editingSkill.proficiencyLevel !== undefined &&
          editingSkill.proficiencyLevel !== null
            ? String(editingSkill.proficiencyLevel)
            : "",
      });
    } else {
      setFormState(emptyForm);
    }
  }, [editingSkill]);

  const closeDialog = () => {
    setIsDialogOpen(false);
    setEditingSkill(null);
    setFormError(null);
  };

  const openForCreate = () => {
    setEditingSkill(null);
    setFormState(emptyForm);
    setIsDialogOpen(true);
  };

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setFormError(null);

    const parsedSkillId = Number(formState.skillId);
    if (!parsedSkillId || Number.isNaN(parsedSkillId)) {
      setFormError("Skill ID is required");
      return;
    }

    try {
      if (editingSkill) {
        await updateSkill.mutateAsync({
          skillId: String(parsedSkillId),
          data: {
            yearsOfExperience: toNumber(formState.yearsOfExperience),
            proficiencyLevel: toNumber(formState.proficiencyLevel),
          },
        });
      } else {
        await addSkill.mutateAsync([
          {
            skillId: parsedSkillId,
            yearsOfExperience: toNumber(formState.yearsOfExperience),
            proficiencyLevel: toNumber(formState.proficiencyLevel),
          },
        ]);
      }
      skillsQuery.refetch();
      closeDialog();
    } catch (error) {
      const message =
        error instanceof Error ? error.message : "Failed to save skill";
      setFormError(message);
    }
  };

  const handleDelete = (skill: CandidateSkill) => {
    if (!skill.skillId) return;

    deleteSkill.mutate(String(skill.skillId), {
      onSuccess: () => skillsQuery.refetch(),
    });
  };

  const skills: CandidateSkill[] = useMemo(() => {
    return skillsQuery.data ?? [];
  }, [skillsQuery.data]);

  const skillCatalog = useMemo(() => {
    return skillCatalogQuery.data ?? [];
  }, [skillCatalogQuery.data]);

  const isSaving = addSkill.isPending || updateSkill.isPending;
  const isCatalogLoading = skillCatalogQuery.isLoading;

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold">Skills</h1>
          <p className="text-muted-foreground">
            {!profile
              ? "Create your profile first to manage your skills."
              : "Showcase your strongest competencies and keep them current."}
          </p>
        </div>
        <Button onClick={openForCreate} disabled={!profile}>
          Add skill
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Current skills</CardTitle>
          <CardDescription>
            Update experience or proficiency. You can remove a skill anytime.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {!profile ? (
            <div className="rounded-lg border border-amber-200 bg-amber-50 p-6 text-center">
              <p className="text-sm text-amber-700">
                Please create your profile before adding skills.
              </p>
            </div>
          ) : skillsQuery.isLoading ? (
            <LoadingSpinner />
          ) : !skills.length ? (
            <p className="text-sm text-muted-foreground">
              No skills added yet. Use the button above to add the skills that
              best describe you.
            </p>
          ) : (
            <div className="space-y-2">
              {/* Header row for desktop */}
              <div className="hidden grid-cols-12 gap-4 border-b pb-2 text-sm font-medium text-muted-foreground md:grid">
                <div className="col-span-4">Skill</div>
                <div className="col-span-2">Category</div>
                <div className="col-span-2">Experience</div>
                <div className="col-span-2">Proficiency</div>
                <div className="col-span-2">Actions</div>
              </div>

              {skills.map((skill) => (
                <div
                  key={skill.id}
                  className="grid grid-cols-1 gap-3 rounded-lg border p-4 md:grid-cols-12 md:gap-4 md:items-center"
                >
                  {/* Skill Name */}
                  <div className="md:col-span-4">
                    <p className="text-base font-semibold md:text-sm">
                      {skill.skillName ?? "Skill"}
                    </p>
                  </div>

                  {/* Category */}
                  <div className="md:col-span-2">
                    <p className="text-xs text-muted-foreground md:text-sm">
                      {skill.category ?? "General"}
                    </p>
                  </div>

                  {/* Experience */}
                  <div className="md:col-span-2">
                    <div className="md:hidden">
                      <p className="text-xs text-muted-foreground">
                        Experience
                      </p>
                    </div>
                    <p className="font-medium text-sm">
                      {skill.yearsOfExperience ?? "—"} yrs
                    </p>
                  </div>

                  {/* Proficiency */}
                  <div className="md:col-span-2">
                    <div className="md:hidden">
                      <p className="text-xs text-muted-foreground">
                        Proficiency
                      </p>
                    </div>
                    <p className="font-medium text-sm">
                      {skill.proficiencyLevel
                        ? proficiencyLabels[skill.proficiencyLevel] ??
                          `Level ${skill.proficiencyLevel}`
                        : "—"}
                    </p>
                  </div>

                  {/* Actions */}
                  <div className="flex items-center gap-2 md:col-span-2 md:justify-end">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => {
                        setEditingSkill(skill);
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
                      onClick={() => handleDelete(skill)}
                      disabled={!profile}
                    >
                      Remove
                    </Button>
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
              {editingSkill ? "Update skill" : "Add skill"}
            </DialogTitle>
            <DialogDescription>
              {editingSkill
                ? "Adjust your experience or proficiency for this skill."
                : "Provide the catalog skill ID along with experience details."}
            </DialogDescription>
          </DialogHeader>
          <form className="space-y-4" onSubmit={handleSubmit}>
            {!editingSkill && (
              <div className="space-y-2">
                <Label htmlFor="skillId">Skill</Label>
                <Select
                  value={formState.skillId}
                  onValueChange={(value) =>
                    setFormState((prev) => ({
                      ...prev,
                      skillId: value,
                    }))
                  }
                  disabled={isCatalogLoading || !skillCatalog.length}
                >
                  <SelectTrigger id="skillId">
                    <SelectValue
                      placeholder={
                        isCatalogLoading
                          ? "Loading skills..."
                          : "Select a skill"
                      }
                    />
                  </SelectTrigger>
                  <SelectContent className="max-h-64 overflow-y-auto bg-white/90 border shadow-lg z-50">
                    {skillCatalog.map((skill) => (
                      <SelectItem key={skill.id} value={String(skill.id)}>
                        {skill.name}
                        {skill.category ? ` · ${skill.category}` : ""}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {skillCatalogQuery.isError && (
                  <p className="text-sm text-destructive">
                    {getErrorMessage(skillCatalogQuery.error)}
                  </p>
                )}
              </div>
            )}

            <div className="space-y-2">
              <Label htmlFor="yearsOfExperience">Years of experience</Label>
              <Input
                id="yearsOfExperience"
                value={formState.yearsOfExperience}
                inputMode="decimal"
                onChange={(event) =>
                  setFormState((prev) => ({
                    ...prev,
                    yearsOfExperience: event.target.value,
                  }))
                }
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="proficiencyLevel">Proficiency level (1-5)</Label>
              <Input
                id="proficiencyLevel"
                value={formState.proficiencyLevel}
                inputMode="numeric"
                onChange={(event) =>
                  setFormState((prev) => ({
                    ...prev,
                    proficiencyLevel: event.target.value,
                  }))
                }
              />
              <p className="text-xs text-muted-foreground">
                1 = Foundation, 5 = Mastery
              </p>
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
