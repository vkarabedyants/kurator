'use client';

import React, { useState, useEffect } from 'react';
import { useRouter, useParams } from 'next/navigation';
import { ArrowLeft, Save, Calendar, Users, MessageSquare, FileText, AlertCircle, Loader2 } from 'lucide-react';
import { api } from '@/services/api';
import { UpdateInteractionDto, InteractionDto, ContactDto, InteractionType, InteractionResult, InfluenceStatus } from '@/types/api';

export default function EditInteractionPage() {
  const router = useRouter();
  const params = useParams();
  const interactionId = params.id as string;

  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [interaction, setInteraction] = useState<InteractionDto | null>(null);
  const [contact, setContact] = useState<ContactDto | null>(null);
  const [formData, setFormData] = useState<UpdateInteractionDto>({
    interactionDate: '',
    interactionType: InteractionType.Meeting,
    result: InteractionResult.Positive,
    comment: '',
    nextTouchDate: '',
    influenceStatusChange: null
  });

  useEffect(() => {
    loadInteraction();
  }, [interactionId]);

  const loadInteraction = async () => {
    try {
      setIsLoading(true);
      const response = await api.get(`/interactions/${interactionId}`);
      if (response.data) {
        setInteraction(response.data);

        // Load contact details
        const contactResponse = await api.get(`/contacts/${response.data.contactId}`);
        if (contactResponse.data) {
          setContact(contactResponse.data);
        }

        // Set form data
        setFormData({
          interactionDate: response.data.interactionDate.split('T')[0],
          interactionType: response.data.interactionType,
          result: response.data.result,
          comment: response.data.comment || '',
          nextTouchDate: response.data.nextTouchDate ? response.data.nextTouchDate.split('T')[0] : '',
          influenceStatusChange: response.data.influenceStatusChange || null
        });
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Не удалось загрузить взаимодействие');
    } finally {
      setIsLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSaving(true);
    setError(null);

    try {
      await api.put(`/interactions/${interactionId}`, formData);

      // Redirect back to the contact page if available, otherwise to interactions list
      if (interaction?.contactId) {
        router.push(`/contacts/${interaction.contactId}`);
      } else {
        router.push('/interactions');
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Не удалось обновить взаимодействие');
    } finally {
      setIsSaving(false);
    }
  };

  const handleChange = (field: keyof UpdateInteractionDto, value: any) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="flex items-center gap-2">
          <Loader2 className="h-6 w-6 animate-spin text-blue-600" />
          <span className="text-gray-600">Загрузка взаимодействия...</span>
        </div>
      </div>
    );
  }

  if (!interaction) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <h2 className="text-2xl font-bold text-gray-900 mb-2">Взаимодействие не найдено</h2>
          <button
            onClick={() => router.push('/interactions')}
            className="text-blue-600 hover:text-blue-800"
          >
            Вернуться к взаимодействиям
          </button>
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
            Назад
          </button>
          <h1 className="text-3xl font-bold text-gray-900">Редактировать взаимодействие</h1>
          <p className="text-gray-600 mt-2">Редактирование взаимодействия #{interaction.id}</p>
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
          {/* Contact Information (Read-only) */}
          <div className="space-y-4">
            <h2 className="text-lg font-semibold text-gray-900 border-b pb-2">Информация о контакте</h2>

            {contact && (
              <div className="p-4 bg-gray-50 rounded-lg">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
                  <div>
                    <span className="font-medium text-gray-700">ID контакта:</span>
                    <span className="ml-2 text-gray-900">{contact.contactId}</span>
                  </div>
                  <div>
                    <span className="font-medium text-gray-700">Имя:</span>
                    <span className="ml-2 text-gray-900">{contact.fullName}</span>
                  </div>
                  <div>
                    <span className="font-medium text-gray-700">Организация:</span>
                    <span className="ml-2 text-gray-900">{contact.organization}</span>
                  </div>
                  <div>
                    <span className="font-medium text-gray-700">Текущий статус:</span>
                    <span className="ml-2 text-gray-900">{contact.influenceStatus}</span>
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Interaction Details */}
          <div className="space-y-4">
            <h2 className="text-lg font-semibold text-gray-900 border-b pb-2">Детали взаимодействия</h2>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  <Calendar className="inline h-4 w-4 mr-1" />
                  Дата и время *
                </label>
                <input
                  type="datetime-local"
                  required
                  value={formData.interactionDate}
                  onChange={(e) => handleChange('interactionDate', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Тип взаимодействия *
                </label>
                <select
                  required
                  value={formData.interactionType}
                  onChange={(e) => handleChange('interactionType', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value={InteractionType.Meeting}>Встреча</option>
                  <option value={InteractionType.Call}>Звонок</option>
                  <option value={InteractionType.Correspondence}>Переписка</option>
                  <option value={InteractionType.Event}>Событие</option>
                  <option value={InteractionType.Other}>Другое</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Результат *
                </label>
                <select
                  required
                  value={formData.result}
                  onChange={(e) => handleChange('result', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value={InteractionResult.Positive}>Положительный</option>
                  <option value={InteractionResult.Neutral}>Нейтральный</option>
                  <option value={InteractionResult.Negative}>Отрицательный</option>
                  <option value={InteractionResult.Postponed}>Отложено</option>
                  <option value={InteractionResult.NoResult}>Без результата</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Дата следующего контакта
                </label>
                <input
                  type="date"
                  value={formData.nextTouchDate}
                  onChange={(e) => handleChange('nextTouchDate', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
            </div>
          </div>

          {/* Comment */}
          <div className="space-y-4">
            <h2 className="text-lg font-semibold text-gray-900 border-b pb-2">Детали и заметки</h2>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                <MessageSquare className="inline h-4 w-4 mr-1" />
                Комментарий / Детали *
              </label>
              <textarea
                required
                value={formData.comment}
                onChange={(e) => handleChange('comment', e.target.value)}
                rows={4}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Опишите детали взаимодействия, обсуждаемые темы, результаты..."
              />
            </div>
          </div>

          {/* Status Change */}
          {contact && (
            <div className="space-y-4">
              <h2 className="text-lg font-semibold text-gray-900 border-b pb-2">Изменение статуса влияния</h2>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Новый статус влияния (опционально)
                </label>
                <select
                  value={formData.influenceStatusChange || ''}
                  onChange={(e) => handleChange('influenceStatusChange', e.target.value || null)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value="">Без изменений</option>
                  <option value={InfluenceStatus.A}>A - Высокое влияние</option>
                  <option value={InfluenceStatus.B}>B - Среднее влияние</option>
                  <option value={InfluenceStatus.C}>C - Низкое влияние</option>
                  <option value={InfluenceStatus.D}>D - Без влияния</option>
                </select>
                {formData.influenceStatusChange && formData.influenceStatusChange !== contact.influenceStatus && (
                  <p className="text-sm text-amber-600 mt-1">
                    Статус изменится с {contact.influenceStatus} на {formData.influenceStatusChange}
                  </p>
                )}
              </div>
            </div>
          )}

          {/* Metadata */}
          <div className="bg-gray-50 rounded-lg p-4 text-sm text-gray-600 space-y-1">
            <div>Создано: {new Date(interaction.createdAt).toLocaleString()}</div>
            <div>Создано пользователем: {interaction.curatorName}</div>
            {interaction.updatedAt && (
              <>
                <div>Последнее обновление: {new Date(interaction.updatedAt).toLocaleString()}</div>
                {interaction.updatedBy && <div>Обновлено пользователем: {interaction.updatedBy}</div>}
              </>
            )}
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
              disabled={isSaving}
              className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50 flex items-center gap-2"
            >
              <Save className="h-4 w-4" />
              {isSaving ? 'Сохранение...' : 'Сохранить изменения'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}