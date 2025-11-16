'use client';

import React, { useState, useEffect } from 'react';
import { BarChart3, TrendingUp, Users, Calendar, Activity, PieChart, Filter, Download } from 'lucide-react';
import { api } from '@/services/api';

interface AnalyticsData {
  contactsCount: number;
  newContactsThisMonth: number;
  interactionsCount: number;
  interactionsThisMonth: number;
  blockStatistics: BlockStat[];
  influenceStatusDistribution: StatusDistribution[];
  influenceTypeDistribution: TypeDistribution[];
  topCurators: CuratorActivity[];
  statusChanges: StatusChange[];
  monthlyTrends: MonthlyTrend[];
}

interface BlockStat {
  blockName: string;
  blockCode: string;
  contactsCount: number;
  interactionsCount: number;
  curatorsCount: number;
}

interface StatusDistribution {
  status: string;
  count: number;
  percentage: number;
}

interface TypeDistribution {
  type: string;
  count: number;
}

interface CuratorActivity {
  curatorName: string;
  interactionsCount: number;
  contactsManaged: number;
  lastActivity: string;
}

interface StatusChange {
  date: string;
  contactName: string;
  fromStatus: string;
  toStatus: string;
  curatorName: string;
}

interface MonthlyTrend {
  month: string;
  contacts: number;
  interactions: number;
}

