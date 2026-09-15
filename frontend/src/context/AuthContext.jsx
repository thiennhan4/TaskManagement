import { createContext, useContext, useReducer, useEffect, useCallback } from 'react';
import { authApi } from '../api/authApi';
import { setAccessToken } from '../api/axiosInstance';

const AuthContext = createContext(null);

const initialState = {
  user: null,
  token: null,
  isLoading: true,
  isAuthenticated: false,
};

function authReducer(state, action) {
  switch (action.type) {
    case 'AUTH_SUCCESS':
      return {
        ...state,
        user: action.payload.user,
        token: action.payload.token,
        isLoading: false,
        isAuthenticated: true,
      };
    case 'LOGOUT':
      return { ...initialState, isLoading: false };
    case 'SET_LOADING':
      return { ...state, isLoading: action.payload };
    default:
      return state;
  }
}

export function AuthProvider({ children }) {
  const [state, dispatch] = useReducer(authReducer, initialState);

  // Attempt silent refresh on mount (check httpOnly cookie)
  useEffect(() => {
    const initAuth = async () => {
      try {
        const { data } = await authApi.refresh();
        if (data.success) {
          setAccessToken(data.data.token);
          dispatch({ type: 'AUTH_SUCCESS', payload: data.data });
        } else {
          dispatch({ type: 'LOGOUT' });
        }
      } catch {
        dispatch({ type: 'LOGOUT' });
      }
    };
    initAuth();
  }, []);

  const login = useCallback(async (email, password) => {
    const { data } = await authApi.login({ email, password });
    if (!data.success) throw new Error(data.message || 'Login failed');
    setAccessToken(data.data.token);
    dispatch({ type: 'AUTH_SUCCESS', payload: data.data });
    return data.data;
  }, []);

  const register = useCallback(async (fullName, email, password) => {
    const { data } = await authApi.register({ fullName, email, password });
    if (!data.success) throw new Error(data.message || 'Registration failed');
    setAccessToken(data.data.token);
    dispatch({ type: 'AUTH_SUCCESS', payload: data.data });
    return data.data;
  }, []);

  const googleLogin = useCallback(async (credential) => {
    const { data } = await authApi.googleLogin(credential);
    if (!data.success) throw new Error(data.message || 'Google Login failed');
    setAccessToken(data.data.token);
    dispatch({ type: 'AUTH_SUCCESS', payload: data.data });
    return data.data;
  }, []);

  const logout = useCallback(async () => {
    try {
      await authApi.logout();
    } catch {
      /* ignore logout errors */
    }
    setAccessToken(null);
    dispatch({ type: 'LOGOUT' });
  }, []);

  return (
    <AuthContext.Provider value={{ ...state, login, googleLogin, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within AuthProvider');

  const hasRole = (role) => context.user?.role === role;

  return { ...context, hasRole };
}

export default AuthContext;
