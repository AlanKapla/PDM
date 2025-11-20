import { authApi } from "../api/authApi";

export const registerUser = async (form: any) => {
  const payload = {
    email: form.email,
    password: form.password,
    externalToken: "",
    provider: 0
  };

  const res = await authApi.register(payload);

  return res.ok;
};

export const loginUser = async (form: any) => {
  const payload = {
    email: form.email,
    password: form.password,
    externalToken: "",
    provider: 0
  };

  const res = await authApi.login(payload);

  if (!res.ok) return null;

  const json = await res.json();

  return {
    token: json.accessToken,
    refreshToken: json.refreshToken
  };
};

export const getUserDetails = async () => {
  const res = await authApi.getProfile();

  if (!res.ok) return null;

  return res.json();
};
