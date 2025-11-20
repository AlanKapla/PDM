import { authApi } from "../api/authApi";

export const getUserDetails = async () => {
  const token = localStorage.getItem("token");
  if (!token) return null;

  const res = await authApi.getProfile();

  if (!res.ok) return null;

  return res.json();
};
