import { useState, useEffect } from "react";
import { Outlet } from "react-router";
import Sidebar from "../components/Sidebar/Sidebar";
import Navbar from "../components/Navbar/Navbar";
import styles from "./MainLayout.module.css";

export default function MainLayout() {
  const [isMobile, setIsMobile] = useState(() => window.innerWidth <= 1024);

  const [sidebarOpen, setSidebarOpen] = useState(
    () => window.innerWidth > 1024,
  );

  useEffect(() => {
    const handleResize = () => {
      const mobile = window.innerWidth <= 1024;

      setIsMobile(mobile);
      setSidebarOpen(!mobile);
    };

    window.addEventListener("resize", handleResize);

    return () => {
      window.removeEventListener("resize", handleResize);
    };
  }, []);

  const closeSidebar = () => setSidebarOpen(false);

  return (
    <div
      className={`${styles.layout} ${sidebarOpen ? styles.sidebarOpen : styles.sidebarClosed}`}
    >
      <Navbar
        onMenuClick={() => setSidebarOpen((prev) => !prev)}
        isOpen={sidebarOpen}
      />

      <Sidebar
        isOpen={sidebarOpen}
        onClose={closeSidebar}
        isMobile={isMobile}
      />

      <main className={styles.mainContent}>
        <Outlet />
      </main>
    </div>
  );
}
