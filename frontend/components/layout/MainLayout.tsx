'use client';

import { useState, useEffect } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { useTranslations } from 'next-intl';
import Navigation, { MobileNavigation, Breadcrumb } from '@/components/ui/Navigation';
import { UserRole } from '@/types/api';

// Generate breadcrumb items based on current path
function generateBreadcrumbs(pathname: string, t: (key: string) => string): Array<{ label: string; href?: string }> {
  const pathSegments = pathname.split('/').filter(Boolean);
  const breadcrumbs = [{ label: t('navigation.home'), href: '/' }];

  if (pathSegments.length === 0) return breadcrumbs;

  const pathMap: Record<string, string> = {
    contacts: t('navigation.contacts'),
    interactions: t('navigation.interactions'),
    blocks: t('navigation.blocks'),
    users: t('navigation.users'),
    analytics: t('navigation.analytics'),
    'audit-log': t('navigation.audit'),
    dashboard: t('dashboard.title'),
    settings: t('common.settings'),
  };

  let currentPath = '';
  pathSegments.forEach((segment, index) => {
    currentPath += `/${segment}`;
    const label = pathMap[segment] || segment;

    if (index === pathSegments.length - 1) {
      // Last item is not a link
      breadcrumbs.push({ label, href: '' });
    } else {
      breadcrumbs.push({ label, href: currentPath });
    }
  });

  return breadcrumbs;
}

export default function MainLayout({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<{ login?: string; role?: string } | null>(() => {
    // Initialize from localStorage on first render (client-side only)
    if (typeof window !== 'undefined') {
      const userStr = localStorage.getItem('user');
      return userStr ? JSON.parse(userStr) : null;
    }
    return null;
  });
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const router = useRouter();
  const pathname = usePathname();
  const t = useTranslations();

  useEffect(() => {
    // Check authentication and redirect if needed
    if (!user && typeof window !== 'undefined') {
      const userStr = localStorage.getItem('user');
      if (!userStr) {
        router.push('/login');
      }
    }
  }, [user, router]);

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    router.push('/login');
  };

  const handleMobileMenuToggle = () => {
    setMobileMenuOpen(!mobileMenuOpen);
  };

  const handleMobileMenuClose = () => {
    setMobileMenuOpen(false);
  };

  if (!user) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  const breadcrumbs = generateBreadcrumbs(pathname, t);

  return (
    <div className="min-h-screen bg-slate-50">
      {/* Top Navigation */}
      <nav className="bg-white shadow-sm border-b border-slate-200 fixed top-0 w-full z-20">
        <div className="px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center">
              {/* Mobile menu button */}
              <button
                onClick={handleMobileMenuToggle}
                className="lg:hidden p-2 rounded-md text-slate-400 hover:text-slate-600 hover:bg-slate-100 mr-2"
                aria-label={t('common.open_mobile_menu') || 'Открыть мобильное меню'}
              >
                <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
                </svg>
              </button>

              {/* Desktop sidebar toggle */}
              <button
                onClick={() => setSidebarOpen(!sidebarOpen)}
                className="hidden lg:block p-2 rounded-md text-slate-400 hover:text-slate-600 hover:bg-slate-100"
                aria-label={t('common.toggle_sidebar') || 'Переключить боковую панель'}
              >
                <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
                </svg>
              </button>

              <h1 className="ml-4 text-2xl font-bold text-blue-600">Kurator</h1>
            </div>

            <div className="flex items-center space-x-4">
              {/* User info */}
              <div className="hidden md:flex items-center space-x-2">
                <span className="text-slate-800 text-sm font-medium">
                  {user.login}
                </span>
                <span className="text-xs text-slate-700 bg-slate-100 px-2 py-1 rounded">
                  {user.role}
                </span>
              </div>

              {/* Logout button */}
              <button
                onClick={handleLogout}
                className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium transition-colors"
              >
                {t('common.logout')}
              </button>
            </div>
          </div>
        </div>
      </nav>

      {/* Mobile Navigation */}
      <MobileNavigation
        isOpen={mobileMenuOpen}
        onClose={handleMobileMenuClose}
        user={{
          name: user.login,
          email: user.email || '',
          role: user.role,
        }}
        onLogout={handleLogout}
      />

      <div className="flex pt-16">
        {/* Desktop Sidebar */}
        <aside
          className={`hidden lg:block ${
            sidebarOpen ? 'w-64' : 'w-16'
          } bg-white shadow-sm border-r border-slate-200 fixed left-0 h-full overflow-y-auto transition-all duration-300 z-10`}
        >
          <Navigation />
        </aside>

        {/* Main Content */}
        <main className={`flex-1 ${sidebarOpen ? 'lg:ml-64' : 'lg:ml-16'} transition-all duration-300`}>
          <div className="min-h-screen">
            {/* Breadcrumb Navigation */}
            <div className="bg-white border-b border-slate-200 px-4 sm:px-6 lg:px-8 py-3">
              <Breadcrumb items={breadcrumbs} />
            </div>

            {/* Page Content */}
            <div className="p-4 sm:p-6 lg:p-8">
              {children}
            </div>
          </div>
        </main>
      </div>
    </div>
  );
}