'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { dashboardApi } from '@/services/api';
import { CuratorDashboard, AdminDashboard, UserRole } from '@/types/api';
import { formatDistanceToNow, format } from 'date-fns';

export default function DashboardPage() {
  const [user, setUser] = useState<any>(null);
  const [dashboardData, setDashboardData] = useState<CuratorDashboard | AdminDashboard | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const router = useRouter();

  useEffect(() => {
    const userStr = localStorage.getItem('user');
    if (!userStr) {
      router.push('/login');
      return;
    }
    const userData = JSON.parse(userStr);
    setUser(userData);
    loadDashboard(userData);
  }, [router]);

  const loadDashboard = async (userData: any) => {
    try {
      setLoading(true);
      setError(null);

      if (userData.role === UserRole.Admin) {
        const data = await dashboardApi.getAdminDashboard();
        setDashboardData(data);
      } else if (userData.role === UserRole.Curator || userData.role === UserRole.BackupCurator) {
        const data = await dashboardApi.getCuratorDashboard();
        setDashboardData(data);
      } else if (userData.role === UserRole.ThreatAnalyst) {
        // Threat analyst gets a simplified view
        router.push('/watchlist');
        return;
      }
    } catch (err) {
      setError('Не удалось загрузить данные панели управления');
      console.error('Dashboard error:', err);
    } finally {
      setLoading(false);
    }
  };

  if (!user || loading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-red-500">{error}</div>
      </div>
    );
  }

  const isAdmin = user.role === UserRole.Admin;
  const isCurator = user.role === UserRole.Curator || user.role === UserRole.BackupCurator;

  return (
    <div className="p-6">
      <div className="mb-6">
        <h1 className="text-3xl font-bold text-gray-900">
          Добро пожаловать, {user.login}!
        </h1>
        <p className="text-gray-600 mt-1">
          Роль: <span className="font-semibold">{user.role}</span>
          {user.role === UserRole.BackupCurator && <span className="ml-2 text-yellow-600">(Резервный)</span>}
        </p>
      </div>

      {isCurator && dashboardData && 'totalContacts' in dashboardData && (
        <CuratorDashboardView data={dashboardData as CuratorDashboard} />
      )}

      {isAdmin && dashboardData && 'totalBlocks' in dashboardData && (
        <AdminDashboardView data={dashboardData as AdminDashboard} />
      )}
    </div>
  );
}

