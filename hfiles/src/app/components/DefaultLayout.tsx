import React from 'react'
import Sidebar from '@/LabSidebar.tsx/Sidebar'
import Footer from './Footer'
import LabHeader from './LabHeader';

interface HomeProps {
  children: React.ReactNode;
}

const DefaultLayout : React.FC<HomeProps> = ({ children }) => {
  return (
     <div className="h-screen flex flex-col overflow-hidden">
      {/* Header */}
      <div className="sticky top-0 z-50">
       <LabHeader />
      </div>

      {/* Main Content Area with Sidebar and Content */}
      <div className="flex flex-grow overflow-hidden pb-14">
        {/* The key change is removing the wrapping div from the sidebar */}
        <Sidebar className="h-full" />
        

        {/* Main Children Content */}
        <main className="flex-grow overflow-y-auto lg:pb-15 sm:pb-25 mx-auto ">{children}</main>
      </div>

      {/* Footer */}
      <div className="sticky bottom-0 z-50">
        <Footer />
      </div>
    </div>
  )
}

export default DefaultLayout