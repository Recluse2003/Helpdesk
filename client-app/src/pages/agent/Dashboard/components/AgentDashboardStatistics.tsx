import styles from "../AgentDashboard.module.css";

interface AgentDashboardStatisticsProps {
  assignedTicketCount: number;
  unassignedTicketCount: number;
  awaitingCustomerResponseCount: number;
  isLoading: boolean;
}

export default function AgentDashboardStatistics({
  assignedTicketCount,
  unassignedTicketCount,
  awaitingCustomerResponseCount,
  isLoading,
}: AgentDashboardStatisticsProps) {
  if (isLoading) {
    return <div className={styles.emptyMessage}>Loading statistics...</div>;
  }

  return (
    <div className={styles.agentStatistics}>
      <div className={styles.agentStatistic}>
        <span>Assigned tickets</span>
        <span>{assignedTicketCount}</span>
      </div>

      <div className={styles.agentStatistic}>
        <span>Unassigned tickets</span>
        <span>{unassignedTicketCount}</span>
      </div>

      <div className={styles.agentStatistic}>
        <span>Awaiting customer responses</span>
        <span>{awaitingCustomerResponseCount}</span>
      </div>
    </div>
  );
}
