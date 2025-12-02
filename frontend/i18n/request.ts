import {getRequestConfig} from 'next-intl/server';
import {cookies, headers} from 'next/headers';

const SUPPORTED_LOCALES = ['ru', 'uk'] as const;
type SupportedLocale = typeof SUPPORTED_LOCALES[number];

function isSupportedLocale(locale: string): locale is SupportedLocale {
  return SUPPORTED_LOCALES.includes(locale as SupportedLocale);
}

export default getRequestConfig(async () => {
  // Try to get locale from cookie first
  const cookieStore = await cookies();
  const localeCookie = cookieStore.get('locale')?.value;

  if (localeCookie && isSupportedLocale(localeCookie)) {
    return {
      locale: localeCookie,
      messages: (await import(`../messages/${localeCookie}.json`)).default
    };
  }

  // Try to detect from Accept-Language header
  const headersList = await headers();
  const acceptLanguage = headersList.get('accept-language');

  if (acceptLanguage) {
    // Parse Accept-Language header (e.g., "uk-UA,uk;q=0.9,ru;q=0.8,en;q=0.7")
    const languages = acceptLanguage.split(',').map(lang => {
      const [code] = lang.trim().split(';');
      return code.split('-')[0].toLowerCase();
    });

    // Find first supported locale
    for (const lang of languages) {
      if (isSupportedLocale(lang)) {
        return {
          locale: lang,
          messages: (await import(`../messages/${lang}.json`)).default
        };
      }
    }
  }

  // Default to Russian
  const defaultLocale: SupportedLocale = 'ru';
  return {
    locale: defaultLocale,
    messages: (await import(`../messages/${defaultLocale}.json`)).default
  };
});
