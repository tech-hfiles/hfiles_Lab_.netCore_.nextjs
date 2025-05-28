"use client";
import React, { useEffect, useState } from "react";
import DefaultLayout from "../components/DefaultLayout";
import { ListReport, ResendReport } from "@/services/labServiceApi";
import { useSearchParams } from "next/navigation";
import { useFormik } from "formik";
import * as Yup from "yup";
import { toast, ToastContainer } from "react-toastify";


const SharedReportsPage = () => {
  const [isResendMode, setIsResendMode] = useState(false);
  const [reportslist, setReportsList] = useState<any[]>([]);
  const [userDetail, setUserdetail] = useState<any>({});
  const searchParams = useSearchParams();
  const userId = searchParams.get("userId");


    // Base URL where your files are served from your .NET Core API's wwwroot/uploads folder
  const BASE_FILE_URL = "https://localhost:7227/uploads/";

  const LabReportList = async () => {
    const response = await ListReport(Number(userId));
    setReportsList(response.data.reports);
    setUserdetail(response.data.userDetails);
  };

  useEffect(() => {
    LabReportList();
  }, []);

  const formik = useFormik({
    initialValues: {
      ids: [] as number[],
    },
    validationSchema: Yup.object({
      ids: Yup.array().min(1, "Please select at least one report."),
    }),
    onSubmit: async (values, { resetForm }) => {
      try {
        await ResendReport({ ids: values.ids });
        toast.success("Report(s) resent successfully.");
        resetForm();
        setIsResendMode(false);
      } catch (error) {
        console.error("Failed to resend reports", error);
        toast.error("Failed to resend reports.");
      }
    },
  });

  return (
    <DefaultLayout>
      <div className="mx-auto p-4">
        {/* Page Title */}
        <div className="text-center mb-4">
          <h1 className="text-2xl font-bold">Shared Reports</h1>
          <div className="border-b border-gray-300 w-36 mx-auto mt-1"></div>
        </div>

        {/* User Profile Card */}
        <div className="bg-blue-100 rounded-lg flex flex-col sm:flex-row sm:items-center max-w-lg border">
          <div className="mx-auto sm:mx-0 sm:mr-4">
            <div className="w-16 h-16 bg-gray-200 rounded-full overflow-hidden mx-3">
              <img
                src="/3d77b13a07b3de61003c22d15543e99c9e08b69b.jpg"
                alt="Profile"
                className="w-full h-full object-cover"
              />
            </div>
          </div>
          <div className="flex-1 text-center sm:text-left">
            <h2 className="text-blue-800 text-xl font-bold">
              {userDetail.fullName}
            </h2>
            <p className="text-black">
              <span className="font-bold">Email:</span> {userDetail.email}
            </p>
          </div>
          <div className="bg-white p-2 rounded-lg mb-3 sm:mb-26 mt-3 sm:mt-0">
            <p className="text-sm">{userDetail.hfid}</p>
          </div>
        </div>

        <form onSubmit={formik.handleSubmit}>
          <div>
            {/* Report Info */}
            {reportslist.map((report: any, index: number) => {
              const id = report.labUserReportId;
              const isChecked = formik.values.ids.includes(id);

              return (
                <div key={index}>
                  <div className="flex justify-end">
                    <p>{report.createdDate}</p>
                  </div>
                  <div className="border mb-3"></div>

                  {report.branchName !== report.labName && (
                    <div className="flex justify-center items-center mb-2 text-green-600">
                      <div className="w-5 h-5 rounded-full bg-green-500 flex items-center justify-center mr-1">
                        <span className="text-white text-sm">✓</span>
                      </div>
                      <p className="text-sm">
                        This report was sent by the Mumbai branch.
                      </p>
                    </div>
                  )}

                  <div className="border border-gray-300 rounded w-32 h-32 flex flex-col items-center justify-center gap-2 mb-3 relative px-2">
                    {isResendMode && (
                      <input
                        type="checkbox"
                        className="absolute bottom-2 right-2 w-4 h-4"
                        checked={isChecked}
                        onChange={() => {
                          const newIds = isChecked
                            ? formik.values.ids.filter((itemId) => itemId !== id)
                            : [...formik.values.ids, id];
                          formik.setFieldValue("ids", newIds);
                        }}
                      />
                    )}

                    {report.fileURL?.toLowerCase().endsWith(".pdf") ? (
                      <a
                        href={`${BASE_FILE_URL}${report.fileURL}`}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="text-blue-600 underline"
                      >
                        View PDF Report
                      </a>
                    ) : (
                      <img
                        src={`${BASE_FILE_URL}${report.fileURL}`}
                        alt="Report Thumbnail"
                        className="w-32 h-32 object-contain"
                      />
                    )}
                  </div>

                  <p className="text-sm w-full text-start whitespace-nowrap overflow-hidden text-ellipsis px-1">
                    {report.filename}
                  </p>
                </div>
              );
            })}
          </div>

          {/* Validation Message */}
          {formik.errors.ids && formik.touched.ids && (
            <div className="text-red-600 text-sm mt-2">{formik.errors.ids}</div>
          )}
          <div className="border mt-3"></div>

          {/* Resend & Submit Buttons */}
          <div className="flex justify-end mt-6 space-x-2">
            {isResendMode && (
              <button
                type="submit"
                className="bg-green-600 hover:bg-green-500 text-white font-semibold px-6 py-2 rounded-sm"
              >
                Submit
              </button>
            )}
            <button
              type="button"
              onClick={() => {
                setIsResendMode(!isResendMode);
                formik.resetForm();
              }}
              className={`${
                isResendMode
                  ? "bg-gray-400 hover:bg-gray-500 text-white"
                  : "bg-yellow-300 hover:bg-yellow-400 text-gray-800"
              } font-semibold px-6 py-2 rounded-sm`}
            >
              {isResendMode ? "Cancel" : "Resend"}
            </button>
          </div>
        </form>
        <ToastContainer />
      </div>
    </DefaultLayout>
  );
};

export default SharedReportsPage;
