import { api } from "./axiosInstances";
import type {
  TicketPriority,
  TicketStatus,
  TicketActivityType,
} from "../types/ticketTypes";

export interface GetAgentDashboardResponse {
  assignedTicketCount: number;
  unassignedTicketCount: number;
  awaitingCustomerResponseCount: number;
  ticketsNeedingAttention: AgentDashboardTicketDto[];
  recentActivities: AgentRecentActivityDto[];
}

export interface AgentDashboardTicketDto {
  id: string;
  title: string;
  priority: TicketPriority;
  status: TicketStatus;
  createdAt: string;
  updatedAt: string;
  customerId: string;
  customerDisplayName: string;
  customerImageUrl: string | null;
}

export interface AgentRecentActivityDto {
  ticketId: string;
  ticketTitle: string;
  eventType: TicketActivityType;
  details: string | null;
  createdAt: string;
  actorUserId: string | null;
  actorDisplayName: string | null;
  targetUserId: string | null;
  targetDisplayName: string | null;
}

export async function getAgentDashboard(): Promise<GetAgentDashboardResponse> {
  const { data } =
    await api.get<GetAgentDashboardResponse>("/agents/dashboard");

  return data;
}
