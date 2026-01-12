import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import {
  useJobApplicationById,
  useShortlistApplication,
  useRejectApplication,
  useHoldApplication,
  useSendTestInvitation,
  useCompleteTest,
  useMoveApplicationToReview,
  useUpdateInternalNotes,
  useUpdateApplicationStatus,
} from "@/hooks/staff/jobApplications.hooks";
import { useJobPositionById } from "@/hooks/staff/jobPositions.hooks";
import {
  useCandidateProfileById,
  useCandidateResumeById,
} from "@/hooks/staff/candidates.hooks";
import {
  useExtendOffer,
  useGetOfferByApplication,
} from "@/hooks/staff/jobOffer.hooks";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
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
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import type { components } from "@/types/api";
import { getStatusMeta } from "@/constants/applicationStatus";
import {
  formatDateToLocal,
  formatDateTimeToLocal,
  convertLocalDateTimeToUTC,
} from "@/utils/dateUtils";
import { getErrorMessage } from "@/utils/error";
import { InterviewManagementSection } from "@/components/interviews/InterviewManagementSection";

const getCandidateName = (
  candidate?: components["schemas"]["JobApplicationCandidateDto"]
) => {
  if (!candidate) return "—";
  const first = candidate.firstName ?? "";
  const last = candidate.lastName ?? "";
  const full = `${first} ${last}`.trim();
  return full || "—";
};

