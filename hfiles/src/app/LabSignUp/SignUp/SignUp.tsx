"use client";
import React, { useState } from "react";
import { useFormik } from "formik";
import * as Yup from "yup";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faEye, faEyeSlash } from "@fortawesome/free-solid-svg-icons";
import { otpGenerate, signUp } from "@/services/labServiceApi";
import { toast, ToastContainer } from "react-toastify";
import { SignUpValidationsSchema } from "@/app/components/schemas/validations";
import { useRouter } from "next/navigation";

const SignUp = () => {
  const router = useRouter();
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [otpVisible, setOtpVisible] = useState(false);
  const [isOtpSending, setIsOtpSending] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);


  const formik = useFormik({
    initialValues: {
      labName: "",
      labEmail: "",
      phone: "",
      pincode: "",
      password: "",
      confirmPassword: "",
      acceptTerms: false,
      otp: "",
    },
    validationSchema: SignUpValidationsSchema,
    onSubmit: async (values) => {
      setIsSubmitting(true);
      const payload = {
        labName: values.labName,
        email: values.labEmail,
        phoneNumber: values.phone,
        pincode: values.pincode,
        password: values.password,
        confirmPassword: values.confirmPassword,
        otp: values.otp,
      };
      try {
        const response = await signUp(payload);
        console.log("Sign Up Response:", response);
        toast.success("Sign Up Successful");
        router.push("/thankYou");
      } catch (error) {
        console.error("Sign Up Error:", error);
        toast.error("There was an error signing up.");
      } finally {
        setIsSubmitting(false);
      }
      console.log("Submitted Data:", values);
    },
  });

  const handleGetOtp = async () => {
    const errors = await formik.validateForm();
    formik.setTouched({
      labName: true,
      labEmail: true,
      phone: true,
      pincode: true,
      password: true,
      confirmPassword: true,
    });

    if (Object.keys(errors).length === 0) {
      try {
        setIsOtpSending(true);
        await otpGenerate({
          labName: formik.values.labName,
          email: formik.values.labEmail,
        });
        setOtpVisible(true);
        toast.success("Otp Send Your Mail Successful");
      } catch (error) {
        console.error("OTP Error:", error);
        toast.error("Failed to send OTP. Please try again.");
      } finally {
        setIsOtpSending(false);
      }
    } else {
      console.error("Validation failed", errors);
    }
  };


  return (
    <div className="w-full bg-gradient-to-b from-white to-cyan-300 min-h-screen flex flex-col items-center justify-center">
      <div className="text-center">
        <h1 className="text-lg md:text-lg font-bold">
          <span className="text-red-500">Streamline</span> your laboratory
          services with ease and efficiency
        </h1>
        <div className="w-1/2 mx-auto my-3 border-b-2 border-black"></div>
      </div>

      <div className="flex flex-col md:flex-row border rounded-lg bg-white shadow-lg max-w-4xl w-full overflow-hidden">
        {/* Left Section - Image */}
        <div className="w-full md:w-1/2 flex items-center justify-center p-1 bg-gradient-to-b from-white to-blue-50 rounded-l-lg">
          <img
            src="\ba91fa3f487c3568d0707a1660ecaf8e94b71022.png"
            alt="Chemistry Lab Experiment"
            className="w-[500px] h-[400px] object-contain -mr-35 z-1"
          />
        </div>

        {/* Right Section - Form */}
        <div className="w-full md:w-1/2 p-8 flex items-center justify-center shadow-xl/30 rounded-tl-4xl">
          <div className="w-full max-w-md">
            <h1 className="text-center text-lg font-semibold mb-2">
              <span className="text-blue-600 font-bold">Sign Up</span> to
              Simplify Your Laboratory Today
            </h1>
            <div className="w-1/2 mx-auto my-3 border-b-2 border-black"></div>

            <form
              onSubmit={formik.handleSubmit}
              className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-6"
            >
              {/* Lab Name */}
              <div>
                <input
                  type="text"
                  name="labName"
                  placeholder="Enter Lab Name"
                  className="w-full border border-gray-300 rounded-md px-4 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  value={formik.values.labName}
                />
                {formik.touched.labName && formik.errors.labName && (
                  <p className="text-red-500 text-xs mt-1">
                    {formik.errors.labName}
                  </p>
                )}
              </div>

              {/* Email */}
              <div>
                <input
                  type="email"
                  name="labEmail"
                  placeholder="Enter Lab Email"
                  className="w-full border border-gray-300 rounded-md px-4 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  value={formik.values.labEmail}
                />
                {formik.touched.labEmail && formik.errors.labEmail && (
                  <p className="text-red-500 text-xs mt-1">
                    {formik.errors.labEmail}
                  </p>
                )}
              </div>

              {/* Phone */}
              <div>
                <input
                  type="text"
                  name="phone"
                  placeholder="Your Number"
                  className="w-full border border-gray-300 rounded-md px-4 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  value={formik.values.phone}
                />
                {formik.touched.phone && formik.errors.phone && (
                  <p className="text-red-500 text-xs mt-1">
                    {formik.errors.phone}
                  </p>
                )}
              </div>

              {/* Pincode */}
              <div>
                <input
                  type="text"
                  name="pincode"
                  placeholder="Enter Pin-Code"
                  className="w-full border border-gray-300 rounded-md px-4 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  value={formik.values.pincode}
                />
                {formik.touched.pincode && formik.errors.pincode && (
                  <p className="text-red-500 text-xs mt-1">
                    {formik.errors.pincode}
                  </p>
                )}
              </div>

              {/* Password */}
              <div className="relative">
                <input
                  type={showPassword ? "text" : "password"}
                  name="password"
                  placeholder="Password"
                  className="w-full border border-gray-300 rounded-md px-4 py-2 pr-10 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  value={formik.values.password}
                />
                <span
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500 cursor-pointer"
                >
                  <FontAwesomeIcon icon={showPassword ? faEye : faEyeSlash} />
                </span>
                {formik.touched.password && formik.errors.password && (
                  <p className="text-red-500 text-xs mt-1">
                    {formik.errors.password}
                  </p>
                )}
              </div>

              {/* Confirm Password */}
              <div className="relative">
                <input
                  type={showConfirm ? "text" : "password"}
                  name="confirmPassword"
                  placeholder="Confirm Password"
                  className="w-full border border-gray-300 rounded-md px-4 py-2 pr-10 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  value={formik.values.confirmPassword}
                />
                <span
                  onClick={() => setShowConfirm(!showConfirm)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500 cursor-pointer"
                >
                  <FontAwesomeIcon icon={showConfirm ? faEye : faEyeSlash} />
                </span>
                {formik.touched.confirmPassword &&
                  formik.errors.confirmPassword && (
                    <p className="text-red-500 text-xs mt-1">
                      {formik.errors.confirmPassword}
                    </p>
                  )}
              </div>

              {/* OTP Fields */}
              {otpVisible && (
                <div className="md:col-span-2 flex flex-col items-center mt-4">
                  <div className="flex gap-3">
                    {[0, 1, 2, 3, 4, 5].map((index) => (
                      <input
                        key={index}
                        id={`otp-${index}`}
                        type="text"
                        maxLength={1}
                        className="w-10 h-12 text-center border border-gray-400 rounded-md text-lg focus:outline-none focus:border-blue-500"
                        value={formik.values.otp[index] || ""}
                        onChange={(e) => {
                          const val = e.target.value;
                          if (/^\d?$/.test(val)) {
                            const newOtp =
                              formik.values.otp.substring(0, index) +
                              val +
                              formik.values.otp.substring(index + 1);
                            formik.setFieldValue("otp", newOtp);

                            if (val && index < 5) {
                              const nextInput = document.getElementById(
                                `otp-${index + 1}`
                              );
                              nextInput?.focus();
                            }
                          }
                        }}
                      />
                    ))}
                  </div>
                  <button
                    type="button"
                    onClick={async () => {
                      formik.setFieldValue("otp", "");
                      await handleGetOtp();
                    }}
                    className="text-blue-700 text-sm font-medium mt-2 self-end"
                  >
                    Resend OTP
                  </button>
                </div>
              )}

              {formik.touched.otp && formik.errors.otp && (
                <div className="md:col-span-2 text-center">
                  <p className="text-red-500 text-xs mt-1">
                    {formik.errors.otp}
                  </p>
                </div>
              )}

              {/* Terms & Conditions */}
              {otpVisible && (
                <div className="md:col-span-2 flex justify-center gap-2 mt-4">
                  <input
                    type="checkbox"
                    name="acceptTerms"
                    id="acceptTerms"
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                    checked={formik.values.acceptTerms}
                    className="w-4 h-4 accent-blue-600"
                  />
                  <label htmlFor="acceptTerms" className="text-sm text-gray-700">
                    I Accept The{" "}
                    <a
                      href="#"
                      className="text-blue-800 font-semibold hover:underline"
                    >
                      Terms & Conditions
                    </a>
                  </label>
                </div>
              )}
              {formik.touched.acceptTerms && formik.errors.acceptTerms && (
                <div className="md:col-span-2 text-center">
                  <p className="text-red-500 text-xs mt-1">
                    {formik.errors.acceptTerms}
                  </p>
                </div>
              )}

              {/* Button */}
              <div className="md:col-span-2 mt-6">
                {!otpVisible ? (
                  <button
                    type="button"
                    onClick={handleGetOtp}
                    disabled={isOtpSending}
                    className={`w-full font-bold py-3 rounded-md border-2 border-yellow-400 ${isOtpSending
                        ? "bg-gray-300 text-gray-700 cursor-not-allowed"
                        : "bg-yellow-300 hover:bg-yellow-400 text-black cursor-pointer"
                      } transition-colors duration-200`}
                  >
                    {isOtpSending ? "Sending OTP..." : "Get OTP"}
                  </button>
                ) : (
                  <button
                    type="submit"
                    disabled={isSubmitting}
                    className={`w-full font-bold py-3 rounded-md ${isSubmitting
                        ? "bg-gray-400 text-gray-700 cursor-not-allowed"
                        : "bg-blue-800 hover:bg-blue-900 text-white cursor-pointer"
                      } transition-colors duration-200`}
                  >
                    {isSubmitting ? "Signing Up..." : "Sign Up"}
                  </button>
                )}
              </div>
            </form>
            {!otpVisible && (
              <p className="text-center mt-6 text-sm text-gray-600">
                Already Have an Account?{" "}
                <a
                  href="/labLogin"
                  className="text-blue-800 font-semibold hover:underline"
                >
                  Log in
                </a>
              </p>
            )}
          </div>
        </div>
      </div>
      <ToastContainer />
    </div>
  );
};

export default SignUp;
