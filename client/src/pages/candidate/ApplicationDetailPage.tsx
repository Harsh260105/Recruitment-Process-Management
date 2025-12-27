import { useParams, Link } from "react-router-dom";
import { useState } from "react";
import { ProfileRequiredWrapper } from "@/components/common/ProfileRequiredWrapper";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  useCandidateApplicationById,
  useWithdrawApplication,
  useUpdateApplication,
} from "@/hooks/candidate/applications.hooks";
import { ArrowLeft, Calendar, User, FileText, Clock } from "lucide-react";
import { getStatusMeta } from "@/constants/applicationStatus";
import { formatDateTimeToLocal } from "@/utils/dateUtils";
import { getErrorMessage } from "@/utils/error";

export const ApplicationDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const applicationQuery = useCandidateApplicationById(id);
  const withdrawMutation = useWithdrawApplication();
  const updateMutation = useUpdateApplication();
  const [isWithdrawDialogOpen, setIsWithdrawDialogOpen] = useState(false);
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
  const [coverLetter, setCoverLetter] = useState("");

  const application = applicationQuery.data;

  const canWithdraw =
    application?.status &&
    [1, 2, 3, 4, 5, 6, 7, 11].includes(application.status);
  const canEditCoverLetter =
    application?.status && [1, 2, 3, 4, 11].includes(application.status);

  const handleWithdraw = async () => {
    if (!id) return;
    try {
      const response = await withdrawMutation.mutateAsync(id);
      setIsWithdrawDialogOpen(false);
      // Could add a toast notification here with response.message
      console.log("Application withdrawn:", response.message);
    } catch (error) {
      setIsWithdrawDialogOpen(false);
      console.error("Failed to withdraw application:", getErrorMessage(error));
    }
  };

  const handleEditCoverLetter = () => {
    setCoverLetter(application?.coverLetter || "");
    setIsEditDialogOpen(true);
  };

  const handleSaveCoverLetter = async () => {
    if (!id) return;
    try {
      const response = await updateMutation.mutateAsync({
        applicationId: id,
        data: { coverLetter: coverLetter || null },
      });
      setIsEditDialogOpen(false);
      // Could add a toast notification here with response.message
      console.log("Cover letter updated:", response.message);
    } catch (error) {
      console.error("Failed to update cover letter:", getErrorMessage(error));
    }
  };

  if (applicationQuery.isLoading) {
    return (
      <ProfileRequiredWrapper>
        <div className="space-y-6 flex justify-center py-10">
          <LoadingSpinner />
        </div>
      </ProfileRequiredWrapper>
    );
  }

  if (applicationQuery.isError || !application) {
    return (
      <ProfileRequiredWrapper>
        <div className="space-y-6">
          <Card className="border-destructive/30 bg-destructive/5">
            <CardContent className="py-6">
              <p className="text-destructive font-medium text-center">
                {getErrorMessage(applicationQuery.error)}
              </p>
              <Button variant="outline" size="sm" className="mt-4" asChild>
                <Link to="/candidate/applications">Back to Applications</Link>
              </Button>
            </CardContent>
          </Card>
        </div>
      </ProfileRequiredWrapper>
    );
  }

  const statusInfo = getStatusMeta(application.status);

  return (
    <ProfileRequiredWrapper>
      <div className="space-y-6">
        <div className="mb-6">
          <Button variant="ghost" asChild className="mb-4">
            <Link to="/candidate/applications">
              <ArrowLeft className="w-4 h-4 mr-2" />
              Back to Applications
            </Link>
          </Button>
          <h1 className="text-3xl font-bold text-foreground">
            {application.jobPosition?.title}
          </h1>
          <p className="text-muted-foreground mt-2">Application Details</p>
        </div>

        <div className="grid gap-6 lg:grid-cols-3">
          <div className="lg:col-span-2 space-y-6">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <FileText className="w-5 h-5" />
                  Application Information
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="text-sm font-medium text-foreground">
                      Job Position
                    </label>
                    <p className="text-foreground">
                      {application.jobPosition?.title}
                    </p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-foreground">
                      Applied Date
                    </label>
                    <p className="text-foreground flex items-center gap-2">
                      <Calendar className="w-4 h-4" />
                      {formatDateTimeToLocal(application.appliedDate)}
                    </p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-foreground">
                      Status
                    </label>
                    <div className="mt-1">
                      <Badge variant={statusInfo.variant}>
                        {statusInfo.label}
                      </Badge>
                    </div>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-foreground">
                      Assigned Recruiter
                    </label>
                    <p className="text-foreground flex items-center gap-2">
                      <User className="w-4 h-4" />
                      {application.assignedRecruiterName || "Not assigned"}
                    </p>
                  </div>
                  {application.testScore && (
                    <div>
                      <label className="text-sm font-medium text-foreground">
                        Test Score
                      </label>
                      <p className="text-foreground">
                        {application.testScore}%
                      </p>
                    </div>
                  )}
                  {application.testCompletedAt && (
                    <div>
                      <label className="text-sm font-medium text-foreground">
                        Test Completed
                      </label>
                      <p className="text-foreground">
                        {formatDateTimeToLocal(application.testCompletedAt)}
                      </p>
                    </div>
                  )}
                  {application.rejectionReason && (
                    <div className="md:col-span-2">
                      <label className="text-sm font-medium text-foreground">
                        Rejection Reason
                      </label>
                      <p className="text-foreground bg-red-50 p-3 rounded border-l-4 border-red-400">
                        {application.rejectionReason}
                      </p>
                    </div>
                  )}
                </div>
                {application.coverLetter && (
                  <div>
                    <label className="text-sm font-medium text-foreground">
                      Cover Letter
                    </label>
                    <p className="text-foreground mt-1 p-3 bg-muted rounded whitespace-pre-wrap">
                      {application.coverLetter}
                    </p>
                  </div>
                )}
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Job Position Details</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="text-sm font-medium text-foreground">
                      Department
                    </label>
                    <p className="text-foreground">
                      {application.jobPosition?.department || "—"}
                    </p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-foreground">
                      Location
                    </label>
                    <p className="text-foreground">
                      {application.jobPosition?.location || "—"}
                    </p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-foreground">
                      Employment Type
                    </label>
                    <p className="text-foreground">
                      {application.jobPosition?.employmentType || "—"}
                    </p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-foreground">
                      Experience Level
                    </label>
                    <p className="text-foreground">
                      {application.jobPosition?.experienceLevel || "—"}
                    </p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-foreground">
                      Salary Range
                    </label>
                    <p className="text-foreground">
                      {application.jobPosition?.salaryRange || "—"}
                    </p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-foreground">
                      Min Experience
                    </label>
                    <p className="text-foreground">
                      {application.jobPosition?.minExperience === 0
                        ? "Fresher"
                        : application.jobPosition?.minExperience
                        ? `${application.jobPosition.minExperience} years`
                        : "—"}
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>

            {application.jobOffer && (
              <Card>
                <CardHeader>
                  <CardTitle>Job Offer</CardTitle>
                </CardHeader>
                <CardContent className="space-y-3">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <label className="text-sm font-medium text-foreground">
                        Offered Salary
                      </label>
                      <p className="text-foreground font-semibold text-lg">
                        ₹
                        {application.jobOffer.offeredSalary?.toLocaleString(
                          "en-IN"
                        ) || "—"}
                      </p>
                    </div>
                    <div>
                      <label className="text-sm font-medium text-foreground">
                        Offer Status
                      </label>
                      <p className="text-foreground">
                        {application.jobOffer.offerStatus}
                      </p>
                    </div>
                    <div>
                      <label className="text-sm font-medium text-foreground">
                        Offer Date
                      </label>
                      <p className="text-foreground">
                        {formatDateTimeToLocal(application.jobOffer.offerDate)}
                      </p>
                    </div>
                    <div>
                      <label className="text-sm font-medium text-foreground">
                        Expiry Date
                      </label>
                      <p className="text-foreground">
                        {application.jobOffer.expiryDate
                          ? formatDateTimeToLocal(
                              application.jobOffer.expiryDate
                            )
                          : "—"}
                      </p>
                    </div>
                    {application.jobOffer.responseDate && (
                      <div>
                        <label className="text-sm font-medium text-foreground">
                          Response Date
                        </label>
                        <p className="text-foreground">
                          {formatDateTimeToLocal(
                            application.jobOffer.responseDate
                          )}
                        </p>
                      </div>
                    )}
                  </div>
                </CardContent>
              </Card>
            )}

            {application.statusHistory &&
              application.statusHistory.length > 0 && (
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <Clock className="w-5 h-5" />
                      Status History
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-3">
                      {application.statusHistory.map((history, index) => (
                        <div
                          key={index}
                          className="flex items-start gap-3 pb-3 border-b border-gray-100 last:border-b-0 last:pb-0"
                        >
                          <div className="w-2 h-2 bg-blue-500 rounded-full mt-2 shrink-0"></div>
                          <div className="flex-1">
                            <div className="flex items-center gap-2 mb-1">
                              <Badge variant="outline" className="text-xs">
                                {getStatusMeta(history.fromStatus).label}
                              </Badge>
                              <span className="text-muted-foreground">→</span>
                              <Badge variant="outline" className="text-xs">
                                {getStatusMeta(history.toStatus).label}
                              </Badge>
                              <span className="text-xs text-gray-500">
                                {formatDateTimeToLocal(history.changedAt)}
                              </span>
                            </div>
                            {history.comments && (
                              <p className="text-sm text-muted-foreground mb-2">
                                {history.comments}
                              </p>
                            )}
                            <p className="text-xs text-muted-foreground">
                              Changed by: {history.changedByName || "System"}
                            </p>
                          </div>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>
              )}
          </div>

          <div className="space-y-6">
            <Card>
              <CardHeader>
                <CardTitle>Quick Actions</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                {application.status === 9 && (
                  <Button className="w-full" variant="outline" asChild>
                    <Link to="/candidate/jobs">Browse Similar Jobs</Link>
                  </Button>
                )}
                {application.status === 8 && (
                  <Button className="w-full" variant="outline" asChild>
                    <Link to="/candidate/offers">View Your Offers</Link>
                  </Button>
                )}
                <Button
                  className="w-full"
                  variant="outline"
                  disabled={updateMutation.isPending || !canEditCoverLetter}
                  onClick={handleEditCoverLetter}
                >
                  Edit Cover Letter
                </Button>
                <Button
                  className="w-full"
                  variant="destructive"
                  disabled={withdrawMutation.isPending || !canWithdraw}
                  onClick={() => setIsWithdrawDialogOpen(true)}
                >
                  Withdraw Application
                </Button>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Next Steps</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="text-sm text-muted-foreground">
                  {application.status === 1 &&
                    "Your application is being reviewed. We'll update you soon."}
                  {application.status === 2 && "Complete the test to proceed."}
                  {application.status === 4 &&
                    "Your application is under review."}
                  {application.status === 5 &&
                    "Congratulations! You're shortlisted for interviews. Prepare well."}
                  {application.status === 6 &&
                    "Prepare for your upcoming interviews."}
                  {application.status === 7 &&
                    "Great news! You're selected. Expect an offer soon."}
                  {application.status === 8 &&
                    "Welcome aboard! Check your offers for details."}
                  {application.status === 9 &&
                    "Unfortunately, this application was not successful. Keep applying!"}
                  {application.status === 10 &&
                    "You withdrew this application."}
                  {application.status === 11 &&
                    "This application is on hold. We'll resume soon."}
                </div>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>

      <Dialog
        open={isWithdrawDialogOpen}
        onOpenChange={setIsWithdrawDialogOpen}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Withdraw Application</DialogTitle>
            <DialogDescription>
              Are you sure you want to withdraw your application for{" "}
              <strong>{application?.jobPosition?.title}</strong>? This action
              cannot be undone and may affect your candidacy for this position.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsWithdrawDialogOpen(false)}
            >
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleWithdraw}
              disabled={withdrawMutation.isPending}
            >
              {withdrawMutation.isPending ? "Withdrawing..." : "Withdraw"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Edit Cover Letter</DialogTitle>
            <DialogDescription>
              Update your cover letter for the position{" "}
              <strong>{application?.jobPosition?.title}</strong>.
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <textarea
              value={coverLetter}
              onChange={(e) => setCoverLetter(e.target.value)}
              placeholder="Enter your cover letter..."
              className="w-full h-40 p-3 border border-input rounded-md bg-background text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring resize-none"
            />
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsEditDialogOpen(false)}
            >
              Cancel
            </Button>
            <Button
              onClick={handleSaveCoverLetter}
              disabled={updateMutation.isPending}
            >
              {updateMutation.isPending ? "Saving..." : "Save"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </ProfileRequiredWrapper>
  );
};
