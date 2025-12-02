'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { referencesApi } from '@/services/api';
import { UserRole, ReferenceValue } from '@/types/api';

const REFERENCE_CATEGORIES = [
  { id: 'organizations', name: 'Организации', description: 'Государственные органы, компании и т.д.' },
  { id: 'influence_statuses', name: 'Статусы влияния', description: 'Описания статусов A, B, C, D' },
  { id: 'influence_types', name: 'Типы влияния', description: 'Типы влияния' },
  { id: 'communication_channels', name: 'Каналы коммуникации', description: 'Способы связи' },
  { id: 'contact_sources', name: 'Источники контактов', description: 'Как были получены контакты' },
  { id: 'interaction_types', name: 'Типы взаимодействия', description: 'Встреча, Звонок, Email и т.д.' },
  { id: 'interaction_results', name: 'Результаты взаимодействия', description: 'Позитивный, Негативный, Нейтральный и т.д.' },
  { id: 'block_statuses', name: 'Статусы блоков', description: 'Активный, Архивный' },
];

export default function ReferencesPage() {
  const [user, setUser] = useState<any>(null);
  const [selectedCategory, setSelectedCategory] = useState<string>('organizations');
  const [values, setValues] = useState<ReferenceValue[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showAddForm, setShowAddForm] = useState(false);
  const [editingValue, setEditingValue] = useState<ReferenceValue | null>(null);
  const [formData, setFormData] = useState({ value: '', description: '', displayOrder: 0 });
  const router = useRouter();

  useEffect(() => {
    const userStr = localStorage.getItem('user');
    if (!userStr) {
      router.push('/login');
      return;
    }
    const userData = JSON.parse(userStr);
    setUser(userData);

    if (userData.role !== UserRole.Admin) {
      router.push('/dashboard');
      return;
    }
  }, [router]);

  useEffect(() => {
    if (user && selectedCategory) {
      loadValues();
    }
  }, [selectedCategory, user]);

  const loadValues = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await referencesApi.getByCategory(selectedCategory);
      setValues(data || []);
    } catch (err) {
      setError('Не удалось загрузить справочные значения');
      console.error('Error loading values:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      const data = {
        category: selectedCategory,
        ...formData,
        isActive: true
      };

      if (editingValue) {
        await referencesApi.update(editingValue.id, data);
      } else {
        await referencesApi.create(data);
      }

      setShowAddForm(false);
      setEditingValue(null);
      setFormData({ value: '', description: '', displayOrder: 0 });
      loadValues();
    } catch (err) {
      console.error('Error saving value:', err);
      alert('Не удалось сохранить справочное значение');
    }
  };

  const handleEdit = (value: ReferenceValue) => {
    setEditingValue(value);
    setFormData({
      value: value.value,
      description: value.description || '',
      displayOrder: value.displayOrder || 0
    });
    setShowAddForm(true);
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Вы уверены, что хотите удалить это значение?')) return;

    try {
      await referencesApi.delete(id);
      loadValues();
    } catch (err) {
      console.error('Error deleting value:', err);
      alert('Не удалось удалить справочное значение. Возможно, оно используется.');
    }
  };

  const currentCategory = REFERENCE_CATEGORIES.find(c => c.id === selectedCategory);

  if (!user) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
      </div>
    );
  }

  return (
    <div className="p-6">
      <div className="mb-6">
        <h1 className="text-3xl font-bold text-gray-900">Управление справочниками</h1>
        <p className="text-gray-600 mt-1">Управление значениями выпадающих списков и системными настройками</p>
      </div>

      {/* Category Selector */}
      <div className="mb-6">
        <div className="flex flex-wrap gap-2">
          {REFERENCE_CATEGORIES.map((cat) => (
            <button
              key={cat.id}
              onClick={() => {
                setSelectedCategory(cat.id);
                setShowAddForm(false);
                setEditingValue(null);
              }}
              className={`px-4 py-2 rounded-md transition-colors ${
                selectedCategory === cat.id
                  ? 'bg-indigo-600 text-white'
                  : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
              }`}
            >
              {cat.name}
            </button>
          ))}
        </div>
        {currentCategory && (
          <p className="text-sm text-gray-500 mt-2">{currentCategory.description}</p>
        )}
      </div>

      {/* Add Button */}
      <div className="mb-4">
        <button
          onClick={() => {
            setShowAddForm(true);
            setEditingValue(null);
            setFormData({ value: '', description: '', displayOrder: 0 });
          }}
          className="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700"
        >
          + Добавить новое значение
        </button>
      </div>

      {/* Add/Edit Form */}
      {showAddForm && (
        <div className="bg-white rounded-lg shadow-md p-6 mb-6">
          <h2 className="text-xl font-semibold mb-4">
            {editingValue ? 'Редактировать справочное значение' : 'Добавить новое справочное значение'}
          </h2>
          <form onSubmit={handleSubmit}>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Значение *
                </label>
                <input
                  type="text"
                  value={formData.value}
                  onChange={(e) => setFormData({ ...formData, value: e.target.value })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Порядок отображения
                </label>
                <input
                  type="number"
                  value={formData.displayOrder}
                  onChange={(e) => setFormData({ ...formData, displayOrder: parseInt(e.target.value) || 0 })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
                />
              </div>
              <div className="md:col-span-2">
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Описание
                </label>
                <textarea
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  rows={3}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
                />
              </div>
            </div>
            <div className="mt-4 flex gap-2">
              <button
                type="submit"
                className="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700"
              >
                {editingValue ? 'Обновить' : 'Создать'}
              </button>
              <button
                type="button"
                onClick={() => {
                  setShowAddForm(false);
                  setEditingValue(null);
                }}
                className="px-4 py-2 bg-gray-300 text-gray-700 rounded-md hover:bg-gray-400"
              >
                Отмена
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Values List */}
      <div className="bg-white rounded-lg shadow">
        {loading ? (
          <div className="p-6 text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600 mx-auto"></div>
          </div>
        ) : error ? (
          <div className="p-6 text-center text-red-500">{error}</div>
        ) : values.length === 0 ? (
          <div className="p-6 text-center text-gray-500">
            Для этой категории значений не найдено. Нажмите &quot;Добавить новое значение&quot;, чтобы создать.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Порядок
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Значение
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Описание
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Статус
                  </th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Действия
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {values
                  .sort((a, b) => (a.displayOrder || 0) - (b.displayOrder || 0))
                  .map((value) => (
                  <tr key={value.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {value.displayOrder || 0}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                      {value.value}
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-500">
                      {value.description || '-'}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm">
                      <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                        value.isActive
                          ? 'bg-green-100 text-green-800'
                          : 'bg-gray-100 text-gray-800'
                      }`}>
                        {value.isActive ? 'Активный' : 'Неактивный'}
                      </span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                      <button
                        onClick={() => handleEdit(value)}
                        className="text-indigo-600 hover:text-indigo-900 mr-3"
                      >
                        Редактировать
                      </button>
                      <button
                        onClick={() => handleDelete(value.id)}
                        className="text-red-600 hover:text-red-900"
                      >
                        Удалить
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}