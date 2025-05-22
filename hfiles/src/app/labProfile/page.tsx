"use client";
import React from "react";
import DefaultLayout from "../components/DefaultLayout";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faArrowRightArrowLeft, faPencil, faPlusCircle, faSearch, faUser, faUserPlus } from "@fortawesome/free-solid-svg-icons";

const page = () => {
  return (
    <DefaultLayout>
      <div className="p-2 sm:p-4">
        {/* Header */}
        <div className="mb-4">
          <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-2">
            <div className="text-xl font-bold text-black mx-3">
              Profile:
            </div>
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

        <div className="w-full lg:max-w-2xl  p-2 sm:p-4">
          {/* Top Label */}
          <div className="mb-2 px-2 text-blue-800 font-semibold text-sm sm:text-base">
            Account: <span className="text-gray-800">Ahemdabad - 380052, Gujarat</span>
          </div>
          <div className="bg-white rounded-3xl  shadow-md flex flex-col sm:flex-row border mb-2">
            {/* Left - Avatar */}
            <div className="relative mb-3 mt-3 mx-3 flex justify-center">
              <img
                src="/250bd3d11edb6cfc8add2600b5bb25c75b95d560.jpg"
                alt="Goku"
                className="w-32 h-32 sm:w-[224px] sm:h-[180px] rounded-full object-cover"
              />
              <div className="absolute bottom-2 right-4 p-2 bg-blue-900 w-[30px] h-[30px] rounded-full cursor-pointer">
                <FontAwesomeIcon icon={faPencil} size="sm" className="text-white mb-1" />
              </div>
            </div>

            {/* Right - Info */}
            <div className="ml-6 mb-5 flex flex-col justify-between">
              <div className="text-sm bg-gray-800 text-white px-2 py-1 rounded-full w-fit sm:ml-[220px] mb-2">
                HF_id: HF120624RAN1097
              </div>
              <div className="text-sm sm:text-base">
                <h2 className="text-lg sm:text-xl font-bold text-blue-800">NorthStar</h2>
                <p>
                  <span className="font-semibold">E-mail:</span> Northstarofficial@gmail.com
                </p>
                <p>
                  <span className="font-semibold">Phone:</span> 123456789012
                </p>
                <p className="break-words">
                  <span className="font-semibold">Address:</span> 5-A, Ravi Push Apartment, Ahmedabad - 380052, Gujarat
                </p>
              </div>
            </div>
          </div>
        </div>

        <div className="border-b mb-4"></div>

        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center px-2 sm:px-4 py-2 gap-2">
          <div>
            <p className="text-blue-900 font-bold text-base sm:text-lg">All Branches:</p>
          </div>
          <div className="w-full sm:w-auto">
            <button className="bg-yellow-300 hover:bg-yellow-400 px-4 py-2 rounded flex items-center gap-2 border w-full sm:w-auto justify-center">
              <FontAwesomeIcon icon={faUserPlus} size="sm" />
              <span>Add Branch</span>
              
            </button>
          </div>
        </div>

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
                    <span className="font-semibold">E-mail:</span> ankithfiles@gmail.com
                  </p>
                  <p className="break-words">
                    <span className="font-semibold">Address:</span> Marine Lines, Mumbai - 400002, Maharashtra
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
        </div>      </div>
    </DefaultLayout>
  );
};

export default page;