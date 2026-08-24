import { me, type AuthUser } from './api';

let currentUser: AuthUser | null = null;
let loaded = false;

export function getCurrentUser() {
  return currentUser;
}

export async function ensureAuthenticated() {
  if (loaded) return currentUser;

  try {
    currentUser = await me();
  } catch {
    currentUser = null;
  } finally {
    loaded = true;
  }

  return currentUser;
}

export function setCurrentUser(user: AuthUser | null) {
  currentUser = user;
  loaded = true;
}