function CuratorDashboardView({ data }: { data: CuratorDashboard }) {
  return (
    <div className="space-y-6">
      {/* Key Metrics */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <MetricCard
          title="Всего контактов"
          value={data.totalContacts}
          icon="👥"
          color="bg-blue-500"
        />
        <MetricCard
          title="Взаимодействия (За месяц)"
          value={data.interactionsLastMonth}
          icon="🤝"
          color="bg-green-500"
        />
        <MetricCard
          title="Средний интервал"
          value={`${data.averageInteractionInterval} дней`}
          icon="⏱️"
          color="bg-yellow-500"
        />
        <MetricCard
          title="Просроченные контакты"
          value={data.overdueContacts}
          icon="⚠️"
          color="bg-red-500"
          highlight={data.overdueContacts > 0}
        />
      </div>

      {/* Contacts Requiring Attention */}
      {data.contactsRequiringAttention.length > 0 && (
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold mb-4 text-red-600">
            ⚠️ Контакты, требующие внимания
          </h2>
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">ID контакта</th>
                  <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">Имя</th>
                  <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">Статус</th>
                  <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">Дней просрочено</th>
                  <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">Следующий контакт</th>
                  <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">Действие</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {data.contactsRequiringAttention.map((contact) => (
                  <tr key={contact.id} className="hover:bg-gray-50">
                    <td className="px-4 py-2 text-sm font-medium text-gray-900">{contact.contactId}</td>
                    <td className="px-4 py-2 text-sm text-gray-500">{contact.fullName}</td>
                    <td className="px-4 py-2 text-sm">
                      <span className={`px-2 py-1 rounded-full text-xs font-medium ${
                        contact.influenceStatus === 'A' ? 'bg-green-100 text-green-800' :
                        contact.influenceStatus === 'B' ? 'bg-blue-100 text-blue-800' :
                        contact.influenceStatus === 'C' ? 'bg-yellow-100 text-yellow-800' :
                        'bg-red-100 text-red-800'
                      }`}>
                        {contact.influenceStatus}
                      </span>
                    </td>
                    <td className="px-4 py-2 text-sm text-red-600 font-medium">{contact.daysOverdue} дней</td>
                    <td className="px-4 py-2 text-sm text-gray-500">
                      {contact.nextTouchDate ? format(new Date(contact.nextTouchDate), 'dd MMM yyyy') : 'Не установлено'}
                    </td>
                    <td className="px-4 py-2 text-sm">
                      <Link
                        href={`/contacts/${contact.id}`}
                        className="text-indigo-600 hover:text-indigo-900"
                      >
                        Просмотреть
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Recent Interactions */}
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold mb-4">Последние взаимодействия</h2>
          <div className="space-y-3">
            {data.recentInteractions.length === 0 ? (
              <p className="text-gray-500">Нет недавних взаимодействий</p>
            ) : (
              data.recentInteractions.map((interaction) => (
                <div key={interaction.id} className="border-l-4 border-indigo-500 pl-4 py-2">
                  <div className="flex justify-between items-start">
                    <div>
                      <p className="font-medium text-gray-900">{interaction.contactName}</p>
                      <p className="text-sm text-gray-500">ID: {interaction.contactId}</p>
                      <p className="text-xs text-gray-400">
                        {formatDistanceToNow(new Date(interaction.interactionDate), { addSuffix: true })}
                      </p>
                    </div>
                    <div className="text-right">
                      <span className="text-xs text-gray-500">{interaction.interactionTypeId}</span>
                      <br />
                      <span className={`text-xs px-2 py-1 rounded ${
                        interaction.resultId === 'Positive' ? 'bg-green-100 text-green-800' :
                        interaction.resultId === 'Negative' ? 'bg-red-100 text-red-800' :
                        'bg-gray-100 text-gray-800'
                      }`}>
                        {interaction.resultId}
                      </span>
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        {/* Contacts by Status */}
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold mb-4">Контакты по статусу влияния</h2>
          <div className="space-y-2">
            {Object.entries(data.contactsByInfluenceStatus || {}).map(([status, count]) => (
              <div key={status} className="flex justify-between items-center">
                <div className="flex items-center">
                  <span className={`w-3 h-3 rounded-full mr-2 ${
                    status === 'A' ? 'bg-green-500' :
                    status === 'B' ? 'bg-blue-500' :
                    status === 'C' ? 'bg-yellow-500' :
                    'bg-red-500'
                  }`}></span>
                  <span className="text-gray-700">Статус {status}</span>
                </div>
                <span className="font-semibold">{count}</span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

function AdminDashboardView({ data }: { data: AdminDashboard }) {
  return (
    <div className="space-y-6">
      {/* Key Metrics */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <MetricCard
          title="Всего контактов"
          value={data.totalContacts}
          subtitle={`+${data.newContactsLastMonth} за месяц`}
          icon="👥"
          color="bg-blue-500"
        />
        <MetricCard
          title="Всего взаимодействий"
          value={data.totalInteractions}
          subtitle={`${data.interactionsLastMonth} за месяц`}
          icon="🤝"
          color="bg-green-500"
        />
        <MetricCard
          title="Активные блоки"
          value={data.totalBlocks}
          icon="📦"
          color="bg-purple-500"
        />
        <MetricCard
          title="Всего пользователей"
          value={data.totalUsers}
          icon="👤"
          color="bg-orange-500"
        />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Contacts by Block */}
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold mb-4">Контакты по блокам</h2>
          <div className="space-y-2">
            {Object.entries(data.contactsByBlock || {})
              .sort(([, a], [, b]) => b - a)
              .slice(0, 5)
              .map(([block, count]) => (
                <div key={block} className="flex justify-between items-center">
                  <span className="text-gray-700">{block}</span>
                  <div className="flex items-center">
                    <div className="w-32 bg-gray-200 rounded-full h-2 mr-2">
                      <div
                        className="bg-indigo-600 h-2 rounded-full"
                        style={{ width: `${(count / data.totalContacts) * 100}%` }}
                      ></div>
                    </div>
                    <span className="font-semibold">{count}</span>
                  </div>
                </div>
              ))}
          </div>
        </div>

        {/* Top Curators */}
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold mb-4">Топ кураторов по активности</h2>
          <div className="space-y-2">
            {Object.entries(data.topCuratorsByActivity || {})
              .sort(([, a], [, b]) => b - a)
              .map(([curator, count], index) => (
                <div key={curator} className="flex justify-between items-center">
                  <div className="flex items-center">
                    <span className={`mr-2 ${index === 0 ? '🥇' : index === 1 ? '🥈' : index === 2 ? '🥉' : '🏅'}`}>
                    </span>
                    <span className="text-gray-700">{curator}</span>
                  </div>
                  <span className="font-semibold">{count} взаимодействий</span>
                </div>
              ))}
          </div>
        </div>
      </div>

      {/* Status Distribution */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold mb-4">Контакты по статусу влияния</h2>
          <div className="grid grid-cols-2 gap-4">
            {Object.entries(data.contactsByInfluenceStatus || {}).map(([status, count]) => (
              <div key={status} className="text-center">
                <div className={`text-3xl font-bold ${
                  status === 'A' ? 'text-green-500' :
                  status === 'B' ? 'text-blue-500' :
                  status === 'C' ? 'text-yellow-500' :
                  'text-red-500'
                }`}>
                  {count}
                </div>
                <div className="text-gray-600">Статус {status}</div>
                <div className="text-xs text-gray-400">
                  {((count / data.totalContacts) * 100).toFixed(1)}%
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold mb-4">Динамика изменения статусов</h2>
          <div className="space-y-2 max-h-64 overflow-y-auto">
            {Object.entries(data.statusChangeDynamics || {})
              .sort(([, a], [, b]) => b - a)
              .map(([transition, count]) => (
                <div key={transition} className="flex justify-between items-center py-1">
                  <span className="text-gray-700 font-mono">{transition}</span>
                  <span className="font-semibold">{count}</span>
                </div>
              ))}
          </div>
        </div>
      </div>

      {/* Recent Audit Logs */}
      <div className="bg-white rounded-lg shadow p-6">
        <h2 className="text-xl font-semibold mb-4">Последняя активность</h2>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">Время</th>
                <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">Пользователь</th>
                <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">Действие</th>
                <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">Объект</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {(data.recentAuditLogs || []).slice(0, 10).map((log) => (
                <tr key={log.id} className="hover:bg-gray-50">
                  <td className="px-4 py-2 text-sm text-gray-500">
                    {formatDistanceToNow(new Date(log.timestamp), { addSuffix: true })}
                  </td>
                  <td className="px-4 py-2 text-sm text-gray-900">{log.userLogin}</td>
                  <td className="px-4 py-2 text-sm text-gray-500">{log.actionType}</td>
                  <td className="px-4 py-2 text-sm text-gray-500">{log.entityType}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

function MetricCard({
  title,
  value,
  subtitle,
  icon,
  color,
  highlight
}: {
  title: string;
  value: string | number;
  subtitle?: string;
  icon: string;
  color: string;
  highlight?: boolean;
}) {
  return (
    <div className={`bg-white rounded-lg shadow p-6 ${highlight ? 'ring-2 ring-red-500' : ''}`}>
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm font-medium text-gray-600">{title}</p>
          <p className="mt-2 text-3xl font-semibold text-gray-900">{value}</p>
          {subtitle && (
            <p className="mt-1 text-xs text-gray-500">{subtitle}</p>
          )}
        </div>
        <div className={`${color} rounded-full p-3 text-white text-2xl`}>
          {icon}
        </div>
      </div>
    </div>
  );
}