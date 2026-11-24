import { TicketStatus } from "../types/ticketTypes";

export const ticketStatusLabels: Record<TicketStatus, string> = {
  [TicketStatus.OPEN]: "Open",
  [TicketStatus.INPROGRESS]: "In Progress",
  [TicketStatus.AWAITINGCUSTOMERRESPONSE]: "Awaiting Response",
  [TicketStatus.RESOLVED]: "Resolved",
  [TicketStatus.CLOSED]: "Closed",
};
