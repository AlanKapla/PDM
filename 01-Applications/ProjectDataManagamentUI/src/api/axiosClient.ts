import axios from "axios";

export const axiosClient = axios.create({
  baseURL: "http://localhost:5121/api",
  withCredentials: true,
});