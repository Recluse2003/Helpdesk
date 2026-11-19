import { Outlet } from "react-router";
import Navbar from "../components/Navbar/Navbar";
import styles from "./MainLayout.module.css";

export default function MainLayout() {
  return (
    <div className={styles.layout}>
      <Navbar />

      <main className={styles.content}>
        <Outlet />
      </main>
    </div>
  );
}
