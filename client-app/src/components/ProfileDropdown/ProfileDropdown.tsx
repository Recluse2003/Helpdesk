import { useEffect, useRef, useState } from "react";
import { Link, useNavigate } from "react-router";
import { ChevronDown, LogOut, Settings, UserRound } from "lucide-react";

import { useAuth } from "../../context/AuthContext";
import styles from "./ProfileDropdown.module.css";

export default function ProfileDropdown() {
  const [isOpen, setIsOpen] = useState(false);

  const dropdownRef = useRef<HTMLDivElement>(null);

  const { user, logout } = useAuth();
  const navigate = useNavigate();

  // Close dropdown when clicking outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (
        dropdownRef.current &&
        !dropdownRef.current.contains(event.target as Node)
      ) {
        setIsOpen(false);
      }
    }

    document.addEventListener("mousedown", handleClickOutside);

    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, []);

  // Close dropdown when pressing Escape
  useEffect(() => {
    function handleEscape(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setIsOpen(false);
      }
    }

    document.addEventListener("keydown", handleEscape);

    return () => {
      document.removeEventListener("keydown", handleEscape);
    };
  }, []);

  function handleToggle() {
    setIsOpen((previous) => !previous);
  }

  function handleClose() {
    setIsOpen(false);
  }

  async function handleLogout() {
    handleClose();

    try {
      await logout();
      navigate("/auth/login", { replace: true });
    } catch (error) {
      console.error("Logout failed:", error);
    }
  }

  return (
    <div className={styles.container} ref={dropdownRef}>
      <button
        type="button"
        className={styles.profileButton}
        onClick={handleToggle}
        aria-expanded={isOpen}
        aria-haspopup="true"
      >
        <span>{user?.displayName}</span>

        <ChevronDown
          size={16}
          className={`${styles.chevron} ${isOpen ? styles.rotated : ""}`}
        />
      </button>

      {isOpen && (
        <div className={styles.dropdown}>
          <div className={styles.dropdownHeader}>
            <strong>{user?.displayName}</strong>
            <span>{user?.email}</span>
          </div>

          <div className={styles.dropdownLinks}>
            <Link
              to="/profile"
              className={styles.dropdownItem}
              onClick={handleClose}
            >
              <UserRound size={17} />
              <span>My Profile</span>
            </Link>

            <Link
              to="/settings"
              className={styles.dropdownItem}
              onClick={handleClose}
            >
              <Settings size={17} />
              <span>Settings</span>
            </Link>
          </div>

          <div className={styles.dropdownFooter}>
            <button
              type="button"
              className={`${styles.dropdownItem} ${styles.logoutButton}`}
              onClick={handleLogout}
            >
              <LogOut size={17} />
              <span>Logout</span>
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
