import { Role } from "../types/authTypes";

export function getAuthenticatedUserPath(roles: Role[]): string {
  if (roles.includes(Role.ADMINISTRATOR)) {
    return "/";
  }

  if (roles.includes(Role.SUPPORTAGENT)) {
    return "/";
  }

  if (roles.includes(Role.CUSTOMER)) {
    return "/tickets/my-tickets";
  }

  return "/unauthorized";
}
