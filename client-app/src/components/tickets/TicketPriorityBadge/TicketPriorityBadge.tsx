import { TicketPriority } from "../../../types/ticketTypes";
import styles from "./TicketPriorityBadge.module.css";

interface TicketPriorityBadgeProps {
  variant: TicketPriority;
}

export default function TicketPriorityBadge({
  variant,
}: TicketPriorityBadgeProps) {
  return (
    <span className={`${styles.badge} ${styles[variant]}`}>{variant}</span>
  );
}
