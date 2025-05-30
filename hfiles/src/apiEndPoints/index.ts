// Api EndPoints

export const API_Lab_Reports = "https://localhost:7227/api/";


export const endPoints = {
    Lab_Reports: {
        OTP_GENERATE: API_Lab_Reports + "labs/signup/otp",
        SIGN_UP: API_Lab_Reports + "labs",
        SIDEBAR_ID : API_Lab_Reports + "labs/hfid"
    },
    Lab_Login: {
        LOGIN_SEND_OTP: API_Lab_Reports + "labs/otp",
        LOGIN_OTP: API_Lab_Reports + "labs/login/otp",
        LOGIN_PASSWORD: API_Lab_Reports + "labs/login/password",
        CHECK_HFID: API_Lab_Reports + "users/hfid"
    },
    Lab_User_Reports:{
        ADD_LAb_USER_REPORT: API_Lab_Reports + "labs/reports/upload",
        Report_LAB_USERReport :  API_Lab_Reports + "labs/reports",
        LIST_LAB_USERLIST: API_Lab_Reports + "labs/reports",
        USERCARD: API_Lab_Reports + "labs/users",
        SEND_REPORT: API_Lab_Reports + "labs/reports/resend",
    },
    Lab_Admin:{
        CREATE_ADMIN: API_Lab_Reports + "labs/super-admins",
        LOGIN_ADMIN: API_Lab_Reports + "labs/users/login",
    },
    Lab_Profile:{
        LIST_BRANCHDATA: API_Lab_Reports + "LabBranch",
    },
    All_Members: {
        ADD_Memeber: API_Lab_Reports + "labs/members/promote",
        DELETE_MEMBER : API_Lab_Reports + "labs/members",
    }
};