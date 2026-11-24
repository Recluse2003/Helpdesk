import styles from "../MyTickets.module.css";
import { Search, X } from "lucide-react";

interface TicketSearchProps {
  searchTerm: string;
  onSearchChange: (value: string) => void;
  isLoading: boolean;
}

export default function TicketSearch({
  searchTerm,
  onSearchChange,
  isLoading,
}: TicketSearchProps) {
  return (
    <div className={styles.searchContainer}>
      <Search size={18} />

      <input
        type="search"
        value={searchTerm}
        onChange={(e) => onSearchChange(e.target.value)}
        disabled={isLoading}
        placeholder="Search tickets..."
        aria-label="Search tickets"
      />

      {searchTerm && (
        <button
          type="button"
          onClick={() => onSearchChange("")}
          disabled={isLoading}
          aria-label="Clear search"
        >
          <X size={18} />
        </button>
      )}
    </div>
  );
}
