import axios from "axios";
import { msalInstance } from "../main";
import { silentRequest } from "../config/authConfig";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || "https://localhost:5001";

console.log("=== AXIOS CLIENT CONFIG ===");
console.log("VITE_API_BASE_URL from env:", import.meta.env.VITE_API_BASE_URL);
console.log("Final API_BASE_URL:", API_BASE_URL);
console.log("Full baseURL:", `${API_BASE_URL}/api`);
console.log("===========================");

export const axiosClient = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  withCredentials: false, // Changed to false - using Bearer tokens instead of cookies
});

// Request interceptor to add access token
// Follows MSAL best practice: acquireTokenSilent first, then fallback to interactive
// See: https://learn.microsoft.com/en-us/entra/identity-platform/scenario-spa-acquire-token
axiosClient.interceptors.request.use(
  async (config) => {
    const accounts = msalInstance.getAllAccounts();
    
    if (accounts.length > 0) {
      const account = msalInstance.getActiveAccount() || accounts[0];
      
      try {
        // Try to acquire token silently from cache
        const response = await msalInstance.acquireTokenSilent({
          ...silentRequest,
          account: account,
        });
        
        // Add token to Authorization header
        config.headers.Authorization = `Bearer ${response.accessToken}`;
        
        console.log("🔑 Token added to request:", config.url);
        console.log("🔑 Token scopes:", response.scopes);
        console.log("🔑 Token expires:", new Date(response.expiresOn || 0).toLocaleString());
      } catch (error: any) {
        console.error("❌ acquireTokenSilent failed:", error.errorCode, error.errorMessage);
        
        // If silent acquisition fails, redirect to login
        // User will be redirected back after authentication
        console.warn("🔄 Redirecting to login for token acquisition");
        await msalInstance.loginRedirect(silentRequest);
        
        // Reject the request - it will be retried after redirect
        return Promise.reject(new Error("Token acquisition required - redirecting to login"));
      }
    } else {
      console.warn("⚠️ No accounts found - user not authenticated");
      // Don't add Authorization header - let the request proceed
      // Backend will return 401 and trigger error interceptor
    }
    
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor to handle 401 errors
axiosClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // If 401 Unauthorized
    if (error.response?.status === 401) {
      // Special case: /user/sync-b2c failed - token is invalid
      if (originalRequest.url?.includes('/user/sync-b2c')) {
        console.error("❌ Authentication failed at /user/sync-b2c - token is invalid");
        // Don't redirect here - ProtectedRoute will handle it when user becomes null
      }

      // For other endpoints, try to refresh token once
      if (!originalRequest._retry) {
        originalRequest._retry = true;

        const accounts = msalInstance.getAllAccounts();
        
        if (accounts.length > 0) {
          const account = msalInstance.getActiveAccount() || accounts[0];
          
          try {
            // Try to acquire a new token
            const response = await msalInstance.acquireTokenSilent({
              ...silentRequest,
              account: account,
              forceRefresh: true, // Force refresh to get a new token
            });
            
            // Update the Authorization header with new token
            originalRequest.headers.Authorization = `Bearer ${response.accessToken}`;
            
            // Retry the original request
            return axiosClient(originalRequest);
          } catch (tokenError) {
            console.error("❌ Token refresh failed:", tokenError);
            // Don't redirect - ProtectedRoute will handle when user is null
            return Promise.reject(tokenError);
          }
        } else {
          // No accounts - token acquisition failed
          console.warn("⚠️ No accounts found - user not authenticated");
          // Don't redirect - ProtectedRoute will handle it
        }
      }
    }

    return Promise.reject(error);
  }
);