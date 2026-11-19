import { Role } from "../types/auth";

export function getAuthenticatedUserPath(roles: Role[]): string {
  if (roles.includes(Role.ADMINISTRATOR)) {
    return "/";
  }

  if (roles.includes(Role.SUPPORTAGENT)) {
    return "/";
  }

  if (roles.includes(Role.CUSTOMER)) {
    return "/my-tickets";
  }

  return "/unauthorized";
}
