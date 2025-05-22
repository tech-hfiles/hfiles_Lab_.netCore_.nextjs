import React from 'react'

const LabHeader = () => {
  return (
       <div>
      <header className="sticky top-0 z-50  text-white px-6 py-4 flex justify-between items-center"  style={{ backgroundColor: '#0331B5' }}>
        <div className="text-2xl font-bold flex items-center">
          <img src="https://hfiles.in/wp-content/uploads/2022/11/hfiles.png" alt="hfiles logo" className="h-11 w-auto mr-2 "style={{ backgroundColor: '#0331B5' }}  onClick={() => window.location.href = '/labHome'}/>
        </div>
        <div className="flex items-center space-x-3">
    <p className="text-white font-medium">NorthStar</p>
    <img
      src="/0e7f5f4a77770635e93d82998df96f869b6624bf.png"
      alt="Profile"
      className="h-10 w-10 rounded-full border-2 border-yellow-400"
    />
  </div>
        
      </header>
    </div>
  )
}

export default LabHeader;