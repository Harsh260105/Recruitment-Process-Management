import { apiClient } from "./apiClient";
import type { components } from "../types/api";
import type { ApiResponse } from "../types/http";

type Schemas = components["schemas"];
type ApiResult<T> = Promise<ApiResponse<T>>;

class CandidateService {
  getCandidateProfile(
    id: string
  ): ApiResult<Schemas["CandidateProfileResponseDto"]> {
    return apiClient.get<Schemas["CandidateProfileResponseDto"]>(
      `/api/CandidateProfile/${id}`
    );
  }

  getCandidateProfileByUserId(
    userId: string
  ): ApiResult<Schemas["CandidateProfileResponseDto"]> {
    return apiClient.get<Schemas["CandidateProfileResponseDto"]>(
      `/api/CandidateProfile/user/${userId}`
    );
  }

  getMyProfile(): ApiResult<Schemas["CandidateProfileResponseDto"]> {
    return apiClient.get<Schemas["CandidateProfileResponseDto"]>(
      "/api/CandidateProfile/my-profile"
    );
  }

  createProfile(
    data: Schemas["CandidateProfileDto"]
  ): ApiResult<Schemas["CandidateProfileResponseDto"]> {
    return apiClient.post<Schemas["CandidateProfileResponseDto"]>(
      "/api/CandidateProfile",
      data
    );
  }

  updateProfile(
    candidateProfileId: string,
    data: Schemas["UpdateCandidateProfileDto"]
  ): ApiResult<Schemas["CandidateProfileResponseDto"]> {
    return apiClient.patch<Schemas["CandidateProfileResponseDto"]>(
      `/api/CandidateProfile/${candidateProfileId}`,
      data
    );
  }

  deleteProfile(id: string): ApiResult<void> {
    return apiClient.delete<void>(`/api/CandidateProfile/${id}`);
  }

  // region: skills ---------------------------------------------------------

  getMySkills(): ApiResult<Schemas["CandidateSkillDto"][]> {
    return apiClient.get<Schemas["CandidateSkillDto"][]>(
      "/api/CandidateProfile/my-skills"
    );
  }

  addSkills(
    skills: Schemas["CreateCandidateSkillDto"][]
  ): ApiResult<Schemas["CandidateSkillDto"][]> {
    return apiClient.post<Schemas["CandidateSkillDto"][]>(
      "/api/CandidateProfile/my-skills",
      skills
    );
  }

  updateSkill(
    skillId: number,
    data: Schemas["UpdateCandidateSkillDto"]
  ): ApiResult<Schemas["CandidateSkillDto"]> {
    return apiClient.patch<Schemas["CandidateSkillDto"]>(
      `/api/CandidateProfile/my-skills/${skillId}`,
      data
    );
  }

  deleteSkill(skillId: number): ApiResult<void> {
    return apiClient.delete<void>(`/api/CandidateProfile/my-skills/${skillId}`);
  }

  // endregion --------------------------------------------------------------

  // region: education ------------------------------------------------------

  getMyEducation(): ApiResult<Schemas["CandidateEducationDto"][]> {
    return apiClient.get<Schemas["CandidateEducationDto"][]>(
      "/api/CandidateProfile/my-education"
    );
  }

  addEducation(
    data: Schemas["CreateCandidateEducationDto"]
  ): ApiResult<Schemas["CandidateEducationDto"]> {
    return apiClient.post<Schemas["CandidateEducationDto"]>(
      "/api/CandidateProfile/my-education",
      data
    );
  }

  updateEducation(
    educationId: string,
    data: Schemas["UpdateCandidateEducationDto"]
  ): ApiResult<Schemas["CandidateEducationDto"]> {
    return apiClient.patch<Schemas["CandidateEducationDto"]>(
      `/api/CandidateProfile/my-education/${educationId}`,
      data
    );
  }

  deleteEducation(educationId: string): ApiResult<void> {
    return apiClient.delete<void>(
      `/api/CandidateProfile/my-education/${educationId}`
    );
  }

  // endregion --------------------------------------------------------------

  // region: work experience ------------------------------------------------

  getMyWorkExperience(): ApiResult<Schemas["CandidateWorkExperienceDto"][]> {
    return apiClient.get<Schemas["CandidateWorkExperienceDto"][]>(
      "/api/CandidateProfile/my-work-experience"
    );
  }

  addWorkExperience(
    data: Schemas["CreateCandidateWorkExperienceDto"]
  ): ApiResult<Schemas["CandidateWorkExperienceDto"]> {
    return apiClient.post<Schemas["CandidateWorkExperienceDto"]>(
      "/api/CandidateProfile/my-work-experience",
      data
    );
  }

  updateWorkExperience(
    workExperienceId: string,
    data: Schemas["UpdateCandidateWorkExperienceDto"]
  ): ApiResult<Schemas["CandidateWorkExperienceDto"]> {
    return apiClient.patch<Schemas["CandidateWorkExperienceDto"]>(
      `/api/CandidateProfile/my-work-experience/${workExperienceId}`,
      data
    );
  }

  deleteWorkExperience(workExperienceId: string): ApiResult<void> {
    return apiClient.delete<void>(
      `/api/CandidateProfile/my-work-experience/${workExperienceId}`
    );
  }

  // endregion --------------------------------------------------------------

  getMyResume(): ApiResult<string> {
    return apiClient.get<string>("/api/CandidateProfile/my-resume");
  }

  getCandidateResume(candidateId: string): ApiResult<string> {
    return apiClient.get<string>(`/api/CandidateProfile/${candidateId}/resume`);
  }

  async uploadResume(
    file: File
  ): ApiResult<Schemas["CandidateProfileResponseDto"]> {
    const formData = new FormData();
    formData.append("file", file);
    return apiClient.postForm<Schemas["CandidateProfileResponseDto"]>(
      "/api/CandidateProfile/my-resume",
      formData
    );
  }

  deleteResume(): ApiResult<void> {
    return apiClient.delete<void>("/api/CandidateProfile/my-resume");
  }

  setApplicationOverride(
    candidateProfileId: string,
    payload: Schemas["CandidateApplicationOverrideRequestDto"]
  ): ApiResult<Schemas["CandidateProfileResponseDto"]> {
    return apiClient.post<Schemas["CandidateProfileResponseDto"]>(
      `/api/CandidateProfile/${candidateProfileId}/application-override`,
      payload
    );
  }
}

export const candidateService = new CandidateService();
