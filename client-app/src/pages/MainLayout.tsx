import { useState, useEffect } from "react";
import { Outlet } from "react-router";
import Sidebar from "../components/Sidebar/Sidebar";
import Navbar from "../components/Navbar/Navbar";
import styles from "./MainLayout.module.css";

type Viewport = "desktop" | "tablet" | "mobile";

function getViewport(): Viewport {
  if (window.innerWidth < 768) return "mobile";
  if (window.innerWidth < 1200) return "tablet";
  return "desktop";
}

export default function MainLayout() {
  const [viewport, setViewport] = useState<Viewport>(getViewport);

  const [desktopExpanded, setDesktopExpanded] = useState(true);
  const [tabletExpanded, setTabletExpanded] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);

  useEffect(() => {
    const handleResize = () => {
      setViewport(getViewport());
    };

    window.addEventListener("resize", handleResize);

    return () => window.removeEventListener("resize", handleResize);
  }, []);

  const handleMenuClick = () => {
    if (viewport === "desktop") {
      setDesktopExpanded((prev) => !prev);
    } else if (viewport === "tablet") {
      setTabletExpanded((prev) => !prev);
    } else {
      setMobileOpen((prev) => !prev);
    }
  };

  const isMobile = viewport === "mobile";

  const isExpanded =
    viewport === "desktop"
      ? desktopExpanded
      : viewport === "tablet"
        ? tabletExpanded
        : true;

  const isSidebarVisible = !isMobile || mobileOpen;

  const layoutClass = isMobile
    ? styles.mobile
    : isExpanded
      ? styles.sidebarExpanded
      : styles.sidebarCompact;

  return (
    <div className={`${styles.layout} ${layoutClass}`}>
      <Navbar
        onMenuClick={handleMenuClick}
        isOpen={isMobile ? mobileOpen : isExpanded}
      />

      <Sidebar
        isOpen={isSidebarVisible}
        isExpanded={isExpanded}
        isMobile={isMobile}
        onClose={() => setMobileOpen(false)}
      />

      <main className={styles.mainContent}>
        <Outlet />
      </main>
    </div>
  );
}
