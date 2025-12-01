import {getRequestConfig} from 'next-intl/server';

export default getRequestConfig(async () => {
  // Всегда используем русскую локаль
  const locale = 'ru';

  return {
    locale,
    messages: (await import(`../messages/${locale}.json`)).default
  };
});
