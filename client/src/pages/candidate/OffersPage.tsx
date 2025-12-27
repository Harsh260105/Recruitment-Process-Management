import { useState } from "react";
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
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { formatDateToLocal } from "@/utils/dateUtils";

import {
  useCandidateOffers,
  useAcceptOffer,
  useRejectOffer,
  useCounterOffer,
} from "@/hooks/candidate/offers.hooks";
import { getErrorMessage } from "@/utils/error";
import { CheckCircle2, XCircle } from "lucide-react";
const offerStatusMap = {
  1: "Pending",
  2: "Accepted",
  3: "Rejected",
  4: "Countered",
  5: "Expired",
  6: "Withdrawn",
};

export const CandidateOffersPage = () => {
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const offersQuery = useCandidateOffers({
    pageSize: 20,
  });

  const acceptOfferMutation = useAcceptOffer();
  const rejectOfferMutation = useRejectOffer();
  const counterOfferMutation = useCounterOffer();

  const allOffers = offersQuery.data?.items || [];

  const handleAcceptOffer = async (offerId: string) => {
    setSuccessMessage(null);
    setErrorMessage(null);
    try {
      const response = await acceptOfferMutation.mutateAsync(offerId);
      setSuccessMessage(response.message || "Offer accepted successfully");
    } catch (error) {
      setErrorMessage(getErrorMessage(error) || "Failed to accept offer");
    }
  };

  const handleRejectOffer = async (offerId: string) => {
    setSuccessMessage(null);
    setErrorMessage(null);
    try {
      const response = await rejectOfferMutation.mutateAsync({ offerId });
      setSuccessMessage(response.message || "Offer rejected successfully");
    } catch (error) {
      setErrorMessage(getErrorMessage(error) || "Failed to reject offer");
    }
  };

  const handleCounterOffer = async (offerId: string) => {
    const counterAmount = prompt("Enter your counter offer amount:");
    if (counterAmount && !isNaN(Number(counterAmount))) {
      setSuccessMessage(null);
      setErrorMessage(null);
      try {
        const response = await counterOfferMutation.mutateAsync({
          offerId,
          counterAmount: Number(counterAmount),
        });
        setSuccessMessage(
          response.message || "Counter offer submitted successfully"
        );
      } catch (error) {
        setErrorMessage(
          getErrorMessage(error) || "Failed to submit counter offer"
        );
      }
    }
  };

  return (
    <ProfileRequiredWrapper>
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold">Job Offers</h1>
          <p className="text-muted-foreground">
            Review and respond to your job offers.
          </p>
        </div>

        {/* Success Message */}
        {successMessage && (
          <Alert className="bg-green-50 border-green-200">
            <CheckCircle2 className="h-4 w-4 text-green-600" />
            <AlertDescription className="text-green-800">
              {successMessage}
            </AlertDescription>
          </Alert>
        )}

        {/* Error Message */}
        {errorMessage && (
          <Alert variant="destructive">
            <XCircle className="h-4 w-4" />
            <AlertDescription>{errorMessage}</AlertDescription>
          </Alert>
        )}

        <Card>
          <CardHeader>
            <CardTitle>Your Offers</CardTitle>
            <CardDescription>
              Manage your job offers and responses.
            </CardDescription>
          </CardHeader>
          <CardContent>
            {offersQuery.isLoading ? (
              <div className="flex justify-center py-10">
                <LoadingSpinner />
              </div>
            ) : offersQuery.isError ? (
              <p className="text-red-500">
                {getErrorMessage(offersQuery.error)}
              </p>
            ) : !allOffers.length ? (
              <div className="text-center py-12">
                <div className="text-4xl mb-4">📋</div>
                <h3 className="text-lg font-semibold mb-2">No Offers Yet</h3>
                <p className="text-muted-foreground">
                  You haven't received any job offers yet.
                </p>
              </div>
            ) : (
              <div className="space-y-4">
                {allOffers.map((offer: any) => (
                  <Card
                    key={offer.id}
                    className="hover:shadow-lg transition-shadow"
                  >
                    <CardContent className="p-4">
                      <div className="flex justify-between items-start">
                        <div className="space-y-1 flex-1">
                          <h4 className="font-semibold">{offer.jobTitle}</h4>
                          <p className="text-sm text-muted-foreground">
                            Offered: ${offer.offeredSalary?.toLocaleString()}
                          </p>
                          <p className="text-sm">
                            Status:{" "}
                            <Badge variant="outline">
                              {offerStatusMap[
                                offer.status as keyof typeof offerStatusMap
                              ] || offer.status}
                            </Badge>
                          </p>
                          <p className="text-sm text-muted-foreground">
                            Offered on: {formatDateToLocal(offer.offerDate)}
                          </p>
                          <p className="text-sm text-muted-foreground">
                            Expires: {formatDateToLocal(offer.expiryDate)}
                          </p>
                          <p className="text-sm text-muted-foreground">
                            Extended by: {offer.extendedByUserName || "N/A"}
                          </p>
                        </div>
                        <div className="flex gap-2 ml-4">
                          {offer.status === 1 && ( // Pending
                            <>
                              <Button
                                size="sm"
                                variant="default"
                                onClick={() => handleAcceptOffer(offer.id)}
                                disabled={acceptOfferMutation.isPending}
                              >
                                Accept
                              </Button>
                              <Button
                                size="sm"
                                variant="outline"
                                onClick={() => handleRejectOffer(offer.id)}
                                disabled={rejectOfferMutation.isPending}
                              >
                                Reject
                              </Button>
                              <Button
                                size="sm"
                                variant="secondary"
                                onClick={() => handleCounterOffer(offer.id)}
                                disabled={counterOfferMutation.isPending}
                              >
                                Counter
                              </Button>
                            </>
                          )}
                          {offer.status === 4 && ( // Countered
                            <Badge variant="secondary">
                              Counter Offer Submitted
                            </Badge>
                          )}
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </ProfileRequiredWrapper>
  );
};
