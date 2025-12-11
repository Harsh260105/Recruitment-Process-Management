import { apiClient } from "./apiClient";
import type { components } from "../types/api";
import type { ApiResponse } from "../types/http";

type Schemas = components["schemas"];
type ApiResult<T> = Promise<ApiResponse<T>>;

class SkillService {
  getSkills(): ApiResult<Schemas["SkillDto"][]> {
    return apiClient.get<Schemas["SkillDto"][]>("/api/skills");
  }
}

export const skillService = new SkillService();
