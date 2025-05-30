'use client'
import React, { useEffect, useState } from "react";
import { useFormik } from "formik";
import * as Yup from "yup";
import { AdminLogin, UserCardList } from "@/services/labServiceApi";
import { toast, ToastContainer } from "react-toastify";
import Home from "../components/Home";
import { useRouter } from "next/navigation";

const AdminLogins = () => {
  const router = useRouter();

  const [showLoginForm, setShowLoginForm] = useState(false);
  const [cardListData, setCardListData] = useState<any[]>([]);
  const [memberListData, setMemberListData] = useState<any[]>([]);

  const userId = localStorage.getItem("userId");
  const email = localStorage.getItem("emailId");

  const formik = useFormik({
    initialValues: {
      userId: 0,
      email: "",
      hfid: "",
      role: "Super Admin",
      password: "",
    },
    validationSchema: Yup.object({
      hfid: Yup.string().required("HFID is required"),
      role: Yup.string().required("Role is required"),
      password: Yup.string().required("Password is required"),
    }),
    onSubmit: async (values, { resetForm }) => {
      try {
        const payload = {
          userId: Number(userId),
          email: email,
          hfid: values.hfid,
          role: values.role,
          password: values.password,
        };
        const response = await AdminLogin(payload);
        toast.success("Admin Login successfully!");
        localStorage.setItem("authToken", response.data.token);
        localStorage.setItem("username", response.data.username);
        router.push("/labHome");
        resetForm();
      } catch (error) {
        console.error("Error logging in:", error);
        toast.error("Invalid password.");
      }
    },
  });

  // Step 1: Get token from localStorage
  const token = localStorage.getItem("authToken");

  if (token) {
    try {
      // Step 2: Decode the payload
      const base64Url = token.split('.')[1];
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );
      const data = JSON.parse(jsonPayload);
      localStorage.setItem("LabAdminId", data.LabAdminId);

    } catch (error) {
      console.error("Failed to decode JWT token:", error);
    }
  } else {
    console.log("No authToken found in localStorage.");
  }

  const BASE_URL = "https://hfiles.in/upload/";

  const CardList = async () => {
    const res = await UserCardList(Number(userId));
    setCardListData(res.data.superAdmin);
    setMemberListData(res.data.members);
  }

  useEffect(() => {
    CardList();
  }, [])

  return (
    <Home>
      <div className="mt-10 p-6 border rounded shadow mx-8">
        <h1 className="text-2xl font-bold mb-4">Lab users</h1>
        <div className="border mb-4"></div>

        <div className="flex flex-wrap justify-center gap-4">
          {/* {cardListData.map((user) => ( */}
            <div
              key={cardListData.userId}
              className="bg-blue-100 rounded-lg flex flex-col sm:flex-row sm:items-center border p-4 shadow-md cursor-pointer"
              onClick={() => {
                setShowLoginForm(true);
                formik.setFieldValue("hfid", cardListData.hfid);
              }}
            >
              <div className="sm:mr-4 flex justify-center sm:justify-start">
                <div className="w-16 h-16 bg-gray-200 rounded-full overflow-hidden">
                  {cardListData.profilePhoto !== "No image preview available" ? (
                    <img
                      src={`${BASE_URL}${cardListData.profilePhoto}`}
                      alt={cardListData.name}
                      className="w-full h-full object-cover"
                    />
                  ) : (
                    <div className="w-full h-full flex items-center justify-center text-xs text-gray-500">
                      No Image
                    </div>
                  )}
                </div>
              </div>
              <div className="text-center sm:text-left mt-3 sm:mt-0 sm:ml-3">
                <h2 className="text-lg font-semibold">{cardListData.name}</h2>
                <p className="text-sm text-gray-700">Role: {cardListData.role}</p>
                <p className="text-xs text-gray-500">{cardListData.email}</p>
              </div>
            </div>

          {/* ))} */}
        </div>
      </div>


           <div className="mt-10 p-6 border rounded shadow mx-8">
        <h1 className="text-2xl font-bold mb-4">Lab Members</h1>
        <div className="border mb-4"></div>

        <div className="flex flex-wrap justify-center gap-4">
          {memberListData.map((user) => (
            <div
              key={user.userId}
              className="bg-blue-100 rounded-lg flex flex-col sm:flex-row sm:items-center border p-4 shadow-md cursor-pointer"
              onClick={() => {
                setShowLoginForm(true);
                formik.setFieldValue("hfid", user.hfid);
              }}
            >
              <div className="sm:mr-4 flex justify-center sm:justify-start">
                <div className="w-16 h-16 bg-gray-200 rounded-full overflow-hidden">
                  {user.profilePhoto !== "No image preview available" ? (
                    <img
                      src={`${BASE_URL}${user.profilePhoto}`}
                      alt={user.name}
                      className="w-full h-full object-cover"
                    />
                  ) : (
                    <div className="w-full h-full flex items-center justify-center text-xs text-gray-500">
                      No Image
                    </div>
                  )}
                </div>
              </div>
              <div className="text-center sm:text-left mt-3 sm:mt-0 sm:ml-3">
                <h2 className="text-lg font-semibold">{user.name}</h2>
                <p className="text-sm text-gray-700">Role: {user.role}</p>
                <p className="text-xs text-gray-500">{user.email}</p>
              </div>
            </div>

          ))}
        </div>
      </div>




      {showLoginForm && (
        <div className="max-w-md mx-auto mt-10 p-6 border rounded shadow">
          <h2 className="text-xl font-bold mb-4">Lab Admin Login</h2>
          <form onSubmit={formik.handleSubmit}>
            <div className="mb-4">
              <label className="block font-medium">HFID</label>
              <input
                type="text"
                name="hfid"
                onChange={formik.handleChange}
                onBlur={formik.handleBlur}
                value={formik.values.hfid}
                className="w-full p-2 border rounded"
              />
              {formik.touched.hfid && formik.errors.hfid && (
                <div className="text-red-600 text-sm">{formik.errors.hfid}</div>
              )}
            </div>

            <div className="mb-4">
              <label className="block font-medium">Role</label>
              <input
                type="text"
                name="role"
                onChange={formik.handleChange}
                onBlur={formik.handleBlur}
                value={formik.values.role}
                disabled
                className="w-full p-2 border rounded"
              />
              {formik.touched.role && formik.errors.role && (
                <div className="text-red-600 text-sm">{formik.errors.role}</div>
              )}
            </div>

            <div className="mb-4">
              <label className="block font-medium">Password</label>
              <input
                type="password"
                name="password"
                onChange={formik.handleChange}
                onBlur={formik.handleBlur}
                value={formik.values.password}
                className="w-full p-2 border rounded"
              />
              {formik.touched.password && formik.errors.password && (
                <div className="text-red-600 text-sm">{formik.errors.password}</div>
              )}
            </div>

            <button
              type="submit"
              className="w-full bg-blue-600 text-white p-2 rounded hover:bg-blue-700"
            >
              Login
            </button>
          </form>
          <ToastContainer />
        </div>
      )}
    </Home>
  );
};

export default AdminLogins;
