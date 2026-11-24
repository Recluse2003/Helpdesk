import { useState } from "react";
import { useNavigate } from "react-router";
import { TicketPriority } from "../../../types/ticketTypes";
import styles from "./CreateTicket.module.css";
import {
  createTicket,
  type CreateTicketRequest,
} from "../../../services/ticketService";
import { useApiFormErrors } from "../../../hooks/useApiFormErrors";
import TicketForm from "./components/TicketForm";
import TicketGuidance from "./components/TicketGuidance";
import TicketExpectations from "./components/TicketExpectations";

export default function CreateTicketPage() {
  const navigate = useNavigate();

  const [formData, setFormData] = useState<CreateTicketRequest>({
    title: "",
    description: "",
    priority: TicketPriority.MEDIUM,
  });

  const [isLoading, setIsLoading] = useState(false);

  const {
    generalError,
    fieldErrors,
    handleApiError,
    clearFieldError,
    clearAllErrors,
  } = useApiFormErrors();

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    clearAllErrors();
    setIsLoading(true);

    try {
      const { ticketId } = await createTicket(formData);

      navigate(`/tickets/${ticketId}`);
    } catch (err: unknown) {
      handleApiError(err);
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <div className={styles.content}>
      <div className={styles.formContainer}>
        <div className={styles.header}>
          <h1>Create a Ticket</h1>
          <p>Describe your issue and our support team will help you.</p>
        </div>

        <hr />

        <TicketForm
          formData={formData}
          setFormData={setFormData}
          onSubmit={handleSubmit}
          isLoading={isLoading}
          generalError={generalError}
          fieldErrors={fieldErrors}
          clearFieldError={clearFieldError}
        />
      </div>

      <div className={styles.infoSection}>
        <TicketGuidance />
        <TicketExpectations />
      </div>
    </div>
  );
}
