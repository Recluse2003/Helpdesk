import type { PagedList } from "../types/commonTypes";
import type { TicketPriority, TicketStatus } from "../types/ticketTypes";
import { api } from "./axiosInstances";

export interface CreateTicketRequest {
  title: string;
  description: string;
  priority: TicketPriority;
}

export interface CreateTicketResponse {
  ticketId: string;
}

export async function createTicket(
  request: CreateTicketRequest,
): Promise<CreateTicketResponse> {
  const { data } = await api.post<CreateTicketResponse>("/tickets", request);

  return data;
}

export interface GetMyTicketsRequest {
  pageNumber: number;
  pageSize: number;
  searchTerm: string;
  status?: TicketStatus;
}

export interface GetMyTicketsResponse {
  tickets: PagedList<TicketSummaryDto>;
  statistics: TicketStatisticsDto;
}

export interface TicketSummaryDto {
  id: string;
  title: string;
  status: TicketStatus;
  priority: TicketPriority;
  createdAt: string;
  updatedAt: string;
  assignedAgentId: string | null;
  assignedAgentUsername: string | null;
  assignedAgentImageUrl: string | null;
}

export interface TicketStatisticsDto {
  totalTickets: number;
  openTickets: number;
  inProgressTickets: number;
  awaitingCustomerResponseTickets: number;
  resolvedTickets: number;
  closedTickets: number;
}

export async function getMyTickets(
  request: GetMyTicketsRequest,
): Promise<GetMyTicketsResponse> {
  const { data } = await api.get<GetMyTicketsResponse>("/tickets/me", {
    params: {
      pageNumber: request.pageNumber,
      pageSize: request.pageSize,
      searchTerm: request.searchTerm,
      status: request.status,
    },
  });

  return data;
}
