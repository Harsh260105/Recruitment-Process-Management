import { useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Calendar,
  Clock,
  Users,
  Video,
  Phone,
  MapPin,
  Plus,
  Edit,
  X,
  CheckCircle,
  XCircle,
  AlertCircle,
  Sparkles,
  ChevronRight,
  Eye,
} from "lucide-react";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
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
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { UserPicker } from "./UserPicker";
import type { components } from "@/types/api";
import {
  useInterviewsByApplication,
  useScheduleInterview,
  useRescheduleInterview,
  useCancelInterview,
  useCompleteInterview,
  useMarkInterviewNoShow,
  useAvailableTimeSlots,
  useMyAvailableTimeSlots,
} from "@/hooks/staff/interviews.hooks";
import {
  formatDateTimeToLocal,
  convertLocalDateTimeToUTC,
} from "@/utils/dateUtils";
import {
  interviewTypeLabels,
  interviewModeLabels,
  getInterviewStatusMeta,
} from "@/constants/interviewEvaluations";
import { getErrorMessage } from "@/utils/error";

type InterviewSummary = components["schemas"]["InterviewSummaryDto"];
type ScheduleInterviewDto = components["schemas"]["ScheduleInterviewDto"];
type AvailableTimeSlot = components["schemas"]["AvailableTimeSlotDto"];

interface InterviewManagementSectionProps {
  jobApplicationId: string;
}

// Form schema
const scheduleFormSchema = z.object({
  title: z.string().min(1, "Interview title is required"),
  interviewType: z.number().min(1).max(7),
  interviewMode: z.number().min(1).max(3),
  scheduledDateTime: z.string().min(1, "Scheduled date and time is required"),
  durationMinutes: z.number().min(30).max(120),
  meetingDetails: z.string().optional(),
  instructions: z.string().optional(),
  participants: z
    .array(
      z.object({
        staffProfileId: z.string(),
        userId: z.string(),
        name: z.string(),
        email: z.string().optional(),
        department: z.string().optional(),
      })
    )
    .min(1, "At least one participant is required"),
});

type ScheduleFormData = z.infer<typeof scheduleFormSchema>;

