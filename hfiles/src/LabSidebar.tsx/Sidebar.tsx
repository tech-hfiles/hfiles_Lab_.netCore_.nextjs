"use client";
import { SidebarData } from "@/services/labServiceApi";
import {
  faArrowLeft,
  faArrowRight,
  faFileAlt,
  faHome,
  faUser,
} from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import Link from "next/link";
import { usePathname } from "next/navigation";
import React, { useState, useEffect } from "react";


type LabInfo = {
  hfid: number;
  labName: string;
};

const Sidebar = ({ className = "" }) => {
  const [collapsed, setCollapsed] = useState(false);
  const [isMobile, setIsMobile] = useState(false);
  const pathname = usePathname();
  const emailId =localStorage.getItem('emailId');
  const [labName, setLabName] = useState<LabInfo[]>([]) as any;

  // Handle responsive behavior
  useEffect(() => {
    const handleResize = () => {
      setIsMobile(window.innerWidth < 768);
      if (window.innerWidth < 768) {
        setCollapsed(true);
      }
    };

    // Set initial state
    handleResize();

    // Add event listener
    window.addEventListener("resize", handleResize);

    // Clean up
    return () => window.removeEventListener("resize", handleResize);
  }, []);

  const toggleSidebar = () => {
    setCollapsed(!collapsed);
  };

  // Check if a link is active
  const isActive = (path: any) => {
    return pathname === path;
  };

  const ListData = async () =>{
 const res = await SidebarData(String(emailId));
 setLabName(res.data)
 }

 useEffect(() =>{
ListData();
},[])

  return (
    <div
      className={`flex flex-col bg-gradient-to-b from-blue-200 to-blue-100 ${
        collapsed ? "w-16" : "w-64"
      } transition-all duration-300 ease-in-out overflow-y-auto h-screen ${className} shadow-md`}
    >
      {/* Toggle Button */}
      <button
        onClick={toggleSidebar}
        className=" hover:bg-blue-300 rounded-lg m-2 transition-colors"
        aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}
      >
        {collapsed ? (
          <FontAwesomeIcon icon={faArrowRight} size="lg" />
        ) : (
          <FontAwesomeIcon icon={faArrowLeft} size="lg" />
        )}
      </button>

      {/* Logo */}
      <div className="flex justify-center py-3">
        {collapsed ? (
          <div className="w-10 h-10 bg-white rounded-full flex items-center justify-center shadow-md">
            {/* <span className="text-blue-600 font-bold text-xs">NS</span> */}
          </div>
        ) : (
          <div className="w-40 flex flex-col items-center">
            <div className="w-16 h-16 bg-white rounded-full border flex items-center justify-center mb-2 overflow-hidden shadow-md">
              <img
                src="/c320115f6850bb4e112784af2aaf059259d7bfe9.jpg"
                alt="NorthStar Logo"
                className="w-full h-full object-cover"
              />
            </div>
            <span className="text-blue-600 font-bold text-lg">{labName.labName}</span>
          </div>
        )}
      </div>

      {/* User ID */}
      <div
        className={`px-4 py-2 text-center ${
          collapsed ? "text-xs" : "text-sm"
        } bg-gradient-to-r from-blue-300 to-white`}
      >
        <span className="text-black">
          {collapsed ? "ID" : `HF_Id: ${labName.hfid}`}
        </span>
      </div>
 
      {/* Main content with navigation and bottom image */}
      <div className="flex flex-col flex-grow">
        {/* Navigation Links */}
        <nav className="mt-2 px-2 md:px-6">
          <ul className="space-y-2">
            <li>
              <Link
                href="/labHome"
                className={`flex items-center px-4 py-2 rounded-lg transition-colors ${
                  isActive("/labHome")
                    ? "text-blue-600"
                    : "text-black hover:bg-blue-200"
                }`}
              >
                <FontAwesomeIcon icon={faHome} className="w-5 h-5" />
                {!collapsed && <span className="ml-4">Home</span>}
              </Link>
            </li>

            <li>
              <Link
                href="/labProfile"
                className={`flex items-center px-4 py-2 rounded-lg transition-colors ${
                  isActive("/labProfile")
                    ? "text-blue-600"
                    : "text-black hover:bg-blue-200"
                }`}
              >
                <FontAwesomeIcon icon={faUser} className="w-5 h-5" />
                {!collapsed && <span className="ml-4">Profile</span>}
              </Link>
            </li>
            <li>
              <Link
                href="/labForms"
                className={`flex items-center px-4 py-2 rounded-lg transition-colors ${
                  isActive("/labForms")
                    ? "text-blue-600"
                    : "text-black hover:bg-blue-200"
                }`}
              >
                <FontAwesomeIcon icon={faFileAlt} className="w-5 h-5" />
                {!collapsed && <span className="ml-4">Forms</span>}
              </Link>
            </li>
          </ul>
        </nav>

        {(!isMobile || !collapsed) && (
          <div className="mt-auto mb-26 p-4">
            <img
              src="/f02ab4b2b4aeffe41f18ff4ece3c64bd20e9a0f4.png"
              alt="Sidebar Logo"
              className="w-full object-cover rounded-lg shadow-md"
            />
          </div>
        )}
      </div>
    </div>
  );
};

export default Sidebar;
