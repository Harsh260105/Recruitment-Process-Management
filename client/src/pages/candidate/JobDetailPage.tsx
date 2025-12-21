import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import {
  ArrowLeft,
  Briefcase,
  CalendarDays,
  MapPin,
  Users,
} from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { ProfileRequiredWrapper } from "@/components/common/ProfileRequiredWrapper";
import {
  useApplyToJob,
  useCandidateProfile,
  useJobPositionDetails,
} from "@/hooks/candidate";
import { formatDateToLocal } from "@/utils/dateUtils";
import { getErrorMessage } from "@/utils/error";

const formatApplicantsCount = (count: number): string => {
  if (count < 100) return count.toString();
  const floored = Math.floor(count / 100) * 100;
  return `${floored}+`;
};

export const CandidateJobDetailPage = () => {
  const { jobId } = useParams<{ jobId: string }>();
  const { data: job, isLoading, isError, error } = useJobPositionDetails(jobId);
  const { data: profile } = useCandidateProfile();
  const applyMutation = useApplyToJob();
  const [coverLetter, setCoverLetter] = useState("");
  const [feedback, setFeedback] = useState<{
    type: "success" | "error";
    message: string;
  } | null>(null);

  useEffect(() => {
    setFeedback(null);
    setCoverLetter("");
  }, [jobId]);

  const computedStatus = useMemo(() => job?.status?.toLowerCase() ?? "", [job]);
  const isDeadlinePassed = useMemo(() => {
    if (!job?.applicationDeadline) return false;
    try {
      return new Date(job.applicationDeadline) < new Date();
    } catch {
      return false;
    }
  }, [job?.applicationDeadline]);
  const isClosed =
    computedStatus === "closed" ||
    computedStatus === "inactive" ||
    computedStatus === "draft";

  const canApply = Boolean(profile?.id) && !isClosed && !isDeadlinePassed;

  const handleApply = async () => {
    if (!jobId) {
      setFeedback({ type: "error", message: "Job information missing." });
      return;
    }

    if (!profile?.id) {
      setFeedback({
        type: "error",
        message: "Create your profile to apply for roles.",
      });
      return;
    }

    try {
      await applyMutation.mutateAsync({
        jobPositionId: jobId,
        candidateProfileId: profile.id,
        coverLetter: coverLetter.trim() ? coverLetter.trim() : undefined,
      });
      setFeedback({
        type: "success",
        message: "Application submitted successfully.",
      });
      setCoverLetter("");
    } catch (applyError) {
      setFeedback({
        type: "error",
        message: getErrorMessage(applyError),
      });
    }
  };

  if (isLoading) {
    return (
      <div className="flex justify-center py-10">
        <LoadingSpinner />
      </div>
    );
  }

  if (isError) {
    return (
      <Card className="border-destructive/30 bg-destructive/5">
        <CardContent className="py-6 text-destructive">
          {getErrorMessage(error)}
        </CardContent>
      </Card>
    );
  }

  if (!job) {
    return (
      <Card>
        <CardContent className="py-6 text-center text-muted-foreground">
          Job details are not available at the moment.
        </CardContent>
      </Card>
    );
  }

  return (
    <ProfileRequiredWrapper>
      <div className="space-y-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <Button asChild variant="ghost" size="sm">
            <Link to="/candidate/jobs">
              <ArrowLeft className="h-4 w-4" /> Back to jobs
            </Link>
          </Button>
          <Badge
            variant={isClosed || isDeadlinePassed ? "destructive" : "secondary"}
          >
            {isClosed
              ? "Closed"
              : isDeadlinePassed
              ? "Deadline passed"
              : job.status || "Open"}
          </Badge>
        </div>

        <Card>
          <CardHeader className="space-y-3">
            <div>
              <CardTitle className="text-3xl font-semibold">
                {job.title ?? "Opportunity"}
              </CardTitle>
              <CardDescription>{job.department || "General"}</CardDescription>
            </div>
            <div className="flex flex-wrap gap-7 text-sm text-muted-foreground">
              <span className="flex items-center gap-1">
                <MapPin className="h-4 w-4" /> {job.location || "Remote"}
              </span>
              <span className="flex items-center gap-1">
                <Briefcase className="h-4 w-4" />{" "}
                {job.employmentType || "Flexible"}
              </span>
              <span className="flex items-center gap-1">
                <CalendarDays className="h-4 w-4" /> Apply by{" "}
                {formatDateToLocal(job.applicationDeadline)}
              </span>
              {typeof job.totalApplicants === "number" && (
                <span className="flex items-center gap-1">
                  <Users className="h-4 w-4" />{" "}
                  {formatApplicantsCount(job.totalApplicants)} applicants
                </span>
              )}
              {job.salaryRange && <span>{job.salaryRange}</span>}
              {job.experienceLevel && <span>{job.experienceLevel}</span>}
            </div>
          </CardHeader>
          <CardContent className="space-y-6">
            {job.description && (
              <section className="space-y-2">
                <h2 className="text-lg font-semibold">Role overview</h2>
                <p className="text-sm text-muted-foreground whitespace-pre-wrap">
                  {job.description}
                </p>
              </section>
            )}

            {job.jobResponsibilities && (
              <section className="space-y-2">
                <h2 className="text-lg font-semibold">Responsibilities</h2>
                <p className="text-sm text-muted-foreground whitespace-pre-wrap">
                  {job.jobResponsibilities}
                </p>
              </section>
            )}

            {job.requiredQualifications && (
              <section className="space-y-2">
                <h2 className="text-lg font-semibold">Qualifications</h2>
                <p className="text-sm text-muted-foreground whitespace-pre-wrap">
                  {job.requiredQualifications}
                </p>
              </section>
            )}

            {job.skills?.length ? (
              <section className="space-y-2">
                <h2 className="text-lg font-semibold">Key skills</h2>
                <div className="flex flex-wrap gap-2">
                  {job.skills.map((skill, index) => (
                    <Badge
                      key={`${skill.skillId ?? skill.skillName ?? index}`}
                      variant={skill.isRequired ? "default" : "secondary"}
                    >
                      <div className="flex items-center gap-2">
                        {skill.skillName ?? "Skill"}
                        {skill.minimumExperience !== undefined &&
                        skill.minimumExperience !== null ? (
                          <span className="text-[10px] text-muted-foreground">
                            {skill.minimumExperience === 0
                              ? "Fresher"
                              : `${skill.minimumExperience}+ yrs`}
                          </span>
                        ) : null}
                      </div>
                    </Badge>
                  ))}
                </div>
              </section>
            ) : null}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Ready to apply?</CardTitle>
            <CardDescription>
              A quick cover note helps recruiters understand your motivation.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {(isClosed || isDeadlinePassed) && (
              <div className="rounded-md border border-destructive/40 bg-destructive/5 p-3 text-sm text-destructive">
                This role is no longer accepting applications.
              </div>
            )}

            {feedback && (
              <div
                className={`rounded-md border p-3 text-sm ${
                  feedback.type === "success"
                    ? "border-emerald-200 bg-emerald-50 text-emerald-800"
                    : "border-destructive/40 bg-destructive/5 text-destructive"
                }`}
              >
                {feedback.message}
              </div>
            )}

            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground">
                Optional cover letter
              </label>
              <textarea
                className="min-h-[120px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                value={coverLetter}
                onChange={(event) => setCoverLetter(event.target.value)}
                placeholder="Share a brief note with the hiring team"
              />
            </div>

            <div className="flex flex-wrap gap-3">
              <Button
                onClick={handleApply}
                disabled={!canApply || applyMutation.isPending}
              >
                {applyMutation.isPending
                  ? "Submitting..."
                  : "Submit application"}
              </Button>
              <Button variant="outline" asChild>
                <Link to="/candidate/jobs">Browse more roles</Link>
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    </ProfileRequiredWrapper>
  );
};
