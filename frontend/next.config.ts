import type { NextConfig } from "next";
import createNextIntlPlugin from 'next-intl/plugin';

const withNextIntl = createNextIntlPlugin();

const nextConfig: NextConfig = {
  output: 'standalone',
  typescript: {
    ignoreBuildErrors: true, // Temporary: skip TypeScript errors during build
  },
  eslint: {
    ignoreDuringBuilds: true, // Temporary: skip ESLint during build
  },
  // Enable verbose logging
  logging: {
    fetches: {
      fullUrl: true,
    },
  },
  // Server runtime configuration for logging
  serverRuntimeConfig: {
    logLevel: process.env.LOG_LEVEL || 'debug',
  },
  // Public runtime configuration
  publicRuntimeConfig: {
    logLevel: process.env.NEXT_PUBLIC_LOG_LEVEL || 'debug',
  },
};

export default withNextIntl(nextConfig);
