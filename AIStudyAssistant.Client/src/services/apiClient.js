import axios from 'axios';

// Base API URL configured via Vite environment variable
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';
const TOKEN_STORAGE_KEY = 'ai_study_assistant_token_v1';

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true, // Enable sending HttpOnly cookies cross-origin
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 35000,
});

// Attach JWT Bearer token if present in localStorage or memory
apiClient.interceptors.request.use(
  (config) => {
    try {
      const token = localStorage.getItem(TOKEN_STORAGE_KEY);
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
    } catch {
      // Ignore storage access errors in restricted sandbox environments
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor to normalize errors and handle 401s
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      try {
        localStorage.removeItem(TOKEN_STORAGE_KEY);
      } catch {
        // Ignore
      }
      // Dispatch custom event so AuthContext can handle logout
      window.dispatchEvent(new CustomEvent('auth:session-expired'));
    }

    if (error.response) {
      const serverError =
        error.response.data?.error ||
        error.response.data?.title ||
        (typeof error.response.data === 'string' ? error.response.data : null) ||
        `Server error (${error.response.status}).`;

      const err = new Error(serverError);
      err.status = error.response.status;
      err.data = error.response.data;
      return Promise.reject(err);
    } else if (error.request) {
      const err = new Error(
        `Unable to reach backend service. Please ensure the API is running at ${API_BASE_URL}.`
      );
      err.isNetworkError = true;
      return Promise.reject(err);
    }

    return Promise.reject(new Error(error.message || 'An unexpected error occurred.'));
  }
);

export { TOKEN_STORAGE_KEY, API_BASE_URL };
export default apiClient;
