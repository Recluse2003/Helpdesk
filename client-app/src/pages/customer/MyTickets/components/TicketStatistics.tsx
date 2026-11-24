import type { TicketStatisticsDto } from "../../../../services/ticketService";
import styles from "../MyTickets.module.css";

interface TicketStatisticsProps {
  statistics: TicketStatisticsDto | null;
  isLoading: boolean;
}

export default function TicketStatistics({
  statistics,
  isLoading,
}: TicketStatisticsProps) {
  if (isLoading) {
    return <div className={styles.statisticsEmpty}>Loading statistics...</div>;
  }

  if (!statistics || statistics.totalTickets === 0) {
    return (
      <div className={styles.statisticsEmpty}>
        <h3>No ticket statistics yet</h3>
        <p>
          Submit your first ticket to start seeing your support activity here.
        </p>
      </div>
    );
  }

  return (
    <div className={styles.ticketStatistics}>
      <div className={styles.ticketStatistic}>
        <span>Total tickets</span>
        <span>{statistics.totalTickets}</span>
      </div>

      <div className={styles.ticketStatistic}>
        <span>Open</span>
        <span>{statistics.openTickets}</span>
      </div>

      <div className={styles.ticketStatistic}>
        <span>In progress</span>
        <span>{statistics.inProgressTickets}</span>
      </div>

      <div className={styles.ticketStatistic}>
        <span>Awaiting response</span>
        <span>{statistics.awaitingCustomerResponseTickets}</span>
      </div>

      <div className={styles.ticketStatistic}>
        <span>Resolved</span>
        <span>{statistics.resolvedTickets}</span>
      </div>

      <div className={styles.ticketStatistic}>
        <span>Closed</span>
        <span>{statistics.closedTickets}</span>
      </div>
    </div>
  );
}
