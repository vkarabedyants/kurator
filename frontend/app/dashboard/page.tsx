'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { useTranslations } from 'next-intl';
import Link from 'next/link';
import { dashboardApi } from '@/services/api';
import { logger } from '@/lib/logger';
import { CuratorDashboard, AdminDashboard, UserRole } from '@/types/api';
import { formatDistanceToNow, format } from 'date-fns';

export default function DashboardPage() {
  const [user, setUser] = useState<any>(null);
  const [dashboardData, setDashboardData] = useState<CuratorDashboard | AdminDashboard | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const router = useRouter();
  const t = useTranslations('dashboard');

  useEffect(() => {
    logger.navigation('unknown', '/dashboard', { component: 'DashboardPage' });
    logger.info('Dashboard page loaded', { component: 'DashboardPage' });

    const userStr = localStorage.getItem('user');
    if (!userStr) {
      logger.warn('No user found in localStorage, redirecting to login', { component: 'DashboardPage' });
      router.push('/login');
      return;
    }
    const userData = JSON.parse(userStr);
    logger.debug('User loaded from localStorage', {
      component: 'DashboardPage',
      userId: userData.id,
      role: userData.role,
    });
    setUser(userData);
    loadDashboard(userData);
  }, [router]);

  const loadDashboard = async (userData: any) => {
    const timer = logger.startTimer('dashboard_load');
    logger.info('Loading dashboard data', {
      component: 'DashboardPage',
      userId: userData.id,
      role: userData.role,
    });

    try {
      setLoading(true);
      setError(null);

      if (userData.role === UserRole.Admin) {
        logger.debug('Fetching admin dashboard', { component: 'DashboardPage' });
        const data = await dashboardApi.getAdminDashboard();
        logger.info('Admin dashboard loaded', {
          component: 'DashboardPage',
          totalContacts: data.totalContacts,
          totalBlocks: data.totalBlocks,
          totalUsers: data.totalUsers,
        });
        setDashboardData(data);
      } else if (userData.role === UserRole.Curator) {
        logger.debug('Fetching curator dashboard', { component: 'DashboardPage' });
        const data = await dashboardApi.getCuratorDashboard();
        logger.info('Curator dashboard loaded', {
          component: 'DashboardPage',
          totalContacts: data.totalContacts,
          overdueContacts: data.overdueContacts,
        });
        setDashboardData(data);
      } else if (userData.role === UserRole.ThreatAnalyst) {
        logger.debug('Threat analyst redirecting to watchlist', { component: 'DashboardPage' });
        router.push('/watchlist');
        return;
      }
      timer();
    } catch (err) {
      timer();
      setError(t('error_loading'));
      logger.error('Dashboard loading failed', {
        component: 'DashboardPage',
        userId: userData.id,
        role: userData.role,
      }, err);
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
  const isCurator = user.role === UserRole.Curator;

  return (
    <div className="p-6">
      <div className="mb-6">
        <h1 className="text-3xl font-bold text-slate-800">
          {t('welcome_user', { login: user.login })}
        </h1>
        <p className="text-slate-600 mt-1">
          {t('role')}: <span className="font-semibold text-slate-800">{user.role}</span>
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
  const t = useTranslations('dashboard');

  return (
    <div className="space-y-6">
      {/* Key Metrics */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <MetricCard
          title={t('stats_total_contacts')}
          value={data.totalContacts}
          icon="👥"
          color="bg-blue-500"
        />
        <MetricCard
          title={t('stats_interactions_month')}
          value={data.interactionsLastMonth}
          icon="🤝"
          color="bg-green-500"
        />
        <MetricCard
          title={t('stats_average_interval')}
          value={t('stats_average_interval_days', { days: data.averageInteractionInterval })}
          icon="⏱️"
          color="bg-yellow-500"
        />
        <MetricCard
          title={t('stats_overdue_contacts')}
          value={data.overdueContacts}
          icon="⚠️"
          color="bg-red-500"
          highlight={data.overdueContacts > 0}
        />
      </div>

      {/* Contacts Requiring Attention */}
      {(data.contactsRequiringAttention?.length ?? 0) > 0 && (
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold mb-4 text-red-600">
            ⚠️ {t('contacts_requiring_attention')}
          </h2>
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-200">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-4 py-2 text-left text-xs font-semibold text-slate-700 uppercase">{t('table_contact_id')}</th>
                  <th className="px-4 py-2 text-left text-xs font-semibold text-slate-700 uppercase">{t('table_name')}</th>
                  <th className="px-4 py-2 text-left text-xs font-semibold text-slate-700 uppercase">{t('table_status')}</th>
                  <th className="px-4 py-2 text-left text-xs font-semibold text-slate-700 uppercase">{t('table_days_overdue')}</th>
                  <th className="px-4 py-2 text-left text-xs font-semibold text-slate-700 uppercase">{t('table_next_contact')}</th>
                  <th className="px-4 py-2 text-left text-xs font-semibold text-slate-700 uppercase">{t('table_action')}</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-slate-200">
                {(data.contactsRequiringAttention ?? []).map((contact) => (
                  <tr key={contact.id} className="hover:bg-slate-50">
                    <td className="px-4 py-2 text-sm font-medium text-slate-900">{contact.contactId}</td>
                    <td className="px-4 py-2 text-sm text-slate-600">{contact.fullName}</td>
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
                    <td className="px-4 py-2 text-sm text-red-600 font-medium">{t('days_overdue', { days: contact.daysOverdue })}</td>
                    <td className="px-4 py-2 text-sm text-slate-600">
                      {contact.nextTouchDate ? format(new Date(contact.nextTouchDate), 'dd MMM yyyy') : t('not_set')}
                    </td>
                    <td className="px-4 py-2 text-sm">
                      <Link
                        href={`/contacts/${contact.id}`}
                        className="text-indigo-600 hover:text-indigo-900"
                      >
                        {t('view')}
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
          <h2 className="text-xl font-semibold mb-4 text-slate-800">{t('recent_interactions')}</h2>
          <div className="space-y-3">
            {(data.recentInteractions?.length ?? 0) === 0 ? (
              <p className="text-slate-500">{t('no_recent_interactions')}</p>
            ) : (
              (data.recentInteractions ?? []).map((interaction) => (
                <div key={interaction.id} className="border-l-4 border-indigo-500 pl-4 py-2">
                  <div className="flex justify-between items-start">
                    <div>
                      <p className="font-medium text-slate-900">{interaction.contactName}</p>
                      <p className="text-sm text-slate-600">ID: {interaction.contactId}</p>
                      <p className="text-xs text-slate-500">
                        {formatDistanceToNow(new Date(interaction.interactionDate), { addSuffix: true })}
                      </p>
                    </div>
                    <div className="text-right">
                      <span className="text-xs text-slate-600">{interaction.interactionTypeId}</span>
                      <br />
                      <span className={`text-xs px-2 py-1 rounded ${
                        interaction.resultId === 'Positive' ? 'bg-green-100 text-green-800' :
                        interaction.resultId === 'Negative' ? 'bg-red-100 text-red-800' :
                        'bg-slate-100 text-slate-800'
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
          <h2 className="text-xl font-semibold mb-4 text-slate-800">{t('contacts_by_status')}</h2>
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
                  <span className="text-slate-700">{t('status_label', { status })}</span>
                </div>
                <span className="font-semibold text-slate-900">{count}</span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

function AdminDashboardView({ data }: { data: AdminDashboard }) {
  const t = useTranslations('dashboard');

  return (
    <div className="space-y-6">
      {/* Quick Actions for Admin */}
      <div className="bg-white rounded-lg shadow p-6">
        <h2 className="text-xl font-semibold mb-4 text-slate-800">{t('quick_actions_admin')}</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <Link
            href="/users"
            className="bg-blue-600 hover:bg-blue-700 text-white px-6 py-3 rounded-md text-center font-medium transition-colors"
          >
            👥 {t('action_manage_users')}
          </Link>
          <Link
            href="/blocks"
            className="bg-purple-600 hover:bg-purple-700 text-white px-6 py-3 rounded-md text-center font-medium transition-colors"
          >
            📦 {t('action_manage_blocks')}
          </Link>
          <Link
            href="/audit-log"
            className="bg-gray-600 hover:bg-gray-700 text-white px-6 py-3 rounded-md text-center font-medium transition-colors"
          >
            📝 {t('action_audit_log')}
          </Link>
        </div>
      </div>

      {/* Key Metrics */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <MetricCard
          title={t('stats_total_contacts')}
          value={data.totalContacts}
          subtitle={`+${data.newContactsLastMonth} ${t('stats_new_contacts_month')}`}
          icon="👥"
          color="bg-blue-500"
        />
        <MetricCard
          title={t('stats_total_interactions')}
          value={data.totalInteractions}
          subtitle={`${data.interactionsLastMonth} ${t('stats_interactions_last_month')}`}
          icon="🤝"
          color="bg-green-500"
        />
        <MetricCard
          title={t('stats_active_blocks')}
          value={data.totalBlocks}
          icon="📦"
          color="bg-purple-500"
        />
        <MetricCard
          title={t('stats_total_users')}
          value={data.totalUsers}
          icon="👤"
          color="bg-orange-500"
        />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Contacts by Block */}
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold mb-4 text-slate-800">{t('contacts_by_block')}</h2>
          <div className="space-y-2">
            {Object.entries(data.contactsByBlock || {})
              .sort(([, a], [, b]) => b - a)
              .slice(0, 5)
              .map(([block, count]) => (
                <div key={block} className="flex justify-between items-center">
                  <span className="text-slate-700">{block}</span>
                  <div className="flex items-center">
                    <div className="w-32 bg-slate-200 rounded-full h-2 mr-2">
                      <div
                        className="bg-indigo-600 h-2 rounded-full"
                        style={{ width: `${(count / data.totalContacts) * 100}%` }}
                      ></div>
                    </div>
                    <span className="font-semibold text-slate-900">{count}</span>
                  </div>
                </div>
              ))}
          </div>
        </div>

        {/* Top Curators */}
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold mb-4 text-slate-800">{t('top_curators')}</h2>
          <div className="space-y-2">
            {Object.entries(data.topCuratorsByActivity || {})
              .sort(([, a], [, b]) => b - a)
              .map(([curator, count], index) => (
                <div key={curator} className="flex justify-between items-center">
                  <div className="flex items-center">
                    <span className={`mr-2 ${index === 0 ? '🥇' : index === 1 ? '🥈' : index === 2 ? '🥉' : '🏅'}`}>
                    </span>
                    <span className="text-slate-700">{curator}</span>
                  </div>
                  <span className="font-semibold text-slate-900">{t('interactions_count', { count })}</span>
                </div>
              ))}
          </div>
        </div>
      </div>

      {/* Status Distribution */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold mb-4 text-slate-800">{t('contacts_by_status')}</h2>
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
                <div className="text-slate-600">{t('status_label', { status })}</div>
                <div className="text-xs text-slate-500">
                  {((count / data.totalContacts) * 100).toFixed(1)}%
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold mb-4 text-slate-800">{t('status_dynamics')}</h2>
          <div className="space-y-2 max-h-64 overflow-y-auto">
            {Object.entries(data.statusChangeDynamics || {})
              .sort(([, a], [, b]) => b - a)
              .map(([transition, count]) => (
                <div key={transition} className="flex justify-between items-center py-1">
                  <span className="text-slate-700 font-mono">{transition}</span>
                  <span className="font-semibold text-slate-900">{count}</span>
                </div>
              ))}
          </div>
        </div>
      </div>

      {/* Recent Audit Logs */}
      <div className="bg-white rounded-lg shadow p-6">
        <h2 className="text-xl font-semibold mb-4 text-slate-800">{t('last_activity')}</h2>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-slate-200">
            <thead className="bg-slate-50">
              <tr>
                <th className="px-4 py-2 text-left text-xs font-semibold text-slate-700 uppercase">{t('table_time')}</th>
                <th className="px-4 py-2 text-left text-xs font-semibold text-slate-700 uppercase">{t('table_user')}</th>
                <th className="px-4 py-2 text-left text-xs font-semibold text-slate-700 uppercase">{t('table_action_type')}</th>
                <th className="px-4 py-2 text-left text-xs font-semibold text-slate-700 uppercase">{t('table_object')}</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-slate-200">
              {(data.recentAuditLogs || []).slice(0, 10).map((log) => (
                <tr key={log.id} className="hover:bg-slate-50">
                  <td className="px-4 py-2 text-sm text-slate-600">
                    {formatDistanceToNow(new Date(log.timestamp), { addSuffix: true })}
                  </td>
                  <td className="px-4 py-2 text-sm text-slate-900">{log.userLogin}</td>
                  <td className="px-4 py-2 text-sm text-slate-600">{log.actionType}</td>
                  <td className="px-4 py-2 text-sm text-slate-600">{log.entityType}</td>
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
          <p className="text-sm font-medium text-slate-600">{title}</p>
          <p className="mt-2 text-3xl font-semibold text-slate-900">{value}</p>
          {subtitle && (
            <p className="mt-1 text-xs text-slate-500">{subtitle}</p>
          )}
        </div>
        <div className={`${color} rounded-full p-3 text-white text-2xl`}>
          {icon}
        </div>
      </div>
    </div>
  );
}