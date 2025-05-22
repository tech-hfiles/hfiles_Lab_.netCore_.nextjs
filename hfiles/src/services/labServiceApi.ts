import { API_Lab_Reports, endPoints } from "@/apiEndPoints";
import axiosInstance from "@/utils/axiosClient";
import axios from "axios";
// import axiosInstance from "@/utils/axiosClient";

// const axiosInstance = axios.create({
//   baseURL: API_Lab_Reports,
// });

// // Attach token in headers
// axiosInstance.interceptors.request.use(
//   function (config) {
//     const token = localStorage.getItem("token"); // Or "authToken" if you prefer
//     if (token) {
//       config.headers["Authorization"] = `Bearer ${token}`;
//     }
//     return config;
//   },
//   function (error) {
//     return Promise.reject(error);
//   }
// );
// lab Signup

export const otpGenerate = async (data: any) => {
  return axios.post(`${endPoints.Lab_Reports.OTP_GENERATE}`, data);
};

export const signUp = async (data: any) => {
  return axios.post(`${endPoints.Lab_Reports.SIGN_UP}`, data);
};

// Lab Login

export const SendOTP = async (data: any) => {
  return axios.post(`${endPoints.Lab_Login.LOGIN_SEND_OTP}`, data);
};

export const LoginOTP = async (data: any) => {
  return axios.post(`${endPoints.Lab_Login.LOGIN_OTP}`, data);
};

export const LoginPassword = async (data: any) => {
  return axios.post(`${endPoints.Lab_Login.LOGIN_PASSWORD}`, data);
};

// Lab User Reports
// axios Instance pass the token in api header

export const AddLabUserReport = async (data:any) => {
  return axiosInstance.post(endPoints.Lab_User_Reports.ADD_LAb_USER_REPORT, data);
};


