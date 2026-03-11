import type { Config } from 'tailwindcss'

export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        background: '#f8fafc',
        foreground: '#0f172a',
        primary: {
          DEFAULT: '#4f46e5',
          foreground: '#ffffff',
        },
        accent: {
          DEFAULT: '#7c3aed',
          foreground: '#ffffff',
        },
        card: {
          DEFAULT: '#ffffff',
          foreground: '#0f172a',
          border: '#e2e8f0',
        }
      },
      fontFamily: {
        sans: ['Outfit', 'sans-serif'],
      },
      boxShadow: {
        'glass-sm': '0 4px 12px rgba(31, 38, 135, 0.05)',
        'glass-md': '0 8px 32px rgba(31, 38, 135, 0.1)',
      },
      backdropBlur: {
        xs: '2px',
      }
    },
  },
  plugins: [],
} satisfies Config

