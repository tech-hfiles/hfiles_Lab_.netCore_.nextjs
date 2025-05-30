"use client";
import React, { useEffect, useRef, useState } from "react";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import {
  faChevronDown,
  faChevronLeft,
  faChevronRight,
  faSearch,
} from "@fortawesome/free-solid-svg-icons";
import DefaultLayout from "../components/DefaultLayout";
import DatePicker from "react-datepicker";
import CustomDatePicker from "../components/Datepicker/CustomDatePicker";
import { useRouter } from "next/navigation";
import { ListUser } from "@/services/labServiceApi";
import { number } from "yup";


type Patient = {
  hfid: string;
  name: string;
  reportType: string;
  date: string;
  viewType: string;
  highlighted?: boolean;
  userId:string;
};

const page = () => {

  const [calendarOpen, setCalendarOpen] = useState(false);
  const [selectedDate, setSelectedDate] = useState(null);
  const [searchQuery, setSearchQuery] = useState("");
  const dateRef = useRef(null);
  const router = useRouter();
  const [patientData, setPatientData] = useState<Patient[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 20;
  const [formattedStart, setFormattedStart] = useState("");
  const [formattedEnd, setFormattedEnd] = useState("");
    const userId = localStorage.getItem("userId");


  const userList = async () => {
    const response = await ListUser(userId,formattedStart, formattedEnd);
    setPatientData(response.data)

  }
  const filteredData = patientData.filter((patient) =>
    patient.hfid.toLowerCase().includes(searchQuery.toLowerCase()) ||
    patient.name.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const paginatedData = filteredData.slice(
    (currentPage - 1) * pageSize,
    currentPage * pageSize
  );

  const totalPages = Math.ceil(filteredData.length / pageSize);


 useEffect(() => {
  userList();
}, [userId,formattedStart, formattedEnd]);


  const handleDateRangeSelect = (start: Date, end: Date) => {
  const startStr = start.toLocaleDateString("en-GB");
  const endStr = end.toLocaleDateString("en-GB");
  setFormattedStart(startStr);
  setFormattedEnd(endStr);
  console.log(`startDate=${startStr}&endDate=${endStr}`);
};



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
                placeholder="Search by ID or Name"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
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
                     <FontAwesomeIcon icon={faChevronDown} className="ml-2" />
                  </div>
                  <div className="absolute z-10 mt-2 bg-white border rounded shadow">
                    {calendarOpen && (
                      <CustomDatePicker onDateRangeSelect={handleDateRangeSelect} />
                    )}
                  </div>
                </th>

                <th className="p-3 font-semibold text-black text-left">View</th>
              </tr>
            </thead>
            <tbody>
              {paginatedData.map((patient, index) => (
                <tr
                  key={index}
                  className={`border-t transition-colors duration-200 cursor-pointer ${patient.highlighted
                    ? " hover:bg-blue-200"
                    : "bg-white hover:bg-gray-100"
                    }`}
                >
                  <td className="p-3 ">{patient.hfid}</td>
                  <td className="p-3 ">{patient.name}</td>
                  <td className="p-3  text-blue-700 font-medium">
                    {patient.reportType}
                  </td>
                  <td className="p-3 ">{patient.date}</td>
                  <td className="p-3">
                    <button
                      // onClick={() => router.push("/shareReport")}
                      onClick={() => router.push(`/shareReport?userId=${patient.userId}`)}
                      className={`px-3 py-1 rounded font-semibold text-black border ${patient.viewType === "green"
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
            className={`cursor-pointer text-blue-700 hover:text-blue-900 ${currentPage === 1 ? 'opacity-50 cursor-not-allowed' : ''}`}
            onClick={() => currentPage > 1 && setCurrentPage(currentPage - 1)}
          />
          {[...Array(totalPages)].map((_, index) => (
            <span
              key={index}
              className={`px-2 py-1 rounded cursor-pointer ${currentPage === index + 1 ? "text-white bg-blue-500" : "hover:bg-blue-200"}`}
              onClick={() => setCurrentPage(index + 1)}
            >
              {index + 1}
            </span>
          ))}
          <FontAwesomeIcon
            icon={faChevronRight}
            className={`cursor-pointer text-blue-700 hover:text-blue-900 ${currentPage === totalPages ? 'opacity-50 cursor-not-allowed' : ''}`}
            onClick={() => currentPage < totalPages && setCurrentPage(currentPage + 1)}
          />
        </div>

      </div>
    </DefaultLayout>
  );
};

export default page;
