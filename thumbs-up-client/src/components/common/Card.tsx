/**
 * Card Component
 * Container component with consistent styling
 */

import type { BaseComponentProps } from '../../shared/types';

interface CardProps extends BaseComponentProps {
  title?: string;
  subtitle?: string;
  footer?: React.ReactNode;
  padding?: 'none' | 'small' | 'medium' | 'large';
}

export const Card: React.FC<CardProps> = ({
  children,
  title,
  subtitle,
  footer,
  padding = 'medium',
  className = '',
}) => {
  const paddingClasses = {
    none: '',
    small: 'p-3',
    medium: 'p-6',
    large: 'p-8',
  };

  return (
    <div className={`bg-white rounded-lg shadow-md ${className}`}>
      {(title || subtitle) && (
        <div className={`border-b border-gray-200 ${paddingClasses[padding]}`}>
          {title && <h3 className="text-lg font-semibold text-gray-900">{title}</h3>}
          {subtitle && <p className="mt-1 text-sm text-gray-500">{subtitle}</p>}
        </div>
      )}
      <div className={paddingClasses[padding]}>{children}</div>
      {footer && (
        <div className={`border-t border-gray-200 ${paddingClasses[padding]} bg-gray-50`}>
          {footer}
        </div>
      )}
    </div>
  );
};
