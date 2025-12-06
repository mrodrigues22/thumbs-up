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
  onClick?: () => void;
}

export const Card: React.FC<CardProps> = ({
  children,
  title,
  subtitle,
  footer,
  padding = 'medium',
  className = '',
  onClick,
}) => {
  const paddingClasses = {
    none: '',
    small: 'p-3',
    medium: 'p-6',
    large: 'p-8',
  };

  return (
    <div 
      className={`bg-white dark:bg-gray-800 rounded-lg shadow-md ${className}`}
      onClick={onClick}
      role={onClick ? 'button' : undefined}
      tabIndex={onClick ? 0 : undefined}
      onKeyDown={onClick ? (e) => e.key === 'Enter' && onClick() : undefined}
    >
      {(title || subtitle) && (
        <div className={`border-b border-gray-200 dark:border-gray-700 ${paddingClasses[padding]}`}>
          {title && <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">{title}</h3>}
          {subtitle && <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">{subtitle}</p>}
        </div>
      )}
      <div className={paddingClasses[padding]}>{children}</div>
      {footer && (
        <div className={`border-t border-gray-200 dark:border-gray-700 ${paddingClasses[padding]} bg-gray-50 dark:bg-gray-700`}>
          {footer}
        </div>
      )}
    </div>
  );
};
