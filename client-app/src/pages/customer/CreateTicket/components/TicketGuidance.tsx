import { Lightbulb } from "lucide-react";
import styles from "../CreateTicket.module.css";

export default function TicketGuidance() {
  return (
    <section className={styles.infoContainer}>
      <h2 className={styles.infoHeader}>
        <Lightbulb className={styles.guidanceIcon} />
        <span>Writing a good ticket</span>
      </h2>

      <hr />

      <ul className={styles.guidanceList}>
        <li>Explain what you were trying to do.</li>
        <li>Describe what actually happened.</li>
        <li>Include any error messages.</li>
        <li>Mention when the problem started.</li>
      </ul>
    </section>
  );
}
