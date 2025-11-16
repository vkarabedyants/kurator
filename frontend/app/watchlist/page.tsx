'use client';

import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { Plus, Search, Shield, AlertTriangle, Calendar, User, Edit, Trash2, Eye, Filter } from 'lucide-react';
import { api } from '@/services/api';
import { WatchlistDto, RiskLevel, MonitoringFrequency, ThreatSphere } from '@/types/api';

export default function WatchlistPage() {
  const router = useRouter();
  const [watchlist, setWatchlist] = useState<WatchlistDto[]>([]);
  const [filteredWatchlist, setFilteredWatchlist] = useState<WatchlistDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedRiskLevel, setSelectedRiskLevel] = useState<RiskLevel | ''>('');
  const [selectedThreatSphere, setSelectedThreatSphere] = useState<ThreatSphere | ''>('');

  useEffect(() => {
    loadWatchlist();
  }, []);

  useEffect(() => {
    filterWatchlist();
  }, [searchTerm, selectedRiskLevel, selectedThreatSphere, watchlist]);

  const loadWatchlist = async () => {
    try {
      setIsLoading(true);
      const response = await api.get('/watchlist');
      if (response.data?.items) {
        setWatchlist(response.data.items);
      }
    } catch (error) {
      console.error('Failed to load watchlist:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const filterWatchlist = () => {
    let filtered = [...watchlist];

    if (searchTerm) {
      filtered = filtered.filter(item =>
        item.fullNameOrAlias.toLowerCase().includes(searchTerm.toLowerCase()) ||
        item.roleOrStatus?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        item.threatSource.toLowerCase().includes(searchTerm.toLowerCase())
      );
    }

    if (selectedRiskLevel) {
      filtered = filtered.filter(item => item.riskLevel === selectedRiskLevel);
    }

    if (selectedThreatSphere) {
      filtered = filtered.filter(item => item.threatSphere === selectedThreatSphere);
    }

    setFilteredWatchlist(filtered);
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Вы уверены, что хотите удалить эту запись из списка наблюдения?')) {
      return;
    }

    try {
      await api.delete(`/watchlist/${id}`);
      await loadWatchlist();
    } catch (error) {
      console.error('Failed to delete watchlist entry:', error);
    }
  };

  const getRiskLevelColor = (level: RiskLevel) => {
    switch (level) {
      case RiskLevel.Critical:
        return 'bg-red-100 text-red-800 border-red-200';
      case RiskLevel.High:
        return 'bg-orange-100 text-orange-800 border-orange-200';
      case RiskLevel.Medium:
        return 'bg-yellow-100 text-yellow-800 border-yellow-200';
      case RiskLevel.Low:
        return 'bg-green-100 text-green-800 border-green-200';
      default:
        return 'bg-gray-100 text-gray-800 border-gray-200';
    }
  };

  const getThreatSphereIcon = (sphere: ThreatSphere) => {
    switch (sphere) {
      case ThreatSphere.Media:
        return '📰';
      case ThreatSphere.Legal:
        return '⚖️';
      case ThreatSphere.Political:
        return '🏛️';
      case ThreatSphere.Economic:
        return '💼';
      case ThreatSphere.Security:
        return '🛡️';
      case ThreatSphere.Communication:
        return '📡';
      default:
        return '❓';
    }
  };

  const isCheckOverdue = (nextCheckDate: string) => {
    return new Date(nextCheckDate) < new Date();
  };

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="mb-8">
          <div className="flex justify-between items-center mb-4">
            <div>
              <h1 className="text-3xl font-bold text-gray-900 flex items-center gap-2">
                <Shield className="h-8 w-8 text-red-600" />
                Реестр угроз (Список наблюдения)
              </h1>
              <p className="text-gray-600 mt-2">Мониторинг потенциальных угроз и рисков</p>
            </div>
            <button
              onClick={() => router.push('/watchlist/new')}
              className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 flex items-center gap-2"
            >
              <Plus className="h-4 w-4" />
              Добавить угрозу
            </button>
          </div>

          {/* Filters */}
          <div className="bg-white rounded-lg shadow p-4">
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
                <input
                  type="text"
                  placeholder="Поиск угроз..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-10 pr-3 py-2 w-full border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-red-500"
                />
              </div>

              <select
                value={selectedRiskLevel}
                onChange={(e) => setSelectedRiskLevel(e.target.value as RiskLevel | '')}
                className="px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-red-500"
              >
                <option value="">Все уровни риска</option>
                <option value={RiskLevel.Critical}>Критический</option>
                <option value={RiskLevel.High}>Высокий</option>
                <option value={RiskLevel.Medium}>Средний</option>
                <option value={RiskLevel.Low}>Низкий</option>
              </select>

              <select
                value={selectedThreatSphere}
                onChange={(e) => setSelectedThreatSphere(e.target.value as ThreatSphere | '')}
                className="px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-red-500"
              >
                <option value="">Все сферы угроз</option>
                <option value={ThreatSphere.Media}>СМИ</option>
                <option value={ThreatSphere.Legal}>Правовая</option>
                <option value={ThreatSphere.Political}>Политическая</option>
                <option value={ThreatSphere.Economic}>Экономическая</option>
                <option value={ThreatSphere.Security}>Безопасность</option>
                <option value={ThreatSphere.Communication}>Коммуникации</option>
                <option value={ThreatSphere.Other}>Другое</option>
              </select>

              <div className="flex items-center gap-2 text-sm text-gray-600">
                <Filter className="h-4 w-4" />
                <span>{filteredWatchlist.length} угроз найдено</span>
              </div>
            </div>
          </div>
        </div>

        {/* Watchlist Grid */}
        {isLoading ? (
          <div className="flex justify-center items-center h-64">
            <div className="text-gray-500">Загрузка угроз...</div>
          </div>
        ) : filteredWatchlist.length === 0 ? (
          <div className="bg-white rounded-lg shadow p-12 text-center">
            <AlertTriangle className="h-12 w-12 text-gray-400 mx-auto mb-4" />
            <h3 className="text-lg font-medium text-gray-900 mb-2">Угрозы не найдены</h3>
            <p className="text-gray-600">
              {searchTerm || selectedRiskLevel || selectedThreatSphere
                ? 'Попробуйте изменить фильтры'
                : 'Начните с добавления потенциальных угроз для мониторинга'}
            </p>
          </div>
        ) : (
          <div className="grid gap-4">
            {filteredWatchlist.map((item) => (
              <div key={item.id} className="bg-white rounded-lg shadow hover:shadow-lg transition-shadow">
                <div className="p-6">
                  <div className="flex justify-between items-start mb-4">
                    <div>
                      <div className="flex items-center gap-3 mb-2">
                        <span className="text-2xl">{getThreatSphereIcon(item.threatSphere)}</span>
                        <div>
                          <h3 className="text-lg font-semibold text-gray-900">
                            {item.fullNameOrAlias}
                          </h3>
                          <p className="text-sm text-gray-600">{item.roleOrStatus}</p>
                        </div>
                      </div>
                      <div className="flex items-center gap-4 text-sm">
                        <span className={`px-2 py-1 rounded-full border ${getRiskLevelColor(item.riskLevel)}`}>
                          {item.riskLevel === RiskLevel.Critical ? 'Критический риск' :
                           item.riskLevel === RiskLevel.High ? 'Высокий риск' :
                           item.riskLevel === RiskLevel.Medium ? 'Средний риск' :
                           item.riskLevel === RiskLevel.Low ? 'Низкий риск' : 'Риск'}
                        </span>
                        <span className="text-gray-600">
                          {item.threatSphere}
                        </span>
                        <span className="text-gray-600">
                          Мониторинг: {item.monitoringFrequency === MonitoringFrequency.Daily ? 'Ежедневно' :
                                       item.monitoringFrequency === MonitoringFrequency.Weekly ? 'Еженедельно' :
                                       item.monitoringFrequency === MonitoringFrequency.Monthly ? 'Ежемесячно' :
                                       item.monitoringFrequency === MonitoringFrequency.Quarterly ? 'Ежеквартально' :
                                       item.monitoringFrequency}
                        </span>
                      </div>
                    </div>
                    <div className="flex gap-2">
                      <button
                        onClick={() => router.push(`/watchlist/${item.id}`)}
                        className="p-2 text-gray-600 hover:text-blue-600"
                        title="Просмотр деталей"
                      >
                        <Eye className="h-4 w-4" />
                      </button>
                      <button
                        onClick={() => router.push(`/watchlist/${item.id}/edit`)}
                        className="p-2 text-gray-600 hover:text-blue-600"
                        title="Редактировать"
                      >
                        <Edit className="h-4 w-4" />
                      </button>
                      <button
                        onClick={() => handleDelete(item.id)}
                        className="p-2 text-gray-600 hover:text-red-600"
                        title="Удалить"
                      >
                        <Trash2 className="h-4 w-4" />
                      </button>
                    </div>
                  </div>

                  <div className="border-t pt-4">
                    <div className="mb-3">
                      <p className="text-sm font-medium text-gray-700 mb-1">Источник угрозы:</p>
                      <p className="text-sm text-gray-600">{item.threatSource}</p>
                    </div>

                    {item.progressDynamics && (
                      <div className="mb-3">
                        <p className="text-sm font-medium text-gray-700 mb-1">Текущий статус:</p>
                        <p className="text-sm text-gray-600">{item.progressDynamics}</p>
                      </div>
                    )}

                    <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mt-4 text-sm">
                      <div>
                        <p className="text-gray-500">Начало конфликта</p>
                        <p className="font-medium">
                          {new Date(item.conflictStartDate).toLocaleDateString()}
                        </p>
                      </div>
                      <div>
                        <p className="text-gray-500">Последняя проверка</p>
                        <p className="font-medium">
                          {item.lastCheckDate
                            ? new Date(item.lastCheckDate).toLocaleDateString()
                            : 'Никогда'}
                        </p>
                      </div>
                      <div>
                        <p className="text-gray-500">Следующая проверка</p>
                        <p className={`font-medium ${isCheckOverdue(item.nextCheckDate) ? 'text-red-600' : ''}`}>
                          <Calendar className="inline h-3 w-3 mr-1" />
                          {new Date(item.nextCheckDate).toLocaleDateString()}
                          {isCheckOverdue(item.nextCheckDate) && ' (Просрочено)'}
                        </p>
                      </div>
                      <div>
                        <p className="text-gray-500">Наблюдатель</p>
                        <p className="font-medium">
                          <User className="inline h-3 w-3 mr-1" />
                          {item.watchOwnerName || 'Не назначен'}
                        </p>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}