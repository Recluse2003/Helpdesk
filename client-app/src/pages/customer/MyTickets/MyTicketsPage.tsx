import { useEffect, useState } from "react";
import styles from "./MyTickets.module.css";
import {
  getMyTickets,
  type TicketStatisticsDto,
  type TicketSummaryDto,
} from "../../../services/ticketService";
import { useApiFormErrors } from "../../../hooks/useApiFormErrors";
import { TicketStatus } from "../../../types/ticketTypes";
import type { PagedList } from "../../../types/commonTypes";
import { Link } from "react-router";
import TicketSummary from "./components/TicketSummary";
import TicketStatistics from "./components/TicketStatistics";
import TicketSearch from "./components/TicketSearch";
import TicketStatusFilter from "./components/TicketStatusFilter";
import { Pagination } from "../../../components/Pagination/Pagination";
import {
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
} from "lucide-react";

export default function MyTicketsPage() {
  const [pageNumber, setPageNumber] = useState<number>(1);
  const [pageSize, setPageSize] = useState<number>(10);
  const [searchTerm, setSearchTerm] = useState<string>("");
  const [debouncedSearchTerm, setDebouncedSearchTerm] = useState(searchTerm);
  const [status, setStatus] = useState<TicketStatus | undefined>(undefined);
  const [ticketData, setTicketData] =
    useState<PagedList<TicketSummaryDto> | null>(null);
  const [statistics, setStatistics] = useState<TicketStatisticsDto | null>(
    null,
  );
  const [isLoading, setIsLoading] = useState<boolean>(false);

  const { generalError, handleApiError, clearAllErrors } = useApiFormErrors();

  function handleSearchChange(newSearchTerm: string) {
    setSearchTerm(newSearchTerm);
    setPageNumber(1);
  }

  function handleStatusChange(newStatus: TicketStatus | undefined) {
    setStatus(newStatus);
    setPageNumber(1);
  }

  useEffect(() => {
    const timeout = setTimeout(() => {
      setDebouncedSearchTerm(searchTerm);
    }, 400);

    return () => clearTimeout(timeout);
  }, [searchTerm]);

  useEffect(() => {
    async function fetchTickets() {
      clearAllErrors();
      setIsLoading(true);

      try {
        const response = await getMyTickets({
          pageNumber,
          pageSize,
          searchTerm,
          status,
        });

        console.log(response);

        setTicketData(response.tickets);
        setStatistics(response.statistics);
      } catch (err: unknown) {
        handleApiError(err);
      } finally {
        setIsLoading(false);
      }
    }

    fetchTickets();
  }, [
    pageNumber,
    pageSize,
    debouncedSearchTerm,
    status,
    clearAllErrors,
    handleApiError,
  ]);

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <h1>My Tickets</h1>
          <Link to="/tickets/create">Create Ticket</Link>
        </div>
        <p>Manage and track your support requests</p>
      </div>

      <hr />

      <TicketStatistics statistics={statistics} isLoading={isLoading} />

      <hr />

      <h2>Your tickets</h2>

      <div className={styles.ticketFilters}>
        <TicketSearch
          searchTerm={searchTerm}
          onSearchChange={handleSearchChange}
          isLoading={isLoading}
        />
        <TicketStatusFilter
          status={status}
          onStatusChange={handleStatusChange}
          isLoading={isLoading}
        />
      </div>

      {generalError && <p className={styles.errorMessage}>{generalError}</p>}

      {isLoading && !ticketData ? (
        <p>Retrieving your tickets...</p>
      ) : ticketData?.items.length === 0 ? (
        <div className={styles.emptyState}>
          <h3>
            {searchTerm || status ? "No matching tickets" : "No tickets yet"}
          </h3>
          <p>
            {searchTerm || status
              ? "Try changing your search or status filter."
              : "You haven't submitted any tickets yet."}
          </p>
        </div>
      ) : (
        <>
          {isLoading && <p>Updating tickets...</p>}

          {ticketData?.items.map((ticket) => (
            <TicketSummary key={ticket.id} ticket={ticket} />
          ))}

          {ticketData && ticketData.totalPages > 1 && (
            <div className={styles.footer}>
              <span>
                Showing {pageSize * (pageNumber - 1) + 1}-
                {Math.min(pageSize * pageNumber, ticketData.totalCount)} of{" "}
                {ticketData.totalCount} tickets
              </span>

              <Pagination
                currentPage={pageNumber}
                totalPages={ticketData.totalPages}
                onPageChange={setPageNumber}
                className={styles.pagination}
              >
                <>
                  <Pagination.First>
                    <ChevronsLeft />
                  </Pagination.First>

                  <Pagination.Prev>
                    <ChevronLeft />
                  </Pagination.Prev>

                  <Pagination.JumpTo value={pageNumber} />

                  <Pagination.Next>
                    <ChevronRight />
                  </Pagination.Next>

                  <Pagination.Last>
                    <ChevronsRight />
                  </Pagination.Last>
                </>
              </Pagination>
            </div>
          )}
        </>
      )}
    </div>
  );
}
