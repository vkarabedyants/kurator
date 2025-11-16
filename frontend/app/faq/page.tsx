'use client';

import React, { useState, useEffect } from 'react';
import { Book, ChevronDown, ChevronUp, Edit, Plus, Search, Shield, Users, FileText } from 'lucide-react';
import { api } from '@/services/api';
import { FAQDto, FAQVisibility } from '@/types/api';
import { useRouter } from 'next/navigation';

export default function FAQPage() {
  const router = useRouter();
  const [faqs, setFaqs] = useState<FAQDto[]>([]);
  const [filteredFaqs, setFilteredFaqs] = useState<FAQDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [expandedItems, setExpandedItems] = useState<Set<number>>(new Set());
  const [userRole, setUserRole] = useState<string>('');

  useEffect(() => {
    loadFAQs();
    checkUserRole();
  }, []);

  useEffect(() => {
    filterFAQs();
  }, [searchTerm, faqs]);

  const loadFAQs = async () => {
    try {
      setIsLoading(true);
      const response = await api.get('/faq');
      if (response.data?.items) {
        setFaqs(response.data.items);
      }
    } catch (error) {
      console.error('Не удалось загрузить FAQ:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const checkUserRole = async () => {
    try {
      const response = await api.get('/auth/me');
      if (response.data?.role) {
        setUserRole(response.data.role);
      }
    } catch (error) {
      console.error('Не удалось получить роль пользователя:', error);
    }
  };

  const filterFAQs = () => {
    if (!searchTerm) {
      setFilteredFaqs(faqs);
      return;
    }

    const filtered = faqs.filter(faq =>
      faq.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
      faq.content.toLowerCase().includes(searchTerm.toLowerCase())
    );
    setFilteredFaqs(filtered);
  };

  const toggleExpand = (id: number) => {
    const newExpanded = new Set(expandedItems);
    if (newExpanded.has(id)) {
      newExpanded.delete(id);
    } else {
      newExpanded.add(id);
    }
    setExpandedItems(newExpanded);
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Вы уверены, что хотите удалить этот элемент FAQ?')) {
      return;
    }

    try {
      await api.delete(`/faq/${id}`);
      await loadFAQs();
    } catch (error) {
      console.error('Не удалось удалить FAQ:', error);
    }
  };

  const getVisibilityIcon = (visibility: FAQVisibility) => {
    switch (visibility) {
      case FAQVisibility.All:
        return <Users className="h-4 w-4" />;
      case FAQVisibility.CuratorsOnly:
        return <Shield className="h-4 w-4" />;
      case FAQVisibility.AdminOnly:
        return <Shield className="h-4 w-4 text-red-600" />;
      default:
        return null;
    }
  };

  const getVisibilityLabel = (visibility: FAQVisibility) => {
    switch (visibility) {
      case FAQVisibility.All:
        return 'Все пользователи';
      case FAQVisibility.CuratorsOnly:
        return 'Только кураторы';
      case FAQVisibility.AdminOnly:
        return 'Только администраторы';
      default:
        return visibility;
    }
  };

  const categoryGroups = filteredFaqs.reduce((groups, faq) => {
    const category = faq.category || 'Общее';
    if (!groups[category]) {
      groups[category] = [];
    }
    groups[category].push(faq);
    return groups;
  }, {} as Record<string, FAQDto[]>);

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-4xl mx-auto">
        {/* Header */}
        <div className="mb-8">
          <div className="flex justify-between items-center mb-4">
            <div>
              <h1 className="text-3xl font-bold text-gray-900 flex items-center gap-2">
                <Book className="h-8 w-8 text-blue-600" />
                Часто задаваемые вопросы / Правила
              </h1>
              <p className="text-gray-600 mt-2">Руководства и инструкции по использованию системы</p>
            </div>
            {userRole === 'Admin' && (
              <button
                onClick={() => router.push('/faq/new')}
                className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 flex items-center gap-2"
              >
                <Plus className="h-4 w-4" />
                Добавить FAQ
              </button>
            )}
          </div>

          {/* Search */}
          <div className="bg-white rounded-lg shadow p-4">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
              <input
                type="text"
                placeholder="Поиск по FAQ..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10 pr-3 py-2 w-full border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>
        </div>

        {/* Content */}
        {isLoading ? (
          <div className="flex justify-center items-center h-64">
            <div className="text-gray-500">Загрузка FAQ...</div>
          </div>
        ) : filteredFaqs.length === 0 ? (
          <div className="bg-white rounded-lg shadow p-12 text-center">
            <FileText className="h-12 w-12 text-gray-400 mx-auto mb-4" />
            <h3 className="text-lg font-medium text-gray-900 mb-2">Элементы FAQ не найдены</h3>
            <p className="text-gray-600">
              {searchTerm ? 'Попробуйте изменить параметры поиска' : 'Элементы FAQ еще не добавлены'}
            </p>
          </div>
        ) : (
          <div className="space-y-6">
            {Object.entries(categoryGroups).map(([category, items]) => (
              <div key={category} className="bg-white rounded-lg shadow">
                <div className="p-4 border-b bg-gray-50">
                  <h2 className="text-lg font-semibold text-gray-900">{category}</h2>
                </div>
                <div className="divide-y">
                  {items.map((faq) => (
                    <div key={faq.id} className="p-4 hover:bg-gray-50 transition-colors">
                      <div className="flex justify-between items-start">
                        <div className="flex-1">
                          <button
                            onClick={() => toggleExpand(faq.id)}
                            className="w-full text-left focus:outline-none focus:ring-2 focus:ring-blue-500 rounded"
                          >
                            <div className="flex items-center justify-between">
                              <h3 className="font-medium text-gray-900 flex items-center gap-2">
                                {faq.title}
                                <span className="inline-flex items-center gap-1 px-2 py-1 text-xs bg-gray-100 text-gray-600 rounded-full">
                                  {getVisibilityIcon(faq.visibility)}
                                  {getVisibilityLabel(faq.visibility)}
                                </span>
                              </h3>
                              <div className="flex items-center gap-2">
                                {userRole === 'Admin' && (
                                  <div className="flex gap-2" onClick={(e) => e.stopPropagation()}>
                                    <button
                                      onClick={() => router.push(`/faq/${faq.id}/edit`)}
                                      className="p-1 text-gray-600 hover:text-blue-600"
                                      title="Редактировать"
                                    >
                                      <Edit className="h-4 w-4" />
                                    </button>
                                  </div>
                                )}
                                {expandedItems.has(faq.id) ? (
                                  <ChevronUp className="h-5 w-5 text-gray-400" />
                                ) : (
                                  <ChevronDown className="h-5 w-5 text-gray-400" />
                                )}
                              </div>
                            </div>
                          </button>

                          {expandedItems.has(faq.id) && (
                            <div className="mt-4 pl-4 border-l-4 border-blue-200">
                              <div className="prose prose-sm max-w-none text-gray-600">
                                <div dangerouslySetInnerHTML={{ __html: formatContent(faq.content) }} />
                              </div>
                              {faq.updatedAt && (
                                <div className="mt-4 text-xs text-gray-500">
                                  Последнее обновление: {new Date(faq.updatedAt).toLocaleDateString()}
                                </div>
                              )}
                            </div>
                          )}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Categories Legend */}
        <div className="mt-12 bg-white rounded-lg shadow p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Понимание правил</h2>
          <div className="grid gap-4">
            <div className="flex items-start gap-3">
              <div className="p-2 bg-blue-100 rounded-lg">
                <Book className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <h3 className="font-medium text-gray-900">Общие рекомендации</h3>
                <p className="text-sm text-gray-600 mt-1">
                  Основные правила и процедуры, применимые ко всем пользователям системы
                </p>
              </div>
            </div>
            <div className="flex items-start gap-3">
              <div className="p-2 bg-green-100 rounded-lg">
                <Users className="h-5 w-5 text-green-600" />
              </div>
              <div>
                <h3 className="font-medium text-gray-900">Инструкции для кураторов</h3>
                <p className="text-sm text-gray-600 mt-1">
                  Специальные рекомендации для кураторов по управлению контактами и взаимодействиями
                </p>
              </div>
            </div>
            <div className="flex items-start gap-3">
              <div className="p-2 bg-purple-100 rounded-lg">
                <Shield className="h-5 w-5 text-purple-600" />
              </div>
              <div>
                <h3 className="font-medium text-gray-900">Политики безопасности</h3>
                <p className="text-sm text-gray-600 mt-1">
                  Важные меры безопасности и рекомендации по защите данных
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

function formatContent(content: string): string {
  // Convert markdown-like syntax to HTML
  return content
    .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
    .replace(/\*(.*?)\*/g, '<em>$1</em>')
    .replace(/\n\n/g, '</p><p>')
    .replace(/\n/g, '<br>')
    .replace(/^/, '<p>')
    .replace(/$/, '</p>')
    .replace(/- (.*?)(<br>|<\/p>)/g, '<li>$1</li>')
    .replace(/<li>/g, '<ul><li>')
    .replace(/<\/li>(?!.*<li>)/g, '</li></ul>');
}