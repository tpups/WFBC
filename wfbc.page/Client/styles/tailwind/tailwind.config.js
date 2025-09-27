/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    '../../Shared/**/*.{razor,js}',
    '../../Pages/**/*.{razor,js}',
    '../../Shared/Components/**/*.{razor,js}',
    '../../../Shared/**/*.{razor,js}'
  ],
  theme: {
    colors: {
      transparent: 'transparent',
      current: 'currentColor',
      'white': '#ffffff',
      'wfbc-white-1': '#fdfffc',
      'wfbc-black-1': '#02111b',
      'wfbc-red-1': '#bf211e',
      'wfbc-blue-1': '#07689f',
      'wfbc-blue-2': '#13293d',
      'wfbc-yellow-1': '#ffc93c',
      'wfbc-grey-1': '#b9cdda',
      'wfbc-grey-2': '#9db5b2',
      'chart-palette-a-0': '#ffcb51',
      'chart-palette-a-1': '#a188ff',
      'chart-palette-a-2': '#c2ff48',
      'chart-palette-a-3': '#e41973',
      'chart-palette-a-4': '#c199bc',
      'chart-palette-a-5': '#da5fce',
      'chart-palette-a-6': '#de704b',
      'chart-palette-a-7': '#baba00',
      'chart-palette-a-8': '#008eab',
      'chart-palette-a-9': '#74c664',
      'chart-palette-a-10': '#bba775',
      'chart-palette-a-11': '#89cbb3',
    },
    fontFamily: {
      sans: ['Graphik', 'sans-serif'],
      serif: ['Merriweather', 'serif'],
    },
    screens: {
      'sm': '576px',
      // => @media (min-width: 576px) { ... }

      'md': '768px',
      // => @media (min-width: 768px) { ... }

      'lg': '992px',
      // => @media (min-width: 992px) { ... }

      'xl': '1200px',
      // => @media (min-width: 1200px) { ... }

      '2xl': '1536px',
      // => @media (min-width: 1536px) { ... }
    },
    extend: {},
  },
  plugins: [
    require('@tailwindcss/forms')
  ],
}
