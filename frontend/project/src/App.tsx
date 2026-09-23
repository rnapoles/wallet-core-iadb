import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from './presentation/context/AuthProvider';
import { ToastProvider } from './presentation/context/ToastProvider';
import { AppRoutes } from './presentation/routes/AppRoutes';

export function App(): React.JSX.Element {
  return (
    <BrowserRouter>
      <ToastProvider>
        {/* AuthProvider must sit inside BrowserRouter: it navigates to /login
            itself when a session expires (see HttpClient.triggerSessionExpired). */}
        <AuthProvider>
          <AppRoutes />
        </AuthProvider>
      </ToastProvider>
    </BrowserRouter>
  );
}
