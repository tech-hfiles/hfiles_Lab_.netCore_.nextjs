import React, { useState, useRef, useEffect } from 'react';

const LabHeader = () => {
  const username = localStorage.getItem("username")
  const [dropdownOpen, setDropdownOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: any) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
        setDropdownOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleLogout = () => {
    window.location.href = '/labLogin';
    localStorage.removeItem("authToken");
    localStorage.removeItem("userId");
    localStorage.removeItem("emailId");
    localStorage.removeItem("username");
    localStorage.removeItem("LabAdminId");
  };

  return (
    <div>
      <header className="sticky top-0 z-50 text-white px-6 py-4 flex justify-between items-center" style={{ backgroundColor: '#0331B5' }}>
        <div className="text-2xl font-bold flex items-center">
          <img
            src="https://hfiles.in/wp-content/uploads/2022/11/hfiles.png"
            alt="hfiles logo"
            className="h-11 w-auto mr-2 cursor-pointer"
            style={{ backgroundColor: '#0331B5' }}
            onClick={() => (window.location.href = '/labHome')}
          />
        </div>
        <div className="relative flex items-center space-x-3" ref={dropdownRef}>
          <p className="text-white font-medium">{username}</p>
          <img
            src="/0e7f5f4a77770635e93d82998df96f869b6624bf.png"
            alt="Profile"
            className="h-10 w-10 rounded-full border-2 border-yellow-400 cursor-pointer"
            onClick={() => setDropdownOpen(!dropdownOpen)}
          />
          {dropdownOpen && (
            <div className="absolute right-0 mt-23 w-40 bg-white text-black rounded shadow-lg py-2 z-50">
              <button
                onClick={handleLogout}
                className="w-full text-left px-4 py-2 hover:bg-gray-100"
              >
                Logout
              </button>
            </div>
          )}
        </div>
      </header>
    </div>
  );
};

export default LabHeader;
