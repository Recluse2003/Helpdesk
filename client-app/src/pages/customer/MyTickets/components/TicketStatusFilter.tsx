import { TicketStatus } from "../../../../types/ticketTypes";
import { ticketStatusLabels } from "../../../../utils/ticketStatusLabels";
import styles from "../MyTickets.module.css";

interface TicketStatusFilterProps {
  status: TicketStatus | undefined;
  onStatusChange: (status: TicketStatus | undefined) => void;
  isLoading: boolean;
}

export default function TicketStatusFilter({
  status,
  onStatusChange,
  isLoading,
}: TicketStatusFilterProps) {
  return (
    <>
      <label htmlFor="status" className={styles.hidden}>
        Filter tickets by status
      </label>

      <select
        id="status"
        name="status"
        value={status ?? ""}
        onChange={(e) =>
          onStatusChange(
            e.target.value === ""
              ? undefined
              : (e.target.value as TicketStatus),
          )
        }
        disabled={isLoading}
        className={styles.statusFilter}
      >
        <option value="">All statuses</option>

        {Object.values(TicketStatus).map((status) => (
          <option key={status} value={status}>
            {ticketStatusLabels[status]}
          </option>
        ))}
      </select>
    </>
  );
}
