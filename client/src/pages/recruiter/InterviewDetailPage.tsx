import { useState } from "react";
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
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import {
  useInterviewById,
  useRescheduleInterview,
  useCancelInterview,
  useCompleteInterview,
  useMarkInterviewNoShow,
  useMyInterviewEvaluation,
  useSubmitInterviewEvaluation,
  useSetInterviewOutcome,
} from "@/hooks/staff/interviews.hooks";
import {
  formatDateTimeToLocal,
  formatDateToLocal,
  convertLocalDateTimeToUTC,
} from "@/utils/dateUtils";
import {
  interviewTypeLabels,
  interviewModeLabels,
  getInterviewStatusMeta,
  getInterviewOutcomeMeta,
} from "@/constants/interviewEvaluations";
import { getParticipantRoleLabel } from "@/constants/interviewParticipants";
import { getErrorMessage } from "@/utils/error";

export const RecruiterInterviewDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  // Dialog states
  const [rescheduleDialogOpen, setRescheduleDialogOpen] = useState(false);
  const [cancelDialogOpen, setCancelDialogOpen] = useState(false);
  const [completeDialogOpen, setCompleteDialogOpen] = useState(false);
  const [noShowDialogOpen, setNoShowDialogOpen] = useState(false);
  const [evaluationDialogOpen, setEvaluationDialogOpen] = useState(false);
  const [outcomeDialogOpen, setOutcomeDialogOpen] = useState(false);

  // Form states
  const [newDateTime, setNewDateTime] = useState("");
  const [cancelReason, setCancelReason] = useState("");
  const [summaryNotes, setSummaryNotes] = useState("");
  const [noShowNotes, setNoShowNotes] = useState("");
  const [overallRating, setOverallRating] = useState(3);
  const [evaluationComments, setEvaluationComments] = useState("");
  const [selectedOutcome, setSelectedOutcome] = useState<number>(1);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Mutations
  const rescheduleMutation = useRescheduleInterview();
  const cancelMutation = useCancelInterview();
  const completeMutation = useCompleteInterview();
  const noShowMutation = useMarkInterviewNoShow();
  const submitEvaluationMutation = useSubmitInterviewEvaluation();
  const setOutcomeMutation = useSetInterviewOutcome();

  const interviewQuery = useInterviewById(id!);
  const myEvaluationQuery = useMyInterviewEvaluation(id!);

  // Handler functions
  const handleReschedule = async () => {
    if (!id || !newDateTime) return;

    const utcNewDateTime = convertLocalDateTimeToUTC(newDateTime);
    setSuccessMessage(null);
    setErrorMessage(null);

    try {
      const response = await rescheduleMutation.mutateAsync({
        interviewId: id,
        data: {
          newDateTime: utcNewDateTime,
          reason: "Rescheduled by recruiter",
        },
      });
      setSuccessMessage(
        response.message || "Interview rescheduled successfully"
      );
      setRescheduleDialogOpen(false);
      setNewDateTime("");
      interviewQuery.refetch();
    } catch (error) {
      setErrorMessage(
        getErrorMessage(error) || "Failed to reschedule interview"
      );
    }
  };

  const handleCancel = async () => {
    if (!id) return;

    setSuccessMessage(null);
    setErrorMessage(null);

    try {
      const response = await cancelMutation.mutateAsync({
        interviewId: id,
        data: {
          reason: cancelReason || undefined,
        },
      });
      setSuccessMessage(response.message || "Interview cancelled successfully");
      setCancelDialogOpen(false);
      setCancelReason("");
      interviewQuery.refetch();
    } catch (error) {
      setErrorMessage(getErrorMessage(error) || "Failed to cancel interview");
    }
  };

  const handleComplete = async () => {
    if (!id) return;

    setSuccessMessage(null);
    setErrorMessage(null);

    try {
      const response = await completeMutation.mutateAsync({
        interviewId: id,
        data: {
          summaryNotes: summaryNotes || undefined,
        },
      });
      setSuccessMessage(response.message || "Interview marked as complete");
      setCompleteDialogOpen(false);
      setSummaryNotes("");
      interviewQuery.refetch();
    } catch (error) {
      setErrorMessage(getErrorMessage(error) || "Failed to complete interview");
    }
  };

  const handleNoShow = async () => {
    if (!id) return;

    setSuccessMessage(null);
    setErrorMessage(null);

    try {
      const response = await noShowMutation.mutateAsync({
        interviewId: id,
        data: {
          notes: noShowNotes || undefined,
        },
      });
      setSuccessMessage(response.message || "Interview marked as no-show");
      setNoShowDialogOpen(false);
      setNoShowNotes("");
      interviewQuery.refetch();
    } catch (error) {
      setErrorMessage(getErrorMessage(error) || "Failed to mark no-show");
    }
  };

  const handleSubmitEvaluation = async () => {
    if (!id) return;

    setSuccessMessage(null);
    setErrorMessage(null);

    try {
      const response = await submitEvaluationMutation.mutateAsync({
        interviewId: id,
        data: {
          overallRating,
          additionalComments: evaluationComments || undefined,
        },
      });
      setSuccessMessage(
        response.message || "Evaluation submitted successfully"
      );
      setEvaluationDialogOpen(false);
      setEvaluationComments("");
      setOverallRating(3);
      interviewQuery.refetch();
      myEvaluationQuery.refetch();
    } catch (error) {
      setErrorMessage(getErrorMessage(error) || "Failed to submit evaluation");
    }
  };

  const handleSetOutcome = async () => {
    if (!id) return;

    setSuccessMessage(null);
    setErrorMessage(null);

    try {
      await setOutcomeMutation.mutateAsync({
        interviewId: id,
        data: {
          outcome: selectedOutcome,
        },
      });
      setSuccessMessage("Interview outcome set successfully");
      setOutcomeDialogOpen(false);
      interviewQuery.refetch();
    } catch (error) {
      setErrorMessage(getErrorMessage(error) || "Failed to set outcome");
    }
  };

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
      {/* Success/Error Messages */}
      {successMessage && (
        <div className="bg-green-50 text-green-800 p-3 rounded-md text-sm">
          {successMessage}
        </div>
      )}
      {errorMessage && (
        <div className="bg-red-50 text-red-800 p-3 rounded-md text-sm">
          {errorMessage}
        </div>
      )}

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
                          {getParticipantRoleLabel(participant.role)}
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
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle className="flex items-center gap-2">
                  <MessageSquare className="h-5 w-5" />
                  Evaluations ({interview.evaluations?.length || 0})
                </CardTitle>
                {interview.status === 2 && !myEvaluationQuery.data && (
                  <Button
                    size="sm"
                    onClick={() => {
                      setOverallRating(3);
                      setEvaluationComments("");
                      setEvaluationDialogOpen(true);
                    }}
                  >
                    <MessageSquare className="h-4 w-4 mr-2" />
                    Submit Evaluation
                  </Button>
                )}
              </div>
            </CardHeader>
            <CardContent>
              {interview.evaluations && interview.evaluations.length > 0 ? (
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
              ) : (
                <p className="text-sm text-muted-foreground">
                  No evaluations submitted yet
                </p>
              )}
            </CardContent>
          </Card>
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
                <Button
                  variant="outline"
                  className="w-full"
                  onClick={() => {
                    setNewDateTime("");
                    setRescheduleDialogOpen(true);
                  }}
                >
                  <Edit className="h-4 w-4 mr-2" />
                  Reschedule
                </Button>
                <Button
                  variant="outline"
                  className="w-full"
                  onClick={() => {
                    setSummaryNotes("");
                    setCompleteDialogOpen(true);
                  }}
                >
                  <CheckCircle className="h-4 w-4 mr-2" />
                  Mark Complete
                </Button>
                <Button
                  variant="outline"
                  className="w-full"
                  onClick={() => {
                    setNoShowNotes("");
                    setNoShowDialogOpen(true);
                  }}
                >
                  <XCircle className="h-4 w-4 mr-2" />
                  Mark No-Show
                </Button>
                <Button
                  variant="destructive"
                  className="w-full"
                  onClick={() => {
                    setCancelReason("");
                    setCancelDialogOpen(true);
                  }}
                >
                  <X className="h-4 w-4 mr-2" />
                  Cancel Interview
                </Button>
              </CardContent>
            </Card>
          )}

          {/* Set Outcome - for completed interviews */}
          {interview.status === 2 && (
            <Card>
              <CardHeader>
                <CardTitle>Outcome Management</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2">
                <Button
                  variant="outline"
                  className="w-full"
                  onClick={() => {
                    setSelectedOutcome(interview.outcome || 1);
                    setOutcomeDialogOpen(true);
                  }}
                >
                  <Star className="h-4 w-4 mr-2" />
                  {interview.outcome ? "Change Outcome" : "Set Outcome"}
                </Button>
              </CardContent>
            </Card>
          )}
        </div>
      </div>

      {/* Reschedule Dialog */}
      <Dialog
        open={rescheduleDialogOpen}
        onOpenChange={setRescheduleDialogOpen}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Reschedule Interview</DialogTitle>
            <DialogDescription>
              Select a new date and time for {interview.title}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="new-datetime">New Date & Time</Label>
              <Input
                id="new-datetime"
                type="datetime-local"
                value={newDateTime}
                onChange={(e) => setNewDateTime(e.target.value)}
              />
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setRescheduleDialogOpen(false)}
            >
              Cancel
            </Button>
            <Button
              onClick={handleReschedule}
              disabled={!newDateTime || rescheduleMutation.isPending}
            >
              {rescheduleMutation.isPending ? "Rescheduling..." : "Reschedule"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Cancel Dialog */}
      <Dialog open={cancelDialogOpen} onOpenChange={setCancelDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Cancel Interview</DialogTitle>
            <DialogDescription>
              Are you sure you want to cancel {interview.title}? This action
              cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="cancel-reason">Reason (Optional)</Label>
              <Textarea
                id="cancel-reason"
                placeholder="Please provide a reason for cancellation..."
                value={cancelReason}
                onChange={(e) => setCancelReason(e.target.value)}
                rows={3}
              />
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setCancelDialogOpen(false)}
            >
              Close
            </Button>
            <Button
              variant="destructive"
              onClick={handleCancel}
              disabled={cancelMutation.isPending}
            >
              {cancelMutation.isPending ? "Cancelling..." : "Cancel Interview"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Complete Dialog */}
      <Dialog open={completeDialogOpen} onOpenChange={setCompleteDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Mark Interview as Complete</DialogTitle>
            <DialogDescription>
              Mark {interview.title} as complete and add any summary notes.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="summary-notes">Summary Notes (Optional)</Label>
              <Textarea
                id="summary-notes"
                placeholder="Add any notes about the interview..."
                value={summaryNotes}
                onChange={(e) => setSummaryNotes(e.target.value)}
                rows={4}
              />
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setCompleteDialogOpen(false)}
            >
              Cancel
            </Button>
            <Button
              onClick={handleComplete}
              disabled={completeMutation.isPending}
            >
              {completeMutation.isPending ? "Completing..." : "Mark Complete"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* No-Show Dialog */}
      <Dialog open={noShowDialogOpen} onOpenChange={setNoShowDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Mark as No-Show</DialogTitle>
            <DialogDescription>
              Mark {interview.title} as no-show. The candidate did not attend.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="noshow-notes">Notes (Optional)</Label>
              <Textarea
                id="noshow-notes"
                placeholder="Add any additional notes..."
                value={noShowNotes}
                onChange={(e) => setNoShowNotes(e.target.value)}
                rows={3}
              />
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setNoShowDialogOpen(false)}
            >
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleNoShow}
              disabled={noShowMutation.isPending}
            >
              {noShowMutation.isPending ? "Marking..." : "Mark No-Show"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Evaluation Dialog */}
      <Dialog
        open={evaluationDialogOpen}
        onOpenChange={setEvaluationDialogOpen}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Submit Interview Evaluation</DialogTitle>
            <DialogDescription>
              Provide your evaluation for {interview.title}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="overall-rating">Overall Rating *</Label>
              <div className="flex items-center gap-2">
                {[1, 2, 3, 4, 5].map((rating) => (
                  <button
                    key={rating}
                    type="button"
                    onClick={() => setOverallRating(rating)}
                    className="focus:outline-none"
                  >
                    <Star
                      className={`h-8 w-8 transition-colors ${
                        rating <= overallRating
                          ? "fill-yellow-400 text-yellow-400"
                          : "text-gray-300"
                      }`}
                    />
                  </button>
                ))}
                <span className="ml-2 text-sm font-medium">
                  {overallRating}/5
                </span>
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="evaluation-comments">
                Additional Comments (Optional)
              </Label>
              <Textarea
                id="evaluation-comments"
                placeholder="Share your thoughts about the candidate..."
                value={evaluationComments}
                onChange={(e) => setEvaluationComments(e.target.value)}
                rows={5}
              />
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setEvaluationDialogOpen(false)}
            >
              Cancel
            </Button>
            <Button
              onClick={handleSubmitEvaluation}
              disabled={submitEvaluationMutation.isPending}
            >
              {submitEvaluationMutation.isPending
                ? "Submitting..."
                : "Submit Evaluation"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Set Outcome Dialog */}
      <Dialog open={outcomeDialogOpen} onOpenChange={setOutcomeDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Set Interview Outcome</DialogTitle>
            <DialogDescription>
              Set the final outcome for {interview.title}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-3">
              <Label>Select Outcome *</Label>
              <div className="space-y-2">
                <button
                  type="button"
                  onClick={() => setSelectedOutcome(1)}
                  className={`w-full flex items-center gap-3 p-4 border-2 rounded-lg transition-all ${
                    selectedOutcome === 1
                      ? "border-green-500 bg-green-50"
                      : "border-gray-200 hover:border-gray-300"
                  }`}
                >
                  <CheckCircle className="h-5 w-5 text-green-600" />
                  <div className="text-left">
                    <p className="font-semibold">Pass</p>
                    <p className="text-sm text-muted-foreground">
                      Candidate performed well and should progress
                    </p>
                  </div>
                </button>

                <button
                  type="button"
                  onClick={() => setSelectedOutcome(2)}
                  className={`w-full flex items-center gap-3 p-4 border-2 rounded-lg transition-all ${
                    selectedOutcome === 2
                      ? "border-red-500 bg-red-50"
                      : "border-gray-200 hover:border-gray-300"
                  }`}
                >
                  <XCircle className="h-5 w-5 text-red-600" />
                  <div className="text-left">
                    <p className="font-semibold">Fail</p>
                    <p className="text-sm text-muted-foreground">
                      Candidate did not meet expectations
                    </p>
                  </div>
                </button>

                <button
                  type="button"
                  onClick={() => setSelectedOutcome(3)}
                  className={`w-full flex items-center gap-3 p-4 border-2 rounded-lg transition-all ${
                    selectedOutcome === 3
                      ? "border-yellow-500 bg-yellow-50"
                      : "border-gray-200 hover:border-gray-300"
                  }`}
                >
                  <Star className="h-5 w-5 text-yellow-600" />
                  <div className="text-left">
                    <p className="font-semibold">Pending</p>
                    <p className="text-sm text-muted-foreground">
                      Outcome not yet determined
                    </p>
                  </div>
                </button>
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setOutcomeDialogOpen(false)}
            >
              Cancel
            </Button>
            <Button
              onClick={handleSetOutcome}
              disabled={setOutcomeMutation.isPending}
            >
              {setOutcomeMutation.isPending ? "Setting..." : "Set Outcome"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};
