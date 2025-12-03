'use client';

import { useState, useEffect, useCallback } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import MainLayout from '@/components/layout/MainLayout';
import { contactsApi, interactionsApi } from '@/services/api';
import { ContactDetail, CreateInteractionRequest, InfluenceStatus } from '@/types/api';

export default function ContactDetailPage() {
  const params = useParams();
  const [contact, setContact] = useState<ContactDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showAddInteraction, setShowAddInteraction] = useState(false);
  const [interactionForm, setInteractionForm] = useState<CreateInteractionRequest>({
    contactId: Number(params.id),
    interactionTypeId: null,
    resultId: null,
    comment: '',
  });

  const fetchContact = useCallback(async () => {
    try {
      setLoading(true);
      const data = await contactsApi.getById(Number(params.id));
      setContact(data);
    } catch (err: unknown) {
      const error = err as { message?: string };
      setError(error.message || 'Не удалось загрузить контакт');
    } finally {
      setLoading(false);
    }
  }, [params.id]);

  useEffect(() => {
    fetchContact();
  }, [fetchContact]);

  const handleAddInteraction = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await interactionsApi.create(interactionForm);
      setShowAddInteraction(false);
      fetchContact(); // Refresh to show new interaction
      setInteractionForm({
        contactId: Number(params.id),
        interactionTypeId: null,
        resultId: null,
        comment: '',
      });
    } catch (err: unknown) {
      const error = err as { message?: string };
      alert('Не удалось добавить взаимодействие: ' + error.message);
    }
  };

  const getStatusBadgeColor = (status: string) => {
    switch (status) {
      case 'A': return 'bg-green-100 text-green-800';
      case 'B': return 'bg-blue-100 text-blue-800';
      case 'C': return 'bg-yellow-100 text-yellow-800';
      case 'D': return 'bg-red-100 text-red-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  };

  const getResultBadgeColor = (result: string) => {
    switch (result) {
      case 'Positive': return 'bg-green-100 text-green-800';
      case 'Neutral': return 'bg-gray-100 text-gray-800';
      case 'Negative': return 'bg-red-100 text-red-800';
      default: return 'bg-yellow-100 text-yellow-800';
    }
  };

  if (loading) {
    return (
      <MainLayout>
        <div className="flex justify-center py-12">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
        </div>
      </MainLayout>
    );
  }

  if (error || !contact) {
    return (
      <MainLayout>
        <div className="text-center py-12 text-red-600">
          Ошибка: {error || 'Контакт не найден'}
        </div>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <div className="space-y-6">
        {/* Header */}
        <div className="bg-white shadow rounded-lg">
          <div className="px-6 py-4 border-b border-gray-200">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-2xl font-bold text-gray-900">{contact.fullName}</h2>
                <p className="mt-1 text-sm text-gray-500">ID контакта: {contact.contactId}</p>
              </div>
              <div className="flex space-x-3">
                <Link
                  href={`/contacts/${contact.id}/edit`}
                  className="bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-md text-sm font-medium"
                >
                  Редактировать контакт
                </Link>
                <Link
                  href="/contacts"
                  className="bg-gray-200 hover:bg-gray-300 text-gray-700 px-4 py-2 rounded-md text-sm font-medium"
                >
                  Назад к списку
                </Link>
              </div>
            </div>
          </div>

          {/* Contact Information */}
          <div className="px-6 py-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <h3 className="text-lg font-semibold text-gray-900 mb-4">Основная информация</h3>
                <dl className="space-y-3">
                  <div>
                    <dt className="text-sm font-medium text-gray-500">Блок</dt>
                    <dd className="mt-1 text-sm text-gray-900">{contact.blockName} ({contact.blockCode})</dd>
                  </div>
                  <div>
                    <dt className="text-sm font-medium text-gray-500">Организация</dt>
                    <dd className="mt-1 text-sm text-gray-900">{contact.organizationId || '-'}</dd>
                  </div>
                  <div>
                    <dt className="text-sm font-medium text-gray-500">Должность</dt>
                    <dd className="mt-1 text-sm text-gray-900">{contact.position || '-'}</dd>
                  </div>
                  <div>
                    <dt className="text-sm font-medium text-gray-500">Ответственный куратор</dt>
                    <dd className="mt-1 text-sm text-gray-900">{contact.responsibleCuratorLogin}</dd>
                  </div>
                </dl>
              </div>

              <div>
                <h3 className="text-lg font-semibold text-gray-900 mb-4">Информация о влиянии</h3>
                <dl className="space-y-3">
                  <div>
                    <dt className="text-sm font-medium text-gray-500">Статус влияния</dt>
                    <dd className="mt-1">
                      <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${getStatusBadgeColor(contact.influenceStatusId)}`}>
                        {contact.influenceStatusId}
                      </span>
                    </dd>
                  </div>
                  <div>
                    <dt className="text-sm font-medium text-gray-500">Тип влияния</dt>
                    <dd className="mt-1 text-sm text-gray-900">{contact.influenceType}</dd>
                  </div>
                  <div>
                    <dt className="text-sm font-medium text-gray-500">Описание полезности</dt>
                    <dd className="mt-1 text-sm text-gray-900">{contact.usefulnessDescription || '-'}</dd>
                  </div>
                  <div>
                    <dt className="text-sm font-medium text-gray-500">Канал коммуникации</dt>
                    <dd className="mt-1 text-sm text-gray-900">{contact.communicationChannelId || '-'}</dd>
                  </div>
                </dl>
              </div>
            </div>

            {/* Dates and Statistics */}
            <div className="mt-6 pt-6 border-t border-gray-200">
              <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                <div>
                  <dt className="text-sm font-medium text-gray-500">Дата следующего контакта</dt>
                  <dd className="mt-1 text-sm text-gray-900">
                    {contact.nextTouchDate ? new Date(contact.nextTouchDate).toLocaleDateString() : '-'}
                    {contact.isOverdue && (
                      <span className="ml-2 text-red-600 font-semibold">Просрочено!</span>
                    )}
                  </dd>
                </div>
                <div>
                  <dt className="text-sm font-medium text-gray-500">Последнее взаимодействие</dt>
                  <dd className="mt-1 text-sm text-gray-900">
                    {contact.lastInteractionDate ? (
                      <>
                        {new Date(contact.lastInteractionDate).toLocaleDateString()}
                        {contact.lastInteractionDaysAgo && (
                          <span className="text-gray-500"> ({contact.lastInteractionDaysAgo} дней назад)</span>
                        )}
                      </>
                    ) : 'Никогда'}
                  </dd>
                </div>
                <div>
                  <dt className="text-sm font-medium text-gray-500">Всего взаимодействий</dt>
                  <dd className="mt-1 text-sm text-gray-900">{contact.interactionCount}</dd>
                </div>
              </div>
            </div>

            {/* Notes */}
            {contact.notes && (
              <div className="mt-6 pt-6 border-t border-gray-200">
                <h3 className="text-lg font-semibold text-gray-900 mb-2">Заметки</h3>
                <p className="text-sm text-gray-700 whitespace-pre-wrap">{contact.notes}</p>
              </div>
            )}
          </div>
        </div>

        {/* Interactions */}
        <div className="bg-white shadow rounded-lg">
          <div className="px-6 py-4 border-b border-gray-200">
            <div className="flex items-center justify-between">
              <h3 className="text-lg font-semibold text-gray-900">История взаимодействий</h3>
              <button
                onClick={() => setShowAddInteraction(true)}
                className="bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-md text-sm font-medium"
              >
                Добавить взаимодействие
              </button>
            </div>
          </div>

          {/* Add Interaction Form */}
          {showAddInteraction && (
            <div className="px-6 py-4 border-b border-gray-200 bg-gray-50">
              <form onSubmit={handleAddInteraction} className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700">Тип</label>
                    <select
                      value={interactionForm.interactionTypeId}
                      onChange={(e) => setInteractionForm({...interactionForm, interactionTypeId: e.target.value})}
                      className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
                      required
                    >
                      <option value="Meeting">Встреча</option>
                      <option value="Call">Звонок</option>
                      <option value="Email">Электронная почта</option>
                      <option value="Event">Мероприятие</option>
                      <option value="Other">Другое</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700">Результат</label>
                    <select
                      value={interactionForm.resultId}
                      onChange={(e) => setInteractionForm({...interactionForm, resultId: e.target.value})}
                      className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
                      required
                    >
                      <option value="Positive">Позитивный</option>
                      <option value="Neutral">Нейтральный</option>
                      <option value="Negative">Негативный</option>
                      <option value="Postponed">Отложено</option>
                      <option value="NoResult">Без результата</option>
                    </select>
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">Комментарий</label>
                  <textarea
                    value={interactionForm.comment}
                    onChange={(e) => setInteractionForm({...interactionForm, comment: e.target.value})}
                    rows={3}
                    className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
                    placeholder="Опишите взаимодействие..."
                  />
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700">Изменить статус на</label>
                    <select
                      value={interactionForm.statusChangeTo || ''}
                      onChange={(e) => setInteractionForm({...interactionForm, statusChangeTo: e.target.value as InfluenceStatus || undefined})}
                      className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
                    >
                      <option value="">Без изменений</option>
                      {Object.values(InfluenceStatus).map(status => (
                        <option key={status} value={status}>{status}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700">Дата следующего контакта</label>
                    <input
                      type="date"
                      value={interactionForm.nextTouchDate || ''}
                      onChange={(e) => setInteractionForm({...interactionForm, nextTouchDate: e.target.value || undefined})}
                      className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
                    />
                  </div>
                </div>
                <div className="flex justify-end space-x-3">
                  <button
                    type="button"
                    onClick={() => setShowAddInteraction(false)}
                    className="px-4 py-2 border border-gray-300 rounded-md text-sm font-medium text-gray-700 bg-white hover:bg-gray-50"
                  >
                    Отмена
                  </button>
                  <button
                    type="submit"
                    className="px-4 py-2 border border-transparent rounded-md text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700"
                  >
                    Добавить взаимодействие
                  </button>
                </div>
              </form>
            </div>
          )}

          {/* Interactions List */}
          <div className="px-6 py-4">
            {(contact.interactions?.length ?? 0) === 0 ? (
              <p className="text-center py-6 text-gray-500">Взаимодействий пока нет</p>
            ) : (
              <div className="space-y-4">
                {(contact.interactions ?? []).map((interaction) => (
                  <div key={interaction.id} className="border-l-4 border-indigo-500 pl-4 py-2">
                    <div className="flex items-center justify-between">
                      <div>
                        <p className="text-sm font-medium text-gray-900">
                          {interaction.interactionTypeId}
                          <span className={`ml-2 px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${getResultBadgeColor(interaction.resultId)}`}>
                            {interaction.resultId}
                          </span>
                          {interaction.statusChangeTo && (
                            <span className="ml-2 text-sm text-gray-500">
                              Статус → {interaction.statusChangeTo}
                            </span>
                          )}
                        </p>
                        {interaction.comment && (
                          <p className="mt-1 text-sm text-gray-600">{interaction.comment}</p>
                        )}
                      </div>
                      <div className="text-right">
                        <p className="text-sm text-gray-500">
                          {new Date(interaction.interactionDate).toLocaleDateString()}
                        </p>
                        <p className="text-xs text-gray-400">от {interaction.curatorLogin}</p>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Status History */}
        {(contact.statusHistory?.length ?? 0) > 0 && (
          <div className="bg-white shadow rounded-lg">
            <div className="px-6 py-4 border-b border-gray-200">
              <h3 className="text-lg font-semibold text-gray-900">История изменения статуса</h3>
            </div>
            <div className="px-6 py-4">
              <div className="space-y-3">
                {(contact.statusHistory ?? []).map((history) => (
                  <div key={history.id} className="flex items-center justify-between py-2 border-b border-gray-100">
                    <div className="flex items-center space-x-3">
                      <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${getStatusBadgeColor(history.oldStatus)}`}>
                        {history.oldStatus}
                      </span>
                      <span className="text-gray-400">→</span>
                      <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${getStatusBadgeColor(history.newStatus)}`}>
                        {history.newStatus}
                      </span>
                    </div>
                    <div className="text-right">
                      <p className="text-sm text-gray-500">
                        {new Date(history.changedAt).toLocaleDateString()}
                      </p>
                      <p className="text-xs text-gray-400">от {history.changedBy}</p>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}
      </div>
    </MainLayout>
  );
}