'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useTranslations } from 'next-intl';
import { authApi } from '@/services/api';
import type { SetupMfaRequest } from '@/types/api';

export default function MfaSetupPage() {
  const [qrCodeUrl, setQrCodeUrl] = useState('');
  const [mfaSecret, setMfaSecret] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [setupData, setSetupData] = useState<any>(null);
  const router = useRouter();
  const t = useTranslations();

  useEffect(() => {
    // Получаем данные из localStorage
    const data = localStorage.getItem('pendingMfaSetup');
    if (!data) {
      router.push('/login');
      return;
    }

    const parsed = JSON.parse(data);
    setSetupData(parsed);

    // Автоматически запускаем настройку MFA
    setupMfa(parsed);
  }, []);

  const setupMfa = async (data: any) => {
    setLoading(true);
    setError('');

    try {
      const response = await authApi.setupMfa({
        userId: data.userId,
        password: data.password,
      } as SetupMfaRequest);

      setQrCodeUrl(response.qrCodeUrl);
      setMfaSecret(response.mfaSecret);
    } catch (err: any) {
      const errorMessage = err.response?.data?.message;
      if (errorMessage?.includes('not found')) {
        setError(t('errors.not_found'));
      } else if (errorMessage?.includes('password')) {
        setError(t('auth.login_error_invalid_credentials'));
      } else {
        setError(errorMessage || t('mfa.setup_error'));
      }
    } finally {
      setLoading(false);
    }
  };

  const handleContinue = () => {
    // Сохраняем userId для верификации
    localStorage.setItem('pendingMfaVerification', JSON.stringify({
      userId: setupData.userId,
      login: setupData.login
    }));
    localStorage.removeItem('pendingMfaSetup');
    router.push('/mfa/verify');
  };

  if (!setupData) {
    return null;
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="max-w-2xl w-full space-y-8 p-8 bg-white rounded-lg shadow-lg">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            {t('auth.mfa_setup_title')}
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            {t('auth.mfa_setup_description')}
          </p>
        </div>

        {loading ? (
          <div className="text-center py-8">
            <div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
            <p className="mt-4 text-gray-600">{t('auth.mfa_setup_loading')}</p>
          </div>
        ) : error ? (
          <div className="bg-red-50 border border-red-400 text-red-700 px-4 py-3 rounded">
            {error}
          </div>
        ) : qrCodeUrl ? (
          <div className="space-y-6">
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
              <h3 className="font-semibold text-blue-900 mb-2">{t('auth.mfa_setup_step1_title')}</h3>
              <p className="text-blue-800 text-sm">
                {t('auth.mfa_setup_step1_description')}
              </p>
            </div>

            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
              <h3 className="font-semibold text-blue-900 mb-4">{t('auth.mfa_setup_step2_title')}</h3>
              <div className="flex justify-center mb-4">
                <img
                  src={`https://api.qrserver.com/v1/create-qr-code/?size=250x250&data=${encodeURIComponent(qrCodeUrl)}`}
                  alt="QR Code"
                  className="border-4 border-white shadow-lg"
                />
              </div>
              <p className="text-blue-800 text-sm text-center">
                {t('auth.mfa_setup_step2_description')}
              </p>
            </div>

            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
              <h3 className="font-semibold text-blue-900 mb-2">{t('auth.mfa_setup_manual_title')}</h3>
              <div className="bg-white p-3 rounded border border-blue-300 font-mono text-sm break-all">
                {mfaSecret}
              </div>
              <p className="text-blue-800 text-xs mt-2">
                {t('auth.mfa_setup_manual_description')}
              </p>
            </div>

            <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
              <p className="text-yellow-800 text-sm">
                {t('auth.mfa_setup_security_note')}
              </p>
            </div>

            <button
              onClick={handleContinue}
              className="w-full flex justify-center py-3 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
            >
              {t('auth.mfa_setup_button')}
            </button>
          </div>
        ) : null}
      </div>
    </div>
  );
}
