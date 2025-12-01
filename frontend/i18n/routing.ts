import {defineRouting} from 'next-intl/routing';

export const routing = defineRouting({
  // Поддерживаемые языки: русский, украинский
  locales: ['ru', 'uk'],

  // Язык по умолчанию - русский
  defaultLocale: 'ru',

  // Функция для определения языка из URL
  localePrefix: 'as-needed', // Добавляет префикс только когда необходимо
});
