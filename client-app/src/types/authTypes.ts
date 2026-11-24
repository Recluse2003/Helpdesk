export const Role = {
  ADMINISTRATOR: "Administrator",
  SUPPORTAGENT: "SupportAgent",
  CUSTOMER: "Customer",
} as const;

export type Role = (typeof Role)[keyof typeof Role];

export interface User {
  id: string;
  displayName: string;
  email: string;
  roles: Role[];
}
