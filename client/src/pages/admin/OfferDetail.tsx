import { useState } from "react";
import {
  useGetOffer,
  useWithdrawOffer,
  useExtendOfferExpiry,
  useReviseOffer,
  useMarkOfferExpired,
  useRespondToCounterOffer,
} from "@/hooks/staff/jobOffer.hooks";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import {
  Card,
  CardContent,
  CardTitle,
  CardHeader,
  CardDescription,
} from "@/components/ui/card";
import { useParams, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { getErrorMessage } from "@/utils/error";
import { convertLocalDateToUTC } from "@/utils/dateUtils";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { formatDateToLocal } from "@/utils/dateUtils";
import {
  CheckCircle2,
  XCircle,
  Clock,
  DollarSign,
  Calendar,
  User,
} from "lucide-react";
import { OFFER_STATUS_MAP } from "@/constants/offer.Status";

const OfferDetail = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [actionFeedback, setActionFeedback] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [newExpiry, setNewExpiry] = useState("");
  const [expiryExtendReason, setExtendExpiryReason] = useState("");
  const [withdrawnReason, setWithdrawnReason] = useState("");

  // For revising offer
  const [newSalary, setNewSalary] = useState<number | undefined>();
  const [newBenefits, setNewBenefits] = useState<string | undefined>("");
  const [newJoiningDate, setNewJoiningDate] = useState<string | undefined>("");

  // For responding to counter offer
  const [counterAccepted, setCounterAccepted] = useState<boolean>(false);
  const [revisedSalary, setRevisedSalary] = useState<number | undefined>();
  const [counterResponse, setCounterResponse] = useState<string>("");

  const withdrawOfferMutation = useWithdrawOffer();
  const extendOfferExpiryMutation = useExtendOfferExpiry();
  const reviseOfferMutation = useReviseOffer();
  const markOfferExpiredMutation = useMarkOfferExpired();
  const respondToCounterOfferMutation = useRespondToCounterOffer();

  const isWithdrawingOffer = withdrawOfferMutation.isPending;
  const isExtendingOfferExpiry = extendOfferExpiryMutation.isPending;
  const isRevisingOffer = reviseOfferMutation.isPending;
  const isMarkingOfferExpired = markOfferExpiredMutation.isPending;
  const isRespondingToCounter = respondToCounterOfferMutation.isPending;

  const runAction = async (
    executor: () => Promise<{
      success: boolean;
      message?: string | null;
      data?: any;
      errors?: string[] | null;
    }>,
    fallbackMessage: string,
    afterSuccess: () => void
  ) => {
    setActionError(null);
    setActionFeedback(null);

    try {
      const response = await executor();
      afterSuccess?.();
      setActionFeedback(response.message || fallbackMessage);
    } catch (error) {
      setActionError(getErrorMessage(error));
    }
  };

  if (!id) {
    return (
      <div className="space-y-4">
        <p className="test-sm text-muted-foreground">
          Missing offer identifier.
        </p>
        <Button onClick={() => navigate(-1)}>Go Back</Button>
      </div>
    );
  }

  const offerQuery = useGetOffer(id ?? "");
  const offer = offerQuery.data;

  const handleExtendOfferExpiry = async () => {
    if (!offer?.id) return;
    const offerId = offer.id;
    await runAction(
      () =>
        extendOfferExpiryMutation.mutateAsync({
          id: offerId,
          newExpiryDate: convertLocalDateToUTC(newExpiry),
          reason: expiryExtendReason.trim() || undefined,
        }),
      "Offer expiry extended successfully.",
      () => setActionFeedback(null)
    );
  };

  const handleWithdrawOffer = async () => {
    if (!offer?.id) return;
    const offerId = offer.id;
    await runAction(
      () =>
        withdrawOfferMutation.mutateAsync({
          id: offerId,
          reason: withdrawnReason.trim() || undefined,
        }),
      "Offer withdrawn successfully.",
      () => setActionFeedback(null)
    );
  };

  const handleReviseOffer = async () => {
    if (!offer?.id || !newSalary) return;
    const offerId = offer.id;
    await runAction(
      () =>
        reviseOfferMutation.mutateAsync({
          id: offerId,
          newSalary,
          newBenefits: newBenefits || undefined,
          newJoiningDate: newJoiningDate
            ? convertLocalDateToUTC(newJoiningDate)
            : undefined,
        }),
      "Offer revised successfully.",
      () => setActionFeedback(null)
    );
  };

  const handleMarkOfferExpired = async () => {
    if (!offer?.id) return;
    const offerId = offer.id;
    await runAction(
      () => markOfferExpiredMutation.mutateAsync(offerId),
      "Offer marked as expired successfully.",
      () => setActionFeedback(null)
    );
  };

  const handleRespondToCounterOffer = async () => {
    if (!offer?.id) return;
    const offerId = offer.id;
    await runAction(
      () =>
        respondToCounterOfferMutation.mutateAsync({
          id: offerId,
          accepted: counterAccepted,
          revisedSalary: counterAccepted ? revisedSalary : undefined,
          response: counterResponse.trim() || undefined,
        }),
      counterAccepted
        ? "Counter offer accepted successfully."
        : "Counter offer rejected successfully.",
      () => setActionFeedback(null)
    );
  };

  if (offerQuery.isLoading) {
    return (
      <div className="flex justify-center py-10">
        <LoadingSpinner></LoadingSpinner>
      </div>
    );
  }

  if (offerQuery.isError) {
    return (
      <div className="space-y-4">
        <p className="rounded-md border border-destructive/40 bg-destructive/5 p-3 text-sm text-destructive">
          Unable to load this offer. Please try again later.
        </p>
        <Button variant="outline" onClick={() => navigate(-1)}>
          Go back
        </Button>
      </div>
    );
  }

  if (!offer) {
    return (
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">
          Offer details are unavailable.
        </p>
        <Button variant="outline" onClick={() => navigate(-1)}>
          Go back
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Offer Details</h1>
          <p className="text-muted-foreground">
            Manage this job offer and perform actions.
          </p>
        </div>
        <Button variant="outline" onClick={() => navigate(-1)}>
          Back to Offers
        </Button>
      </div>

      {/* Success/Error Messages */}
      {actionFeedback && (
        <Alert className="bg-green-50 border-green-200">
          <CheckCircle2 className="h-4 w-4 text-green-600" />
          <AlertDescription className="text-green-800">
            {actionFeedback}
          </AlertDescription>
        </Alert>
      )}

      {actionError && (
        <Alert variant="destructive">
          <XCircle className="h-4 w-4" />
          <AlertDescription>{actionError}</AlertDescription>
        </Alert>
      )}

      {/* Offer Details Card */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <DollarSign className="h-5 w-5" />
                {offer.jobTitle || "Job Offer"}
              </CardTitle>
              <CardDescription>Offer ID: {offer.id}</CardDescription>
            </div>
            <Badge variant={offer.status === 1 ? "default" : "secondary"}>
              {OFFER_STATUS_MAP[offer.status || 0] || "Unknown"}
            </Badge>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <div className="flex items-center gap-2 text-sm">
                <DollarSign className="h-4 w-4 text-muted-foreground" />
                <span className="font-medium">Offered Salary:</span>
                <span>${offer.offeredSalary?.toLocaleString()}</span>
              </div>
              <div className="flex items-center gap-2 text-sm">
                <Calendar className="h-4 w-4 text-muted-foreground" />
                <span className="font-medium">Offer Date:</span>
                <span>{formatDateToLocal(offer.offerDate)}</span>
              </div>
              <div className="flex items-center gap-2 text-sm">
                <Clock className="h-4 w-4 text-muted-foreground" />
                <span className="font-medium">Expiry Date:</span>
                <span>{formatDateToLocal(offer.expiryDate)}</span>
              </div>
            </div>
            <div className="space-y-2">
              <div className="flex items-center gap-2 text-sm">
                <User className="h-4 w-4 text-muted-foreground" />
                <span className="font-medium">Extended By:</span>
                <span>{offer.extendedByUserName || "N/A"}</span>
              </div>
              {offer.joiningDate && (
                <div className="flex items-center gap-2 text-sm">
                  <Calendar className="h-4 w-4 text-muted-foreground" />
                  <span className="font-medium">Joining Date:</span>
                  <span>{formatDateToLocal(offer.joiningDate)}</span>
                </div>
              )}
              {offer.counterOfferAmount && (
                <div className="flex items-center gap-2 text-sm">
                  <DollarSign className="h-4 w-4 text-muted-foreground" />
                  <span className="font-medium">Counter Offer:</span>
                  <span>${offer.counterOfferAmount.toLocaleString()}</span>
                </div>
              )}
            </div>
          </div>

          {offer.benefits && (
            <div className="space-y-2">
              <Label className="text-sm font-medium">Benefits</Label>
              <p className="text-sm text-muted-foreground bg-muted p-3 rounded-md">
                {offer.benefits}
              </p>
            </div>
          )}

          {offer.notes && (
            <div className="space-y-2">
              <Label className="text-sm font-medium">Notes</Label>
              <p className="text-sm text-muted-foreground bg-muted p-3 rounded-md">
                {offer.notes}
              </p>
            </div>
          )}

          {offer.counterOfferNotes && (
            <div className="space-y-2">
              <Label className="text-sm font-medium">Counter Offer Notes</Label>
              <p className="text-sm text-muted-foreground bg-muted p-3 rounded-md">
                {offer.counterOfferNotes}
              </p>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Actions Card */}
      <Card>
        <CardHeader>
          <CardTitle>Actions</CardTitle>
          <CardDescription>
            Perform actions on this offer based on its current status.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex flex-wrap gap-2">
            {/* Extend Offer Expiry - Available for pending offers */}
            {(offer.status === 1 || offer.status === 4) && (
              <Dialog>
                <DialogTrigger asChild>
                  <Button variant="outline" size="sm">
                    Extend Expiry
                  </Button>
                </DialogTrigger>
                <DialogContent>
                  <DialogHeader>
                    <DialogTitle>Extend Offer Expiry</DialogTitle>
                  </DialogHeader>
                  <div className="space-y-4">
                    <div className="space-y-2">
                      <Label htmlFor="new-expiry">New Expiry Date</Label>
                      <Input
                        id="new-expiry"
                        type="datetime-local"
                        value={newExpiry}
                        onChange={(e) => setNewExpiry(e.target.value)}
                      />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="extend-reason">Reason (Optional)</Label>
                      <Textarea
                        id="extend-reason"
                        value={expiryExtendReason}
                        onChange={(e) => setExtendExpiryReason(e.target.value)}
                        placeholder="Reason for extending the expiry..."
                      />
                    </div>
                    <Button
                      onClick={handleExtendOfferExpiry}
                      disabled={isExtendingOfferExpiry || !newExpiry}
                      className="w-full"
                    >
                      {isExtendingOfferExpiry
                        ? "Extending..."
                        : "Extend Expiry"}
                    </Button>
                  </div>
                </DialogContent>
              </Dialog>
            )}

            {/* Revise Offer - Available for pending offers */}
            {(offer.status === 1 || offer.status === 4) && (
              <Dialog>
                <DialogTrigger asChild>
                  <Button variant="outline" size="sm">
                    Revise Offer
                  </Button>
                </DialogTrigger>
                <DialogContent>
                  <DialogHeader>
                    <DialogTitle>Revise Offer</DialogTitle>
                  </DialogHeader>
                  <div className="space-y-4">
                    <div className="space-y-2">
                      <Label htmlFor="revise-salary">New Salary</Label>
                      <Input
                        id="revise-salary"
                        type="number"
                        value={newSalary || ""}
                        onChange={(e) =>
                          setNewSalary(Number(e.target.value) || undefined)
                        }
                        placeholder="Enter new salary"
                      />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="revise-benefits">New Benefits</Label>
                      <Textarea
                        id="revise-benefits"
                        value={newBenefits || ""}
                        onChange={(e) =>
                          setNewBenefits(e.target.value || undefined)
                        }
                        placeholder="Enter new benefits..."
                      />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="revise-joining">New Joining Date</Label>
                      <Input
                        id="revise-joining"
                        type="date"
                        value={newJoiningDate || ""}
                        onChange={(e) =>
                          setNewJoiningDate(e.target.value || undefined)
                        }
                      />
                    </div>
                    <Button
                      onClick={handleReviseOffer}
                      disabled={isRevisingOffer || !newSalary}
                      className="w-full"
                    >
                      {isRevisingOffer ? "Revising..." : "Revise Offer"}
                    </Button>
                  </div>
                </DialogContent>
              </Dialog>
            )}

            {/* Withdraw Offer - Available for pending offers */}
            {(offer.status === 1 || offer.status === 4) && (
              <Dialog>
                <DialogTrigger asChild>
                  <Button variant="destructive" size="sm">
                    Withdraw Offer
                  </Button>
                </DialogTrigger>
                <DialogContent>
                  <DialogHeader>
                    <DialogTitle>Withdraw Offer</DialogTitle>
                  </DialogHeader>
                  <div className="space-y-4">
                    <p className="text-sm text-muted-foreground">
                      Are you sure you want to withdraw this offer? This action
                      cannot be undone.
                    </p>
                    <div className="space-y-2">
                      <Label htmlFor="withdraw-reason">Reason (Optional)</Label>
                      <Textarea
                        id="withdraw-reason"
                        value={withdrawnReason}
                        onChange={(e) => setWithdrawnReason(e.target.value)}
                        placeholder="Reason for withdrawing the offer..."
                      />
                    </div>
                    <Button
                      variant="destructive"
                      onClick={handleWithdrawOffer}
                      disabled={isWithdrawingOffer}
                      className="w-full"
                    >
                      {isWithdrawingOffer ? "Withdrawing..." : "Withdraw Offer"}
                    </Button>
                  </div>
                </DialogContent>
              </Dialog>
            )}

            {/* Respond to Counter Offer - Available for countered offers */}
            {offer.status === 4 && (
              <Dialog>
                <DialogTrigger asChild>
                  <Button variant="default" size="sm">
                    Respond to Counter
                  </Button>
                </DialogTrigger>
                <DialogContent>
                  <DialogHeader>
                    <DialogTitle>Respond to Counter Offer</DialogTitle>
                  </DialogHeader>
                  <div className="space-y-4">
                    <div className="space-y-2">
                      <Label>Response Type</Label>
                      <div className="flex gap-4">
                        <label className="flex items-center gap-2">
                          <input
                            type="radio"
                            name="counter-response"
                            checked={counterAccepted}
                            onChange={() => setCounterAccepted(true)}
                          />
                          Accept Counter
                        </label>
                        <label className="flex items-center gap-2">
                          <input
                            type="radio"
                            name="counter-response"
                            checked={!counterAccepted}
                            onChange={() => setCounterAccepted(false)}
                          />
                          Reject Counter
                        </label>
                      </div>
                    </div>
                    {counterAccepted && (
                      <div className="space-y-2">
                        <Label htmlFor="counter-salary">
                          Revised Salary (Optional)
                        </Label>
                        <Input
                          id="counter-salary"
                          type="number"
                          value={revisedSalary || ""}
                          onChange={(e) =>
                            setRevisedSalary(
                              Number(e.target.value) || undefined
                            )
                          }
                          placeholder="Enter revised salary (leave empty to use counteroffer amount)"
                        />
                      </div>
                    )}
                    <div className="space-y-2">
                      <Label htmlFor="counter-response-text">
                        Response Message
                      </Label>
                      <Textarea
                        id="counter-response-text"
                        value={counterResponse}
                        onChange={(e) => setCounterResponse(e.target.value)}
                        placeholder="Enter your response..."
                      />
                    </div>
                    <Button
                      onClick={handleRespondToCounterOffer}
                      disabled={isRespondingToCounter}
                      className="w-full"
                    >
                      {isRespondingToCounter
                        ? "Responding..."
                        : "Send Response"}
                    </Button>
                  </div>
                </DialogContent>
              </Dialog>
            )}

            {/* Mark as Expired - Available for pending offers */}
            {offer.status === 1 && (
              <Button
                variant="outline"
                size="sm"
                onClick={handleMarkOfferExpired}
                disabled={isMarkingOfferExpired}
              >
                {isMarkingOfferExpired ? "Marking..." : "Mark as Expired"}
              </Button>
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

export default OfferDetail;
