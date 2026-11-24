import { Link } from "react-router";
import { UserRound } from "lucide-react";
import TicketPriorityBadge from "../../../../components/tickets/TicketPriorityBadge/TicketPriorityBadge";
import TicketStatusBadge from "../../../../components/tickets/TicketStatusBadge/TicketStatusBadge";
import type { TicketSummaryDto } from "../../../../services/ticketService";
import styles from "../MyTickets.module.css";
import { formatDate, formatDateTime } from "../../../../utils/dateUtils";

interface TicketSummaryProps {
  ticket: TicketSummaryDto;
}

export default function TicketSummary({ ticket }: TicketSummaryProps) {
  return (
    <article className={styles.ticketCard}>
      <div className={styles.ticketHeader}>
        <span className={styles.ticketId}>#{ticket.id}</span>

        <div className={styles.ticketBadges}>
          <TicketPriorityBadge variant={ticket.priority} />
          <TicketStatusBadge variant={ticket.status} />
        </div>
      </div>

      <h3>{ticket.title}</h3>

      <div className={styles.ticketMeta}>
        <span>
          Created: {formatDate(ticket.createdAt)} · Updated:{" "}
          {formatDateTime(ticket.updatedAt)}
        </span>
      </div>

      <hr />

      <div className={styles.ticketFooter}>
        <div className={styles.ticketAssignedAgent}>
          {ticket.assignedAgentImageUrl ? (
            <img
              src={ticket.assignedAgentImageUrl}
              alt=""
              className={styles.agentAvatar}
            />
          ) : (
            <div className={styles.agentAvatarPlaceholder}>
              <UserRound size={32} />
            </div>
          )}

          <span>
            {ticket.assignedAgentUsername
              ? ticket.assignedAgentUsername
              : "Awaiting assignment"}
          </span>
        </div>

        <Link to={`/tickets/${ticket.id}`} className={styles.viewTicketButton}>
          View Ticket
        </Link>
      </div>
    </article>
  );
}
