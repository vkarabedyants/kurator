import { defineConfig, globalIgnores } from "eslint/config";
import nextVitals from "eslint-config-next/core-web-vitals";
import nextTs from "eslint-config-next/typescript";

const eslintConfig = defineConfig([
  ...nextVitals,
  ...nextTs,
  // Override default ignores of eslint-config-next.
  globalIgnores([
    // Default ignores of eslint-config-next:
    ".next/**",
    "out/**",
    "build/**",
    "next-env.d.ts",
  ]),
  // Custom rules for the entire codebase
  {
    rules: {
      // Allow any in catch blocks and other places where it's hard to type
      "@typescript-eslint/no-explicit-any": "warn",
      // Allow unused vars starting with underscore
      "@typescript-eslint/no-unused-vars": ["warn", {
        "argsIgnorePattern": "^_",
        "varsIgnorePattern": "^_"
      }],
      // Allow missing dependencies in useEffect (common pattern)
      "react-hooks/exhaustive-deps": "warn",
      // Allow empty interfaces (common in API types)
      "@typescript-eslint/no-empty-object-type": "warn",
    },
  },
  // Test file specific rules
  {
    files: ["**/__tests__/**/*", "**/*.test.*", "**/*.spec.*", "**/test/**/*"],
    rules: {
      // Allow any in test files
      "@typescript-eslint/no-explicit-any": "off",
      // Allow require in test files
      "@typescript-eslint/no-require-imports": "off",
      // Allow missing display names in test mocks
      "react/display-name": "off",
    },
  },
  // Config and setup files
  {
    files: ["*.config.js", "*.config.mjs", "*.setup.js", "jest.config.js", "jest.setup.js"],
    rules: {
      // Allow require in config files
      "@typescript-eslint/no-require-imports": "off",
    },
  },
]);

export default eslintConfig;
