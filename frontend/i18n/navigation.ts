import {createNavigation} from 'next-intl/navigation';
import {routing} from './routing';

// Легковесные обёртки вокруг навигационных API Next.js
// которые учитывают конфигурацию маршрутизации
export const {Link, redirect, usePathname, useRouter, getPathname} =
  createNavigation(routing);
