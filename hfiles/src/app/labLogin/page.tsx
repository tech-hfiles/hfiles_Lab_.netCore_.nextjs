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
        // localStorage.setItem("userEmail", values.email);
        localStorage.setItem("emailId", values.email);
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
        //  localStorage.setItem("authToken", response.data.token);
        toast.success("Login successful!");
        localStorage.setItem("emailId", response.data.email);
        // router.push("/labAdminCreate");
        if (response.data.isSuperAdmin) {
          router.push("/labAdminLogin");
        } else {
          router.push("/labAdminCreate");
        }

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
        //  localStorage.setItem("authToken", response.data.token);
        // localStorage.setItem("authToken", response.data.token);
        console.log(response.data, "response.data")

        toast.success("Login successful!");
        localStorage.setItem("userId", response.data.userId);
        localStorage.setItem("emailId", response.data.email);
        // router.push("/labAdminCreate");
        if (response.data.isSuperAdmin) {
          router.push("/labAdminLogin");
        } else {
          router.push("/labAdminCreate");
        }

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


  return (
    <Home>
      <div className="min-h-screen bg-gradient-to-b from-white-50 to-cyan-200 flex flex-col items-center justify-center py-10 px-4">
        {/* Top Header/Slogan */}
        <h1 className="text-xl md:text-2xl font-semibold text-red-600 mb-8 text-center">
          Streamline <span className="text-gray-800">your laboratory services with ease and efficiency</span>
        </h1>

        {/* Main White Card (contains both image and login form) */}
<div className=" overflow-hidden flex flex-col md:flex-row max-w-4xl w-full relative border border-gray-300">
          {/* Left Section - Image */}
          <div className="md:w-1/2 p-6 flex items-center justify-center  relative ">
            <img
              src="/ba91fa3f487c3568d0707a1660ecaf8e94b71022.png"
              alt="Chemistry Lab Illustration"
              width={700} // Adjust based on your image's aspect ratio
              height={500} 
              className="max-h-96 md:max-h-full -mr-42 -mb-30"
            />
          </div>

          {/* Right Section - Your existing Login Card content */}
          <div className="md:w-1/2 p-8 md:p-12 flex flex-col justify-center bg-white shadow-xl "  style={{ borderTopLeftRadius: "20%" }}>
            <div className="flex justify-center mb-6">
              <img
                src="/104075137c369bb0056ee4f80f11111c548425f6.png"
                alt="Logo"
                width={70}
                height={70}
                className="rounded-full shadow-md p-2"
              />
            </div>
            <h2 className="text-center text-2xl font-bold text-gray-800 mb-6">
              {showOtp || showPasswordLogin ? "Welcome Back!" : "Welcome!"}
            </h2>

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
                    className={`w-full px-4 py-3 rounded-md text-gray-800 focus:outline-none bg-gray-100 border ${emailFormik.touched.email && emailFormik.errors.email
                        ? "border-red-500"
                        : "border-gray-300"
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
                  className={`w-full text-blue-900 font-medium py-3 rounded-md mb-2 transition duration-300
                    ${isSubmittingOTP
                      ? "bg-yellow-300 cursor-not-allowed"
                      : "bg-blue-600 hover:bg-blue-700 text-white" // Changed to match the original button color
                    }`}
                >
                  {isSubmittingOTP ? "Sending..." : "Get OTP"}
                </button>

                <div className="text-gray-600 text-sm my-2 text-center">or</div> {/* Changed text color to match original */}

                <button
                  type="button"
                  onClick={handlePasswordLogin}
                  className="w-full bg-blue-600 text-white font-medium py-3 rounded-md hover:bg-blue-700 transition duration-300" // Changed to match the original button color
                >
                  Login with Password
                </button>
              </form>
            )}

            {showOtp && (
              <form onSubmit={otpFormik.handleSubmit} className="w-full">
                <div className="w-full mb-4 bg-gray-100 px-4 py-3 rounded-md text-gray-800 border border-gray-300"> {/* Changed to rounded-md and added border */}
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
                    className={`w-full px-4 py-3 rounded-md text-gray-800 focus:outline-none bg-gray-100 border ${ // Changed to rounded-md
                      otpFormik.touched.otp && otpFormik.errors.otp
                        ? "border-red-500"
                        : "border-gray-300"
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
                <div className="text-gray-600 text-right mb-2"> {/* Changed text color */}
                  {formatTime(timer)}
                </div>

                <button
                  type="submit"
                  className="w-full bg-blue-600 text-white font-medium py-3 rounded-md mb-4 hover:bg-blue-700 transition duration-300" // Changed to rounded-md and button color
                >
                  SIGN IN
                </button>

                <div className="text-gray-600 text-sm my-2 text-center">Or</div> {/* Changed text color */}

                <button
                  type="button"
                  onClick={handlePasswordLogin}
                  className="w-full bg-blue-600 text-white font-medium py-3 rounded-md mb-4 hover:bg-blue-700 transition duration-300" // Changed to rounded-md and button color
                >
                  Login with Password
                </button>

                {/* Resend OTP link */}
                <div className="text-center">
                  <button
                    type="button"
                    onClick={handleResendOTP}
                    disabled={isResendDisabled}
                    className={`text-blue-600 underline ${ // Changed to blue-600 for consistency
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
                    className={`w-full px-4 py-3 rounded-md text-gray-800 focus:outline-none bg-gray-100 border ${ // Changed to rounded-md
                      passwordLoginFormik.touched.email &&
                        passwordLoginFormik.errors.email
                        ? "border-red-500"
                        : "border-gray-300"
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
                    className={`w-full px-4 py-3 rounded-md text-gray-800 focus:outline-none bg-gray-100 border ${ // Changed to rounded-md
                      passwordLoginFormik.touched.password &&
                        passwordLoginFormik.errors.password
                        ? "border-red-500"
                        : "border-gray-300"
                      }`}
                  />
                  <button
                    type="button"
                    onClick={togglePasswordVisibility}
                    className="absolute right-4 top-1/2 transform -translate-y-1/2 text-gray-500 hover:text-gray-700" // Adjusted positioning and added hover
                    aria-label={showPassword ? 'Hide password' : 'Show password'}
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
                      className="h-5 w-5" // Set a fixed size for the icon
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
                  <a href="#" className="text-blue-600 text-sm hover:text-blue-700"> {/* Changed color to blue-600 */}
                    Forgot Password?
                  </a>
                </div>

                <button
                  type="submit"
                  className="w-full bg-blue-600 text-white font-medium py-3 rounded-md mb-4 hover:bg-blue-700 transition duration-300" // Changed button color and rounded-md
                >
                  Login
                </button>

                <div className="text-gray-600 text-sm my-2 text-center">Or</div> {/* Changed text color */}

                {/* Login with OTP button */}
                <button
                  type="button"
                  onClick={async () => {
                    setShowOtp(true);
                    setShowPasswordLogin(false);
                    await emailFormik.submitForm();
                    setTimer(60);
                  }}
                  className="w-full bg-blue-600 text-white font-medium py-3 rounded-md mb-4 hover:bg-blue-700 transition duration-300" // Changed button color and rounded-md
                >
                  Login with OTP
                </button>
              </form>
            )}

            <div className="text-gray-600 text-center mt-4"> {/* Changed text color */}
              New User? Click{" "}
              <a href="#" className="text-blue-600 hover:text-blue-700" onClick={() => window.location.href = '/'}> {/* Changed color to blue-600 */}
                here
              </a>{" "}
              to Sign Up
            </div>
          </div>
        </div>
        <ToastContainer />
      </div>


    </Home>
  );
};

export default LoginPage;
