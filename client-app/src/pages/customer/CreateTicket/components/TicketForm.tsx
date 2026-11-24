import type { CreateTicketRequest } from "../../../../services/ticketService";
import { TicketPriority } from "../../../../types/ticketTypes";
import styles from "../CreateTicket.module.css";

interface TicketFormProps {
  formData: CreateTicketRequest;
  setFormData: React.Dispatch<React.SetStateAction<CreateTicketRequest>>;
  onSubmit: (e: React.FormEvent<HTMLFormElement>) => void;
  isLoading: boolean;
  generalError: string | null;
  fieldErrors: Record<string, string[]>;
  clearFieldError: (field: string) => void;
}

export default function TicketForm({
  formData,
  setFormData,
  onSubmit,
  isLoading,
  generalError,
  fieldErrors,
  clearFieldError,
}: TicketFormProps) {
  function handleChange(
    e: React.ChangeEvent<
      HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement
    >,
  ) {
    const { name, value } = e.target;

    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));

    clearFieldError(name);
  }

  return (
    <form onSubmit={onSubmit} noValidate>
      {generalError && <div role="alert">{generalError}</div>}

      <div className={styles.formField}>
        <label htmlFor="title">Subject</label>

        <input
          id="title"
          name="title"
          type="text"
          value={formData.title}
          onChange={handleChange}
          disabled={isLoading}
          className={fieldErrors.title ? styles.inputError : ""}
        />

        {fieldErrors.title?.map((error, index) => (
          <p key={index} className={styles.fieldError}>
            {error}
          </p>
        ))}
      </div>

      <div className={styles.formField}>
        <label htmlFor="priority">Priority</label>

        <select
          id="priority"
          name="priority"
          value={formData.priority}
          onChange={handleChange}
          disabled={isLoading}
          className={fieldErrors.priority ? styles.inputError : ""}
        >
          {Object.values(TicketPriority).map((priority) => (
            <option key={priority} value={priority}>
              {priority}
            </option>
          ))}
        </select>

        {fieldErrors.priority?.map((error, index) => (
          <p key={index} className={styles.fieldError}>
            {error}
          </p>
        ))}
      </div>

      <div className={styles.formField}>
        <label htmlFor="description">Description</label>

        <textarea
          id="description"
          name="description"
          value={formData.description}
          onChange={handleChange}
          disabled={isLoading}
          className={fieldErrors.description ? styles.inputError : ""}
          rows={6}
        />

        {fieldErrors.description?.map((error, index) => (
          <p key={index} className={styles.fieldError}>
            {error}
          </p>
        ))}
      </div>

      <div className={styles.formActions}>
        <button type="submit" disabled={isLoading}>
          {isLoading ? "Submitting ticket..." : "Submit ticket"}
        </button>
      </div>
    </form>
  );
}
