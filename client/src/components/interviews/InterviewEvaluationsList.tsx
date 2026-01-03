import {
  useAllInterviewEvaluations,
  useInterviewAverageScore,
} from "@/hooks/staff/interviews.hooks";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { Star, User } from "lucide-react";
import type { components } from "@/types/api";
import { getEvaluationRecommendationMeta } from "@/constants/interviewEvaluations";
import { formatDateToLocal } from "@/utils/dateUtils";

type Schemas = components["schemas"];

interface InterviewEvaluationsListProps {
  interviewId: string;
}

export const InterviewEvaluationsList = ({
  interviewId,
}: InterviewEvaluationsListProps) => {
  const evaluationsQuery = useAllInterviewEvaluations(interviewId);
  const averageScoreQuery = useInterviewAverageScore(interviewId);

  if (evaluationsQuery.isLoading) {
    return (
      <div className="flex justify-center py-4">
        <LoadingSpinner />
      </div>
    );
  }

  if (evaluationsQuery.isError) {
    return (
      <p className="text-sm text-destructive">
        Failed to load evaluations. Please try again.
      </p>
    );
  }

  const evaluations = evaluationsQuery.data || [];

  return (
    <div className="space-y-4">
      {/* Average Score */}
      {averageScoreQuery.data !== undefined && (
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium">Average Score</p>
                <div className="flex items-center gap-2 mt-1">
                  <div className="flex items-center gap-1">
                    {[1, 2, 3, 4, 5].map((star) => (
                      <Star
                        key={star}
                        className={`h-4 w-4 ${
                          star <= Math.round(averageScoreQuery.data || 0)
                            ? "fill-yellow-400 text-yellow-400"
                            : "text-gray-300"
                        }`}
                      />
                    ))}
                  </div>
                  <span className="text-lg font-semibold">
                    {averageScoreQuery.data.toFixed(1)}/5
                  </span>
                </div>
              </div>
              <div className="text-right">
                <p className="text-sm text-muted-foreground">
                  Total Evaluations
                </p>
                <p className="text-2xl font-bold">{evaluations.length}</p>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Individual Evaluations */}
      {evaluations.length > 0 ? (
        <div className="space-y-3">
          {evaluations.map(
            (evaluation: Schemas["InterviewEvaluationResponseDto"]) => (
              <Card key={evaluation.id}>
                <CardHeader className="pb-3">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <User className="h-4 w-4" />
                      <span className="text-sm font-medium">
                        {evaluation.evaluatorUserName || "Anonymous"}
                      </span>
                    </div>
                    <Badge
                      variant={
                        getEvaluationRecommendationMeta(
                          evaluation.recommendation
                        ).variant
                      }
                    >
                      {
                        getEvaluationRecommendationMeta(
                          evaluation.recommendation
                        ).label
                      }
                    </Badge>
                  </div>
                </CardHeader>
                <CardContent className="space-y-4">
                  {/* Overall Rating */}
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium">Overall:</span>
                    <div className="flex items-center gap-1">
                      {[1, 2, 3, 4, 5].map((star) => (
                        <Star
                          key={star}
                          className={`h-4 w-4 ${
                            star <= (evaluation.overallRating || 0)
                              ? "fill-yellow-400 text-yellow-400"
                              : "text-gray-300"
                          }`}
                        />
                      ))}
                      <span className="text-sm text-muted-foreground ml-1">
                        {evaluation.overallRating ?? "–"}/5
                      </span>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-4 text-sm">
                    <div className="flex justify-between">
                      <span>Overall Rating:</span>
                      <span className="font-medium">
                        {evaluation.overallRating ?? "Not rated"}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span>Recommendation:</span>
                      <span className="font-medium">
                        {
                          getEvaluationRecommendationMeta(
                            evaluation.recommendation
                          ).label
                        }
                      </span>
                    </div>
                  </div>

                  <div>
                    <span className="text-sm font-medium">Strengths:</span>
                    <p className="text-sm text-muted-foreground mt-1">
                      {evaluation.strengths || "Not provided"}
                    </p>
                  </div>

                  <div>
                    <span className="text-sm font-medium">Concerns:</span>
                    <p className="text-sm text-muted-foreground mt-1">
                      {evaluation.concerns || "Not provided"}
                    </p>
                  </div>

                  {evaluation.additionalComments && (
                    <div>
                      <span className="text-sm font-medium">
                        Additional Comments:
                      </span>
                      <p className="text-sm text-muted-foreground mt-1">
                        {evaluation.additionalComments}
                      </p>
                    </div>
                  )}

                  {/* Timestamp */}
                  <div className="text-xs text-muted-foreground pt-2 border-t">
                    Evaluated on{" "}
                    {evaluation.createdAt
                      ? formatDateToLocal(evaluation.createdAt)
                      : "Unknown date"}
                  </div>
                </CardContent>
              </Card>
            )
          )}
        </div>
      ) : (
        <Card>
          <CardContent className="pt-6">
            <p className="text-sm text-muted-foreground text-center py-4">
              No evaluations submitted yet.
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  );
};
