/**
 * Utility functions for date handling
 */

/**
 * Formats a UTC date string to local date string
 * Assumes the input is in ISO format or UTC
 * @param dateString - The UTC date string from backend
 * @param options - Intl.DateTimeFormatOptions for formatting
 * @returns Formatted local date string
 */
export const formatDateToLocal = (
  dateString: string | undefined | null,
  options: Intl.DateTimeFormatOptions = {
    year: "numeric",
    month: "short",
    day: "numeric",
  }
): string => {
  if (!dateString) return "N/A";

  try {
    // Ensure the date string is treated as UTC
    const utcDate = dateString.includes("Z") ? dateString : `${dateString}Z`;
    const date = new Date(utcDate);

    // Check if date is valid
    if (isNaN(date.getTime())) return "Invalid Date";

    return date.toLocaleDateString(undefined, options);
  } catch {
    return "Invalid Date";
  }
};

/**
 * Formats a UTC date string to local date and time string
 * @param dateString - The UTC date string from backend
 * @param options - Intl.DateTimeFormatOptions for formatting
 * @returns Formatted local date and time string
 */
export const formatDateTimeToLocal = (
  dateString: string | undefined | null,
  options: Intl.DateTimeFormatOptions = {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }
): string => {
  if (!dateString) return "N/A";

  try {
    // Ensure the date string is treated as UTC
    const utcDate = dateString.includes("Z") ? dateString : `${dateString}Z`;
    const date = new Date(utcDate);

    // Check if date is valid
    if (isNaN(date.getTime())) return "Invalid Date";

    return date.toLocaleString(undefined, options);
  } catch {
    return "Invalid Date";
  }
};
