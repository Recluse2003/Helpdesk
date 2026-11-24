import { Clock } from "lucide-react";
import styles from "../CreateTicket.module.css";

export default function TicketExpectations() {
  return (
    <section className={styles.infoContainer}>
      <h2 className={styles.infoHeader}>
        <Clock className={styles.expectationsIcon} />
        <span>What happens next?</span>
      </h2>

      <hr />

      <ol className={styles.expectationsList}>
        <li>
          <h3>Ticket submitted</h3> - Your request is received and recorded.
        </li>
        <li>
          <h3>Agent reviews your ticket</h3> - A support agent reviews the issue
          and takes ownership.
        </li>
        <li>
          <h3>Investigation and response</h3> - The agent investigates and
          communicates with you through the ticket.
        </li>
        <li>
          <h3>Issue resolved</h3> - The ticket is marked as resolved once the
          issue has been addressed.
        </li>
      </ol>
    </section>
  );
}
