import { authApi } from "../api/authApi";

export async function getUserDetails() {
  try {
    const res = await authApi.get("/User/me");
    return res.data;
  } catch (err) {
    console.error("Błąd podczas pobierania danych użytkownika", err);
    return null;
  }
}
