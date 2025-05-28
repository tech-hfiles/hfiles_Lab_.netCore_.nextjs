'use client'

import React, { useState } from 'react'
import { useFormik } from 'formik'
import * as Yup from 'yup'
import { AdminCreate, HfidCheck } from '@/services/labServiceApi'
import Home from '../components/Home'
import { toast, ToastContainer } from 'react-toastify'
import { useRouter } from 'next/navigation'

const AdminCreates = () => {
  const router = useRouter();
  const userId = localStorage.getItem("userId");
  const email = localStorage.getItem("emailId");

  const [isHFIDValid, setIsHFIDValid] = useState(false);
  const [checking, setChecking] = useState(false);

  const validateHFID = async (hfid: string) => {
    try {
      setChecking(true);
      const res = await HfidCheck({ hfid });
      if (res?.data) {
        toast.success('HFID is valid');
        setIsHFIDValid(true);
      } else {
        toast.error('Invalid HFID');
        setIsHFIDValid(false);
      }
    } catch (err) {
      console.error(err);
      toast.error('Error validating HFID');
    } finally {
      setChecking(false);
    }
  };

  const formik = useFormik({
    initialValues: {
      hfid: '',
      role: 'Super Admin',
      password: '',
      confirmPassword: ''
    },
    validationSchema: Yup.object({
      hfid: Yup.string().required('HFID is required'),
      role: Yup.string().required('Role is required'),
      password: Yup.string().required('Password is required'),
      confirmPassword: Yup.string()
        .oneOf([Yup.ref('password')], 'Passwords must match')
        .required('Confirm Password is required'),
    }),
    onSubmit: async (values, { resetForm }) => {
      try {
        const payload = {
          userId: Number(userId),
          email: email,
          hfid: values.hfid,
          role: values.role,
          password: values.password,
          confirmPassword: values.confirmPassword,
        };
        const response = await AdminCreate(payload);
        localStorage.setItem("authToken", response.data.token);
        localStorage.setItem("username", response.data.username);
        toast.success('Admin created successfully!');
        router.push("/labHome");
        resetForm();
      } catch (error) {
        console.error(error);
        toast.error('Something went wrong!');
      }
    },
  });


  return (
    <Home>
      <div className="max-w-md mx-auto p-4 shadow rounded">
        <h2 className="text-xl font-semibold mb-4">Create Lab Admin</h2>

        {/* HFID Check Section */}
        <div className="space-y-4">
          <label className="block text-sm font-medium">HFID</label>
          <input
            type="text"
            name="hfid"
            value={formik.values.hfid}
            onChange={formik.handleChange}
            onBlur={formik.handleBlur}
            className="w-full border p-2 rounded"
            disabled={isHFIDValid}
          />
          {formik.touched.hfid && formik.errors.hfid && (
            <div className="text-red-500 text-sm">{formik.errors.hfid}</div>
          )}
          {!isHFIDValid && (
            <button
              type="button"
              onClick={() => validateHFID(formik.values.hfid)}
              className="bg-green-600 text-white py-2 px-4 rounded hover:bg-green-700"
              disabled={checking}
            >
              {checking ? 'Checking...' : 'Verify HFID'}
            </button>
          )}
        </div>

        {/* Admin Form Section */}
        {isHFIDValid && (
          <form onSubmit={formik.handleSubmit} className="space-y-4 mt-6">
            <div>
              <label className="block text-sm font-medium">Role</label>
              <input
                type="text"
                name="role"
                value={formik.values.role}
                onChange={formik.handleChange}
                onBlur={formik.handleBlur}
                disabled
                className="w-full border p-2 rounded"
              />
              {formik.touched.role && formik.errors.role && (
                <div className="text-red-500 text-sm">{formik.errors.role}</div>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium">Password</label>
              <input
                type="password"
                name="password"
                value={formik.values.password}
                onChange={formik.handleChange}
                onBlur={formik.handleBlur}
                className="w-full border p-2 rounded"
              />
              {formik.touched.password && formik.errors.password && (
                <div className="text-red-500 text-sm">{formik.errors.password}</div>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium">Confirm Password</label>
              <input
                type="password"
                name="confirmPassword"
                value={formik.values.confirmPassword}
                onChange={formik.handleChange}
                onBlur={formik.handleBlur}
                className="w-full border p-2 rounded"
              />
              {formik.touched.confirmPassword && formik.errors.confirmPassword && (
                <div className="text-red-500 text-sm">{formik.errors.confirmPassword}</div>
              )}
            </div>

            <button
              type="submit"
              className="bg-blue-600 text-white py-2 px-4 rounded hover:bg-blue-700"
            >
              Create Admin
            </button>
          </form>
        )}
        <ToastContainer />
      </div>
    </Home>
  );
};

export default AdminCreates;
