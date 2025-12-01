'use client';

import { useState, useEffect, useRef } from 'react';
import { useRouter } from 'next/navigation';
import { useTranslations } from 'next-intl';
import { authApi } from '@/services/api';
import type { VerifyMfaRequest } from '@/types/api';

export default function MfaVerifyPage() {
  const [code, setCode] = useState(['', '', '', '', '', '']);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [verificationData, setVerificationData] = useState<any>(null);
  const inputRefs = useRef<(HTMLInputElement | null)[]>([]);
  const router = useRouter();
  const t = useTranslations();

  useEffect(() => {
    // Получаем данные из localStorage
    const data = localStorage.getItem('pendingMfaVerification');
    if (!data) {
      router.push('/login');
      return;
    }

    setVerificationData(JSON.parse(data));

    // Фокус на первом поле
    inputRefs.current[0]?.focus();
  }, []);

  const handleCodeChange = (index: number, value: string) => {
    // Разрешаем только цифры
    if (value && !/^\d$/.test(value)) {
      return;
    }

    const newCode = [...code];
    newCode[index] = value;
    setCode(newCode);
    setError('');

    // Автоматический переход к следующему полю
    if (value && index < 5) {
      inputRefs.current[index + 1]?.focus();
    }

    // Автоматическая отправка при заполнении всех полей
    if (newCode.every(digit => digit !== '') && index === 5) {
      handleVerify(newCode.join(''));
    }
  };

  const handleKeyDown = (index: number, e: React.KeyboardEvent) => {
    // Backspace - переход к предыдущему полю
    if (e.key === 'Backspace' && !code[index] && index > 0) {
      inputRefs.current[index - 1]?.focus();
    }
  };

  const handlePaste = (e: React.ClipboardEvent) => {
    e.preventDefault();
    const pastedData = e.clipboardData.getData('text').replace(/\D/g, '').slice(0, 6);

    if (pastedData.length === 6) {
      const newCode = pastedData.split('');
      setCode(newCode);
      handleVerify(pastedData);
    }
  };

  const handleVerify = async (totpCode: string) => {
    if (!verificationData) return;

    setLoading(true);
    setError('');

    try {
      const response = await authApi.verifyMfa({
        userId: verificationData.userId,
        totpCode: totpCode,
      } as VerifyMfaRequest);

      // Сохраняем токен и данные пользователя
      localStorage.setItem('token', response.token);
      localStorage.setItem('user', JSON.stringify(response.user));
      localStorage.removeItem('pendingMfaVerification');
      localStorage.removeItem('pendingMfaSetup');

      // Переход на дашборд
      router.push('/dashboard');
    } catch (err: any) {
      const errorMessage = err.response?.data?.message;
      if (errorMessage?.includes('Invalid MFA code')) {
        setError(t('auth.mfa_verify_error'));
      } else if (errorMessage?.includes('not found')) {
        setError(t('errors.not_found'));
      } else {
        setError(errorMessage || t('auth.mfa_verify_error'));
      }
      setCode(['', '', '', '', '', '']);
      inputRefs.current[0]?.focus();
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const totpCode = code.join('');

    if (totpCode.length !== 6) {
      setError('Введите 6-значный код');
      return;
    }

    handleVerify(totpCode);
  };

  if (!verificationData) {
    return null;
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="max-w-md w-full space-y-8 p-8 bg-white rounded-lg shadow-lg">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            {t('auth.mfa_verify_title')}
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            {t('auth.mfa_verify_description')}
          </p>
          {verificationData.login && (
            <p className="mt-1 text-center text-xs text-gray-500">
              {t('auth.mfa_verify_user', { user: verificationData.login })}
            </p>
          )}
        </div>

        <form onSubmit={handleSubmit} className="mt-8 space-y-6">
          {error && (
            <div className="bg-red-50 border border-red-400 text-red-700 px-4 py-3 rounded text-center">
              {error}
            </div>
          )}

          <div className="flex justify-center gap-2">
            {code.map((digit, index) => (
              <input
                key={index}
                ref={(el) => {
                  inputRefs.current[index] = el;
                }}
                type="text"
                inputMode="numeric"
                pattern="[0-9]*"
                maxLength={1}
                value={digit}
                onChange={(e) => handleCodeChange(index, e.target.value)}
                onKeyDown={(e) => handleKeyDown(index, e)}
                onPaste={index === 0 ? handlePaste : undefined}
                disabled={loading}
                placeholder={index === 0 ? t('auth.mfa_verify_code_placeholder') : ''}
                className="w-12 h-14 text-center text-2xl font-bold border-2 border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 disabled:bg-gray-100 disabled:cursor-not-allowed"
              />
            ))}
          </div>

          <div className="space-y-3">
            <button
              type="submit"
              disabled={loading || code.some(digit => digit === '')}
              className="w-full flex justify-center py-3 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {loading ? t('auth.mfa_verify_loading') : t('auth.mfa_verify_button')}
            </button>

            <button
              type="button"
              onClick={() => {
                localStorage.removeItem('pendingMfaVerification');
                localStorage.removeItem('pendingMfaSetup');
                router.push('/login');
              }}
              className="w-full flex justify-center py-2 px-4 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
            >
              {t('auth.mfa_verify_back_to_login')}
            </button>
          </div>

          <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
            <p className="text-blue-800 text-sm">
              <strong>{t('auth.mfa_verify_tip_title')}:</strong> {t('auth.mfa_verify_tip_description')}
            </p>
          </div>
        </form>
      </div>
    </div>
  );
}
