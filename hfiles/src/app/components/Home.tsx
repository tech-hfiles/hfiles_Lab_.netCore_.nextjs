
import React from "react";
import Header from "./Header";
import Footer from "./Footer";

interface HomeProps {
  children: React.ReactNode;
}

const Home: React.FC<HomeProps> = ({ children }) => {
  return (
    <div className="h-screen flex flex-col">
      {/* Sticky Header */}
      <div className="sticky top-0 z-50">
        <Header />
      </div>

      {/* Scrollable Main Content */}
      <main className="flex-grow overflow-y-auto pb-25">{children}</main>

      {/* Sticky Footer */}
      <div className="sticky bottom-0 z-50">
        <Footer />
      </div>
    </div>
  );
};

export default Home;
