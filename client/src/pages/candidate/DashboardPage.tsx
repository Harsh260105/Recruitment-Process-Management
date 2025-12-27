import { useMemo } from "react";
import { Link } from "react-router-dom";
import {
  Briefcase,
  CalendarClock,
  FileText,
  Gift,
  CheckCircle2,
  Circle,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import {
  useCandidateApplications,
  useCandidateUpcomingInterviews,
  useCandidateOffers,
  useCandidateProfile,
} from "@/hooks/candidate";
import { useProfileCompletion } from "@/hooks/candidate";
import { formatDateToLocal, formatDateTimeToLocal } from "@/utils/dateUtils";
import { getStatusMeta } from "@/constants/applicationStatus";

const MAX_ACTIVE_APPLICATIONS = 3;
const ACTIVE_APPLICATION_STATUSES = new Set([1, 2, 3, 4, 5, 6, 7, 11]);
const PENDING_OFFER_STATUSES = new Set([1, 4]);

export const CandidateDashboardPage = () => {
  const {
    data: profile,
    isLoading: profileLoading,
    error: profileError,
  } = useCandidateProfile();

  const completion = useProfileCompletion();
  const profileId = profile?.id;

  const applicationsQuery = useCandidateApplications(profileId);
  const interviewsQuery = useCandidateUpcomingInterviews({ pageSize: 5 });
  const offersQuery = useCandidateOffers({ pageSize: 5 });

  const applications = useMemo(
    () => applicationsQuery.data ?? [],
    [applicationsQuery.data]
  );

  const interviews = useMemo(
    () => interviewsQuery.data?.items ?? [],
    [interviewsQuery.data]
  );

  const offers = useMemo(
    () => offersQuery.data?.items ?? [],
    [offersQuery.data]
  );

  const activeApplications = useMemo(() => {
    return [...applications].filter((application) =>
      ACTIVE_APPLICATION_STATUSES.has(application.status ?? 0)
    );
  }, [applications]);

  const hasReachedApplicationCap =
    activeApplications.length >= MAX_ACTIVE_APPLICATIONS;

  const hasActiveOverride =
    profile?.canBypassApplicationLimits &&
    (!profile.overrideExpiresAt ||
      new Date(profile.overrideExpiresAt) > new Date());

  const nextInterview = useMemo(() => {
    return [...interviews].filter(
      (interview) => interview.scheduledDateTime
    )[0];
  }, [interviews]);

  const pendingOfferCount = useMemo(() => {
    return offers.filter((offer) =>
      PENDING_OFFER_STATUSES.has(offer.status ?? 0)
    ).length;
  }, [offers]);

  const nextPendingOffer = useMemo(() => {
    return [...offers]
      .filter((offer) => PENDING_OFFER_STATUSES.has(offer.status ?? 0))
      .sort((a, b) => {
        const aDate = a.expiryDate ?? a.offerDate ?? "";
        const bDate = b.expiryDate ?? b.offerDate ?? "";
        return new Date(aDate).getTime() - new Date(bDate).getTime();
      })[0];
  }, [offers]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">
          Welcome back, {profile?.firstName ?? "there"}
        </h1>
        <p className="text-muted-foreground">
          {profileError
            ? "We couldn't load your profile. Please try again."
            : "Here's everything happening with your applications right now."}
        </p>
      </div>

      {hasActiveOverride ? (
        <div className="rounded-lg border border-green-200 bg-green-50 p-4 text-sm text-green-900">
          <p className="font-medium">Exception granted</p>
          <p>
            You can apply for additional roles beyond the standard limit (one
            use only). This exception expires on{" "}
            {profile.overrideExpiresAt
              ? formatDateToLocal(profile.overrideExpiresAt)
              : "an unspecified date"}
            .
          </p>
        </div>
      ) : (
        hasReachedApplicationCap && (
          <div className="rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900">
            <p className="font-medium">Application limit reached</p>
            <p>
              You already have {activeApplications.length} active applications.
              The limit is {MAX_ACTIVE_APPLICATIONS} concurrent applications. If
              it is a mistake, please contact support.
            </p>
          </div>
        )
      )}

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Profile</CardTitle>
            <FileText className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            {profileLoading ? (
              <LoadingSpinner />
            ) : (
              <div>
                <div className="flex items-baseline justify-between">
                  <p className="text-3xl font-bold">
                    {completion.completionPercentage}%
                  </p>
                  <span className="text-xs text-muted-foreground">
                    complete
                  </span>
                </div>
                <div className="mt-3 h-2 rounded-full bg-muted">
                  <div
                    className="h-full rounded-full bg-primary transition-all"
                    style={{ width: `${completion.completionPercentage}%` }}
                  />
                </div>
                <p className="mt-2 text-xs text-muted-foreground">
                  {completion.completionPercentage === 100
                    ? "Profile looks great!"
                    : "Add more details to unlock better matches."}
                </p>
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Applications</CardTitle>
            <Briefcase className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            {applicationsQuery.fetchStatus === "fetching" ? (
              <LoadingSpinner />
            ) : profileId ? (
              <div className="space-y-3">
                <div className="flex items-baseline gap-2">
                  <p className="text-3xl font-bold">
                    {activeApplications.length}
                  </p>
                  <span className="text-xs text-muted-foreground">
                    of {MAX_ACTIVE_APPLICATIONS} active slots
                  </span>
                </div>
                <p className="text-sm text-muted-foreground">
                  {applications.length} total submissions
                </p>
                <Button asChild variant="outline" size="sm">
                  <Link to="/candidate/applications">Open applications</Link>
                </Button>
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">
                Complete your profile to start applying.
              </p>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Interviews</CardTitle>
            <CalendarClock className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            {interviewsQuery.isLoading ? (
              <LoadingSpinner />
            ) : (
              <div className="space-y-2">
                <div className="flex items-baseline gap-2">
                  <p className="text-3xl font-bold">{interviews.length}</p>
                  <span className="text-xs text-muted-foreground">
                    upcoming rounds
                  </span>
                </div>
                {nextInterview ? (
                  <p className="text-sm text-muted-foreground">
                    Next: {nextInterview.title ?? "Interview"} on{" "}
                    {formatDateTimeToLocal(nextInterview.scheduledDateTime)}
                  </p>
                ) : (
                  <p className="text-sm text-muted-foreground">
                    Waiting for the next interview to be scheduled.
                  </p>
                )}
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Offers</CardTitle>
            <Gift className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            {offersQuery.isLoading ? (
              <LoadingSpinner />
            ) : (
              <div className="space-y-2">
                <div className="flex items-baseline gap-2">
                  <p className="text-3xl font-bold">{pendingOfferCount}</p>
                  <span className="text-xs text-muted-foreground">
                    pending decisions
                  </span>
                </div>
                {nextPendingOffer ? (
                  <p className="text-sm text-muted-foreground">
                    Next deadline:{" "}
                    {formatDateToLocal(nextPendingOffer.expiryDate)}
                  </p>
                ) : (
                  <p className="text-sm text-muted-foreground">
                    Offers will appear here as soon as they arrive.
                  </p>
                )}
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {(!profile || completion.completionPercentage < 100) && (
        <Card className="border-primary/20 bg-primary/5">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <FileText className="h-5 w-5" />
              Complete Your Profile
            </CardTitle>
            <p className="text-sm text-muted-foreground">
              {!profile
                ? "Get started by creating your profile to unlock all features"
                : `You're ${completion.completionPercentage}% done! Complete these steps to maximize your chances`}
            </p>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              <div className="flex items-center gap-3">
                {completion.hasProfile ? (
                  <CheckCircle2 className="h-5 w-5 flex-shrink-0 text-green-600" />
                ) : (
                  <Circle className="h-5 w-5 flex-shrink-0 text-muted-foreground" />
                )}
                <div className="flex-1">
                  <p className="text-sm font-medium">
                    Basic profile information
                  </p>
                  <p className="text-xs text-muted-foreground">
                    Add your location, experience, and contact details
                  </p>
                </div>
                {!completion.hasProfile && (
                  <Button asChild size="sm" variant="outline">
                    <Link to="/candidate/profile">Create</Link>
                  </Button>
                )}
              </div>

              <div className="flex items-center gap-3">
                {completion.hasSkills ? (
                  <CheckCircle2 className="h-5 w-5 flex-shrink-0 text-green-600" />
                ) : (
                  <Circle className="h-5 w-5 flex-shrink-0 text-muted-foreground" />
                )}
                <div className="flex-1">
                  <p className="text-sm font-medium">Skills & expertise</p>
                  <p className="text-xs text-muted-foreground">
                    List your technical and professional skills
                  </p>
                </div>
                {completion.hasProfile && !completion.hasSkills && (
                  <Button asChild size="sm" variant="outline">
                    <Link to="/candidate/skills">Add</Link>
                  </Button>
                )}
              </div>

              <div className="flex items-center gap-3">
                {completion.hasEducation ? (
                  <CheckCircle2 className="h-5 w-5 flex-shrink-0 text-green-600" />
                ) : (
                  <Circle className="h-5 w-5 flex-shrink-0 text-muted-foreground" />
                )}
                <div className="flex-1">
                  <p className="text-sm font-medium">Education history</p>
                  <p className="text-xs text-muted-foreground">
                    Add your degrees and certifications
                  </p>
                </div>
                {completion.hasProfile && !completion.hasEducation && (
                  <Button asChild size="sm" variant="outline">
                    <Link to="/candidate/education">Add</Link>
                  </Button>
                )}
              </div>

              <div className="flex items-center gap-3">
                {completion.hasExperience ? (
                  <CheckCircle2 className="h-5 w-5 flex-shrink-0 text-green-600" />
                ) : (
                  <Circle className="h-5 w-5 flex-shrink-0 text-muted-foreground" />
                )}
                <div className="flex-1">
                  <p className="text-sm font-medium">Work experience</p>
                  <p className="text-xs text-muted-foreground">
                    Showcase your career journey and achievements
                  </p>
                </div>
                {completion.hasProfile && !completion.hasExperience && (
                  <Button asChild size="sm" variant="outline">
                    <Link to="/candidate/experience">Add</Link>
                  </Button>
                )}
              </div>

              <div className="flex items-center gap-3">
                {completion.hasResume ? (
                  <CheckCircle2 className="h-5 w-5 flex-shrink-0 text-green-600" />
                ) : (
                  <Circle className="h-5 w-5 flex-shrink-0 text-muted-foreground" />
                )}
                <div className="flex-1">
                  <p className="text-sm font-medium">Resume upload</p>
                  <p className="text-xs text-muted-foreground">
                    Upload your latest resume for quick applications
                  </p>
                </div>
                {completion.hasProfile && !completion.hasResume && (
                  <Button asChild size="sm" variant="outline">
                    <Link to="/candidate/profile">Upload</Link>
                  </Button>
                )}
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle>Active applications</CardTitle>
            <Button asChild variant="ghost" size="sm">
              <Link to="/candidate/applications">View history</Link>
            </Button>
          </CardHeader>
          <CardContent>
            {applicationsQuery.fetchStatus === "fetching" ? (
              <LoadingSpinner />
            ) : !activeApplications.length ? (
              <p className="text-sm text-muted-foreground">
                No active applications yet. Explore open roles from the jobs
                section.
              </p>
            ) : (
              <div className="space-y-3">
                {activeApplications.map((application) => (
                  <div
                    key={application.id}
                    className="flex items-start justify-between rounded-lg border p-3"
                  >
                    <div>
                      <p className="font-medium">
                        {application.jobTitle ?? "Untitled role"}
                      </p>
                      <p className="text-xs text-muted-foreground">
                        Applied {formatDateToLocal(application.appliedDate)} •
                        Recruiter: {application.assignedRecruiterName ?? "—"}
                      </p>
                    </div>
                    <Badge variant={getStatusMeta(application.status).variant}>
                      {getStatusMeta(application.status).label}
                    </Badge>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle>Next steps</CardTitle>
            <Button asChild variant="ghost" size="sm">
              <Link to="/candidate/interviews">Manage schedule</Link>
            </Button>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <div className="rounded-lg border p-3">
                <p className="text-sm font-medium">Upcoming interview</p>
                {interviewsQuery.isLoading ? (
                  <LoadingSpinner />
                ) : nextInterview ? (
                  <p className="text-xs text-muted-foreground">
                    {nextInterview.title ?? "Interview"} •{" "}
                    {formatDateTimeToLocal(nextInterview.scheduledDateTime)}
                  </p>
                ) : (
                  <p className="text-xs text-muted-foreground">
                    Nothing scheduled yet. We'll notify you as soon as a round
                    is booked.
                  </p>
                )}
              </div>
              <div className="rounded-lg border p-3">
                <p className="text-sm font-medium">Pending offer</p>
                {offersQuery.isLoading ? (
                  <LoadingSpinner />
                ) : nextPendingOffer ? (
                  <p className="text-xs text-muted-foreground">
                    {nextPendingOffer.jobTitle ?? "Job offer"} • Respond by{" "}
                    {formatDateToLocal(nextPendingOffer.expiryDate)}
                  </p>
                ) : (
                  <p className="text-xs text-muted-foreground">
                    No offers awaiting your response right now.
                  </p>
                )}
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
};
