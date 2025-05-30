'use client';
import React, { useState, useEffect } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faTimes, faEye, faEyeSlash, faPlus ,faLock, faSquarePlus } from '@fortawesome/free-solid-svg-icons';
import { useFormik } from 'formik';
import * as Yup from 'yup';

type UserInfo = {
  name: string;
  email: string;
  profilePhoto: string;
};

const AddTeamMemberModal: React.FC<AddTeamMemberModalProps> = ({ isOpen, onClose, onSubmit }) => {
  const [showAssignPassword, setShowAssignPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [userFound, setUserFound] = useState<UserInfo | null>(null) as  any;
  const [hasBranches, setHasBranches] = useState(false); // This would come from your API/data

  // Mock branch data - replace with your actual branch data
  const branches = [
    { id: 1, name: 'Gurukul - Ahmedabad' },
    { id: 2, name: 'Branch 2 - City' },
    { id: 3, name: 'Branch 3 - Location' }
  ];

  // Mock user data - replace with your actual API call
  const mockUserLookup = (hfId) => {
    if (hfId === 'ankit123') {
      return {
        name: 'Ankit k.',
        email: 'ankitkuchara420@gmail.com',
        profilePhoto: '/api/placeholder/60/60'
      };
    }
    return null;
  };

  // Formik validation schema
  const validationSchema = Yup.object({
    hfId: Yup.string()
      .required('HF ID is required')
      .min(3, 'HF ID must be at least 3 characters'),
    branch: Yup.string()
      .required('Branch selection is required'),
    assignPassword: Yup.string()
      .required('Password is required')
      .min(6, 'Password must be at least 6 characters')
      .matches(
        /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/,
        'Password must contain at least one uppercase letter, one lowercase letter, and one number'
      ),
    confirmPassword: Yup.string()
      .required('Confirm password is required')
      .oneOf([Yup.ref('assignPassword')], 'Passwords must match'),
  });

  const formik = useFormik({
    initialValues: {
      hfId: '',
      branch: '',
      assignPassword: '',
      confirmPassword: ''
    },
    validationSchema,
    onSubmit: (values) => {
      if (!userFound) {
        formik.setFieldError('hfId', 'Please verify name and email before adding');
        return;
      }
      onSubmit(values);
      handleClose();
    },
  });

  const handleClose = () => {
    formik.resetForm();
    setUserFound(null);
    setHasBranches(false);
    setShowAssignPassword(false);
    setShowConfirmPassword(false);
    onClose();
  };

  // Handle HF ID lookup
  useEffect(() => {
    if (formik.values.hfId) {
      const user = mockUserLookup(formik.values.hfId);
      setUserFound(user as string);
      
      // Simulate branch availability check
      setHasBranches(formik.values.hfId.length > 3);
      
      // Set default branch if no branches available
      if (formik.values.hfId.length <= 3) {
        formik.setFieldValue('branch', 'From Gurukul - Ahmedabad');
      }
    } else {
      setUserFound(null);
      setHasBranches(false);
    }
  }, [formik.values.hfId]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg shadow-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto">
        <div className='flex justify-end mx-2 mt-2'>

      <button
            onClick={handleClose}
            className="text-gray-400 hover:text-gray-600 text-xl flex justify-end"
          >
            <FontAwesomeIcon icon={faTimes} />
          </button>
        </div>
        {/* Header */}
        <div className="flex items-center justify-center p-3 border-b">
          <div className="flex items-center gap-2">
            <FontAwesomeIcon icon={faSquarePlus} className="text-blue-600" />
            <h2 className="text-xl font-semibold text-blue-600">Add a Team Member</h2>
          </div>
          
        </div>

        <form onSubmit={formik.handleSubmit} className="space-y-6">
  <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mt-2 mx-3">
    {/* HF ID Field with Icon */}
    <div className="flex items-center gap-3">
      <div className="bg-blue-600 text-white px-3 py-2 rounded font-semibold text-sm">HF</div>
      <div className="flex-1 relative">
        <input
          type="text"
          name="hfId"
          value={formik.values.hfId}
          onChange={formik.handleChange}
          onBlur={formik.handleBlur}
          placeholder="Member's HF id."
          className={`flex-1 w-full px-3 py-2 border rounded focus:outline-none focus:ring-2 focus:ring-blue-500 ${
          formik.touched.hfId && formik.errors.hfId ? 'border-red-500' : 'border-gray-300'
        }`}

        />
        {formik.touched.hfId && formik.errors.hfId && (
          <p className="text-red-500 text-sm mt-1">{formik.errors.hfId}</p>
        )}
      </div>
    </div>

    {/* Branch Selection */}
    <div className="relative">
      {hasBranches ? (
        <select
          name="branch"
          value={formik.values.branch}
          onChange={formik.handleChange}
          onBlur={formik.handleBlur}
          className={`flex-1 w-full px-3 py-2 border rounded focus:outline-none focus:ring-2 focus:ring-blue-500 ${
            formik.touched.branch && formik.errors.branch ? 'border-red-500' : 'border-gray-300'
          }`}
        >
          <option value="">Select a branch they work from</option>
          {branches.map(branch => (
            <option key={branch.id} value={branch.name}>
              {branch.name}
            </option>
          ))}
        </select>
      ) : (
        <input
          type="text"
          name="branch"
          value={formik.values.branch || "From Gurukul - Ahmedabad"}
          onChange={formik.handleChange}
          readOnly
          className={`flex-1 w-full px-3 py-2 border rounded focus:outline-none focus:ring-2 focus:ring-blue-500  ${
            formik.touched.branch && formik.errors.branch ? 'border-red-500' : 'border-gray-300'
          }`}
        />
      )}
      {formik.touched.branch && formik.errors.branch && (
        <p className="text-red-500 text-sm mt-1">{formik.errors.branch}</p>
      )}
    </div>

    {/* Assign Password */}
    <div className="flex items-center gap-3">
      <FontAwesomeIcon icon={faLock} className="text-gray-600 px-3 py-2" />
      <div className="flex-1 relative">
        <input
          type={showAssignPassword ? 'text' : 'password'}
          name="assignPassword"
          value={formik.values.assignPassword}
          onChange={formik.handleChange}
          onBlur={formik.handleBlur}
          placeholder="Assign Password"
          className={`flex-1 w-full px-3 py-2 border rounded focus:outline-none focus:ring-2 focus:ring-blue-500 ${
            formik.touched.assignPassword && formik.errors.assignPassword ? 'border-red-500' : 'border-gray-300'
          }`}
        />
        <button
          type="button"
          onClick={() => setShowAssignPassword(!showAssignPassword)}
          className="absolute right-3 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-gray-600"
        >
          <FontAwesomeIcon icon={showAssignPassword ? faEye : faEyeSlash} />
        </button>
        {formik.touched.assignPassword && formik.errors.assignPassword && (
          <p className="text-red-500 text-sm mt-1">{formik.errors.assignPassword}</p>
        )}
      </div>
    </div>

    {/* Confirm Password */}
    <div className="flex items-center gap-3">
      <div className="flex-1 relative">
        <input
          type={showConfirmPassword ? 'text' : 'password'}
          name="confirmPassword"
          value={formik.values.confirmPassword}
          onChange={formik.handleChange}
          onBlur={formik.handleBlur}
          placeholder="Confirm Password"
          className={`flex-1 w-full px-3 py-2 border rounded focus:outline-none focus:ring-2 focus:ring-blue-500  ${
            formik.touched.confirmPassword && formik.errors.confirmPassword ? 'border-red-500' : 'border-gray-300'
          }`}
        />
        <button
          type="button"
          onClick={() => setShowConfirmPassword(!showConfirmPassword)}
          className="absolute right-3 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-gray-600"
        >
          <FontAwesomeIcon icon={showConfirmPassword ? faEye : faEyeSlash} />
        </button>
        {formik.touched.confirmPassword && formik.errors.confirmPassword && (
          <p className="text-red-500 text-sm mt-1">{formik.errors.confirmPassword}</p>
        )}
      </div>
    </div>
  </div>

  {/* Submit Button */}
    <div className="flex flex-col md:flex-row justify-between items-start p-3 border-t gap-2 mx-3">
  {/* Left Column: Profile + Add Button */}
  <div className="flex flex-col items-center md:items-start w-full sm:w-auto max-w-lg">
    {/* User Profile Card */}
    <div className="bg-blue-100 rounded-lg flex flex-col sm:flex-row sm:items-center border w-full p-4">
      <div className="w-16 h-16 bg-gray-200 rounded-full overflow-hidden mx-auto sm:mx-0 sm:mr-4">
        <img
          src="/3d77b13a07b3de61003c22d15543e99c9e08b69b.jpg"
          alt="Profile"
          className="w-full h-full object-cover"
        />
      </div>
      <div className="flex-1 text-center sm:text-left mt-3 sm:mt-0">
        <h2 className="text-blue-800 text-xl font-bold">Ankit k.</h2>
        <p className="text-black">ankitkuchara420@gmail.com</p>

        {/* Validation Error Message */}
      </div>
    </div>
        {!formik.isValid && (
          <p className="text-red-600 text-sm mt-2 flex items-center gap-1">
            <span>⚠️</span> Please verify name and email before adding.
          </p>
        )}

    {/* Add Button Below Card */}
    <button
      type="button"
      onClick={formik.handleSubmit}
      disabled={!formik.isValid || formik.isSubmitting}
      className={`mt-4 w-full sm:w-full px-6 py-2 rounded-md text-sm font-medium transition cursor-pointer ${
        !formik.isValid || formik.isSubmitting
          ? 'bg-gray-400 text-gray-600 cursor-not-allowed'
          : 'bg-blue-800 text-white hover:bg-blue-900'
      }`}
    >
      {formik.isSubmitting ? 'Adding...' : 'Add'}
    </button>
  </div>

  {/* Right Illustration */}
  <div className="hidden md:block">
    <img
      src="/f02ab4b2b4aeffe41f18ff4ece3c64bd20e9a0f4.png"
      alt="Illustration"
      className="w-[200px] h-[200px] object-cover"
    />
  </div>
</div>

</form>

      </div>
    </div>
  );
};

export default AddTeamMemberModal;