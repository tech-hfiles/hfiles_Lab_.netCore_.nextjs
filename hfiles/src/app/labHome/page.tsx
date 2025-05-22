"use client";
import React, { useRef, useState } from "react";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import {
  faChevronLeft,
  faChevronRight,
  faSearch,
} from "@fortawesome/free-solid-svg-icons";
import DefaultLayout from "../components/DefaultLayout";
import DatePicker from "react-datepicker";
import CustomDatePicker from "../components/Datepicker/CustomDatePicker";
import { useRouter } from "next/navigation";

const page = () => {
  const [calendarOpen, setCalendarOpen] = useState(false);
  const [selectedDate, setSelectedDate] = useState(null);
  const dateRef = useRef(null);
  const router = useRouter();
  const patientData = [
    {
      hf_id: "HF120624RAN1097",
      name: "Aarav Maheta",
      reportType: "Radiology",
      date: "March 5, 2023",
      highlighted: false,
      viewType: "default",
    },
    {
      hf_id: "HF120624RAN1097",
      name: "Tejas Chauhan",
      reportType: "Dental Report",
      date: "March 4, 2025",
      highlighted: true,
      viewType: "green",
    },
    {
      hf_id: "HF120624RAN1097",
      name: "Tejas Chauhan",
      reportType: "Radiology",
      date: "March 5, 2024",
      highlighted: false,
      viewType: "default",
    },
    {
      hf_id: "HF120624RAN1097",
      name: "Palak hfiles",
      reportType: "Dental Report",
      date: "March 5, 2024",
      highlighted: false,
      viewType: "default",
    },
    {
      hf_id: "HF120624RAN1097",
      name: "Palak hfiles",
      reportType: "Radiology",
      date: "March 5, 2024",
      highlighted: false,
      viewType: "default",
    },
    {
      hf_id: "HF120624RAN1097",
      name: "Palak hfiles",
      reportType: "Dental Report",
      date: "March 5, 2024",
      highlighted: false,
      viewType: "default",
    },
    {
      hf_id: "HF120624RAN1097",
      name: "Aarav Maheta",
      reportType: "Radiology",
      date: "March 5, 2023",
      highlighted: false,
      viewType: "default",
    },
  ];

  return (
    <DefaultLayout>
      <div className="p-4">
        {/* Header */}
        <div className="mb-4">
          <div className="flex justify-between items-center">
            <div className="text-xl font-bold text-black mx-3">
              Your Patients
            </div>
            <div className="relative">
              <input
                type="text"
                placeholder="Search"
                className="pl-2 pr-10 py-1 border rounded-full focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              <FontAwesomeIcon
                icon={faSearch}
                className="absolute right-0 top-0 text-white bg-black p-2 rounded-full hover:bg-gray-800 cursor-pointer"
              />
            </div>
          </div>
          <div className="border-b mx-3"></div>
        </div>

        {/* Table */}
        <div className="overflow-x-auto rounded-2xl border border-black">
          <p className="mb-3 text-gray-700 text-sm mx-7 mt-2 ">
            All the individuals you've supported by sending timely and accurate
            health reports are listed here.
          </p>
          <div className="border-b border-gray-400 mx-2"></div>
          <table className="min-w-full text-sm ">
            <thead>
              <tr className="bg-white ">
                <th className="p-3 font-semibold text-black text-left  ">
                  HF_id
                </th>
                <th className="p-3 font-semibold text-black text-left ">
                  Name
                </th>
                <th className="p-3 font-semibold text-black text-left ">
                  Report Type
                </th>
                <th className="p-3 font-semibold text-black relative">
                  <div
                    className="flex items-center cursor-pointer"
                    onClick={() => setCalendarOpen(!calendarOpen)}
                    ref={dateRef}
                  >
                    Date
                    <span className="ml-2">▼</span>
                  </div>
                  <div className="absolute z-10 mt-2 bg-white border rounded shadow">
                    {calendarOpen && <CustomDatePicker />}
                  </div>
                </th>

                <th className="p-3 font-semibold text-black text-left">View</th>
              </tr>
            </thead>
            <tbody>
              {patientData.map((patient, index) => (
                <tr
                  key={index}
                  className={`border-t transition-colors duration-200 cursor-pointer ${
                    patient.highlighted
                      ? " hover:bg-blue-200"
                      : "bg-white hover:bg-gray-100"
                  }`}
                >
                  <td className="p-3 ">{patient.hf_id}</td>
                  <td className="p-3 ">{patient.name}</td>
                  <td className="p-3  text-blue-700 font-medium">
                    {patient.reportType}
                  </td>
                  <td className="p-3 ">{patient.date}</td>
                  <td className="p-3">
                    <button
                      onClick={() => router.push("/shareReport")}
                      className={`px-3 py-1 rounded font-semibold text-black border ${
                        patient.viewType === "green"
                          ? "bg-green-400"
                          : "bg-blue-300"
                      }`}
                    >
                      See more
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className="flex justify-end mx-4 items-center space-x-3 mt-3">
          <FontAwesomeIcon
            icon={faChevronLeft}
            className="cursor-pointer text-blue-700 hover:text-blue-900"
          />
          <span className="px-2 py-1 rounded text-white bg-blue-500">1</span>
          <span className="px-2 py-1 rounded hover:bg-blue-200 cursor-pointer">
            2
          </span>
          <span className="px-2 py-1 rounded hover:bg-blue-200 cursor-pointer">
            3
          </span>
          <FontAwesomeIcon
            icon={faChevronRight}
            className="cursor-pointer text-blue-700 hover:text-blue-900"
          />
        </div>
      </div>
    </DefaultLayout>
  );
};

export default page;
