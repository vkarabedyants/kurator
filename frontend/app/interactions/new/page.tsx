'use client';

import React, { useState, useEffect, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { ArrowLeft, Save, Calendar, Users, MessageSquare, FileText, AlertCircle, Loader2 } from 'lucide-react';
import { api } from '@/services/api';
import { CreateInteractionDto, ContactDto, InteractionType, InteractionResult, InfluenceStatus } from '@/types/api';

function NewInteractionForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const contactIdParam = searchParams.get('contactId');

  const [isLoading, setIsLoading] = useState(false);
  const [isLoadingContacts, setIsLoadingContacts] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [contacts, setContacts] = useState<ContactDto[]>([]);
  const [selectedContact, setSelectedContact] = useState<ContactDto | null>(null);
  const [formData, setFormData] = useState<CreateInteractionDto>({
    contactId: contactIdParam ? parseInt(contactIdParam) : 0,
    interactionDate: new Date().toISOString().split('T')[0],
    interactionType: InteractionType.Meeting,
    result: InteractionResult.Positive,
    comment: '',
    nextTouchDate: '',
    influenceStatusChange: null
  });

  useEffect(() => {
    loadContacts();
  }, []);

  useEffect(() => {
    if (contactIdParam && contacts.length > 0) {
      const contact = contacts.find(c => c.id === parseInt(contactIdParam));
      if (contact) {
        setSelectedContact(contact);
        setFormData(prev => ({ ...prev, contactId: contact.id }));
      }
    }
  }, [contactIdParam, contacts]);

  const loadContacts = async () => {
    try {
      setIsLoadingContacts(true);
      const response = await api.get('/contacts');
      if (response.data?.items) {
        setContacts(response.data.items);
      }
    } catch (err: any) {
      setError('Не удалось загрузить контакты');
    } finally {
      setIsLoadingContacts(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!formData.contactId) {
      setError('Пожалуйста, выберите контакт');
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const dataToSubmit = {
        ...formData,
        influenceStatusChange: formData.influenceStatusChange || undefined
      };

      await api.post('/interactions', dataToSubmit);

      if (contactIdParam) {
        router.push(`/contacts/${contactIdParam}`);
      } else {
        router.push('/interactions');
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Не удалось создать взаимодействие');
    } finally {
      setIsLoading(false);
    }
  };

  const handleChange = (field: keyof CreateInteractionDto, value: any) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleContactChange = (contactId: string) => {
    const id = parseInt(contactId);
    const contact = contacts.find(c => c.id === id);
    setSelectedContact(contact || null);
    handleChange('contactId', id);
  };

  if (isLoadingContacts) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="flex items-center gap-2">
          <Loader2 className="h-6 w-6 animate-spin text-blue-600" />
          <span className="text-gray-600">Загрузка контактов...</span>
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
          <h1 className="text-3xl font-bold text-gray-900">Создать новое взаимодействие</h1>
          <p className="text-gray-600 mt-2">Записать новое взаимодействие с контактом</p>
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
          {/* Contact Selection */}
          <div className="space-y-4">
            <h2 className="text-lg font-semibold text-gray-900 border-b pb-2">Информация о контакте</h2>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                <Users className="inline h-4 w-4 mr-1" />
                Выберите контакт *
              </label>
              <select
                required
                value={formData.contactId || ''}
                onChange={(e) => handleContactChange(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                disabled={!!contactIdParam}
              >
                <option value="">Выберите контакт...</option>
                {contacts.map((contact) => (
                  <option key={contact.id} value={contact.id}>
                    {contact.contactId} - {contact.fullName} ({contact.organization})
                  </option>
                ))}
              </select>

              {selectedContact && (
                <div className="mt-2 p-3 bg-blue-50 rounded-lg">
                  <div className="text-sm text-blue-900">
                    <div className="font-medium">{selectedContact.fullName}</div>
                    <div>{selectedContact.position} в {selectedContact.organization}</div>
                    <div>Текущий статус: {selectedContact.influenceStatus}</div>
                  </div>
                </div>
              )}
            </div>
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
          {selectedContact && (
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
                {formData.influenceStatusChange && (
                  <p className="text-sm text-amber-600 mt-1">
                    Статус изменится с {selectedContact.influenceStatus} на {formData.influenceStatusChange}
                  </p>
                )}
              </div>
            </div>
          )}

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
              {isLoading ? 'Создание...' : 'Создать взаимодействие'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default function NewInteractionPage() {
  return (
    <Suspense fallback={
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="flex items-center gap-2">
          <div className="h-6 w-6 border-2 border-blue-600 border-t-transparent rounded-full animate-spin" />
          <span className="text-gray-600">Загрузка...</span>
        </div>
      </div>
    }>
      <NewInteractionForm />
    </Suspense>
  );
}