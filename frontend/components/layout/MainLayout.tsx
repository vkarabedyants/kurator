'use client';

import { useState, useEffect } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import Link from 'next/link';
import { UserRole } from '@/types/api';

interface NavItem {
  label: string;
  href: string;
  roles?: UserRole[];
  icon?: string;
}

const navItems: NavItem[] = [
  { label: 'Панель управления', href: '/dashboard', icon: '📊' },
  { label: 'Контакты', href: '/contacts', icon: '👥', roles: [UserRole.Admin, UserRole.Curator, UserRole.BackupCurator] },
  { label: 'Взаимодействия', href: '/interactions', icon: '🤝', roles: [UserRole.Admin, UserRole.Curator, UserRole.BackupCurator] },
  { label: 'Блоки', href: '/blocks', icon: '📦', roles: [UserRole.Admin] },
  { label: 'Пользователи', href: '/users', icon: '👤', roles: [UserRole.Admin] },
  { label: 'Список наблюдения', href: '/watchlist', icon: '⚠️', roles: [UserRole.Admin, UserRole.ThreatAnalyst] },
  { label: 'Аналитика', href: '/analytics', icon: '📈', roles: [UserRole.Admin] },
  { label: 'Журнал аудита', href: '/audit-log', icon: '📋', roles: [UserRole.Admin] },
  { label: 'ЧаВо', href: '/faq', icon: '❓' },
  { label: 'Справочники', href: '/references', icon: '⚙️', roles: [UserRole.Admin] },
];

export default function MainLayout({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<any>(null);
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    const userStr = localStorage.getItem('user');
    if (!userStr) {
      router.push('/login');
      return;
    }
    setUser(JSON.parse(userStr));
  }, [router]);

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    router.push('/login');
  };

  if (!user) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
      </div>
    );
  }

  const filteredNavItems = navItems.filter(item => {
    if (!item.roles) return true;
    return item.roles.includes(user.role as UserRole);
  });

  return (
    <div className="min-h-screen bg-gray-100">
      {/* Top Navigation */}
      <nav className="bg-white shadow-sm border-b border-gray-200 fixed top-0 w-full z-10">
        <div className="px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center">
              <button
                onClick={() => setSidebarOpen(!sidebarOpen)}
                className="p-2 rounded-md text-gray-400 hover:text-gray-500 hover:bg-gray-100"
              >
                <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
                </svg>
              </button>
              <h1 className="ml-4 text-2xl font-bold text-indigo-600">КУРАТОР</h1>
            </div>
            <div className="flex items-center space-x-4">
              <span className="text-gray-700">
                {user.login} ({user.role})
              </span>
              <button
                onClick={handleLogout}
                className="bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-md text-sm font-medium"
              >
                Выход
              </button>
            </div>
          </div>
        </div>
      </nav>

      <div className="flex pt-16">
        {/* Sidebar */}
        <aside
          className={`${
            sidebarOpen ? 'w-64' : 'w-16'
          } bg-white shadow-lg transition-all duration-300 fixed left-0 h-full overflow-y-auto`}
        >
          <nav className="mt-5">
            <div className="px-2">
              {filteredNavItems.map((item) => {
                const isActive = pathname === item.href;
                return (
                  <Link
                    key={item.href}
                    href={item.href}
                    className={`
                      group flex items-center px-2 py-2 text-sm font-medium rounded-md mb-1
                      ${isActive
                        ? 'bg-indigo-100 text-indigo-700'
                        : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900'
                      }
                    `}
                  >
                    {item.icon && (
                      <span className="mr-3 text-lg" role="img" aria-label={item.label}>
                        {item.icon}
                      </span>
                    )}
                    {sidebarOpen && <span>{item.label}</span>}
                  </Link>
                );
              })}
            </div>
          </nav>
        </aside>

        {/* Main Content */}
        <main className={`flex-1 ${sidebarOpen ? 'ml-64' : 'ml-16'} transition-all duration-300`}>
          <div className="p-6">
            {children}
          </div>
        </main>
      </div>
    </div>
  );
}