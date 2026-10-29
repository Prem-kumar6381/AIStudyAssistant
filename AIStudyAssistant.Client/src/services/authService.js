import apiClient, { TOKEN_STORAGE_KEY } from './apiClient';

/**
 * Registers a new user account.
 * @param {string} displayName - The user's full name or nickname.
 * @param {string} email - The user's email address.
 * @param {string} password - The user's password.
 * @returns {Promise<{ token: string, user: object, expiresAt: string }>}
 */
export async function register(displayName, email, password) {
  const response = await apiClient.post('/api/auth/register', {
    displayName: displayName.trim(),
    email: email.trim(),
    password,
  });

  if (response.data?.token) {
    saveToken(response.data.token);
  }

  return response.data;
}

/**
 * Authenticates an existing user.
 * @param {string} email - The user's email.
 * @param {string} password - The user's password.
 * @returns {Promise<{ token: string, user: object, expiresAt: string }>}
 */
export async function login(email, password) {
  const response = await apiClient.post('/api/auth/login', {
    email: email.trim(),
    password,
  });

  if (response.data?.token) {
    saveToken(response.data.token);
  }

  return response.data;
}

/**
 * Fetches the currently authenticated user's profile.
 * @returns {Promise<object>}
 */
export async function getCurrentUser() {
  const response = await apiClient.get('/api/auth/me');
  return response.data;
}

/**
 * Logs out the current user, clearing local tokens and calling the server logout endpoint.
 */
export async function logout() {
  try {
    await apiClient.post('/api/auth/logout');
  } catch {
    // Continue local cleanup even if network call fails
  } finally {
    clearToken();
  }
}

export function saveToken(token) {
  try {
    localStorage.setItem(TOKEN_STORAGE_KEY, token);
  } catch {
    // Sandbox fallback
  }
}

export function getToken() {
  try {
    return localStorage.getItem(TOKEN_STORAGE_KEY);
  } catch {
    return null;
  }
}

export function clearToken() {
  try {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
  } catch {
    // Ignore
  }
}

export default {
  register,
  login,
  getCurrentUser,
  logout,
  saveToken,
  getToken,
  clearToken,
};
