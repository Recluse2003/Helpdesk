import { Link } from "react-router";
import type { AgentRecentActivityDto } from "../../../../services/agentService";
import { TicketActivityType } from "../../../../types/ticketTypes";
import { formatDateTime } from "../../../../utils/dateUtils";
import styles from "../AgentDashboard.module.css";

interface AgentDashboardRecentActivitiesProps {
  recentActivities: AgentRecentActivityDto[];
}

function getActivityDescription(activity: AgentRecentActivityDto): string {
  const actor = activity.actorDisplayName ?? "System";
  const target = activity.targetDisplayName;

  switch (activity.eventType) {
    case TicketActivityType.CREATED:
      return `${actor} created this ticket`;

    case TicketActivityType.ASSIGNED:
      return target
        ? `${actor} assigned this ticket to ${target}`
        : `${actor} claimed this ticket`;

    case TicketActivityType.REASSIGNED:
      return target
        ? `${actor} reassigned this ticket to ${target}`
        : `This ticket has been reassigned`;

    case TicketActivityType.UNASSIGNED:
      return target
        ? `${actor} unassigned ${target} from this ticket`
        : `${actor} unassigned this ticket`;

    case TicketActivityType.STATUSCHANGED:
      return `${actor} changed the ticket status`;

    case TicketActivityType.PRIORITYCHANGED:
      return `${actor} changed the ticket priority`;

    case TicketActivityType.ESCALATIONREQUESTED:
      return `${actor} requested an escalation`;

    case TicketActivityType.ESCALATIONACCEPTED:
      return `${actor} accepted the escalation`;

    case TicketActivityType.ESCALATIONREJECTED:
      return `${actor} rejected the escalation`;

    case TicketActivityType.REOPENED:
      return `${actor} has reopened this ticket`;

    default:
      return "Ticket activity recorded";
  }
}

export default function AgentDashboardRecentActivities({
  recentActivities,
}: AgentDashboardRecentActivitiesProps) {
  return (
    <section className={styles.recentActivities}>
      <div className={styles.sectionHeader}>
        <h2>Recent activity</h2>
      </div>

      {recentActivities.length === 0 ? (
        <p className={styles.emptyMessage}>No recent activity to display.</p>
      ) : (
        <div className={styles.activityList}>
          {recentActivities.map((activity, index) => (
            <article
              key={`${activity.ticketId}-${activity.createdAt}-${index}`}
              className={styles.activityItem}
            >
              <div className={styles.activityIndicator} />

              <div className={styles.activityContent}>
                <p className={styles.activityDescription}>
                  {getActivityDescription(activity)}
                </p>

                <Link
                  to={`/tickets/${activity.ticketId}`}
                  className={styles.activityTicket}
                >
                  {activity.ticketTitle}
                </Link>

                {activity.details && (
                  <p className={styles.activityDetails}>{activity.details}</p>
                )}

                <span className={styles.activityTime}>
                  {formatDateTime(activity.createdAt)}
                </span>
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
