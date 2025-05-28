import React from "react";

const Header = () => {
  return (
    <header className="sticky top-0 z-50 text-white px-4 py-3 bg-[#0331B5]">
      <div className="flex justify-between mx-2 items-center">
        <div className="flex items-center text-xl font-bold">
          <img
            src="https://hfiles.in/wp-content/uploads/2022/11/hfiles.png"
            alt="HFiles Logo"
            className="h-10 w-auto mr-2"
          />
        </div>
        <button className="bg-yellow-400 text-blue-700 text-sm sm:text-base font-semibold px-4 py-2 rounded hover:bg-yellow-300 transition">
          Sign In
        </button>
      </div>
    </header>
  );
};

export default Header;
