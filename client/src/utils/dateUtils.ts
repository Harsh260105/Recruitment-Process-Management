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
    let utcDate = dateString;

    if (utcDate.endsWith("Z")) {}

    else if (/[+-]\d{2}:\d{2}$/.test(utcDate)) {
      const date = new Date(utcDate);
      if (!isNaN(date.getTime())) {
        utcDate = date.toISOString();
      }
    }
    else {
      utcDate = `${utcDate}Z`;
    }

    const date = new Date(utcDate);

    if (isNaN(date.getTime())) return "Invalid Date";

    return date.toLocaleDateString(undefined, options);
  } catch {
    return "Invalid Date";
  }
};

/**
 * Converts a local datetime string to UTC ISO string for API calls
 * This ensures the backend receives the correct UTC time
 * @param localDateTimeString - The local datetime string from datetime-local input
 * @returns UTC ISO string for API
 */
export const convertLocalDateTimeToUTC = (
  localDateTimeString: string
): string => {
  if (!localDateTimeString) return "";

  try {
    const localDate = new Date(localDateTimeString);
    return localDate.toISOString();
  } catch {
    return "";
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
    let utcDate = dateString;

    if (utcDate.endsWith("Z")) {}
    
    else if (/[+-]\d{2}:\d{2}$/.test(utcDate)) {
      const date = new Date(utcDate);
      if (!isNaN(date.getTime())) {
        utcDate = date.toISOString();
      }
    }

    else {
      utcDate = `${utcDate}Z`;
    }

    const date = new Date(utcDate);

    if (isNaN(date.getTime())) return "Invalid Date";

    return date.toLocaleString(undefined, options);
  } catch {
    return "Invalid Date";
  }
};

/**
 * Converts a UTC date string to local date string in YYYY-MM-DD format for date inputs
 * @param dateString - The UTC date string from backend
 * @returns Local date string in YYYY-MM-DD format
 */
export const convertUTCToLocalDateString = (dateString: string): string => {
  if (!dateString) return "";

  try {
    let utcDate = dateString;

    if (utcDate.endsWith("Z")) {}

    else if (/[+-]\d{2}:\d{2}$/.test(utcDate)) {
      const date = new Date(utcDate);
      if (!isNaN(date.getTime())) {
        utcDate = date.toISOString();
      }
    }

    else {
      utcDate = `${utcDate}Z`;
    }

    const date = new Date(utcDate);

    if (isNaN(date.getTime())) return "";

    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");

    return `${year}-${month}-${day}`;
  } catch {
    return "";
  }
};

/**
 * Converts a local date string (YYYY-MM-DD) to UTC ISO string for API calls
 * Treats the date as midnight in the local timezone, then converts to UTC
 * @param localDateString - The local date string from date input
 * @returns UTC ISO string for API
 */
export const convertLocalDateToUTC = (localDateString: string): string => {
  if (!localDateString) return "";

  try {
    const localDate = new Date(localDateString + "T00:00:00");
    return localDate.toISOString();
  } catch {
    return "";
  }
};

/**
 * Converts a UTC ISO string to local datetime string in YYYY-MM-DDTHH:mm format
 * This format is suitable for datetime-local input fields
 * @param utcDateTimeString - The UTC ISO date string from backend
 * @returns Local datetime string in YYYY-MM-DDTHH:mm format
 */
export const convertUTCToLocalDateTimeString = (
  utcDateTimeString: string
): string => {
  if (!utcDateTimeString) return "";

  try {
    let utcDate = utcDateTimeString;

    if (utcDate.endsWith("Z")) {}
    
    else if (/[+-]\d{2}:\d{2}$/.test(utcDate)) {
      const date = new Date(utcDate);
      if (!isNaN(date.getTime())) {
        utcDate = date.toISOString();
      }
    }
    else {
      utcDate = `${utcDate}Z`;
    }

    const date = new Date(utcDate);

    if (isNaN(date.getTime())) return "";

    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");
    const hours = String(date.getHours()).padStart(2, "0");
    const minutes = String(date.getMinutes()).padStart(2, "0");

    return `${year}-${month}-${day}T${hours}:${minutes}`;
  } catch {
    return "";
  }
};
