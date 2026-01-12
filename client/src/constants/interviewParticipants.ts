export const participantRoleLabels: Record<number, string> = {
  1: "Primary Interviewer",
  2: "Interviewer",
  3: "Observer",
  4: "Shadow",
};

export const getParticipantRoleLabel = (role?: number) => {
  return participantRoleLabels[role ?? 0] ?? "Unknown";
};
