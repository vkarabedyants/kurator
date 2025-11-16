'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import MainLayout from '@/components/layout/MainLayout';
import { interactionsApi } from '@/services/api';
import { Interaction } from '@/types/api';

export default function InteractionsPage() {
  const router = useRouter();
  const [interactions, setInteractions] = useState<Interaction[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [filterType, setFilterType] = useState<string>('');
  const [filterResult, setFilterResult] = useState<string>('');
  const [dateFrom, setDateFrom] = useState<string>('');
  const [dateTo, setDateTo] = useState<string>('');

  useEffect(() => {
    fetchInteractions();
  }, [page, filterType, filterResult, dateFrom, dateTo]);

  const fetchInteractions = async () => {
    try {
      setLoading(true);
      const response = await interactionsApi.getAll({
        page,
        pageSize: 20,
        interactionTypeId: filterType || undefined,
        resultId: filterResult || undefined,
        fromDate: dateFrom || undefined,
        toDate: dateTo || undefined,
      });
      setInteractions(response.data);
      setTotalPages(response.totalPages);
    } catch (err: any) {
      setError(err.message || 'Не удалось загрузить взаимодействия');
    } finally {
      setLoading(false);
    }
  };

  const getResultBadgeColor = (result: string) => {
    switch (result) {
      case 'Positive': return 'bg-green-100 text-green-800';
      case 'Neutral': return 'bg-gray-100 text-gray-800';
      case 'Negative': return 'bg-red-100 text-red-800';
      case 'Postponed': return 'bg-yellow-100 text-yellow-800';
      case 'NoResult': return 'bg-gray-100 text-gray-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  };

  const getTypeBadgeColor = (type: string) => {
    switch (type) {
      case 'Meeting': return 'bg-blue-100 text-blue-800';
      case 'Call': return 'bg-green-100 text-green-800';
      case 'Email': return 'bg-purple-100 text-purple-800';
      case 'Event': return 'bg-pink-100 text-pink-800';
      case 'Other': return 'bg-gray-100 text-gray-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  };

  const handleDelete = async (id: number) => {
    if (!window.confirm('Вы уверены, что хотите удалить это взаимодействие?')) {
      return;
    }

    try {
      await interactionsApi.delete(id);
      fetchInteractions();
    } catch (err: any) {
      alert('Не удалось удалить взаимодействие: ' + err.message);
    }
  };

  return (
    <MainLayout>
      <div className="bg-white shadow rounded-lg">
        {/* Header */}
        <div className="px-6 py-4 border-b border-gray-200">
          <div className="flex items-center justify-between">
            <h2 className="text-xl font-semibold text-gray-900">Журнал взаимодействий</h2>
            <Link
              href="/interactions/new"
              className="bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-md text-sm font-medium"
            >
              Добавить взаимодействие
            </Link>
          </div>
        </div>

        {/* Filters */}
        <div className="px-6 py-4 border-b border-gray-200">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Тип</label>
              <select
                value={filterType}
                onChange={(e) => setFilterType(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
              >
                <option value="">Все типы</option>
                <option value="Meeting">Встреча</option>
                <option value="Call">Звонок</option>
                <option value="Email">Электронная почта</option>
                <option value="Event">Событие</option>
                <option value="Other">Другое</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Результат</label>
              <select
                value={filterResult}
                onChange={(e) => setFilterResult(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
              >
                <option value="">Все результаты</option>
                <option value="Positive">Положительный</option>
                <option value="Neutral">Нейтральный</option>
                <option value="Negative">Отрицательный</option>
                <option value="Postponed">Отложено</option>
                <option value="NoResult">Без результата</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">С даты</label>
              <input
                type="date"
                value={dateFrom}
                onChange={(e) => setDateFrom(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">По дату</label>
              <input
                type="date"
                value={dateTo}
                onChange={(e) => setDateTo(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
              />
            </div>
          </div>
        </div>

        {/* Content */}
        <div className="px-6 py-4">
          {loading ? (
            <div className="flex justify-center py-12">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
            </div>
          ) : error ? (
            <div className="text-center py-12 text-red-600">
              Ошибка: {error}
            </div>
          ) : interactions.length === 0 ? (
            <div className="text-center py-12 text-gray-500">
              Взаимодействия не найдены
            </div>
          ) : (
            <div className="space-y-4">
              {interactions.map((interaction) => (
                <div key={interaction.id} className="border rounded-lg p-4 hover:shadow-md transition">
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center space-x-3 mb-2">
                        <span className={`px-2 py-1 text-xs font-semibold rounded-full ${getTypeBadgeColor(interaction.interactionTypeId)}`}>
                          {interaction.interactionTypeId}
                        </span>
                        <span className={`px-2 py-1 text-xs font-semibold rounded-full ${getResultBadgeColor(interaction.resultId)}`}>
                          {interaction.resultId}
                        </span>
                        {interaction.statusChangeTo && (
                          <span className="text-sm text-gray-600">
                            Статус изменен на: <span className="font-semibold">{interaction.statusChangeTo}</span>
                          </span>
                        )}
                      </div>
                      {interaction.comment && (
                        <p className="text-gray-700 mb-2">{interaction.comment}</p>
                      )}
                      <div className="flex items-center space-x-4 text-sm text-gray-500">
                        <span>Контакт #{interaction.contactId}</span>
                        <span>{new Date(interaction.interactionDate).toLocaleDateString()}</span>
                        <span>от {interaction.curatorLogin}</span>
                        {interaction.nextTouchDate && (
                          <span>Следующее: {new Date(interaction.nextTouchDate).toLocaleDateString()}</span>
                        )}
                      </div>
                    </div>
                    <div className="flex space-x-2 ml-4">
                      <Link
                        href={`/contacts/${interaction.contactId}`}
                        className="text-indigo-600 hover:text-indigo-900 text-sm"
                      >
                        Просмотр контакта
                      </Link>
                      <button
                        onClick={() => handleDelete(interaction.id)}
                        className="text-red-600 hover:text-red-900 text-sm"
                      >
                        Удалить
                      </button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="mt-6 flex items-center justify-between">
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
                className="px-4 py-2 border border-gray-300 rounded-md text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Предыдущая
              </button>
              <span className="text-sm text-gray-700">
                Страница {page} из {totalPages}
              </span>
              <button
                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                disabled={page === totalPages}
                className="px-4 py-2 border border-gray-300 rounded-md text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Следующая
              </button>
            </div>
          )}
        </div>
      </div>
    </MainLayout>
  );
}