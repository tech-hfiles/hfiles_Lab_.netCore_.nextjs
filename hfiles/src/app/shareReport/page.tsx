"use client";
import React, { useState } from "react";
import DefaultLayout from "../components/DefaultLayout";

const SharedReportsPage = () => {
  const [userData] = useState({
    name: "Ankit HFiles",
    email: "ankithfiles@gmail.com",
    id: "HF010125ANK1312",
  });

  const [isResendMode, setIsResendMode] = useState(false);

  const reports = [
    {
      id: 1,
      type: "Dental Report",
      date: "March 5, 2024",
      fromMumbai: false,
    },
    {
      id: 2,
      type: "Immunization",
      date: "April 2, 2025",
      fromMumbai: true,
    },
  ];

  return (
    <DefaultLayout>
      <div className=" mx-auto p-4">
        {/* Page Title */}
        <div className="text-center mb-4">
          <h1 className="text-2xl font-bold">Shared Reports</h1>
          <div className="border-b border-gray-300 w-36 mx-auto mt-1"></div>
        </div>

        {/* User Profile Card */}
        <div className="bg-blue-100 rounded-lg flex flex-col sm:flex-row sm:items-center max-w-lg border">
          <div className="mx-auto sm:mx-0 sm:mr-4">
            <div className="w-16 h-16 bg-gray-200 rounded-full overflow-hidden mx-3">
              <img
                src="/3d77b13a07b3de61003c22d15543e99c9e08b69b.jpg"
                alt="Profile"
                className="w-full h-full object-cover"
              />
            </div>
          </div>
          <div className="flex-1 text-center sm:text-left">
            <h2 className="text-blue-800 text-xl font-bold">{userData.name}</h2>
            <p className="text-black">
              <span className="font-bold">Email:</span>{" "}
              <span>{userData.email}</span>
            </p>
          </div>
          <div className="bg-white p-2 rounded-lg mb-3 sm:mb-26 mt-3 sm:mt-0">
            <p className="text-sm">{userData.id}</p>
          </div>
        </div>

        <div className="">
          {/* Report Info */}
          {reports.map((report, index) => (
            <div key={report.id}>
              <div className="flex justify-end ">
                <p>{report.date}</p>
              </div>
              <div className="border mb-3 "></div>

              {/* Mumbai Notice */}
              {report.fromMumbai && index === 1 && (
                <div className="flex justify-center items-center mb-2 text-green-600">
                  <div className="w-5 h-5 rounded-full bg-green-500 flex items-center justify-center mr-1">
                    <span className="text-white text-sm">✓</span>
                  </div>
                  <p className="text-sm">
                    This report was sent by the Mumbai branch.
                  </p>
                </div>
              )}
              <div className="border border-gray-300 rounded w-32 h-32 flex flex-col items-center gap-4 mb-5 relative">
                {/* Step 4: Conditional checkbox */}
                {isResendMode && (
                  <input
                    type="checkbox"
                    className="absolute bottom-2 right-2  w-4 h-4"
                  />
                )}
                <img
                  src="/c320115f6850bb4e112784af2aaf059259d7bfe9.jpg"
                  alt="My Image"
                  className="max-w-full max-h-full object-contain"
                />
                <p className="">{report.type}</p>
              </div>
            </div>
          ))}
        </div>
        <div className="border mt-10"></div>

        {/* Resend Button */}
        <div className="flex justify-end mt-6">
          <button
            onClick={() => setIsResendMode(!isResendMode)}
            className={`${isResendMode
                ? "bg-blue-600 text-white hover:bg-blue-500"
                : "bg-yellow-300 hover:bg-yellow-400"
              } text-gray-800 font-semibold px-6 py-2 rounded-sm transition-colors`}
          >
            Resend
          </button>
        </div>
      </div>
    </DefaultLayout>
  );
};

export default SharedReportsPage;
