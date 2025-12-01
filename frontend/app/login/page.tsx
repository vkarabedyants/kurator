'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useTranslations } from 'next-intl';
import { authApi } from '@/services/api';
import { logger } from '@/lib/logger';
import type { LoginRequest } from '@/types/api';

export default function LoginPage() {
  const [login, setLogin] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const router = useRouter();
  const t = useTranslations();

  // Log page load
  useEffect(() => {
    logger.navigation('unknown', '/login', { component: 'LoginPage' });
    logger.info('Login page loaded', { component: 'LoginPage' });
  }, []);

  // Автоматически скрывать сообщение об ошибке через 5 секунд
  useEffect(() => {
    if (error) {
      const timer = setTimeout(() => {
        setError('');
      }, 5000); // 5 секунд вместо мгновенного исчезновения

      return () => clearTimeout(timer);
    }
  }, [error]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    const timer = logger.startTimer('login');
    logger.userAction('Login form submitted', { login, component: 'LoginPage' });

    try {
      const response = await authApi.login({
        login,
        password,
      } as LoginRequest);

      // Check if MFA setup is required
      if (response.requireMfaSetup) {
        logger.auth('MFA setup required, redirecting', { login, userId: response.userId });
        localStorage.setItem('pendingMfaSetup', JSON.stringify({
          userId: response.userId,
          login: response.login || login,
          password: password
        }));
        timer();
        router.push('/mfa/setup');
        return;
      }

      // Check if MFA verification is required
      if (response.requireMfaVerification) {
        logger.auth('MFA verification required, redirecting', { login, userId: response.userId });
        localStorage.setItem('pendingMfaVerification', JSON.stringify({
          userId: response.userId,
          login: response.login || login
        }));
        timer();
        router.push('/mfa/verify');
        return;
      }

      // Normal login without MFA (if token exists)
      if (response.token && response.user) {
        logger.auth('Login successful, storing credentials', {
          login,
          userId: response.user.id,
          role: response.user.role,
        });
        localStorage.setItem('token', response.token);
        localStorage.setItem('user', JSON.stringify(response.user));
        timer();
        logger.navigation('/login', '/dashboard', { reason: 'login_success' });
        router.push('/dashboard');
      } else {
        logger.error('Login response missing token or user', { login, response: JSON.stringify(response) });
        setError(t('auth.server_error'));
      }
    } catch (err: any) {
      timer();
      const errorMessage = err.response?.data?.message;
      logger.error('Login failed', {
        login,
        status: err.response?.status,
        errorMessage,
      }, err);

      if (errorMessage?.includes('Invalid credentials')) {
        setError(t('auth.login_error_invalid_credentials'));
      } else if (errorMessage?.includes('locked')) {
        setError(t('auth.login_error_account_locked', { minutes: '15' }));
      } else if (errorMessage?.includes('disabled')) {
        setError(t('auth.login_error_account_disabled'));
      } else {
        setError(errorMessage || t('auth.login_error_generic'));
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="max-w-md w-full space-y-8 p-8 bg-white rounded-lg shadow-lg">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-black">
            {t('auth.title')}
          </h2>
          <p className="mt-2 text-center text-sm text-gray-700">
            {t('auth.subtitle')}
          </p>
        </div>
        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          {error && (
            <div className="bg-red-50 border border-red-400 text-red-700 px-4 py-3 rounded">
              {error}
            </div>
          )}
          <div className="rounded-md shadow-sm -space-y-px">
            <div>
              <label htmlFor="login" className="sr-only">
                {t('common.login')}
              </label>
              <input
                id="login"
                name="login"
                type="text"
                required
                className="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-black rounded-t-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm"
                placeholder={t('auth.login_placeholder')}
                value={login}
                onChange={(e) => setLogin(e.target.value)}
              />
            </div>
            <div>
              <label htmlFor="password" className="sr-only">
                {t('common.password')}
              </label>
              <input
                id="password"
                name="password"
                type="password"
                required
                className="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-black rounded-b-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm"
                placeholder={t('auth.password_placeholder')}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
            </div>
          </div>

          <div>
            <button
              type="submit"
              disabled={loading}
              className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50"
            >
              {loading ? t('auth.login_button_loading') : t('auth.login_button')}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