export default function AnalyticsPage() {
  const [analytics, setAnalytics] = useState<AnalyticsData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [selectedPeriod, setSelectedPeriod] = useState('month');
  const [selectedBlock, setSelectedBlock] = useState('all');

  useEffect(() => {
    loadAnalytics();
  }, [selectedPeriod, selectedBlock]);

  const loadAnalytics = async () => {
    try {
      setIsLoading(true);
      const response = await api.get('/dashboard/admin', {
        params: {
          period: selectedPeriod,
          blockId: selectedBlock === 'all' ? undefined : selectedBlock
        }
      });
      if (response.data) {
        setAnalytics(response.data);
      }
    } catch (error) {
      console.error('Не удалось загрузить аналитику:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const exportData = () => {
    // В реальной реализации это инициировало бы загрузку
    console.log('Экспорт данных аналитики...');
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-gray-500">Загрузка аналитики...</div>
      </div>
    );
  }

  if (!analytics) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-gray-500">Данные аналитики недоступны</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="mb-8">
          <div className="flex justify-between items-center mb-4">
            <div>
              <h1 className="text-3xl font-bold text-gray-900 flex items-center gap-2">
                <BarChart3 className="h-8 w-8 text-purple-600" />
                Панель аналитики
              </h1>
              <p className="text-gray-600 mt-2">Общесистемная аналитика и статистика</p>
            </div>
            <button
              onClick={exportData}
              className="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 flex items-center gap-2"
            >
              <Download className="h-4 w-4" />
              Экспорт данных
            </button>
          </div>

          {/* Filters */}
          <div className="bg-white rounded-lg shadow p-4 flex gap-4">
            <select
              value={selectedPeriod}
              onChange={(e) => setSelectedPeriod(e.target.value)}
              className="px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-purple-500"
            >
              <option value="week">Последняя неделя</option>
              <option value="month">Последний месяц</option>
              <option value="quarter">Последний квартал</option>
              <option value="year">Последний год</option>
            </select>

            <select
              value={selectedBlock}
              onChange={(e) => setSelectedBlock(e.target.value)}
              className="px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-purple-500"
            >
              <option value="all">Все блоки</option>
              {analytics.blockStatistics.map(block => (
                <option key={block.blockCode} value={block.blockCode}>
                  {block.blockName}
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* Key Metrics */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
          <div className="bg-white rounded-lg shadow p-6">
            <div className="flex items-center justify-between mb-2">
              <Users className="h-8 w-8 text-blue-600" />
              <span className="text-sm text-green-600 font-medium">
                +{analytics.newContactsThisMonth} в этом месяце
              </span>
            </div>
            <h3 className="text-2xl font-bold text-gray-900">{analytics.contactsCount}</h3>
            <p className="text-gray-600">Всего контактов</p>
          </div>

          <div className="bg-white rounded-lg shadow p-6">
            <div className="flex items-center justify-between mb-2">
              <Activity className="h-8 w-8 text-green-600" />
              <span className="text-sm text-green-600 font-medium">
                +{analytics.interactionsThisMonth} в этом месяце
              </span>
            </div>
            <h3 className="text-2xl font-bold text-gray-900">{analytics.interactionsCount}</h3>
            <p className="text-gray-600">Всего взаимодействий</p>
          </div>

          <div className="bg-white rounded-lg shadow p-6">
            <div className="flex items-center justify-between mb-2">
              <TrendingUp className="h-8 w-8 text-purple-600" />
            </div>
            <h3 className="text-2xl font-bold text-gray-900">
              {analytics.blockStatistics.length}
            </h3>
            <p className="text-gray-600">Активные блоки</p>
          </div>

          <div className="bg-white rounded-lg shadow p-6">
            <div className="flex items-center justify-between mb-2">
              <Calendar className="h-8 w-8 text-orange-600" />
            </div>
            <h3 className="text-2xl font-bold text-gray-900">
              {Math.round(analytics.interactionsCount / 30)}
            </h3>
            <p className="text-gray-600">Среднее кол-во взаимодействий в день</p>
          </div>
        </div>

        {/* Charts Row */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8">
          {/* Influence Status Distribution */}
          <div className="bg-white rounded-lg shadow p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <PieChart className="h-5 w-5 text-purple-600" />
              Распределение по статусам влияния
            </h3>
            <div className="space-y-3">
              {analytics.influenceStatusDistribution.map((status) => (
                <div key={status.status} className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <div className={`w-4 h-4 rounded-full ${getStatusColor(status.status)}`}></div>
                    <span className="font-medium">Статус {status.status}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="text-gray-600">{status.count} контактов</span>
                    <span className="text-sm text-gray-500">({status.percentage}%)</span>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Influence Type Distribution */}
          <div className="bg-white rounded-lg shadow p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">
              Распределение по типам влияния
            </h3>
            <div className="space-y-3">
              {analytics.influenceTypeDistribution.map((type) => (
                <div key={type.type} className="flex items-center justify-between">
                  <span className="text-gray-700">{type.type}</span>
                  <div className="flex items-center gap-2">
                    <div className="w-32 bg-gray-200 rounded-full h-2">
                      <div
                        className="bg-blue-600 h-2 rounded-full"
                        style={{ width: `${(type.count / analytics.contactsCount) * 100}%` }}
                      ></div>
                    </div>
                    <span className="text-sm text-gray-600 w-12 text-right">{type.count}</span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Block Statistics */}
        <div className="bg-white rounded-lg shadow p-6 mb-8">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">Статистика по блокам</h3>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b">
                  <th className="text-left py-2">Блок</th>
                  <th className="text-center py-2">Контакты</th>
                  <th className="text-center py-2">Взаимодействия</th>
                  <th className="text-center py-2">Кураторы</th>
                  <th className="text-center py-2">Среднее взаимодействий/контакт</th>
                </tr>
              </thead>
              <tbody>
                {analytics.blockStatistics.map((block) => (
                  <tr key={block.blockCode} className="border-b hover:bg-gray-50">
                    <td className="py-3">
                      <div>
                        <span className="font-medium">{block.blockName}</span>
                        <span className="ml-2 text-xs text-gray-500">({block.blockCode})</span>
                      </div>
                    </td>
                    <td className="text-center">{block.contactsCount}</td>
                    <td className="text-center">{block.interactionsCount}</td>
                    <td className="text-center">{block.curatorsCount}</td>
                    <td className="text-center">
                      {block.contactsCount > 0
                        ? (block.interactionsCount / block.contactsCount).toFixed(1)
                        : '0'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        {/* Top Curators */}
        <div className="bg-white rounded-lg shadow p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">Топ активных кураторов</h3>
          <div className="grid gap-4">
            {analytics.topCurators.slice(0, 5).map((curator, index) => (
              <div key={curator.curatorName} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                <div className="flex items-center gap-3">
                  <span className="text-lg font-bold text-gray-400">#{index + 1}</span>
                  <div>
                    <p className="font-medium text-gray-900">{curator.curatorName}</p>
                    <p className="text-sm text-gray-600">
                      {curator.contactsManaged} управляемых контактов
                    </p>
                  </div>
                </div>
                <div className="text-right">
                  <p className="font-bold text-purple-600">{curator.interactionsCount}</p>
                  <p className="text-xs text-gray-500">взаимодействий</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

function getStatusColor(status: string): string {
  switch (status) {
    case 'A':
      return 'bg-green-500';
    case 'B':
      return 'bg-blue-500';
    case 'C':
      return 'bg-yellow-500';
    case 'D':
      return 'bg-red-500';
    default:
      return 'bg-gray-500';
  }
}