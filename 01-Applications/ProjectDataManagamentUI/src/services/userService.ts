import { authApi } from "../api/authApi";

export const getUserDetails = async () => {
  const token = localStorage.getItem("token");
  if (!token) throw new Error("Brak tokenu");

  const res = await authApi.getProfile(token);

  if (!res.ok) throw new Error("Nie udało się pobrać profilu");

  return await res.json();
};
