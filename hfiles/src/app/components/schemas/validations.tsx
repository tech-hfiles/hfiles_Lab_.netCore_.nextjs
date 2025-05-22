import * as Yup from "yup";

export const SignUpValidationsSchema = Yup.object({
      labName: Yup.string().required("Lab name is required"),
      labEmail: Yup.string()
        .matches(
          /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/,
          "Please enter a valid email address"
        )
        .required("Email is required"),
      phone: Yup.string()
        .matches(
          /^[0-9]{10}$/,
          "Phone number must be exactly 10 digits and contain only numbers"
        )
        .required("Phone number is required"),
      pincode: Yup.string()
        .matches(
          /^[0-9]{6}$/,
          "Pincode must be exactly 6 digits and contain only numbers"
        )
        .required("Pincode is required"),
      password: Yup.string()
        .min(6, "Minimum 6 characters")
        .required("Password is required"),
      confirmPassword: Yup.string()
        .oneOf([Yup.ref("password")], "Passwords must match")
        .required("Confirm your password"),
      acceptTerms: Yup.boolean().when("otp", {
        is: (otp: string) => otp && otp.length === 6,
        then: (schema) =>
          schema.oneOf([true], "You must accept the terms and conditions"),
        otherwise: (schema) => schema.notRequired(),
      }),
      otp: Yup.string()
        .trim()
        .matches(/^\d{6}$/, "OTP must be exactly 6 digits")
        .when({
          is: true,
          then: (schema) => schema.required("OTP is required").strict(true),
          otherwise: (schema) => schema.notRequired(),
        }),
    });
