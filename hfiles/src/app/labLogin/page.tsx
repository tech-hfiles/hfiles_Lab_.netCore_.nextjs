"use client";
import React, { useState, useEffect } from "react";
import { useFormik } from "formik";
import * as Yup from "yup";
import Home from "../components/Home";
import { LoginOTP, LoginPassword, SendOTP } from "@/services/labServiceApi";
import { toast, ToastContainer } from "react-toastify";
import { useRouter } from "next/navigation";

const LoginPage = () => {
  const router = useRouter();
  const [showOtp, setShowOtp] = useState(false);
  const [showPasswordLogin, setShowPasswordLogin] = useState(false);
  const [timer, setTimer] = useState(60);
  const [isResendDisabled, setIsResendDisabled] = useState(true);
  const [showPassword, setShowPassword] = useState(false);
  const [isSubmittingOTP, setIsSubmittingOTP] = useState(false);

  useEffect(() => {
    let interval = null;
    if (showOtp && timer > 0) {
      interval = setInterval(() => {
        setTimer((prevTimer) => prevTimer - 1);
      }, 1000);
    } else if (timer === 0) {
      setIsResendDisabled(false);
    }

    return () => {
      if (interval) clearInterval(interval);
    };
  }, [showOtp, timer]);

  const formatTime = (seconds: any) => {
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = seconds % 60;
    return `${minutes.toString().padStart(2, "0")}:${remainingSeconds
      .toString()
      .padStart(2, "0")}`;
  };

  const emailValidationSchema = Yup.object({
    email: Yup.string()
      .matches(
        /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/,
        "Please enter a valid email address"
      )
       .required("Email is required"),
  });

  const otpValidationSchema = Yup.object({
    otp: Yup.string()
      .matches(/^\d+$/, "OTP must contain only digits")
      .length(6, "OTP must be exactly 6 digits")
      .required("OTP is required"),
  });

  const passwordLoginValidationSchema = Yup.object({
    email: Yup.string().required("Email ID / Contact No. is required"),
    password: Yup.string().required("Password is required"),
  });

  const emailFormik = useFormik({
    initialValues: {
      email: "",
    },
    validationSchema: emailValidationSchema,
    onSubmit: async (values) => {
      setIsSubmittingOTP(true);
      try {
        await SendOTP({
          email: values.email,
        });
        toast.success("OTP sent successfully!");
      } catch (error) {
        toast.error("Failed to send OTP. Please try again.");
        console.error("Failed to send OTP:", error);
      } finally {
        setIsSubmittingOTP(false);
      }
      console.log("Form submitted with:", values);
    },
  });

  const otpFormik = useFormik({
    initialValues: {
      email: "",
      otp: "",
    },
    validationSchema: otpValidationSchema,
    onSubmit: async (values) => {
      try {
     const response = await LoginOTP({
          email: emailFormik.values.email,
          otp: values.otp,
        });
         localStorage.setItem("authToken", response.data.token);
        toast.success("Login successful!");
        console.log("OTP login submitted:", values);
        setTimeout(() => {
          router.push("/labHome");
        }, 2000);
      } catch (error) {
        toast.error("Login failed. Please try again.");
        console.error("Login failed:", error);
      }
      console.log("OTP submitted:", values.otp);
    },
  });

  const passwordLoginFormik = useFormik({
    initialValues: {
      email: "",
      password: "",
    },
    validationSchema: passwordLoginValidationSchema,
    onSubmit: async (values) => {
      try {
    const response = await LoginPassword({
          email: values.email,
          password: values.password,
        });
         localStorage.setItem("authToken", response.data.token);
        toast.success("Login successful!");
        console.log("Password login submitted:", values);
        setTimeout(() => {
          router.push("/labHome");
        }, 2000);
      } catch (error) {
        toast.error("Login failed. Please try again.");
        console.error("Login failed:", error);
      }
      console.log("Password login submitted:", values);
    },
  });

  const handleGetOTP = async () => {
    const errors = await emailFormik.validateForm();

    if (Object.keys(errors).length === 0) {
      try {
        await emailFormik.submitForm();
        setShowOtp(true);
        setShowPasswordLogin(false);
        setTimer(60);
        setIsResendDisabled(true);
      } catch (error) {
        console.error("Error while sending OTP:", error);
      }
    } else {
      emailFormik.setTouched({ email: true });
    }
  };

  const handlePasswordLogin = () => {
    emailFormik.validateForm().then((errors) => {
      if (Object.keys(errors).length === 0) {
        passwordLoginFormik.setFieldValue("email", emailFormik.values.email);
        setShowPasswordLogin(true);
        setShowOtp(false);
      } else {
        emailFormik.setTouched({ email: true });
      }
    });
  };

  const handleResendOTP = async () => {
    if (!isResendDisabled) {
      try {
        await emailFormik.submitForm();
        toast.success("OTP sent successfully!");
        setTimer(60);
        setIsResendDisabled(true);
      } catch (error) {
        console.error("Error resending OTP:", error);
      }
    }
  };

  const handleBackToLogin = () => {
    setShowOtp(false);
    setShowPasswordLogin(false);
    otpFormik.resetForm();
    passwordLoginFormik.resetForm();
  };

  const togglePasswordVisibility = () => {
    setShowPassword(!showPassword);
  };


  // Step 1: Get token from localStorage
  const token = localStorage.getItem("authToken");

  if (token) {
    // Step 2: Decode the payload
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
      return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
    }).join(''));
    // Step 3: Parse the JSON payload
    const data = JSON.parse(jsonPayload);
     localStorage.setItem("UserId", data.UserId);
  } else {
    console.log("No authToken found in localStorage.");
  }



  return (
    <Home>
      <div className="w-full flex items-center justify-center p-8 mt-2 px-4">
        <div className="w-full max-w-md bg-blue-800 rounded-xl shadow-lg p-8 flex flex-col items-center">
          <div className="bg-blue-800 rounded-full p-3 mb-5">
            <div className="text-white font-bold text-xl flex items-center">
              <img
                src="/104075137c369bb0056ee4f80f11111c548425f6.png"
                alt="Profile"
                className="w-25 h-full object-cover"
              />
            </div>
          </div>

          <h1 className="text-white text-3xl font-bold mb-8">
            {showOtp || showPasswordLogin ? "Welcome Back!" : "Welcome!"}
          </h1>

          {!showOtp && !showPasswordLogin && (
            <form onSubmit={emailFormik.handleSubmit} className="w-full">
              {/* Email field */}
              <div className="w-full mb-2">
                <input
                  type="email"
                  id="email"
                  name="email"
                  placeholder="User ID / Email ID"
                  value={emailFormik.values.email}
                  onChange={emailFormik.handleChange}
                  onBlur={emailFormik.handleBlur}
                  className={`w-full px-4 py-3 rounded-full text-gray-800 focus:outline-none bg-gray-100 ${
                    emailFormik.touched.email && emailFormik.errors.email
                      ? "border-2 border-red-500"
                      : ""
                  }`}
                />
              </div>
              {emailFormik.touched.email && emailFormik.errors.email && (
                <div className="text-red-500 text-xs mb-2">
                  {emailFormik.errors.email}
                </div>
              )}

              <button
                type="button"
                onClick={handleGetOTP}
                disabled={isSubmittingOTP}
                className={`w-full text-blue-900 font-medium py-3 rounded-full mb-2 transition duration-300
                ${
                  isSubmittingOTP
                    ? "bg-yellow-300 cursor-not-allowed"
                    : "bg-yellow-400 hover:bg-yellow-500"
                }`}
              >
                {isSubmittingOTP ? "Sending..." : "Get OTP"}
              </button>

              <div className="text-white text-sm my-2 text-center">or</div>

              <button
                type="button"
                onClick={handlePasswordLogin}
                className="w-full bg-yellow-400 text-blue-900 font-medium py-3 rounded-full hover:bg-yellow-500 transition duration-300"
              >
                Login with Password
              </button>
            </form>
          )}

          {showOtp && (
            <form onSubmit={otpFormik.handleSubmit} className="w-full">
              <div className="w-full mb-4 bg-gray-100 px-4 py-3 rounded-full text-gray-800">
                {emailFormik.values.email}
              </div>

              {/* OTP field */}
              <div className="w-full mb-2">
                <input
                  type="text"
                  id="otp"
                  name="otp"
                  placeholder="Enter OTP"
                  value={otpFormik.values.otp}
                  onChange={otpFormik.handleChange}
                  onBlur={otpFormik.handleBlur}
                  className={`w-full px-4 py-3 rounded-full text-gray-800 focus:outline-none bg-gray-100 ${
                    otpFormik.touched.otp && otpFormik.errors.otp
                      ? "border-2 border-red-500"
                      : ""
                  }`}
                  maxLength={6}
                />
              </div>
              {otpFormik.touched.otp && otpFormik.errors.otp && (
                <div className="text-red-500 text-xs mb-2">
                  {otpFormik.errors.otp}
                </div>
              )}

              {/* Timer display */}
              <div className="text-white text-right mb-2">
                {formatTime(timer)}
              </div>

              <button
                type="submit"
                className="w-full bg-yellow-400 text-blue-900 font-medium py-3 rounded-full mb-4 hover:bg-yellow-500 transition duration-300"
              >
                SIGN IN
              </button>

              <div className="text-white text-sm my-2 text-center">Or</div>

              <button
                type="button"
                onClick={handlePasswordLogin}
                className="w-full bg-yellow-400 text-blue-900 font-medium py-3 rounded-full mb-4 hover:bg-yellow-500 transition duration-300"
              >
                Login with Password
              </button>

              {/* Resend OTP link */}
              <div className="text-center">
                <button
                  type="button"
                  onClick={handleResendOTP}
                  disabled={isResendDisabled}
                  className={`text-white underline ${
                    isResendDisabled
                      ? "opacity-50 cursor-not-allowed"
                      : "cursor-pointer"
                  }`}
                >
                  Resend OTP
                </button>
              </div>
            </form>
          )}

          {showPasswordLogin && (
            <form
              onSubmit={passwordLoginFormik.handleSubmit}
              className="w-full"
            >
              {/* Email/Contact field */}
              <div className="w-full mb-4">
                <input
                  type="text"
                  id="email"
                  name="email"
                  placeholder="Email Id / Contact No."
                  value={passwordLoginFormik.values.email}
                  onChange={passwordLoginFormik.handleChange}
                  onBlur={passwordLoginFormik.handleBlur}
                  className={`w-full px-4 py-3 rounded-full text-gray-800 focus:outline-none bg-gray-100 ${
                    passwordLoginFormik.touched.email &&
                    passwordLoginFormik.errors.email
                      ? "border-2 border-red-500"
                      : ""
                  }`}
                />
              </div>
              {passwordLoginFormik.touched.email &&
                passwordLoginFormik.errors.email && (
                  <div className="text-red-500 text-xs mb-2">
                    {passwordLoginFormik.errors.email}
                  </div>
                )}

              <div className="w-full mb-2 relative">
                <input
                  type={showPassword ? "text" : "password"}
                  id="password"
                  name="password"
                  placeholder="Password"
                  value={passwordLoginFormik.values.password}
                  onChange={passwordLoginFormik.handleChange}
                  onBlur={passwordLoginFormik.handleBlur}
                  className={`w-full px-4 py-3 rounded-full text-gray-800 focus:outline-none bg-gray-100 ${
                    passwordLoginFormik.touched.password &&
                    passwordLoginFormik.errors.password
                      ? "border-2 border-red-500"
                      : ""
                  }`}
                />
                <button
                  type="button"
                  onClick={togglePasswordVisibility}
                  className="absolute right-4 top-1/2 transform -translate-y-1/2"
                >
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    width="24"
                    height="24"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    className="text-gray-500"
                  >
                    {showPassword ? (
                      <>
                        <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
                        <circle cx="12" cy="12" r="3"></circle>
                      </>
                    ) : (
                      <>
                        <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path>
                        <line x1="1" y1="1" x2="23" y2="23"></line>
                      </>
                    )}
                  </svg>
                </button>
              </div>
              {passwordLoginFormik.touched.password &&
                passwordLoginFormik.errors.password && (
                  <div className="text-red-500 text-xs mb-2">
                    {passwordLoginFormik.errors.password}
                  </div>
                )}

              <div className="text-right mb-4">
                <a href="#" className="text-yellow-400 text-sm">
                  Forgot Password ?
                </a>
              </div>

              <button
                type="submit"
                className="w-full bg-yellow-400 text-blue-900 font-medium py-3 rounded-full mb-4 hover:bg-yellow-500 transition duration-300"
              >
                Login
              </button>

              <div className="text-white text-sm my-2 text-center">Or</div>

              {/* Login with OTP button */}
              <button
                type="button"
                onClick={async () => {
                  setShowOtp(true);
                  setShowPasswordLogin(false);
                  await emailFormik.submitForm();
                  setTimer(60);
                }}
                className="w-full bg-red-500 text-white font-medium py-3 rounded-full mb-4 hover:bg-red-600 transition duration-300"
              >
                Login with OTP
              </button>
            </form>
          )}

          <div className="text-white text-center mt-4">
            New User? Click{" "}
            <a href="#" className="text-yellow-400" onClick={() => window.location.href = '/'}>
              here
            </a>{" "}
            to Sign Up
          </div>
        </div>
        <ToastContainer />
      </div>
    </Home>
  );
};

export default LoginPage;
