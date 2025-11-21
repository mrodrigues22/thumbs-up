/**
 * Textarea Component
 * Reusable textarea with label and error handling
 */

import type { FormFieldProps } from '../../shared/types';

interface TextareaProps extends FormFieldProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  disabled?: boolean;
  rows?: number;
}

export const Textarea: React.FC<TextareaProps> = ({
  label,
  name,
  value,
  onChange,
  placeholder,
  required = false,
  error,
  helperText,
  disabled = false,
  rows = 4,
}) => {
  return (
    <div className="mb-4">
      <label htmlFor={name} className="block text-sm font-medium text-gray-700 mb-1">
        {label}
        {required && <span className="text-red-600 ml-1">*</span>}
      </label>
      <textarea
        id={name}
        name={name}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        required={required}
        disabled={disabled}
        rows={rows}
        className={`input-field ${error ? 'border-red-500 focus:ring-red-500' : ''}`}
      />
      {helperText && !error && (
        <p className="mt-1 text-sm text-gray-500">{helperText}</p>
      )}
      {error && (
        <p className="mt-1 text-sm text-red-600">{error}</p>
      )}
    </div>
  );
};
