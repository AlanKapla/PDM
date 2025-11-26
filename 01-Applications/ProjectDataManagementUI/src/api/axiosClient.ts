import axios from "axios";

export const axiosClient = axios.create({
  baseURL: "/api",
  withCredentials: true,
});

let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value?: any) => void;
  reject: (reason?: any) => void;
}> = [];

const processQueue = (error: any = null) => {
  failedQueue.forEach((promise) => {
    if (error) {
      promise.reject(error);
    } else {
      promise.resolve();
    }
  });

  failedQueue = [];
};

// Interceptor do obsługi wygasłych tokenów z refresh flow
axiosClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // Jeśli to 401 i nie próbowaliśmy jeszcze odświeżyć tokenu
    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        // Kolejny request czeka na refresh
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then(() => {
            return axiosClient(originalRequest);
          })
          .catch((err) => {
            return Promise.reject(err);
          });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        // Spróbuj odświeżyć token
        const refreshResponse = await axios.post(
          "/api/User/refresh",
          {},
          { withCredentials: true, validateStatus: (status) => status < 500 }
        );

        if (refreshResponse.status === 401) {
          // Refresh token też wygasł - sesja wygasła
          processQueue(new Error("Session expired"));
          console.warn("Sesja wygasła - przekierowanie na login");
          window.location.href = "/login";
          return Promise.reject(new Error("Session expired"));
        }

        // Sukces - przetwórz kolejkę i ponów oryginalny request
        processQueue();
        return axiosClient(originalRequest);
      } catch (refreshError) {
        // Błąd sieciowy lub inny problem
        processQueue(refreshError);
        console.error("Błąd odświeżania tokenu:", refreshError);
        window.location.href = "/login";
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);