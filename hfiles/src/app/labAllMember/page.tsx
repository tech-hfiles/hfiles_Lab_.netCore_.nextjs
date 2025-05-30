'use client';

import React, { useEffect, useState } from "react";
import DefaultLayout from '../components/DefaultLayout';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faCircleMinus, faSearch, faUserPlus, faUsers } from '@fortawesome/free-solid-svg-icons';
import { AddMember, DeleteMember, UserCardList } from "@/services/labServiceApi";
import { useFormik } from "formik";
import * as Yup from 'yup';
import { toast, ToastContainer } from "react-toastify";
import AddTeamMemberModal from "./components/AddTeamMemberModal";

interface Admin {
  adminId: number;
  name: string;
  email: string;
  hfid: string;
  profilePhoto: string;
  status: string;
}

interface Member {
  memberId: number;
  name: string;
  email: string;
  hfid: string;
  profilePhoto: string;
  status: string;
  promotedByName: string;
}

const Page = () => {
  const [adminsList, setAdminsList] = useState<Admin[]>([]);
  const [memberList, setMemberList] = useState<Member[]>([]);
  const [showCheckboxes, setShowCheckboxes] = useState(false);
  const [manageMode, setManageMode] = useState(false);
  const [showAddMemberModal, setShowAddMemberModal] = useState(false) as any;


  const userId = localStorage.getItem("userId");
  const BASE_URL = "https://hfiles.in/upload/";

  const formik = useFormik({
    initialValues: {
      selectedMembers: [] as number[],
    },
    validationSchema: Yup.object({
      selectedMembers: Yup.array()
        .min(1, 'Please Select a Member to Assign Admin Access.'),
    }),
    onSubmit: async (values) => {
      const payload = {
        ids: values.selectedMembers,
      };
      try {
        const response = await AddMember(payload);
        toast.success(`${response.data.message}`)
        await CardList();
        formik.resetForm();
        setShowCheckboxes(false);
      } catch (error) {
        console.error("AddMember API error:", error);
        toast.error("Failed to assign admin access. Please try again.");
      }
    },
  });

  const CardList = async () => {
    const res = await UserCardList(Number(userId));
    setAdminsList(res.data.superAdmin);
    setMemberList(res.data.members);
  };

  useEffect(() => {
    CardList();
  }, []);

  const handleRemoveMember = async (memberId: number) => {
    try {
      const response = await DeleteMember(memberId)
      toast.success("Member marked as deleted successfully.");
      await CardList();
      formik.resetForm();
      setManageMode(false);
    } catch (error) {
      toast.error("Admin cannot be deleted.");
      console.error(error);
    }
  };

    const handleAddTeamMember = async (formData:any) => {
    console.log("New team member data:", formData);
    
    try {
      // You can make your API call here to add the new team member
      // Example:
      // const response = await AddTeamMemberAPI(formData);
      
      toast.success("Team member added successfully!");
      await CardList(); // Refresh the list
      setShowAddMemberModal(false);
    } catch (error) {
      console.error("Add team member error:", error);
      toast.error("Failed to add team member. Please try again.");
    }
  };


  return (
    <DefaultLayout>
      <div className="p-2 sm:p-4">
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-2">
          <div className="text-xl font-bold text-black mx-3">All Members:</div>
          <div className="relative w-full sm:w-auto mx-3">
            <input
              type="text"
              placeholder="Search"
              className="pl-2 pr-10 py-1 border rounded-full focus:outline-none focus:ring-2 focus:ring-blue-500 w-full"
            />
            <FontAwesomeIcon
              icon={faSearch}
              className="absolute right-0 top-0 text-white bg-black p-2 rounded-full hover:bg-gray-800 cursor-pointer"
            />
          </div>
        </div>

        <div className="border-b "></div>

        <div className="mb-8">
          <h2 className="text-lg font-semibold text-blue-600 mb-4 mt-4">Admins:</h2>
          <div className="flex flex-col md:flex-row gap-4">
            <div className="w-full md:w-1/2 space-y-4">
              {/* {adminsList.map((admin) => ( */}
                <div key={adminsList.adminId} className="relative flex items-start gap-4 border rounded-lg p-4 bg-white">
                  <img
                    src={`${BASE_URL}${adminsList.profilePhoto}`}
                    alt={adminsList.name}
                    className="w-20 h-20 rounded-sm object-cover"
                  />
                  <div className="gap-3 p-2">
                    <p className="text-sm"><span className="font-semibold">Name:</span> {adminsList.name}</p>
                    <p className="text-sm"><span className="font-semibold">E-mail:</span> {adminsList.email}</p>
                    <p className="text-sm"><span className="font-semibold">HF_id:</span> {adminsList.hfid}</p>
                  </div>
                  <span className={`absolute top-0 right-0 text-xs text-white px-2 py-1 rounded bg-green-600`}>
                    Main
                  </span>
                </div>
              {/* ))} */}
            </div>
          </div>

          <div className="mt-2 flex justify-end mb-4">
            <button
              className="bg-blue-600 text-white px-4 py-2 rounded text-sm font-medium hover:bg-blue-700 flex items-center gap-2 cursor-pointer"
              onClick={() => setShowCheckboxes(!showCheckboxes)}
            >
              <FontAwesomeIcon icon={faUserPlus} className="h-4 w-4" />
              Add Admin
            </button>
          </div>

          <div className="border"></div>

          <div className="p-4">
            <div className="flex justify-between items-center mb-4">
              <h2 className="text-lg font-semibold text-blue-600">Team:</h2>
              <button
                type="button"
                className="bg-yellow-300 text-black px-4 py-2 rounded font-medium flex items-center gap-2 shadow hover:bg-yellow-400 cursor-pointer"
                onClick={() => setManageMode(!manageMode)}
              >
                <FontAwesomeIcon icon={faUsers} className="h-5 w-5" />
                {manageMode ? "Cancel" : "Manage Team"}
              </button>
            </div>

            <form onSubmit={formik.handleSubmit}>
              {formik.errors.selectedMembers && formik.touched.selectedMembers && (
                <p className="text-red-500 text-sm mt-2 text-center">{formik.errors.selectedMembers}</p>
              )}
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
                {memberList.map((member) => {
                  const isChecked = formik.values.selectedMembers.includes(member.memberId);
                  return (
                    <div key={member.memberId} className="flex flex-col">
                      {/* Member Card */}
                      <div className="relative flex flex-col sm:flex-row items-start gap-4 border rounded-lg p-4 bg-white shadow">
                        <div className="relative w-20 h-20">
                          <img
                            src={`${BASE_URL}${member.profilePhoto}`}
                            alt={member.name}
                            className={`w-full h-full object-cover rounded ${showCheckboxes ? 'opacity-40' : ''}`}
                          />
                          {showCheckboxes && (
                            <div className="absolute inset-0 flex items-center justify-center">
                              <input
                                type="checkbox"
                                checked={isChecked}
                                onChange={() => {
                                  const selected = formik.values.selectedMembers;
                                  if (selected.includes(member.memberId)) {
                                    formik.setFieldValue(
                                      'selectedMembers',
                                      selected.filter(id => id !== member.memberId)
                                    );
                                  } else {
                                    formik.setFieldValue(
                                      'selectedMembers',
                                      [...selected, member.memberId]
                                    );
                                  }
                                }}
                                className="w-6 h-6 bg-green-600 text-white accent-green-600 rounded border-2 border-white shadow-lg"
                              />
                            </div>
                          )}
                        </div>

                        <div className="flex-1 gap-3 p-2">
                          <p className="text-sm"><span className="font-semibold">Name:</span> {member.name}</p>
                          <p className="text-sm"><span className="font-semibold">E-mail:</span> {member.email}</p>
                          <p className="text-sm"><span className="font-semibold">HF_id:</span> {member.hfid}</p>
                        </div>

                        <span className="absolute top-0 right-0 bg-gray-100 text-gray-800 text-xs px-2 py-1 rounded">
                          By {member.promotedByName}
                        </span>
                      </div>

                      {/* Remove Member Button - Outside the card, centered below */}
                      {manageMode && (
                        <div className="flex justify-end mt-2">
                          <button
                            type="button"
                            onClick={() => handleRemoveMember(member.memberId)}
                            className="text-red-500 text-sm font-medium hover:text-red-700 hover:underline flex items-center gap-1 cursor-pointer"
                          >
                            Remove Member
                            <FontAwesomeIcon icon={faCircleMinus} />
                          </button>
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
              {manageMode && (
                <div className="flex justify-center mt-6">
                  <button
                    type="button"
                    onClick={() => setShowAddMemberModal(true)}
                    className="bg-blue-800 text-white px-6 py-2 rounded-md text-sm font-medium hover:bg-blue-900 transition cursor-pointer"
                  >
                    Add Team Member
                  </button>
                </div>
              )}

              {showCheckboxes && (
                <div className="flex justify-center mt-6">
                  <button
                    type="submit"
                    className="bg-blue-800 text-white px-8 py-2 rounded-md text-lg font-semibold hover:bg-blue-900 transition cursor-pointer"
                  >
                    Submit
                  </button>
                </div>
              )}
            </form>
          </div>
        </div>

 <AddTeamMemberModal
          isOpen={showAddMemberModal}
          onClose={() => setShowAddMemberModal(false)}
          onSubmit={handleAddTeamMember}
        />

        <ToastContainer />
      </div>
    </DefaultLayout>
  );
};

export default Page;
