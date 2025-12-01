'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useTranslations } from 'next-intl';
import {
  Home,
  Users,
  FileText,
  BarChart3,
  Shield,
  Menu,
  X,
  ChevronDown,
  User,
  LogOut,
  Settings
} from 'lucide-react';
import LanguageSwitcher from './LanguageSwitcher';

interface NavigationItem {
  label: string;
  href: string;
  icon: React.ComponentType<{ className?: string }>;
  children?: NavigationItem[];
}


interface NavigationProps {
  className?: string;
  mobileMenuOpen?: boolean;
  onMobileMenuToggle?: () => void;
}

export default function Navigation({
  className = '',
  mobileMenuOpen = false,
  onMobileMenuToggle,
}: NavigationProps) {
  const pathname = usePathname();
  const t = useTranslations();
  const [openDropdowns, setOpenDropdowns] = useState<Set<string>>(new Set());

  const navigationItems: NavigationItem[] = [
    {
      label: t('navigation.home'),
      href: '/',
      icon: Home,
    },
    {
      label: t('navigation.contacts'),
      href: '/contacts',
      icon: Users,
      children: [
        { label: t('navigation.contacts_all'), href: '/contacts', icon: Users },
        { label: t('navigation.contacts_new'), href: '/contacts/new', icon: Users },
      ],
    },
    {
      label: t('navigation.interactions'),
      href: '/interactions',
      icon: FileText,
      children: [
        { label: t('navigation.interactions_all'), href: '/interactions', icon: FileText },
        { label: t('navigation.interactions_new'), href: '/interactions/new', icon: FileText },
      ],
    },
    {
      label: t('navigation.blocks'),
      href: '/blocks',
      icon: Shield,
    },
    {
      label: t('navigation.users'),
      href: '/users',
      icon: User,
    },
    {
      label: t('navigation.analytics'),
      href: '/analytics',
      icon: BarChart3,
    },
    {
      label: t('navigation.audit'),
      href: '/audit-log',
      icon: FileText,
    },
  ];

  const toggleDropdown = (label: string) => {
    const newOpenDropdowns = new Set(openDropdowns);
    if (newOpenDropdowns.has(label)) {
      newOpenDropdowns.delete(label);
    } else {
      // Закрываем все другие dropdown перед открытием нового
      newOpenDropdowns.clear();
      newOpenDropdowns.add(label);
    }
    setOpenDropdowns(newOpenDropdowns);
  };

  const isActive = (href: string) => {
    if (href === '/') {
      return pathname === '/';
    }
    return pathname?.startsWith(href);
  };

  const isDropdownOpen = (label: string) => openDropdowns.has(label);

  return (
    <nav className={`bg-white shadow-sm border-r border-slate-200 ${className}`}>
      <div className="px-4 py-6">
        {/* Logo/Brand */}
        <div className="flex items-center justify-between mb-8">
          <div className="flex items-center">
            <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
              <span className="text-white font-bold text-sm">K</span>
            </div>
            <span className="ml-2 text-xl font-bold text-slate-900">Kurator</span>
          </div>

          {/* Language Switcher */}
          <LanguageSwitcher />
        </div>

        {/* Navigation Items */}
        <ul className="space-y-2">
          {navigationItems.map((item) => {
            const Icon = item.icon;
            const active = isActive(item.href);
            const hasChildren = item.children && item.children.length > 0;
            const dropdownOpen = isDropdownOpen(item.label);

            return (
              <li key={item.label}>
                {hasChildren ? (
                  <div>
                  <button
                    onClick={() => toggleDropdown(item.label)}
                    className={`w-full flex items-center px-3 py-2 text-sm font-medium rounded-lg transition-colors ${
                      active
                        ? 'bg-blue-50 text-blue-700'
                        : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900'
                    }`}
                    aria-expanded={dropdownOpen}
                    aria-controls={`${item.label.toLowerCase()}-dropdown`}
                  >
                      <Icon className="w-5 h-5 mr-3" />
                      <span className="flex-1 text-left">{item.label}</span>
                      <ChevronDown
                        className={`w-4 h-4 transition-transform ${
                          dropdownOpen ? 'transform rotate-180' : ''
                        }`}
                      />
                    </button>

                    {dropdownOpen && (
                      <ul className="mt-2 ml-8 space-y-1">
                        {item.children!.map((child) => {
                          const ChildIcon = child.icon;
                          const childActive = isActive(child.href);

                          return (
                            <li key={child.href}>
                              <Link
                                href={child.href}
                                className={`flex items-center px-3 py-2 text-sm font-medium rounded-lg transition-colors ${
                                  childActive
                                    ? 'bg-blue-50 text-blue-700'
                                    : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900'
                                }`}
                              >
                                <ChildIcon className="w-4 h-4 mr-3" />
                                {child.label}
                              </Link>
                            </li>
                          );
                        })}
                      </ul>
                    )}
                  </div>
                ) : (
                  <Link
                    href={item.href}
                    className={`flex items-center px-3 py-2 text-sm font-medium rounded-lg transition-colors ${
                      active
                        ? 'bg-blue-50 text-blue-700'
                        : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900'
                    }`}
                  >
                    <Icon className="w-5 h-5 mr-3" />
                    {item.label}
                  </Link>
                )}
              </li>
            );
          })}
        </ul>
      </div>
    </nav>
  );
}

