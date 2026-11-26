import type { AgentDashboardTicketDto } from "../../../../services/agentService";
import { Link } from "react-router";
import styles from "../AgentDashboard.module.css";
import TicketPriorityBadge from "../../../../components/tickets/TicketPriorityBadge/TicketPriorityBadge";
import TicketStatusBadge from "../../../../components/tickets/TicketStatusBadge/TicketStatusBadge";
import { formatDateTime } from "../../../../utils/dateUtils";

interface AgentDashboardTicketsProps {
  tickets: AgentDashboardTicketDto[];
}

export default function AgentDashboardTickets({
  tickets,
}: AgentDashboardTicketsProps) {
  return (
    <section className={styles.tickets}>
      <div className={styles.sectionHeader}>
        <h2>Tickets needing attention</h2>
        <Link to="/agent/tickets">View all tickets</Link>
      </div>

      <hr />

      {tickets.length === 0 ? (
        <p className={styles.emptyMessage}>
          No tickets currently need your attention.
        </p>
      ) : (
        <div className={styles.ticketList}>
          {tickets.map((ticket) => (
            <article key={ticket.id} className={styles.ticketRow}>
              <div className={styles.ticketInfo}>
                <span className={styles.ticketId}>#{ticket.id}</span>

                <Link
                  to={`/tickets/${ticket.id}`}
                  className={styles.ticketTitle}
                >
                  {ticket.title}
                </Link>

                <span className={styles.customerName}>
                  {ticket.customerDisplayName}
                </span>
              </div>

              <div className={styles.ticketBadges}>
                <TicketPriorityBadge variant={ticket.priority} />
                <TicketStatusBadge variant={ticket.status} />
              </div>

              <span className={styles.ticketUpdated}>
                Updated {formatDateTime(ticket.updatedAt)}
              </span>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
