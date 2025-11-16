'use client';

import React, { useState, useEffect } from 'react';
import { History, Filter, User, Calendar, Activity, Search, FileText, Edit, Trash2, Plus, Eye } from 'lucide-react';
import { api } from '@/services/api';
import { AuditLogDto, AuditActionType } from '@/types/api';

export default function AuditLogPage() {
  const [auditLogs, setAuditLogs] = useState<AuditLogDto[]>([]);
  const [filteredLogs, setFilteredLogs] = useState<AuditLogDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedUser, setSelectedUser] = useState('');
  const [selectedAction, setSelectedAction] = useState<AuditActionType | ''>('');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [users, setUsers] = useState<string[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 50;

  useEffect(() => {
    loadAuditLogs();
  }, []);

  useEffect(() => {
    filterLogs();
  }, [searchTerm, selectedUser, selectedAction, dateFrom, dateTo, auditLogs]);

  const loadAuditLogs = async () => {
    try {
      setIsLoading(true);
      const response = await api.get('/audit-log', {
        params: {
          pageSize: 500 // Load more initially
        }
      });
      if (response.data?.items) {
        setAuditLogs(response.data.items);

        // Extract unique users for filter
        const uniqueUsers = [...new Set(response.data.items.map((log: AuditLogDto) => log.userName))] as string[];
        setUsers(uniqueUsers);
      }
    } catch (error) {
      console.error('Не удалось загрузить журнал аудита:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const filterLogs = () => {
    let filtered = [...auditLogs];

    if (searchTerm) {
      filtered = filtered.filter(log =>
        log.entityType.toLowerCase().includes(searchTerm.toLowerCase()) ||
        log.entityId?.toString().includes(searchTerm) ||
        log.userName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        log.details?.toLowerCase().includes(searchTerm.toLowerCase())
      );
    }

    if (selectedUser) {
      filtered = filtered.filter(log => log.userName === selectedUser);
    }

    if (selectedAction) {
      filtered = filtered.filter(log => log.actionType === selectedAction);
    }

    if (dateFrom) {
      filtered = filtered.filter(log => new Date(log.timestamp) >= new Date(dateFrom));
    }

    if (dateTo) {
      const endDate = new Date(dateTo);
      endDate.setHours(23, 59, 59, 999);
      filtered = filtered.filter(log => new Date(log.timestamp) <= endDate);
    }

    setFilteredLogs(filtered);
    setCurrentPage(1);
  };

  const getActionIcon = (action: AuditActionType) => {
    switch (action) {
      case AuditActionType.Create:
        return <Plus className="h-4 w-4 text-green-600" />;
      case AuditActionType.Update:
        return <Edit className="h-4 w-4 text-blue-600" />;
      case AuditActionType.Delete:
        return <Trash2 className="h-4 w-4 text-red-600" />;
      case AuditActionType.View:
        return <Eye className="h-4 w-4 text-gray-600" />;
      case AuditActionType.Login:
        return <User className="h-4 w-4 text-purple-600" />;
      case AuditActionType.ChangeStatus:
        return <Activity className="h-4 w-4 text-orange-600" />;
      default:
        return <FileText className="h-4 w-4 text-gray-600" />;
    }
  };

  const getActionLabel = (action: AuditActionType) => {
    switch (action) {
      case AuditActionType.Create:
        return 'Создано';
      case AuditActionType.Update:
        return 'Обновлено';
      case AuditActionType.Delete:
        return 'Удалено';
      case AuditActionType.View:
        return 'Просмотрено';
      case AuditActionType.Login:
        return 'Вход выполнен';
      case AuditActionType.ChangeStatus:
        return 'Статус изменен';
      default:
        return action;
    }
  };

  const getActionColor = (action: AuditActionType) => {
    switch (action) {
      case AuditActionType.Create:
        return 'bg-green-100 text-green-800';
      case AuditActionType.Update:
        return 'bg-blue-100 text-blue-800';
      case AuditActionType.Delete:
        return 'bg-red-100 text-red-800';
      case AuditActionType.View:
        return 'bg-gray-100 text-gray-800';
      case AuditActionType.Login:
        return 'bg-purple-100 text-purple-800';
      case AuditActionType.ChangeStatus:
        return 'bg-orange-100 text-orange-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp);
    const today = new Date();
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);

    if (date.toDateString() === today.toDateString()) {
      return `Сегодня, ${date.toLocaleTimeString()}`;
    } else if (date.toDateString() === yesterday.toDateString()) {
      return `Вчера, ${date.toLocaleTimeString()}`;
    } else {
      return date.toLocaleString();
    }
  };

  // Pagination
  const totalPages = Math.ceil(filteredLogs.length / itemsPerPage);
  const paginatedLogs = filteredLogs.slice(
    (currentPage - 1) * itemsPerPage,
    currentPage * itemsPerPage
  );

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="mb-8">
          <div className="flex justify-between items-center mb-4">
            <div>
              <h1 className="text-3xl font-bold text-gray-900 flex items-center gap-2">
                <History className="h-8 w-8 text-indigo-600" />
                Журнал аудита
              </h1>
              <p className="text-gray-600 mt-2">Отслеживание всех действий пользователей и системных изменений</p>
            </div>
            <div className="text-sm text-gray-600">
              Всего записей: {filteredLogs.length}
            </div>
          </div>

          {/* Filters */}
          <div className="bg-white rounded-lg shadow p-4">
            <div className="grid grid-cols-1 md:grid-cols-5 gap-4">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
                <input
                  type="text"
                  placeholder="Поиск по журналу..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-10 pr-3 py-2 w-full border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500"
                />
              </div>

              <select
                value={selectedUser}
                onChange={(e) => setSelectedUser(e.target.value)}
                className="px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500"
              >
                <option value="">Все пользователи</option>
                {users.map(user => (
                  <option key={user} value={user}>{user}</option>
                ))}
              </select>

              <select
                value={selectedAction}
                onChange={(e) => setSelectedAction(e.target.value as AuditActionType | '')}
                className="px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500"
              >
                <option value="">Все действия</option>
                <option value={AuditActionType.Create}>Создание</option>
                <option value={AuditActionType.Update}>Обновление</option>
                <option value={AuditActionType.Delete}>Удаление</option>
                <option value={AuditActionType.View}>Просмотр</option>
                <option value={AuditActionType.Login}>Вход в систему</option>
                <option value={AuditActionType.ChangeStatus}>Изменение статуса</option>
              </select>

              <input
                type="date"
                value={dateFrom}
                onChange={(e) => setDateFrom(e.target.value)}
                className="px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500"
                placeholder="From date"
              />

              <input
                type="date"
                value={dateTo}
                onChange={(e) => setDateTo(e.target.value)}
                className="px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500"
                placeholder="To date"
              />
            </div>

            {(searchTerm || selectedUser || selectedAction || dateFrom || dateTo) && (
              <div className="mt-3 flex items-center gap-2">
                <span className="text-sm text-gray-600">Активные фильтры:</span>
                <button
                  onClick={() => {
                    setSearchTerm('');
                    setSelectedUser('');
                    setSelectedAction('');
                    setDateFrom('');
                    setDateTo('');
                  }}
                  className="text-sm text-indigo-600 hover:text-indigo-800"
                >
                  Очистить все
                </button>
              </div>
            )}
          </div>
        </div>

        {/* Logs Table */}
        {isLoading ? (
          <div className="flex justify-center items-center h-64">
            <div className="text-gray-500">Загрузка журнала аудита...</div>
          </div>
        ) : paginatedLogs.length === 0 ? (
          <div className="bg-white rounded-lg shadow p-12 text-center">
            <History className="h-12 w-12 text-gray-400 mx-auto mb-4" />
            <h3 className="text-lg font-medium text-gray-900 mb-2">Записи аудита не найдены</h3>
            <p className="text-gray-600">
              {searchTerm || selectedUser || selectedAction || dateFrom || dateTo
                ? 'Попробуйте изменить параметры фильтров'
                : 'Действия еще не записывались'}
            </p>
          </div>
        ) : (
          <>
            <div className="bg-white rounded-lg shadow overflow-hidden">
              <table className="w-full">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Время
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Пользователь
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Действие
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Сущность
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Детали
                    </th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {paginatedLogs.map((log) => (
                    <tr key={log.id} className="hover:bg-gray-50">
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                        <div className="flex items-center gap-2">
                          <Calendar className="h-4 w-4 text-gray-400" />
                          {formatTimestamp(log.timestamp)}
                        </div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm">
                        <div className="flex items-center gap-2">
                          <User className="h-4 w-4 text-gray-400" />
                          <span className="font-medium text-gray-900">{log.userName}</span>
                        </div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm">
                        <div className="flex items-center gap-2">
                          {getActionIcon(log.actionType)}
                          <span className={`px-2 py-1 rounded-full text-xs ${getActionColor(log.actionType)}`}>
                            {getActionLabel(log.actionType)}
                          </span>
                        </div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                        <div>
                          <span className="font-medium">{log.entityType}</span>
                          {log.entityId && (
                            <span className="text-gray-500 ml-1">#{log.entityId}</span>
                          )}
                        </div>
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-600">
                        {log.oldValue && log.newValue ? (
                          <div className="max-w-xs">
                            <span className="text-red-600 line-through">{log.oldValue}</span>
                            <span className="mx-2">→</span>
                            <span className="text-green-600">{log.newValue}</span>
                          </div>
                        ) : (
                          <span className="text-gray-500">{log.details || '-'}</span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="mt-6 flex justify-center">
                <nav className="flex gap-2">
                  <button
                    onClick={() => setCurrentPage(Math.max(1, currentPage - 1))}
                    disabled={currentPage === 1}
                    className="px-3 py-2 text-sm text-gray-700 bg-white border rounded-lg hover:bg-gray-50 disabled:opacity-50"
                  >
                    Назад
                  </button>

                  {[...Array(Math.min(5, totalPages))].map((_, idx) => {
                    const pageNum = idx + 1;
                    return (
                      <button
                        key={pageNum}
                        onClick={() => setCurrentPage(pageNum)}
                        className={`px-3 py-2 text-sm rounded-lg ${
                          currentPage === pageNum
                            ? 'bg-indigo-600 text-white'
                            : 'text-gray-700 bg-white border hover:bg-gray-50'
                        }`}
                      >
                        {pageNum}
                      </button>
                    );
                  })}

                  {totalPages > 5 && <span className="px-3 py-2 text-gray-500">...</span>}

                  {totalPages > 5 && (
                    <button
                      onClick={() => setCurrentPage(totalPages)}
                      className={`px-3 py-2 text-sm rounded-lg ${
                        currentPage === totalPages
                          ? 'bg-indigo-600 text-white'
                          : 'text-gray-700 bg-white border hover:bg-gray-50'
                      }`}
                    >
                      {totalPages}
                    </button>
                  )}

                  <button
                    onClick={() => setCurrentPage(Math.min(totalPages, currentPage + 1))}
                    disabled={currentPage === totalPages}
                    className="px-3 py-2 text-sm text-gray-700 bg-white border rounded-lg hover:bg-gray-50 disabled:opacity-50"
                  >
                    Далее
                  </button>
                </nav>
              </div>
            )}
          </>
        )}

        {/* Summary Statistics */}
        <div className="mt-8 bg-white rounded-lg shadow p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">Сводка активности</h3>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div className="text-center">
              <p className="text-2xl font-bold text-green-600">
                {filteredLogs.filter(l => l.actionType === AuditActionType.Create).length}
              </p>
              <p className="text-sm text-gray-600">Создано</p>
            </div>
            <div className="text-center">
              <p className="text-2xl font-bold text-blue-600">
                {filteredLogs.filter(l => l.actionType === AuditActionType.Update).length}
              </p>
              <p className="text-sm text-gray-600">Обновлено</p>
            </div>
            <div className="text-center">
              <p className="text-2xl font-bold text-red-600">
                {filteredLogs.filter(l => l.actionType === AuditActionType.Delete).length}
              </p>
              <p className="text-sm text-gray-600">Удалено</p>
            </div>
            <div className="text-center">
              <p className="text-2xl font-bold text-purple-600">
                {filteredLogs.filter(l => l.actionType === AuditActionType.Login).length}
              </p>
              <p className="text-sm text-gray-600">Входов в систему</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}