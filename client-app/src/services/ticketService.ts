import type { PagedList } from "../types/commonTypes";
import type {
  TicketActivityType,
  TicketPriority,
  TicketStatus,
} from "../types/ticketTypes";
import { api } from "./axiosInstances";

export interface AddTicketMessageRequest {
  content: string;
  isPublic: boolean;
}

export async function addTicketMessage(
  ticketId: string,
  request: AddTicketMessageRequest,
): Promise<void> {
  await api.post(`/tickets/${ticketId}/message`, request);
}

export interface AssignTicketRequest {
  assigneeId: string;
}

export async function assignTicket(
  ticketId: string,
  request: AssignTicketRequest,
): Promise<void> {
  await api.post(`/tickets/${ticketId}/assign`, request);
}

export interface ChangeTicketPriorityRequest {
  priority: TicketPriority;
}

export async function changeTicketPriority(
  ticketId: string,
  request: ChangeTicketPriorityRequest,
): Promise<void> {
  await api.post(`/tickets/${ticketId}/priority`, request);
}

export async function claimTicket(ticketId: string): Promise<void> {
  await api.post(`/tickets/${ticketId}/claim`);
}

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

export interface GetSupportQueueRequest {
  pageNumber?: number;
  pageSize?: number;
  status?: TicketStatus;
  priority?: TicketPriority;
  customerId?: string;
  assigneeId?: string;
}

export interface QueueTicketDto {
  id: string;
  title: string;
  status: TicketStatus;
  priority: TicketPriority;
  createdAt: string;
  updatedAt: string;
  createdById: string;
  createdByDisplayName: string;
  createdByImageUrl: string | null;
  assignedAgentId: string;
  assignedAgentDisplayName: string;
  assignedAgentImageUrl: string | null;
}

export async function getSupportQueue(
  request: GetSupportQueueRequest,
): Promise<PagedList<QueueTicketDto>> {
  const { data } = await api.get<PagedList<QueueTicketDto>>("/tickets/queue", {
    params: request,
  });

  return data;
}

export interface TicketDetailsResponse {
  id: string;
  title: string;
  description: string;
  status: TicketStatus;
  priority: TicketPriority;
  createdAt: string;
  updatedAt: string;
  timeline: TicketTimelineItemDto[];
}

export interface TicketTimelineItemDto {
  id: string;
  type: TicketTimelineItemType;
  actorName: string;
  createdAt: string;
  content: string | null;
  details: string | null;
  activityType: TicketActivityType | null;
}

export const TicketTimelineItemType = {
  MESSAGE: "Message",
  ACTIVITY: "Activity",
};

export type TicketTimelineItemType =
  (typeof TicketTimelineItemType)[keyof typeof TicketTimelineItemType];

export async function getTicketDetails(
  ticketId: string,
): Promise<TicketDetailsResponse> {
  const { data } = await api.get<TicketDetailsResponse>(
    `/tickets/${ticketId}/priority`,
  );

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