export const InterviewManagementSection = ({
  jobApplicationId,
}: InterviewManagementSectionProps) => {
  const [rescheduleDialogOpen, setRescheduleDialogOpen] = useState(false);
  const [cancelDialogOpen, setCancelDialogOpen] = useState(false);
  const [completeDialogOpen, setCompleteDialogOpen] = useState(false);
  const [noShowDialogOpen, setNoShowDialogOpen] = useState(false);
  const [selectedInterview, setSelectedInterview] =
    useState<InterviewSummary | null>(null);
  const [showScheduleForm, setShowScheduleForm] = useState(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const navigate = useNavigate();

  // React Hook Form for scheduling
  const scheduleForm = useForm<ScheduleFormData>({
    resolver: zodResolver(scheduleFormSchema),
    defaultValues: {
      title: "",
      interviewType: 1,
      interviewMode: 2,
      scheduledDateTime: "",
      durationMinutes: 60,
      meetingDetails: "",
      instructions: "",
      participants: [],
    },
  });

  const {
    register,
    handleSubmit,
    control,
    watch,
    reset,
    setValue,
    formState: { errors, isSubmitting },
  } = scheduleForm;

  // Watch form values for availability
  const watchedScheduledDateTime = watch("scheduledDateTime");
  const watchedParticipants = watch("participants");
  const watchedDurationMinutes = watch("durationMinutes");
  const watchedInterviewMode = watch("interviewMode");

  // Form state for actions
  const [newDateTime, setNewDateTime] = useState("");
  const [cancelReason, setCancelReason] = useState("");
  const [summaryNotes, setSummaryNotes] = useState("");
  const [noShowNotes, setNoShowNotes] = useState("");

  // Available slots - query params
  const [showSlotSuggestions, setShowSlotSuggestions] = useState(false);
  const [showMyAvailability, setShowMyAvailability] = useState(false);
  const [teamSlotsParams, setTeamSlotsParams] = useState<
    components["schemas"]["GetAvailableTimeSlotsRequestDto"] | null
  >(null);
  const [mySlotsParams, setMySlotsParams] = useState<{
    startDate: string;
    endDate: string;
    durationMinutes: number;
  } | null>(null);

  const interviewsQuery = useInterviewsByApplication(jobApplicationId);
  const scheduleInterviewMutation = useScheduleInterview();
  const rescheduleMutation = useRescheduleInterview();
  const cancelMutation = useCancelInterview();
  const completeMutation = useCompleteInterview();
  const noShowMutation = useMarkInterviewNoShow();
  const teamSlotsQuery = useAvailableTimeSlots(teamSlotsParams);
  const mySlotsQuery = useMyAvailableTimeSlots(mySlotsParams);

  // Extract data from queries
  const suggestedSlots = teamSlotsQuery.data || [];
  const myAvailableSlots = mySlotsQuery.data || [];

  const handleOpenScheduleDialog = () => {
    resetScheduleForm();
    setShowScheduleForm(true);
  };

  const handleCloseScheduleForm = () => {
    setShowScheduleForm(false);
    resetScheduleForm();
  };

  const resetScheduleForm = () => {
    reset();
    setTeamSlotsParams(null);
    setShowSlotSuggestions(false);
    setMySlotsParams(null);
    setShowMyAvailability(false);
  };

  const handleFetchAvailableSlots = () => {
    if (!watchedScheduledDateTime || watchedParticipants.length === 0) {
      return;
    }

    const baseDate = new Date(watchedScheduledDateTime);
    // Fetch slots only for the selected day to reduce API load
    const startDate = new Date(baseDate);
    const endDate = new Date(baseDate);

    setTeamSlotsParams({
      startDate: startDate.toISOString(),
      endDate: endDate.toISOString(),
      durationMinutes: watchedDurationMinutes,
      participantUserIds: watchedParticipants.map((p) => p.userId),
    });
    setShowSlotSuggestions(true);
  };

  const handleFetchMyAvailableSlots = () => {
    if (!watchedScheduledDateTime) {
      return;
    }

    const baseDate = new Date(watchedScheduledDateTime);
    // Fetch slots only for the selected day to reduce API load
    const startDate = new Date(baseDate);
    const endDate = new Date(baseDate);

    setMySlotsParams({
      startDate: startDate.toISOString(),
      endDate: endDate.toISOString(),
      durationMinutes: watchedDurationMinutes,
    });
    setShowMyAvailability(true);
  };

  const handleSelectSlot = (slot: AvailableTimeSlot) => {
    if (slot.startDateTime) {
      setValue("scheduledDateTime", slot.startDateTime);
      setShowSlotSuggestions(false);
    }
  };

  const handleScheduleInterview = handleSubmit(
    async (data: ScheduleFormData) => {
      // Convert local datetime to UTC for backend
      const utcScheduledDateTime = convertLocalDateTimeToUTC(
        data.scheduledDateTime
      );

      const payload: ScheduleInterviewDto = {
        jobApplicationId,
        title: data.title,
        interviewType: data.interviewType as 1 | 2 | 3 | 4 | 5 | 6 | 7,
        scheduledDateTime: utcScheduledDateTime,
        durationMinutes: data.durationMinutes,
        mode: data.interviewMode as 1 | 2 | 3,
        meetingDetails: data.meetingDetails || undefined,
        instructions: data.instructions || undefined,
        participantUserIds: data.participants.map((p) => p.userId),
      };

      setSuccessMessage(null);
      setErrorMessage(null);

      try {
        const response = await scheduleInterviewMutation.mutateAsync(payload);
        setSuccessMessage(
          response.message || "Interview scheduled successfully"
        );
        handleCloseScheduleForm();
      } catch (error) {
        setErrorMessage(
          getErrorMessage(error) || "Failed to schedule interview"
        );
      }
    }
  );

  const handleOpenRescheduleDialog = (interview: InterviewSummary) => {
    setSelectedInterview(interview);
    setNewDateTime("");
    setRescheduleDialogOpen(true);
  };

  const handleRescheduleInterview = async () => {
    if (!selectedInterview?.id || !newDateTime) {
      return;
    }

    // Convert local datetime to UTC for backend
    const utcNewDateTime = convertLocalDateTimeToUTC(newDateTime);

    setSuccessMessage(null);
    setErrorMessage(null);

    try {
      const response = await rescheduleMutation.mutateAsync({
        interviewId: selectedInterview.id,
        data: {
          newDateTime: utcNewDateTime,
          reason: "Rescheduled by recruiter",
        },
      });
      setSuccessMessage(
        response.message || "Interview rescheduled successfully"
      );
      setRescheduleDialogOpen(false);
      setSelectedInterview(null);
    } catch (error) {
      setErrorMessage(
        getErrorMessage(error) || "Failed to reschedule interview"
      );
    }
  };

  const handleOpenCancelDialog = (interview: InterviewSummary) => {
    setSelectedInterview(interview);
    setCancelReason("");
    setCancelDialogOpen(true);
  };

  const handleCancelInterview = async () => {
    if (!selectedInterview?.id) {
      return;
    }

    setSuccessMessage(null);
    setErrorMessage(null);

    try {
      const response = await cancelMutation.mutateAsync({
        interviewId: selectedInterview.id,
        data: {
          reason: cancelReason || undefined,
        },
      });
      setSuccessMessage(response.message || "Interview cancelled successfully");
      setCancelDialogOpen(false);
      setSelectedInterview(null);
    } catch (error) {
      setErrorMessage(getErrorMessage(error) || "Failed to cancel interview");
    }
  };

  const handleOpenCompleteDialog = (interview: InterviewSummary) => {
    setSelectedInterview(interview);
    setSummaryNotes("");
    setCompleteDialogOpen(true);
  };

  const handleCompleteInterview = async () => {
    if (!selectedInterview?.id) {
      return;
    }

    setSuccessMessage(null);
    setErrorMessage(null);

    try {
      const response = await completeMutation.mutateAsync({
        interviewId: selectedInterview.id,
        data: {
          summaryNotes: summaryNotes || undefined,
        },
      });
      setSuccessMessage(response.message || "Interview marked as complete");
      setCompleteDialogOpen(false);
      setSelectedInterview(null);
    } catch (error) {
      setErrorMessage(getErrorMessage(error) || "Failed to complete interview");
    }
  };

  const handleOpenNoShowDialog = (interview: InterviewSummary) => {
    setSelectedInterview(interview);
    setNoShowNotes("");
    setNoShowDialogOpen(true);
  };

  const handleMarkNoShow = async () => {
    if (!selectedInterview?.id) {
      return;
    }

    setSuccessMessage(null);
    setErrorMessage(null);

    try {
      const response = await noShowMutation.mutateAsync({
        interviewId: selectedInterview.id,
        data: {
          notes: noShowNotes || undefined,
        },
      });
      setSuccessMessage(response.message || "Interview marked as no-show");
      setNoShowDialogOpen(false);
      setSelectedInterview(null);
    } catch (error) {
      setErrorMessage(getErrorMessage(error) || "Failed to mark no-show");
    }
  };

  const getModeIcon = (mode?: number) => {
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

  if (interviewsQuery.isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Interview Management</CardTitle>
        </CardHeader>
        <CardContent className="flex justify-center py-8">
          <LoadingSpinner />
        </CardContent>
      </Card>
    );
  }

  const interviews = interviewsQuery.data?.items || [];

  return (
    <>
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>Interview Management</CardTitle>
              <CardDescription>
                Schedule and track interviews for this application
              </CardDescription>
            </div>
            {!showScheduleForm && (
              <Button size="sm" onClick={handleOpenScheduleDialog}>
                <Plus className="h-4 w-4" />
                Schedule Interview
              </Button>
            )}
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
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
          {interviewsQuery.isError && (
            <p className="text-sm text-destructive">
              Failed to load interviews. Please try again.
            </p>
          )}

          {/* Inline Schedule Form */}
          {showScheduleForm && (
            <div className="border rounded-lg p-6 bg-muted/30 space-y-6">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-lg font-semibold">
                  Schedule New Interview
                </h3>
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={handleCloseScheduleForm}
                >
                  <X className="h-4 w-4" />
                </Button>
              </div>

              <div className="grid lg:grid-cols-2 gap-6">
                {/* Left Column - Form */}
                <div className="space-y-4">
                  <div className="space-y-2">
                    <Label htmlFor="title">Interview Title *</Label>
                    <Input
                      id="title"
                      {...register("title")}
                      placeholder="e.g., Technical Round 1"
                    />
                    {errors.title && (
                      <p className="text-sm text-destructive">
                        {errors.title.message}
                      </p>
                    )}
                  </div>

                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <Label htmlFor="type">Interview Type *</Label>
                      <Controller
                        name="interviewType"
                        control={control}
                        render={({ field }) => (
                          <Select
                            value={field.value.toString()}
                            onValueChange={(value) =>
                              field.onChange(Number(value))
                            }
                          >
                            <SelectTrigger>
                              <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                              {Object.entries(interviewTypeLabels).map(
                                ([key, label]) => (
                                  <SelectItem key={key} value={key}>
                                    {label}
                                  </SelectItem>
                                )
                              )}
                            </SelectContent>
                          </Select>
                        )}
                      />
                      {errors.interviewType && (
                        <p className="text-sm text-destructive">
                          {errors.interviewType.message}
                        </p>
                      )}
                    </div>

                    <div className="space-y-2">
                      <Label htmlFor="mode">Interview Mode *</Label>
                      <Controller
                        name="interviewMode"
                        control={control}
                        render={({ field }) => (
                          <Select
                            value={field.value.toString()}
                            onValueChange={(value) =>
                              field.onChange(Number(value))
                            }
                          >
                            <SelectTrigger>
                              <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                              {Object.entries(interviewModeLabels).map(
                                ([key, label]) => (
                                  <SelectItem key={key} value={key}>
                                    {label}
                                  </SelectItem>
                                )
                              )}
                            </SelectContent>
                          </Select>
                        )}
                      />
                      {errors.interviewMode && (
                        <p className="text-sm text-destructive">
                          {errors.interviewMode.message}
                        </p>
                      )}
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <Label htmlFor="datetime">Scheduled Date & Time *</Label>
                      <Input
                        id="datetime"
                        type="datetime-local"
                        {...register("scheduledDateTime")}
                      />
                      {errors.scheduledDateTime && (
                        <p className="text-sm text-destructive">
                          {errors.scheduledDateTime.message}
                        </p>
                      )}
                    </div>

                    <div className="space-y-2">
                      <Label htmlFor="duration">Duration *</Label>
                      <Controller
                        name="durationMinutes"
                        control={control}
                        render={({ field }) => (
                          <Select
                            value={field.value.toString()}
                            onValueChange={(value) =>
                              field.onChange(Number(value))
                            }
                          >
                            <SelectTrigger>
                              <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                              <SelectItem value="30">30 min</SelectItem>
                              <SelectItem value="45">45 min</SelectItem>
                              <SelectItem value="60">1 hour</SelectItem>
                              <SelectItem value="90">1.5 hours</SelectItem>
                              <SelectItem value="120">2 hours</SelectItem>
                            </SelectContent>
                          </Select>
                        )}
                      />
                      {errors.durationMinutes && (
                        <p className="text-sm text-destructive">
                          {errors.durationMinutes.message}
                        </p>
                      )}
                    </div>
                  </div>

                  <div className="space-y-2">
                    <Controller
                      name="participants"
                      control={control}
                      render={({ field }) => (
                        <UserPicker
                          selected={field.value}
                          onChange={field.onChange}
                          placeholder="Search and add interviewers"
                        />
                      )}
                    />
                    {errors.participants && (
                      <p className="text-sm text-destructive">
                        {errors.participants.message}
                      </p>
                    )}
                  </div>

                  {watchedInterviewMode !== 2 && (
                    <div className="space-y-2">
                      <Label htmlFor="meeting">
                        Meeting Details (Location/Call-in Info)
                      </Label>
                      <Input
                        id="meeting"
                        {...register("meetingDetails")}
                        placeholder="e.g., meeting Address or call-in info"
                      />
                      {errors.meetingDetails && (
                        <p className="text-sm text-destructive">
                          {errors.meetingDetails.message}
                        </p>
                      )}
                    </div>
                  )}

                  {watchedInterviewMode === 2 && (
                    <div className="space-y-2">
                      <Label>Meeting Details</Label>
                      <div className="p-3 bg-muted rounded-md text-sm text-muted-foreground">
                        Meeting link will be generated automatically for online
                        interviews
                      </div>
                    </div>
                  )}

                  <div className="space-y-2">
                    <Label htmlFor="instructions">
                      Instructions for Candidate
                    </Label>
                    <Textarea
                      id="instructions"
                      {...register("instructions")}
                      placeholder="Any special instructions or preparation needed"
                      rows={3}
                    />
                    {errors.instructions && (
                      <p className="text-sm text-destructive">
                        {errors.instructions.message}
                      </p>
                    )}
                  </div>

                  <div className="flex gap-2 pt-4">
                    <Button
                      onClick={handleScheduleInterview}
                      disabled={
                        isSubmitting || scheduleInterviewMutation.isPending
                      }
                      className="flex-1"
                    >
                      {isSubmitting || scheduleInterviewMutation.isPending
                        ? "Scheduling..."
                        : "Schedule Interview"}
                    </Button>
                    <Button
                      variant="outline"
                      onClick={handleCloseScheduleForm}
                      disabled={
                        isSubmitting || scheduleInterviewMutation.isPending
                      }
                    >
                      Cancel
                    </Button>
                  </div>
                </div>

                {/* Right Column - Availability */}
                <div className="space-y-4">
                  <div className="space-y-2">
                    <h4 className="text-sm font-semibold flex items-center gap-2">
                      <Sparkles className="h-4 w-4 text-primary" />
                      Smart Scheduling Assistant
                    </h4>
                    <p className="text-xs text-muted-foreground">
                      Find the best time for your interview
                    </p>
                  </div>

                  {watchedScheduledDateTime && (
                    <div className="flex gap-2">
                      <Button
                        type="button"
                        size="sm"
                        variant="outline"
                        onClick={handleFetchMyAvailableSlots}
                        disabled={mySlotsQuery.isLoading}
                        className="flex-1"
                      >
                        {mySlotsQuery.isLoading ? (
                          <>Loading...</>
                        ) : (
                          <>
                            <Users className="h-3 w-3" />
                            My Availability
                          </>
                        )}
                      </Button>
                      {watchedParticipants.length > 0 && (
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          onClick={handleFetchAvailableSlots}
                          disabled={teamSlotsQuery.isLoading}
                          className="flex-1"
                        >
                          {teamSlotsQuery.isLoading ? (
                            <>Loading...</>
                          ) : (
                            <>
                              <AlertCircle className="h-3 w-3" />
                              Team Availability
                            </>
                          )}
                        </Button>
                      )}
                    </div>
                  )}

                  {/* My Availability Slots */}
                  {showMyAvailability && myAvailableSlots.length > 0 && (
                    <div className="border rounded-lg p-4 bg-background space-y-3">
                      <div className="flex items-center justify-between">
                        <p className="text-sm font-medium">Your Free Times</p>
                        <Badge variant="outline">
                          {myAvailableSlots.length} slots
                        </Badge>
                      </div>
                      <div className="max-h-[400px] overflow-y-auto space-y-2">
                        {myAvailableSlots.map((slot, index) => (
                          <button
                            key={index}
                            onClick={() => handleSelectSlot(slot)}
                            className="w-full rounded-md border p-3 text-left hover:bg-accent hover:border-primary transition-colors text-sm"
                          >
                            <div className="flex items-center justify-between gap-2">
                              <div className="flex-1 min-w-0">
                                <p className="font-medium truncate">
                                  {slot.startDateTime
                                    ? formatDateTimeToLocal(slot.startDateTime)
                                    : "—"}
                                </p>
                                <p className="text-xs text-muted-foreground">
                                  {slot.durationMinutes} minutes
                                </p>
                              </div>
                              <ChevronRight className="h-4 w-4 text-muted-foreground" />
                            </div>
                          </button>
                        ))}
                      </div>
                    </div>
                  )}

                  {/* Team Availability Slots */}
                  {showSlotSuggestions && suggestedSlots.length > 0 && (
                    <div className="border rounded-lg p-4 bg-background space-y-3">
                      <div className="flex items-center justify-between">
                        <p className="text-sm font-medium">
                          Team Available Times
                        </p>
                        <Badge variant="outline">
                          {suggestedSlots.length} slots
                        </Badge>
                      </div>
                      <p className="text-xs text-muted-foreground">
                        Everyone is free at these times
                      </p>
                      <div className="max-h-[400px] overflow-y-auto space-y-2">
                        {suggestedSlots.map((slot, index) => (
                          <button
                            key={index}
                            onClick={() => handleSelectSlot(slot)}
                            className="w-full rounded-md border p-3 text-left hover:bg-accent hover:border-primary transition-colors text-sm"
                          >
                            <div className="flex items-center justify-between gap-2">
                              <div className="flex-1 min-w-0">
                                <p className="font-medium truncate">
                                  {slot.startDateTime
                                    ? formatDateTimeToLocal(slot.startDateTime)
                                    : "—"}
                                </p>
                                <p className="text-xs text-muted-foreground">
                                  {slot.durationMinutes} minutes
                                </p>
                              </div>
                              <ChevronRight className="h-4 w-4 text-muted-foreground" />
                            </div>
                            {slot.availableParticipants &&
                              slot.availableParticipants.length > 0 && (
                                <p className="text-xs text-muted-foreground mt-2">
                                  ✓ {slot.availableParticipants.join(", ")}
                                </p>
                              )}
                          </button>
                        ))}
                      </div>
                    </div>
                  )}

                  {/* Empty States */}
                  {!watchedScheduledDateTime && (
                    <div className="border rounded-lg p-8 text-center">
                      <Calendar className="h-10 w-10 mx-auto text-muted-foreground mb-3" />
                      <p className="text-sm text-muted-foreground">
                        Select a date to see available times
                      </p>
                    </div>
                  )}

                  {watchedScheduledDateTime &&
                    !showMyAvailability &&
                    !showSlotSuggestions && (
                      <div className="border rounded-lg p-8 text-center">
                        <Clock className="h-10 w-10 mx-auto text-muted-foreground mb-3" />
                        <p className="text-sm text-muted-foreground mb-3">
                          Check availability to find optimal times
                        </p>
                      </div>
                    )}

                  {showMyAvailability && myAvailableSlots.length === 0 && (
                    <div className="border rounded-lg p-8 text-center">
                      <AlertCircle className="h-10 w-10 mx-auto text-muted-foreground mb-3" />
                      <p className="text-sm text-muted-foreground">
                        No free slots found in your schedule
                      </p>
                    </div>
                  )}

                  {showSlotSuggestions && suggestedSlots.length === 0 && (
                    <div className="border rounded-lg p-8 text-center">
                      <AlertCircle className="h-10 w-10 mx-auto text-muted-foreground mb-3" />
                      <p className="text-sm text-muted-foreground">
                        No common free time for all participants
                      </p>
                    </div>
                  )}
                </div>
              </div>
            </div>
          )}

          {/* Empty State when no interviews and form not shown */}
          {!showScheduleForm &&
            interviews.length === 0 &&
            !interviewsQuery.isError && (
              <div className="text-center py-12">
                <Calendar className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
                <h3 className="text-lg font-semibold mb-2">
                  No Interviews Scheduled
                </h3>
                <p className="text-sm text-muted-foreground mb-4">
                  Get started by scheduling the first interview round
                </p>
                <Button size="sm" onClick={handleOpenScheduleDialog}>
                  <Plus className="h-4 w-4" />
                  Schedule Interview
                </Button>
              </div>
            )}

          {/* Interview List */}
          {!showScheduleForm && interviews.length > 0 && (
            <div className="space-y-3">
              {interviews.map((interview) => (
                <div
                  key={interview.id}
                  className="rounded-lg border bg-card p-4 hover:shadow-md transition-shadow"
                >
                  <div className="flex items-start justify-between mb-3">
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-1">
                        <h4 className="font-semibold">{interview.title}</h4>
                        <Badge
                          variant={
                            getInterviewStatusMeta(interview.status).variant
                          }
                        >
                          {getInterviewStatusMeta(interview.status).label}
                        </Badge>
                      </div>
                      <div className="flex items-center gap-4 text-sm text-muted-foreground">
                        <span className="flex items-center gap-1">
                          <Calendar className="h-3 w-3" />
                          {formatDateTimeToLocal(interview.scheduledDateTime)}
                        </span>
                        <span className="flex items-center gap-1">
                          <Clock className="h-3 w-3" />
                          Round {interview.roundNumber}
                        </span>
                        <span className="flex items-center gap-1">
                          {getModeIcon(interview.mode)}
                          {interviewModeLabels[interview.mode ?? 2]}
                        </span>
                      </div>
                    </div>
                  </div>

                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-4 text-sm">
                      <span className="flex items-center gap-1 text-muted-foreground">
                        <Users className="h-3 w-3" />
                        {interview.participantCount} participants
                      </span>
                      {interview.interviewType && (
                        <Badge variant="outline">
                          {interviewTypeLabels[interview.interviewType]}
                        </Badge>
                      )}
                      {interview.averageRating && (
                        <span className="text-xs text-muted-foreground">
                          Rating: {interview.averageRating.toFixed(1)}/5
                        </span>
                      )}
                    </div>

                    <div className="flex items-center gap-2">
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() =>
                          navigate(`/recruiter/interviews/${interview.id}`)
                        }
                      >
                        <Eye className="h-3 w-3" />
                        View Details
                      </Button>
                      {interview.status === 1 && (
                        <>
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() =>
                              handleOpenRescheduleDialog(interview)
                            }
                          >
                            <Edit className="h-3 w-3" />
                            Reschedule
                          </Button>
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => handleOpenCompleteDialog(interview)}
                          >
                            <CheckCircle className="h-3 w-3" />
                            Complete
                          </Button>
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => handleOpenNoShowDialog(interview)}
                          >
                            <XCircle className="h-3 w-3" />
                            No-Show
                          </Button>
                          <Button
                            size="sm"
                            variant="destructive"
                            onClick={() => handleOpenCancelDialog(interview)}
                          >
                            <X className="h-3 w-3" />
                            Cancel
                          </Button>
                        </>
                      )}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Reschedule Dialog */}
      <Dialog
        open={rescheduleDialogOpen}
        onOpenChange={setRescheduleDialogOpen}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Reschedule Interview</DialogTitle>
            <DialogDescription>
              Select a new date and time for {selectedInterview?.title}
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
              onClick={handleRescheduleInterview}
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
              Are you sure you want to cancel {selectedInterview?.title}?
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="cancel-reason">Reason (optional)</Label>
              <Textarea
                id="cancel-reason"
                value={cancelReason}
                onChange={(e) => setCancelReason(e.target.value)}
                placeholder="Reason for cancellation"
                rows={3}
              />
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setCancelDialogOpen(false)}
            >
              Keep Interview
            </Button>
            <Button
              variant="destructive"
              onClick={handleCancelInterview}
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
            <DialogTitle>Mark Interview Complete</DialogTitle>
            <DialogDescription>
              Mark {selectedInterview?.title} as completed
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="summary-notes">Summary Notes (optional)</Label>
              <Textarea
                id="summary-notes"
                value={summaryNotes}
                onChange={(e) => setSummaryNotes(e.target.value)}
                placeholder="Brief summary of the interview"
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
              onClick={handleCompleteInterview}
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
              Mark {selectedInterview?.title} as candidate no-show
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="noshow-notes">Notes (optional)</Label>
              <Textarea
                id="noshow-notes"
                value={noShowNotes}
                onChange={(e) => setNoShowNotes(e.target.value)}
                placeholder="Additional notes about the no-show"
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
              onClick={handleMarkNoShow}
              disabled={noShowMutation.isPending}
            >
              {noShowMutation.isPending ? "Updating..." : "Mark No-Show"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
};
