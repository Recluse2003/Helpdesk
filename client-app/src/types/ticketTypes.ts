export const TicketPriority = {
  LOW: "Low",
  MEDIUM: "Medium",
  HIGH: "High",
  URGENT: "Urgent",
} as const;

export type TicketPriority =
  (typeof TicketPriority)[keyof typeof TicketPriority];

export const TicketStatus = {
  OPEN: "Open",
  INPROGRESS: "InProgress",
  AWAITINGCUSTOMERRESPONSE: "AwaitingCustomerResponse",
  RESOLVED: "Resolved",
  CLOSED: "Closed",
};

export type TicketStatus = (typeof TicketStatus)[keyof typeof TicketStatus];
