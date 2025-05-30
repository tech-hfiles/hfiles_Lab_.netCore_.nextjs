import { API_Lab_Reports, endPoints } from "@/apiEndPoints";
import axiosInstance from "@/utils/axiosClient";
import axios from "axios";


export const otpGenerate = async (data: any) => {
  return axios.post(`${endPoints.Lab_Reports.OTP_GENERATE}`, data);
};

export const signUp = async (data: any) => {
  return axios.post(`${endPoints.Lab_Reports.SIGN_UP}`, data);
};

export const SidebarData = async (email:string) => {
  return axiosInstance.get(`${endPoints.Lab_Reports.SIDEBAR_ID}?email=${email}`);
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

export const AddLabUserReport = async (data:FormData) => {
  return axiosInstance.post(`${endPoints.Lab_User_Reports.ADD_LAb_USER_REPORT}`, data);
};

export const ListReport = async (userId:number) => {
  return axiosInstance.get(`${endPoints.Lab_User_Reports.Report_LAB_USERReport}/${userId}`);
};

export const ListUser = async (labId: any, startDate?: any, endDate?: any) => {
  const baseUrl = endPoints.Lab_User_Reports.LIST_LAB_USERLIST;
  const params = new URLSearchParams();
 params.append("labId", labId);

  if (startDate) params.append("startDate", startDate);
  if (endDate) params.append("endDate", endDate);

  const finalUrl = params.toString() ? `${baseUrl}?${params.toString()}` : baseUrl;

  return axiosInstance.get(finalUrl);
};


export const AdminCreate = async (data:any) => {
  return axios.post(`${endPoints.Lab_Admin.CREATE_ADMIN}`, data);
}

export const HfidCheck = async (data:any) => {
  return axios.post(`${endPoints.Lab_Login.CHECK_HFID}`, data);
}

export const AdminLogin = async (data:any) => {
  return axios.post(`${endPoints.Lab_Admin.LOGIN_ADMIN}`, data);
}

export const UserCardList =async (labId:number) =>{
  return axios.get(`${endPoints.Lab_User_Reports.USERCARD}?labId=${labId}`);
}

export const ResendReport = async (payload: { ids: number[] }) => {
  return axiosInstance.post(`${endPoints.Lab_User_Reports.SEND_REPORT}`,payload);
}


// Profile api MiddleWare 

export const ListBranchData = async () => {
  return axiosInstance.get(`${endPoints.Lab_Profile.LIST_BRANCHDATA}`);
}

// All Members API
export const AddMember = async (data:any) => {
  return axiosInstance.post(`${endPoints.All_Members.ADD_Memeber}`, data);
}

export const DeleteMember = async (id:number) => {
  return axiosInstance.delete(`${endPoints.All_Members.DELETE_MEMBER}/${id}`);
}