export const RecruiterApplicationDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const applicationQuery = useJobApplicationById(id ?? "");
  const application = applicationQuery.data as
    | components["schemas"]["JobApplicationStaffViewDto"]
    | undefined;
  const updateInternalNotesMutation = useUpdateInternalNotes();
  const shortlistMutation = useShortlistApplication();
  const rejectMutation = useRejectApplication();
  const holdMutation = useHoldApplication();
  const sendTestMutation = useSendTestInvitation();
  const completeTestMutation = useCompleteTest();
  const moveToReviewMutation = useMoveApplicationToReview();
  const updateStatusMutation = useUpdateApplicationStatus();
  const extendOfferMutation = useExtendOffer();
  const jobPositionQuery = useJobPositionById(application?.jobPositionId ?? "");
  const candidateProfileQuery = useCandidateProfileById(
    application?.candidateProfileId ?? "",
    { enabled: !!application?.candidateProfileId }
  );
  const candidateResumeQuery = useCandidateResumeById(
    application?.candidateProfileId ?? "",
    { enabled: !!application?.candidateProfileId }
  );
  const offerQuery = useGetOfferByApplication(application?.id ?? "");

  const [notesValue, setNotesValue] = useState("");
  const [notesFeedback, setNotesFeedback] = useState<string | null>(null);
  const [notesError, setNotesError] = useState<string | null>(null);
  const [shortlistNotes, setShortlistNotes] = useState("");
  const [rejectReason, setRejectReason] = useState("");
  const [holdReason, setHoldReason] = useState("");
  const [testScoreInput, setTestScoreInput] = useState("");
  const [actionFeedback, setActionFeedback] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  // Extend Offer Dialog State
  const [extendOfferDialogOpen, setExtendOfferDialogOpen] = useState(false);
  const [offerSalary, setOfferSalary] = useState("");
  const [offerBenefits, setOfferBenefits] = useState("");
  const [offerJobTitle, setOfferJobTitle] = useState("");
  const [offerExpiryDate, setOfferExpiryDate] = useState("");
  const [offerJoiningDate, setOfferJoiningDate] = useState("");
  const [offerNotes, setOfferNotes] = useState("");

  const statusMeta = getStatusMeta(application?.status as number);

  const historyItems = useMemo(() => {
    return application?.statusHistory ?? [];
  }, [application?.statusHistory]);

  useEffect(() => {
    setNotesValue(application?.internalNotes ?? "");
    setNotesFeedback(null);
    setNotesError(null);
  }, [application?.internalNotes]);

  const notesBaseline = application?.internalNotes ?? "";
  const hasNotesChanged = notesValue !== notesBaseline;
  const isSavingNotes = updateInternalNotesMutation.isPending;
  const isShortlisting = shortlistMutation.isPending;
  const isRejecting = rejectMutation.isPending;
  const isPuttingOnHold = holdMutation.isPending;
  const isSendingTest = sendTestMutation.isPending;
  const isCompletingTest = completeTestMutation.isPending;
  const isMovingToReview = moveToReviewMutation.isPending;
  const isUpdatingStatus = updateStatusMutation.isPending;
  const isExtendingOffer = extendOfferMutation.isPending;

  const handleSaveNotes = async () => {
    if (!application?.id || !hasNotesChanged) return;
    setNotesError(null);
    setNotesFeedback(null);
    try {
      const response = await updateInternalNotesMutation.mutateAsync({
        applicationId: application.id,
        notes: notesValue,
      });
      await queryClient.invalidateQueries({ queryKey: ["jobApplication", id] });
      setNotesFeedback(response.message || "Notes updated successfully.");
    } catch (error) {
      setNotesError(
        getErrorMessage(error) || "Failed to update internal notes."
      );
    }
  };

  const handleResetNotes = () => {
    setNotesValue(notesBaseline);
    setNotesFeedback(null);
    setNotesError(null);
  };

  const runAction = async (
    executor: () => Promise<{
      success: boolean;
      message?: string | null;
      data?: any;
      errors?: string[] | null;
    }>,
    fallbackMessage: string,
    afterSuccess?: () => void
  ) => {
    setActionFeedback(null);
    setActionError(null);
    try {
      const response = await executor();
      await queryClient.invalidateQueries({ queryKey: ["jobApplication", id] });
      afterSuccess?.();
      setActionFeedback(response.message || fallbackMessage);
    } catch (error) {
      setActionError(getErrorMessage(error));
    }
  };

  const handleShortlist = async () => {
    if (!application?.id) return;
    const applicationId = application.id as string;
    await runAction(
      () =>
        shortlistMutation.mutateAsync({
          applicationId,
          notes: shortlistNotes.trim() || undefined,
        }),
      "Application shortlisted.",
      () => setShortlistNotes("")
    );
  };

  const handleReject = async () => {
    if (!application?.id) return;
    if (!rejectReason.trim()) {
      setActionFeedback(null);
      setActionError("Provide a rejection reason before continuing.");
      return;
    }
    const applicationId = application.id as string;
    await runAction(
      () =>
        rejectMutation.mutateAsync({
          applicationId,
          reason: rejectReason.trim(),
        }),
      "Application rejected.",
      () => setRejectReason("")
    );
  };

  const handlePutOnHold = async () => {
    if (!application?.id) return;
    const applicationId = application.id as string;
    await runAction(
      () =>
        holdMutation.mutateAsync({
          applicationId,
          reason: holdReason.trim() || undefined,
        }),
      "Application moved to on-hold.",
      () => setHoldReason("")
    );
  };

  const handleSendTest = async () => {
    if (!application?.id) return;
    const applicationId = application.id as string;
    await runAction(
      () =>
        sendTestMutation.mutateAsync({
          applicationId,
        }),
      "Test invitation sent."
    );
  };

  const handleCompleteTest = async () => {
    if (!application?.id) return;
    if (!testScoreInput.trim()) {
      setActionFeedback(null);
      setActionError("Enter a score before marking the test complete.");
      return;
    }
    const parsedScore = Number(testScoreInput);
    if (Number.isNaN(parsedScore)) {
      setActionFeedback(null);
      setActionError("Score must be a number.");
      return;
    }
    const applicationId = application.id as string;
    await runAction(
      () =>
        completeTestMutation.mutateAsync({
          applicationId,
          score: parsedScore,
        }),
      "Test marked as completed.",
      () => setTestScoreInput("")
    );
  };

  const handleMoveToReview = async () => {
    if (!application?.id) return;
    const applicationId = application.id as string;
    await runAction(
      () =>
        moveToReviewMutation.mutateAsync({
          applicationId,
        }),
      "Application moved to review."
    );
  };

  const handleSelectForOffer = async () => {
    if (!application?.id) return;
    const applicationId = application.id as string;
    await runAction(
      () =>
        updateStatusMutation.mutateAsync({
          applicationId,
          data: {
            status: 7, // Selected
            comments: "Selected for job offer",
          },
        }),
      "Application selected for offer."
    );
  };

  const handleHireCandidate = async () => {
    if (!application?.id) return;
    const applicationId = application.id as string;
    await runAction(
      () =>
        updateStatusMutation.mutateAsync({
          applicationId,
          data: {
            status: 8, // Hired
            comments: "Candidate hired",
          },
        }),
      "Candidate marked as hired."
    );
  };

  const handleExtendOffer = async () => {
    if (!application?.id) return;

    if (!offerSalary.trim() || !offerExpiryDate) {
      setActionFeedback(null);
      setActionError("Salary and expiry date are required.");
      return;
    }

    const parsedSalary = Number(offerSalary);

    if (Number.isNaN(parsedSalary) || parsedSalary <= 0) {
      setActionFeedback(null);
      setActionError("Salary must be a valid positive number.");
      return;
    }

    const applicationId = application.id as string;

    await runAction(
      () =>
        extendOfferMutation.mutateAsync({
          jobApplicationId: applicationId,
          offeredSalary: parsedSalary,
          benefits: offerBenefits.trim() || undefined,
          jobTitle: offerJobTitle.trim() || undefined,
          expiryDate: convertLocalDateTimeToUTC(offerExpiryDate),
          joiningDate: offerJoiningDate
            ? convertLocalDateTimeToUTC(offerJoiningDate)
            : undefined,
          notes: offerNotes.trim() || undefined,
        }),
      "Job offer extended successfully.",
      () => {
        setExtendOfferDialogOpen(false);
        setOfferSalary("");
        setOfferBenefits("");
        setOfferJobTitle("");
        setOfferExpiryDate("");
        setOfferJoiningDate("");
        setOfferNotes("");
      }
    );
  };

  const handleResumeDownload = async () => {
    const resumeUrl = candidateResumeQuery.data;
    if (!resumeUrl) {
      console.error("Resume URL not available");
      return;
    }

    try {
      // Open resume in new tab/window
      window.open(resumeUrl, "_blank");
    } catch (error) {
      console.error("Failed to open resume:", error);
    }
  };

  if (!id) {
    return (
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">
          Missing application identifier.
        </p>
        <Button variant="outline" onClick={() => navigate(-1)}>
          Go back
        </Button>
      </div>
    );
  }

  if (applicationQuery.isLoading) {
    return (
      <div className="flex justify-center py-10">
        <LoadingSpinner />
      </div>
    );
  }

  if (applicationQuery.isError) {
    return (
      <div className="space-y-4">
        <p className="rounded-md border border-destructive/40 bg-destructive/5 p-3 text-sm text-destructive">
          Unable to load this application. Please try again later.
        </p>
        <Button variant="outline" onClick={() => navigate(-1)}>
          Go back
        </Button>
      </div>
    );
  }

  if (!application) {
    return (
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">
          Application details are unavailable.
        </p>
        <Button variant="outline" onClick={() => navigate(-1)}>
          Go back
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <p className="text-sm text-muted-foreground">Application</p>
          <h1 className="text-3xl font-semibold text-foreground">
            {getCandidateName(application.candidate)}
          </h1>
          <p className="text-muted-foreground text-sm">
            {application.jobPosition?.title ?? "Untitled role"}
          </p>
          <div className="mt-2 flex items-center gap-2">
            <Badge variant={statusMeta.variant}>{statusMeta.label}</Badge>
            <span className="text-xs text-muted-foreground">
              Applied {formatDateToLocal(application.appliedDate)}
            </span>
          </div>
          <div className="mt-1">
            <span className="text-xs text-muted-foreground">ID: </span>
            <code className="text-xs font-mono bg-muted px-1 py-0.5 rounded">
              {application.id}
            </code>
          </div>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          {offerQuery.data?.id && (
            <Button
              variant="default"
              onClick={() => navigate(`/admin/offer/${offerQuery.data.id}`)}
            >
              View Offer
            </Button>
          )}
          <Button variant="outline" onClick={() => navigate(-1)}>
            Back to applications
          </Button>
          <Button
            variant="outline"
            onClick={async () => {
              await Promise.all([
                applicationQuery.refetch(),
                jobPositionQuery.refetch(),
                candidateProfileQuery.refetch(),
                candidateResumeQuery.refetch(),
                offerQuery.refetch(),
              ]);
            }}
          >
            Refresh
          </Button>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Job Position</CardTitle>
            <CardDescription>Complete job position details.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3 text-sm">
            <div>
              <p className="text-muted-foreground">Title</p>
              <p className="font-medium text-lg">
                {application.jobPosition?.title ?? "—"}
              </p>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <p className="text-muted-foreground">Department</p>
                <p>{application.jobPosition?.department ?? "—"}</p>
              </div>
              <div>
                <p className="text-muted-foreground">Location</p>
                <p>{application.jobPosition?.location ?? "—"}</p>
              </div>
              <div>
                <p className="text-muted-foreground">Employment Type</p>
                <p>{application.jobPosition?.employmentType ?? "—"}</p>
              </div>
              <div>
                <p className="text-muted-foreground">Experience Level</p>
                <p>{application.jobPosition?.experienceLevel ?? "—"}</p>
              </div>
              <div>
                <p className="text-muted-foreground">Salary Range</p>
                <p>{application.jobPosition?.salaryRange ?? "—"}</p>
              </div>
              <div>
                <p className="text-muted-foreground">Minimum Experience</p>
                <p>
                  {application.jobPosition?.minExperience
                    ? `${application.jobPosition.minExperience} years`
                    : "—"}
                </p>
              </div>
            </div>
            {jobPositionQuery.data?.description && (
              <div>
                <p className="text-muted-foreground">Description</p>
                <p className="whitespace-pre-wrap">
                  {jobPositionQuery.data.description}
                </p>
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Candidate Profile</CardTitle>
            <CardDescription>
              Basic candidate information and profile links.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            {candidateProfileQuery.isLoading && (
              <div className="flex justify-center py-4">
                <LoadingSpinner />
              </div>
            )}
            {candidateProfileQuery.isError && (
              <p className="text-sm text-destructive">
                Failed to load detailed candidate profile.
              </p>
            )}
            {candidateProfileQuery.data && (
              <>
                {/* Basic Information */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <div className="space-y-4">
                    <div>
                      <p className="text-muted-foreground text-sm">Name</p>
                      <p className="font-medium">
                        {getCandidateName(application.candidate)}
                      </p>
                    </div>
                    <div>
                      <p className="text-muted-foreground text-sm">Email</p>
                      <p>{application.candidate?.email ?? "—"}</p>
                    </div>
                    <div>
                      <p className="text-muted-foreground text-sm">Phone</p>
                      <p>{application.candidate?.phoneNumber ?? "—"}</p>
                    </div>
                    <div>
                      <p className="text-muted-foreground text-sm">Location</p>
                      <p>{candidateProfileQuery.data.currentLocation ?? "—"}</p>
                    </div>
                    <div>
                      <p className="text-muted-foreground text-sm">College</p>
                      <p>{candidateProfileQuery.data.college ?? "—"}</p>
                    </div>
                    <div>
                      <p className="text-muted-foreground text-sm">Degree</p>
                      <p>
                        {candidateProfileQuery.data.degree
                          ? `${candidateProfileQuery.data.degree} ${
                              candidateProfileQuery.data.graduationYear
                                ? `(${candidateProfileQuery.data.graduationYear})`
                                : ""
                            }`
                          : "—"}
                      </p>
                    </div>
                  </div>
                  <div className="space-y-4">
                    <div>
                      <p className="text-muted-foreground text-sm">
                        Experience
                      </p>
                      <p>
                        {candidateProfileQuery.data.totalExperience
                          ? `${candidateProfileQuery.data.totalExperience} years`
                          : "—"}
                      </p>
                    </div>
                    <div>
                      <p className="text-muted-foreground text-sm">
                        Current CTC
                      </p>
                      <p>
                        {candidateProfileQuery.data.currentCTC
                          ? `₹${candidateProfileQuery.data.currentCTC.toLocaleString()}`
                          : "—"}
                      </p>
                    </div>
                    <div>
                      <p className="text-muted-foreground text-sm">
                        Expected CTC
                      </p>
                      <p>
                        {candidateProfileQuery.data.expectedCTC
                          ? `₹${candidateProfileQuery.data.expectedCTC.toLocaleString()}`
                          : "—"}
                      </p>
                    </div>
                    <div>
                      <p className="text-muted-foreground text-sm">
                        Notice Period
                      </p>
                      <p>
                        {candidateProfileQuery.data.noticePeriod
                          ? `${candidateProfileQuery.data.noticePeriod} days`
                          : "—"}
                      </p>
                    </div>
                    <div>
                      <p className="text-muted-foreground text-sm">Source</p>
                      <p>{candidateProfileQuery.data.source ?? "—"}</p>
                    </div>
                    <div>
                      <p className="text-muted-foreground text-sm">
                        Open to Relocation
                      </p>
                      <p>
                        {candidateProfileQuery.data.isOpenToRelocation
                          ? "Yes"
                          : "No"}
                      </p>
                    </div>
                  </div>
                </div>

                {/* Profile Links */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <p className="text-muted-foreground text-sm">
                      LinkedIn Profile
                    </p>
                    <p>
                      {candidateProfileQuery.data.linkedInProfile ? (
                        <a
                          href={candidateProfileQuery.data.linkedInProfile}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-blue-600 hover:underline"
                        >
                          {candidateProfileQuery.data.linkedInProfile}
                        </a>
                      ) : (
                        "—"
                      )}
                    </p>
                  </div>
                  <div>
                    <p className="text-muted-foreground text-sm">
                      GitHub Profile
                    </p>
                    <p>
                      {candidateProfileQuery.data.gitHubProfile ? (
                        <a
                          href={candidateProfileQuery.data.gitHubProfile}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-blue-600 hover:underline"
                        >
                          {candidateProfileQuery.data.gitHubProfile}
                        </a>
                      ) : (
                        "—"
                      )}
                    </p>
                  </div>
                  <div>
                    <p className="text-muted-foreground text-sm">Portfolio</p>
                    <p>
                      {candidateProfileQuery.data.portfolioUrl ? (
                        <a
                          href={candidateProfileQuery.data.portfolioUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-blue-600 hover:underline"
                        >
                          {candidateProfileQuery.data.portfolioUrl}
                        </a>
                      ) : (
                        "—"
                      )}
                    </p>
                  </div>
                  <div>
                    <p className="text-muted-foreground text-sm">Resume</p>
                    <p>
                      {candidateProfileQuery.data.resumeFileName ? (
                        candidateResumeQuery.isLoading ? (
                          <span className="text-sm text-muted-foreground">
                            Loading resume...
                          </span>
                        ) : candidateResumeQuery.data ? (
                          <Button
                            variant="link"
                            className="p-0 h-auto text-blue-600 hover:underline"
                            onClick={handleResumeDownload}
                          >
                            Download Resume (
                            {candidateProfileQuery.data.resumeFileName})
                          </Button>
                        ) : (
                          <span className="text-sm text-muted-foreground">
                            Resume not available
                          </span>
                        )
                      ) : (
                        "No resume uploaded"
                      )}
                    </p>
                  </div>
                </div>
              </>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Skills, Education & Experience Section */}
      {candidateProfileQuery.data && (
        <div className="grid gap-6 md:grid-cols-3">
          {/* Skills */}
          {candidateProfileQuery.data.skills &&
            candidateProfileQuery.data.skills.length > 0 && (
              <Card>
                <CardHeader>
                  <CardTitle className="text-lg">Technical Skills</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-1 gap-3">
                    {candidateProfileQuery.data.skills.map((skill) => (
                      <div
                        key={skill.id}
                        className="flex items-center justify-between p-3 rounded-lg border bg-muted/50"
                      >
                        <div>
                          <p className="font-medium text-sm">
                            {skill.skillName}
                          </p>
                          <p className="text-xs text-muted-foreground">
                            {skill.category}
                          </p>
                        </div>
                        <div className="text-right">
                          <p className="text-sm font-medium">
                            {skill.yearsOfExperience} years
                          </p>
                          <p className="text-xs text-muted-foreground">
                            Level {skill.proficiencyLevel}/5
                          </p>
                        </div>
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>
            )}

          {/* Education */}
          {candidateProfileQuery.data.education &&
            candidateProfileQuery.data.education.length > 0 && (
              <Card>
                <CardHeader>
                  <CardTitle className="text-lg">Education</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-3">
                    {candidateProfileQuery.data.education.map((edu) => (
                      <div
                        key={edu.id}
                        className="p-3 rounded-lg border bg-muted/50"
                      >
                        <div className="flex justify-between items-start">
                          <div>
                            <p className="font-medium text-sm">
                              {edu.degree} in {edu.fieldOfStudy}
                            </p>
                            <p className="text-sm text-muted-foreground">
                              {edu.institutionName}
                            </p>
                          </div>
                          <p className="text-xs text-muted-foreground">
                            {edu.startYear} - {edu.endYear || "Present"}
                          </p>
                        </div>
                        {edu.gpa && (
                          <p className="text-xs text-muted-foreground mt-1">
                            GPA: {edu.gpa}/{edu.gpaScale}
                          </p>
                        )}
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>
            )}

          {/* Work Experience */}
          {candidateProfileQuery.data.workExperience &&
            candidateProfileQuery.data.workExperience.length > 0 && (
              <Card>
                <CardHeader>
                  <CardTitle className="text-lg">Work Experience</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-3">
                    {candidateProfileQuery.data.workExperience.map((exp) => (
                      <div
                        key={exp.id}
                        className="p-3 rounded-lg border bg-muted/50"
                      >
                        <div className="flex justify-between items-start">
                          <div>
                            <p className="font-medium text-sm">
                              {exp.jobTitle}
                            </p>
                            <p className="text-sm text-muted-foreground">
                              {exp.companyName}
                            </p>
                            {exp.location && (
                              <p className="text-xs text-muted-foreground">
                                {exp.location}
                              </p>
                            )}
                          </div>
                          <p className="text-xs text-muted-foreground">
                            {exp.startDate
                              ? formatDateToLocal(exp.startDate)
                              : ""}{" "}
                            -{" "}
                            {exp.endDate
                              ? formatDateToLocal(exp.endDate)
                              : "Present"}
                          </p>
                        </div>
                        {exp.jobDescription && (
                          <p className="text-sm mt-2 whitespace-pre-wrap">
                            {exp.jobDescription}
                          </p>
                        )}
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>
            )}
        </div>
      )}

      {!candidateProfileQuery.data?.skills?.length &&
        !candidateProfileQuery.data?.education?.length &&
        !candidateProfileQuery.data?.workExperience?.length &&
        candidateProfileQuery.data && (
          <Card>
            <CardContent className="py-8">
              <p className="text-center text-sm text-muted-foreground">
                No detailed profile information available.
              </p>
            </CardContent>
          </Card>
        )}

      {application.coverLetter && (
        <Card>
          <CardHeader>
            <CardTitle>Cover Letter</CardTitle>
            <CardDescription>Candidate's application letter.</CardDescription>
          </CardHeader>
          <CardContent>
            <p className="text-sm whitespace-pre-wrap">
              {application.coverLetter}
            </p>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader>
          <CardTitle>Assignment & notes</CardTitle>
          <CardDescription>Ownership and internal context.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4 text-sm">
          <div>
            <p className="text-muted-foreground">Assigned recruiter</p>
            <p className="font-medium">
              {application.assignedRecruiter
                ? `${application.assignedRecruiter.firstName ?? ""} ${
                    application.assignedRecruiter.lastName ?? ""
                  }`.trim() || "—"
                : "Unassigned"}
            </p>
          </div>
          <div>
            <p className="text-muted-foreground">Internal notes</p>
            <div className="mt-2 space-y-2">
              <textarea
                className="min-h-[140px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                value={notesValue}
                onChange={(event) => setNotesValue(event.target.value)}
              />
              <div className="flex flex-wrap gap-2 text-xs text-muted-foreground">
                <span>
                  Last updated:{" "}
                  {application.updatedAt
                    ? formatDateTimeToLocal(application.updatedAt)
                    : "—"}
                </span>
              </div>
              {(notesError || notesFeedback) && (
                <p
                  className={
                    notesError
                      ? "text-xs text-destructive"
                      : "text-xs text-emerald-600"
                  }
                >
                  {notesError ?? notesFeedback}
                </p>
              )}
              <div className="flex flex-wrap gap-2">
                <Button
                  size="sm"
                  onClick={handleSaveNotes}
                  disabled={!hasNotesChanged || isSavingNotes}
                >
                  {isSavingNotes ? "Saving..." : "Save notes"}
                </Button>
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={handleResetNotes}
                  disabled={!hasNotesChanged || isSavingNotes}
                >
                  Discard changes
                </Button>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      <InterviewManagementSection jobApplicationId={id} />

      <Card>
        <CardHeader>
          <CardTitle>Status management & history</CardTitle>
          <CardDescription>
            Current status, available actions, and complete status timeline.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-6 lg:grid-cols-3">
            {/* Status Actions Section */}
            <div className="space-y-4 lg:col-span-2">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-muted-foreground">
                    Current status
                  </p>
                  <Badge variant={statusMeta.variant} className="mt-1">
                    {statusMeta.label}
                  </Badge>
                </div>
                <div className="text-right text-sm text-muted-foreground">
                  <p>Applied {formatDateToLocal(application.appliedDate)}</p>
                  {application.updatedAt && (
                    <p>
                      Last updated{" "}
                      {formatDateTimeToLocal(application.updatedAt)}
                    </p>
                  )}
                </div>
              </div>

              {(actionError || actionFeedback) && (
                <p
                  className={
                    actionError
                      ? "text-sm text-destructive"
                      : "text-sm text-emerald-600"
                  }
                >
                  {actionError ?? actionFeedback}
                </p>
              )}

              <div className="space-y-4">
                {/* Applied Status Actions */}
                {application.status === 1 && (
                  <div className="space-y-3">
                    <p className="text-sm font-medium">Available actions</p>
                    <div className="flex flex-wrap gap-2">
                      <Button
                        size="sm"
                        onClick={handleMoveToReview}
                        disabled={isMovingToReview}
                      >
                        {isMovingToReview ? "Moving..." : "Move to review"}
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={handleSendTest}
                        disabled={isSendingTest}
                      >
                        {isSendingTest ? "Sending..." : "Send test"}
                      </Button>
                      <Button
                        size="sm"
                        variant="destructive"
                        onClick={handleReject}
                        disabled={isRejecting}
                      >
                        {isRejecting ? "Rejecting..." : "Reject"}
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={handlePutOnHold}
                        disabled={isPuttingOnHold}
                      >
                        {isPuttingOnHold ? "Updating..." : "Put on hold"}
                      </Button>
                    </div>
                  </div>
                )}

                {/* Test Invited Status Actions */}
                {application.status === 2 && (
                  <div className="space-y-3">
                    <p className="text-sm font-medium">Available actions</p>
                    <div className="flex flex-wrap gap-2">
                      <Button
                        size="sm"
                        onClick={handleCompleteTest}
                        disabled={isCompletingTest}
                      >
                        {isCompletingTest ? "Saving..." : "Mark test complete"}
                      </Button>
                      <Button
                        size="sm"
                        variant="destructive"
                        onClick={handleReject}
                        disabled={isRejecting}
                      >
                        {isRejecting ? "Rejecting..." : "Reject"}
                      </Button>
                    </div>
                    <div>
                      <label className="text-sm font-medium">Test score</label>
                      <Input
                        type="number"
                        min="0"
                        max="100"
                        placeholder="Score for completed test"
                        value={testScoreInput}
                        onChange={(event) =>
                          setTestScoreInput(event.target.value)
                        }
                        className="mt-1"
                      />
                    </div>
                  </div>
                )}

                {/* Test Completed Status Actions */}
                {application.status === 3 && (
                  <div className="space-y-3">
                    <p className="text-sm font-medium">Available actions</p>
                    <div className="flex flex-wrap gap-2">
                      <Button
                        size="sm"
                        onClick={handleShortlist}
                        disabled={isShortlisting}
                      >
                        {isShortlisting ? "Shortlisting..." : "Shortlist"}
                      </Button>
                      <Button
                        size="sm"
                        onClick={handleMoveToReview}
                        disabled={isMovingToReview}
                      >
                        {isMovingToReview ? "Moving..." : "Move to review"}
                      </Button>
                      <Button
                        size="sm"
                        variant="destructive"
                        onClick={handleReject}
                        disabled={isRejecting}
                      >
                        {isRejecting ? "Rejecting..." : "Reject"}
                      </Button>
                    </div>
                  </div>
                )}

                {/* Under Review Status Actions */}
                {application.status === 4 && (
                  <div className="space-y-3">
                    <p className="text-sm font-medium">Available actions</p>
                    <div className="flex flex-wrap gap-2">
                      <Button
                        size="sm"
                        onClick={handleShortlist}
                        disabled={isShortlisting}
                      >
                        {isShortlisting ? "Shortlisting..." : "Shortlist"}
                      </Button>
                      <Button
                        size="sm"
                        variant="destructive"
                        onClick={handleReject}
                        disabled={isRejecting}
                      >
                        {isRejecting ? "Rejecting..." : "Reject"}
                      </Button>
                    </div>
                  </div>
                )}

                {/* Shortlisted Status Actions */}
                {application.status === 5 && (
                  <div className="space-y-3">
                    <p className="text-sm font-medium">Available actions</p>
                    <p className="text-xs text-muted-foreground">
                      Candidate is ready for interviews. Schedule interview
                      rounds from Interview management tab.
                    </p>
                    <div className="flex flex-wrap gap-2">
                      <Button
                        size="sm"
                        variant="destructive"
                        onClick={handleReject}
                        disabled={isRejecting}
                      >
                        {isRejecting ? "Rejecting..." : "Reject"}
                      </Button>
                    </div>
                  </div>
                )}

                {/* Interview Status Actions */}
                {application.status === 6 && (
                  <div className="space-y-3">
                    <p className="text-sm font-medium">Available actions</p>
                    <p className="text-xs text-muted-foreground">
                      After interviews are completed, make a final decision.
                    </p>
                    <div className="flex flex-wrap gap-2">
                      <Button
                        size="sm"
                        variant="default"
                        onClick={handleSelectForOffer}
                        disabled={isUpdatingStatus}
                      >
                        {isUpdatingStatus ? "Selecting..." : "Select for offer"}
                      </Button>
                      <Button
                        size="sm"
                        variant="destructive"
                        onClick={handleReject}
                        disabled={isRejecting}
                      >
                        {isRejecting ? "Rejecting..." : "Reject"}
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={handlePutOnHold}
                        disabled={isPuttingOnHold}
                      >
                        {isPuttingOnHold ? "Updating..." : "Put on hold"}
                      </Button>
                    </div>
                  </div>
                )}

                {/* Selected Status Actions */}
                {application.status === 7 && (
                  <div className="space-y-3">
                    <p className="text-sm font-medium">Available actions</p>
                    <p className="text-xs text-muted-foreground">
                      Extend a job offer to the candidate or finalize hiring.
                    </p>
                    {offerQuery.data?.id && (
                      <div className="rounded-md bg-blue-50 border border-blue-200 p-3">
                        <p className="text-sm text-blue-800">
                          An offer has already been extended.{" "}
                          <button
                            onClick={() =>
                              navigate(`/admin/offer/${offerQuery.data.id}`)
                            }
                            className="underline font-medium hover:text-blue-900"
                          >
                            View Offer Details
                          </button>
                        </p>
                      </div>
                    )}
                    <div className="flex flex-wrap gap-2">
                      <Dialog
                        open={extendOfferDialogOpen}
                        onOpenChange={setExtendOfferDialogOpen}
                      >
                        <DialogTrigger asChild>
                          <Button
                            size="sm"
                            variant="default"
                            disabled={!!offerQuery.data?.id}
                          >
                            Extend Offer
                          </Button>
                        </DialogTrigger>
                        <DialogContent className="max-h-[90vh] overflow-y-auto">
                          <DialogHeader>
                            <DialogTitle>Extend Job Offer</DialogTitle>
                          </DialogHeader>
                          <div className="space-y-4">
                            <div className="space-y-2">
                              <Label htmlFor="offer-salary">
                                Offered Salary *
                              </Label>
                              <Input
                                id="offer-salary"
                                type="number"
                                placeholder="e.g., 120000"
                                value={offerSalary}
                                onChange={(e) => setOfferSalary(e.target.value)}
                              />
                            </div>
                            <div className="space-y-2">
                              <Label htmlFor="offer-job-title">Job Title</Label>
                              <Input
                                id="offer-job-title"
                                placeholder="e.g., Senior Software Engineer"
                                value={offerJobTitle}
                                onChange={(e) =>
                                  setOfferJobTitle(e.target.value)
                                }
                              />
                            </div>
                            <div className="space-y-2">
                              <Label htmlFor="offer-expiry">
                                Expiry Date *
                              </Label>
                              <Input
                                id="offer-expiry"
                                type="datetime-local"
                                value={offerExpiryDate}
                                onChange={(e) =>
                                  setOfferExpiryDate(e.target.value)
                                }
                              />
                            </div>
                            <div className="space-y-2">
                              <Label htmlFor="offer-joining">
                                Joining Date
                              </Label>
                              <Input
                                id="offer-joining"
                                type="datetime-local"
                                value={offerJoiningDate}
                                onChange={(e) =>
                                  setOfferJoiningDate(e.target.value)
                                }
                              />
                            </div>
                            <div className="space-y-2">
                              <Label htmlFor="offer-benefits">Benefits</Label>
                              <Textarea
                                id="offer-benefits"
                                placeholder="Health insurance, 401k, etc."
                                value={offerBenefits}
                                onChange={(e) =>
                                  setOfferBenefits(e.target.value)
                                }
                                rows={3}
                              />
                            </div>
                            <div className="space-y-2">
                              <Label htmlFor="offer-notes">Notes</Label>
                              <Textarea
                                id="offer-notes"
                                placeholder="Additional notes or conditions"
                                value={offerNotes}
                                onChange={(e) => setOfferNotes(e.target.value)}
                                rows={3}
                              />
                            </div>
                            <Button
                              onClick={handleExtendOffer}
                              disabled={
                                isExtendingOffer ||
                                !offerSalary.trim() ||
                                !offerExpiryDate
                              }
                              className="w-full"
                            >
                              {isExtendingOffer
                                ? "Extending Offer..."
                                : "Extend Offer"}
                            </Button>
                          </div>
                        </DialogContent>
                      </Dialog>
                      <Button
                        size="sm"
                        variant="default"
                        onClick={handleHireCandidate}
                        disabled={isUpdatingStatus}
                      >
                        {isUpdatingStatus ? "Hiring..." : "Mark as hired"}
                      </Button>
                      <Button
                        size="sm"
                        variant="destructive"
                        onClick={handleReject}
                        disabled={isRejecting}
                      >
                        {isRejecting ? "Rejecting..." : "Reject"}
                      </Button>
                    </div>
                  </div>
                )}

                {/* On Hold Status Actions */}
                {application.status === 11 && (
                  <div className="space-y-3">
                    <p className="text-sm font-medium">Available actions</p>
                    <p className="text-xs text-muted-foreground">
                      Resume the application by moving it to an appropriate
                      status.
                    </p>
                    <div className="flex flex-wrap gap-2">
                      <Button
                        size="sm"
                        onClick={handleMoveToReview}
                        disabled={isMovingToReview}
                      >
                        {isMovingToReview ? "Moving..." : "Move to review"}
                      </Button>
                      <Button
                        size="sm"
                        onClick={handleShortlist}
                        disabled={isShortlisting}
                      >
                        {isShortlisting ? "Shortlisting..." : "Shortlist"}
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={handleSendTest}
                        disabled={isSendingTest}
                      >
                        {isSendingTest ? "Sending..." : "Send test"}
                      </Button>
                      <Button
                        size="sm"
                        variant="destructive"
                        onClick={handleReject}
                        disabled={isRejecting}
                      >
                        {isRejecting ? "Rejecting..." : "Reject"}
                      </Button>
                    </div>
                  </div>
                )}

                {/* Input fields for actions that need them */}
                {(application.status === 1 ||
                  application.status === 3 ||
                  application.status === 4 ||
                  application.status === 5 ||
                  application.status === 6 ||
                  application.status === 7 ||
                  application.status === 11) && (
                  <div className="grid gap-4 md:grid-cols-2">
                    {(application.status === 1 ||
                      application.status === 3 ||
                      application.status === 11) && (
                      <>
                        <div>
                          <label className="text-sm font-medium">
                            Shortlist notes
                          </label>
                          <textarea
                            className="mt-1 min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                            placeholder="Optional notes for shortlisting"
                            value={shortlistNotes}
                            onChange={(event) =>
                              setShortlistNotes(event.target.value)
                            }
                          />
                        </div>
                        <div>
                          <label className="text-sm font-medium">
                            Rejection reason
                          </label>
                          <textarea
                            className="mt-1 min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                            placeholder="Required for rejection"
                            value={rejectReason}
                            onChange={(event) =>
                              setRejectReason(event.target.value)
                            }
                          />
                        </div>
                        <div>
                          <label className="text-sm font-medium">
                            Hold reason
                          </label>
                          <textarea
                            className="mt-1 min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                            placeholder="Optional reason for hold"
                            value={holdReason}
                            onChange={(event) =>
                              setHoldReason(event.target.value)
                            }
                          />
                        </div>
                      </>
                    )}
                    {(application.status === 3 ||
                      application.status === 4 ||
                      application.status === 5 ||
                      application.status === 6 ||
                      application.status === 7 ||
                      application.status === 11) && (
                      <div>
                        <label className="text-sm font-medium">
                          Rejection reason
                        </label>
                        <textarea
                          className="mt-1 min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                          placeholder="Required for rejection"
                          value={rejectReason}
                          onChange={(event) =>
                            setRejectReason(event.target.value)
                          }
                        />
                      </div>
                    )}
                  </div>
                )}
              </div>
            </div>

            {/* Status History Section */}
            <div className="space-y-3">
              <div>
                <p className="text-sm font-medium">Status timeline</p>
                <p className="text-xs text-muted-foreground">
                  Complete history of status changes
                </p>
              </div>
              {historyItems.length === 0 && (
                <p className="text-muted-foreground text-sm">
                  No status changes captured yet.
                </p>
              )}
              <div className="space-y-3 max-h-96 overflow-y-auto">
                {historyItems.map((entry) => (
                  <div
                    key={entry?.id}
                    className="rounded-lg border border-slate-100 bg-white/80 px-4 py-3"
                  >
                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <div className="font-medium text-sm">
                        {getStatusMeta(entry?.fromStatus as number).label} →{" "}
                        {getStatusMeta(entry?.toStatus as number).label}
                      </div>
                      <span className="text-xs text-muted-foreground">
                        {formatDateTimeToLocal(entry?.changedAt)}
                      </span>
                    </div>
                    <p className="text-xs text-muted-foreground mt-1">
                      {entry?.changedByName ?? "System"}
                    </p>
                    {entry?.comments && (
                      <p className="mt-2 whitespace-pre-wrap text-sm">
                        {entry.comments}
                      </p>
                    )}
                  </div>
                ))}
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};
