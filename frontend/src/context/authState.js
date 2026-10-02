import { createContext, useContext } from 'react';
const AuthContext = createContext(null);
export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within AuthProvider');

  const hasRole = (role) => context.user?.role === role;

  return { ...context, hasRole };
}
export default AuthContext;
