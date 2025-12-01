'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { contactsApi, blocksApi, usersApi, referencesApi } from '@/services/api';
import { encryptField, getBlockRecipients, KeyManager } from '@/lib/encryption';

interface ContactFormProps {
  contactId?: number;
  mode: 'create' | 'edit';
}

export default function ContactForm({ contactId, mode }: ContactFormProps) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [blocks, setBlocks] = useState<any[]>([]);
  const [users, setUsers] = useState<any[]>([]);
  const [references, setReferences] = useState<{
    organizations: any[];
    statuses: any[];
    types: any[];
    channels: any[];
    sources: any[];
  }>({
    organizations: [],
    statuses: [],
    types: [],
    channels: [],
    sources: [],
  });

  const [formData, setFormData] = useState({
    blockId: null as number | null,
    fullName: '',
    organizationId: null as number | null,
    position: '',
    influenceStatusId: null as number | null,
    influenceTypeId: null as number | null,
    usefulnessDescription: '',
    communicationChannelId: null as number | null,
    contactSourceId: null as number | null,
    nextTouchDate: '',
    notes: '',
    responsibleCuratorId: null as number | null,
  });

  useEffect(() => {
    loadData();
  }, []);

  useEffect(() => {
    if (mode === 'edit' && contactId) {
      loadContact();
    }
  }, [mode, contactId]);

  const loadData = async () => {
    try {
      const [blocksData, usersData, orgs, statuses, types, channels, sources] = await Promise.all([
        blocksApi.getAll(),
        usersApi.getAll(),
        referencesApi.getByCategory('organization'),
        referencesApi.getByCategory('influence_status'),
        referencesApi.getByCategory('influence_type'),
        referencesApi.getByCategory('communication_channel'),
        referencesApi.getByCategory('contact_source'),
      ]);

      setBlocks(blocksData);
      setUsers(usersData.filter((u: any) => u.role === 'Curator'));
      setReferences({
        organizations: orgs,
        statuses: statuses,
        types: types,
        channels: channels,
        sources: sources,
      });
    } catch (err) {
      console.error('Failed to load data:', err);
    }
  };

  const loadContact = async () => {
    try {
      const contact = await contactsApi.getById(contactId!);
      setFormData({
        blockId: contact.blockId,
        fullName: contact.fullName,
        organizationId: contact.organizationId ? Number(contact.organizationId) : null,
        position: contact.position || '',
        influenceStatusId: null, // Will need to map from influenceStatus string if needed
        influenceTypeId: null, // Will need to map from influenceType string if needed
        usefulnessDescription: contact.usefulnessDescription || '',
        communicationChannelId: contact.communicationChannelId ? Number(contact.communicationChannelId) : null,
        contactSourceId: contact.contactSourceId ? Number(contact.contactSourceId) : null,
        nextTouchDate: contact.nextTouchDate ? contact.nextTouchDate.split('T')[0] : '',
        notes: contact.notes || '',
        responsibleCuratorId: contact.responsibleCuratorId,
      });
    } catch (err) {
      setError('Не удалось загрузить контакт');
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      // Check if private key is loaded
      const privateKey = KeyManager.getPrivateKey();
      if (!privateKey) {
        // Prompt user to load private key
        setError('Пожалуйста, загрузите ваш приватный ключ для шифрования данных');
        setLoading(false);
        return;
      }

      // Check if blockId is valid
      if (!formData.blockId) {
        setError('Пожалуйста, выберите блок');
        setLoading(false);
        return;
      }

      // Get recipients for the block (admin + curators)
      const recipients = await getBlockRecipients(formData.blockId, usersApi);

      // Encrypt sensitive fields
      const encryptedFullName = await encryptField(formData.fullName, recipients);
      const encryptedNotes = formData.notes
        ? await encryptField(formData.notes, recipients)
        : null;

      const requestData: any = {
        blockId: formData.blockId,
        fullName: formData.fullName, // The API expects unencrypted fullName
        fullNameEncrypted: encryptedFullName,
        organizationId: formData.organizationId ? String(formData.organizationId) : undefined,
        position: formData.position || undefined,
        influenceStatus: formData.influenceStatusId || 0, // TODO: Map ID to enum value
        influenceType: formData.influenceTypeId || 0, // TODO: Map ID to enum value
        usefulnessDescription: formData.usefulnessDescription || undefined,
        communicationChannelId: formData.communicationChannelId ? String(formData.communicationChannelId) : undefined,
        contactSourceId: formData.contactSourceId ? String(formData.contactSourceId) : undefined,
        nextTouchDate: formData.nextTouchDate || undefined,
        notes: formData.notes || undefined,
        notesEncrypted: encryptedNotes,
        responsibleCuratorId: formData.responsibleCuratorId,
      };

      if (mode === 'create') {
        await contactsApi.create(requestData);
      } else {
        await contactsApi.update(contactId!, requestData);
      }

      router.push('/contacts');
    } catch (err: any) {
      setError(err.message || 'Не удалось сохранить контакт');
    } finally {
      setLoading(false);
    }
  };

  const handleKeyUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      try {
        await KeyManager.loadPrivateKeyFromFile(file);
        setError(null);
      } catch (err) {
        setError('Не удалось загрузить ключ');
      }
    }
  };

  return (
    <div className="max-w-2xl mx-auto">
      <h1 className="text-2xl font-bold mb-6">
        {mode === 'create' ? 'Создать контакт' : 'Редактировать контакт'}
      </h1>

      {!KeyManager.getPrivateKey() && (
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4 mb-6">
          <p className="text-yellow-800 mb-2">
            Для шифрования данных необходимо загрузить ваш приватный ключ
          </p>
          <input
            type="file"
            accept=".key"
            onChange={handleKeyUpload}
            data-testid="file-input"
            className="block w-full text-sm text-gray-500
              file:mr-4 file:py-2 file:px-4
              file:rounded-md file:border-0
              file:text-sm file:font-semibold
              file:bg-indigo-50 file:text-indigo-700
              hover:file:bg-indigo-100"
          />
        </div>
      )}

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-800 px-4 py-3 rounded mb-4">
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-6">
        <div>
          <label htmlFor="block-select" className="block text-sm font-medium text-gray-700 mb-2">
            Блок *
          </label>
          <select
            id="block-select"
            required
            value={formData.blockId || ''}
            onChange={(e) => setFormData({ ...formData, blockId: e.target.value ? parseInt(e.target.value) : null })}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
          >
            <option value="">Выберите блок</option>
            {blocks.map((block) => (
              <option key={block.id} value={block.id}>
                {block.name} ({block.code})
              </option>
            ))}
          </select>
        </div>

        <div>
          <label htmlFor="fullname-input" className="block text-sm font-medium text-gray-700 mb-2">
            ФИО *
          </label>
          <input
            id="fullname-input"
            type="text"
            required
            value={formData.fullName}
            onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
            placeholder="Введите ФИО контакта"
          />
          <p className="text-sm text-gray-500 mt-1">Это поле будет зашифровано</p>
        </div>

        <div>
          <label htmlFor="organization-select" className="block text-sm font-medium text-gray-700 mb-2">
            Организация
          </label>
          <select
            id="organization-select"
            value={formData.organizationId || ''}
            onChange={(e) => setFormData({ ...formData, organizationId: e.target.value ? parseInt(e.target.value) : null })}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
          >
            <option value="">Не указано</option>
            {references.organizations.map((org) => (
              <option key={org.id} value={org.id}>
                {org.name}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label htmlFor="position-input" className="block text-sm font-medium text-gray-700 mb-2">
            Должность
          </label>
          <input
            id="position-input"
            type="text"
            value={formData.position}
            onChange={(e) => setFormData({ ...formData, position: e.target.value })}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
            placeholder="Введите должность"
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Статус влияния
            </label>
            <select
              value={formData.influenceStatusId || ''}
              onChange={(e) => setFormData({ ...formData, influenceStatusId: e.target.value ? parseInt(e.target.value) : null })}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
            >
              <option value="">Не указан</option>
              {references.statuses.map((status) => (
                <option key={status.id} value={status.id}>
                  {status.name} - {status.description}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Тип влияния
            </label>
            <select
              value={formData.influenceTypeId || ''}
              onChange={(e) => setFormData({ ...formData, influenceTypeId: e.target.value ? parseInt(e.target.value) : null })}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
            >
              <option value="">Не указан</option>
              {references.types.map((type) => (
                <option key={type.id} value={type.id}>
                  {type.name}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div>
          <label htmlFor="usefulness-textarea" className="block text-sm font-medium text-gray-700 mb-2">
            Чем может быть полезен
          </label>
          <textarea
            id="usefulness-textarea"
            value={formData.usefulnessDescription}
            onChange={(e) => setFormData({ ...formData, usefulnessDescription: e.target.value })}
            rows={3}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
            placeholder="Опишите, чем может быть полезен контакт"
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Канал коммуникации
            </label>
            <select
              value={formData.communicationChannelId || ''}
              onChange={(e) => setFormData({ ...formData, communicationChannelId: e.target.value ? parseInt(e.target.value) : null })}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
            >
              <option value="">Не указан</option>
              {references.channels.map((channel) => (
                <option key={channel.id} value={channel.id}>
                  {channel.name}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Источник контакта
            </label>
            <select
              value={formData.contactSourceId || ''}
              onChange={(e) => setFormData({ ...formData, contactSourceId: e.target.value ? parseInt(e.target.value) : null })}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
            >
              <option value="">Не указан</option>
              {references.sources.map((source) => (
                <option key={source.id} value={source.id}>
                  {source.name}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div>
          <label htmlFor="next-touch-date" className="block text-sm font-medium text-gray-700 mb-2">
            Дата следующего касания
          </label>
          <input
            id="next-touch-date"
            type="date"
            value={formData.nextTouchDate}
            onChange={(e) => setFormData({ ...formData, nextTouchDate: e.target.value })}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Примечания
          </label>
          <textarea
            value={formData.notes}
            onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
            rows={4}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
            placeholder="Введите конфиденциальные примечания"
          />
          <p className="text-sm text-gray-500 mt-1">Это поле будет зашифровано</p>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Ответственный куратор
          </label>
          <select
            value={formData.responsibleCuratorId || ''}
            onChange={(e) => setFormData({ ...formData, responsibleCuratorId: e.target.value ? parseInt(e.target.value) : null })}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
          >
            <option value="">Не назначен</option>
            {users.map((user) => (
              <option key={user.id} value={user.id}>
                {user.login}
              </option>
            ))}
          </select>
        </div>

        <div className="flex justify-end space-x-3">
          <button
            type="button"
            onClick={() => router.push('/contacts')}
            className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50"
          >
            Отмена
          </button>
          <button
            type="submit"
            disabled={loading || !KeyManager.getPrivateKey()}
            className="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 disabled:bg-gray-400"
          >
            {loading ? 'Сохранение...' : mode === 'create' ? 'Создать' : 'Сохранить'}
          </button>
        </div>
      </form>
    </div>
  );
}