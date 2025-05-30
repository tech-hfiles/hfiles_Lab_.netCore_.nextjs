import { faArrowLeft, faArrowRight } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import React, { useState } from 'react';

type CustomDatePickerProps = {
  onDateRangeSelect: (startDate: Date, endDate: Date) => void;
};

const CustomDatePicker: React.FC<CustomDatePickerProps> = ({ onDateRangeSelect }) => {
  const [currentMonth, setCurrentMonth] = useState(new Date());
  const [startDate, setStartDate] = useState<Date | null>(null);
  const [endDate, setEndDate] = useState<Date | null>(null);
  const [selectingEnd, setSelectingEnd] = useState(false);

  const currentMonthName = currentMonth.toLocaleString('default', { month: 'long' });
  const currentYear = currentMonth.getFullYear();

  const previousMonth = () => {
    setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() - 1, 1));
  };

  const nextMonth = () => {
    setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1, 1));
  };

  const handleDayClick = (date: Date) => {
    if (!startDate || (startDate && endDate)) {
      setStartDate(date);
      setEndDate(null);
      setSelectingEnd(true);
    } else if (selectingEnd) {
      if (date < startDate) {
        setEndDate(startDate);
        setStartDate(date);
      } else {
        setEndDate(date);
      }
      setSelectingEnd(false);
    }
  };

  const isSelected = (date: Date) =>
    (startDate && date.getTime() === startDate.getTime()) ||
    (endDate && date.getTime() === endDate.getTime());

  const isInRange = (date: Date) =>
    startDate && endDate && date > startDate && date < endDate;

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

  for (let i = 1; i <= daysInMonth; i++) {
    days.push({
      day: i,
      currentMonth: true,
      date: new Date(currentMonth.getFullYear(), currentMonth.getMonth(), i)
    });
  }

  const remainingDays = 42 - days.length;
  for (let i = 1; i <= remainingDays; i++) {
    days.push({
      day: i,
      currentMonth: false,
      date: new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1, i)
    });
  }

  const dayNames = ["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"];

  return (
    <div className="bg-gray-800 text-white p-4 rounded-lg shadow-lg w-64">
      <div className="flex justify-between items-center mb-4">
        <button onClick={previousMonth} className="text-gray-400 hover:text-white">
          <FontAwesomeIcon icon={faArrowLeft} />
        </button>
        <div className="text-lg font-semibold">
          {currentMonthName} {currentYear}
        </div>
        <button onClick={nextMonth} className="text-gray-400 hover:text-white">
          <FontAwesomeIcon icon={faArrowRight} />
        </button>
      </div>

      <div className="grid grid-cols-7 gap-1 mb-2">
        {dayNames.map((day, index) => (
          <div key={index} className="text-center text-gray-400 text-sm py-1">
            {day}
          </div>
        ))}
      </div>

      <div className="grid grid-cols-7 gap-1">
        {days.map((day, index) => (
          <div
            key={index}
            onClick={() => handleDayClick(day.date)}
            className={`
              text-center py-2 cursor-pointer
              ${!day.currentMonth ? 'text-gray-600' : 'text-white'}
              ${isSelected(day.date) ? 'bg-blue-600 rounded-full' : ''}
              ${isInRange(day.date) ? 'bg-blue-400 text-white rounded-full' : ''}
              ${!isSelected(day.date) && !isInRange(day.date) ? 'hover:bg-gray-700 rounded-full' : ''}
            `}
          >
            {day.day}
          </div>
        ))}
      </div>

      {startDate && endDate && (
        <button
          onClick={() => onDateRangeSelect(startDate, endDate)}
          className="mt-4 w-full bg-blue-600 text-white py-1 rounded"
        >
          Apply Filter
        </button>
      )}
    </div>
  );
};

export default CustomDatePicker;
