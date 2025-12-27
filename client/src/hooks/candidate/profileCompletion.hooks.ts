import { useMemo } from "react";
import { useCandidateProfile } from "./profile.hooks";

export type ProfileCompletionStatus = {
  hasProfile: boolean;
  hasSkills: boolean;
  hasEducation: boolean;
  hasExperience: boolean;
  hasResume: boolean;
  completionPercentage: number;
  missingSteps: string[];
};

export const useProfileCompletion = (): ProfileCompletionStatus => {
  const { data: profile, isLoading } = useCandidateProfile();

  return useMemo(() => {
    if (isLoading || !profile) {
      return {
        hasProfile: false,
        hasSkills: false,
        hasEducation: false,
        hasExperience: false,
        hasResume: false,
        completionPercentage: 0,
        missingSteps: ["Create your profile"],
      };
    }

    const hasProfile = !!profile.id;
    const hasSkills = (profile.skills?.length ?? 0) > 0;
    const hasEducation = (profile.education?.length ?? 0) > 0;
    const hasExperience = (profile.workExperience?.length ?? 0) > 0;
    const hasResume = !!profile.resumeFileName;

    const completedSteps = [
      hasProfile,
      hasSkills,
      hasEducation,
      hasExperience,
      hasResume,
    ].filter(Boolean).length;

    const completionPercentage = Math.round((completedSteps / 5) * 100);

    const missingSteps: string[] = [];
    if (!hasProfile) missingSteps.push("Create your profile");
    if (!hasSkills) missingSteps.push("Add skills");
    if (!hasEducation) missingSteps.push("Add education");
    if (!hasExperience) missingSteps.push("Add work experience");
    if (!hasResume) missingSteps.push("Upload resume");

    return {
      hasProfile,
      hasSkills,
      hasEducation,
      hasExperience,
      hasResume,
      completionPercentage,
      missingSteps,
    };
  }, [profile, isLoading]);
};
