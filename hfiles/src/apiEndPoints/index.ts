// Api EndPoints

export const API_Lab_Reports = "https://localhost:7227/api/";


export const endPoints = {
    Lab_Reports: {
        OTP_GENERATE: API_Lab_Reports + "LabOtp/generate",
        SIGN_UP: API_Lab_Reports + "LabSignupUser/labsignup",
    },
    Lab_Login: {
        LOGIN_SEND_OTP: API_Lab_Reports + "LabLogin/send-otp",
        LOGIN_OTP: API_Lab_Reports + "LabLogin/login-otp",
        LOGIN_PASSWORD: API_Lab_Reports + "LabLogin/login-password",
    },
    Lab_User_Reports:{
        ADD_LAb_USER_REPORT: API_Lab_Reports + "LabUserReport/upload-batch",
    }
};