import { useParams, useNavigate } from "react-router-dom";
import {
  ArrowLeft,
  Calendar,
  Users,
  Video,
  Phone,
  MapPin,
  User,
  Mail,
  Building,
  CheckCircle,
  XCircle,
  Edit,
  Star,
  MessageSquare,
  X,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { useInterviewById } from "@/hooks/staff/interviews.hooks";
import { formatDateTimeToLocal, formatDateToLocal } from "@/utils/dateUtils";
import {
  interviewTypeLabels,
  interviewModeLabels,
  getInterviewStatusMeta,
  getInterviewOutcomeMeta,
} from "@/constants/interviewEvaluations";
import { getErrorMessage } from "@/utils/error";

export const RecruiterInterviewDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const interviewQuery = useInterviewById(id!);

  if (interviewQuery.isLoading) {
    return (
      <div className="flex justify-center py-12">
        <LoadingSpinner />
      </div>
    );
  }

  if (interviewQuery.isError || !interviewQuery.data) {
    return (
      <div className="text-center py-12">
        <h2 className="text-2xl font-semibold text-destructive mb-2">
          Error Loading Interview
        </h2>
        <p className="text-muted-foreground mb-4">
          {interviewQuery.error
            ? getErrorMessage(interviewQuery.error)
            : "Interview not found or access denied."}
        </p>
        <Button onClick={() => navigate(-1)}>
          <ArrowLeft className="h-4 w-4 mr-2" />
          Go Back
        </Button>
      </div>
    );
  }

  const interview = interviewQuery.data;

  const getModeIcon = (mode: number) => {
    switch (mode) {
      case 1:
        return <MapPin className="h-4 w-4" />;
      case 2:
        return <Video className="h-4 w-4" />;
      case 3:
        return <Phone className="h-4 w-4" />;
      default:
        return <Video className="h-4 w-4" />;
    }
  };

  const getOutcomeIcon = (outcome?: number) => {
    switch (outcome) {
      case 1:
        return <CheckCircle className="h-4 w-4 text-green-600" />;
      case 2:
        return <XCircle className="h-4 w-4 text-red-600" />;
      case 3:
        return <Star className="h-4 w-4 text-yellow-600" />;
      default:
        return null;
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="sm" onClick={() => navigate(-1)}>
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back
          </Button>
          <div>
            <h1 className="text-3xl font-semibold">{interview.title}</h1>
            <p className="text-muted-foreground">
              Interview Details - Round {interview.roundNumber}
            </p>
          </div>
        </div>
        <Badge variant={getInterviewStatusMeta(interview.status).variant}>
          {getInterviewStatusMeta(interview.status).label}
        </Badge>
      </div>

      <div className="grid lg:grid-cols-3 gap-6">
        {/* Main Details */}
        <div className="lg:col-span-2 space-y-6">
          {/* Interview Information */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Calendar className="h-5 w-5" />
                Interview Information
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid md:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <p className="text-sm font-medium text-muted-foreground">
                    Scheduled Date & Time
                  </p>
                  <p className="text-lg font-semibold">
                    {formatDateTimeToLocal(interview.scheduledDateTime)}
                  </p>
                </div>
                <div className="space-y-2">
                  <p className="text-sm font-medium text-muted-foreground">
                    Duration
                  </p>
                  <p className="text-lg font-semibold">
                    {interview.durationMinutes} minutes
                  </p>
                </div>
                <div className="space-y-2">
                  <p className="text-sm font-medium text-muted-foreground">
                    Interview Type
                  </p>
                  <p className="font-semibold">
                    {interview.interviewType
                      ? interviewTypeLabels[interview.interviewType]
                      : "Unknown"}
                  </p>
                </div>
                <div className="space-y-2">
                  <p className="text-sm font-medium text-muted-foreground">
                    Mode
                  </p>
                  <div className="flex items-center gap-2">
                    {getModeIcon(interview.mode ?? 2)}
                    <span className="font-semibold">
                      {interviewModeLabels[interview.mode ?? 2]}
                    </span>
                  </div>
                </div>
              </div>

              {interview.meetingDetails && (
                <div className="space-y-2">
                  <p className="text-sm font-medium text-muted-foreground">
                    Meeting Details
                  </p>
                  <p className="text-sm bg-muted p-3 rounded-md">
                    {interview.meetingDetails}
                  </p>
                </div>
              )}

              {interview.instructions && (
                <div className="space-y-2">
                  <p className="text-sm font-medium text-muted-foreground">
                    Instructions
                  </p>
                  <p className="text-sm bg-muted p-3 rounded-md">
                    {interview.instructions}
                  </p>
                </div>
              )}

              {interview.outcome && (
                <div className="space-y-2">
                  <p className="text-sm font-medium text-muted-foreground">
                    Outcome
                  </p>
                  <div className="flex items-center gap-2">
                    {getOutcomeIcon(interview.outcome)}
                    <span className="font-semibold">
                      {getInterviewOutcomeMeta(interview.outcome).label}
                    </span>
                  </div>
                </div>
              )}

              {interview.summaryNotes && (
                <div className="space-y-2">
                  <p className="text-sm font-medium text-muted-foreground">
                    Summary Notes
                  </p>
                  <p className="text-sm bg-muted p-3 rounded-md">
                    {interview.summaryNotes}
                  </p>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Participants */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Users className="h-5 w-5" />
                Participants ({interview.participants?.length || 0})
              </CardTitle>
            </CardHeader>
            <CardContent>
              {interview.participants && interview.participants.length > 0 ? (
                <div className="space-y-3">
                  {interview.participants.map((participant) => (
                    <div
                      key={participant.id}
                      className="flex items-center justify-between p-3 border rounded-lg"
                    >
                      <div className="flex items-center gap-3">
                        <User className="h-8 w-8 text-muted-foreground" />
                        <div>
                          <p className="font-medium">
                            {participant.participantUserName}
                          </p>
                          <p className="text-sm text-muted-foreground">
                            {participant.participantUserEmail}
                          </p>
                        </div>
                      </div>
                      <div className="flex items-center gap-2">
                        {participant.isLead && (
                          <Badge variant="secondary">Lead</Badge>
                        )}
                        <Badge variant="outline">
                          {participant.role === 1 ? "Interviewer" : "Observer"}
                        </Badge>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-muted-foreground">
                  No participants assigned
                </p>
              )}
            </CardContent>
          </Card>

          {/* Evaluations */}
          {interview.evaluations && interview.evaluations.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <MessageSquare className="h-5 w-5" />
                  Evaluations ({interview.evaluations.length})
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {interview.evaluations.map((evaluation) => (
                    <div key={evaluation.id} className="border rounded-lg p-4">
                      <div className="flex items-center justify-between mb-3">
                        <div className="flex items-center gap-2">
                          <User className="h-4 w-4" />
                          <span className="font-medium">
                            {evaluation.evaluatorUserName}
                          </span>
                        </div>
                        <div className="flex items-center gap-1">
                          <Star className="h-4 w-4 fill-yellow-400 text-yellow-400" />
                          <span className="font-semibold">
                            {evaluation.overallRating}/5
                          </span>
                        </div>
                      </div>
                      {evaluation.additionalComments && (
                        <p className="text-sm text-muted-foreground mb-2">
                          {evaluation.additionalComments}
                        </p>
                      )}
                      <p className="text-xs text-muted-foreground">
                        Submitted on {formatDateToLocal(evaluation.createdAt)}
                      </p>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Job Information */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Building className="h-5 w-5" />
                Job Information
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div>
                <p className="text-sm font-medium text-muted-foreground">
                  Position
                </p>
                <p className="font-semibold">{interview.job?.jobTitle}</p>
              </div>
              <div>
                <p className="text-sm font-medium text-muted-foreground">
                  Department
                </p>
                <p>{interview.job?.department || "Not specified"}</p>
              </div>
              <div>
                <p className="text-sm font-medium text-muted-foreground">
                  Location
                </p>
                <p>{interview.job?.location || "Not specified"}</p>
              </div>
              <div className="border-t my-4" />
              <div>
                <p className="text-sm font-medium text-muted-foreground">
                  Candidate
                </p>
                <div className="flex items-center gap-2">
                  <User className="h-4 w-4" />
                  <span className="font-medium">
                    {interview.job?.candidateFullName}
                  </span>
                </div>
                <div className="flex items-center gap-2 mt-1">
                  <Mail className="h-4 w-4" />
                  <span className="text-sm text-muted-foreground">
                    {interview.job?.candidateEmail}
                  </span>
                </div>
              </div>
              {interview.job?.assignedRecruiterName && (
                <>
                  <div className="border-t my-4" />
                  <div>
                    <p className="text-sm font-medium text-muted-foreground">
                      Assigned Recruiter
                    </p>
                    <p>{interview.job.assignedRecruiterName}</p>
                  </div>
                </>
              )}
            </CardContent>
          </Card>

          {/* Scheduled By */}
          <Card>
            <CardHeader>
              <CardTitle>Scheduled By</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="font-medium">{interview.scheduledByUserName}</p>
            </CardContent>
          </Card>

          {/* Actions */}
          {interview.status === 1 && (
            <Card>
              <CardHeader>
                <CardTitle>Actions</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2">
                <Button variant="outline" className="w-full">
                  <Edit className="h-4 w-4 mr-2" />
                  Reschedule
                </Button>
                <Button variant="outline" className="w-full">
                  <CheckCircle className="h-4 w-4 mr-2" />
                  Mark Complete
                </Button>
                <Button variant="outline" className="w-full">
                  <XCircle className="h-4 w-4 mr-2" />
                  Mark No-Show
                </Button>
                <Button variant="destructive" className="w-full">
                  <X className="h-4 w-4 mr-2" />
                  Cancel Interview
                </Button>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
};
