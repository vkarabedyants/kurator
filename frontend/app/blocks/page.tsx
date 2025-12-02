'use client';

import { useState, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import MainLayout from '@/components/layout/MainLayout';
import { blocksApi, usersApi } from '@/services/api';
import { Block, User, BlockStatus } from '@/types/api';

export default function BlocksPage() {
  const t = useTranslations('blocks');
  const tCommon = useTranslations('common');
  const [blocks, setBlocks] = useState<Block[]>([]);
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showAddForm, setShowAddForm] = useState(false);
  const [editingBlock, setEditingBlock] = useState<Block | null>(null);
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    code: '',
    status: BlockStatus.Active,
    primaryCuratorId: 0,
    backupCuratorId: 0,
  });

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      const [blocksData, usersData] = await Promise.all([
        blocksApi.getAll(),
        usersApi.getAll()
      ]);
      setBlocks(blocksData);
      setUsers(usersData.filter(u => u.role === 'Curator'));
    } catch (err: unknown) {
      const error = err as { message?: string };
      setError(error.message || t('load_error'));
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editingBlock) {
        // Update block first
        await blocksApi.update(editingBlock.id, {
          name: formData.name,
          description: formData.description,
          status: formData.status.toString()
        });
        // Handle curator assignments separately if needed
        // Note: Curator assignments are now handled via /blocks/{id}/curators endpoint
      } else {
        // Create block
        const result = await blocksApi.create({
          name: formData.name,
          code: formData.code,
          description: formData.description,
          status: formData.status.toString()
        });
        // Assign curators after block creation if specified
        if (result.id && formData.primaryCuratorId > 0) {
          await blocksApi.assignCurator(result.id, {
            userId: formData.primaryCuratorId,
            curatorType: 'Primary'
          });
        }
        if (result.id && formData.backupCuratorId > 0) {
          await blocksApi.assignCurator(result.id, {
            userId: formData.backupCuratorId,
            curatorType: 'Backup'
          });
        }
      }
      setShowAddForm(false);
      setEditingBlock(null);
      resetForm();
      fetchData();
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } }; message?: string };
      alert(t('save_error') + ': ' + (error.response?.data?.message || error.message));
    }
  };

  const handleEdit = (block: Block) => {
    setEditingBlock(block);
    // Find primary and backup curators from the curators array
    const primaryCurator = block.curators?.find(c => c.curatorType === 'Primary');
    const backupCurator = block.curators?.find(c => c.curatorType === 'Backup');

    setFormData({
      name: block.name,
      description: block.description || '',
      code: block.code,
      status: block.status as BlockStatus,
      primaryCuratorId: primaryCurator?.userId || 0,
      backupCuratorId: backupCurator?.userId || 0,
    });
    setShowAddForm(true);
  };

  const handleDelete = async (id: number) => {
    if (!window.confirm(t('delete_confirm'))) {
      return;
    }
    try {
      await blocksApi.delete(id);
      fetchData();
    } catch (err: unknown) {
      const error = err as { message?: string };
      alert(t('delete_error') + ': ' + error.message);
    }
  };

  const resetForm = () => {
    setFormData({
      name: '',
      description: '',
      code: '',
      status: BlockStatus.Active,
      primaryCuratorId: 0,
      backupCuratorId: 0,
    });
  };

  const getStatusBadge = (status: BlockStatus) => {
    return status === BlockStatus.Active
      ? 'bg-green-100 text-green-800'
      : 'bg-gray-100 text-gray-800';
  };

  return (
    <MainLayout>
      <div className="bg-white shadow rounded-lg">
        {/* Header */}
        <div className="px-6 py-4 border-b border-gray-200">
          <div className="flex items-center justify-between">
            <h2 className="text-xl font-semibold text-gray-900">{t('management')}</h2>
            <button
              onClick={() => setShowAddForm(true)}
              className="bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-md text-sm font-medium"
            >
              {t('add_new')}
            </button>
          </div>
        </div>

        {/* Add/Edit Form */}
        {showAddForm && (
          <div className="px-6 py-4 border-b border-gray-200 bg-gray-50">
            <h3 className="text-lg font-medium text-gray-900 mb-4">
              {editingBlock ? t('edit_block') : t('add_block')}
            </h3>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700">{t('name_required')}</label>
                  <input
                    type="text"
                    value={formData.name}
                    onChange={(e) => setFormData({...formData, name: e.target.value})}
                    className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
                    required
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">{t('code_required')}</label>
                  <input
                    type="text"
                    value={formData.code}
                    onChange={(e) => setFormData({...formData, code: e.target.value.toUpperCase()})}
                    className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
                    placeholder={t('code_placeholder')}
                    required
                    maxLength={10}
                  />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">{t('description')}</label>
                <textarea
                  value={formData.description}
                  onChange={(e) => setFormData({...formData, description: e.target.value})}
                  rows={2}
                  className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
                />
              </div>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700">{tCommon('status')}</label>
                  <select
                    value={formData.status}
                    onChange={(e) => setFormData({...formData, status: e.target.value as BlockStatus})}
                    className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
                  >
                    <option value={BlockStatus.Active}>{t('status_active')}</option>
                    <option value={BlockStatus.Archived}>{t('status_archived')}</option>
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">{t('primary_curator')}</label>
                  <select
                    value={formData.primaryCuratorId}
                    onChange={(e) => setFormData({...formData, primaryCuratorId: Number(e.target.value)})}
                    className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
                  >
                    <option value={0}>{t('not_assigned')}</option>
                    {users.map(user => (
                      <option key={user.id} value={user.id}>{user.login}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">{t('backup_curator')}</label>
                  <select
                    value={formData.backupCuratorId}
                    onChange={(e) => setFormData({...formData, backupCuratorId: Number(e.target.value)})}
                    className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
                  >
                    <option value={0}>{t('not_assigned')}</option>
                    {users.filter(u => u.id !== formData.primaryCuratorId).map(user => (
                      <option key={user.id} value={user.id}>{user.login}</option>
                    ))}
                  </select>
                </div>
              </div>
              <div className="flex justify-end space-x-3">
                <button
                  type="button"
                  onClick={() => {
                    setShowAddForm(false);
                    setEditingBlock(null);
                    resetForm();
                  }}
                  className="px-4 py-2 border border-gray-300 rounded-md text-sm font-medium text-gray-700 bg-white hover:bg-gray-50"
                >
                  {tCommon('cancel')}
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 border border-transparent rounded-md text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700"
                >
                  {editingBlock ? t('update') : t('create')}
                </button>
              </div>
            </form>
          </div>
        )}

        {/* Content */}
        <div className="px-6 py-4">
          {loading ? (
            <div className="flex justify-center py-12">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
            </div>
          ) : error ? (
            <div className="text-center py-12 text-red-600">
              {error}
            </div>
          ) : blocks.length === 0 ? (
            <div className="text-center py-12 text-gray-500">
              {t('no_blocks')}
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {blocks.map((block) => (
                <div key={block.id} className="border rounded-lg p-4 hover:shadow-md transition">
                  <div className="flex items-start justify-between mb-3">
                    <div>
                      <h3 className="text-lg font-semibold text-gray-900">{block.name}</h3>
                      <p className="text-sm text-gray-600">{t('code')}: {block.code}</p>
                    </div>
                    <span className={`px-2 py-1 text-xs font-semibold rounded-full ${getStatusBadge(block.status)}`}>
                      {block.status === BlockStatus.Active ? t('status_active') : t('status_archived')}
                    </span>
                  </div>
                  {block.description && (
                    <p className="text-sm text-gray-600 mb-3">{block.description}</p>
                  )}
                  <div className="space-y-1 text-sm">
                    <p className="text-gray-700">
                      <span className="font-medium">{t('primary_curator')}:</span>{' '}
                      {block.curators?.find(c => c.curatorType === 'Primary')?.userLogin || t('not_assigned')}
                    </p>
                    <p className="text-gray-700">
                      <span className="font-medium">{t('backup_curator')}:</span>{' '}
                      {block.curators?.find(c => c.curatorType === 'Backup')?.userLogin || t('not_assigned')}
                    </p>
                    <p className="text-gray-500 text-xs">
                      {t('created_at')}: {new Date(block.createdAt).toLocaleDateString()}
                    </p>
                  </div>
                  <div className="mt-4 flex justify-end space-x-2">
                    <button
                      onClick={() => handleEdit(block)}
                      className="text-indigo-600 hover:text-indigo-900 text-sm font-medium"
                    >
                      {tCommon('edit')}
                    </button>
                    <button
                      onClick={() => handleDelete(block.id)}
                      className="text-red-600 hover:text-red-900 text-sm font-medium"
                    >
                      {tCommon('delete')}
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </MainLayout>
  );
}