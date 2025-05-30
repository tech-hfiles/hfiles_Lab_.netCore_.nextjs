import { faWhatsapp } from "@fortawesome/free-brands-svg-icons";
import { faEnvelope } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import React from "react";

const Footer = () => {
  return (
    <footer className="w-full text-white px-4 py-3 bg-[#0331B5]">
      <div className="max-w-screen-xl mx-auto flex flex-col sm:flex-row sm:justify-between items-center text-center sm:text-left gap-3">
        {/* Legal Section */}
        <div className="flex flex-wrap justify-center sm:justify-start items-center space-x-2">
          <a href="#" className="hover:underline text-sm">
            Terms & Conditions
          </a>
          <span className="mx-1">|</span>
          <a href="#" className="hover:underline text-sm">
            Privacy & Policy
          </a>
        </div>

        {/* Info Section */}
        <div className="flex flex-wrap justify-center items-center space-x-2 text-sm">
          <a href="#" className="hover:underline">
            FAQ's
          </a>
          <span className="mx-1">|</span>
          <p>© 2025 HFiles</p>
        </div>

        {/* Contact Section */}
        <div className="flex flex-wrap justify-center sm:justify-end items-center space-x-4 text-sm">
          <a
            href="https://wa.me/919978043453"
            target="_blank"
            rel="noopener noreferrer"
            className="flex items-center space-x-1"
          >
            <FontAwesomeIcon icon={faWhatsapp} />
            <span>9978043453</span>
          </a>
          <span className="mx-1">|</span>
          <div className="flex items-center space-x-1">
            <FontAwesomeIcon icon={faEnvelope} />
            <span>contact@hfiles.in</span>
          </div>
        </div>
      </div>
    </footer>
  );
};

export default Footer;
