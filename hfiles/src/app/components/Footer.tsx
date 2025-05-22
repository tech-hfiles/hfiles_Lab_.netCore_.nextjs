import { faWhatsapp } from "@fortawesome/free-brands-svg-icons";
import {
  faEnvelope,
} from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import React from "react";

const Footer = () => {
  return (
    <footer className="fixed bottom-0 left-0 w-full z-40  text-white px-6 py-4" style={{ backgroundColor: '#0331B5' }}>
      <div className="max-w-auto mx-auto flex flex-col md:flex-row justify-between items-center \-sm text-center md:text-left gap-3">
        {/* Section 1: Legal */}
        <div className="flex items-center space-x-2">
          <a href="#" className="hover:underline">
            Terms & Conditions
          </a>
          <span className="mx-1">|</span>
          <a href="#" className="hover:underline">
            Privacy & Policy
          </a>
        </div>

        {/* Section 2: Info */}
        <div className="flex items-center space-x-2">
          <a href="#" className="hover:underline">
            FAQ's
          </a>
          <span className="mx-1">|</span>
          <p>Copyright@2025</p>
        </div>

        {/* Section 3: Contact */}
        <div className="flex items-center space-x-4">
          <a
            href="https://wa.me/919978043453"
            target="_blank"
            rel="noopener noreferrer"
            className="flex items-center space-x-1"
          >
            <FontAwesomeIcon icon={faWhatsapp} className="mt-1" />
            <span>9978043453</span>
          </a>
          <span className="mx-1">|</span>
          <div className="flex items-center space-x-1">
            <FontAwesomeIcon icon={faEnvelope} className="mt-1" />
            <span>contact@hfiles.in</span>
          </div>
        </div>
      </div>
    </footer>
  );
};

export default Footer;