// Mobile Navigation Component
interface MobileNavigationProps {
  isOpen: boolean;
  onClose: () => void;
  user?: {
    name: string;
    email: string;
    role: string;
  };
  onLogout?: () => void;
}

export function MobileNavigation({ isOpen, onClose, user, onLogout }: MobileNavigationProps) {
  const pathname = usePathname();
  const t = useTranslations();
  const [openDropdowns, setOpenDropdowns] = useState<Set<string>>(new Set());

  const navigationItems: NavigationItem[] = [
    {
      label: t('navigation.home'),
      href: '/',
      icon: Home,
    },
    {
      label: t('navigation.contacts'),
      href: '/contacts',
      icon: Users,
      children: [
        { label: t('navigation.contacts_all'), href: '/contacts', icon: Users },
        { label: t('navigation.contacts_new'), href: '/contacts/new', icon: Users },
      ],
    },
    {
      label: t('navigation.interactions'),
      href: '/interactions',
      icon: FileText,
      children: [
        { label: t('navigation.interactions_all'), href: '/interactions', icon: FileText },
        { label: t('navigation.interactions_new'), href: '/interactions/new', icon: FileText },
      ],
    },
    {
      label: t('navigation.blocks'),
      href: '/blocks',
      icon: Shield,
    },
    {
      label: t('navigation.users'),
      href: '/users',
      icon: User,
    },
    {
      label: t('navigation.analytics'),
      href: '/analytics',
      icon: BarChart3,
    },
    {
      label: t('navigation.audit'),
      href: '/audit-log',
      icon: FileText,
    },
  ];

  const toggleDropdown = (label: string) => {
    const newOpenDropdowns = new Set(openDropdowns);
    if (newOpenDropdowns.has(label)) {
      newOpenDropdowns.delete(label);
    } else {
      // Закрываем все другие dropdown перед открытием нового
      newOpenDropdowns.clear();
      newOpenDropdowns.add(label);
    }
    setOpenDropdowns(newOpenDropdowns);
  };

  const isActive = (href: string) => {
    if (href === '/') {
      return pathname === '/';
    }
    return pathname?.startsWith(href);
  };

  const isDropdownOpen = (label: string) => openDropdowns.has(label);

  const handleLinkClick = () => {
    onClose();
    setOpenDropdowns(new Set()); // Close all dropdowns
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 lg:hidden">
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-black bg-opacity-50"
        onClick={onClose}
        data-testid="mobile-nav-backdrop"
      />

      {/* Mobile menu */}
      <div className="fixed inset-y-0 left-0 w-80 bg-white shadow-xl flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-gray-200">
          <div className="flex items-center">
            <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
              <span className="text-white font-bold text-sm">K</span>
            </div>
            <span className="ml-2 text-xl font-bold text-slate-900">Kurator</span>
          </div>
          <button
            onClick={onClose}
            className="p-2 hover:bg-slate-100 rounded-lg transition-colors"
            aria-label="Закрыть мобильное меню"
          >
                <X className="w-5 h-5 text-slate-400" />
          </button>
        </div>

        {/* User info */}
        {user && (
          <div className="px-4 py-3 border-b border-gray-200">
            <div className="flex items-center">
              <div className="w-10 h-10 bg-blue-600 rounded-full flex items-center justify-center">
                <User className="w-5 h-5 text-white" />
              </div>
              <div className="ml-3">
                <div className="text-sm font-medium text-slate-900">{user.name}</div>
                <div className="text-xs text-slate-600">{user.email}</div>
                <div className="text-xs text-blue-600 font-medium">{user.role}</div>
              </div>
            </div>
          </div>
        )}

        {/* Navigation */}
        <div className="flex-1 overflow-y-auto px-4 py-6">
          <ul className="space-y-2">
            {navigationItems.map((item) => {
              const Icon = item.icon;
              const active = isActive(item.href);
              const hasChildren = item.children && item.children.length > 0;
              const dropdownOpen = isDropdownOpen(item.label);

              return (
                <li key={item.label}>
                  {hasChildren ? (
                    <div>
                      <button
                        onClick={() => toggleDropdown(item.label)}
                        className={`w-full flex items-center px-3 py-2 text-sm font-medium rounded-lg transition-colors ${
                          active
                            ? 'bg-blue-50 text-blue-700'
                            : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900'
                        }`}
                      >
                        <Icon className="w-5 h-5 mr-3" />
                        <span className="flex-1 text-left">{item.label}</span>
                        <ChevronDown
                          className={`w-4 h-4 transition-transform ${
                            dropdownOpen ? 'transform rotate-180' : ''
                          }`}
                        />
                      </button>

                      {dropdownOpen && (
                        <ul
                          id={`${item.label.toLowerCase()}-dropdown`}
                          className="mt-2 ml-8 space-y-1"
                        >
                          {item.children!.map((child) => {
                            const ChildIcon = child.icon;
                            const childActive = isActive(child.href);

                            return (
                              <li key={child.href}>
                                <Link
                                  href={child.href}
                                  onClick={handleLinkClick}
                                  className={`flex items-center px-3 py-2 text-sm font-medium rounded-lg transition-colors ${
                                    childActive
                                      ? 'bg-blue-50 text-blue-700'
                                      : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900'
                                  }`}
                                >
                                  <ChildIcon className="w-4 h-4 mr-3" />
                                  {child.label}
                                </Link>
                              </li>
                            );
                          })}
                        </ul>
                      )}
                    </div>
                  ) : (
                    <Link
                      href={item.href}
                      onClick={handleLinkClick}
                      className={`flex items-center px-3 py-2 text-sm font-medium rounded-lg transition-colors ${
                        active
                          ? 'bg-blue-50 text-blue-700'
                          : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900'
                      }`}
                    >
                      <Icon className="w-5 h-5 mr-3" />
                      {item.label}
                    </Link>
                  )}
                </li>
              );
            })}
          </ul>
        </div>

        {/* Footer actions */}
        <div className="border-t border-gray-200 p-4 space-y-2">
          <Link
            href="/settings"
            onClick={handleLinkClick}
            className="flex items-center px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-50 hover:text-slate-900 rounded-lg transition-colors"
          >
            <Settings className="w-5 h-5 mr-3" />
            {t('common.settings')}
          </Link>
          <button
            onClick={() => {
              onLogout?.();
              onClose();
            }}
            className="w-full flex items-center px-3 py-2 text-sm font-medium text-red-600 hover:bg-red-50 hover:text-red-700 rounded-lg transition-colors"
          >
            <LogOut className="w-5 h-5 mr-3" />
            {t('common.logout')}
          </button>
        </div>
      </div>
    </div>
  );
}

// Breadcrumb Component
interface BreadcrumbItem {
  label: string;
  href?: string;
}

interface BreadcrumbProps {
  items: BreadcrumbItem[];
  className?: string;
}

export function Breadcrumb({ items, className = '' }: BreadcrumbProps) {
  return (
    <nav className={`flex ${className}`} aria-label="Breadcrumb">
      <ol className="flex items-center space-x-2">
        {items.map((item, index) => {
          const isLast = index === items.length - 1;

          return (
            <li key={index} className="flex items-center">
              {index > 0 && (
                <svg
                  className="w-4 h-4 text-slate-400 mx-2"
                  fill="currentColor"
                  viewBox="0 0 20 20"
                  data-testid="breadcrumb-separator"
                >
                  <path
                    fillRule="evenodd"
                    d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z"
                    clipRule="evenodd"
                  />
                </svg>
              )}

              {item.href && !isLast ? (
                <Link
                  href={item.href}
                  className="text-sm font-medium text-slate-600 hover:text-slate-800 transition-colors"
                >
                  {item.label}
                </Link>
              ) : (
                <span
                  className={`text-sm font-medium ${
                    isLast ? 'text-slate-900' : 'text-slate-600'
                  }`}
                >
                  {item.label}
                </span>
              )}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
