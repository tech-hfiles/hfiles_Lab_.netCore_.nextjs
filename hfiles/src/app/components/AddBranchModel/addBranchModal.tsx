import { faBuilding, faEnvelope, faMapMarkerAlt, faPhone, faTimes } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import React from 'react'

const addBranchModal = () => {
      const [formData, setFormData] = React.useState({
    name: "",
    email: "",
    phone: "",
    pinCode: ""
  });

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleSave = () => {
    // Validate form data
    if (!formData.name || !formData.email || !formData.phone || !formData.pinCode) {
      alert("Please fill in all fields");
      return;
    }
    
    // Call the onSave callback with form data
    onSave(formData);
    
    // Reset form
    setFormData({
      name: "",
      email: "",
      phone: "",
      pinCode: ""
    });
    
    // Close modal
    onClose();
  };

  const handleClose = () => {
    // Reset form when closing
    setFormData({
      name: "",
      email: "",
      phone: "",
      pinCode: ""
    });
    onClose();
  };

  if (!isOpen) return null;
  
  return (
    <div> <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-2xl max-h-[90vh] overflow-y-auto">
        {/* Modal Header */}
        <div className="flex items-center justify-between p-6 border-b">
          <div className="flex items-center gap-2">
            <FontAwesomeIcon icon={faBuilding} className="text-blue-600" />
            <h2 className="text-xl font-bold text-blue-600">Add New Branch</h2>
          </div>
          <button
            onClick={handleClose}
            className="text-gray-400 hover:text-gray-600 transition-colors"
          >
            <FontAwesomeIcon icon={faTimes} size="lg" />
          </button>
        </div>

        {/* Modal Content */}
        <div className="flex flex-col md:flex-row">
          {/* Left Side - Form */}
          <div className="flex-1 p-6">
            <div className="space-y-4">
              {/* Lab Name */}
              <div className="relative">
                <FontAwesomeIcon 
                  icon={faBuilding} 
                  className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400"
                />
                <input
                  type="text"
                  name="name"
                  value={formData.name}
                  onChange={handleInputChange}
                  placeholder="NorthStar"
                  className="w-full pl-10 pr-4 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                />
              </div>

              {/* Email */}
              <div className="relative">
                <FontAwesomeIcon 
                  icon={faEnvelope} 
                  className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400"
                />
                <input
                  type="email"
                  name="email"
                  value={formData.email}
                  onChange={handleInputChange}
                  placeholder="Enter Lab Email"
                  className="w-full pl-10 pr-4 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                />
              </div>

              {/* Phone */}
              <div className="relative">
                <FontAwesomeIcon 
                  icon={faPhone} 
                  className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400"
                />
                <input
                  type="tel"
                  name="phone"
                  value={formData.phone}
                  onChange={handleInputChange}
                  placeholder="Lab Number"
                  className="w-full pl-10 pr-4 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                />
              </div>

              {/* Pin Code */}
              <div className="relative">
                <FontAwesomeIcon 
                  icon={faMapMarkerAlt} 
                  className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400"
                />
                <input
                  type="text"
                  name="pinCode"
                  value={formData.pinCode}
                  onChange={handleInputChange}
                  placeholder="Enter Pin-Code"
                  className="w-full pl-10 pr-4 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                />
              </div>

              {/* Location Info */}
              <div className="text-sm text-blue-600 pl-2">
                Gujarat: 380052, Ahmedabad, Gujarat
              </div>
            </div>
          </div>

          {/* Right Side - Illustration */}
          <div className="w-full md:w-80 p-6 bg-gray-50 flex items-center justify-center">
            <div className="text-center">
              <div className="w-48 h-32 bg-gray-200 rounded-lg mb-4 flex items-center justify-center">
                <div className="text-center">
                  <FontAwesomeIcon icon={faBuilding} size="3x" className="text-gray-400 mb-2" />
                  <div className="text-sm text-gray-500">Branch Office</div>
                </div>
              </div>
              <div className="text-sm text-gray-600">
                Add your new branch details
              </div>
            </div>
          </div>
        </div>

        {/* Modal Footer */}
        <div className="flex justify-end gap-3 p-6 border-t bg-gray-50">
          <button
            onClick={handleClose}
            className="px-6 py-2 border border-gray-300 rounded-lg text-gray-700 hover:bg-gray-100 transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={handleSave}
            className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
          >
            Save
          </button>
        </div>
      </div>
    </div></div>
  )
}

export default addBranchModal