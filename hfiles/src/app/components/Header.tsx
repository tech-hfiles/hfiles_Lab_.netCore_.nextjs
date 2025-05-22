import React from 'react'

const Header = () => {
  return (
    <div>
      <header className="sticky top-0 z-50  text-white px-6 py-4 flex justify-between items-center"  style={{ backgroundColor: '#0331B5' }}>
        <div className="text-2xl font-bold flex items-center">
          <img src="https://hfiles.in/wp-content/uploads/2022/11/hfiles.png" alt="hfiles logo"  className="h-11 w-auto mr-2 "style={{ backgroundColor: '#0331B5' }} />
        </div>
        <button className="bg-yellow-400 text-blue-700 font-semibold px-4 py-2 rounded hover:bg-yellow-300 transition">
          Sign In
        </button>
      </header>
    </div>
  )
};
export default Header
