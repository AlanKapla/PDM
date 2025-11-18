import { authApi } from "../api/authApi";

export const registerUser = async (form: any) => {
  try {
    const res = await authApi.post("/User/register", form);
    return res.data; 
  } catch (err) {
    return null; 
  }
};

export const loginUser = async (form: any) => {
  try {
    const res = await authApi.post("/User/login", form);

    const data = res.data;

    localStorage.setItem("token", data.token);

    return data;
  } catch (err) {
    return null;
  }
};

export const getCurrentUser = async () => {
  try {
    const res = await authApi.get("/User/me");
    return res.data;
  } catch (err) {
    return null;
  }
};
