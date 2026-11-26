import { useEffect, useState } from "react";
import styles from "./AgentDashboard.module.css";
import AgentDashboardStatistics from "./components/AgentDashboardStatistics";
import {
  getAgentDashboard,
  type GetAgentDashboardResponse,
} from "../../../services/agentService";
import { useApiFormErrors } from "../../../hooks/useApiFormErrors";
import AgentDashboardRecentActivities from "./components/AgentDashboardRecentActivities";
import AgentDashboardTickets from "./components/AgentDashboardTickets";

export default function AgentDashboardPage() {
  const [dashboardData, setDashboardData] =
    useState<GetAgentDashboardResponse | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  const { generalError, handleApiError, clearAllErrors } = useApiFormErrors();

  useEffect(() => {
    async function fetchDashboard() {
      clearAllErrors();
      setIsLoading(true);

      try {
        const response = await getAgentDashboard();
        setDashboardData(response);
      } catch (err: unknown) {
        handleApiError(err);
      } finally {
        setIsLoading(false);
      }
    }

    fetchDashboard();
  }, [clearAllErrors, handleApiError]);

  return (
    <>
      <div className={styles.header}>
        <h1>Dashboard</h1>
        <p>Here's what's happening with your tickets today.</p>
      </div>

      <hr />

      {generalError ? (
        <p className={styles.errorMessage}>{generalError}</p>
      ) : (
        <>
          <AgentDashboardStatistics
            assignedTicketCount={dashboardData?.assignedTicketCount ?? 0}
            unassignedTicketCount={dashboardData?.unassignedTicketCount ?? 0}
            awaitingCustomerResponseCount={
              dashboardData?.awaitingCustomerResponseCount ?? 0
            }
            isLoading={isLoading}
          />

          <AgentDashboardRecentActivities
            recentActivities={dashboardData?.recentActivities ?? []}
          />

          <AgentDashboardTickets
            tickets={dashboardData?.ticketsNeedingAttention ?? []}
          />
        </>
      )}
    </>
  );
}
