/** @jest-config-loader esbuild-register */

import type { Config } from 'jest';
import { createCjsPreset } from 'jest-preset-angular/presets/index.js';

export default {
  ...createCjsPreset(),
  // Extends the preset's own pattern (which already exempts .mjs and @angular/common/locales)
  // rather than replacing it -- replacing it broke transforming Angular's own .mjs packages.
  transformIgnorePatterns: [
    String.raw`node_modules/(?!(.*\.mjs$|@angular/common/locales/.*\.js$|frappe-gantt/.*\.js$))`
  ],
  moduleNameMapper: {
    '\\.(css|scss)$': '<rootDir>/jest-style-mock.js'
  },
  setupFilesAfterEnv: ['<rootDir>/setup-jest.ts']
} satisfies Config;
