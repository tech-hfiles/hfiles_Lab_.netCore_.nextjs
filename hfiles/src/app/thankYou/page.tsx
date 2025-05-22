"use client";
import React from 'react';
import Home from '../components/Home';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faCheckCircle } from '@fortawesome/free-solid-svg-icons';

const ThankYouPage = () => {
  return (
    <Home>
      <div className="min-h-auto bg-gray-50 flex items-center justify-center p-5">
        <div className="bg-white rounded-xl shadow-lg p-8 max-w-2xl w-full text-center animate-fade-in">
          <div className="mb-6 animate-scale-up">
            <FontAwesomeIcon icon={faCheckCircle}  size='xl'
              className="w-16 h-16 text-green-500 mx-auto drop-shadow-lg" 
            />
          </div>
          
          <h1 className="text-4xl font-bold text-gray-800 mb-4">
            Thank You! 🎉
          </h1>
          
          <p className="text-xl text-gray-600 font-medium mb-6">
            Your submission has been received
          </p>
          
          <p className="text-gray-700 leading-relaxed mb-8">
            We appreciate you taking the time to complete this form. Our team will 
            review your information and respond within 1-2 business days.
          </p>

          <div className="bg-gray-50 p-5 rounded-lg my-6">
            <p className="text-gray-600 mb-2">Need immediate assistance?</p>
            <p className="text-green-600 font-semibold hover:underline">
              <a href="mailto:contact@hfiles.in">contact@hfiles.in</a>
            </p>
          </div>

          <button
            className="bg-green-600 hover:bg-green-700 text-white font-medium py-3 px-6 rounded-lg transition-colors duration-300"
            onClick={() => window.location.href = '/'}
          >
            Return to Homepage
          </button>
        </div>
      </div>
    </Home>
  );
};

export default ThankYouPage;

