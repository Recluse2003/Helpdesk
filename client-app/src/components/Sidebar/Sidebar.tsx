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
  onClose: () => void;
  isMobile: boolean;
}

export default function Sidebar({ isOpen, onClose, isMobile }: SidebarProps) {
  const { user } = useAuth();

  const isCustomer = user?.roles.includes(Role.CUSTOMER);
  const isAdmin = user?.roles.includes(Role.ADMINISTRATOR);
  const isSupportAgent = user?.roles.includes(Role.SUPPORTAGENT);

  return (
    <>
      <aside className={`${styles.sidebar} ${isOpen ? styles.open : ""}`}>
        <div className={styles.container}>
          <div className={styles.sectionTitle}>WORKSPACE</div>

          <div className={styles.links}>
            <NavLink to="/dashboard" onClick={onClose}>
              <LayoutDashboard /> Dashboard
            </NavLink>

            {isCustomer && (
              <>
                <NavLink to="/my-tickets" onClick={onClose}>
                  <Ticket /> My Tickets
                </NavLink>

                <NavLink to="/create-ticket" onClick={onClose}>
                  <PlusCircle /> Create Ticket
                </NavLink>
              </>
            )}

            {(isAdmin || isSupportAgent) && (
              <>
                <NavLink to="/ticket-queue" end onClick={onClose}>
                  <GalleryVerticalEnd /> Ticket Queue
                </NavLink>

                <NavLink to="/my-assigned-tickets" onClick={onClose}>
                  <TicketCheck /> My Assigned Tickets
                </NavLink>
              </>
            )}

            {isAdmin && (
              <>
                <NavLink to="/tickets" onClick={onClose}>
                  <Tickets /> All Tickets
                </NavLink>

                <NavLink to="/users" onClick={onClose}>
                  <Users /> Users
                </NavLink>

                <NavLink to="/escalations" onClick={onClose}>
                  <GitPullRequest /> Escalation Requests
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
