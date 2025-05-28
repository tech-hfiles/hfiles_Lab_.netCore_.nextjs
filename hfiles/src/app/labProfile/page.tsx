"use client";
import React, { useState } from "react";
import DefaultLayout from "../components/DefaultLayout";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import {
  faArrowRightArrowLeft,
  faBuilding,
  faEnvelope,
  faLocationDot,
  faPencil,
  faSearch,
  faTimes,
  faUserPlus,
} from "@fortawesome/free-solid-svg-icons";

import { useFormik } from "formik";
import * as Yup from "yup";
import { faPhone } from "@fortawesome/free-solid-svg-icons/faPhone";
import { useRouter } from "next/navigation";

const page = () => {
  const router = useRouter();
  const [isModalOpen, setIsModalOpen] = useState(false);

  // Formik & Yup schema
  const formik = useFormik({
    initialValues: {
      branchName: "North Star",
      labEmail: "",
      labName: "",
      pinCode: "",
    },
    validationSchema: Yup.object({
      branchName: Yup.string().required("Branch Name is required"),
      labEmail: Yup.string()
        .email("Invalid email address")
        .required("Lab Email is required"),
      labName: Yup.string().required("LAN Number is required"),
      pinCode: Yup.string()
        .matches(/^\d{6}$/, "Pin-code must be exactly 6 digits")
        .required("Pin-code is required"),
    }),
    onSubmit: (values) => {
      console.log("Branch Data:", values);
      setIsModalOpen(false);
      formik.resetForm();
    },
  });


  return (
    <DefaultLayout>
      <div className="p-2 sm:p-4">
        {/* Header */}
        <div className="mb-4">
          <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-2">
            <div className="text-xl font-bold text-black mx-3">Profile:</div>
            <div className="relative w-full sm:w-auto mx-3">
              <input
                type="text"
                placeholder="Search"
                className="pl-2 pr-10 py-1 border rounded-full focus:outline-none focus:ring-2 focus:ring-blue-500 w-full"
              />
              <FontAwesomeIcon
                icon={faSearch}
                className="absolute right-0 top-0 text-white bg-black p-2 rounded-full hover:bg-gray-800 cursor-pointer"
              />
            </div>
          </div>
          <div className="border-b mx-3"></div>
        </div>

        {/* Profile Card */}
        <div className="w-full lg:max-w-2xl  p-2 sm:p-4">
          <div className="mb-2 px-2 text-blue-800 font-semibold text-sm sm:text-base">
            Account:{" "}
            <span className="text-gray-800">Ahemdabad - 380052, Gujarat</span>
          </div>
          <div className="bg-white rounded-3xl  shadow-md flex flex-col sm:flex-row border mb-2">
            <div className="relative mb-3 mt-3 mx-3 flex justify-center">
              <img
                src="/250bd3d11edb6cfc8add2600b5bb25c75b95d560.jpg"
                alt="Goku"
                className="w-32 h-32 sm:w-[224px] sm:h-[180px] rounded-full object-cover"
              />
              <div className="absolute bottom-2 right-4 p-2 bg-blue-900 w-[30px] h-[30px] rounded-full cursor-pointer">
                <FontAwesomeIcon
                  icon={faPencil}
                  size="sm"
                  className="text-white mb-1"
                />
              </div>
            </div>

            <div className="ml-6 mb-5 flex flex-col justify-between">
              <div className="text-sm bg-gray-800 text-white px-2 py-1 rounded-full w-fit sm:ml-[220px] mb-2">
                HF_id: HF120624RAN1097
              </div>
              <div className="text-sm sm:text-base">
                <h2 className="text-lg sm:text-xl font-bold text-blue-800">
                  NorthStar
                </h2>
                <p>
                  <span className="font-semibold">E-mail:</span>{" "}
                  Northstarofficial@gmail.com
                </p>
                <p>
                  <span className="font-semibold">Phone:</span> 123456789012
                </p>
                <p className="break-words">
                  <span className="font-semibold">Address:</span> 5-A, Ravi Push
                  Apartment, Ahmedabad - 380052, Gujarat
                </p>
              </div>
            </div>
          </div>
        </div>

          <div className="w-full sm:w-auto flex justify-end mb-3">
            <button
              className="bg-yellow-300 hover:bg-yellow-400 px-4 py-2 rounded flex items-center gap-2 border w-full sm:w-auto justify-center"
              onClick={() => router.push("/labAllMember")}
                >
              <span>Manage All Members</span>
            </button>
          </div>
        <div className="border-b mb-4"></div>

        {/* All Branches Header */}
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center px-2 sm:px-4 py-2 gap-2">
          <div>
            <p className="text-blue-900 font-bold text-base sm:text-lg">
              All Branches:
            </p>
          </div>
          <div className="w-full sm:w-auto">
            <button
              className="bg-yellow-300 hover:bg-yellow-400 px-4 py-2 rounded flex items-center gap-2 border w-full sm:w-auto justify-center"
              onClick={() => setIsModalOpen(true)}
            >
              <FontAwesomeIcon icon={faUserPlus} size="sm" />
              <span>Add Branch</span>
              
            </button>
          </div>
        </div>

        {/* Branch Card */}
        <div className="flex flex-col md:flex-row justify-between items-start md:items-center px-2 md:px-4 py-2 gap-4">
          <div className="w-full md:max-w-2xl lg:max-w-2xl p-2 md:p-4">
            <div className="bg-white rounded-3xl shadow-md flex flex-col md:flex-row border mb-2">
              <div className="border border-gray-300 rounded w-32 h-32 md:w-32 md:h-32 flex flex-col items-center gap-4 mb-5 relative mx-auto md:mx-3 mt-3">
                <img
                  src="/c320115f6850bb4e112784af2aaf059259d7bfe9.jpg"
                  alt="Goku"
                  className="w-full h-full rounded-full object-cover"
                />
              </div>

              <div className="ml-0 md:ml-6 mb-5 flex flex-col justify-between">
                <div className="text-sm bg-yellow-300 text-black px-2 py-2 rounded-full w-fit md:ml-[160px] lg:ml-[282px] mb-2">
                  HF_id: HF120624RAN1097
                </div>
                <div className="text-sm md:text-base px-2 md:px-0">
                  <p>
                    <span className="font-semibold">Name:</span> Ankit hfiles
                  </p>
                  <p>
                    <span className="font-semibold">E-mail:</span>{" "}
                    ankithfiles@gmail.com
                  </p>
                  <p className="break-words">
                    <span className="font-semibold">Address:</span> Marine Lines,
                    Mumbai - 400002, Maharashtra
                  </p>
                </div>
              </div>
            </div>
          </div>
          <div className="w-full md:w-auto px-2 md:px-0">
            <button className="bg-gradient-to-r from-white to-blue-300 px-4 py-2 rounded flex items-center gap-2 border w-full md:w-auto justify-center">
              <FontAwesomeIcon icon={faArrowRightArrowLeft} size="sm" />
              <span>Switch Branch</span>
            </button>
          </div>
        </div>

        {/* Modal */}
        {isModalOpen && (
          <div className="fixed inset-0 bg-black bg-opacity-10 flex justify-center items-center z-50 p-4"
            onClick={() => setIsModalOpen(false)}>

            <div
              className="bg-white rounded-2xl p-4 sm:p-6 w-2/3 max-w-lg sm:max-w-4xl relative shadow-2xl overflow-y-auto max-h-[85vh]"
              onClick={(e) => e.stopPropagation()}
            >
              {/* Close Button */}
              <button
                onClick={() => setIsModalOpen(false)}
                className="absolute top-4 right-4 text-gray-400 hover:text-gray-600 transition-colors"
              >
                <FontAwesomeIcon icon={faTimes} size="lg" />
              </button>

              {/* Header */}
              <div className="text-center mb-6 sm:mb-8">
                <div className="flex justify-center items-center gap-2 sm:gap-3 mb-3 sm:mb-4">
                  <FontAwesomeIcon icon={faLocationDot} />
                  <h2 className="text-xl sm:text-2xl font-bold text-blue-600">Add New Branch</h2>
                </div>
                <div className="w-16 sm:w-24 h-1 bg-blue-600 mx-auto rounded"></div>
              </div>

              {/* Content Section */}
              <div className="flex flex-col lg:flex-row gap-4 sm:gap-8 items-start">
                {/* Form Section */}
                <div className="flex-1 border-b lg:border-b-0 lg:border-r border-gray-200 pb-4 lg:pb-0 lg:pr-8">
                  <form onSubmit={formik.handleSubmit} className="space-y-5 sm:space-y-6" noValidate>
                    {/* Branch Name */}
                    <div className="flex items-start gap-3">
                      <FontAwesomeIcon icon={faBuilding} className="text-gray-600 mt-3" />
                      <div className="flex-1">
                        <input
                          type="text"
                          id="branchName"
                          name="branchName"
                          value={formik.values.branchName}
                          onChange={formik.handleChange}
                          onBlur={formik.handleBlur}
                          placeholder="Enter Branch Name"
                          className={`w-full border rounded-lg px-4 py-3 focus:outline-none focus:ring-2 ${formik.touched.branchName && formik.errors.branchName
                            ? "focus:ring-red-500 border-red-500"
                            : "focus:ring-blue-500 border-gray-300"
                            }`}
                          required
                        />
                        {formik.touched.branchName && formik.errors.branchName && (
                          <p className="text-red-500 text-sm mt-1">{formik.errors.branchName}</p>
                        )}
                      </div>
                    </div>

                    {/* Lab Email */}
                    <div className="flex items-start gap-3">
                      <FontAwesomeIcon icon={faEnvelope} className="text-gray-600 mt-3" />
                      <div className="flex-1">
                        <input
                          type="email"
                          id="labEmail"
                          name="labEmail"
                          value={formik.values.labEmail}
                          onChange={formik.handleChange}
                          onBlur={formik.handleBlur}
                          placeholder="Enter Lab Email"
                          className={`w-full border rounded-lg px-4 py-3 focus:outline-none focus:ring-2 ${formik.touched.labEmail && formik.errors.labEmail
                            ? "focus:ring-red-500 border-red-500"
                            : "focus:ring-blue-500 border-gray-300"
                            }`}
                          required
                        />
                        {formik.touched.labEmail && formik.errors.labEmail && (
                          <p className="text-red-500 text-sm mt-1">{formik.errors.labEmail}</p>
                        )}
                      </div>
                    </div>

                    {/* Lab Number */}
                    <div className="flex items-start gap-3">
                      <FontAwesomeIcon icon={faPhone} className="text-gray-600 mt-3" />
                      <div className="flex-1">
                        <input
                          type="text"
                          id="labName"
                          name="labName"
                          value={formik.values.labName}
                          onChange={formik.handleChange}
                          onBlur={formik.handleBlur}
                          placeholder="Lab Number"
                          className={`w-full border rounded-lg px-4 py-3 focus:outline-none focus:ring-2 ${formik.touched.labName && formik.errors.labName
                            ? "focus:ring-red-500 border-red-500"
                            : "focus:ring-blue-500 border-gray-300"
                            }`}
                          required
                        />
                        {formik.touched.labName && formik.errors.labName && (
                          <p className="text-red-500 text-sm mt-1">{formik.errors.labName}</p>
                        )}
                      </div>
                    </div>

                    {/* Pin Code */}
                    <div className="flex items-start gap-3">
                      <FontAwesomeIcon icon={faLocationDot} className="text-gray-600 mt-3" />
                      <div className="flex-1">
                        <input
                          type="text"
                          id="pinCode"
                          name="pinCode"
                          value={formik.values.pinCode}
                          onChange={formik.handleChange}
                          onBlur={formik.handleBlur}
                          placeholder="Enter Pin-Code"
                          className={`w-full border rounded-lg px-4 py-3 focus:outline-none focus:ring-2 ${formik.touched.pinCode && formik.errors.pinCode
                            ? "focus:ring-red-500 border-red-500"
                            : "focus:ring-blue-500 border-gray-300"
                            }`}
                          required
                        />
                        {formik.touched.pinCode && formik.errors.pinCode && (
                          <p className="text-red-500 text-sm mt-1">{formik.errors.pinCode}</p>
                        )}
                      </div>
                    </div>

                    {/* Location Text */}
                    <div className="text-center text-blue-600 text-sm mt-4">
                      Gurukul-380052, Ahmedabad, Gujarat
                    </div>

                    {/* Save Button */}
                    <div className="text-center pt-2">
                      <button
                        type="submit"
                        className="w-full sm:w-40 bg-blue-600 text-white font-semibold py-2 rounded-xl hover:bg-blue-700 transition-colors text-lg"
                      >
                        Save
                      </button>
                    </div>
                  </form>
                </div>

                {/* Image Section */}
                <div className="flex-shrink-0 flex justify-center w-full lg:w-auto">
                  <img
                    src="/f02ab4b2b4aeffe41f18ff4ece3c64bd20e9a0f4.png"
                    alt="Branch Illustration"
                    className="w-full max-w-xs sm:max-w-sm object-cover rounded-lg"
                  />
                </div>
              </div>
            </div>
          </div>

        )}
      </div>
    </DefaultLayout>
  );
};

export default page;
