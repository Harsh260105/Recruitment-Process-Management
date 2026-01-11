import { useParams } from "react-router-dom";
import { ProfileRequiredWrapper } from "@/components/common/ProfileRequiredWrapper";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { formatDateToLocal, formatDateTimeToLocal } from "@/utils/dateUtils";
import { useCandidateInterviewDetail } from "@/hooks/candidate/interviews.hooks";
import { getErrorMessage } from "@/utils/error";

const interviewModeMap = {
  1: "In-Person",
  2: "Online",
  3: "Phone",
};

const interviewTypeMap = {
  1: "Screening",
  2: "Technical",
  3: "Cultural",
  4: "Final",
};

const interviewStatusMap = {
  1: "Scheduled",
  2: "Completed",
  3: "Cancelled",
  4: "No-Show",
};

const interviewOutcomeMap = {
  1: "Pass",
  2: "Fail",
  3: "Pending",
};

const evaluationRecommendationMap = {
  1: "Pass",
  2: "Fail",
  3: "Maybe",
};

const participantRoleMap = {
  1: "Primary Interviewer",
  2: "Interviewer",
  3: "Observer",
  4: "Shadow",
};

export const CandidateInterviewDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const {
    data: interview,
    isLoading,
    error,
  } = useCandidateInterviewDetail(id!);

  if (isLoading) {
    return (
      <ProfileRequiredWrapper>
        <div className="flex justify-center py-10">
          <LoadingSpinner />
        </div>
      </ProfileRequiredWrapper>
    );
  }

  if (error || !interview) {
    return (
      <ProfileRequiredWrapper>
        <div className="text-center py-12">
          <div className="text-4xl mb-4">❌</div>
          <h3 className="text-lg font-semibold mb-2">
            Error Loading Interview
          </h3>
          <p className="text-muted-foreground">
            {error
              ? getErrorMessage(error)
              : "Interview not found or access denied."}
          </p>
        </div>
      </ProfileRequiredWrapper>
    );
  }

  return (
    <ProfileRequiredWrapper>
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold">
            {interview.title || "Interview Details"}
          </h1>
          <p className="text-muted-foreground">
            Detailed information about your interview.
          </p>
        </div>

        {/* Basic Interview Info */}
        <Card>
          <CardHeader>
            <CardTitle>Interview Information</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <p className="text-sm font-medium">Title</p>
                <p>{interview.title || "N/A"}</p>
              </div>
              <div>
                <p className="text-sm font-medium">Type</p>
                <p>
                  {interviewTypeMap[
                    interview.interviewType as keyof typeof interviewTypeMap
                  ] || "Unknown"}
                </p>
              </div>
              <div>
                <p className="text-sm font-medium">Round</p>
                <p>{interview.roundNumber || "N/A"}</p>
              </div>
              <div>
                <p className="text-sm font-medium">Status</p>
                <Badge variant="secondary">
                  {interviewStatusMap[
                    interview.status as keyof typeof interviewStatusMap
                  ] || interview.status}
                </Badge>
              </div>
              <div>
                <p className="text-sm font-medium">Scheduled Date & Time</p>
                <p>{formatDateTimeToLocal(interview.scheduledDateTime)}</p>
              </div>
              <div>
                <p className="text-sm font-medium">Duration</p>
                <p>
                  {interview.durationMinutes
                    ? `${interview.durationMinutes} minutes`
                    : "N/A"}
                </p>
              </div>
              <div>
                <p className="text-sm font-medium">Mode</p>
                <p>
                  {interviewModeMap[
                    interview.mode as keyof typeof interviewModeMap
                  ] || interview.mode}
                </p>
              </div>
              <div>
                <p className="text-sm font-medium">Outcome</p>
                <p>
                  {interview.outcome
                    ? interviewOutcomeMap[
                        interview.outcome as keyof typeof interviewOutcomeMap
                      ]
                    : "N/A"}
                </p>
              </div>
            </div>
            {interview.meetingDetails && (
              <div>
                <p className="text-sm font-medium">Meeting Details</p>
                <p className="whitespace-pre-wrap">
                  {interview.meetingDetails}
                </p>
              </div>
            )}
            {interview.instructions && (
              <div>
                <p className="text-sm font-medium">Instructions</p>
                <p className="whitespace-pre-wrap">{interview.instructions}</p>
              </div>
            )}
            {interview.permissions?.canViewInternalNotes &&
              interview.summaryNotes && (
                <div>
                  <p className="text-sm font-medium">Summary Notes</p>
                  <p className="whitespace-pre-wrap">
                    {interview.summaryNotes}
                  </p>
                </div>
              )}
            {interview.permissions?.canViewInternalNotes &&
              interview.scheduledByUserName && (
                <div>
                  <p className="text-sm font-medium">Scheduled By</p>
                  <p>{interview.scheduledByUserName}</p>
                </div>
              )}
          </CardContent>
        </Card>

        {/* Job Information */}
        {interview.job && (
          <Card>
            <CardHeader>
              <CardTitle>Job Information</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <p className="text-sm font-medium">Job Title</p>
                  <p>{interview.job.jobTitle || "N/A"}</p>
                </div>
                <div>
                  <p className="text-sm font-medium">Department</p>
                  <p>{interview.job.department || "N/A"}</p>
                </div>
                <div>
                  <p className="text-sm font-medium">Location</p>
                  <p>{interview.job.location || "N/A"}</p>
                </div>
                <div>
                  <p className="text-sm font-medium">Candidate</p>
                  <p>{interview.job.candidateFullName || "N/A"}</p>
                </div>
                <div>
                  <p className="text-sm font-medium">Candidate Email</p>
                  <p>{interview.job.candidateEmail || "N/A"}</p>
                </div>
                {interview.job.assignedRecruiterName && (
                  <div>
                    <p className="text-sm font-medium">Assigned Recruiter</p>
                    <p>{interview.job.assignedRecruiterName}</p>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        )}

        {/* Contact for Rescheduling */}
        <Card>
          <CardHeader>
            <CardTitle>Need to Reschedule?</CardTitle>
            <CardDescription>
              Contact your assigned recruiter or HR for changes.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {interview.job?.assignedRecruiterName && (
                <p className="text-sm">
                  <strong>Assigned Recruiter:</strong>{" "}
                  {interview.job.assignedRecruiterName}
                </p>
              )}
              <p className="text-sm">
                <strong>HR Support:</strong> hr@company.com
              </p>
              <p className="text-xs text-muted-foreground">
                Please reach out at least 24 hours in advance for rescheduling
                requests.
              </p>
            </div>
          </CardContent>
        </Card>

        {/* Participants */}
        {interview.participants && interview.participants.length > 0 && (
          <Card>
            <CardHeader>
              <CardTitle>Participants</CardTitle>
              <CardDescription>
                People involved in this interview.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {interview.participants.map((participant) => (
                  <div
                    key={participant.id}
                    className="flex justify-between items-center p-4 border rounded-lg"
                  >
                    <div>
                      <p className="font-medium">
                        {participant.participantUserName || "Unknown"}
                      </p>
                      <p className="text-sm text-muted-foreground">
                        {participant.participantUserEmail}
                      </p>
                      <p className="text-sm">
                        Role:{" "}
                        {participantRoleMap[
                          participant.role as keyof typeof participantRoleMap
                        ] || participant.role}
                        {participant.isLead && " (Lead)"}
                      </p>
                      {participant.notes && (
                        <p className="text-sm text-muted-foreground">
                          Notes: {participant.notes}
                        </p>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        )}

        {/* Evaluations */}
        {interview.permissions?.canViewEvaluations &&
          interview.evaluations &&
          interview.evaluations.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>Evaluations</CardTitle>
                <CardDescription>Feedback from interviewers.</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {interview.evaluations.map((evaluation) => (
                    <div key={evaluation.id} className="p-4 border rounded-lg">
                      <div className="flex justify-between items-start mb-2">
                        <div>
                          <p className="font-medium">
                            {evaluation.evaluatorUserName || "Anonymous"}
                          </p>
                          <p className="text-sm text-muted-foreground">
                            {evaluation.evaluatorUserEmail}
                          </p>
                        </div>
                        <div className="text-right">
                          <Badge variant="outline">
                            {evaluationRecommendationMap[
                              evaluation.recommendation as keyof typeof evaluationRecommendationMap
                            ] || evaluation.recommendation}
                          </Badge>
                          {evaluation.overallRating && (
                            <p className="text-sm mt-1">
                              Rating: {evaluation.overallRating}/10
                            </p>
                          )}
                        </div>
                      </div>
                      {evaluation.strengths && (
                        <div className="mb-2">
                          <p className="text-sm font-medium">Strengths:</p>
                          <p className="text-sm">{evaluation.strengths}</p>
                        </div>
                      )}
                      {evaluation.concerns && (
                        <div className="mb-2">
                          <p className="text-sm font-medium">Concerns:</p>
                          <p className="text-sm">{evaluation.concerns}</p>
                        </div>
                      )}
                      {evaluation.additionalComments && (
                        <div>
                          <p className="text-sm font-medium">
                            Additional Comments:
                          </p>
                          <p className="text-sm">
                            {evaluation.additionalComments}
                          </p>
                        </div>
                      )}
                      <p className="text-xs text-muted-foreground mt-2">
                        Created: {formatDateToLocal(evaluation.createdAt)}
                        {evaluation.updatedAt !== evaluation.createdAt &&
                          ` | Updated: ${formatDateToLocal(
                            evaluation.updatedAt
                          )}`}
                      </p>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}
      </div>
    </ProfileRequiredWrapper>
  );
};
