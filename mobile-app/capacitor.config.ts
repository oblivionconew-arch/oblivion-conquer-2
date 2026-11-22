import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'br.com.oblivionco.mobile',
  appName: 'Oblivion Conquer',
  webDir: 'www',
  server: {
    // Carrega diretamente a interface web de producao do jogo
    url: 'https://oblivionco.com.br/play.html',
    cleartext: false,
    androidScheme: 'https',
    allowNavigation: [
      'oblivionco.com.br',
      '*.oblivionco.com.br',
      '*.supabase.co'
    ]
  },
  android: {
    allowMixedContent: false,
    backgroundColor: '#07090f',
    captureInput: true,
    webContentsDebuggingEnabled: false
  },
  plugins: {
    SplashScreen: {
      launchShowDuration: 2200,
      launchAutoHide: true,
      backgroundColor: '#07090f',
      androidSplashResourceName: 'splash',
      androidScaleType: 'CENTER_CROP',
      showSpinner: true,
      spinnerColor: '#f5b942'
    },
    StatusBar: {
      overlaysWebView: true,
      style: 'DARK',
      backgroundColor: '#07090f'
    }
  }
};

export default config;
