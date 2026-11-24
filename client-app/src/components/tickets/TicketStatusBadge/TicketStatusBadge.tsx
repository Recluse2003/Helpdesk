import { TicketStatus } from "../../../types/ticketTypes";
import { ticketStatusLabels } from "../../../utils/ticketStatusLabels";
import styles from "./TicketStatusBadge.module.css";

interface TicketStatusBadgeProps {
  variant: TicketStatus;
}

export default function TicketStatusBadge({ variant }: TicketStatusBadgeProps) {
  return (
    <span className={`${styles.badge} ${styles[variant]}`}>
      {ticketStatusLabels[variant]}
    </span>
  );
}
