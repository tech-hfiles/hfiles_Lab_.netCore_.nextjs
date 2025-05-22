// src/utils/axiosClient.ts
import axios from "axios";
import { toast } from "react-toastify";

// Get token from localStorage
const getUserToken = () => {
  return localStorage.getItem("authToken");
};

// Request Interceptor
const requestHandler = (request : any) => {
  document.body.classList.add("loading-indicator");

  const token = getUserToken();
  if (token) {
    request.headers["Authorization"] = `Bearer ${token}`;
  }

  return request;
};

// Response Interceptor (Success)
const successHandler = (response : any) => {
  document.body.classList.remove("loading-indicator");
  return response;
};

// Response Interceptor (Error)
const errorHandler = (error : any) => {
  document.body.classList.remove("loading-indicator");

  const errorMessage =
    error?.response?.data?.errorMessage || "An unexpected error occurred";

  switch (error?.response?.status) {
    case 401:
    case 404:
    default:
      toast.error(errorMessage);
      break;
  }

  return Promise.reject({ ...error });
};

// Axios Instance
const axiosInstance = axios.create({
  baseURL: "https://localhost:7227/api/", // Change if needed
});

// Attach interceptors
axiosInstance.interceptors.request.use(requestHandler);
axiosInstance.interceptors.response.use(successHandler, errorHandler);

export default axiosInstance;
