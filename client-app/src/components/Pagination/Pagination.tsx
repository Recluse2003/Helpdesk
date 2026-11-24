import {
  createContext,
  useContext,
  useEffect,
  useState,
  type ComponentProps,
  type HTMLAttributes,
  type ReactElement,
} from "react";

export interface PaginationContextValue {
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  siblingCount?: number | null;
}

export const PaginationContext = createContext<PaginationContextValue | null>(
  null,
);

export function usePagination(): PaginationContextValue {
  const context = useContext(PaginationContext);

  if (!context) {
    throw new Error(
      "usePagination must be used within a <Pagination> provider",
    );
  }

  return context;
}

interface PaginationProps extends HTMLAttributes<HTMLDivElement> {
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  siblingCount?: number | null;
  children?: ReactElement;
}

export function Pagination({
  currentPage,
  totalPages,
  onPageChange,
  siblingCount,
  children,
  ...props
}: PaginationProps) {
  return (
    <PaginationContext.Provider
      value={{ currentPage, totalPages, onPageChange, siblingCount }}
    >
      <nav aria-label="Pagination" {...props}>
        {children}
      </nav>
    </PaginationContext.Provider>
  );
}

function First({
  children,
  onClick,
  disabled,
  ...props
}: ComponentProps<"button">) {
  const { currentPage, onPageChange } = usePagination();
  const isDisabled = disabled || currentPage <= 1;

  return (
    <button
      type="button"
      disabled={isDisabled}
      data-disabled={isDisabled ? "true" : undefined}
      onClick={(e) => {
        onClick?.(e);
        if (!isDisabled) onPageChange(1);
      }}
      {...props}
    >
      {children ?? "First"}
    </button>
  );
}

function Prev({
  children,
  onClick,
  disabled,
  ...props
}: ComponentProps<"button">) {
  const { currentPage, onPageChange } = usePagination();
  const isDisabled = disabled || currentPage <= 1;

  return (
    <button
      type="button"
      disabled={isDisabled}
      data-disabled={isDisabled ? "true" : undefined}
      onClick={(e) => {
        onClick?.(e);
        if (!isDisabled) onPageChange(currentPage - 1);
      }}
      {...props}
    >
      {children ?? "Prev"}
    </button>
  );
}

export interface PageProps extends ComponentProps<"button"> {
  page: number;
}

function Page({ page, children, onClick, disabled, ...props }: PageProps) {
  const { currentPage, onPageChange } = usePagination();
  const isActive = currentPage === page;

  return (
    <button
      type="button"
      disabled={disabled}
      aria-current={isActive ? "page" : undefined}
      data-active={isActive ? "true" : undefined}
      onClick={(e) => {
        onClick?.(e);
        if (!disabled) onPageChange(page);
      }}
      {...props}
    >
      {children ?? page}
    </button>
  );
}

function Next({
  children,
  onClick,
  disabled,
  ...props
}: ComponentProps<"button">) {
  const { currentPage, totalPages, onPageChange } = usePagination();
  const isDisabled = disabled || currentPage === totalPages;

  return (
    <button
      type="button"
      disabled={isDisabled}
      data-disabled={isDisabled ? "true" : undefined}
      onClick={(e) => {
        onClick?.(e);
        if (!isDisabled) onPageChange(currentPage + 1);
      }}
      {...props}
    >
      {children ?? "Next"}
    </button>
  );
}

function Last({
  children,
  onClick,
  disabled,
  ...props
}: ComponentProps<"button">) {
  const { currentPage, totalPages, onPageChange } = usePagination();
  const isDisabled = disabled || currentPage === totalPages;

  return (
    <button
      type="button"
      disabled={isDisabled}
      data-disabled={isDisabled ? "true" : undefined}
      onClick={(e) => {
        onClick?.(e);
        if (!isDisabled) onPageChange(totalPages);
      }}
      {...props}
    >
      {children ?? "Last"}
    </button>
  );
}

function JumpTo({ onKeyDown, ...props }: ComponentProps<"input">) {
  const { currentPage, totalPages, onPageChange } = usePagination();
  const [inputValue, setInputValue] = useState(String(currentPage));

  useEffect(() => {
    setInputValue(String(currentPage));
  }, [currentPage]);

  function handleKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    onKeyDown?.(e);

    if (e.key === "Enter") {
      const target = Number(inputValue);

      if (target >= 1 && target <= totalPages) {
        onPageChange(target);
      } else {
        setInputValue(String(currentPage));
      }
    }
  }

  return (
    <input
      type="text"
      inputMode="numeric"
      value={inputValue}
      onChange={(e) => setInputValue(e.target.value)}
      onKeyDown={handleKeyDown}
      aria-label="Jump to page"
      {...props}
    />
  );
}

Pagination.First = First;
Pagination.Prev = Prev;
Pagination.Page = Page;
Pagination.Next = Next;
Pagination.Last = Last;
Pagination.JumpTo = JumpTo;
