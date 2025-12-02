'use client';

import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { ArrowLeft, Save, User, Building2, Hash, Phone, FileText, AlertCircle } from 'lucide-react';
import { useTranslations } from 'next-intl';
import { api } from '@/services/api';
import { CreateContactRequest } from '@/types/api';

interface Block {
  id: number;
  name: string;
  code: string;
}

interface ReferenceValue {
  id: number;
  category: string;
  code: string;
  value: string;
  description?: string;
  order: number;
  isActive: boolean;
}

interface ReferencesByCategory {
  [category: string]: ReferenceValue[];
}

export default function NewContactPage() {
  const router = useRouter();
  const t = useTranslations('contacts');
  const tCommon = useTranslations('common');
  const [isLoading, setIsLoading] = useState(false);
  const [isLoadingData, setIsLoadingData] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [blocks, setBlocks] = useState<Block[]>([]);
  const [references, setReferences] = useState<ReferencesByCategory>({});

  const [formData, setFormData] = useState<CreateContactRequest>({
    blockId: 0,
    fullName: '',
    organizationId: null,
    position: null,
    influenceStatusId: null,
    influenceTypeId: null,
    usefulnessDescription: null,
    communicationChannelId: null,
    contactSourceId: null,
    nextTouchDate: null,
    notes: null
  });

  useEffect(() => {
    loadInitialData();
  }, []);

  const loadInitialData = async () => {
    try {
      setIsLoadingData(true);

      // Load user's accessible blocks
      const blocksResponse = await api.get<Block[]>('/blocks/my-blocks');
      setBlocks(blocksResponse.data || []);

      // Load reference values
      const referencesResponse = await api.get<ReferencesByCategory>('/references/by-category');
      setReferences(referencesResponse.data || {});

      // Set default blockId if only one block is available
      if (blocksResponse.data && blocksResponse.data.length === 1) {
        setFormData(prev => ({ ...prev, blockId: blocksResponse.data[0].id }));
      }
    } catch (err: any) {
      setError(err.response?.data?.message || t('load_error'));
    } finally {
      setIsLoadingData(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!formData.blockId) {
      setError(t('select_block_error'));
      return;
    }

    if (!formData.fullName.trim()) {
      setError(t('enter_name_error'));
      return;
    }

    setIsLoading(true);
    setError(null);

    console.log('Sending contact creation request with data:', JSON.stringify(formData, null, 2));

    try {
      const response = await api.post('/contacts', formData);
      if (response.data) {
        router.push(`/contacts/${response.data.id}`);
      }
    } catch (err: any) {
      const errorMessage = err.response?.data?.error?.message || err.response?.data?.message || t('create_error');
      setError(errorMessage);
      console.error('Error creating contact:', err.response?.data);
      console.error('Full error:', err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleChange = (field: keyof CreateContactRequest, value: any) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  if (isLoadingData) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600">{tCommon('loading')}</p>
        </div>
      </div>
    );
  }

  if (blocks.length === 0) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="bg-white shadow rounded-lg p-8 max-w-md">
          <div className="text-center">
            <AlertCircle className="h-12 w-12 text-yellow-600 mx-auto mb-4" />
            <h2 className="text-xl font-semibold text-gray-900 mb-2">{t('no_blocks_available')}</h2>
            <p className="text-gray-600 mb-6">{t('no_blocks_message')}</p>
            <button
              onClick={() => router.back()}
              className="px-4 py-2 bg-gray-100 hover:bg-gray-200 rounded-lg transition-colors"
            >
              {tCommon('back')}
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-4xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <button
            onClick={() => router.back()}
            className="inline-flex items-center gap-2 text-gray-600 hover:text-gray-900 mb-4"
          >
            <ArrowLeft className="h-4 w-4" />
            {tCommon('back')}
          </button>
          <h1 className="text-3xl font-bold text-gray-900">{t('create_new')}</h1>
          <p className="text-gray-600 mt-2">{t('create_subtitle')}</p>
        </div>

        {/* Error Alert */}
        {error && (
          <div className="mb-6 bg-red-50 border border-red-200 rounded-lg p-4">
            <div className="flex items-center gap-2">
              <AlertCircle className="h-5 w-5 text-red-600" />
              <p className="text-red-800">{error}</p>
            </div>
          </div>
        )}

        {/* Form */}
        <form onSubmit={handleSubmit} className="bg-white shadow rounded-lg p-6 space-y-6">
          {/* Block Selection */}
          <div className="space-y-4">
            <h2 className="text-lg font-semibold text-gray-900 border-b pb-2">Блок</h2>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Блок *
              </label>
              <select
                required
                value={formData.blockId}
                onChange={(e) => handleChange('blockId', parseInt(e.target.value))}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value={0}>Выберите блок</option>
                {blocks.map(block => (
                  <option key={block.id} value={block.id}>
                    {block.name} ({block.code})
                  </option>
                ))}
              </select>
            </div>
          </div>

          {/* Basic Information */}
          <div className="space-y-4">
            <h2 className="text-lg font-semibold text-gray-900 border-b pb-2">Основная информация</h2>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  <User className="inline h-4 w-4 mr-1" />
                  Полное имя (ФИО) *
                </label>
                <input
                  type="text"
                  required
                  value={formData.fullName}
                  onChange={(e) => handleChange('fullName', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="Введите полное имя"
                />
                <p className="text-xs text-gray-500 mt-1">Будет зашифровано</p>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  <Building2 className="inline h-4 w-4 mr-1" />
                  Организация
                </label>
                <select
                  value={formData.organizationId || ''}
                  onChange={(e) => handleChange('organizationId', e.target.value ? parseInt(e.target.value) : null)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value="">Не выбрано</option>
                  {references.Organization?.map(ref => (
                    <option key={ref.id} value={ref.id}>
                      {ref.value}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  <Hash className="inline h-4 w-4 mr-1" />
                  Должность/Роль
                </label>
                <input
                  type="text"
                  value={formData.position || ''}
                  onChange={(e) => handleChange('position', e.target.value || null)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="Введите должность или роль"
                />
              </div>
            </div>
          </div>

          {/* Influence Details */}
          <div className="space-y-4">
            <h2 className="text-lg font-semibold text-gray-900 border-b pb-2">Детали влияния</h2>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Статус влияния
                </label>
                <select
                  value={formData.influenceStatusId || ''}
                  onChange={(e) => handleChange('influenceStatusId', e.target.value ? parseInt(e.target.value) : null)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value="">Не выбрано</option>
                  {references.InfluenceStatus?.map(ref => (
                    <option key={ref.id} value={ref.id}>
                      {ref.value} {ref.description && `- ${ref.description}`}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Тип влияния
                </label>
                <select
                  value={formData.influenceTypeId || ''}
                  onChange={(e) => handleChange('influenceTypeId', e.target.value ? parseInt(e.target.value) : null)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value="">Не выбрано</option>
                  {references.InfluenceType?.map(ref => (
                    <option key={ref.id} value={ref.id}>
                      {ref.value} {ref.description && `- ${ref.description}`}
                    </option>
                  ))}
                </select>
              </div>

              <div className="md:col-span-2">
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Чем может помочь (полезность)
                </label>
                <textarea
                  value={formData.usefulnessDescription || ''}
                  onChange={(e) => handleChange('usefulnessDescription', e.target.value || null)}
                  rows={3}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="Опишите, чем этот контакт может быть полезен..."
                />
              </div>
            </div>
          </div>

          {/* Communication Details */}
          <div className="space-y-4">
            <h2 className="text-lg font-semibold text-gray-900 border-b pb-2">Детали коммуникации</h2>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  <Phone className="inline h-4 w-4 mr-1" />
                  Канал коммуникации
                </label>
                <select
                  value={formData.communicationChannelId || ''}
                  onChange={(e) => handleChange('communicationChannelId', e.target.value ? parseInt(e.target.value) : null)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value="">Не выбрано</option>
                  {references.CommunicationChannel?.map(ref => (
                    <option key={ref.id} value={ref.id}>
                      {ref.value}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Источник контакта
                </label>
                <select
                  value={formData.contactSourceId || ''}
                  onChange={(e) => handleChange('contactSourceId', e.target.value ? parseInt(e.target.value) : null)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value="">Не выбрано</option>
                  {references.ContactSource?.map(ref => (
                    <option key={ref.id} value={ref.id}>
                      {ref.value}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Дата следующего контакта
                </label>
                <input
                  type="date"
                  value={formData.nextTouchDate || ''}
                  onChange={(e) => handleChange('nextTouchDate', e.target.value || null)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
            </div>
          </div>

          {/* Notes */}
          <div className="space-y-4">
            <h2 className="text-lg font-semibold text-gray-900 border-b pb-2">Дополнительная информация</h2>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                <FileText className="inline h-4 w-4 mr-1" />
                Конфиденциальные заметки
              </label>
              <textarea
                value={formData.notes || ''}
                onChange={(e) => handleChange('notes', e.target.value || null)}
                rows={4}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Введите конфиденциальные заметки об этом контакте..."
              />
              <p className="text-sm text-gray-500 mt-1">Эта информация будет зашифрована</p>
            </div>
          </div>

          {/* Actions */}
          <div className="flex justify-end gap-4 pt-4 border-t">
            <button
              type="button"
              onClick={() => router.back()}
              className="px-4 py-2 text-gray-700 bg-gray-100 hover:bg-gray-200 rounded-lg transition-colors"
            >
              Отмена
            </button>
            <button
              type="submit"
              disabled={isLoading}
              className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50 flex items-center gap-2"
            >
              <Save className="h-4 w-4" />
              {isLoading ? 'Создание...' : 'Создать контакт'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
