import { NavLink } from "react-router";
import { useAuth } from "../../context/AuthContext";
import { Role } from "../../types/auth";
import {
  Ticket,
  PlusCircle,
  LayoutDashboard,
  TicketCheck,
  GalleryVerticalEnd,
  Tickets,
  Users,
  GitPullRequest,
} from "lucide-react";
import styles from "./Sidebar.module.css";

interface SidebarProps {
  isOpen: boolean;
  isExpanded: boolean;
  isMobile: boolean;
  onClose: () => void;
}

export default function Sidebar({
  isOpen,
  isExpanded,
  isMobile,
  onClose,
}: SidebarProps) {
  const { user } = useAuth();

  const isCustomer = user?.roles.includes(Role.CUSTOMER);
  const isAdmin = user?.roles.includes(Role.ADMINISTRATOR);
  const isSupportAgent = user?.roles.includes(Role.SUPPORTAGENT);

  return (
    <>
      <aside
        className={[
          styles.sidebar,
          isExpanded ? styles.expanded : styles.compact,
          isMobile && isOpen ? styles.mobileOpen : "",
        ].join(" ")}
      >
        <div className={styles.container}>
          <div className={styles.sectionTitle}>WORKSPACE</div>

          <div className={styles.links}>
            <NavLink to="/dashboard" onClick={onClose} title="Dashboard">
              <LayoutDashboard />
              <span className={styles.linkText}>Dashboard</span>
            </NavLink>

            {isCustomer && (
              <>
                <NavLink to="/my-tickets" onClick={onClose} title="My Tickets">
                  <Ticket />
                  <span className={styles.linkText}>My Tickets</span>
                </NavLink>

                <NavLink
                  to="/create-ticket"
                  onClick={onClose}
                  title="Create Ticket"
                >
                  <PlusCircle />
                  <span className={styles.linkText}>Create Ticket</span>
                </NavLink>
              </>
            )}

            {isSupportAgent && (
              <>
                <NavLink to="/ticket-queue" end onClick={onClose}>
                  <GalleryVerticalEnd />
                  <span className={styles.linkText}>Ticket Queue</span>
                </NavLink>

                <NavLink to="/my-assigned-tickets" onClick={onClose}>
                  <TicketCheck />
                  <span className={styles.linkText}>My Assigned Tickets</span>
                </NavLink>
              </>
            )}

            {isAdmin && (
              <>
                <NavLink to="/tickets" onClick={onClose}>
                  <Tickets />
                  <span className={styles.linkText}>All Tickets</span>
                </NavLink>

                <NavLink to="/users" onClick={onClose}>
                  <Users />
                  <span className={styles.linkText}>Users</span>
                </NavLink>

                <NavLink to="/escalations" onClick={onClose}>
                  <GitPullRequest />
                  <span className={styles.linkText}>Escalation Requests</span>
                </NavLink>
              </>
            )}
          </div>
        </div>
      </aside>

      {isOpen && isMobile && (
        <button
          className={styles.overlay}
          onClick={onClose}
          aria-label="Close sidebar"
          type="button"
        />
      )}
    </>
  );
}
