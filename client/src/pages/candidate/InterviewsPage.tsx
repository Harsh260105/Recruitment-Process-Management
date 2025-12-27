import { useState } from "react";
import { Link } from "react-router-dom";
import InfiniteScroll from "react-infinite-scroll-component";
import { ProfileRequiredWrapper } from "@/components/common/ProfileRequiredWrapper";

import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

import { Badge } from "@/components/ui/badge";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { formatDateToLocal } from "@/utils/dateUtils";

import {
  useCandidateUpcomingInterviews,
  useCandidateInterviewHistoryInfinite,
} from "@/hooks/candidate/interviews.hooks";
import { getErrorMessage } from "@/utils/error";

const interviewModeMap = {
  1: "In-Person",
  2: "Online",
  3: "Phone",
};

const interviewTypeMap = {
  1: "Screening",
  2: "Technical",
  3: "Behavioral",
  4: "Managerial",
  5: "Cultural",
  6: "Final",
  7: "Panel",
};

const interviewStatusMap = {
  1: "Scheduled",
  2: "Completed",
  3: "Cancelled",
  4: "No-Show",
};

export const CandidateInterviewsPage = () => {
  const [upcomingInterviewDays, setUpcomingInterviewDays] = useState(7);

  const upcomingInterviewsQuery = useCandidateUpcomingInterviews({
    days: upcomingInterviewDays,
    pageSize: 10,
  });
  const interviewHistoryQuery = useCandidateInterviewHistoryInfinite({
    pageSize: 10,
  });

  const allUpcomingInterviews = upcomingInterviewsQuery.data?.items || [];
  const allHistoryInterviews =
    (interviewHistoryQuery.data as any)?.pages?.flatMap(
      (page: any) => page.items || []
    ) || [];

  return (
    <ProfileRequiredWrapper>
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold">Interviews</h1>
          <p className="text-muted-foreground">
            View and manage your upcoming and past interviews.
          </p>
        </div>

        {/* Upcoming Interviews Section */}
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <div>
                <CardTitle>Upcoming Interviews</CardTitle>
                <CardDescription>Your scheduled interviews.</CardDescription>
              </div>
              <div className="flex items-center gap-2">
                <span className="text-sm text-muted-foreground">
                  Days ahead:
                </span>
                <Select
                  value={upcomingInterviewDays.toString()}
                  onValueChange={(value) =>
                    setUpcomingInterviewDays(parseInt(value))
                  }
                >
                  <SelectTrigger className="w-24">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="1">1</SelectItem>
                    <SelectItem value="7">7</SelectItem>
                    <SelectItem value="14">14</SelectItem>
                    <SelectItem value="30">30</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            {upcomingInterviewsQuery.isLoading ? (
              <div className="flex justify-center py-10">
                <LoadingSpinner />
              </div>
            ) : upcomingInterviewsQuery.isError ? (
              <p className="text-red-500">
                {getErrorMessage(upcomingInterviewsQuery.error)}
              </p>
            ) : !allUpcomingInterviews.length ? (
              <div className="text-center py-12">
                <div className="text-4xl mb-4">📅</div>
                <h3 className="text-lg font-semibold mb-2">
                  No Upcoming Interviews
                </h3>
                <p className="text-muted-foreground">
                  You have no upcoming interviews scheduled in the upcoming{" "}
                  {upcomingInterviewDays} days.
                </p>
              </div>
            ) : (
              <div className="space-y-4">
                {allUpcomingInterviews.map((interview: any) => (
                  <Card
                    key={interview.id}
                    className="hover:shadow-lg transition-shadow"
                  >
                    <CardContent className="p-4">
                      <div className="flex justify-between items-start">
                        <div className="space-y-1">
                          <h4 className="font-semibold">{interview.title}</h4>
                          <p className="text-sm text-muted-foreground">
                            {formatDateToLocal(interview.scheduledDateTime)}
                          </p>
                          <p className="text-sm">
                            Mode:{" "}
                            {interviewModeMap[
                              interview.mode as keyof typeof interviewModeMap
                            ] || interview.mode}
                          </p>
                          <p className="text-sm">
                            Type:{" "}
                            {interviewTypeMap[
                              interview.interviewType as keyof typeof interviewTypeMap
                            ] || "Unknown"}
                          </p>
                          <p className="text-sm">
                            Round: {interview.roundNumber || "N/A"}
                          </p>
                          <p className="text-sm text-muted-foreground">
                            Application ID:{" "}
                            {interview.jobApplicationId || "N/A"}
                          </p>
                        </div>
                        <Link
                          to={`/candidate/interviews/${interview.id}`}
                          className="text-sm text-blue-600 hover:underline mt-2 block"
                        >
                          View Details →
                        </Link>
                      </div>
                      <Badge variant="outline">Upcoming</Badge>
                    </CardContent>
                  </Card>
                ))}
              </div>
            )}
          </CardContent>
        </Card>

        {/* Interview History Section */}
        <Card>
          <CardHeader>
            <CardTitle>Interview History</CardTitle>
            <CardDescription>Your past interviews.</CardDescription>
          </CardHeader>
          <CardContent>
            {interviewHistoryQuery.isLoading ? (
              <div className="flex justify-center py-10">
                <LoadingSpinner />
              </div>
            ) : interviewHistoryQuery.isError ? (
              <p className="text-red-500">
                {getErrorMessage(interviewHistoryQuery.error)}
              </p>
            ) : !allHistoryInterviews.length ? (
              <div className="text-center py-12">
                <div className="text-4xl mb-4">📋</div>
                <h3 className="text-lg font-semibold mb-2">
                  No Interview History
                </h3>
                <p className="text-muted-foreground">
                  You haven't participated in any interviews yet.
                </p>
              </div>
            ) : (
              <InfiniteScroll
                dataLength={allHistoryInterviews.length}
                next={interviewHistoryQuery.fetchNextPage}
                hasMore={!!interviewHistoryQuery.hasNextPage}
                loader={<LoadingSpinner />}
                endMessage={
                  <p className="text-center text-muted-foreground py-4">
                    No more past interviews.
                  </p>
                }
              >
                <div className="space-y-4">
                  {allHistoryInterviews.map((interview: any) => (
                    <Card
                      key={interview.id}
                      className="hover:shadow-lg transition-shadow"
                    >
                      <CardContent className="p-4">
                        <div className="flex justify-between items-start">
                          <div className="space-y-1">
                            <h4 className="font-semibold">{interview.title}</h4>
                            <p className="text-sm text-muted-foreground">
                              {formatDateToLocal(interview.scheduledDateTime)}
                            </p>
                            <p className="text-sm">
                              Mode:{" "}
                              {interviewModeMap[
                                interview.mode as keyof typeof interviewModeMap
                              ] || interview.mode}
                            </p>
                            <p className="text-sm">
                              Type:{" "}
                              {interviewTypeMap[
                                interview.interviewType as keyof typeof interviewTypeMap
                              ] || "Unknown"}
                            </p>
                            <p className="text-sm">
                              Round: {interview.roundNumber || "N/A"}
                            </p>
                            <p className="text-sm text-muted-foreground">
                              Application ID:{" "}
                              {interview.jobApplicationId || "N/A"}
                            </p>
                          </div>
                          <Link
                            to={`/candidate/interviews/${interview.id}`}
                            className="text-sm text-blue-600 hover:underline mt-2 block"
                          >
                            View Details →
                          </Link>
                        </div>
                        <Badge variant="secondary">
                          {interviewStatusMap[
                            interview.status as keyof typeof interviewStatusMap
                          ] || interview.status}
                        </Badge>
                      </CardContent>
                    </Card>
                  ))}
                </div>
              </InfiniteScroll>
            )}
          </CardContent>
        </Card>
      </div>
    </ProfileRequiredWrapper>
  );
};
