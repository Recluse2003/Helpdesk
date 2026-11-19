import { Link, NavLink } from "react-router";
import { useAuth } from "../../context/AuthContext";
import { Role } from "../../types/auth";
import ProfileDropdown from "../ProfileDropdown/ProfileDropdown";
import styles from "./Navbar.module.css";

export default function Navbar() {
  const { user } = useAuth();

  const isCustomer = user?.roles.includes(Role.CUSTOMER);
  const isAdmin = user?.roles.includes(Role.ADMINISTRATOR);
  const isSupportAgent = user?.roles.includes(Role.SUPPORTAGENT);

  return (
    <nav className={styles.navbar} role="navigation">
      <div className={styles.container}>
        <Link to="/" className={styles.logo}>
          Helpdesk
        </Link>

        <div className={styles.links}>
          {isCustomer && (
            <>
              <NavLink className={styles.activate} to="/my-tickets">
                My Tickets
              </NavLink>

              <NavLink to="/create-ticket">Create Ticket</NavLink>
            </>
          )}

          {(isAdmin || isSupportAgent) && (
            <>
              <NavLink to="/">Dashboard</NavLink>

              <NavLink to="/tickets">Tickets</NavLink>
            </>
          )}

          {isAdmin && (
            <>
              <NavLink to="/escalations">Escalation requests</NavLink>

              <NavLink to="/users">Users</NavLink>
            </>
          )}
        </div>

        <div className={styles.userSection}>
          <ProfileDropdown />
        </div>
      </div>
    </nav>
  );
}
