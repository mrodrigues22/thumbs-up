/**
 * Layout Component
 * Main layout wrapper with navbar and content area
 */

import { Navbar } from './Navbar';
import type { BaseComponentProps } from '../../shared/types';

interface LayoutProps extends BaseComponentProps {
  showNavbar?: boolean;
}

export const Layout: React.FC<LayoutProps> = ({
  children,
  showNavbar = true,
  className = '',
}) => {
  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 transition-colors">
      {showNavbar && <Navbar />}
      <main className={`${className}`}>{children}</main>
    </div>
  );
};
