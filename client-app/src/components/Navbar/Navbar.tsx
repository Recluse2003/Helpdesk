import { Link } from "react-router";
import ProfileDropdown from "../ProfileDropdown/ProfileDropdown";
import styles from "./Navbar.module.css";
import { PanelLeftClose, PanelLeftOpen } from "lucide-react";

interface NavbarProps {
  onMenuClick: () => void;
  isOpen: boolean;
}

export default function Navbar({ onMenuClick, isOpen }: NavbarProps) {
  return (
    <nav className={styles.navbar}>
      <div className={styles.container}>
        <div className={styles.navbarHeader}>
          <button
            className={styles.sidebarButton}
            onClick={onMenuClick}
            aria-label="Toggle sidebar"
          >
            {isOpen ? <PanelLeftOpen /> : <PanelLeftClose />}
          </button>

          <Link to="/" className={styles.logo}>
            Helpdesk
          </Link>
        </div>

        <div className={styles.actions}>
          <ProfileDropdown />
        </div>
      </div>
    </nav>
  );
}
