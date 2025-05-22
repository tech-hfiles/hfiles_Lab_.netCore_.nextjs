import { faArrowLeft, faArrowRight } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import React, { useState } from 'react'

const CustomDatePicker = () => {
      const [currentMonth, setCurrentMonth] = useState(new Date());
  const [selectedDate, setSelectedDate] = useState(new Date());
  const [calendarOpen, setCalendarOpen] = useState(true);

  const currentMonthName = currentMonth.toLocaleString('default', { month: 'long' });
  const currentYear = currentMonth.getFullYear();

  const previousMonth = () => {
    setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() - 1, 1));
  };

  const nextMonth = () => {
    setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1, 1));
  };

  const daysInMonth = new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1, 0).getDate();
  const firstDayOfMonth = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), 1).getDay();
  
  const prevMonthDays = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), 0).getDate();
  
  const days = [];
  
  for (let i = firstDayOfMonth - 1; i >= 0; i--) {
    days.push({
      day: prevMonthDays - i,
      currentMonth: false,
      date: new Date(currentMonth.getFullYear(), currentMonth.getMonth() - 1, prevMonthDays - i)
    });
  }
  
  // Current month days
  for (let i = 1; i <= daysInMonth; i++) {
    days.push({
      day: i,
      currentMonth: true,
      date: new Date(currentMonth.getFullYear(), currentMonth.getMonth(), i)
    });
  }
  
  // Next month days
  const remainingDays = 42 - days.length; 
  for (let i = 1; i <= remainingDays; i++) {
    days.push({
      day: i,
      currentMonth: false,
      date: new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1, i)
    });
  }
  
  // Check if a day is selected
  const isSelected = (date: any) => {
    return date.getDate() === selectedDate.getDate() && 
           date.getMonth() === selectedDate.getMonth() &&
           date.getFullYear() === selectedDate.getFullYear();
  };
  
  const handleDayClick = (date: any) => {
    setSelectedDate(date);
  };
  
  const dayNames = ["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"];
  return (
  <div className="bg-gray-800 text-white p-4 rounded-lg shadow-lg w-64">
      {calendarOpen && (
        <div>
          {/* Header with month and year */}
          <div className="flex justify-between items-center mb-4">
            <button onClick={previousMonth} className="text-gray-400 hover:text-white">
                <FontAwesomeIcon icon={faArrowLeft}  />
            </button>
            <div className="text-lg font-semibold">
              {currentMonthName} {currentYear}
            </div>
            <button onClick={nextMonth} className="text-gray-400 hover:text-white">
               <FontAwesomeIcon icon={faArrowRight} size="lg" />
            </button>
          </div>
          
          {/* Days of week */}
          <div className="grid grid-cols-7 gap-1 mb-2">
            {dayNames.map((day, index) => (
              <div key={index} className="text-center text-gray-400 text-sm py-1">
                {day}
              </div>
            ))}
          </div>
          
          {/* Calendar days */}
          <div className="grid grid-cols-7 gap-1">
            {days.map((day, index) => (
              <div
                key={index}
                onClick={() => handleDayClick(day.date)}
                className={`
                  text-center py-2 cursor-pointer
                  ${!day.currentMonth ? 'text-gray-600' : 'text-white'}
                  ${isSelected(day.date) ? 'bg-blue-600 rounded-full' : 'hover:bg-gray-700 rounded-full'}
                `}
              >
                {day.day}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}

export default CustomDatePicker