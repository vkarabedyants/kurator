'use client';

import React, { useState, useEffect } from 'react';
import { useRouter, useParams } from 'next/navigation';
import { ArrowLeft, Save, User, Building2, Hash, Phone, FileText, AlertCircle, Loader2 } from 'lucide-react';
import { api } from '@/services/api';
import { UpdateContactDto, ContactDto, InfluenceStatus, InfluenceType, CommunicationChannel, ContactSource } from '@/types/api';

export default function EditContactPage() {
  const router = useRouter();
  const params = useParams();
  const contactId = params.id as string;

  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [contact, setContact] = useState<ContactDto | null>(null);
  const [formData, setFormData] = useState<UpdateContactDto>({
    fullName: '',
    organization: '',
    position: '',
    influenceStatus: InfluenceStatus.C,
    influenceType: InfluenceType.Functional,
    howCanHelp: '',
    communicationChannel: CommunicationChannel.Personal,
    contactSource: ContactSource.PersonalAcquaintance,
    nextTouchDate: '',
    notes: ''
  });

  useEffect(() => {
    loadContact();
  }, [contactId]);

  const loadContact = async () => {
    try {
      setIsLoading(true);
      const response = await api.get(`/contacts/${contactId}`);
      if (response.data) {
        setContact(response.data);
        setFormData({
          fullName: response.data.fullName || '',
          organization: response.data.organization || '',
          position: response.data.position || '',
          influenceStatus: response.data.influenceStatus,
          influenceType: response.data.influenceType,
          howCanHelp: response.data.howCanHelp || '',
          communicationChannel: response.data.communicationChannel,
          contactSource: response.data.contactSource,
          nextTouchDate: response.data.nextTouchDate ? response.data.nextTouchDate.split('T')[0] : '',
          notes: response.data.notes || ''
        });
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Не удалось загрузить контакт');
    } finally {
      setIsLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSaving(true);
    setError(null);

    try {
      await api.put(`/contacts/${contactId}`, formData);
      router.push(`/contacts/${contactId}`);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Не удалось обновить контакт');
    } finally {
      setIsSaving(false);
    }
  };

  const handleChange = (field: keyof UpdateContactDto, value: any) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="flex items-center gap-2">
          <Loader2 className="h-6 w-6 animate-spin text-blue-600" />
          <span className="text-gray-600">Загрузка контакта...</span>
        </div>
      </div>
    );
  }

  if (!contact) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <h2 className="text-2xl font-bold text-gray-900 mb-2">Контакт не найден</h2>
          <button
            onClick={() => router.push('/contacts')}
            className="text-blue-600 hover:text-blue-800"
          >
            Вернуться к контактам
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-4xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <button
            onClick={() => router.back()}
            className="inline-flex items-center gap-2 text-gray-600 hover:text-gray-900 mb-4"
          >
            <ArrowLeft className="h-4 w-4" />
            Назад
          </button>
          <h1 className="text-3xl font-bold text-gray-900">Редактировать контакт</h1>
          <p className="text-gray-600 mt-2">Редактирование: {contact.contactId}</p>
        </div>

        {/* Error Alert */}
        {error && (
          <div className="mb-6 bg-red-50 border border-red-200 rounded-lg p-4">
            <div className="flex items-center gap-2">
              <AlertCircle className="h-5 w-5 text-red-600" />
              <p className="text-red-800">{error}</p>
            </div>
          </div>
        )}

        {/* Form */}
        <form onSubmit={handleSubmit} className="bg-white shadow rounded-lg p-6 space-y-6">
          {/* Basic Information */}
          <div className="space-y-4">
            <h2 className="text-lg font-semibold text-gray-900 border-b pb-2">Основная информация</h2>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  <User className="inline h-4 w-4 mr-1" />
                  Полное имя *
                </label>
                <input
                  type="text"
                  required
                  value={formData.fullName}
                  onChange={(e) => handleChange('fullName', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="Введите полное имя"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  <Building2 className="inline h-4 w-4 mr-1" />
                  Организация *
                </label>
                <input
                  type="text"
                  required
                  value={formData.organization}
                  onChange={(e) => handleChange('organization', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="Введите организацию"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  <Hash className="inline h-4 w-4 mr-1" />
                  Должность/Роль *
                </label>
                <input
                  type="text"
                  required
                  value={formData.position}
                  onChange={(e) => handleChange('position', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="Введите должность или роль"
                />
              </div>
            </div>
          </div>

          {/* Influence Details */}
          <div className="space-y-4">
            <h2 className="text-lg font-semibold text-gray-900 border-b pb-2">Детали влияния</h2>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Статус влияния *
                </label>
                <select
                  required
                  value={formData.influenceStatus}
                  onChange={(e) => handleChange('influenceStatus', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value={InfluenceStatus.A}>A - Высокое влияние</option>
                  <option value={InfluenceStatus.B}>B - Среднее влияние</option>
                  <option value={InfluenceStatus.C}>C - Низкое влияние</option>
                  <option value={InfluenceStatus.D}>D - Нет влияния</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Тип влияния *
                </label>
                <select
                  required
                  value={formData.influenceType}
                  onChange={(e) => handleChange('influenceType', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value={InfluenceType.Navigational}>Навигационный - Обеспечивает доступ</option>
                  <option value={InfluenceType.Interpretational}>Интерпретационный - Помогает понять позиции</option>
                  <option value={InfluenceType.Functional}>Функциональный - Помогает решать вопросы</option>
                  <option value={InfluenceType.Reputational}>Репутационный - Влияет на общественное восприятие</option>
                  <option value={InfluenceType.Analytical}>Аналитический - Предоставляет стратегическую оценку</option>
                </select>
              </div>

              <div className="md:col-span-2">
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Чем может помочь
                </label>
                <textarea
                  value={formData.howCanHelp}
                  onChange={(e) => handleChange('howCanHelp', e.target.value)}
                  rows={3}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="Опишите, чем этот контакт может быть полезен..."
                />
              </div>
            </div>
          </div>

          {/* Communication Details */}
          <div className="space-y-4">
            <h2 className="text-lg font-semibold text-gray-900 border-b pb-2">Детали коммуникации</h2>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  <Phone className="inline h-4 w-4 mr-1" />
                  Канал коммуникации *
                </label>
                <select
                  required
                  value={formData.communicationChannel}
                  onChange={(e) => handleChange('communicationChannel', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value={CommunicationChannel.Official}>Официальный</option>
                  <option value={CommunicationChannel.ThroughIntermediary}>Через посредника</option>
                  <option value={CommunicationChannel.ThroughAssociation}>Через ассоциацию</option>
                  <option value={CommunicationChannel.Personal}>Личный</option>
                  <option value={CommunicationChannel.Legal}>Правовой</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Источник контакта *
                </label>
                <select
                  required
                  value={formData.contactSource}
                  onChange={(e) => handleChange('contactSource', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value={ContactSource.PersonalAcquaintance}>Личное знакомство</option>
                  <option value={ContactSource.Association}>Ассоциация</option>
                  <option value={ContactSource.Recommendation}>Рекомендация</option>
                  <option value={ContactSource.Event}>Мероприятие</option>
                  <option value={ContactSource.Media}>СМИ</option>
                  <option value={ContactSource.Other}>Другое</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Дата следующего контакта
                </label>
                <input
                  type="date"
                  value={formData.nextTouchDate}
                  onChange={(e) => handleChange('nextTouchDate', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
            </div>
          </div>

          {/* Notes */}
          <div className="space-y-4">
            <h2 className="text-lg font-semibold text-gray-900 border-b pb-2">Дополнительная информация</h2>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                <FileText className="inline h-4 w-4 mr-1" />
                Конфиденциальные заметки
              </label>
              <textarea
                value={formData.notes}
                onChange={(e) => handleChange('notes', e.target.value)}
                rows={4}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Введите конфиденциальные заметки об этом контакте..."
              />
              <p className="text-sm text-gray-500 mt-1">Эта информация будет зашифрована</p>
            </div>
          </div>

          {/* Metadata */}
          <div className="bg-gray-50 rounded-lg p-4 text-sm text-gray-600 space-y-1">
            <div>Создано: {new Date(contact.createdAt).toLocaleString()}</div>
            {contact.updatedAt && (
              <>
                <div>Последнее обновление: {new Date(contact.updatedAt).toLocaleString()}</div>
                {contact.updatedBy && <div>Обновлено: {contact.updatedBy}</div>}
              </>
            )}
          </div>

          {/* Actions */}
          <div className="flex justify-end gap-4 pt-4 border-t">
            <button
              type="button"
              onClick={() => router.back()}
              className="px-4 py-2 text-gray-700 bg-gray-100 hover:bg-gray-200 rounded-lg transition-colors"
            >
              Отмена
            </button>
            <button
              type="submit"
              disabled={isSaving}
              className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50 flex items-center gap-2"
            >
              <Save className="h-4 w-4" />
              {isSaving ? 'Сохранение...' : 'Сохранить изменения'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}