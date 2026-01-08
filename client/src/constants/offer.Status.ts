export type OfferStatusOptions = {
  value: string;
  status: string;
};

// Pending = 1
//     Accepted = 2,
//     Rejected = 3,
//     Countered = 4,
//     Expired = 5,
//     Withdrawn = 6

export const OFFER_STATUS_OPTIONS: OfferStatusOptions[] = [
  { value: "1", status: "Pending" },
  { value: "2", status: "Accepted" },
  { value: "3", status: "Rejected" },
  { value: "4", status: "Countered" },
  { value: "5", status: "Expired" },
  { value: "6", status: "Withdrawn" },
];

export const OFFER_STATUS_ENUM_MAP: Record<string, number> = {
  Unknown: 0,
  Pending: 1,
  Accepted: 2,
  Rejected: 3,
  Countered: 4,
  Expired: 5,
  Withdrawn: 6,
};

export const OFFER_STATUS_MAP: Record<number, string> = {
  0: "Unknown",
  1: "Pending",
  2: "Accepted",
  3: "Rejected",
  4: "Countered",
  5: "Expired",
  6: "Withdrawn",
};

export const getOfferStatusBadgeVariant = (status: number) => {
  switch (status) {
    case 1: // Pending
      return "pending";
    case 2: // Accepted
      return "success";
    case 3: // Rejected
      return "destructive";
    case 4: // Countered
      return "warning";
    case 5: // Expired
      return "secondary";
    default:
      return "outline";
  }
};
